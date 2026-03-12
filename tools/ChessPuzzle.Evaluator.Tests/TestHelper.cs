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
