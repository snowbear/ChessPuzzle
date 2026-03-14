using ChessPuzzle.Core.Models;
using ChessPuzzle.Evaluator.Validators;

namespace ChessPuzzle.Evaluator.Tests.Validators;

public class OpenSquareConsistencyValidatorTests
{
    private readonly OpenSquareConsistencyValidator _validator = new();

    [Fact]
    public void Validate_OccupiedSquareMarkedOpen_ReturnsNoErrors()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        // a1 has a white rook in the standard starting position
        puzzle.StartPosition.Squares = new Dictionary<string, string>
        {
            ["a1"] = "open"
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_EmptySquareMarkedOpen_ReturnsError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        // e4 is empty in the standard starting position
        puzzle.StartPosition.Squares = new Dictionary<string, string>
        {
            ["e4"] = "open"
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.Contains(errors, e => e.Code == "OPEN_SQUARE_EMPTY");
    }

    [Fact]
    public void Validate_NoSquaresMarkedOpen_ReturnsNoErrors()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        // No squares marked open at all
        puzzle.StartPosition.Squares = new Dictionary<string, string>();

        var errors = _validator.Validate(puzzle).ToList();

        Assert.Empty(errors);
    }
}
