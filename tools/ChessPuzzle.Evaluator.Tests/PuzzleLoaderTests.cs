using ChessPuzzle.Core;
using ChessPuzzle.Core.Models;

namespace ChessPuzzle.Evaluator.Tests;

public class PuzzleLoaderTests
{
    private const string FullPuzzleJson = """
        {
            "metadata": { "id": "puzzle-001", "title": "The Knight's Outpost", "author": "System", "difficulty": 2 },
            "startPosition": {
                "fen": "r2qk2r/pppb1ppp/2np1n2/2b1p3/2B1P3/2NP1N2/PPP2PPP/R1BQR1K1 w kq - 0 7",
                "squares": { "c3": "open", "c4": "open" }
            },
            "pieceConstraints": {
                "white": { "knight": { "min": 1, "max": 1 }, "bishop": { "min": 1, "max": 1 } }
            },
            "halfMoveCount": 4,
            "revealedFinalPosition": "5rk1/8/8/3B4/8/8/8/8 w - - 0 1",
            "hints": [
                {
                    "scope": { "halfMove": 1 },
                    "constraints": { "piece": "knight", "toSquare": "d5" },
                    "text": "White opens with a knight jump to d5"
                },
                {
                    "scope": "any",
                    "constraints": { "isCapture": true },
                    "text": "A capture happens"
                },
                {
                    "scope": "final",
                    "constraints": { "isCheckmate": true },
                    "text": "Checkmate"
                }
            ]
        }
        """;

    [Fact]
    public void FromJson_FullPuzzle_DeserializesAllFields()
    {
        var puzzle = PuzzleLoader.FromJson(FullPuzzleJson);

        Assert.NotNull(puzzle);
        Assert.Equal("puzzle-001", puzzle.Metadata.Id);
        Assert.Equal("The Knight's Outpost", puzzle.Metadata.Title);
        Assert.Equal("System", puzzle.Metadata.Author);
        Assert.Equal(2, puzzle.Metadata.Difficulty);

        Assert.Equal("r2qk2r/pppb1ppp/2np1n2/2b1p3/2B1P3/2NP1N2/PPP2PPP/R1BQR1K1 w kq - 0 7", puzzle.StartPosition.Fen);
        Assert.Equal(2, puzzle.StartPosition.Squares!.Count);
        Assert.Equal("open", puzzle.StartPosition.Squares["c3"]);
        Assert.Equal("open", puzzle.StartPosition.Squares["c4"]);

        Assert.Equal(4, puzzle.HalfMoveCount);
        Assert.Equal("5rk1/8/8/3B4/8/8/8/8 w - - 0 1", puzzle.RevealedFinalPosition);

        Assert.Equal(3, puzzle.Hints.Count);
    }

    [Fact]
    public void FromJson_HintWithAnyScope_ParsedCorrectly()
    {
        var puzzle = PuzzleLoader.FromJson(FullPuzzleJson);

        var anyHint = puzzle.Hints[1];
        Assert.True(anyHint.Scope.IsAny);
        Assert.False(anyHint.Scope.IsFinal);
        Assert.Null(anyHint.Scope.HalfMove);
        Assert.Null(anyHint.Scope.HalfMoveRange);
        Assert.True(anyHint.Constraints.IsCapture);
        Assert.Equal("A capture happens", anyHint.Text);
    }

    [Fact]
    public void FromJson_HintWithFinalScope_ParsedCorrectly()
    {
        var puzzle = PuzzleLoader.FromJson(FullPuzzleJson);

        var finalHint = puzzle.Hints[2];
        Assert.True(finalHint.Scope.IsFinal);
        Assert.False(finalHint.Scope.IsAny);
        Assert.Null(finalHint.Scope.HalfMove);
        Assert.Null(finalHint.Scope.HalfMoveRange);
        Assert.True(finalHint.Constraints.IsCheckmate);
        Assert.Equal("Checkmate", finalHint.Text);
    }

