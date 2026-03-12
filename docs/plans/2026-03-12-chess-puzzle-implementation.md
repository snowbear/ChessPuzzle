# Chess Puzzle Game — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a browser-based deduction chess puzzle game where players place pieces on a partially hidden starting position, play moves satisfying objective hints, and match a revealed final position.

**Architecture:** Single-page app with ES modules. cm-chessboard renders the interactive board and thumbnail. chess.js handles move validation and game state. Puzzle data loaded from static JSON. No backend.

**Tech Stack:** chess.js v1.x, cm-chessboard v8.x, vanilla JS (ES modules), HTML/CSS

---

### Task 0: Project Setup & Dependencies

**Files:**
- Create: `index.html`
- Create: `css/style.css`
- Create: `js/app.js`
- Create: `start.sh`
- Create: `start.bat`
- Create: `package.json`

**Step 1: Initialize npm and install dependencies**

```bash
cd /c/work/ChessPuzzle
npm init -y
npm install chess.js cm-chessboard
```

**Step 2: Create start scripts**

`start.sh`:
```bash
#!/bin/bash
npx http-server -p 8080 -o
```

`start.bat`:
```bat
npx http-server -p 8080 -o
```

**Step 3: Create index.html**

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Chess Puzzle</title>
    <link rel="stylesheet" href="node_modules/cm-chessboard/assets/chessboard.css"/>
    <link rel="stylesheet" href="node_modules/cm-chessboard/assets/extensions/markers/markers.css"/>
    <link rel="stylesheet" href="node_modules/cm-chessboard/assets/extensions/promotion-dialog/promotion-dialog.css"/>
    <link rel="stylesheet" href="css/style.css"/>
</head>
<body>
    <div id="app">
        <header id="puzzle-header">
            <h1 id="puzzle-title">Chess Puzzle</h1>
        </header>
        <main id="puzzle-layout">
            <aside id="piece-tray"></aside>
            <section id="board-container">
                <div id="main-board"></div>
            </section>
            <aside id="hints-panel">
                <h2>Hints</h2>
                <ul id="hints-list"></ul>
            </aside>
        </main>
        <footer id="controls">
            <div id="move-history"></div>
            <div id="navigation">
                <button id="btn-start">&lt;&lt;</button>
                <button id="btn-back">&lt;</button>
                <button id="btn-forward">&gt;</button>
                <button id="btn-end">&gt;&gt;</button>
                <button id="btn-submit">Submit</button>
            </div>
            <div id="thumbnail-container">
                <h3>Target Position</h3>
                <div id="thumbnail-board"></div>
            </div>
        </footer>
    </div>
    <script type="module" src="js/app.js"></script>
</body>
</html>
```

**Step 4: Create minimal css/style.css**

```css
* { margin: 0; padding: 0; box-sizing: border-box; }
body { font-family: system-ui, sans-serif; background: #1a1a2e; color: #eee; }

#app { max-width: 1200px; margin: 0 auto; padding: 1rem; }

#puzzle-header { text-align: center; margin-bottom: 1rem; }

#puzzle-layout {
    display: grid;
    grid-template-columns: 180px 1fr 220px;
    gap: 1rem;
    align-items: start;
}

#board-container { width: 100%; max-width: 560px; }

#piece-tray {
    background: #16213e;
    border-radius: 8px;
    padding: 0.75rem;
}
#piece-tray.disabled { opacity: 0.4; pointer-events: none; }

#hints-panel {
    background: #16213e;
    border-radius: 8px;
    padding: 0.75rem;
}
#hints-panel h2 { margin-bottom: 0.5rem; font-size: 1rem; }
#hints-list { list-style: none; }
#hints-list li { padding: 0.3rem 0; border-bottom: 1px solid #333; font-size: 0.85rem; }
#hints-list li.pass { color: #4ecca3; }
#hints-list li.fail { color: #e94560; }

#controls {
    margin-top: 1rem;
    display: flex;
    flex-wrap: wrap;
    gap: 1rem;
    align-items: start;
}

#move-history {
    flex: 1;
    background: #16213e;
    border-radius: 8px;
    padding: 0.75rem;
    min-height: 60px;
    font-family: monospace;
    font-size: 0.85rem;
}
#move-history .move { cursor: pointer; padding: 0.1rem 0.3rem; display: inline-block; }
#move-history .move:hover { background: #333; border-radius: 3px; }
#move-history .move.active { background: #4ecca3; color: #000; border-radius: 3px; }
#move-history .move.invalid { color: #e94560; text-decoration: line-through; }

#navigation { display: flex; gap: 0.5rem; align-items: center; }
#navigation button {
    padding: 0.4rem 0.8rem;
    background: #16213e;
    color: #eee;
    border: 1px solid #333;
    border-radius: 4px;
    cursor: pointer;
    font-size: 1rem;
}
#navigation button:hover { background: #333; }
#btn-submit {
    background: #4ecca3 !important;
    color: #000 !important;
    font-weight: bold;
}

#thumbnail-container {
    width: 180px;
}
#thumbnail-container h3 { font-size: 0.85rem; margin-bottom: 0.3rem; text-align: center; }
#thumbnail-board { width: 180px; height: 180px; }
```

**Step 5: Create minimal js/app.js to verify setup**

```javascript
import { Chess } from '../node_modules/chess.js/dist/chess.js'
import { Chessboard, COLOR } from '../node_modules/cm-chessboard/src/Chessboard.js'
import { FEN } from '../node_modules/cm-chessboard/src/model/Position.js'

const game = new Chess()
console.log('chess.js loaded, starting FEN:', game.fen())

const board = new Chessboard(document.getElementById('main-board'), {
    position: FEN.start,
    orientation: COLOR.white,
    assetsUrl: 'node_modules/cm-chessboard/assets/',
    style: {
        cssClass: 'default',
        showCoordinates: true,
    }
})

