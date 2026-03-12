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

        if (!TryParseFen(puzzle.RevealedFinalPosition))
            yield return new ValidationError("INVALID_FINAL_FEN",
                $"Cannot parse revealed final position FEN: {puzzle.RevealedFinalPosition}");
    }

    private static bool TryParseFen(string? fen)
    {
        if (string.IsNullOrEmpty(fen)) return false;
        try { ChessBoard.LoadFromFen(fen); return true; }
        catch { return false; }
    }
}
