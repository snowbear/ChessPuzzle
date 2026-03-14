using System.Text.Json;
using System.Text.Json.Serialization;
using ChessPuzzle.Core;
using ChessPuzzle.Evaluator;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: chesspuzzle-eval [--validate-only] <puzzle.json> [puzzle2.json] [directory/]");
    return 1;
}

var validateOnly = false;
var pathArgs = args.AsSpan();

if (args[0] == "--validate-only")
{
    validateOnly = true;
    pathArgs = pathArgs[1..];
}

if (pathArgs.Length == 0)
{
    Console.Error.WriteLine("No file or directory arguments provided.");
    return 1;
}

var files = new List<string>();
foreach (var arg in pathArgs)
{
    if (Directory.Exists(arg))
        files.AddRange(Directory.GetFiles(arg, "*.json"));
    else if (File.Exists(arg))
        files.Add(arg);
    else
    {
        Console.Error.WriteLine($"File not found: {arg}");
        return 1;
    }
}

if (files.Count == 0)
{
    Console.Error.WriteLine("No puzzle files found.");
    return 1;
}

var evaluator = new PuzzleEvaluator();
var results = new List<object>();
var allValid = true;

foreach (var file in files)
{
    try
    {
        var puzzle = PuzzleLoader.FromFile(file);
        var evalResult = validateOnly
            ? evaluator.ValidateOnly(puzzle)
            : evaluator.Evaluate(puzzle);

        if (!evalResult.IsValid)
            allValid = false;

        results.Add(new
        {
            file = Path.GetFileName(file),
            valid = evalResult.IsValid,
            errors = evalResult.Errors.Select(e => new { code = e.Code, message = e.Message }),
            solutionCount = evalResult.SolutionCount,
            searchSpaceSize = evalResult.SearchSpaceSize,
            complexity = evalResult.Complexity,
            remarks = evalResult.Remarks
        });
    }
    catch (Exception ex)
    {
        allValid = false;
        results.Add(new
        {
            file = Path.GetFileName(file),
            valid = false,
            errors = new[] { new { code = "LOAD_ERROR", message = ex.Message } },
            solutionCount = (int?)null,
            searchSpaceSize = (long?)null,
            complexity = (double?)null,
            remarks = Array.Empty<string>()
        });
    }
}

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

var output = results.Count == 1
    ? JsonSerializer.Serialize(results[0], jsonOptions)
    : JsonSerializer.Serialize(results, jsonOptions);

Console.WriteLine(output);

return allValid ? 0 : 1;