console.log('cm-chessboard loaded')
```

**Step 6: Test it**

Run: `npx http-server -p 8080 -o`
Expected: Browser opens, shows a chessboard with starting position, console logs confirm both libraries loaded.

**Step 7: Commit**

```bash
git init
git add .
git commit -m "chore: project setup with chess.js and cm-chessboard"
```

---

### Task 1: Puzzle Data Model & Loader

**Files:**
- Create: `js/puzzle.js`
- Create: `puzzles/puzzle-001.json`

**Step 1: Create a sample puzzle JSON**

`puzzles/puzzle-001.json` — a simple puzzle: White to play 2 half-moves (white then black). Starting position has a few open squares. One hint.

```json
{
    "metadata": {
        "id": "puzzle-001",
        "title": "Scholar's Setup",
        "author": "System",
        "difficulty": 1
    },
    "startPosition": {
        "fen": "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
        "squares": {
            "e2": "open",
            "d2": "open",
            "d1": "revealed",
            "e1": "revealed"
        }
    },
    "pieceConstraints": {
        "white": {
            "pawn": { "min": 1, "max": 2 }
        }
    },
    "halfMoveCount": 2,
    "revealedFinalPosition": {
        "e4": { "color": "white", "type": "pawn" }
    },
    "hints": [
        {
            "scope": { "halfMove": 1 },
            "constraints": { "piece": "pawn" },
            "text": "White moved a pawn on their first move"
        }
    ]
}
```

Note: The `startPosition.fen` gives the base FEN (revealed + default pieces). The `squares` map overrides square states — only squares listed here differ from the FEN. Squares not in the map are implicitly "revealed" (whatever is in the FEN). Squares marked "open" mean the FEN piece (if any) is removed and the player must decide what goes there. Squares marked "blocked" are forced empty.

**Step 2: Create js/puzzle.js**

```javascript
/**
 * Puzzle data model and loader.
 *
 * Square states in startPosition.squares:
 * - "revealed" (or absent from map): piece from FEN is fixed
 * - "open": player can place a piece here (FEN piece is stripped)
 * - "blocked": guaranteed empty, player cannot place
 */

export class Puzzle {
    constructor(data) {
        this.metadata = data.metadata
        this.startPosition = data.startPosition
        this.pieceConstraints = data.pieceConstraints || {}
        this.halfMoveCount = data.halfMoveCount
        this.revealedFinalPosition = data.revealedFinalPosition || {}
        this.hints = data.hints || []
    }

    /**
     * Returns the set of open squares (where player can place pieces).
     */
    getOpenSquares() {
        const open = []
        const squares = this.startPosition.squares || {}
        for (const [sq, state] of Object.entries(squares)) {
            if (state === 'open') open.push(sq)
        }
        return open
    }

    /**
     * Returns the set of blocked squares (forced empty).
     */
    getBlockedSquares() {
        const blocked = []
        const squares = this.startPosition.squares || {}
        for (const [sq, state] of Object.entries(squares)) {
            if (state === 'blocked') blocked.push(sq)
        }
        return blocked
    }

    /**
     * Returns the base FEN with open squares cleared.
     * This is the starting board the player sees (before placing).
     */
    getInitialFen() {
        return this.startPosition.fen
    }

    /**
     * Get min/max for a given color and piece type.
     * Returns { min: 0, max: 0 } if no constraint specified.
     */
    getConstraint(color, pieceType) {
        const colorConstraints = this.pieceConstraints[color]
        if (!colorConstraints) return { min: 0, max: 0 }
        return colorConstraints[pieceType] || { min: 0, max: 0 }
    }
}

/**
 * Load a puzzle from a JSON file path.
 */
export async function loadPuzzle(path) {
    const response = await fetch(path)
    if (!response.ok) throw new Error(`Failed to load puzzle: ${path}`)
    const data = await response.json()
    return new Puzzle(data)
}
```

**Step 3: Test puzzle loading in app.js**

Update `js/app.js` temporarily:
```javascript
import { loadPuzzle } from './puzzle.js'

const puzzle = await loadPuzzle('puzzles/puzzle-001.json')
console.log('Puzzle loaded:', puzzle.metadata.title)
console.log('Open squares:', puzzle.getOpenSquares())
console.log('Hints:', puzzle.hints.length)
```

Run: refresh browser
Expected: Console shows puzzle title, open squares, hint count.

**Step 4: Commit**

```bash
git add js/puzzle.js puzzles/puzzle-001.json
git commit -m "feat: puzzle data model and JSON loader"
```

---

### Task 2: Game State Manager (chess.js wrapper)

**Files:**
- Create: `js/game.js`

**Step 1: Create js/game.js**

This module wraps chess.js and manages the game state including placed pieces and move history.

```javascript
import { Chess } from '../node_modules/chess.js/dist/chess.js'

/**
 * Manages game state: piece placement on starting position + move sequence.
 * Wraps chess.js for legal move validation.
 */
export class GameState {
    constructor(puzzle) {
        this.puzzle = puzzle
        this.placedPieces = {}  // { square: { color: 'w', type: 'p' } }
        this.moves = []         // array of chess.js Move objects
        this.currentMoveIndex = -1  // -1 = at starting position (move 0)
        this._rebuildChess()
    }

    /**
     * Rebuild the chess.js instance from base FEN + placed pieces.
     * Called whenever placed pieces change.
     */
    _rebuildChess() {
        this.chess = new Chess(this.puzzle.getInitialFen())

        // Remove pieces on open squares
        for (const sq of this.puzzle.getOpenSquares()) {
            this.chess.remove(sq)
        }

        // Remove pieces on blocked squares
        for (const sq of this.puzzle.getBlockedSquares()) {
            this.chess.remove(sq)
        }

        // Place user-added pieces
        for (const [sq, piece] of Object.entries(this.placedPieces)) {
            this.chess.put(piece, sq)
        }
    }

