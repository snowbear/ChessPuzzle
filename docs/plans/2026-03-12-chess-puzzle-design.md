# Chess Puzzle Game — Design Document

## Overview

A browser-based deduction chess puzzle game. The player reasons about a partially hidden starting position and a set of objective hints to reconstruct the correct piece placement and move sequence, ultimately matching a partially revealed final position.

## Core Game Loop

1. **Observe** — player sees a partially revealed starting position (some pieces fixed, some squares blocked-empty, some open) and a thumbnail of the partially revealed final position.
2. **Place** — player drags pieces from a piece tray onto open squares of the starting position, respecting per-piece min/max constraints.
3. **Play** — player makes legal chess moves (the required number of half-moves) on the board.
4. **Submit** — game validates that the final board matches all revealed final squares and that all hints are satisfied.
5. **Feedback** — pass/fail per hint, mismatched final squares highlighted. Player can go back and adjust.

## Data Model

### Puzzle JSON

```json
{
  "metadata": {
    "id": "puzzle-001",
    "title": "The Knight's Gambit",
    "author": "...",
    "difficulty": 3
  },
  "startPosition": {
    "squares": {
      "a1": { "type": "revealed", "piece": { "color": "white", "type": "rook" } },
      "b1": { "type": "open" },
      "c1": { "type": "blocked" },
      "...": "..."
    },
    "castlingRights": { "whiteKingside": true, "whiteQueenside": false, "blackKingside": true, "blackQueenside": false },
    "enPassantTarget": null,
    "activeColor": "white"
  },
  "pieceConstraints": {
    "white": {
      "king": { "min": 0, "max": 0 },
      "queen": { "min": 0, "max": 1 },
      "rook": { "min": 0, "max": 0 },
      "bishop": { "min": 1, "max": 2 },
      "knight": { "min": 0, "max": 1 },
      "pawn": { "min": 0, "max": 3 }
    },
    "black": {
      "...": "..."
    }
  },
  "halfMoveCount": 6,
  "revealedFinalPosition": {
    "e4": { "color": "white", "type": "queen" },
    "g8": { "color": "black", "type": "king" },
    "...": "..."
  },
  "hints": [
    {
      "scope": { "halfMove": 3 },
      "constraints": { "color": "black", "piece": "knight", "isCheck": true },
      "text": "Black moved a knight and gave check on move 2 (half-move 3)"
    },
    {
      "scope": "any",
      "constraints": { "isCapture": true, "capturedPiece": "rook", "toRank": 8 },
      "text": "A rook was captured on the 8th rank at some point"
    },
    {
      "scope": "final",
      "constraints": { "isCheckmate": true },
      "text": "Final position is checkmate"
    }
  ]
}
```

### Square States

**Starting position:**
- `revealed` — fixed piece, player cannot change
- `blocked` — guaranteed empty, player cannot place here
- `open` — player may place a piece here or leave empty

**Final position (revealed):**
- Squares present in `revealedFinalPosition` must match exactly after the last move
- All other squares are unknown (can be anything)

### Piece Constraints

Per piece type and color: `{min, max}` count of pieces the player must **add** to the starting position. Enforced by the piece tray UI (not at submit time). Min = player must place at least this many. Max = player cannot place more than this many.

## Hint System

### Structure

A hint combines a **scope** (when/where it applies) with **constraints** (what must be true):

**Scope types:**
- `{ halfMove: N }` — applies to a specific half-move
- `{ halfMoveRange: [start, end] }` — applies to a range
- `"any"` — must be true for at least one half-move
- `"final"` — applies to the final position

