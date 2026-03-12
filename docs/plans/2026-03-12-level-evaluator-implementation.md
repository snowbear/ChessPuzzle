# Level Evaluator — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a .NET tool that validates chess puzzle definitions (hard-stop checks) and computes quality metrics (solution space exploration + complexity score).

**Architecture:** Class library (`ChessPuzzle.Evaluator`) with validator pipeline (`IValidator` implementations) and solution space explorer. Thin CLI wrapper (`ChessPuzzle.Evaluator.Cli`). Shared models in `ChessPuzzle.Core`. Uses `Gera.Chess` NuGet package for chess logic.

**Tech Stack:** .NET 10, C#, xUnit, Gera.Chess NuGet package

---

### Task 0: Move Web Game Into `game/` Subfolder

**Files:**
- Move: `index.html`, `css/`, `js/`, `puzzles/`, `node_modules/`, `package.json`, `package-lock.json`, `start.bat`, `start.sh` → `game/`
- Keep at root: `CLAUDE.md`, `docs/`

**Step 1: Move files**

```bash
cd /c/work/ChessPuzzle
mkdir game
git mv index.html css js puzzles start.bat start.sh package.json package-lock.json game/
mv node_modules game/
```

**Step 2: Verify game still works**

```bash
cd game && npx http-server -p 8080
```

Open browser, confirm puzzle loads and plays.

**Step 3: Commit**

```bash
cd /c/work/ChessPuzzle
git add -A
git commit -m "chore: move web game into game/ subfolder"
```

---

### Task 1: .NET Solution Scaffolding

**Files:**
- Create: `tools/ChessPuzzleTools.sln`
- Create: `tools/NuGet.config`
- Create: `tools/ChessPuzzle.Core/ChessPuzzle.Core.csproj`
- Create: `tools/ChessPuzzle.Evaluator/ChessPuzzle.Evaluator.csproj`
- Create: `tools/ChessPuzzle.Evaluator.Cli/ChessPuzzle.Evaluator.Cli.csproj`
- Create: `tools/ChessPuzzle.Evaluator.Tests/ChessPuzzle.Evaluator.Tests.csproj`

**Step 1: Create solution and projects**

```bash
cd /c/work/ChessPuzzle
mkdir tools && cd tools

dotnet new sln -n ChessPuzzleTools

dotnet new classlib -n ChessPuzzle.Core -f net10.0
dotnet new classlib -n ChessPuzzle.Evaluator -f net10.0
dotnet new console -n ChessPuzzle.Evaluator.Cli -f net10.0
dotnet new xunit -n ChessPuzzle.Evaluator.Tests -f net10.0

dotnet sln add ChessPuzzle.Core
dotnet sln add ChessPuzzle.Evaluator
dotnet sln add ChessPuzzle.Evaluator.Cli
dotnet sln add ChessPuzzle.Evaluator.Tests
```

**Step 2: Add project references**

```bash
cd /c/work/ChessPuzzle/tools
dotnet add ChessPuzzle.Evaluator reference ChessPuzzle.Core
dotnet add ChessPuzzle.Evaluator.Cli reference ChessPuzzle.Evaluator
dotnet add ChessPuzzle.Evaluator.Cli reference ChessPuzzle.Core
dotnet add ChessPuzzle.Evaluator.Tests reference ChessPuzzle.Evaluator
dotnet add ChessPuzzle.Evaluator.Tests reference ChessPuzzle.Core
```

**Step 3: Add NuGet packages**

```bash
cd /c/work/ChessPuzzle/tools
dotnet add ChessPuzzle.Evaluator package Gera.Chess
dotnet add ChessPuzzle.Evaluator.Cli package System.Text.Json
```

**Step 4: Create local NuGet.config**

Create `tools/NuGet.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

The `<clear />` directive removes all inherited feeds (including org-internal ones), ensuring only nuget.org is used.

**Step 5: Verify build**

```bash
cd /c/work/ChessPuzzle/tools
dotnet build
```

Expected: Build succeeded, 0 errors.

**Step 6: Clean up generated boilerplate**

Delete auto-generated `Class1.cs` from ChessPuzzle.Core and ChessPuzzle.Evaluator. Delete auto-generated `UnitTest1.cs` from Tests.

**Step 7: Commit**

```bash
cd /c/work/ChessPuzzle
git add tools/
git commit -m "feat: scaffold .NET solution for evaluator tools"
```

---

### Task 2: Core Models + Puzzle Loader

**Files:**
- Create: `tools/ChessPuzzle.Core/Models/Puzzle.cs`
- Create: `tools/ChessPuzzle.Core/Models/EvaluationResult.cs`
- Create: `tools/ChessPuzzle.Core/Models/ValidationError.cs`
- Create: `tools/ChessPuzzle.Core/PuzzleLoader.cs`
- Create: `tools/ChessPuzzle.Evaluator.Tests/PuzzleLoaderTests.cs`

**Step 1: Write the failing test**

`tools/ChessPuzzle.Evaluator.Tests/PuzzleLoaderTests.cs`:

```csharp
using ChessPuzzle.Core;
using ChessPuzzle.Core.Models;

namespace ChessPuzzle.Evaluator.Tests;

public class PuzzleLoaderTests
{
    [Fact]
    public void LoadFromJson_ValidPuzzle_ParsesAllFields()
    {
        var json = """
        {
            "metadata": {
                "id": "test-001",
                "title": "Test Puzzle",
                "author": "Test",
                "difficulty": 2
            },
            "startPosition": {
                "fen": "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
                "squares": {
                    "e2": "open",
                    "d2": "open"
                }
            },
            "pieceConstraints": {
                "white": {
                    "pawn": { "min": 1, "max": 2 }
                }
            },
            "halfMoveCount": 2,
            "revealedFinalPosition": "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq - 0 1",
            "hints": [
                {
                    "scope": { "halfMove": 1 },
                    "constraints": { "piece": "pawn", "toSquare": "e4" },
                    "text": "White plays pawn to e4"
                }
            ]
        }
        """;

        var puzzle = PuzzleLoader.FromJson(json);

        Assert.Equal("test-001", puzzle.Metadata.Id);
        Assert.Equal("Test Puzzle", puzzle.Metadata.Title);
        Assert.Equal(2, puzzle.Metadata.Difficulty);
        Assert.Contains("e2", puzzle.StartPosition.Squares.Keys);
        Assert.Equal("open", puzzle.StartPosition.Squares["e2"]);
        Assert.Equal(2, puzzle.HalfMoveCount);
        Assert.Single(puzzle.Hints);
        Assert.Equal(1, puzzle.Hints[0].Scope.HalfMove);
        Assert.Equal("pawn", puzzle.Hints[0].Constraints.Piece);
        Assert.Equal("e4", puzzle.Hints[0].Constraints.ToSquare);
        Assert.NotNull(puzzle.PieceConstraints["white"]);
        Assert.Equal(1, puzzle.PieceConstraints["white"]["pawn"].Min);
        Assert.Equal(2, puzzle.PieceConstraints["white"]["pawn"].Max);
    }