    /**
     * Place a piece on an open square. Returns true if successful.
     */
    placePiece(square, piece) {
        if (!this.puzzle.getOpenSquares().includes(square)) return false
        this.placedPieces[square] = piece
        this._rebuildChess()
        this._revalidateMoves()
        return true
    }

    /**
     * Remove a player-placed piece. Returns true if there was one.
     */
    removePlacedPiece(square) {
        if (!this.placedPieces[square]) return false
        delete this.placedPieces[square]
        this._rebuildChess()
        this._revalidateMoves()
        return true
    }

    /**
     * Get the chess.js instance at the current move index.
     * Replays moves from the start up to currentMoveIndex.
     */
    getChessAtCurrentMove() {
        const chess = new Chess(this.chess.fen(), { skipValidation: true })
        // chess starts at the rebuilt base position (move 0)
        // We need to actually start from the rebuilt chess and replay
        // But chess.js doesn't let us clone easily, so rebuild + replay
        const freshChess = new Chess()
        freshChess.load(this.chess.fen())

        for (let i = 0; i <= this.currentMoveIndex && i < this.moves.length; i++) {
            const move = this.moves[i]
            if (!move.valid) break
            try {
                freshChess.move({ from: move.from, to: move.to, promotion: move.promotion })
            } catch {
                break
            }
        }
        return freshChess
    }

    /**
     * Get the FEN at the current move index.
     */
    getCurrentFen() {
        return this.getChessAtCurrentMove().fen()
    }

    /**
     * Try to make a move from the current position. Returns the Move object or null.
     */
    makeMove(from, to, promotion) {
        if (this.currentMoveIndex !== this.moves.length - 1 &&
            this.moves.length > 0) {
            // If we're not at the end, truncate future moves
            this.moves = this.moves.slice(0, this.currentMoveIndex + 1)
        }

        const chess = this.getChessAtCurrentMove()
        try {
            const moveObj = chess.move({ from, to, promotion })
            this.moves.push({ ...moveObj, valid: true })
            this.currentMoveIndex = this.moves.length - 1
            return moveObj
        } catch {
            return null
        }
    }

    /**
     * Navigate to a specific move index. -1 = starting position.
     */
    goToMove(index) {
        if (index < -1 || index >= this.moves.length) return false
        this.currentMoveIndex = index
        return true
    }

    /**
     * Check if we're at the starting position (move 0 / placement phase).
     */
    isAtStartPosition() {
        return this.currentMoveIndex === -1
    }

    /**
     * Check if all half-moves have been played.
     */
    isComplete() {
        const validMoves = this.moves.filter(m => m.valid)
        return validMoves.length >= this.puzzle.halfMoveCount
    }

    /**
     * Re-validate existing moves after a placement change.
     * Marks moves as valid/invalid without removing them.
     */
    _revalidateMoves() {
        const chess = new Chess()
        chess.load(this.chess.fen())

        for (const move of this.moves) {
            try {
                chess.move({ from: move.from, to: move.to, promotion: move.promotion })
                move.valid = true
            } catch {
                move.valid = false
                // All subsequent moves are also invalid
                break
            }
        }
        // Mark remaining moves as invalid too
        let foundInvalid = false
        for (const move of this.moves) {
            if (!move.valid) foundInvalid = true
            if (foundInvalid) move.valid = false
        }
    }

    /**
     * Get the final board position (after all valid moves).
     */
    getFinalFen() {
        const chess = new Chess()
        chess.load(this.chess.fen())
        for (const move of this.moves) {
            if (!move.valid) break
            try {
                chess.move({ from: move.from, to: move.to, promotion: move.promotion })
            } catch {
                break
            }
        }
        return chess.fen()
    }

    /**
     * Get the count of each placed piece type per color.
     */
    getPlacedPieceCounts() {
        const counts = { white: {}, black: {} }
        for (const piece of Object.values(this.placedPieces)) {
            const color = piece.color === 'w' ? 'white' : 'black'
            const type = this._pieceTypeName(piece.type)
            counts[color][type] = (counts[color][type] || 0) + 1
        }
        return counts
    }

    _pieceTypeName(shortType) {
        const map = { p: 'pawn', n: 'knight', b: 'bishop', r: 'rook', q: 'queen', k: 'king' }
        return map[shortType] || shortType
    }
}
```

**Step 2: Wire into app.js for basic test**

```javascript
import { loadPuzzle } from './puzzle.js'
import { GameState } from './game.js'

const puzzle = await loadPuzzle('puzzles/puzzle-001.json')
const gameState = new GameState(puzzle)
console.log('Starting FEN:', gameState.getCurrentFen())
console.log('Is at start:', gameState.isAtStartPosition())
```

Run: refresh browser
Expected: Console shows FEN with open squares cleared, isAtStartPosition is true.

**Step 3: Commit**

```bash
git add js/game.js
git commit -m "feat: game state manager wrapping chess.js"
```

---

### Task 3: Board Rendering (cm-chessboard wrapper)

**Files:**
- Create: `js/board.js`

**Step 1: Create js/board.js**

```javascript
import { Chessboard, COLOR, INPUT_EVENT_TYPE, PIECE } from
    '../node_modules/cm-chessboard/src/Chessboard.js'
import { FEN } from '../node_modules/cm-chessboard/src/model/Position.js'
import { Markers, MARKER_TYPE } from
    '../node_modules/cm-chessboard/src/extensions/markers/Markers.js'
