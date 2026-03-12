using ChessPuzzle.Core.Models;

namespace ChessPuzzle.Evaluator.Tests;

public class PuzzleEvaluatorTests
{
    private readonly PuzzleEvaluator _evaluator = new();

    [Fact]
    public void ValidateOnly_ValidPuzzle_ReturnsIsValidWithNoErrors()
    {
        var puzzle = TestHelper.MakeValidPuzzle();

        var result = _evaluator.ValidateOnly(puzzle);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Null(result.SolutionCount);
        Assert.Null(result.SearchSpaceSize);
        Assert.Null(result.Complexity);
    }

    [Fact]
    public void ValidateOnly_InvalidPuzzle_EmptyFen_ReturnsErrors()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.StartPosition.Fen = "";

        var result = _evaluator.ValidateOnly(puzzle);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Evaluate_InvalidPuzzle_ReturnsErrorsAndSkipsExplorer()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.StartPosition.Fen = "";

        var result = _evaluator.Evaluate(puzzle);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Null(result.SolutionCount);
        Assert.Null(result.SearchSpaceSize);
        Assert.Null(result.Complexity);
    }

    [Fact]
    public void Evaluate_ValidPuzzle_RunsExplorerAndReturnsMetrics()
    {
        var puzzle = TestHelper.MakeValidPuzzle();

        var result = _evaluator.Evaluate(puzzle);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        // Explorer now runs a real search; the test puzzle has HalfMoveCount=2
        // from the starting position, so SearchSpaceSize > 0.
        // The revealed final position shows only e4 played (1 half-move state),
        // but the search plays 2 half-moves, so no sequence matches.
        Assert.Equal(0, result.SolutionCount);
        Assert.True(result.SearchSpaceSize > 0, "SearchSpaceSize should be > 0 with real explorer");
        Assert.Equal(0.0, result.Complexity);
        Assert.Contains("No valid solutions found", result.Remarks);
    }
}
