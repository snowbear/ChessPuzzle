# Level Evaluator — Design Document

## Overview

A .NET tool that validates and scores chess puzzle definitions. It provides:
1. **Quick hard-stop validation** — catches design failures without search (e.g. castle hint when castling is impossible)
2. **Slower numeric metrics** — explores the solution space to compute complexity and related scores

The evaluator is designed as a library so the future level generator can call it directly as a fitness function, with a thin CLI wrapper for manual use.

## Repository Layout

```
C:\work\ChessPuzzle/
├── game/                            — existing web game (moved from root)
│   ├── index.html
│   ├── css/
│   ├── js/
│   ├── puzzles/
│   ├── node_modules/
│   ├── package.json
│   ├── start.bat
│   └── start.sh
│
├── tools/
│   ├── ChessPuzzleTools.sln
│   ├── ChessPuzzle.Core/           — shared models, puzzle loading
│   ├── ChessPuzzle.Evaluator/      — validation + metrics library
│   ├── ChessPuzzle.Evaluator.Cli/  — console app wrapper
│   └── ChessPuzzle.Evaluator.Tests/
│
├── CLAUDE.md
└── docs/
```

Puzzles stay inside `game/` since the web app references them. The CLI accepts a path to any puzzle file.

## Core Models

```csharp
// EvaluationResult.cs
public class EvaluationResult
{
    public List<ValidationError> Errors { get; set; }
    public bool IsValid => Errors.Count == 0;
    public int? SolutionCount { get; set; }       // null if validation failed
    public long? SearchSpaceSize { get; set; }
    public double? Complexity { get; set; }        // solutions / search_space
    public List<string> Remarks { get; set; }      // free-form observations
}

// ValidationError.cs
public class ValidationError
{
    public string Code { get; set; }       // e.g. "INVALID_FEN", "CASTLE_IMPOSSIBLE"
    public string Message { get; set; }    // human-readable
}
```

- **Metrics are nullable** — if hard-stop validation fails, the expensive search is skipped.
- **Everything in `Errors` is a hard stop.** No severity levels.
- **`Remarks`** — flat list of contextual observations from any part of the pipeline (e.g. "no valid solutions found", "single unique solution"). Not tied to a specific metric.
- **`Puzzle.cs`** mirrors the JSON format from the web game so both tools read the same puzzle files.

## Validator Pipeline

```csharp
public interface IValidator
{
    IEnumerable<ValidationError> Validate(Puzzle puzzle);
}
```

Each validator is a small focused class. The orchestrator runs them all and collects errors. Adding a new validator = add a class implementing `IValidator`, register it. No changes to existing code.

### Initial Validators

1. **SchemaValidator** — required fields present, correct types, min ≤ max on piece constraints, non-negative values
2. **FenValidator** — FEN string parses successfully
3. **OpenSquareConsistencyValidator** — square occupied in FEN but marked `open` → error (reverse is fine — free square not marked open is valid)
4. **KingConstraintValidator** — king on board → piece constraint must be min=max=0; king missing → must be min=max=1
5. **CastlingHintValidator** — castle hint present but castling rights absent or king/rook not in starting squares
6. **PieceExistenceValidator** — hint references a piece type that doesn't exist on board, isn't in piece constraints, and no pawns available for promotion
7. **PawnCaptureReachValidator** — pawn capture hint but no pawn in a sector that could reach the target square in time
8. **EnPassantFirstMoveValidator** — en passant hint on half-move 1 when FEN doesn't indicate an en passant target

**TODO:** Promotion hint validators (once the promotion hint type is added to the puzzle format). Promotion-specific checks: can't promote to pawn, must have a pawn, last rank must be reachable in time.

## Solution Space Explorer

```
SolutionSpaceExplorer.Explore(puzzle) → ExplorationResult
```

### Three-phase pipeline

1. **Enumerate placements** — all valid combinations of pieces on open squares, respecting piece constraints (min/max counts)
2. **For each placement, enumerate move sequences** — from the resulting position, play all legal move sequences of length `halfMoveCount`
3. **For each complete sequence, validate** — check revealed final position match + all hints satisfied

```csharp
public class ExplorationResult
{
    public int SolutionCount { get; set; }
    public long SearchSpaceSize { get; set; }        // total sequences checked
    public List<Solution> Solutions { get; set; }     // actual valid solutions
}

public class Solution
{
    public Dictionary<string, Piece> Placement { get; set; }  // square → piece placed
    public List<string> Moves { get; set; }                    // algebraic notation
}
```

### Pruning

v1 is brute force — correctness first. However, each phase is a separate method/stage so pruning can be injected between them later. This will be important soon: with 4 pieces and 20 open squares, placements alone are 20×19×18×17 = 116,280 combinations.

## Evaluator Orchestrator

1. Run all validators → if any errors, return early with null metrics
2. Run `SolutionSpaceExplorer.Explore()`
3. Compute `Complexity = SolutionCount / SearchSpaceSize`
4. Add remarks (e.g. "no valid solutions found", "single unique solution")
5. Return `EvaluationResult`

## CLI Interface

```
chesspuzzle-eval puzzle-001.json
chesspuzzle-eval puzzle-001.json puzzle-002.json
chesspuzzle-eval puzzles/
```

Output: JSON to stdout.

```json
{
  "file": "puzzle-001.json",
  "valid": true,
  "errors": [],
  "solutionCount": 1,
  "searchSpaceSize": 24,
  "complexity": 0.0417,
  "remarks": ["single unique solution"]
}
```

On validation failure:
```json
{
  "file": "puzzle-002.json",
  "valid": false,
  "errors": [
    { "code": "CASTLE_IMPOSSIBLE", "message": "Hint 4 requires kingside castling but black has no castling rights" }
  ],
  "solutionCount": null,
  "searchSpaceSize": null,
  "complexity": null,
  "remarks": []
}
```

Multiple files → JSON array. Exit code 0 if all valid, 1 if any invalid.

## Chess Library

Uses a NuGet chess library (TBD — to be researched at implementation time) for FEN parsing, move validation, legal move generation, and check/checkmate detection. No custom chess logic.

## Future Considerations

- **Combined Quality score** — weighted average of complexity + future metrics, added when there are multiple metrics to balance
- **Additional metrics** — hint redundancy, placement difficulty, move-order difficulty. These can often be derived externally by comparing evaluator runs with modified inputs, so they don't necessarily need to live inside the evaluator.
- **Pruning optimizations** — skip placements/branches that provably violate hints early
- **Promotion hints** — new hint type with specific validators