import { PromotionDialog, PROMOTION_DIALOG_RESULT_TYPE } from
    '../node_modules/cm-chessboard/src/extensions/promotion-dialog/PromotionDialog.js'

const ASSETS_URL = 'node_modules/cm-chessboard/assets/'

/**
 * Maps chess.js piece format to cm-chessboard PIECE constants.
 * chess.js: { color: 'w'|'b', type: 'p'|'n'|'b'|'r'|'q'|'k' }
 * cm-chessboard: 'wp', 'bn', etc.
 */
function toCmPiece(color, type) {
    return color + type  // e.g. 'w' + 'p' = 'wp'
}

/**
 * Wraps cm-chessboard for the main interactive board.
 */
export class BoardView {
    constructor(elementId, options = {}) {
        this.elementId = elementId
        this.onPieceDrop = options.onPieceDrop || null      // (from, to) => bool
        this.onPiecePickup = options.onPiecePickup || null  // (square) => bool
        this.onSquareClick = options.onSquareClick || null   // (square) => void
        this.board = null
    }

    async init() {
        this.board = new Chessboard(document.getElementById(this.elementId), {
            position: FEN.empty,
            orientation: COLOR.white,
            assetsUrl: ASSETS_URL,
            style: {
                cssClass: 'default',
                showCoordinates: true,
            },
            extensions: [
                { class: Markers },
                { class: PromotionDialog }
            ]
        })
        // Small delay to let the board render
        await new Promise(r => setTimeout(r, 100))
    }

    /**
     * Set position from FEN string.
     */
    setPosition(fen, animated = true) {
        this.board.setPosition(fen, animated)
    }

    /**
     * Enable move input with a handler.
     */
    enableMoveInput(handler) {
        this.board.enableMoveInput((event) => {
            switch (event.type) {
                case INPUT_EVENT_TYPE.moveInputStarted:
                    if (this.onPiecePickup) {
                        return this.onPiecePickup(event.squareFrom)
                    }
                    return true
                case INPUT_EVENT_TYPE.validateMoveInput:
                    if (this.onPieceDrop) {
                        return this.onPieceDrop(event.squareFrom, event.squareTo)
                    }
                    return true
                case INPUT_EVENT_TYPE.moveInputFinished:
                    break
            }
        })
    }

    disableMoveInput() {
        this.board.disableMoveInput()
    }

    /**
     * Add a marker to a square.
     */
    addMarker(square, type = MARKER_TYPE.frame) {
        this.board.addMarker(type, square)
    }

    /**
     * Remove markers from a square (or all if no args).
     */
    removeMarkers(square, type) {
        this.board.removeMarkers(type, square)
    }

    /**
     * Mark open squares with a visual indicator.
     */
    markOpenSquares(squares) {
        for (const sq of squares) {
            this.board.addMarker(MARKER_TYPE.frame, sq)
        }
    }

    /**
     * Mark blocked squares with a visual indicator.
     */
    markBlockedSquares(squares) {
        for (const sq of squares) {
            this.board.addMarker(MARKER_TYPE.frameDanger, sq)
        }
    }

    /**
     * Clear all markers.
     */
    clearMarkers() {
        this.board.removeMarkers()
    }

    /**
     * Place a single piece on the board.
     */
    setPiece(square, color, type) {
        this.board.setPiece(square, toCmPiece(color, type), true)
    }

    /**
     * Remove a piece from a square.
     */
    removePiece(square) {
        this.board.setPiece(square, null)
    }

    /**
     * Show promotion dialog and return selected piece.
     */
    showPromotionDialog(square, color) {
        return new Promise((resolve) => {
            this.board.showPromotionDialog(square, color === 'w' ? COLOR.white : COLOR.black, (result) => {
                if (result.type === PROMOTION_DIALOG_RESULT_TYPE.pieceSelected) {
                    resolve(result.piece[1]) // e.g. 'wq' -> 'q'
                } else {
                    resolve(null)
                }
            })
        })
    }
}

/**
 * Wraps cm-chessboard for the small read-only thumbnail board.
 */
export class ThumbnailView {
    constructor(elementId) {
        this.elementId = elementId
        this.board = null
    }

    async init() {
        this.board = new Chessboard(document.getElementById(this.elementId), {
            position: FEN.empty,
            orientation: COLOR.white,
            assetsUrl: ASSETS_URL,
            style: {
                cssClass: 'default',
                showCoordinates: false,
            },
            extensions: [{ class: Markers }]
        })
        await new Promise(r => setTimeout(r, 100))
    }

    /**
     * Show only the revealed final position pieces.
     * revealedFinalPosition: { "e4": { color: "white", type: "pawn" }, ... }
     */
    showRevealedPosition(revealedFinalPosition) {
        // Start with empty board
        this.board.setPosition(FEN.empty, false)
        const colorMap = { white: 'w', black: 'b' }
        const typeMap = { pawn: 'p', knight: 'n', bishop: 'b', rook: 'r', queen: 'q', king: 'k' }
        for (const [sq, piece] of Object.entries(revealedFinalPosition)) {
            const cmPiece = toCmPiece(colorMap[piece.color], typeMap[piece.type])
            this.board.setPiece(sq, cmPiece, false)
        }
    }

    /**
     * Highlight mismatched squares (after submit).
     */
    markMismatches(squares) {
        for (const sq of squares) {
            this.board.addMarker(MARKER_TYPE.frameDanger, sq)
        }
    }

    clearMarkers() {
        this.board.removeMarkers()
    }
}
```

**Step 2: Wire boards into app.js**

```javascript
import { loadPuzzle } from './puzzle.js'
import { GameState } from './game.js'
import { BoardView, ThumbnailView } from './board.js'

const puzzle = await loadPuzzle('puzzles/puzzle-001.json')
const gameState = new GameState(puzzle)

