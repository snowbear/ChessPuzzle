using Chess;
using ChessPuzzle.Core.Models;

namespace ChessPuzzle.Evaluator;

public static class SolutionSpaceExplorer
{
    public static ExplorationResult Explore(Puzzle puzzle)
    {
        var result = new ExplorationResult();

        try
        {
            // Pre-parse the revealed final position into a sparse map of occupied squares
            Dictionary<int, char>? revealedSquares = null;
            if (!string.IsNullOrEmpty(puzzle.RevealedFinalPosition))
                revealedSquares = ParseRevealedSquares(puzzle.RevealedFinalPosition);

            var placements = EnumeratePlacements(puzzle);

            if (placements.Count > 0)
            {
                foreach (var placement in placements)
                {
                    string fen = ApplyPlacementToFen(puzzle.StartPosition.Fen, placement);

                    if (!ChessBoard.TryLoadFromFen(fen, out var board))
                        continue;

                    SearchMoveTree(board, puzzle, revealedSquares, placement, puzzle.HalfMoveCount,
                        new List<Move>(), result);
                }
            }
            else
            {
                // No placements needed — search from starting position directly
                if (!ChessBoard.TryLoadFromFen(puzzle.StartPosition.Fen, out var board))
                    return result;

                SearchMoveTree(board, puzzle, revealedSquares, new Dictionary<string, string>(),
                    puzzle.HalfMoveCount, new List<Move>(), result);
            }

            result.SolutionCount = result.Solutions.Count;
        }
        catch
        {
            // If anything goes wrong (bad FEN, etc.), return empty result
        }

        return result;
    }

    /// <summary>
    /// DFS through the move tree. At leaf nodes, validates the position inline
    /// using the board state that's already built up — no replay needed.
    /// </summary>
    private static void SearchMoveTree(
        ChessBoard board,
        Puzzle puzzle,
        Dictionary<int, char>? revealedSquares,
        Dictionary<string, string> placement,
        int remainingDepth,
        List<Move> movesSoFar,
        ExplorationResult result)
    {
        if (remainingDepth == 0)
        {
            result.SearchSpaceSize++;

            // Validate final position — only check squares that are revealed (have pieces)
            if (revealedSquares != null)
            {
                var actualFenBoard = board.ToFen().Split(' ')[0];
                var actualExpanded = ExpandFenBoard(actualFenBoard);
                foreach (var (index, expectedChar) in revealedSquares)
                {
                    if (actualExpanded[index] != expectedChar)
                        return;
                }
            }

            // Validate all hints against the moves we've accumulated
            foreach (var hint in puzzle.Hints)
            {
                if (!CheckHint(hint, movesSoFar, board))
                    return;
            }

            // Valid solution found
            result.Solutions.Add(new Solution
            {
                Placement = new Dictionary<string, string>(placement),
                Moves = movesSoFar.Select(m => m.San ?? m.ToString()).ToList()
            });
            return;
        }

        if (board.IsEndGame)
            return;

        var moves = board.Moves(generateSan: true);
        foreach (var move in moves)
        {
            board.Move(move);
            movesSoFar.Add(move);
            SearchMoveTree(board, puzzle, revealedSquares, placement, remainingDepth - 1,
                movesSoFar, result);
            movesSoFar.RemoveAt(movesSoFar.Count - 1);
            board.Cancel();
        }
    }

    // --- Phase 1: Enumerate Placements ---

    internal static List<Dictionary<string, string>> EnumeratePlacements(Puzzle puzzle)
    {
        var openSquares = GetOpenSquares(puzzle);
        if (openSquares.Count == 0)
            return new List<Dictionary<string, string>>();

        var pieceLists = GetPieceCombinations(puzzle);
        if (pieceLists.Count == 0)
            return new List<Dictionary<string, string>>();

        var allPlacements = new List<Dictionary<string, string>>();

        foreach (var pieces in pieceLists)
        {
            if (pieces.Count > openSquares.Count)
                continue; // Can't place more pieces than open squares

            // Choose which squares get pieces, then permute pieces onto those squares
            var squareCombinations = Combinations(openSquares, pieces.Count);
            foreach (var squares in squareCombinations)
            {
                var permutations = Permutations(pieces);
                foreach (var perm in permutations)
                {
                    var placement = new Dictionary<string, string>();
                    for (int i = 0; i < squares.Count; i++)
                    {
                        placement[squares[i]] = perm[i];
                    }
                    allPlacements.Add(placement);
                }
            }
        }

        return allPlacements;
    }

    private static List<string> GetOpenSquares(Puzzle puzzle)
    {
        if (puzzle.StartPosition.Squares == null)
            return new List<string>();

        return puzzle.StartPosition.Squares
            .Where(kv => kv.Value == "open")
            .Select(kv => kv.Key)
            .OrderBy(s => s)
            .ToList();
    }

