using Chess;
using ChessPuzzle.Core.Models;

namespace ChessPuzzle.Evaluator.Validators;

public class KingConstraintValidator : IValidator
{
    public IEnumerable<ValidationError> Validate(Puzzle puzzle)
    {
        ChessBoard board;
        try { board = ChessBoard.LoadFromFen(puzzle.StartPosition.Fen); }
        catch { yield break; } // FenValidator handles this

        foreach (var error in ValidateKingConstraint(board, puzzle, "white", PieceColor.White))
            yield return error;

        foreach (var error in ValidateKingConstraint(board, puzzle, "black", PieceColor.Black))
            yield return error;
    }

    private static IEnumerable<ValidationError> ValidateKingConstraint(
        ChessBoard board, Puzzle puzzle, string colorName, PieceColor color)
    {
        bool kingOnBoard = HasKing(board, color);
        var constraint = GetKingConstraint(puzzle, colorName);

        if (kingOnBoard)
        {
            // King is on the board: constraint must be absent or min=0,max=0
            if (constraint != null && (constraint.Min != 0 || constraint.Max != 0))
            {
                yield return new ValidationError("KING_CONSTRAINT_MISMATCH",
                    $"{Capitalize(colorName)} king is on the board but piece constraint allows placement (min={constraint.Min}, max={constraint.Max}); expected absent or min=0, max=0");
            }
        }
        else
        {
            // King is NOT on the board: constraint must be min=1,max=1
            if (constraint == null || constraint.Min != 1 || constraint.Max != 1)
            {
                yield return new ValidationError("KING_CONSTRAINT_MISMATCH",
                    $"{Capitalize(colorName)} king is missing from the board but piece constraint is not min=1, max=1");
            }
        }
    }

    private static readonly string[] Files = { "a", "b", "c", "d", "e", "f", "g", "h" };

    private static bool HasKing(ChessBoard board, PieceColor color)
    {
        foreach (var file in Files)
        {
            for (int rank = 1; rank <= 8; rank++)
            {
                var piece = board[$"{file}{rank}"];
                if (piece != null && piece.Color == color && piece.Type == PieceType.King)
                    return true;
            }
        }
        return false;
    }

    private static PieceConstraint? GetKingConstraint(Puzzle puzzle, string colorName)
    {
        if (puzzle.PieceConstraints == null)
            return null;

        if (!puzzle.PieceConstraints.TryGetValue(colorName, out var pieces))
            return null;

        if (!pieces.TryGetValue("king", out var constraint))
            return null;

        return constraint;
    }

    private static string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];
}
