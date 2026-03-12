using Chess;
using ChessPuzzle.Core.Models;

namespace ChessPuzzle.Evaluator.Validators;

public class PawnCaptureReachValidator : IValidator
{
    private static readonly string[] Files = { "a", "b", "c", "d", "e", "f", "g", "h" };

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
            if (hint.Constraints.Piece != "pawn")
                continue;

            if (hint.Constraints.IsCapture != true)
                continue;

            if (string.IsNullOrEmpty(hint.Constraints.ToSquare))
                continue;

            var toSquare = hint.Constraints.ToSquare;
            if (toSquare.Length != 2)
                continue;

            int targetFile = FileToIndex(toSquare[0]);
            int targetRank = toSquare[1] - '0';
            if (targetFile < 0 || targetRank < 1 || targetRank > 8)
                continue;

            var colors = GetColorsForHint(hint, activeColor);
            int availableHalfMoves = GetAvailableHalfMoves(hint);
            if (availableHalfMoves <= 0)
                continue;

            bool anyPawnCanReach = false;
            foreach (var color in colors)
            {
                var pieceColor = color == "white" ? PieceColor.White : PieceColor.Black;
                int movesForColor = GetMovesForColor(availableHalfMoves, color, activeColor);

                if (movesForColor <= 0)
                    continue;

                if (CanAnyPawnReachCapture(board, puzzle, pieceColor, targetFile, targetRank, movesForColor))
                {
                    anyPawnCanReach = true;
                    break;
                }
            }

            if (!anyPawnCanReach)
            {
                var colorDesc = colors.Count == 1 ? colors[0] : "either color";
                yield return new ValidationError("PAWN_CAPTURE_UNREACHABLE",
                    $"Pawn capture hint targeting {toSquare} is unreachable for {colorDesc}: " +
                    $"no pawn can geometrically reach the capture square");
            }
        }
    }

    private static bool CanAnyPawnReachCapture(ChessBoard board, Puzzle puzzle,
        PieceColor pieceColor, int targetFile, int targetRank, int movesForColor)
    {
        bool isWhite = pieceColor == PieceColor.White;

        // Check all pawns of this color on the board
        foreach (var file in Files)
        {
            for (int rank = 1; rank <= 8; rank++)
            {
                var piece = board[$"{file}{rank}"];
                if (piece != null && piece.Color == pieceColor && piece.Type == PieceType.Pawn)
                {
                    if (CanPawnReachCapture(FileToIndex(file[0]), rank, isWhite, targetFile, targetRank, movesForColor))
                        return true;
                }
            }
        }

        // Check open squares - a pawn could be placed there
        if (puzzle.StartPosition.Squares != null)
        {
            foreach (var (square, value) in puzzle.StartPosition.Squares)
            {
                if (value == "open" && square.Length == 2)
                {
                    int file = FileToIndex(square[0]);
                    int rank = square[1] - '0';
                    if (file >= 0 && rank >= 1 && rank <= 8)
                    {
                        // A pawn could be placed on this open square
                        if (CanPawnReachCapture(file, rank, isWhite, targetFile, targetRank, movesForColor))
                            return true;
                    }
                }
            }
        }

        // Check piece constraints for pawns of this color
        string colorName = isWhite ? "white" : "black";
        if (puzzle.PieceConstraints != null &&
            puzzle.PieceConstraints.TryGetValue(colorName, out var pieces) &&
            pieces.TryGetValue("pawn", out var constraint) &&
            constraint.Max > 0)
        {
            // There could be a pawn placed on an open square - we already checked open squares above
            // If there are piece constraints but no open squares, we can't say much
            // Be conservative: if there's a pawn constraint, assume it could potentially reach
            return true;
        }

        return false;
    }

    /// <summary>
    /// Check if a pawn at (pawnFile, pawnRank) can reach (targetFile, targetRank) as a capture
    /// within the given number of moves for its color.
    ///
    /// For a capture, the pawn must be on an adjacent file (targetFile +/- 1) and one rank behind
    /// the target. A pawn can only change file by capturing, which is complex, so we simplify:
    /// the pawn must already be on an adjacent file to the target.
    ///
    /// White pawn: needs to reach rank (targetRank - 1) on file (targetFile +/- 1), then capture.
    /// The advance from pawnRank to (targetRank - 1) takes (targetRank - 1 - pawnRank) moves.
    /// Plus 1 move for the capture itself. Total moves needed = targetRank - pawnRank.
    ///
    /// Black pawn: needs to reach rank (targetRank + 1) on file (targetFile +/- 1), then capture.
    /// The advance takes (pawnRank - targetRank - 1) moves. Plus 1 for capture = pawnRank - targetRank.
    /// </summary>
    private static bool CanPawnReachCapture(int pawnFile, int pawnRank, bool isWhite,
        int targetFile, int targetRank, int movesForColor)
    {
        // Pawn must be on adjacent file to capture diagonally
        if (Math.Abs(pawnFile - targetFile) != 1)
            return false;

        if (isWhite)
        {
            // White pawns move forward = increasing rank
            // Must be behind the target rank
            if (pawnRank >= targetRank)
                return false;

            int movesNeeded = targetRank - pawnRank; // includes the capture move
            return movesNeeded <= movesForColor;
        }
        else
        {
            // Black pawns move forward = decreasing rank
            if (pawnRank <= targetRank)
                return false;

            int movesNeeded = pawnRank - targetRank;
            return movesNeeded <= movesForColor;
        }
    }

    private static int GetAvailableHalfMoves(Hint hint)
    {
        if (hint.Scope.HalfMove.HasValue)
            return hint.Scope.HalfMove.Value;

        if (hint.Scope.HalfMoveRange is { Length: 2 } range)
            return range[1]; // max half-move in range

        // For IsAny or IsFinal, we can't determine statically, skip
        return -1;
    }

    private static int GetMovesForColor(int totalHalfMoves, string color, string activeColor)
    {
        // Count how many half-moves belong to this color within the first totalHalfMoves
        int count = 0;
        for (int hm = 1; hm <= totalHalfMoves; hm++)
        {
            if (GetColorForHalfMove(hm, activeColor) == color)
                count++;
        }
        return count;
    }

    private static int FileToIndex(char file)
    {
        int idx = file - 'a';
        return idx >= 0 && idx < 8 ? idx : -1;
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
