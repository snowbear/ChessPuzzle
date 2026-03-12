import { loadPuzzle } from './puzzle.js'
import { GameState } from './game.js'
import { BoardView, ThumbnailView } from './board.js'
import { PieceTray } from './pieceTray.js'
import { MoveHistory } from './moveHistory.js'
import { validateSolution } from './validation.js'

// --- Init ---

const puzzle = await loadPuzzle('puzzles/puzzle-001.json')
const gameState = new GameState(puzzle)

// Update page title
document.getElementById('puzzle-title').textContent = puzzle.metadata.title

// Board
const boardView = new BoardView('main-board', {
    onPiecePickup: handlePiecePickup,
    onPieceDrop: handlePieceDrop,
    onSquareClick: handleSquareClick,
})
await boardView.init()

// Thumbnail
const thumbnail = new ThumbnailView('thumbnail-board')
await thumbnail.init()
thumbnail.showRevealedPosition(puzzle.revealedFinalPosition)

// Piece tray
const pieceTray = new PieceTray('piece-tray', puzzle, gameState)

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
        // If a tray piece is selected, clicking a placed piece removes it
        if (pieceTray.selectedPiece && gameState.placedPieces[square]) {
            gameState.removePlacedPiece(square)
            refreshBoard()
            return false
        }
        // If no tray piece selected, allow normal piece movement (to make moves)
        if (!pieceTray.selectedPiece) {
            return true
        }
        // Tray piece selected but clicking a revealed piece — ignore
        return false
    } else {
        // In play phase: allow normal piece movement
        return true
    }
}

/**
 * Handle clicks on empty squares (for piece placement from tray).
 * cm-chessboard's move input only fires for squares with pieces;
 * this handler covers clicking empty open squares during placement.
 */
function handleSquareClick(square) {
    if (!gameState.isAtStartPosition()) return
    if (!pieceTray.selectedPiece) return
    if (!puzzle.getOpenSquares().includes(square)) return

    const { color, type } = pieceTray.selectedPiece
    const typeName = { p: 'pawn', n: 'knight', b: 'bishop', r: 'rook', q: 'queen', k: 'king' }[type]
    const colorName = color === 'w' ? 'white' : 'black'
    const constraint = puzzle.getConstraint(colorName, typeName)
    const counts = gameState.getPlacedPieceCounts()
    const placed = (counts[colorName] && counts[colorName][typeName]) || 0

    if (placed < constraint.max) {
        gameState.removePlacedPiece(square)
        gameState.placePiece(square, { color, type })
        refreshBoard()
    }
}

function handlePieceDrop(from, to) {
    if (gameState.isAtStartPosition() && pieceTray.selectedPiece) {
        // Placement mode: place selected tray piece onto open square
        if (puzzle.getOpenSquares().includes(to)) {
            const { color, type } = pieceTray.selectedPiece
            const typeName = { p: 'pawn', n: 'knight', b: 'bishop', r: 'rook', q: 'queen', k: 'king' }[type]
            const colorName = color === 'w' ? 'white' : 'black'
            const constraint = puzzle.getConstraint(colorName, typeName)
            const counts = gameState.getPlacedPieceCounts()
            const placed = (counts[colorName] && counts[colorName][typeName]) || 0

            if (placed < constraint.max) {
                gameState.removePlacedPiece(to)
                gameState.placePiece(to, { color, type })
                refreshBoard()
            }
        }
        return false
    } else {
        // Play phase (or start position with no tray piece selected): make a move
        // Play phase: try to make a legal move
        const chess = gameState.getChessAtCurrentMove()
        const piece = chess.get(from)
        if (!piece) return false

        // Check for promotion
        const isPromotion = piece.type === 'p' &&
            ((piece.color === 'w' && to[1] === '8') || (piece.color === 'b' && to[1] === '1'))

        if (isPromotion) {
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
    // No animation during placement to avoid flicker when placing pieces
    const animate = !gameState.isAtStartPosition()
    boardView.setPosition(gameState.getCurrentFen(), animate)
    boardView.clearMarkers()

    // Show square markers at starting position
    if (gameState.isAtStartPosition()) {
        boardView.markOpenSquares(
            puzzle.getOpenSquares().filter(sq => !gameState.placedPieces[sq])
        )
        boardView.markBlockedSquares(puzzle.getBlockedSquares())
    }

    // Update phase indicator
    const phaseEl = document.getElementById('phase-indicator')
    const validMoveCount = gameState.moves.filter(m => m.valid).length
    const remaining = puzzle.halfMoveCount - validMoveCount
    if (gameState.isAtStartPosition()) {
        phaseEl.className = 'placement'
        phaseEl.textContent = `Place pieces on highlighted squares | ${puzzle.halfMoveCount} half-moves to play`
    } else if (remaining > 0) {
        phaseEl.className = 'play'
        phaseEl.textContent = `Move ${validMoveCount + 1} of ${puzzle.halfMoveCount} | ${remaining} half-move${remaining !== 1 ? 's' : ''} remaining`
    } else {
        phaseEl.className = 'play'
        phaseEl.textContent = `All ${puzzle.halfMoveCount} moves played \u2014 submit to check!`
    }

    // Update piece tray
    pieceTray.setEnabled(gameState.isAtStartPosition())
    pieceTray.render()

    // Update move history
    moveHistory.render()

    // Enable move input and square selection for placement
    boardView.enableMoveInput()
    if (gameState.isAtStartPosition()) {
        boardView.enableSquareSelect()
    } else {
        boardView.disableSquareSelect()
    }
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