    [Fact]
    public void LoadFromJson_HintWithAnyScope_ParsesCorrectly()
    {
        var json = """
        {
            "metadata": { "id": "t", "title": "t", "author": "t", "difficulty": 1 },
            "startPosition": { "fen": "8/8/8/8/8/8/8/8 w - - 0 1", "squares": {} },
            "pieceConstraints": {},
            "halfMoveCount": 1,
            "revealedFinalPosition": "8/8/8/8/8/8/8/8 w - - 0 1",
            "hints": [
                {
                    "scope": "any",
                    "constraints": { "isCapture": true },
                    "text": "A capture happens"
                }
            ]
        }
        """;

        var puzzle = PuzzleLoader.FromJson(json);

        Assert.True(puzzle.Hints[0].Scope.IsAny);
    }

    [Fact]
    public void LoadFromJson_HintWithFinalScope_ParsesCorrectly()
    {
        var json = """
        {
            "metadata": { "id": "t", "title": "t", "author": "t", "difficulty": 1 },
            "startPosition": { "fen": "8/8/8/8/8/8/8/8 w - - 0 1", "squares": {} },
            "pieceConstraints": {},
            "halfMoveCount": 1,
            "revealedFinalPosition": "8/8/8/8/8/8/8/8 w - - 0 1",
            "hints": [
                {
                    "scope": "final",
                    "constraints": { "isCheckmate": true },
                    "text": "Checkmate"
                }
            ]
        }
        """;

        var puzzle = PuzzleLoader.FromJson(json);

        Assert.True(puzzle.Hints[0].Scope.IsFinal);
    }
}
```

**Step 2: Run test to verify it fails**

```bash
cd /c/work/ChessPuzzle/tools
dotnet test ChessPuzzle.Evaluator.Tests
```

Expected: FAIL — types don't exist yet.

**Step 3: Implement models**

`tools/ChessPuzzle.Core/Models/Puzzle.cs`:

```csharp
using System.Text.Json.Serialization;

namespace ChessPuzzle.Core.Models;

public class Puzzle
{
    public PuzzleMetadata Metadata { get; set; } = new();
    public StartPosition StartPosition { get; set; } = new();
    public Dictionary<string, Dictionary<string, PieceConstraint>> PieceConstraints { get; set; } = new();
    public int HalfMoveCount { get; set; }
    public string RevealedFinalPosition { get; set; } = "";
    public List<Hint> Hints { get; set; } = new();
}

public class PuzzleMetadata
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public int Difficulty { get; set; }
}

public class StartPosition
{
    public string Fen { get; set; } = "";
    public Dictionary<string, string> Squares { get; set; } = new();
}

public class PieceConstraint
{
    public int Min { get; set; }
    public int Max { get; set; }
}

public class Hint
{
    public HintScope Scope { get; set; } = new();
    public HintConstraints Constraints { get; set; } = new();
    public string Text { get; set; } = "";
}

// HintScope needs custom deserialization because it can be:
//   { "halfMove": 3 }           → specific half-move
//   { "halfMoveRange": [1, 4] } → range
//   "any"                        → any half-move
//   "final"                      → final position
public class HintScope
{
    public int? HalfMove { get; set; }
    public int[]? HalfMoveRange { get; set; }
    public bool IsAny { get; set; }
    public bool IsFinal { get; set; }
}

public class HintConstraints
{
    public string? Color { get; set; }
    public string? Piece { get; set; }
    public string? CapturedPiece { get; set; }
    public string? ToSquare { get; set; }
    public int? ToRank { get; set; }
    public string? ToFile { get; set; }
    public string? FromSquare { get; set; }
    public int? FromRank { get; set; }
    public string? FromFile { get; set; }
    public bool? IsCheck { get; set; }
    public bool? IsCapture { get; set; }
    [JsonPropertyName("isCastle")]
    public object? IsCastle { get; set; }  // can be bool or string ("kingside"/"queenside")
    public bool? IsEnPassant { get; set; }
    public bool? IsPromotion { get; set; }
    public string? PromotionPiece { get; set; }
    public bool? IsCheckmate { get; set; }
    public bool? IsStalemate { get; set; }
}
```

`tools/ChessPuzzle.Core/Models/EvaluationResult.cs`:

```csharp
namespace ChessPuzzle.Core.Models;

public class EvaluationResult
{
    public List<ValidationError> Errors { get; set; } = new();
    public bool IsValid => Errors.Count == 0;
    public int? SolutionCount { get; set; }
    public long? SearchSpaceSize { get; set; }
    public double? Complexity { get; set; }
    public List<string> Remarks { get; set; } = new();
}
```

`tools/ChessPuzzle.Core/Models/ValidationError.cs`:

```csharp
namespace ChessPuzzle.Core.Models;

public class ValidationError
{
    public string Code { get; set; } = "";
    public string Message { get; set; } = "";

    public ValidationError() { }

    public ValidationError(string code, string message)
    {
        Code = code;
        Message = message;
    }
}
```

**Step 4: Implement PuzzleLoader with custom HintScope deserialization**

`tools/ChessPuzzle.Core/PuzzleLoader.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using ChessPuzzle.Core.Models;

namespace ChessPuzzle.Core;

public static class PuzzleLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new HintScopeConverter() }
    };

    public static Puzzle FromJson(string json)
    {
        return JsonSerializer.Deserialize<Puzzle>(json, Options)
            ?? throw new JsonException("Failed to deserialize puzzle JSON");
    }

    public static Puzzle FromFile(string path)
    {
        var json = File.ReadAllText(path);
        return FromJson(json);
    }
}

