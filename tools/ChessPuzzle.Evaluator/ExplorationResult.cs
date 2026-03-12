namespace ChessPuzzle.Evaluator;

public class ExplorationResult
{
    public int SolutionCount { get; set; }
    public long SearchSpaceSize { get; set; }
    public List<Solution> Solutions { get; set; } = new();
}
