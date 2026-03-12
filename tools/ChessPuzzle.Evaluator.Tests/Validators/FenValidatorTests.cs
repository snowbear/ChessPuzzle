using ChessPuzzle.Core.Models;
using ChessPuzzle.Evaluator.Validators;

namespace ChessPuzzle.Evaluator.Tests.Validators;

public class FenValidatorTests
{
    private readonly FenValidator _validator = new();

    [Fact]
    public void Validate_ValidFen_ReturnsNoErrors()
    {
        var puzzle = TestHelper.MakeValidPuzzle();

        var errors = _validator.Validate(puzzle).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_GarbageStartingFen_ReturnsInvalidFenError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.StartPosition.Fen = "not-a-valid-fen";

        var errors = _validator.Validate(puzzle).ToList();

        Assert.Contains(errors, e => e.Code == "INVALID_FEN");
    }

    [Fact]
    public void Validate_InvalidRevealedFinalPosition_ReturnsInvalidFinalFenError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.RevealedFinalPosition = "also-garbage";

        var errors = _validator.Validate(puzzle).ToList();

        Assert.Contains(errors, e => e.Code == "INVALID_FINAL_FEN");
    }
}