    /// <summary>
    /// Generates all possible lists of individual pieces based on PieceConstraints.
    /// Each constraint specifies a min/max count for a color+pieceType combo.
    /// We enumerate all count combinations and expand to piece lists.
    /// </summary>
    private static List<List<string>> GetPieceCombinations(Puzzle puzzle)
    {
        if (puzzle.PieceConstraints == null || puzzle.PieceConstraints.Count == 0)
            return new List<List<string>>();

        // Build list of (label, min, max) for each constrained piece type
        var constraints = new List<(string label, int min, int max)>();
        foreach (var (color, pieceTypes) in puzzle.PieceConstraints)
        {
            foreach (var (pieceType, constraint) in pieceTypes)
            {
                constraints.Add(($"{color} {pieceType}", constraint.Min, constraint.Max));
            }
        }

        if (constraints.Count == 0)
            return new List<List<string>>();

        // Enumerate all count combinations
        var countCombinations = new List<List<(string label, int count)>>();
        EnumerateCountCombinations(constraints, 0, new List<(string label, int count)>(), countCombinations);

        // Expand each count combination into a piece list
        var result = new List<List<string>>();
        foreach (var combo in countCombinations)
        {
            var pieces = new List<string>();
            foreach (var (label, count) in combo)
            {
                for (int i = 0; i < count; i++)
                    pieces.Add(label);
            }
            if (pieces.Count > 0)
                result.Add(pieces);
        }

        return result;
    }

    private static void EnumerateCountCombinations(
        List<(string label, int min, int max)> constraints,
        int index,
        List<(string label, int count)> current,
        List<List<(string label, int count)>> results)
    {
        if (index == constraints.Count)
        {
            results.Add(new List<(string label, int count)>(current));
            return;
        }

        var (label, min, max) = constraints[index];
        for (int count = min; count <= max; count++)
        {
            current.Add((label, count));
            EnumerateCountCombinations(constraints, index + 1, current, results);
            current.RemoveAt(current.Count - 1);
        }
    }

    /// <summary>
    /// Returns all ways to choose k items from the list (order doesn't matter).
    /// </summary>
    internal static List<List<T>> Combinations<T>(List<T> items, int k)
    {
        var result = new List<List<T>>();
        CombinationsHelper(items, k, 0, new List<T>(), result);
        return result;
    }

    private static void CombinationsHelper<T>(List<T> items, int k, int start, List<T> current, List<List<T>> result)
    {
        if (current.Count == k)
        {
            result.Add(new List<T>(current));
            return;
        }
        for (int i = start; i < items.Count; i++)
        {
            current.Add(items[i]);
            CombinationsHelper(items, k, i + 1, current, result);
            current.RemoveAt(current.Count - 1);
        }
    }

    /// <summary>
    /// Returns all distinct permutations of the list (handles duplicates).
    /// </summary>
    internal static List<List<string>> Permutations(List<string> items)
    {
        var result = new List<List<string>>();
        var sorted = items.OrderBy(x => x).ToList();
        PermutationsHelper(sorted, new bool[sorted.Count], new List<string>(), result);
        return result;
    }

    private static void PermutationsHelper(List<string> items, bool[] used, List<string> current, List<List<string>> result)
    {
        if (current.Count == items.Count)
        {
            result.Add(new List<string>(current));
            return;
        }
        for (int i = 0; i < items.Count; i++)
        {
            if (used[i]) continue;
            // Skip duplicates: if same as previous and previous wasn't used, skip
            if (i > 0 && items[i] == items[i - 1] && !used[i - 1]) continue;

            used[i] = true;
            current.Add(items[i]);
            PermutationsHelper(items, used, current, result);
            current.RemoveAt(current.Count - 1);
            used[i] = false;
        }
    }

    private static bool CheckHint(Hint hint, List<Move> executedMoves, ChessBoard finalBoard)
    {
        var scope = hint.Scope;

        if (scope.HalfMove.HasValue)
        {
            int idx = scope.HalfMove.Value - 1; // 1-based to 0-based
            if (idx < 0 || idx >= executedMoves.Count)
                return false;
            return CheckMoveConstraints(hint.Constraints, executedMoves[idx], idx, executedMoves, finalBoard);
        }

        if (scope.HalfMoveRange != null && scope.HalfMoveRange.Length == 2)
        {
            int start = scope.HalfMoveRange[0] - 1;
            int end = scope.HalfMoveRange[1] - 1;
            for (int i = start; i <= end && i < executedMoves.Count; i++)
            {
                if (i >= 0 && CheckMoveConstraints(hint.Constraints, executedMoves[i], i, executedMoves, finalBoard))
                    return true;
            }
            return false;
        }

        if (scope.IsAny)
        {
            for (int i = 0; i < executedMoves.Count; i++)
            {
                if (CheckMoveConstraints(hint.Constraints, executedMoves[i], i, executedMoves, finalBoard))
                    return true;
            }
            return false;
        }

        if (scope.IsFinal)
        {
            return CheckFinalConstraints(hint.Constraints, finalBoard);
        }

        return true; // Unknown scope, pass
    }

