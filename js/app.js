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
        // In placement phase: clicking a player-placed piece removes it
        if (gameState.placedPieces[square]) {
            gameState.removePlacedPiece(square)
            refreshBoard()
            return false  // cancel the drag (piece is removed)
        }
        // Don't allow picking up revealed pieces in placement phase
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
            const typeName = { p: 'pawn', n: 'knight', b: 'bishop', r: 'rook', q: 'queen', k: 'king' }[type]
            const colorName = color === 'w' ? 'white' : 'black'
            const constraint = puzzle.getConstraint(colorName, typeName)
            const counts = gameState.getPlacedPieceCounts()
            const placed = (counts[colorName] && counts[colorName][typeName]) || 0

            if (placed < constraint.max) {
                // Remove any existing placed piece on this square first
                gameState.removePlacedPiece(to)
                gameState.placePiece(to, { color, type })
                refreshBoard()
            }
        }
        return false  // always return false in placement (we handle board update manually)
    } else {
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
    boardView.setPosition(gameState.getCurrentFen(), true)
    boardView.clearMarkers()

    // Show square markers at starting position
    if (gameState.isAtStartPosition()) {
        boardView.markOpenSquares(
            puzzle.getOpenSquares().filter(sq => !gameState.placedPieces[sq])
        )
        boardView.markBlockedSquares(puzzle.getBlockedSquares())
    }

    // Update piece tray
    pieceTray.setEnabled(gameState.isAtStartPosition())
    pieceTray.render()

    // Update move history
    moveHistory.render()

    // Enable move input
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
