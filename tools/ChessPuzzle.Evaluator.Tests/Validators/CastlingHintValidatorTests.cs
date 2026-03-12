using ChessPuzzle.Core.Models;
using ChessPuzzle.Evaluator.Validators;

namespace ChessPuzzle.Evaluator.Tests.Validators;

public class CastlingHintValidatorTests
{
    private readonly CastlingHintValidator _validator = new();

    [Fact]
    public void Validate_CastleHintWithRightsPresent_NoError()
    {
        // Standard starting position has KQkq rights, white to move
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { HalfMove = 1 },
                Constraints = new HintConstraints { IsCastle = new CastleValue(true) }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.DoesNotContain(errors, e => e.Code == "CASTLE_IMPOSSIBLE");
    }

    [Fact]
    public void Validate_KingsideCastleWhiteTurn_NoKingsideRight_ReturnsError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        // Only queenside rights for white
        puzzle.StartPosition.Fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w Qkq - 0 1";
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { HalfMove = 1 },
                Constraints = new HintConstraints { IsCastle = new CastleValue("kingside") }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.Contains(errors, e => e.Code == "CASTLE_IMPOSSIBLE");
    }

    [Fact]
    public void Validate_QueensideCastleBlack_NoBlackQueensideRight_ReturnsError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        // White has all rights, black only has kingside
        puzzle.StartPosition.Fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQk - 0 1";
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { HalfMove = 2 }, // half-move 2 = black's turn (white starts)
                Constraints = new HintConstraints { IsCastle = new CastleValue("queenside") }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.Contains(errors, e => e.Code == "CASTLE_IMPOSSIBLE");
    }

    [Fact]
    public void Validate_AnyScopeCastleHint_AtLeastOneColorHasRights_NoError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        // Only white has kingside right
        puzzle.StartPosition.Fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w K - 0 1";
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { IsAny = true },
                Constraints = new HintConstraints { IsCastle = new CastleValue(true) }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.DoesNotContain(errors, e => e.Code == "CASTLE_IMPOSSIBLE");
    }

    [Fact]
    public void Validate_AnyScopeCastleHint_NoColorHasRights_ReturnsError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        // No castling rights
        puzzle.StartPosition.Fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w - - 0 1";
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { IsAny = true },
                Constraints = new HintConstraints { IsCastle = new CastleValue(true) }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.Contains(errors, e => e.Code == "CASTLE_IMPOSSIBLE");
    }

    [Fact]
    public void Validate_NoCastleHints_NoError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { HalfMove = 1 },
                Constraints = new HintConstraints { Piece = "knight" }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.DoesNotContain(errors, e => e.Code == "CASTLE_IMPOSSIBLE");
    }

    [Fact]
    public void Validate_CastleFalse_NoError()
    {
        // IsCastle = false should not trigger validation
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.StartPosition.Fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w - - 0 1";
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { HalfMove = 1 },
                Constraints = new HintConstraints { IsCastle = new CastleValue(false) }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.DoesNotContain(errors, e => e.Code == "CASTLE_IMPOSSIBLE");
    }

    [Fact]
    public void Validate_ExplicitColorConstraint_UsesConstraintColor()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        // White to move, only black has kingside castling right
        puzzle.StartPosition.Fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w k - 0 1";
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { HalfMove = 1 },
                Constraints = new HintConstraints
                {
                    IsCastle = new CastleValue("kingside"),
                    Color = "white" // Explicit color says white
                }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        // White has no kingside right, should error
        Assert.Contains(errors, e => e.Code == "CASTLE_IMPOSSIBLE");
    }

    [Fact]
    public void Validate_BlackToMove_HalfMove1IsBlack()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        // Black to move, black has kingside right
        puzzle.StartPosition.Fen = "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b k - 0 1";
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { HalfMove = 1 },
                Constraints = new HintConstraints { IsCastle = new CastleValue("kingside") }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.DoesNotContain(errors, e => e.Code == "CASTLE_IMPOSSIBLE");
    }

    [Fact]
    public void Validate_KingNotOnStartingSquare_ReturnsError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        // White king on d1 instead of e1, but FEN says K castling right
        puzzle.StartPosition.Fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBK1BNR w K - 0 1";
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { HalfMove = 1 },
                Constraints = new HintConstraints { IsCastle = new CastleValue("kingside") }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.Contains(errors, e => e.Code == "CASTLE_IMPOSSIBLE");
    }

    [Fact]
    public void Validate_RookNotOnStartingSquare_ReturnsError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        // White kingside rook not on h1 (replaced with nothing), but FEN says K right
        puzzle.StartPosition.Fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBN1 w K - 0 1";
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { HalfMove = 1 },
                Constraints = new HintConstraints { IsCastle = new CastleValue("kingside") }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.Contains(errors, e => e.Code == "CASTLE_IMPOSSIBLE");
    }

    [Fact]
    public void Validate_KingSquareIsOpen_NoError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        // King not on e1 but the square is open (player could place king there)
        puzzle.StartPosition.Fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQ1BNR w K - 0 1";
        puzzle.StartPosition.Squares = new Dictionary<string, string>
        {
            ["e1"] = "open"
        };
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { HalfMove = 1 },
                Constraints = new HintConstraints { IsCastle = new CastleValue("kingside") }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        // h1 has a rook, e1 is open, so castling is possible
        Assert.DoesNotContain(errors, e => e.Code == "CASTLE_IMPOSSIBLE");
    }

    [Fact]
    public void Validate_AnyScopeKingsideHint_OneColorHasKingsideRight_NoError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        // Only black has kingside right
        puzzle.StartPosition.Fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w k - 0 1";
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { IsAny = true },
                Constraints = new HintConstraints { IsCastle = new CastleValue("kingside") }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.DoesNotContain(errors, e => e.Code == "CASTLE_IMPOSSIBLE");
    }

    [Fact]
    public void Validate_AnyScopeKingsideHint_NoColorHasKingsideRight_ReturnsError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        // Only queenside rights
        puzzle.StartPosition.Fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w Qq - 0 1";
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { IsAny = true },
                Constraints = new HintConstraints { IsCastle = new CastleValue("kingside") }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.Contains(errors, e => e.Code == "CASTLE_IMPOSSIBLE");
    }

    [Fact]
    public void Validate_HalfMoveRange_CastleHint_NoRightsForEitherColor_ReturnsError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.StartPosition.Fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w - - 0 1";
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { HalfMoveRange = new[] { 1, 3 } },
                Constraints = new HintConstraints { IsCastle = new CastleValue(true) }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.Contains(errors, e => e.Code == "CASTLE_IMPOSSIBLE");
    }

    [Fact]
    public void Validate_HalfMoveRange_CastleHint_OneColorInRangeHasRights_NoError()
    {
        var puzzle = TestHelper.MakeValidPuzzle();
        // White to move, only white has kingside right
        puzzle.StartPosition.Fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w K - 0 1";
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { HalfMoveRange = new[] { 1, 2 } },
                Constraints = new HintConstraints { IsCastle = new CastleValue(true) }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        // Half-move 1 is white, white has K right => no error
        Assert.DoesNotContain(errors, e => e.Code == "CASTLE_IMPOSSIBLE");
    }
}
