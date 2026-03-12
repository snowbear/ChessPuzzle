using ChessPuzzle.Core.Models;
using ChessPuzzle.Evaluator.Validators;

namespace ChessPuzzle.Evaluator.Tests.Validators;

public class EnPassantFirstMoveValidatorTests
{
    private readonly EnPassantFirstMoveValidator _validator = new();

    [Fact]
    public void Validate_EnPassantOnMove1_NoEnPassantInFen_ReturnsError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        // Standard starting FEN has "-" for en passant
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { HalfMove = 1 },
                Constraints = new HintConstraints { IsEnPassant = true }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.Contains(errors, e => e.Code == "EN_PASSANT_IMPOSSIBLE");
    }

    [Fact]
    public void Validate_EnPassantOnMove1_EnPassantTargetInFen_NoError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        // FEN with en passant target on e3
        puzzle.StartPosition.Fen = "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1";
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { HalfMove = 1 },
                Constraints = new HintConstraints { IsEnPassant = true }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.DoesNotContain(errors, e => e.Code == "EN_PASSANT_IMPOSSIBLE");
    }

    [Fact]
    public void Validate_EnPassantOnMove3_NoEnPassantInFen_NoError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        // Can't validate statically for half-move != 1
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { HalfMove = 3 },
                Constraints = new HintConstraints { IsEnPassant = true }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.DoesNotContain(errors, e => e.Code == "EN_PASSANT_IMPOSSIBLE");
    }
}