internal class HintScopeConverter : JsonConverter<HintScope>
{
    public override HintScope Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            return value switch
            {
                "any" => new HintScope { IsAny = true },
                "final" => new HintScope { IsFinal = true },
                _ => throw new JsonException($"Unknown scope string: {value}")
            };
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            var scope = new HintScope();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                var prop = reader.GetString();
                reader.Read();
                switch (prop)
                {
                    case "halfMove":
                        scope.HalfMove = reader.GetInt32();
                        break;
                    case "halfMoveRange":
                        var range = JsonSerializer.Deserialize<int[]>(ref reader);
                        scope.HalfMoveRange = range;
                        break;
                }
            }
            return scope;
        }

        throw new JsonException($"Unexpected token for HintScope: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, HintScope value, JsonSerializerOptions options)
    {
        if (value.IsAny) { writer.WriteStringValue("any"); return; }
        if (value.IsFinal) { writer.WriteStringValue("final"); return; }

        writer.WriteStartObject();
        if (value.HalfMove.HasValue)
            writer.WriteNumber("halfMove", value.HalfMove.Value);
        if (value.HalfMoveRange != null)
        {
            writer.WriteStartArray("halfMoveRange");
            foreach (var v in value.HalfMoveRange) writer.WriteNumberValue(v);
            writer.WriteEndArray();
        }
        writer.WriteEndObject();
    }
}
```

**Step 5: Run tests**

```bash
cd /c/work/ChessPuzzle/tools
dotnet test ChessPuzzle.Evaluator.Tests
```

Expected: All 3 tests PASS.

**Step 6: Commit**

```bash
cd /c/work/ChessPuzzle
git add tools/
git commit -m "feat: core models and puzzle loader with JSON deserialization"
```

---

### Task 3: Validator Pipeline Infrastructure + SchemaValidator

**Files:**
- Create: `tools/ChessPuzzle.Evaluator/IValidator.cs`
- Create: `tools/ChessPuzzle.Evaluator/Validators/SchemaValidator.cs`
- Create: `tools/ChessPuzzle.Evaluator.Tests/Validators/SchemaValidatorTests.cs`

**Step 1: Write failing tests**

`tools/ChessPuzzle.Evaluator.Tests/Validators/SchemaValidatorTests.cs`:

```csharp
using ChessPuzzle.Core.Models;
using ChessPuzzle.Evaluator.Validators;

namespace ChessPuzzle.Evaluator.Tests.Validators;

public class SchemaValidatorTests
{
    private readonly SchemaValidator _validator = new();

    private static Puzzle MakeValidPuzzle() => new()
    {
        Metadata = new PuzzleMetadata { Id = "t", Title = "t", Author = "t", Difficulty = 1 },
        StartPosition = new StartPosition { Fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1" },
        HalfMoveCount = 1,
        RevealedFinalPosition = "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq - 0 1",
        Hints = new List<Hint>
        {
            new() { Scope = new HintScope { HalfMove = 1 }, Constraints = new HintConstraints { Piece = "pawn" }, Text = "test" }
        }
    };

    [Fact]
    public void ValidPuzzle_NoErrors()
    {
        var errors = _validator.Validate(MakeValidPuzzle()).ToList();
        Assert.Empty(errors);
    }

    [Fact]
    public void MissingFen_ReturnsError()
    {
        var puzzle = MakeValidPuzzle();
        puzzle.StartPosition.Fen = "";
        var errors = _validator.Validate(puzzle).ToList();
        Assert.Contains(errors, e => e.Code == "MISSING_FEN");
    }

    [Fact]
    public void ZeroHalfMoveCount_ReturnsError()
    {
        var puzzle = MakeValidPuzzle();
        puzzle.HalfMoveCount = 0;
        var errors = _validator.Validate(puzzle).ToList();
        Assert.Contains(errors, e => e.Code == "INVALID_HALF_MOVE_COUNT");
    }

    [Fact]
    public void NegativeMin_ReturnsError()
    {
        var puzzle = MakeValidPuzzle();
        puzzle.PieceConstraints["white"] = new Dictionary<string, PieceConstraint>
        {
            ["pawn"] = new() { Min = -1, Max = 2 }
        };
        var errors = _validator.Validate(puzzle).ToList();
        Assert.Contains(errors, e => e.Code == "INVALID_PIECE_CONSTRAINT");
    }

    [Fact]
    public void MinGreaterThanMax_ReturnsError()
    {
        var puzzle = MakeValidPuzzle();
        puzzle.PieceConstraints["white"] = new Dictionary<string, PieceConstraint>
        {
            ["pawn"] = new() { Min = 3, Max = 1 }
        };
        var errors = _validator.Validate(puzzle).ToList();
        Assert.Contains(errors, e => e.Code == "INVALID_PIECE_CONSTRAINT");
    }
}
```

**Step 2: Run test to verify it fails**

```bash
cd /c/work/ChessPuzzle/tools
dotnet test ChessPuzzle.Evaluator.Tests
```

**Step 3: Implement**

`tools/ChessPuzzle.Evaluator/IValidator.cs`:

```csharp
using ChessPuzzle.Core.Models;

namespace ChessPuzzle.Evaluator;

public interface IValidator
{
    IEnumerable<ValidationError> Validate(Puzzle puzzle);
}
```

`tools/ChessPuzzle.Evaluator/Validators/SchemaValidator.cs`:

```csharp
using ChessPuzzle.Core.Models;

namespace ChessPuzzle.Evaluator.Validators;

public class SchemaValidator : IValidator
{
    public IEnumerable<ValidationError> Validate(Puzzle puzzle)
    {
        if (string.IsNullOrWhiteSpace(puzzle.Metadata.Id))
            yield return new ValidationError("MISSING_ID", "Puzzle metadata.id is required");

        if (string.IsNullOrWhiteSpace(puzzle.Metadata.Title))
            yield return new ValidationError("MISSING_TITLE", "Puzzle metadata.title is required");

        if (string.IsNullOrWhiteSpace(puzzle.StartPosition.Fen))
            yield return new ValidationError("MISSING_FEN", "startPosition.fen is required");

        if (puzzle.HalfMoveCount <= 0)
            yield return new ValidationError("INVALID_HALF_MOVE_COUNT", "halfMoveCount must be positive");

        if (string.IsNullOrWhiteSpace(puzzle.RevealedFinalPosition))
            yield return new ValidationError("MISSING_FINAL_POSITION", "revealedFinalPosition is required");

        foreach (var (color, pieces) in puzzle.PieceConstraints)
        {
            foreach (var (pieceType, constraint) in pieces)
            {
                if (constraint.Min < 0 || constraint.Max < 0 || constraint.Min > constraint.Max)
                    yield return new ValidationError("INVALID_PIECE_CONSTRAINT",
                        $"Piece constraint {color}.{pieceType}: min={constraint.Min}, max={constraint.Max} is invalid");
            }
        }
    }
}
```

**Step 4: Run tests**

```bash
cd /c/work/ChessPuzzle/tools
dotnet test ChessPuzzle.Evaluator.Tests
```

Expected: All PASS.

**Step 5: Commit**

```bash
cd /c/work/ChessPuzzle
git add tools/
git commit -m "feat: validator pipeline interface + schema validator"
```

---

### Task 4: FenValidator + OpenSquareConsistencyValidator

**Files:**
- Create: `tools/ChessPuzzle.Evaluator/Validators/FenValidator.cs`
- Create: `tools/ChessPuzzle.Evaluator/Validators/OpenSquareConsistencyValidator.cs`
- Create: `tools/ChessPuzzle.Evaluator.Tests/Validators/FenValidatorTests.cs`
- Create: `tools/ChessPuzzle.Evaluator.Tests/Validators/OpenSquareConsistencyValidatorTests.cs`

**Step 1: Write failing tests**

`FenValidatorTests.cs`:

```csharp
using ChessPuzzle.Core.Models;
using ChessPuzzle.Evaluator.Validators;

namespace ChessPuzzle.Evaluator.Tests.Validators;

public class FenValidatorTests
{
    private readonly FenValidator _validator = new();

