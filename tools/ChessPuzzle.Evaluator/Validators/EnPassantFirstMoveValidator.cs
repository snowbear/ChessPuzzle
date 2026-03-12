using ChessPuzzle.Core.Models;

namespace ChessPuzzle.Evaluator.Validators;

public class EnPassantFirstMoveValidator : IValidator
{
    public IEnumerable<ValidationError> Validate(Puzzle puzzle)
    {
        var fen = puzzle.StartPosition.Fen;
        if (string.IsNullOrEmpty(fen))
            yield break;

        var fenParts = fen.Split(' ');
        if (fenParts.Length < 4)
            yield break;

        var enPassantField = fenParts[3]; // e.g. "e3" or "-"

        foreach (var hint in puzzle.Hints)
        {
            if (hint.Constraints.IsEnPassant != true)
                continue;

            // Only validate if the hint is on half-move 1
            if (hint.Scope.HalfMove != 1)
                continue;

            if (enPassantField == "-")
            {
                yield return new ValidationError("EN_PASSANT_IMPOSSIBLE",
                    "En passant hint on half-move 1 but FEN has no en passant target square");
            }
        }
    }
}
