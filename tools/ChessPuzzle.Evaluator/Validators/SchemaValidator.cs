using ChessPuzzle.Core.Models;

namespace ChessPuzzle.Evaluator.Validators;

public class SchemaValidator : IValidator
{
    public IEnumerable<ValidationError> Validate(Puzzle puzzle)
    {
        if (string.IsNullOrWhiteSpace(puzzle.Metadata.Id))
            yield return new ValidationError("MISSING_ID", "metadata.id is required");

        if (string.IsNullOrWhiteSpace(puzzle.Metadata.Title))
            yield return new ValidationError("MISSING_TITLE", "metadata.title is required");

        if (string.IsNullOrWhiteSpace(puzzle.StartPosition.Fen))
            yield return new ValidationError("MISSING_FEN", "startPosition.fen is required");

        if (puzzle.HalfMoveCount <= 0)
            yield return new ValidationError("INVALID_HALF_MOVE_COUNT", "halfMoveCount must be positive");

        if (string.IsNullOrWhiteSpace(puzzle.RevealedFinalPosition))
            yield return new ValidationError("MISSING_FINAL_POSITION", "revealedFinalPosition is required");

        if (puzzle.PieceConstraints != null)
        {
            foreach (var (color, pieces) in puzzle.PieceConstraints)
            {
                foreach (var (piece, constraint) in pieces)
                {
                    if (constraint.Min < 0)
                        yield return new ValidationError("INVALID_PIECE_CONSTRAINT",
                            $"pieceConstraints.{color}.{piece}.min must be >= 0");

                    if (constraint.Max < 0)
                        yield return new ValidationError("INVALID_PIECE_CONSTRAINT",
                            $"pieceConstraints.{color}.{piece}.max must be >= 0");

                    if (constraint.Min > constraint.Max)
                        yield return new ValidationError("INVALID_PIECE_CONSTRAINT",
                            $"pieceConstraints.{color}.{piece}.min must be <= max");
                }
            }
        }
    }
}