**Constraint fields** (all optional, combined with AND):
- `color` — white/black (validated against half-move parity)
- `piece` — piece type that moved (king, queen, rook, bishop, knight, pawn)
- `capturedPiece` — piece type that was captured
- `toSquare`, `toRank`, `toFile` — destination
- `fromSquare`, `fromRank`, `fromFile` — origin
- `isCheck` — move results in check
- `isCapture` — move is a capture
- `isCastle` — kingside/queenside/true (either)
- `isEnPassant` — move is en passant
- `isPromotion` — move is a promotion
- `promotionPiece` — what the pawn promoted to
- `isCheckmate` — position is checkmate (final scope only)
- `isStalemate` — position is stalemate (final scope only)

### Extensibility

Adding a new constraint type requires:
1. Add the optional field to the constraint schema
2. Add a predicate function that evaluates it against a move/position
3. No structural changes needed

## UI Layout

```
+-------------------------------------------------------+
|  Title / Puzzle Info                                   |
+-------------------+-------------------+----------------+
|                   |                   |                |
|                   |   Main Board      |   Hints Panel  |
|   Piece Tray      |   (interactive)   |   (text list)  |
|   (always visible, |                   |                |
|    disabled when   |                   |                |
|    not at move 0)  |                   |                |
|                   |                   |                |
+-------------------+-------------------+----------------+
|   Move History (algebraic notation, clickable)         |
|   [<< < > >>]  [Submit]                               |
+-------------------------------------------------------+
|   Final Position Thumbnail (small, read-only)          |
+-------------------------------------------------------+
```

### Interaction Details

**At move 0 (placement phase):**
- Piece tray is active — player drags pieces onto open squares
- Player can remove previously placed pieces (not revealed ones)
- Open squares have distinct visual style (dotted border / light shade)
- Blocked squares have distinct visual style (e.g., X mark or dark shade)

**During play (move 1+):**
- Piece tray is visible but grayed out / disabled
- Board enforces legal chess moves only via chess.js
- Illegal moves are rejected silently (piece snaps back)

**Navigation:**
- Back/forward buttons and clickable move list
- Going back to move 0 re-enables piece tray
- If player changes piece placement, subsequent moves that become illegal are highlighted in red in the move history (not auto-removed)

**Submit:**
- Enabled when all half-moves have been played
- Validates: (1) revealed final squares match, (2) all hints pass
- Shows pass/fail per hint, highlights mismatched final squares on thumbnail
- On success: congratulations, mark complete, offer next puzzle

## Tech Stack

- **chess.js** — move validation, legal move generation, FEN, check/checkmate detection
- **cm-chessboard** — SVG board rendering, drag-and-drop, responsive, markers/arrows
- **Vanilla JS with ES modules** — no framework, no build step
- **Local HTTP server required** — `npx http-server` or `python -m http.server` (ship a start script)
- Distribution: zip with a `start.sh` / `start.bat` that launches the server and opens the browser

## Project Structure

```
ChessPuzzle/
├── index.html
├── css/
│   └── style.css
├── js/
│   ├── app.js              — entry point, initializes UI and wires components
│   ├── puzzle.js            — loads puzzle JSON, manages puzzle state
│   ├── board.js             — wraps cm-chessboard, handles rendering modes
│   ├── game.js              — wraps chess.js, legal move enforcement
│   ├── hints.js             — hint constraint definitions and evaluation
│   ├── moveHistory.js       — move list UI, back/forward navigation
│   ├── pieceTray.js         — piece placement UI, min/max enforcement
│   └── validation.js        — submit logic, final position + hint checking
├── puzzles/
│   └── puzzle-001.json
├── assets/
│   └── pieces/              — piece SVGs if not using library defaults
└── lib/
    ├── chess.min.js
    └── cm-chessboard/
```

## Future Considerations (not in v1)

- **Level editor** — visual puzzle creation tool
- **Level generator** — AI-assisted puzzle generation with uniqueness verification
- **Fog overlay on any move** — ghost/overlay of revealed final position on the main board (toggle)
- **Backend** — API + database for sharing puzzles, user progress, leaderboards
- **ES modules migration** — switch from script tags to modules with a local dev server
