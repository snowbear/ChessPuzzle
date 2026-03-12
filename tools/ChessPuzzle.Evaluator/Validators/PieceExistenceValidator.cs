using Chess;
using ChessPuzzle.Core.Models;

namespace ChessPuzzle.Evaluator.Validators;

public class PieceExistenceValidator : IValidator
{
    private static readonly string[] Files = { "a", "b", "c", "d", "e", "f", "g", "h" };

    private static readonly Dictionary<string, PieceType> PieceTypeMap = new()
    {
        ["king"] = PieceType.King,
        ["queen"] = PieceType.Queen,
        ["rook"] = PieceType.Rook,
        ["bishop"] = PieceType.Bishop,
        ["knight"] = PieceType.Knight,
        ["pawn"] = PieceType.Pawn,
    };

    public IEnumerable<ValidationError> Validate(Puzzle puzzle)
    {
        var fen = puzzle.StartPosition.Fen;
        if (string.IsNullOrEmpty(fen))
            yield break;

        var fenParts = fen.Split(' ');
        if (fenParts.Length < 2)
            yield break;

        var activeColor = fenParts[1];

        ChessBoard board;
        try { board = ChessBoard.LoadFromFen(fen); }
        catch { yield break; }

        foreach (var hint in puzzle.Hints)
        {
            var pieceType = hint.Constraints.Piece;
            if (string.IsNullOrEmpty(pieceType))
                continue;

            if (!PieceTypeMap.ContainsKey(pieceType))
                continue;

            var colors = GetColorsForHint(hint, activeColor);

            bool anyColorCanProvide = false;
            foreach (var color in colors)
            {
                if (CanColorProvidePiece(board, puzzle, color, pieceType))
                {
                    anyColorCanProvide = true;
                    break;
                }
            }

            if (!anyColorCanProvide)
            {
                var colorDesc = colors.Count == 1 ? colors[0] : "either color";
                yield return new ValidationError("PIECE_NOT_AVAILABLE",
                    $"Hint references {pieceType} but {colorDesc} cannot provide one: " +
                    $"not on board, not in piece constraints, and no promotion path available");
            }
        }
    }

    private static bool CanColorProvidePiece(ChessBoard board, Puzzle puzzle, string colorName, string pieceType)
    {
        var pieceColor = colorName == "white" ? PieceColor.White : PieceColor.Black;
        var chessPieceType = PieceTypeMap[pieceType];

        // 1. Piece of that type+color exists on the board
        if (HasPieceOnBoard(board, pieceColor, chessPieceType))
            return true;

        // 2. Piece is in piece constraints for that color with min > 0
        var constraint = GetPieceConstraint(puzzle, colorName, pieceType);
        if (constraint != null && constraint.Min > 0)
            return true;

        // 3. Piece is in piece constraints with max > 0
        if (constraint != null && constraint.Max > 0)
            return true;

        // 4. A pawn of that color exists on board or in constraints (max > 0) — promotion possible
        // (Only relevant for non-pawn pieces; a pawn can't promote to a pawn)
        if (pieceType != "pawn" && pieceType != "king")
        {
            if (HasPieceOnBoard(board, pieceColor, PieceType.Pawn))
                return true;

            var pawnConstraint = GetPieceConstraint(puzzle, colorName, "pawn");
            if (pawnConstraint != null && pawnConstraint.Max > 0)
                return true;
        }

        // 5. None of the above
        return false;
    }

    private static bool HasPieceOnBoard(ChessBoard board, PieceColor color, PieceType type)
    {
        foreach (var file in Files)
        {
            for (int rank = 1; rank <= 8; rank++)
            {
                var piece = board[$"{file}{rank}"];
                if (piece != null && piece.Color == color && piece.Type == type)
                    return true;
            }
        }
        return false;
    }

    private static PieceConstraint? GetPieceConstraint(Puzzle puzzle, string colorName, string pieceType)
    {
        if (puzzle.PieceConstraints == null)
            return null;

        if (!puzzle.PieceConstraints.TryGetValue(colorName, out var pieces))
            return null;

        if (!pieces.TryGetValue(pieceType, out var constraint))
            return null;

        return constraint;
    }

    private static List<string> GetColorsForHint(Hint hint, string activeColor)
    {
        if (!string.IsNullOrEmpty(hint.Constraints.Color))
            return new List<string> { hint.Constraints.Color };

        if (hint.Scope.IsAny)
            return new List<string> { "white", "black" };

        if (hint.Scope.HalfMoveRange is { Length: 2 } range)
        {
            var colors = new HashSet<string>();
            for (int hm = range[0]; hm <= range[1]; hm++)
            {
                colors.Add(GetColorForHalfMove(hm, activeColor));
            }
            return colors.ToList();
        }

        if (hint.Scope.HalfMove.HasValue)
            return new List<string> { GetColorForHalfMove(hint.Scope.HalfMove.Value, activeColor) };

        // Final scope — could be either color
        return new List<string> { "white", "black" };
    }

    private static string GetColorForHalfMove(int halfMove, string activeColor)
    {
        bool isOddHalfMove = halfMove % 2 == 1;
        if (activeColor == "w")
            return isOddHalfMove ? "white" : "black";
        else
            return isOddHalfMove ? "black" : "white";
    }
}
