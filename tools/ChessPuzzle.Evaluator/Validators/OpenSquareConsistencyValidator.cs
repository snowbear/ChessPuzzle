using Chess;
using ChessPuzzle.Core.Models;

namespace ChessPuzzle.Evaluator.Validators;

public class OpenSquareConsistencyValidator : IValidator
{
    public IEnumerable<ValidationError> Validate(Puzzle puzzle)
    {
        ChessBoard board;
        try { board = ChessBoard.LoadFromFen(puzzle.StartPosition.Fen); }
        catch { yield break; } // FenValidator handles this

        if (puzzle.StartPosition.Squares == null)
            yield break;

        foreach (var (square, state) in puzzle.StartPosition.Squares)
        {
            if (state != "open") continue;
            if (board[square] == null)
                yield return new ValidationError("OPEN_SQUARE_EMPTY",
                    $"Square {square} is marked open but is empty in the starting FEN");
        }
    }
}
