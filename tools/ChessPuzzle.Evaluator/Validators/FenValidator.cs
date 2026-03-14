using Chess;
using ChessPuzzle.Core.Models;

namespace ChessPuzzle.Evaluator.Validators;

public class FenValidator : IValidator
{
    public IEnumerable<ValidationError> Validate(Puzzle puzzle)
    {
        if (!TryParseFen(puzzle.StartPosition.Fen))
            yield return new ValidationError("INVALID_FEN",
                $"Cannot parse starting FEN: {puzzle.StartPosition.Fen}");

        if (!TryParseFinalFen(puzzle.RevealedFinalPosition))
            yield return new ValidationError("INVALID_FINAL_FEN",
                $"Cannot parse revealed final position FEN: {puzzle.RevealedFinalPosition}");
    }

    private static bool TryParseFen(string? fen)
    {
        if (string.IsNullOrEmpty(fen)) return false;
        try { ChessBoard.LoadFromFen(fen); return true; }
        catch { return false; }
    }

    /// <summary>
    /// Validates the revealed final position FEN. This is a partial board showing
    /// only the revealed pieces, so it may not contain both kings. We first try
    /// normal parsing, then fall back to validating the FEN structure manually.
    /// </summary>
    private static bool TryParseFinalFen(string? fen)
    {
        if (string.IsNullOrEmpty(fen)) return false;
        // Try normal parse first
        if (TryParseFen(fen)) return true;
        // The revealed final position may be a partial board (e.g., missing a king).
        // Validate structure: 8 ranks separated by '/', valid piece chars and digits.
        return IsValidFenStructure(fen);
    }

    private static bool IsValidFenStructure(string fen)
    {
        var parts = fen.Split(' ');
        if (parts.Length < 1) return false;

        var ranks = parts[0].Split('/');
        if (ranks.Length != 8) return false;

        const string validPieceChars = "pnbrqkPNBRQK";
        foreach (var rank in ranks)
        {
            int count = 0;
            foreach (char c in rank)
            {
                if (char.IsDigit(c))
                    count += c - '0';
                else if (validPieceChars.Contains(c))
                    count++;
                else
                    return false;
            }
            if (count != 8) return false;
        }
        return true;
    }
}
