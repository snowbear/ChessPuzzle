using Chess;
using ChessPuzzle.Core.Models;

namespace ChessPuzzle.Evaluator.Validators;

public class CastlingHintValidator : IValidator
{
    private static readonly string[] Files = { "a", "b", "c", "d", "e", "f", "g", "h" };

    public IEnumerable<ValidationError> Validate(Puzzle puzzle)
    {
        var fen = puzzle.StartPosition.Fen;
        if (string.IsNullOrEmpty(fen))
            yield break;

        var fenParts = fen.Split(' ');
        if (fenParts.Length < 3)
            yield break;

        var activeColor = fenParts[1]; // "w" or "b"
        var castlingRights = fenParts[2]; // e.g. "KQkq", "-"

        ChessBoard board;
        try { board = ChessBoard.LoadFromFen(fen); }
        catch { yield break; }

        foreach (var hint in puzzle.Hints)
        {
            var castle = hint.Constraints.IsCastle;
            if (castle == null)
                continue;

            // IsCastle = false means "not a castle move", no validation needed
            if (castle.IsBool && !castle.BoolValue)
                continue;

            var colors = GetColorsForHint(hint, activeColor);
            string? side = castle.IsBool ? null : castle.StringValue; // null = either side

            bool anyColorCanCastle = false;
            foreach (var color in colors)
            {
                if (CanColorCastle(color, side, castlingRights, board, puzzle.StartPosition.Squares))
                {
                    anyColorCanCastle = true;
                    break;
                }
            }

            if (!anyColorCanCastle)
            {
                var sideDesc = side ?? "any";
                var colorDesc = colors.Count == 1 ? colors[0] : "any color";
                yield return new ValidationError("CASTLE_IMPOSSIBLE",
                    $"Castle hint ({sideDesc}) is impossible for {colorDesc}: " +
                    $"castling rights '{castlingRights}' do not support it or pieces are not on starting squares");
            }
        }
    }

    private static List<string> GetColorsForHint(Hint hint, string activeColor)
    {
        // If explicit color constraint, use that
        if (!string.IsNullOrEmpty(hint.Constraints.Color))
            return new List<string> { hint.Constraints.Color };

        // For "any" scope, check both colors
        if (hint.Scope.IsAny)
            return new List<string> { "white", "black" };

        // For half-move range, collect all colors that could move in that range
        if (hint.Scope.HalfMoveRange is { Length: 2 } range)
        {
            var colors = new HashSet<string>();
            for (int hm = range[0]; hm <= range[1]; hm++)
            {
                colors.Add(GetColorForHalfMove(hm, activeColor));
            }
            return colors.ToList();
        }

        // For specific half-move
        if (hint.Scope.HalfMove.HasValue)
        {
            return new List<string> { GetColorForHalfMove(hint.Scope.HalfMove.Value, activeColor) };
        }

        // Final scope - could be either color depending on halfMoveCount, treat as any
        return new List<string> { "white", "black" };
    }

    private static string GetColorForHalfMove(int halfMove, string activeColor)
    {
        // Half-move 1 is the active color's turn
        bool isOddHalfMove = halfMove % 2 == 1;
        if (activeColor == "w")
            return isOddHalfMove ? "white" : "black";
        else
            return isOddHalfMove ? "black" : "white";
    }

    private static bool CanColorCastle(string color, string? side, string castlingRights,
        ChessBoard board, Dictionary<string, string>? squares)
    {
        if (color == "white")
        {
            if (side == "kingside" || side == null)
            {
                if (HasRight(castlingRights, 'K') && PiecesOnStartSquares(board, squares, "e1", "h1"))
                    return true;
            }
            if (side == "queenside" || side == null)
            {
                if (HasRight(castlingRights, 'Q') && PiecesOnStartSquares(board, squares, "e1", "a1"))
                    return true;
            }
        }
        else // black
        {
            if (side == "kingside" || side == null)
            {
                if (HasRight(castlingRights, 'k') && PiecesOnStartSquares(board, squares, "e8", "h8"))
                    return true;
            }
            if (side == "queenside" || side == null)
            {
                if (HasRight(castlingRights, 'q') && PiecesOnStartSquares(board, squares, "e8", "a8"))
                    return true;
            }
        }
        return false;
    }

    private static bool HasRight(string castlingRights, char right)
    {
        return castlingRights.Contains(right);
    }

    private static bool PiecesOnStartSquares(ChessBoard board, Dictionary<string, string>? squares,
        string kingSquare, string rookSquare)
    {
        return IsSquareOkForCastling(board, squares, kingSquare, PieceType.King) &&
               IsSquareOkForCastling(board, squares, rookSquare, PieceType.Rook);
    }

    private static bool IsSquareOkForCastling(ChessBoard board, Dictionary<string, string>? squares,
        string square, PieceType expectedType)
    {
        // If the square is marked as "open", player could place the piece there
        if (squares != null && squares.TryGetValue(square, out var squareValue) &&
            squareValue == "open")
            return true;

        // Check if the expected piece is actually on the square
        var piece = board[square];
        return piece != null && piece.Type == expectedType;
    }
}
