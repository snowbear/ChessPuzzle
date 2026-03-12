using ChessPuzzle.Core.Models;
using ChessPuzzle.Evaluator.Validators;

namespace ChessPuzzle.Evaluator.Tests.Validators;

public class SchemaValidatorTests
{
    private readonly SchemaValidator _validator = new();

    [Fact]
    public void Validate_ValidPuzzle_ReturnsNoErrors()
    {
        var puzzle = TestHelper.MakeValidPuzzle();

        var errors = _validator.Validate(puzzle).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_MissingFen_ReturnsMissingFenError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.StartPosition.Fen = "";

        var errors = _validator.Validate(puzzle).ToList();

        Assert.Contains(errors, e => e.Code == "MISSING_FEN");
    }

    [Fact]
    public void Validate_ZeroHalfMoveCount_ReturnsInvalidHalfMoveCountError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.HalfMoveCount = 0;

        var errors = _validator.Validate(puzzle).ToList();

        Assert.Contains(errors, e => e.Code == "INVALID_HALF_MOVE_COUNT");
    }

    [Fact]
    public void Validate_NegativeMin_ReturnsInvalidPieceConstraintError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.PieceConstraints = new Dictionary<string, Dictionary<string, PieceConstraint>>
        {
            ["white"] = new()
            {
                ["knight"] = new PieceConstraint { Min = -1, Max = 2 }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.Contains(errors, e => e.Code == "INVALID_PIECE_CONSTRAINT");
    }

    [Fact]
    public void Validate_MinGreaterThanMax_ReturnsInvalidPieceConstraintError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.PieceConstraints = new Dictionary<string, Dictionary<string, PieceConstraint>>
        {
            ["white"] = new()
            {
                ["knight"] = new PieceConstraint { Min = 3, Max = 1 }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.Contains(errors, e => e.Code == "INVALID_PIECE_CONSTRAINT");
    }
}
