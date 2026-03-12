namespace ChessPuzzle.Core.Models;

public class EvaluationResult
{
    public List<ValidationError> Errors { get; set; } = new();
    public bool IsValid => Errors.Count == 0;
    public int? SolutionCount { get; set; }
    public long? SearchSpaceSize { get; set; }
    public double? Complexity { get; set; }
    public List<string> Remarks { get; set; } = new();
}
