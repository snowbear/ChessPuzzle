namespace ChessPuzzle.Core.Models;

public class ValidationError
{
    public string Code { get; set; } = "";
    public string Message { get; set; } = "";

    public ValidationError() { }

    public ValidationError(string code, string message)
    {
        Code = code;
        Message = message;
    }
}