    private static bool CheckMoveConstraints(HintConstraints c, Move move, int moveIndex, List<Move> allMoves, ChessBoard finalBoard)
    {
        if (c.Piece != null)
        {
            var expectedType = ParsePieceType(c.Piece);
            if (expectedType == null || move.Piece.Type != expectedType)
                return false;
        }

        if (c.Color != null)
        {
            var expectedColor = c.Color.ToLowerInvariant() == "white" ? PieceColor.White : PieceColor.Black;
            if (move.Piece.Color != expectedColor)
                return false;
        }

        if (c.CapturedPiece != null)
        {
            if (move.CapturedPiece == null)
                return false;
            var expectedType = ParsePieceType(c.CapturedPiece);
            if (expectedType == null || move.CapturedPiece.Type != expectedType)
                return false;
        }

        if (c.ToSquare != null)
        {
            if (move.NewPosition.ToString() != c.ToSquare)
                return false;
        }

        if (c.ToRank.HasValue)
        {
            // Position.Y is 0-based, rank is 1-based
            if (move.NewPosition.Y + 1 != c.ToRank.Value)
                return false;
        }

        if (c.ToFile != null)
        {
            char file = move.NewPosition.File();
            if (file.ToString() != c.ToFile)
                return false;
        }

        if (c.FromSquare != null)
        {
            if (move.OriginalPosition.ToString() != c.FromSquare)
                return false;
        }

        if (c.FromRank.HasValue)
        {
            if (move.OriginalPosition.Y + 1 != c.FromRank.Value)
                return false;
        }

        if (c.FromFile != null)
        {
            char file = move.OriginalPosition.File();
            if (file.ToString() != c.FromFile)
                return false;
        }

        if (c.IsCheck.HasValue)
        {
            if (move.IsCheck != c.IsCheck.Value)
                return false;
        }

        if (c.IsCapture.HasValue)
        {
            bool isCapture = move.CapturedPiece != null;
            if (isCapture != c.IsCapture.Value)
                return false;
        }

        if (c.IsCastle != null)
        {
            if (c.IsCastle.IsBool)
            {
                if (move.IsCastling != c.IsCastle.BoolValue)
                    return false;
            }
            else
            {
                // Specific castle side
                if (!move.IsCastling)
                    return false;
                string side = c.IsCastle.StringValue?.ToLowerInvariant() ?? "";
                if (side == "kingside")
                {
                    // Kingside: king moves to g-file (X=6)
                    if (move.NewPosition.X != 6)
                        return false;
                }
                else if (side == "queenside")
                {
                    // Queenside: king moves to c-file (X=2)
                    if (move.NewPosition.X != 2)
                        return false;
                }
            }
        }

        if (c.IsEnPassant.HasValue)
        {
            if (move.IsEnPassant != c.IsEnPassant.Value)
                return false;
        }

        if (c.IsPromotion.HasValue)
        {
            if (move.IsPromotion != c.IsPromotion.Value)
                return false;
        }

        if (c.PromotionPiece != null)
        {
            if (!move.IsPromotion || move.Promotion == null)
                return false;
            var expectedType = ParsePieceType(c.PromotionPiece);
            if (expectedType == null || move.Promotion.Type != expectedType)
                return false;
        }

        // isCheckmate and isStalemate on a move scope: check if the position after this move is checkmate/stalemate
        if (c.IsCheckmate.HasValue)
        {
            if (move.IsMate != c.IsCheckmate.Value)
                return false;
        }

        if (c.IsStalemate.HasValue)
        {
            // For stalemate on a specific move, we'd need to check if the position after this move is stalemate.
            // IsMate only covers checkmate. For stalemate on final scope we handle it separately.
            // For a move scope, a stalemate means the game ended in stalemate after this move.
            // We can check if this is the last move and the final board shows stalemate.
            if (moveIndex == allMoves.Count - 1)
            {
                bool isStalemate = finalBoard.IsEndGame &&
                                   finalBoard.EndGame?.EndgameType == EndgameType.Stalemate;
                if (isStalemate != c.IsStalemate.Value)
                    return false;
            }
            else
            {
                // Not the last move, can't be stalemate
                if (c.IsStalemate.Value)
                    return false;
            }
        }

        return true;
    }