    [Fact]
    public void ValidFen_NoErrors()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        Assert.Empty(_validator.Validate(puzzle));
    }

    [Fact]
    public void GarbageFen_ReturnsError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.StartPosition.Fen = "not-a-fen";
        var errors = _validator.Validate(puzzle).ToList();
        Assert.Contains(errors, e => e.Code == "INVALID_FEN");
    }

    [Fact]
    public void InvalidRevealedFinalPosition_ReturnsError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.RevealedFinalPosition = "garbage";
        var errors = _validator.Validate(puzzle).ToList();
        Assert.Contains(errors, e => e.Code == "INVALID_FINAL_FEN");
    }
}
```

`OpenSquareConsistencyValidatorTests.cs`:

```csharp
using ChessPuzzle.Core.Models;
using ChessPuzzle.Evaluator.Validators;

namespace ChessPuzzle.Evaluator.Tests.Validators;

public class OpenSquareConsistencyValidatorTests
{
    private readonly OpenSquareConsistencyValidator _validator = new();

    [Fact]
    public void OccupiedSquareMarkedOpen_ReturnsError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        // a1 has a rook in the starting FEN
        puzzle.StartPosition.Squares["a1"] = "open";
        var errors = _validator.Validate(puzzle).ToList();
        Assert.Contains(errors, e => e.Code == "OPEN_SQUARE_OCCUPIED");
    }

    [Fact]
    public void EmptySquareMarkedOpen_NoError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        // e4 is empty in starting position
        puzzle.StartPosition.Squares["e4"] = "open";
        Assert.Empty(_validator.Validate(puzzle));
    }

    [Fact]
    public void EmptySquareNotMarkedOpen_NoError()
    {
        // This is fine — not marking an empty square as open is valid
        var puzzle = TestHelper.MakeValidPuzzle();
        Assert.Empty(_validator.Validate(puzzle));
    }
}
```

Also extract a shared `TestHelper` class used across test files:

`tools/ChessPuzzle.Evaluator.Tests/TestHelper.cs`:

```csharp
using ChessPuzzle.Core.Models;

namespace ChessPuzzle.Evaluator.Tests;

public static class TestHelper
{
    public static Puzzle MakeValidPuzzle() => new()
    {
        Metadata = new PuzzleMetadata { Id = "test-001", Title = "Test", Author = "Test", Difficulty = 1 },
        StartPosition = new StartPosition
        {
            Fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
            Squares = new Dictionary<string, string>()
        },
        PieceConstraints = new(),
        HalfMoveCount = 2,
        RevealedFinalPosition = "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq - 0 1",
        Hints = new List<Hint>()
    };
}
```

Refactor `SchemaValidatorTests` to use `TestHelper.MakeValidPuzzle()` too.

**Step 2: Implement validators**

`FenValidator.cs` — use `ChessBoard.LoadFromFen()` in a try/catch. If it throws, the FEN is invalid.

```csharp
using Chess;
using ChessPuzzle.Core.Models;

namespace ChessPuzzle.Evaluator.Validators;

public class FenValidator : IValidator
{
    public IEnumerable<ValidationError> Validate(Puzzle puzzle)
    {
        if (!TryParseFen(puzzle.StartPosition.Fen))
            yield return new ValidationError("INVALID_FEN",
                $"Cannot parse starting FEN: {puzzle.StartPosition.Fen}");

        if (!TryParseFen(puzzle.RevealedFinalPosition))
            yield return new ValidationError("INVALID_FINAL_FEN",
                $"Cannot parse revealed final position FEN: {puzzle.RevealedFinalPosition}");
    }

