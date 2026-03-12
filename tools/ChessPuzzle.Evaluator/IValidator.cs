using ChessPuzzle.Core.Models;

namespace ChessPuzzle.Evaluator;

public interface IValidator
{
    IEnumerable<ValidationError> Validate(Puzzle puzzle);
}
