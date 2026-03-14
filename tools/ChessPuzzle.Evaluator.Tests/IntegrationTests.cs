using ChessPuzzle.Core;

namespace ChessPuzzle.Evaluator.Tests;

public class IntegrationTests
{
    private readonly PuzzleEvaluator _evaluator = new();

    [Theory]
    [InlineData("../../../../../game/puzzles/puzzle-001.json")]
    [InlineData("../../../../../game/puzzles/puzzle-002.json")]
    public void ExistingPuzzles_AreValid(string path)
    {
        var puzzle = PuzzleLoader.FromFile(path);
        var result = _evaluator.ValidateOnly(puzzle);
        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Message}")));
    }

    [Theory]
    [Trait("Category", "LongRunning")]
    [InlineData("../../../../../game/puzzles/puzzle-001.json")]
    [InlineData("../../../../../game/puzzles/puzzle-002.json")]
    public void ExistingPuzzles_HaveAtLeastOneSolution(string path)
    {
        var puzzle = PuzzleLoader.FromFile(path);
        var result = _evaluator.Evaluate(puzzle);
        Assert.True(result.IsValid);
        Assert.True(result.SolutionCount > 0, $"Expected at least one solution but found {result.SolutionCount}. Remarks: {string.Join("; ", result.Remarks)}");
    }
}