    private static bool TryParseFen(string fen)
    {
        try
        {
            ChessBoard.LoadFromFen(fen);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
```

`OpenSquareConsistencyValidator.cs` — load FEN, check each square marked "open" to see if it's occupied:

```csharp
using Chess;
using ChessPuzzle.Core.Models;

namespace ChessPuzzle.Evaluator.Validators;

public class OpenSquareConsistencyValidator : IValidator
{
    public IEnumerable<ValidationError> Validate(Puzzle puzzle)
    {
        ChessBoard board;
        try { board = ChessBoard.LoadFromFen(puzzle.StartPosition.Fen); }
        catch { yield break; } // FenValidator handles this

        foreach (var (square, state) in puzzle.StartPosition.Squares)
        {
            if (state != "open") continue;
            if (board[square] != null)
                yield return new ValidationError("OPEN_SQUARE_OCCUPIED",
                    $"Square {square} is marked open but is occupied in the starting FEN");
        }
    }
}
```

**Step 3: Run tests, verify pass**

**Step 4: Commit**

```bash
git commit -m "feat: FEN validator and open-square consistency validator"
```

---

### Task 5: KingConstraintValidator

**Files:**
- Create: `tools/ChessPuzzle.Evaluator/Validators/KingConstraintValidator.cs`
- Create: `tools/ChessPuzzle.Evaluator.Tests/Validators/KingConstraintValidatorTests.cs`

**Step 1: Write failing tests**

```csharp
using ChessPuzzle.Core.Models;
using ChessPuzzle.Evaluator.Validators;

namespace ChessPuzzle.Evaluator.Tests.Validators;

public class KingConstraintValidatorTests
{
    private readonly KingConstraintValidator _validator = new();

    [Fact]
    public void KingOnBoard_ConstraintMinMax0_NoError()
    {
        // Standard starting position has both kings
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.PieceConstraints["white"] = new() { ["king"] = new() { Min = 0, Max = 0 } };
        Assert.Empty(_validator.Validate(puzzle));
    }

    [Fact]
    public void KingOnBoard_ConstraintAllowsPlacement_ReturnsError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.PieceConstraints["white"] = new() { ["king"] = new() { Min = 0, Max = 1 } };
        var errors = _validator.Validate(puzzle).ToList();
        Assert.Contains(errors, e => e.Code == "KING_CONSTRAINT_MISMATCH");
    }

    [Fact]
    public void KingMissing_NoConstraint_ReturnsError()
    {
        // FEN with no white king
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.StartPosition.Fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQ1BNR w kq - 0 1";
        // No piece constraints for white king
        var errors = _validator.Validate(puzzle).ToList();
        Assert.Contains(errors, e => e.Code == "KING_CONSTRAINT_MISMATCH");
    }

    [Fact]
    public void KingMissing_ConstraintMinMax1_NoError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.StartPosition.Fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQ1BNR w kq - 0 1";
        puzzle.PieceConstraints["white"] = new() { ["king"] = new() { Min = 1, Max = 1 } };
        Assert.Empty(_validator.Validate(puzzle));
    }
}
```

Note: The FEN `RNBQ1BNR` with no white king may not parse in Gera.Chess. If so, adjust the test to use a valid position without a king by placing pieces differently, or handle the case where FEN parsing fails (skip validation — FenValidator catches it). Adapt as needed at implementation time.

**Step 2: Implement**

The validator loads the FEN, finds whether each color's king exists on the board, then checks the piece constraints match. If FEN can't be parsed, yield break (FenValidator handles that).

**Step 3: Run tests, verify pass**

**Step 4: Commit**

```bash
git commit -m "feat: king constraint validator"
```

---

### Task 6: CastlingHintValidator

**Files:**
- Create: `tools/ChessPuzzle.Evaluator/Validators/CastlingHintValidator.cs`
- Create: `tools/ChessPuzzle.Evaluator.Tests/Validators/CastlingHintValidatorTests.cs`

**Step 1: Write failing tests**

```csharp
using ChessPuzzle.Core.Models;
using ChessPuzzle.Evaluator.Validators;

namespace ChessPuzzle.Evaluator.Tests.Validators;

public class CastlingHintValidatorTests
{
    private readonly CastlingHintValidator _validator = new();

    [Fact]
    public void CastleHint_WithCastlingRights_NoError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.Hints.Add(new Hint
        {
            Scope = new HintScope { HalfMove = 1 },
            Constraints = new HintConstraints { IsCastle = "kingside" },
            Text = "White castles kingside"
        });
        Assert.Empty(_validator.Validate(puzzle));
    }

    [Fact]
    public void CastleHint_NoCastlingRights_ReturnsError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.StartPosition.Fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w - - 0 1"; // no castling rights
        puzzle.Hints.Add(new Hint
        {
            Scope = new HintScope { HalfMove = 1 },
            Constraints = new HintConstraints { IsCastle = "kingside" },
            Text = "White castles kingside"
        });
        var errors = _validator.Validate(puzzle).ToList();
        Assert.Contains(errors, e => e.Code == "CASTLE_IMPOSSIBLE");
    }

    [Fact]
    public void CastleHint_BlackQueenside_NoCastlingRights_ReturnsError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.StartPosition.Fen = "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b K - 0 1"; // only white kingside
        puzzle.Hints.Add(new Hint
        {
            Scope = new HintScope { HalfMove = 2 },
            Constraints = new HintConstraints { IsCastle = "queenside" },
            Text = "Black castles queenside"
        });
        var errors = _validator.Validate(puzzle).ToList();
        Assert.Contains(errors, e => e.Code == "CASTLE_IMPOSSIBLE");
    }
}
```

**Step 2: Implement**

Parse castling rights from the FEN string (4th field). For each hint with `IsCastle`, determine which color moves on that half-move (odd = white, even = black). Check that the relevant castling right exists. For "any" scope, check that at least one color has the relevant castling right.

Also check that the king and rook are on their starting squares in the FEN (king on e1/e8, rook on a1/h1/a8/h8 as appropriate). If the square is "open" (piece to be placed), that's OK — the player might place the right piece there.

**Step 3: Run tests, verify pass**

**Step 4: Commit**

```bash
git commit -m "feat: castling hint validator"
```

---

### Task 7: PieceExistenceValidator

**Files:**
- Create: `tools/ChessPuzzle.Evaluator/Validators/PieceExistenceValidator.cs`
- Create: `tools/ChessPuzzle.Evaluator.Tests/Validators/PieceExistenceValidatorTests.cs`

**Step 1: Write failing tests**

```csharp
using ChessPuzzle.Core.Models;
using ChessPuzzle.Evaluator.Validators;

namespace ChessPuzzle.Evaluator.Tests.Validators;

public class PieceExistenceValidatorTests
{
    private readonly PieceExistenceValidator _validator = new();

    [Fact]
    public void HintReferencesPieceOnBoard_NoError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.Hints.Add(new Hint
        {
            Scope = new HintScope { HalfMove = 1 },
            Constraints = new HintConstraints { Piece = "knight" },
            Text = "A knight moves"
        });
        Assert.Empty(_validator.Validate(puzzle));
    }

    [Fact]
    public void HintReferencesPieceNotOnBoardButInConstraints_NoError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.StartPosition.Fen = "4k3/8/8/8/8/8/8/4K3 w - - 0 1"; // only kings
        puzzle.StartPosition.Squares["e4"] = "open";
        puzzle.PieceConstraints["white"] = new() { ["queen"] = new() { Min = 1, Max = 1 } };
        puzzle.Hints.Add(new Hint
        {
            Scope = new HintScope { HalfMove = 1 },
            Constraints = new HintConstraints { Piece = "queen" },
            Text = "Queen moves"
        });
        Assert.Empty(_validator.Validate(puzzle));
    }

    [Fact]
    public void HintReferencesPieceNowhere_NoPawnsForPromotion_ReturnsError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.StartPosition.Fen = "4k3/8/8/8/8/8/8/4K3 w - - 0 1";
        puzzle.Hints.Add(new Hint
        {
            Scope = new HintScope { HalfMove = 1 },
            Constraints = new HintConstraints { Piece = "queen" },
            Text = "Queen moves"
        });
        var errors = _validator.Validate(puzzle).ToList();
        Assert.Contains(errors, e => e.Code == "PIECE_NOT_AVAILABLE");
    }

    [Fact]
    public void HintReferencesPieceNotOnBoard_ButPawnsExist_NoError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.StartPosition.Fen = "4k3/8/8/8/8/8/P7/4K3 w - - 0 1"; // white pawn exists
        puzzle.Hints.Add(new Hint
        {
            Scope = new HintScope { HalfMove = 1 },
            Constraints = new HintConstraints { Piece = "queen" },
            Text = "Queen moves"
        });
        // Pawn could promote to queen, so no error
        Assert.Empty(_validator.Validate(puzzle));
    }
}
```

**Step 2: Implement**

For each hint with a `Piece` constraint, determine the color (from hint scope half-move parity, or from explicit `Color` constraint). Check:
1. Does a piece of that type+color exist on the board? → OK
2. Is it in piece constraints for that color? → OK
3. Does a pawn of that color exist on board or in constraints? → OK (promotion possible)
4. None of the above → error

**Step 3: Run tests, verify pass**

**Step 4: Commit**

```bash
git commit -m "feat: piece existence validator"
```

---

### Task 8: PawnCaptureReachValidator + EnPassantFirstMoveValidator

**Files:**
- Create: `tools/ChessPuzzle.Evaluator/Validators/PawnCaptureReachValidator.cs`
- Create: `tools/ChessPuzzle.Evaluator/Validators/EnPassantFirstMoveValidator.cs`
- Create: `tools/ChessPuzzle.Evaluator.Tests/Validators/PawnCaptureReachValidatorTests.cs`
- Create: `tools/ChessPuzzle.Evaluator.Tests/Validators/EnPassantFirstMoveValidatorTests.cs`

**Step 1: Write failing tests for EnPassantFirstMoveValidator**

```csharp
using ChessPuzzle.Core.Models;
using ChessPuzzle.Evaluator.Validators;

