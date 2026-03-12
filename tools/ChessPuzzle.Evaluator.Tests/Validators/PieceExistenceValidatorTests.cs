using ChessPuzzle.Core.Models;
using ChessPuzzle.Evaluator.Validators;

namespace ChessPuzzle.Evaluator.Tests.Validators;

public class PieceExistenceValidatorTests
{
    private readonly PieceExistenceValidator _validator = new();

    [Fact]
    public void Validate_PieceOnBoard_NoError()
    {
        // Standard starting position has knights
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

        Assert.DoesNotContain(errors, e => e.Code == "PIECE_NOT_AVAILABLE");
    }

    [Fact]
    public void Validate_PieceNotOnBoardButInConstraints_NoError()
    {
        // Position with no white queens, but constraints allow placing one
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.StartPosition.Fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNB1KBNR w KQkq - 0 1";
        puzzle.PieceConstraints = new Dictionary<string, Dictionary<string, PieceConstraint>>
        {
            ["white"] = new Dictionary<string, PieceConstraint>
            {
                ["queen"] = new PieceConstraint { Min = 0, Max = 1 }
            }
        };
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { HalfMove = 1 },
                Constraints = new HintConstraints { Piece = "queen" }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.DoesNotContain(errors, e => e.Code == "PIECE_NOT_AVAILABLE");
    }

    [Fact]
    public void Validate_PieceNowhere_NoPawns_ReturnsError()
    {
        // Position with no white queens and no white pawns (so no promotion possible)
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.StartPosition.Fen = "rnbqkbnr/pppppppp/8/8/8/8/8/RNB1KBNR w KQkq - 0 1";
        puzzle.PieceConstraints = new();
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { HalfMove = 1 },
                Constraints = new HintConstraints { Piece = "queen" }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.Contains(errors, e => e.Code == "PIECE_NOT_AVAILABLE");
    }

    [Fact]
    public void Validate_PieceNotOnBoardButPawnsExist_NoError()
    {
        // No white queen on board, but white pawns exist (promotion possible)
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.StartPosition.Fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNB1KBNR w KQkq - 0 1";
        puzzle.PieceConstraints = new();
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { HalfMove = 1 },
                Constraints = new HintConstraints { Piece = "queen" }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.DoesNotContain(errors, e => e.Code == "PIECE_NOT_AVAILABLE");
    }

    [Fact]
    public void Validate_AnyScopeHint_OneColorHasPiece_NoError()
    {
        // Only black has a queen on the board, "any" scope hint for queen
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.StartPosition.Fen = "rnbqkbnr/pppppppp/8/8/8/8/8/RNB1KBNR w KQkq - 0 1";
        puzzle.PieceConstraints = new();
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { IsAny = true },
                Constraints = new HintConstraints { Piece = "queen" }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.DoesNotContain(errors, e => e.Code == "PIECE_NOT_AVAILABLE");
    }

    [Fact]
    public void Validate_AnyScopeHint_NeitherColorHasPiece_ReturnsError()
    {
        // No queens for either color, no pawns for either color
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.StartPosition.Fen = "rnb1kbnr/8/8/8/8/8/8/RNB1KBNR w KQkq - 0 1";
        puzzle.PieceConstraints = new();
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { IsAny = true },
                Constraints = new HintConstraints { Piece = "queen" }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.Contains(errors, e => e.Code == "PIECE_NOT_AVAILABLE");
    }

    [Fact]
    public void Validate_NoPieceConstraintOnHint_NoError()
    {
        // Hint without a Piece constraint should be skipped
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { HalfMove = 1 },
                Constraints = new HintConstraints { ToSquare = "e4" }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.DoesNotContain(errors, e => e.Code == "PIECE_NOT_AVAILABLE");
    }

    [Fact]
    public void Validate_PieceInConstraintsWithMinGreaterThanZero_NoError()
    {
        // No white queen on board, but constraint requires placing one (min=1)
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.StartPosition.Fen = "rnbqkbnr/pppppppp/8/8/8/8/8/RNB1KBNR w KQkq - 0 1";
        puzzle.PieceConstraints = new Dictionary<string, Dictionary<string, PieceConstraint>>
        {
            ["white"] = new Dictionary<string, PieceConstraint>
            {
                ["queen"] = new PieceConstraint { Min = 1, Max = 1 }
            }
        };
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { HalfMove = 1 },
                Constraints = new HintConstraints { Piece = "queen" }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.DoesNotContain(errors, e => e.Code == "PIECE_NOT_AVAILABLE");
    }

    [Fact]
    public void Validate_ExplicitColorConstraint_UsesConstraintColor()
    {
        // White to move, hint half-move 1 would be white, but explicit color says black
        // Black has a queen on the board
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.StartPosition.Fen = "rnbqkbnr/pppppppp/8/8/8/8/8/RNB1KBNR w KQkq - 0 1";
        puzzle.PieceConstraints = new();
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { HalfMove = 1 },
                Constraints = new HintConstraints { Piece = "queen", Color = "black" }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        // Black has a queen on board
        Assert.DoesNotContain(errors, e => e.Code == "PIECE_NOT_AVAILABLE");
    }

    [Fact]
    public void Validate_PawnConstraintAllowsPromotion_NoError()
    {
        // No white queen, no white pawns on board, but pawn constraint (max > 0) exists
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.StartPosition.Fen = "rnbqkbnr/pppppppp/8/8/8/8/8/RNB1KBNR w KQkq - 0 1";
        puzzle.PieceConstraints = new Dictionary<string, Dictionary<string, PieceConstraint>>
        {
            ["white"] = new Dictionary<string, PieceConstraint>
            {
                ["pawn"] = new PieceConstraint { Min = 0, Max = 2 }
            }
        };
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { HalfMove = 1 },
                Constraints = new HintConstraints { Piece = "queen" }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.DoesNotContain(errors, e => e.Code == "PIECE_NOT_AVAILABLE");
    }

    [Fact]
    public void Validate_KingAlwaysOnBoard_NoError()
    {
        // King is always on the board in standard position
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { HalfMove = 1 },
                Constraints = new HintConstraints { Piece = "king" }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        Assert.DoesNotContain(errors, e => e.Code == "PIECE_NOT_AVAILABLE");
    }

    [Fact]
    public void Validate_BlackToMove_HalfMove1IsBlack()
    {
        // Black to move, half-move 1 = black. Black has no queen, no pawns => error
        var puzzle = TestHelper.MakeValidPuzzle();
        puzzle.StartPosition.Fen = "rnb1kbnr/8/8/8/8/8/PPPPPPPP/RNBQKBNR b KQkq - 0 1";
        puzzle.PieceConstraints = new();
        puzzle.Hints = new List<Hint>
        {
            new()
            {
                Scope = new HintScope { HalfMove = 1 },
                Constraints = new HintConstraints { Piece = "queen" }
            }
        };

        var errors = _validator.Validate(puzzle).ToList();

        // Half-move 1 with black to move = black's turn, black has no queen and no pawns
        Assert.Contains(errors, e => e.Code == "PIECE_NOT_AVAILABLE");
    }
}
