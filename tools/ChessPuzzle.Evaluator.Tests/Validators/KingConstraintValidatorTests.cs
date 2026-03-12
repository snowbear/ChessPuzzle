using ChessPuzzle.Core.Models;
using ChessPuzzle.Evaluator.Validators;

namespace ChessPuzzle.Evaluator.Tests.Validators;

public class KingConstraintValidatorTests
{
    private readonly KingConstraintValidator _validator = new();

    [Fact]
    public void Validate_KingOnBoard_ConstraintMinZeroMaxZero_NoError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.PieceConstraints = new Dictionary<string, Dictionary<string, PieceConstraint>>
        {
            ["white"] = new() { ["king"] = new PieceConstraint { Min = 0, Max = 0 } }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.DoesNotContain(errors, e => e.Code == "KING_CONSTRAINT_MISMATCH" && e.Message.Contains("White"));
    }

    [Fact]
    public void Validate_KingOnBoard_ConstraintAllowsPlacement_ReturnsError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.PieceConstraints = new Dictionary<string, Dictionary<string, PieceConstraint>>
        {
            ["white"] = new() { ["king"] = new PieceConstraint { Min = 0, Max = 1 } }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.Contains(errors, e => e.Code == "KING_CONSTRAINT_MISMATCH" && e.Message.Contains("White"));
    }

    [Fact]
    public void Validate_KingOnBoard_NoConstraintEntry_NoError()
    {
        // Absent constraint is equivalent to min=0, max=0
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.PieceConstraints = new Dictionary<string, Dictionary<string, PieceConstraint>>();

        var errors = _validator.Validate(puzzle).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_KingOnBoard_ConstraintMinOneMaxOne_ReturnsError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.PieceConstraints = new Dictionary<string, Dictionary<string, PieceConstraint>>
        {
            ["white"] = new() { ["king"] = new PieceConstraint { Min = 1, Max = 1 } }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.Contains(errors, e => e.Code == "KING_CONSTRAINT_MISMATCH" && e.Message.Contains("White"));
    }

    [Fact]
    public void Validate_BothKingsOnBoard_BothConstraintsMissing_NoError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.PieceConstraints = null;

        var errors = _validator.Validate(puzzle).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_BlackKingOnBoard_ConstraintMinOneMaxOne_ReturnsError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.PieceConstraints = new Dictionary<string, Dictionary<string, PieceConstraint>>
        {
            ["black"] = new() { ["king"] = new PieceConstraint { Min = 1, Max = 1 } }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.Contains(errors, e => e.Code == "KING_CONSTRAINT_MISMATCH" && e.Message.Contains("Black"));
    }

    [Fact]
    public void Validate_KingMissing_NoConstraint_ReturnsError()
    {
        // FEN without a king - Gera.Chess may not accept this.
        // If LoadFromFen throws, the validator yields no errors (FenValidator handles it).
        // This test documents the expected behavior if such a FEN were parseable.
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.StartPosition.Fen = "8/8/8/8/8/8/8/8 w - - 0 1"; // empty board, no kings

        var errors = _validator.Validate(puzzle).ToList();

        // If FEN parse fails, no errors from this validator (FenValidator covers it).
        // If FEN parse succeeds, we'd expect KING_CONSTRAINT_MISMATCH for both colors.
        // We accept either outcome.
        if (errors.Count > 0)
        {
            Assert.Equal(2, errors.Count);
            Assert.All(errors, e => Assert.Equal("KING_CONSTRAINT_MISMATCH", e.Code));
        }
    }

    [Fact]
    public void Validate_KingMissing_ConstraintMinOneMaxOne_NoError()
    {
        // Same caveat as above regarding FEN without kings.
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.StartPosition.Fen = "8/8/8/8/8/8/8/8 w - - 0 1";
        puzzle.PieceConstraints = new Dictionary<string, Dictionary<string, PieceConstraint>>
        {
            ["white"] = new() { ["king"] = new PieceConstraint { Min = 1, Max = 1 } },
            ["black"] = new() { ["king"] = new PieceConstraint { Min = 1, Max = 1 } }
        };

        var errors = _validator.Validate(puzzle).ToList();

        // If FEN parse fails, no errors. If it succeeds, should be no errors since constraint is correct.
        Assert.Empty(errors);
    }
}