namespace ChessPuzzle.Evaluator.Tests.Validators;

public class EnPassantFirstMoveValidatorTests
{
    private readonly EnPassantFirstMoveValidator _validator = new();

    [Fact]
    public void EnPassantHintOnMove1_NoEnPassantInFen_ReturnsError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.Hints.Add(new Hint
        {
            Scope = new HintScope { HalfMove = 1 },
            Constraints = new HintConstraints { IsEnPassant = true },
            Text = "En passant on first move"
        });
        // Default FEN has no en passant target (dash in 4th field)
        var errors = _validator.Validate(puzzle).ToList();
        Assert.Contains(errors, e => e.Code == "EN_PASSANT_IMPOSSIBLE");
    }

    [Fact]
    public void EnPassantHintOnMove1_EnPassantInFen_NoError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.StartPosition.Fen = "rnbqkbnr/pppp1ppp/8/4pP2/8/8/PPPPP1PP/RNBQKBNR w KQkq e6 0 3";
        puzzle.Hints.Add(new Hint
        {
            Scope = new HintScope { HalfMove = 1 },
            Constraints = new HintConstraints { IsEnPassant = true },
            Text = "En passant on first move"
        });
        Assert.Empty(_validator.Validate(puzzle));
    }

    [Fact]
    public void EnPassantHintOnMove3_NoEnPassantInFen_NoError()
    {
        // Not first move — can't validate statically, skip
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.Hints.Add(new Hint
        {
            Scope = new HintScope { HalfMove = 3 },
            Constraints = new HintConstraints { IsEnPassant = true },
            Text = "En passant on move 3"
        });
        Assert.Empty(_validator.Validate(puzzle));
    }
}
```

**Step 2: Write failing tests for PawnCaptureReachValidator**

```csharp
using ChessPuzzle.Core.Models;
using ChessPuzzle.Evaluator.Validators;

namespace ChessPuzzle.Evaluator.Tests.Validators;

public class PawnCaptureReachValidatorTests
{
    private readonly PawnCaptureReachValidator _validator = new();

    [Fact]
    public void PawnCaptureHint_PawnInReach_NoError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        // Standard starting position has pawns
        puzzle.Hints.Add(new Hint
        {
            Scope = new HintScope { HalfMove = 1 },
            Constraints = new HintConstraints { Piece = "pawn", IsCapture = true, ToSquare = "d3" },
            Text = "Pawn captures on d3"
        });
        Assert.Empty(_validator.Validate(puzzle));
    }

    [Fact]
    public void PawnCaptureHint_NoPawnCanReach_ReturnsError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.StartPosition.Fen = "4k3/8/8/8/8/8/7P/4K3 w - - 0 1"; // single white pawn on h2
        puzzle.PieceConstraints = new();
        puzzle.Hints.Add(new Hint
        {
            Scope = new HintScope { HalfMove = 1 },
            Constraints = new HintConstraints { Piece = "pawn", IsCapture = true, ToSquare = "a3" },
            Text = "Pawn captures on a3"
        });
        var errors = _validator.Validate(puzzle).ToList();
        Assert.Contains(errors, e => e.Code == "PAWN_CAPTURE_UNREACHABLE");
    }
}
```

**Step 3: Implement both validators**

`EnPassantFirstMoveValidator` — straightforward: if any hint has `IsEnPassant = true` and scope is `halfMove = 1`, check FEN's en passant field (6th space-delimited token) is not "-".

`PawnCaptureReachValidator` — for hints with `piece = "pawn"` and `isCapture = true` and a `toSquare` specified: find all pawns of the relevant color on the board + open squares where pawns from constraints could be placed. Check if any pawn could diagonally reach the target square within the number of half-moves available. For a quick check: a white pawn on file f can capture on files e or g, and advances at most 1 rank per move. If no pawn is geometrically close enough, error.

**Step 4: Run tests, verify pass**

**Step 5: Commit**

```bash
git commit -m "feat: pawn capture reach and en passant first-move validators"
```

---

### Task 9: Evaluator Orchestrator

**Files:**
- Create: `tools/ChessPuzzle.Evaluator/Evaluator.cs`
- Create: `tools/ChessPuzzle.Evaluator.Tests/EvaluatorTests.cs`

**Step 1: Write failing tests**

```csharp
using ChessPuzzle.Core.Models;

namespace ChessPuzzle.Evaluator.Tests;

public class EvaluatorTests
{
    [Fact]
    public void ValidPuzzle_RunsValidationOnly_ReturnsValid()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        var evaluator = new PuzzleEvaluator();

        var result = evaluator.ValidateOnly(puzzle);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Null(result.SolutionCount); // metrics not computed
    }

    [Fact]
    public void InvalidPuzzle_ReturnsErrors_SkipsMetrics()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.StartPosition.Fen = ""; // triggers SchemaValidator

        var evaluator = new PuzzleEvaluator();
        var result = evaluator.Evaluate(puzzle);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Null(result.SolutionCount);
    }
}
```

**Step 2: Implement**

`tools/ChessPuzzle.Evaluator/Evaluator.cs`:

```csharp
using ChessPuzzle.Core.Models;
using ChessPuzzle.Evaluator.Validators;

