using ChessPuzzle.Core.Models;
using ChessPuzzle.Evaluator.Validators;

namespace ChessPuzzle.Evaluator;

public class PuzzleEvaluator
{
    private readonly List<IValidator> _validators = new()
    {
        new SchemaValidator(),
        new FenValidator(),
        new OpenSquareConsistencyValidator(),
        new KingConstraintValidator(),
        new CastlingHintValidator(),
        new PieceExistenceValidator(),
        new PawnCaptureReachValidator(),
        new EnPassantFirstMoveValidator(),
    };

    public EvaluationResult ValidateOnly(Puzzle puzzle)
    {
        var result = new EvaluationResult();
        foreach (var validator in _validators)
            result.Errors.AddRange(validator.Validate(puzzle));
        return result;
    }

    public EvaluationResult Evaluate(Puzzle puzzle)
    {
        var result = ValidateOnly(puzzle);
        if (!result.IsValid)
            return result;

        var exploration = SolutionSpaceExplorer.Explore(puzzle);
        result.SolutionCount = exploration.SolutionCount;
        result.SearchSpaceSize = exploration.SearchSpaceSize;

        if (exploration.SearchSpaceSize > 0)
            result.Complexity = (double)exploration.SolutionCount / exploration.SearchSpaceSize;
        else
            result.Complexity = 0;

        if (exploration.SolutionCount == 0)
            result.Remarks.Add("No valid solutions found");
        else if (exploration.SolutionCount == 1)
            result.Remarks.Add("Single unique solution");
        else
            result.Remarks.Add($"{exploration.SolutionCount} valid solutions found");

        return result;
    }
}