    private static bool CheckFinalConstraints(HintConstraints c, ChessBoard board)
    {
        if (c.IsCheckmate.HasValue)
        {
            bool isCheckmate = board.IsEndGame &&
                               board.EndGame?.EndgameType == EndgameType.Checkmate;
            if (isCheckmate != c.IsCheckmate.Value)
                return false;
        }

        if (c.IsStalemate.HasValue)
        {
            bool isStalemate = board.IsEndGame &&
                               board.EndGame?.EndgameType == EndgameType.Stalemate;
            if (isStalemate != c.IsStalemate.Value)
                return false;
        }

        if (c.IsCheck.HasValue)
        {
            bool isCheck = board.WhiteKingChecked || board.BlackKingChecked;
            if (isCheck != c.IsCheck.Value)
                return false;
        }

        return true;
    }

    private static PieceType? ParsePieceType(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "pawn" => PieceType.Pawn,
            "knight" => PieceType.Knight,
            "bishop" => PieceType.Bishop,
            "rook" => PieceType.Rook,
            "queen" => PieceType.Queen,
            "king" => PieceType.King,
            _ => null
        };
    }

    /// <summary>
    /// Parse a revealed FEN into a sparse map: board index (0-63) → expected piece char.
    /// Only occupied squares are included — empty squares are "unknown" (don't care).
    /// Index 0 = a8, index 63 = h1 (FEN order: rank 8 first, left to right).
    /// </summary>
    private static Dictionary<int, char> ParseRevealedSquares(string revealedFen)
    {
        var result = new Dictionary<int, char>();
        var boardPart = revealedFen.Split(' ')[0];
        int index = 0;
        foreach (char c in boardPart)
        {
            if (c == '/') continue;
            if (char.IsDigit(c))
            {
                index += c - '0'; // skip empty squares
            }
            else
            {
                result[index] = c;
                index++;
            }
        }
        return result;
    }

    /// <summary>
    /// Expand FEN board part into 64-char array for fast indexed lookup.
    /// Index 0 = a8, index 63 = h1.
    /// </summary>
    private static char[] ExpandFenBoard(string boardPart)
    {
        var result = new char[64];
        int index = 0;
        foreach (char c in boardPart)
        {
            if (c == '/') continue;
            if (char.IsDigit(c))
            {
                int count = c - '0';
                for (int i = 0; i < count; i++)
                    result[index++] = '.';
            }
            else
            {
                result[index++] = c;
            }
        }
        return result;
    }

    // --- FEN manipulation ---

    internal static string ApplyPlacementToFen(string fen, Dictionary<string, string> placement)
    {
        var parts = fen.Split(' ');
        var ranks = parts[0].Split('/');

        // Expand each rank to 8 characters
        var expanded = new char[8][];
        for (int r = 0; r < 8; r++)
        {
            expanded[r] = ExpandFenRank(ranks[r]);
        }

        // Apply placements
        foreach (var (square, piece) in placement)
        {
            int file = square[0] - 'a'; // 0-7
            int rank = square[1] - '1'; // 0-7
            // FEN ranks: index 0 = rank 8, index 7 = rank 1
            int fenRankIndex = 7 - rank;
            expanded[fenRankIndex][file] = PieceToFenChar(piece);
        }

        // Compress back
        for (int r = 0; r < 8; r++)
        {
            ranks[r] = CompressFenRank(expanded[r]);
        }

        parts[0] = string.Join("/", ranks);
        return string.Join(" ", parts);
    }

    private static char[] ExpandFenRank(string rank)
    {
        var result = new char[8];
        int col = 0;
        foreach (char c in rank)
        {
            if (char.IsDigit(c))
            {
                int count = c - '0';
                for (int i = 0; i < count; i++)
                    result[col++] = '.';
            }
            else
            {
                result[col++] = c;
            }
        }
        return result;
    }

    private static string CompressFenRank(char[] rank)
    {
        var sb = new System.Text.StringBuilder();
        int emptyCount = 0;
        foreach (char c in rank)
        {
            if (c == '.')
            {
                emptyCount++;
            }
            else
            {
                if (emptyCount > 0)
                {
                    sb.Append(emptyCount);
                    emptyCount = 0;
                }
                sb.Append(c);
            }
        }
        if (emptyCount > 0)
            sb.Append(emptyCount);
        return sb.ToString();
    }

    private static char PieceToFenChar(string piece)
    {
        // piece format: "white knight", "black bishop", etc.
        var parts = piece.Split(' ');
        bool isWhite = parts[0].ToLowerInvariant() == "white";
        char fenChar = parts[1].ToLowerInvariant() switch
        {
            "pawn" => 'p',
            "knight" => 'n',
            "bishop" => 'b',
            "rook" => 'r',
            "queen" => 'q',
            "king" => 'k',
            _ => '?'
        };
        return isWhite ? char.ToUpper(fenChar) : fenChar;
    }
}