namespace ChessPuzzle.Evaluator;

public class PuzzleEvaluator
{
    private readonly List<IValidator> _validators = new()
    {
        new SchemaValidator(),
        new FenValidator(),
        new OpenSquareConsistencyValidator(),
        new KingConstraintValidator(),
        new CastlingHintValidator(),
        new PieceExistenceValidator(),
        new PawnCaptureReachValidator(),
        new EnPassantFirstMoveValidator(),
    };

    public EvaluationResult ValidateOnly(Puzzle puzzle)
    {
        var result = new EvaluationResult();
        foreach (var validator in _validators)
            result.Errors.AddRange(validator.Validate(puzzle));
        return result;
    }

    public EvaluationResult Evaluate(Puzzle puzzle)
    {
        var result = ValidateOnly(puzzle);
        if (!result.IsValid)
            return result;

        var exploration = SolutionSpaceExplorer.Explore(puzzle);
        result.SolutionCount = exploration.SolutionCount;
        result.SearchSpaceSize = exploration.SearchSpaceSize;

        if (exploration.SearchSpaceSize > 0)
            result.Complexity = (double)exploration.SolutionCount / exploration.SearchSpaceSize;
        else
            result.Complexity = 0;

        // Add remarks
        if (exploration.SolutionCount == 0)
            result.Remarks.Add("No valid solutions found");
        else if (exploration.SolutionCount == 1)
            result.Remarks.Add("Single unique solution");
        else
            result.Remarks.Add($"{exploration.SolutionCount} valid solutions found");

        return result;
    }
}
```

**Step 3: Run tests, verify pass**

**Step 4: Commit**

```bash
git commit -m "feat: evaluator orchestrator with validation and metrics pipeline"
```

---

### Task 10: SolutionSpaceExplorer

This is the most complex task. It implements the three-phase brute-force search.

**Files:**
- Create: `tools/ChessPuzzle.Evaluator/SolutionSpaceExplorer.cs`
- Create: `tools/ChessPuzzle.Evaluator/ExplorationResult.cs`
- Create: `tools/ChessPuzzle.Evaluator/Solution.cs`
- Create: `tools/ChessPuzzle.Evaluator.Tests/SolutionSpaceExplorerTests.cs`

**Step 1: Write failing tests**

Use puzzle-001.json as a known-good puzzle (4 half-moves, 2 open squares, 2 pieces to place → should have exactly 1 solution in a small search space).

```csharp
using ChessPuzzle.Core;
using ChessPuzzle.Core.Models;

namespace ChessPuzzle.Evaluator.Tests;

public class SolutionSpaceExplorerTests
{
    [Fact]
    public void SimplePuzzle_FindsSolution()
    {
        // 2 pieces, 2 open squares → 2 placements
        var puzzle = new Puzzle
        {
            StartPosition = new StartPosition
            {
                Fen = "4k3/8/8/8/8/8/8/4K3 w - - 0 1",
                Squares = new() { ["e2"] = "open", ["d2"] = "open" }
            },
            PieceConstraints = new()
            {
                ["white"] = new()
                {
                    ["queen"] = new() { Min = 1, Max = 1 },
                    ["rook"] = new() { Min = 1, Max = 1 }
                }
            },
            HalfMoveCount = 1,
            RevealedFinalPosition = "4k3/8/8/8/8/8/3Q4/4K3 w - - 0 1",
            Hints = new()
            {
                new Hint
                {
                    Scope = new HintScope { HalfMove = 1 },
                    Constraints = new HintConstraints { Piece = "rook" },
                    Text = "Rook moves"
                }
            }
        };

        // Customize this test based on actual puzzle logic — the key point is:
        // the explorer should find at least one solution and report search space size
        var result = SolutionSpaceExplorer.Explore(puzzle);

        Assert.True(result.SearchSpaceSize > 0);
        Assert.True(result.SolutionCount >= 0);
    }

    [Fact]
    public void NoPossibleSolution_ReturnsZero()
    {
        var puzzle = new Puzzle
        {
            StartPosition = new StartPosition
            {
                Fen = "4k3/8/8/8/8/8/8/4K3 w - - 0 1",
                Squares = new()
            },
            PieceConstraints = new(),
            HalfMoveCount = 1,
            // Revealed position demands a queen on d4 but no queen exists
            RevealedFinalPosition = "4k3/8/8/8/3Q4/8/8/4K3 w - - 0 1",
            Hints = new()
        };

        var result = SolutionSpaceExplorer.Explore(puzzle);

        Assert.Equal(0, result.SolutionCount);
    }
}
```

**Step 2: Implement**

`tools/ChessPuzzle.Evaluator/ExplorationResult.cs`:

```csharp
namespace ChessPuzzle.Evaluator;

public class ExplorationResult
{
    public int SolutionCount { get; set; }
    public long SearchSpaceSize { get; set; }
    public List<Solution> Solutions { get; set; } = new();
}
```

`tools/ChessPuzzle.Evaluator/Solution.cs`:

```csharp
namespace ChessPuzzle.Evaluator;

public class Solution
{
    public Dictionary<string, string> Placement { get; set; } = new(); // square → "white queen" etc.
    public List<string> Moves { get; set; } = new(); // SAN notation
}
```

`tools/ChessPuzzle.Evaluator/SolutionSpaceExplorer.cs`:

```csharp
using Chess;
using ChessPuzzle.Core.Models;

namespace ChessPuzzle.Evaluator;

public static class SolutionSpaceExplorer
{
    public static ExplorationResult Explore(Puzzle puzzle)
    {
        var result = new ExplorationResult();

        // Phase 1: Enumerate all valid piece placements
        var placements = EnumeratePlacements(puzzle);

        foreach (var placement in placements)
        {
            // Build FEN with placed pieces
            var board = ApplyPlacement(puzzle.StartPosition.Fen, placement);
            if (board == null) continue;

            // Phase 2: Enumerate all legal move sequences of length halfMoveCount
            var sequences = EnumerateMoveSequences(board, puzzle.HalfMoveCount);

            foreach (var sequence in sequences)
            {
                result.SearchSpaceSize++;

                // Phase 3: Validate final position + hints
                if (ValidateSequence(puzzle, placement, sequence))
                {
                    result.SolutionCount++;
                    result.Solutions.Add(new Solution
                    {
                        Placement = placement.ToDictionary(
                            kv => kv.Key,
                            kv => $"{kv.Value.color} {kv.Value.pieceType}"),
                        Moves = sequence.ToList()
                    });
                }
            }
        }

        return result;
    }