    [Fact]
    public void FromJson_HintWithHalfMoveScope_ParsedCorrectly()
    {
        var puzzle = PuzzleLoader.FromJson(FullPuzzleJson);

        var moveHint = puzzle.Hints[0];
        Assert.Equal(1, moveHint.Scope.HalfMove);
        Assert.False(moveHint.Scope.IsAny);
        Assert.False(moveHint.Scope.IsFinal);
        Assert.Null(moveHint.Scope.HalfMoveRange);
        Assert.Equal("knight", moveHint.Constraints.Piece);
        Assert.Equal("d5", moveHint.Constraints.ToSquare);
        Assert.Equal("White opens with a knight jump to d5", moveHint.Text);
    }

    [Fact]
    public void FromJson_HintWithHalfMoveRangeScope_ParsedCorrectly()
    {
        var json = """
            {
                "metadata": { "id": "test", "title": "Test", "author": "Test", "difficulty": 1 },
                "startPosition": { "fen": "8/8/8/8/8/8/8/8 w - - 0 1" },
                "halfMoveCount": 2,
                "hints": [
                    {
                        "scope": { "halfMoveRange": [1, 4] },
                        "constraints": { "piece": "pawn" },
                        "text": "A pawn moves in moves 1-4"
                    }
                ]
            }
            """;

        var puzzle = PuzzleLoader.FromJson(json);
        var hint = puzzle.Hints[0];

        Assert.NotNull(hint.Scope.HalfMoveRange);
        Assert.Equal(2, hint.Scope.HalfMoveRange!.Length);
        Assert.Equal(1, hint.Scope.HalfMoveRange[0]);
        Assert.Equal(4, hint.Scope.HalfMoveRange[1]);
        Assert.False(hint.Scope.IsAny);
        Assert.False(hint.Scope.IsFinal);
        Assert.Null(hint.Scope.HalfMove);
    }

    [Fact]
    public void FromJson_PieceConstraints_ParsedCorrectly()
    {
        var puzzle = PuzzleLoader.FromJson(FullPuzzleJson);

        Assert.NotNull(puzzle.PieceConstraints);
        Assert.True(puzzle.PieceConstraints!.ContainsKey("white"));

        var white = puzzle.PieceConstraints["white"];
        Assert.True(white.ContainsKey("knight"));
        Assert.Equal(1, white["knight"].Min);
        Assert.Equal(1, white["knight"].Max);
        Assert.True(white.ContainsKey("bishop"));
        Assert.Equal(1, white["bishop"].Min);
        Assert.Equal(1, white["bishop"].Max);
    }

    [Fact]
    public void FromJson_IsCastleAsBool_ParsedCorrectly()
    {
        var json = """
            {
                "metadata": { "id": "test", "title": "Test", "author": "Test", "difficulty": 1 },
                "startPosition": { "fen": "8/8/8/8/8/8/8/8 w - - 0 1" },
                "halfMoveCount": 2,
                "hints": [
                    {
                        "scope": "any",
                        "constraints": { "isCastle": true },
                        "text": "A castle happens"
                    }
                ]
            }
            """;

        var puzzle = PuzzleLoader.FromJson(json);
        var castle = puzzle.Hints[0].Constraints.IsCastle;
        Assert.NotNull(castle);
        Assert.True(castle.IsBool);
        Assert.True(castle.BoolValue);
    }

    [Fact]
    public void FromJson_IsCastleAsString_ParsedCorrectly()
    {
        var json = """
            {
                "metadata": { "id": "test", "title": "Test", "author": "Test", "difficulty": 1 },
                "startPosition": { "fen": "8/8/8/8/8/8/8/8 w - - 0 1" },
                "halfMoveCount": 2,
                "hints": [
                    {
                        "scope": "any",
                        "constraints": { "isCastle": "kingside" },
                        "text": "Kingside castle"
                    }
                ]
            }
            """;

        var puzzle = PuzzleLoader.FromJson(json);
        var castle = puzzle.Hints[0].Constraints.IsCastle;
        Assert.NotNull(castle);
        Assert.False(castle.IsBool);
        Assert.Equal("kingside", castle.StringValue);
    }
}
