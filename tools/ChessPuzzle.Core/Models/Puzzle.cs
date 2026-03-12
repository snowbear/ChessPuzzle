namespace ChessPuzzle.Core.Models;

public class Puzzle
{
    public PuzzleMetadata Metadata { get; set; } = new();
    public StartPosition StartPosition { get; set; } = new();
    public Dictionary<string, Dictionary<string, PieceConstraint>>? PieceConstraints { get; set; }
    public int HalfMoveCount { get; set; }
    public string? RevealedFinalPosition { get; set; }
    public List<Hint> Hints { get; set; } = new();
}

public class PuzzleMetadata
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public int Difficulty { get; set; }
}

public class StartPosition
{
    public string Fen { get; set; } = "";
    public Dictionary<string, string>? Squares { get; set; }
}

public class PieceConstraint
{
    public int Min { get; set; }
    public int Max { get; set; }
}

public class Hint
{
    public HintScope Scope { get; set; } = new();
    public HintConstraints Constraints { get; set; } = new();
    public string Text { get; set; } = "";
}

public class HintScope
{
    public int? HalfMove { get; set; }
    public int[]? HalfMoveRange { get; set; }
    public bool IsAny { get; set; }
    public bool IsFinal { get; set; }
}

/// <summary>
/// Represents the IsCastle field which can be either a bool (true/false)
/// or a string ("kingside"/"queenside").
/// </summary>
public class CastleValue
{
    public bool IsBool { get; }
    public bool BoolValue { get; }
    public string? StringValue { get; }

    public CastleValue(bool value)
    {
        IsBool = true;
        BoolValue = value;
    }

    public CastleValue(string value)
    {
        IsBool = false;
        StringValue = value;
    }
}

public class HintConstraints
{
    public string? Color { get; set; }
    public string? Piece { get; set; }
    public string? CapturedPiece { get; set; }
    public string? ToSquare { get; set; }
    public int? ToRank { get; set; }
    public string? ToFile { get; set; }
    public string? FromSquare { get; set; }
    public int? FromRank { get; set; }
    public string? FromFile { get; set; }
    public bool? IsCheck { get; set; }
    public bool? IsCapture { get; set; }
    public CastleValue? IsCastle { get; set; }
    public bool? IsEnPassant { get; set; }
    public bool? IsPromotion { get; set; }
    public string? PromotionPiece { get; set; }
    public bool? IsCheckmate { get; set; }
    public bool? IsStalemate { get; set; }
}