    // Returns all combinations of pieces on open squares
    // Each placement is a dict: square → (color, pieceType)
    private static IEnumerable<Dictionary<string, (string color, string pieceType)>> EnumeratePlacements(Puzzle puzzle)
    {
        // Build list of pieces to place: expand constraints into individual pieces
        // e.g. white knight min=2, max=2 → [("white","knight"), ("white","knight")]
        // For variable counts (min != max), enumerate all valid counts
        // Then enumerate all permutations of placing these pieces on open squares

        // Implementation: recursive backtracking over open squares
        // ... (detailed implementation at coding time)
        throw new NotImplementedException();
    }

    private static ChessBoard? ApplyPlacement(string baseFen, Dictionary<string, (string color, string pieceType)> placement)
    {
        // Parse FEN, set pieces on placed squares, return new board
        // If resulting position is illegal (e.g. side-to-move king in check from placement), return null
        throw new NotImplementedException();
    }

    private static IEnumerable<List<string>> EnumerateMoveSequences(ChessBoard startBoard, int depth)
    {
        // DFS: at each ply, iterate board.Moves(), make move, recurse
        // Yield complete sequences of exactly `depth` moves
        throw new NotImplementedException();
    }

    private static bool ValidateSequence(
        Puzzle puzzle,
        Dictionary<string, (string color, string pieceType)> placement,
        List<string> moves)
    {
        // 1. Replay moves from placement position
        // 2. Check final board matches revealedFinalPosition on revealed squares
        // 3. Check all hints are satisfied
        throw new NotImplementedException();
    }
}
```

Key implementation notes:
- **`EnumeratePlacements`**: For each piece type with min/max, enumerate counts from min to max. For each count combination, enumerate all ways to assign pieces to open squares (permutations, since piece identity matters — a knight on e4 + bishop on d4 ≠ bishop on e4 + knight on d4).
- **`EnumerateMoveSequences`**: DFS with `board.Moves()` at each ply. Clone board state before recursing. Yield sequences of exactly `halfMoveCount` length.
- **`ValidateSequence`**: Replay the sequence on a fresh board from the placement. Compare final position against `revealedFinalPosition` FEN. Evaluate each hint against the move history.
- **Hint evaluation**: For each hint, based on scope, find the relevant move(s). For each constraint, check the move's properties (piece type, capture, check, etc.). Use Gera.Chess properties: `board.IsEndGame`, `board.EndGame.EndgameType`, `board.WhiteKingChecked`/`board.BlackKingChecked`, piece on square, etc.

**Step 3: Run tests iteratively as you implement each phase**

**Step 4: Test with actual puzzle files**

```bash
cd /c/work/ChessPuzzle/tools
# Add a simple integration test that loads puzzle-001.json
```

**Step 5: Commit**

```bash
git commit -m "feat: solution space explorer with brute-force search"
```

---

### Task 11: Integration Test With Real Puzzles

**Files:**
- Create: `tools/ChessPuzzle.Evaluator.Tests/IntegrationTests.cs`

**Step 1: Write tests using actual puzzle files**

```csharp
using ChessPuzzle.Core;

namespace ChessPuzzle.Evaluator.Tests;

public class IntegrationTests
{
    private readonly PuzzleEvaluator _evaluator = new();

    [Theory]
    [InlineData("../../../../game/puzzles/puzzle-001.json")]
    [InlineData("../../../../game/puzzles/puzzle-002.json")]
    public void ExistingPuzzles_AreValid(string path)
    {
        var puzzle = PuzzleLoader.FromFile(path);
        var result = _evaluator.ValidateOnly(puzzle);
        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Message}")));
    }

    [Theory]
    [InlineData("../../../../game/puzzles/puzzle-001.json")]
    [InlineData("../../../../game/puzzles/puzzle-002.json")]
    public void ExistingPuzzles_HaveAtLeastOneSolution(string path)
    {
        var puzzle = PuzzleLoader.FromFile(path);
        var result = _evaluator.Evaluate(puzzle);
        Assert.True(result.IsValid);
        Assert.True(result.SolutionCount > 0, "Expected at least one solution");
    }
}
```

**Step 2: Run and iterate until both puzzles pass**

**Step 3: Commit**

```bash
git commit -m "feat: integration tests with real puzzle files"
```

---

### Task 12: CLI

**Files:**
- Modify: `tools/ChessPuzzle.Evaluator.Cli/Program.cs`

**Step 1: Implement**

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using ChessPuzzle.Core;
using ChessPuzzle.Evaluator;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: chesspuzzle-eval <puzzle.json> [puzzle2.json] [directory/]");
    return 1;
}

var files = new List<string>();
foreach (var arg in args)
{
    if (Directory.Exists(arg))
        files.AddRange(Directory.GetFiles(arg, "*.json"));
    else if (File.Exists(arg))
        files.Add(arg);
    else
    {
        Console.Error.WriteLine($"File not found: {arg}");
        return 1;
    }
}

var evaluator = new PuzzleEvaluator();
var results = new List<object>();

foreach (var file in files)
{
    var puzzle = PuzzleLoader.FromFile(file);
    var evalResult = evaluator.Evaluate(puzzle);

    results.Add(new
    {
        file = Path.GetFileName(file),
        valid = evalResult.IsValid,
        errors = evalResult.Errors.Select(e => new { code = e.Code, message = e.Message }),
        solutionCount = evalResult.SolutionCount,
        searchSpaceSize = evalResult.SearchSpaceSize,
        complexity = evalResult.Complexity,
        remarks = evalResult.Remarks
    });
}

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

var output = results.Count == 1
    ? JsonSerializer.Serialize(results[0], jsonOptions)
    : JsonSerializer.Serialize(results, jsonOptions);

Console.WriteLine(output);

return results.All(r => ((dynamic)r).valid) ? 0 : 1;
```

**Step 2: Test manually**

```bash
cd /c/work/ChessPuzzle/tools
dotnet run --project ChessPuzzle.Evaluator.Cli -- ../game/puzzles/puzzle-001.json
dotnet run --project ChessPuzzle.Evaluator.Cli -- ../game/puzzles/
```

**Step 3: Commit**

```bash
git commit -m "feat: evaluator CLI with JSON output"
```

---

Plan complete and saved to `docs/plans/2026-03-12-level-evaluator-implementation.md`. Two execution options:

**1. Subagent-Driven (this session)** — I dispatch a fresh subagent per task, review between tasks, fast iteration

**2. Parallel Session (separate)** — Open new session with executing-plans, batch execution with checkpoints

Which approach?