const boardView = new BoardView('main-board')
await boardView.init()
boardView.setPosition(gameState.getCurrentFen(), false)
boardView.markOpenSquares(puzzle.getOpenSquares())
boardView.markBlockedSquares(puzzle.getBlockedSquares())

const thumbnail = new ThumbnailView('thumbnail-board')
await thumbnail.init()
thumbnail.showRevealedPosition(puzzle.revealedFinalPosition)

console.log('Boards initialized')
```

Run: refresh browser
Expected: Main board shows starting position with open squares framed, thumbnail shows revealed final pieces.

**Step 3: Commit**

```bash
git add js/board.js
git commit -m "feat: board rendering with cm-chessboard wrapper"
```

---

### Task 4: Piece Tray UI

**Files:**
- Create: `js/pieceTray.js`

**Step 1: Create js/pieceTray.js**

```javascript
/**
 * Piece tray — shows available pieces for placement.
 * Always visible, disabled when not at move 0.
 */
export class PieceTray {
    constructor(elementId, puzzle, gameState) {
        this.element = document.getElementById(elementId)
        this.puzzle = puzzle
        this.gameState = gameState
        this.onPieceSelected = null  // callback: (color, type) => void
        this.selectedPiece = null    // { color, type } or null
        this.enabled = true
    }

    render() {
        this.element.innerHTML = ''
        const colors = ['white', 'black']
        const types = ['king', 'queen', 'rook', 'bishop', 'knight', 'pawn']
        const placedCounts = this.gameState.getPlacedPieceCounts()

        for (const color of colors) {
            const colorConstraints = this.puzzle.pieceConstraints[color]
            if (!colorConstraints) continue

            const section = document.createElement('div')
            section.className = 'tray-color-section'

            const heading = document.createElement('h3')
            heading.textContent = color.charAt(0).toUpperCase() + color.slice(1)
            heading.style.fontSize = '0.85rem'
            heading.style.marginBottom = '0.3rem'
            section.appendChild(heading)

            for (const type of types) {
                const constraint = colorConstraints[type]
                if (!constraint || constraint.max === 0) continue

                const placed = (placedCounts[color] && placedCounts[color][type]) || 0
                const remaining = constraint.max - placed
                const needMore = Math.max(0, constraint.min - placed)

                const item = document.createElement('div')
                item.className = 'tray-item'
                if (remaining <= 0) item.classList.add('exhausted')
                if (this.selectedPiece &&
                    this.selectedPiece.color === color &&
                    this.selectedPiece.type === type) {
                    item.classList.add('selected')
                }

                const colorShort = color === 'white' ? 'w' : 'b'
                const typeShort = { king: 'k', queen: 'q', rook: 'r', bishop: 'b', knight: 'n', pawn: 'p' }[type]

                // Use a text representation for now; can replace with SVG pieces later
                const pieceSymbols = {
                    wk: '\u2654', wq: '\u2655', wr: '\u2656', wb: '\u2657', wn: '\u2658', wp: '\u2659',
                    bk: '\u265A', bq: '\u265B', br: '\u265C', bb: '\u265D', bn: '\u265E', bp: '\u265F'
                }

                item.innerHTML = `
                    <span class="tray-piece">${pieceSymbols[colorShort + typeShort]}</span>
                    <span class="tray-count">${placed}/${constraint.max} (min: ${constraint.min})</span>
                `

                if (remaining > 0) {
                    item.addEventListener('click', () => {
                        this.selectedPiece = { color: colorShort, type: typeShort }
                        if (this.onPieceSelected) {
                            this.onPieceSelected(colorShort, typeShort)
                        }
                        this.render()
                    })
                }

                section.appendChild(item)
            }

            this.element.appendChild(section)
        }
    }

    setEnabled(enabled) {
        this.enabled = enabled
        if (enabled) {
            this.element.classList.remove('disabled')
        } else {
            this.element.classList.add('disabled')
        }
    }

