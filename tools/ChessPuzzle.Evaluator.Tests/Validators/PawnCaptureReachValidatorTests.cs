using ChessPuzzle.Core.Models;
using ChessPuzzle.Evaluator.Validators;

namespace ChessPuzzle.Evaluator.Tests.Validators;

public class PawnCaptureReachValidatorTests
{
    private readonly PawnCaptureReachValidator _validator = new();

    [Fact]
    public void Validate_PawnCaptureInReach_NoError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        // White pawn on e2, capture target d3 — pawn on adjacent file, 1 rank ahead
        puzzle.StartPosition.Fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { HalfMove = 1 },
                Constraints = new HintConstraints
                {
                    Piece = "pawn",
                    IsCapture = true,
                    ToSquare = "d3"
                }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.DoesNotContain(errors, e => e.Code == "PAWN_CAPTURE_UNREACHABLE");
    }

    [Fact]
    public void Validate_PawnCaptureUnreachable_ReturnsError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        // Only a white pawn on h2, capture target a3 — too far away
        puzzle.StartPosition.Fen = "4k3/8/8/8/8/8/7P/4K3 w - - 0 1";
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { HalfMove = 1 },
                Constraints = new HintConstraints
                {
                    Piece = "pawn",
                    IsCapture = true,
                    ToSquare = "a3"
                }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.Contains(errors, e => e.Code == "PAWN_CAPTURE_UNREACHABLE");
    }

    [Fact]
    public void Validate_PawnCaptureWithoutToSquare_NoError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { HalfMove = 1 },
                Constraints = new HintConstraints
                {
                    Piece = "pawn",
                    IsCapture = true
                    // No ToSquare
                }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.DoesNotContain(errors, e => e.Code == "PAWN_CAPTURE_UNREACHABLE");
    }

    [Fact]
    public void Validate_NonPawnHint_NoError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { HalfMove = 1 },
                Constraints = new HintConstraints
                {
                    Piece = "knight",
                    IsCapture = true,
                    ToSquare = "a3"
                }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.DoesNotContain(errors, e => e.Code == "PAWN_CAPTURE_UNREACHABLE");
    }

    [Fact]
    public void Validate_BlackPawnCaptureInReach_NoError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        // Black pawn on d7, capture target e6 — adjacent file, 1 rank ahead (for black = decreasing)
        puzzle.StartPosition.Fen = "4k3/3p4/8/8/8/8/8/4K3 b - - 0 1";
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { HalfMove = 1 },
                Constraints = new HintConstraints
                {
                    Piece = "pawn",
                    IsCapture = true,
                    ToSquare = "e6"
                }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.DoesNotContain(errors, e => e.Code == "PAWN_CAPTURE_UNREACHABLE");
    }

    [Fact]
    public void Validate_PawnCaptureMultipleMoves_InReach_NoError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        // White pawn on e2, capture target d4 — needs 2 white moves (advance to e3, capture d4)
        // Half-move 3 = white's 2nd move
        puzzle.StartPosition.Fen = "4k3/8/8/8/8/8/4P3/4K3 w - - 0 1";
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { HalfMove = 3 },
                Constraints = new HintConstraints
                {
                    Piece = "pawn",
                    IsCapture = true,
                    ToSquare = "d4"
                }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.DoesNotContain(errors, e => e.Code == "PAWN_CAPTURE_UNREACHABLE");
    }

    [Fact]
    public void Validate_AnyScopeHint_SkipsCheck()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { IsAny = true },
                Constraints = new HintConstraints
                {
                    Piece = "pawn",
                    IsCapture = true,
                    ToSquare = "a3"
                }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        // IsAny scope returns -1 for available half moves, so validator skips
        Assert.DoesNotContain(errors, e => e.Code == "PAWN_CAPTURE_UNREACHABLE");
    }
}