    clearSelection() {
        this.selectedPiece = null
        this.render()
    }
}
```

Add to `css/style.css`:
```css
.tray-color-section { margin-bottom: 0.75rem; }
.tray-item {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.3rem;
    cursor: pointer;
    border-radius: 4px;
}
.tray-item:hover { background: #333; }
.tray-item.selected { background: #4ecca3; color: #000; }
.tray-item.exhausted { opacity: 0.3; cursor: default; }
.tray-piece { font-size: 1.5rem; }
.tray-count { font-size: 0.75rem; }
```

**Step 2: Commit**

```bash
git add js/pieceTray.js css/style.css
git commit -m "feat: piece tray UI component"
```

---

### Task 5: Move History UI

**Files:**
- Create: `js/moveHistory.js`

**Step 1: Create js/moveHistory.js**

```javascript
/**
 * Move history display with back/forward navigation.
 */
export class MoveHistory {
    constructor(elementId, gameState) {
        this.element = document.getElementById(elementId)
        this.gameState = gameState
        this.onMoveSelected = null  // callback: (moveIndex) => void
    }

    render() {
        this.element.innerHTML = ''

        // "Start" position entry
        const startSpan = document.createElement('span')
        startSpan.className = 'move'
        if (this.gameState.currentMoveIndex === -1) startSpan.classList.add('active')
        startSpan.textContent = 'Start'
        startSpan.addEventListener('click', () => {
            if (this.onMoveSelected) this.onMoveSelected(-1)
        })
        this.element.appendChild(startSpan)

        for (let i = 0; i < this.gameState.moves.length; i++) {
            const move = this.gameState.moves[i]
            const span = document.createElement('span')
            span.className = 'move'
            if (i === this.gameState.currentMoveIndex) span.classList.add('active')
            if (!move.valid) span.classList.add('invalid')

            // Show move number for white moves
            const moveNum = Math.floor(i / 2) + 1
            const isWhite = i % 2 === 0
            const prefix = isWhite ? `${moveNum}. ` : ''

            span.textContent = ` ${prefix}${move.san}`
            span.addEventListener('click', () => {
                if (this.onMoveSelected) this.onMoveSelected(i)
            })
            this.element.appendChild(span)
        }
    }
}
```

**Step 2: Commit**

```bash
git add js/moveHistory.js
git commit -m "feat: move history UI component"
```

---

### Task 6: Hint Evaluation Engine

**Files:**
- Create: `js/hints.js`

**Step 1: Create js/hints.js**

```javascript
import { Chess } from '../node_modules/chess.js/dist/chess.js'

/**
 * Evaluates hints against a move sequence.
 *
 * Each hint has:
 * - scope: { halfMove: N } | { halfMoveRange: [start, end] } | "any" | "final"
 * - constraints: object with optional fields
 * - text: human-readable description
 */

/**
 * Map from long piece name to chess.js short code.
 */
const PIECE_SHORT = { pawn: 'p', knight: 'n', bishop: 'b', rook: 'r', queen: 'q', king: 'k' }

/**
 * Check if a single move satisfies all constraints.
 * move: chess.js verbose Move object
 * constraints: hint constraints object
 */
function moveMatchesConstraints(move, constraints, chess) {
    if (constraints.color !== undefined) {
        const expected = constraints.color === 'white' ? 'w' : (constraints.color === 'black' ? 'b' : constraints.color)
        if (move.color !== expected) return false
    }
    if (constraints.piece !== undefined) {
        const expected = PIECE_SHORT[constraints.piece] || constraints.piece
        if (move.piece !== expected) return false
    }
    if (constraints.capturedPiece !== undefined) {
        const expected = PIECE_SHORT[constraints.capturedPiece] || constraints.capturedPiece
        if (move.captured !== expected) return false
    }
    if (constraints.toSquare !== undefined && move.to !== constraints.toSquare) return false
    if (constraints.fromSquare !== undefined && move.from !== constraints.fromSquare) return false
    if (constraints.toRank !== undefined && parseInt(move.to[1]) !== constraints.toRank) return false
    if (constraints.toFile !== undefined && move.to[0] !== constraints.toFile) return false
    if (constraints.fromRank !== undefined && parseInt(move.from[1]) !== constraints.fromRank) return false
    if (constraints.fromFile !== undefined && move.from[0] !== constraints.fromFile) return false

    if (constraints.isCheck === true) {
        if (!chess.inCheck()) return false
    }
    if (constraints.isCapture === true) {
        if (!move.captured) return false
    }
    if (constraints.isCapture === false) {
        if (move.captured) return false
    }
    if (constraints.isCastle !== undefined) {
        const isKingside = move.flags && move.flags.includes('k')
        const isQueenside = move.flags && move.flags.includes('q')
        if (constraints.isCastle === true && !isKingside && !isQueenside) return false
        if (constraints.isCastle === 'kingside' && !isKingside) return false
        if (constraints.isCastle === 'queenside' && !isQueenside) return false
    }
    if (constraints.isEnPassant === true) {
        if (!move.flags || !move.flags.includes('e')) return false
    }
    if (constraints.isPromotion === true) {
        if (!move.promotion) return false
    }
    if (constraints.promotionPiece !== undefined) {
        const expected = PIECE_SHORT[constraints.promotionPiece] || constraints.promotionPiece
        if (move.promotion !== expected) return false
    }

    return true
}

/**
 * Check final-position-only constraints.
 */
function finalMatchesConstraints(constraints, chess) {
    if (constraints.isCheckmate === true && !chess.isCheckmate()) return false
    if (constraints.isStalemate === true && !chess.isStalemate()) return false
    if (constraints.isCheck === true && !chess.inCheck()) return false
    return true
}

/**
 * Evaluate a single hint against the full move sequence.
 * baseFen: the starting FEN (after piece placement)
 * moves: array of move objects with { from, to, promotion, valid }
 * Returns: { pass: boolean, hint: the original hint }
 */
export function evaluateHint(hint, baseFen, moves) {
    const validMoves = moves.filter(m => m.valid)

    if (hint.scope === 'final') {
        // Replay all moves, check final position
        const chess = new Chess()
        chess.load(baseFen)
        for (const m of validMoves) {
            try { chess.move({ from: m.from, to: m.to, promotion: m.promotion }) }
            catch { return { pass: false, hint } }
        }
        return { pass: finalMatchesConstraints(hint.constraints, chess), hint }
    }

    if (hint.scope === 'any') {
        // At least one move must match
        const chess = new Chess()
        chess.load(baseFen)
        for (const m of validMoves) {
            try {
                const moveObj = chess.move({ from: m.from, to: m.to, promotion: m.promotion })
                if (moveMatchesConstraints(moveObj, hint.constraints, chess)) {
                    return { pass: true, hint }
                }
            } catch { return { pass: false, hint } }
        }
        return { pass: false, hint }
    }

    if (hint.scope.halfMove !== undefined) {
        const idx = hint.scope.halfMove - 1  // 1-indexed to 0-indexed
        if (idx < 0 || idx >= validMoves.length) return { pass: false, hint }

        const chess = new Chess()
        chess.load(baseFen)
        for (let i = 0; i <= idx; i++) {
            try {
                const moveObj = chess.move({
                    from: validMoves[i].from,
                    to: validMoves[i].to,
                    promotion: validMoves[i].promotion
                })
                if (i === idx) {
                    return { pass: moveMatchesConstraints(moveObj, hint.constraints, chess), hint }
                }
            } catch { return { pass: false, hint } }
        }
        return { pass: false, hint }
    }

    if (hint.scope.halfMoveRange !== undefined) {
        const [start, end] = hint.scope.halfMoveRange
        const chess = new Chess()
        chess.load(baseFen)
        for (let i = 0; i < validMoves.length && i < end; i++) {
            try {
                const moveObj = chess.move({
                    from: validMoves[i].from,
                    to: validMoves[i].to,
                    promotion: validMoves[i].promotion
                })
                if (i >= start - 1 && i <= end - 1) {
                    if (moveMatchesConstraints(moveObj, hint.constraints, chess)) {
                        return { pass: true, hint }
                    }
                }
            } catch { return { pass: false, hint } }
        }
        return { pass: false, hint }
    }

    // Unknown scope type
    return { pass: false, hint }
}

/**
 * Evaluate all hints. Returns array of { pass, hint }.
 */
export function evaluateAllHints(hints, baseFen, moves) {
    return hints.map(hint => evaluateHint(hint, baseFen, moves))
}
```

**Step 2: Commit**

```bash
git add js/hints.js
git commit -m "feat: hint evaluation engine with composable constraints"
```

---

### Task 7: Validation (Submit Logic)

**Files:**
- Create: `js/validation.js`

**Step 1: Create js/validation.js**

```javascript
import { Chess } from '../node_modules/chess.js/dist/chess.js'
import { evaluateAllHints } from './hints.js'

const PIECE_SHORT = { pawn: 'p', knight: 'n', bishop: 'b', rook: 'r', queen: 'q', king: 'k' }

/**
 * Validate the player's solution.
 *
 * Returns {
 *   success: boolean,
 *   hintResults: [{ pass, hint }],
 *   positionMismatches: [square]  // squares where final pos doesn't match revealed
 * }
 */
export function validateSolution(gameState) {
    const puzzle = gameState.puzzle
    const baseFen = gameState.chess.fen()
    const moves = gameState.moves

    // 1. Check revealed final position
    const finalChess = new Chess()
    finalChess.load(baseFen)
    const validMoves = moves.filter(m => m.valid)
    for (const m of validMoves) {
        try {
            finalChess.move({ from: m.from, to: m.to, promotion: m.promotion })
        } catch {
            break
        }
    }

    const positionMismatches = []
    for (const [sq, expected] of Object.entries(puzzle.revealedFinalPosition)) {
        const actual = finalChess.get(sq)
        const expectedColor = expected.color === 'white' ? 'w' : 'b'
        const expectedType = PIECE_SHORT[expected.type] || expected.type

        if (!actual || actual.color !== expectedColor || actual.type !== expectedType) {
            positionMismatches.push(sq)
        }
    }

    // 2. Check all hints
    const hintResults = evaluateAllHints(puzzle.hints, baseFen, moves)

    // 3. Overall success
    const success = positionMismatches.length === 0 && hintResults.every(r => r.pass)

    return { success, hintResults, positionMismatches }
}
```

**Step 2: Commit**

```bash
git add js/validation.js
git commit -m "feat: solution validation (final position + hints)"
```

---

### Task 8: Wire Everything Together in app.js

**Files:**
- Modify: `js/app.js` (full rewrite)

**Step 1: Write the main app.js**

```javascript
import { loadPuzzle } from './puzzle.js'
import { GameState } from './game.js'
import { BoardView, ThumbnailView } from './board.js'
import { PieceTray } from './pieceTray.js'
import { MoveHistory } from './moveHistory.js'
import { validateSolution } from './validation.js'

// --- Init ---

const puzzle = await loadPuzzle('puzzles/puzzle-001.json')
const gameState = new GameState(puzzle)

// Board
const boardView = new BoardView('main-board', {
    onPiecePickup: handlePiecePickup,
    onPieceDrop: handlePieceDrop,
})
await boardView.init()

// Thumbnail
const thumbnail = new ThumbnailView('thumbnail-board')
await thumbnail.init()
thumbnail.showRevealedPosition(puzzle.revealedFinalPosition)

// Piece tray
const pieceTray = new PieceTray('piece-tray', puzzle, gameState)
pieceTray.onPieceSelected = (color, type) => {
    // Selection is tracked in pieceTray.selectedPiece
}

// Move history
const moveHistory = new MoveHistory('move-history', gameState)
moveHistory.onMoveSelected = (index) => {
    gameState.goToMove(index)
    refreshBoard()
}

// Hints
renderHints()

// Navigation buttons
document.getElementById('btn-start').addEventListener('click', () => {
    gameState.goToMove(-1)
    refreshBoard()
})
document.getElementById('btn-back').addEventListener('click', () => {
    gameState.goToMove(gameState.currentMoveIndex - 1)
    refreshBoard()
})
document.getElementById('btn-forward').addEventListener('click', () => {
    const next = gameState.currentMoveIndex + 1
    if (next < gameState.moves.length) {
        gameState.goToMove(next)
        refreshBoard()
    }
})
document.getElementById('btn-end').addEventListener('click', () => {
    gameState.goToMove(gameState.moves.length - 1)
    refreshBoard()
})

// Submit
document.getElementById('btn-submit').addEventListener('click', handleSubmit)

// Initial render
refreshBoard()

// --- Handlers ---

function handlePiecePickup(square) {
    if (gameState.isAtStartPosition()) {
        // In placement phase: allow picking up player-placed pieces to remove them
        if (gameState.placedPieces[square]) {
            gameState.removePlacedPiece(square)
            refreshBoard()
            return false  // cancel the drag (piece is removed)
        }
        // Don't allow picking up revealed pieces
        return false
    } else {
        // In play phase: allow normal piece movement
        return true
    }
}

function handlePieceDrop(from, to) {
    if (gameState.isAtStartPosition()) {
        // Placement phase: place selected piece from tray onto open square
        if (pieceTray.selectedPiece && puzzle.getOpenSquares().includes(to)) {
            const { color, type } = pieceTray.selectedPiece
            const constraint = puzzle.getConstraint(
                color === 'w' ? 'white' : 'black',
                { p: 'pawn', n: 'knight', b: 'bishop', r: 'rook', q: 'queen', k: 'king' }[type]
            )
            const counts = gameState.getPlacedPieceCounts()
            const colorName = color === 'w' ? 'white' : 'black'
            const typeName = { p: 'pawn', n: 'knight', b: 'bishop', r: 'rook', q: 'queen', k: 'king' }[type]
            const placed = (counts[colorName] && counts[colorName][typeName]) || 0

            if (placed < constraint.max) {
                // Remove any existing placed piece on this square
                gameState.removePlacedPiece(to)
                gameState.placePiece(to, { color, type })
                refreshBoard()
            }
        }
        return false  // handled manually
    } else {
        // Play phase: try to make a legal move
        const chess = gameState.getChessAtCurrentMove()
        const piece = chess.get(from)
        if (!piece) return false

        // Check for promotion
        const isPromotion = piece.type === 'p' &&
            ((piece.color === 'w' && to[1] === '8') || (piece.color === 'b' && to[1] === '1'))

        if (isPromotion) {
            // Show promotion dialog asynchronously
            boardView.showPromotionDialog(to, piece.color).then(promotionPiece => {
                if (promotionPiece) {
                    const move = gameState.makeMove(from, to, promotionPiece)
                    if (move) refreshBoard()
                }
                boardView.setPosition(gameState.getCurrentFen(), false)
            })
            return false  // handled async
        }

        const move = gameState.makeMove(from, to)
        if (move) {
            refreshBoard()
            return true
        }
        return false
    }
}

function handleSubmit() {
    if (!gameState.isComplete()) {
        alert(`Play all ${puzzle.halfMoveCount} half-moves before submitting.`)
        return
    }

    const result = validateSolution(gameState)

    // Update hint display
    const hintItems = document.querySelectorAll('#hints-list li')
    result.hintResults.forEach((hr, i) => {
        if (hintItems[i]) {
            hintItems[i].classList.remove('pass', 'fail')
            hintItems[i].classList.add(hr.pass ? 'pass' : 'fail')
        }
    })

    // Update thumbnail with mismatches
    thumbnail.clearMarkers()
    for (const sq of result.positionMismatches) {
        thumbnail.markMismatches([sq])
    }

    if (result.success) {
        setTimeout(() => alert('Congratulations! Puzzle solved!'), 100)
    }
}

// --- Rendering ---

function refreshBoard() {
    boardView.setPosition(gameState.getCurrentFen(), true)
    boardView.clearMarkers()

    // Show square markers at starting position
    if (gameState.isAtStartPosition()) {
        boardView.markOpenSquares(puzzle.getOpenSquares().filter(sq => !gameState.placedPieces[sq]))
        boardView.markBlockedSquares(puzzle.getBlockedSquares())
    }

    // Update piece tray enabled state
    pieceTray.setEnabled(gameState.isAtStartPosition())
    pieceTray.render()

    // Update move history
    moveHistory.render()

    // Enable/disable move input
    boardView.enableMoveInput()
}

function renderHints() {
    const list = document.getElementById('hints-list')
    list.innerHTML = ''
    for (const hint of puzzle.hints) {
        const li = document.createElement('li')
        li.textContent = hint.text
        list.appendChild(li)
    }
}
```

**Step 2: Test the full flow**

Run: `npx http-server -p 8080 -o`
Expected:
- Board renders with starting position
- Open squares have frame markers
- Piece tray shows available pieces
- Clicking a piece in tray then clicking an open square places it
- Clicking a placed piece removes it
- Navigating forward from move 0 enters play mode
- Legal chess moves work, illegal moves snap back
- Move history shows moves, clickable
- Submit validates and shows results

**Step 3: Create a better sample puzzle**

Update `puzzles/puzzle-001.json` with a more interesting puzzle after testing the basic flow works.

**Step 4: Commit**

```bash
git add js/app.js puzzles/puzzle-001.json
git commit -m "feat: wire all components together in main app"
```

---

### Task 9: Polish & Sample Puzzle

**Files:**
- Modify: `puzzles/puzzle-001.json`
- Modify: `css/style.css`

**Step 1: Create a proper sample puzzle**

Design a small, solvable puzzle: 2-4 half-moves, 1-2 open squares, 2-3 hints. The revealed final position and hints should uniquely determine the solution.

**Step 2: Polish CSS**

- Ensure responsive layout works at common screen sizes
- Style the congratulations state
- Make disabled piece tray visually clear but not distracting

**Step 3: Test end-to-end**

Play through the puzzle manually in the browser. Verify:
- Piece placement works and respects constraints
- Move validation works
- Navigation works (back to placement, forward through moves)
- Changing placement invalidates affected moves (red highlight)
- Submit correctly evaluates hints and final position
- Success message appears on correct solution

**Step 4: Commit**

```bash
git add .
git commit -m "feat: polished sample puzzle and styling"
```

---

## Execution Notes

- Tasks 0-3 must be sequential (each builds on the previous).
- Tasks 4, 5, 6 can be done in parallel (independent components).
- Task 7 depends on Task 6 (uses hints.js).
- Task 8 depends on all previous tasks.
- Task 9 depends on Task 8.

When testing cm-chessboard, check that:
1. The import paths resolve correctly (node_modules paths)
2. The assets URL is correct for piece sprites
3. The board renders without console errors

The chess.js import path may need adjustment depending on the package's dist structure. Check `node_modules/chess.js/` for the actual entry point file.
