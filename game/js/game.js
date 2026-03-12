import { Chess } from '../node_modules/chess.js/dist/esm/chess.js'

/**
 * Manages game state: piece placement on starting position + move sequence.
 * Wraps chess.js for legal move validation.
 */
export class GameState {
    constructor(puzzle) {
        this.puzzle = puzzle
        this.placedPieces = {}  // { square: { color: 'w', type: 'p' } }
        this.moves = []         // array of chess.js Move objects (with added .valid flag)
        this.currentMoveIndex = -1  // -1 = at starting position
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

    placePiece(square, piece) {
        if (!this.puzzle.getOpenSquares().includes(square)) return false
        this.placedPieces[square] = piece
        this._rebuildChess()
        this._revalidateMoves()
        return true
    }

    removePlacedPiece(square) {
        if (!this.placedPieces[square]) return false
        delete this.placedPieces[square]
        this._rebuildChess()
        this._revalidateMoves()
        return true
    }

    getChessAtCurrentMove() {
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

    getCurrentFen() {
        return this.getChessAtCurrentMove().fen()
    }

    makeMove(from, to, promotion) {
        // If not at end, truncate future moves
        if (this.currentMoveIndex < this.moves.length - 1) {
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

    goToMove(index) {
        if (index < -1 || index >= this.moves.length) return false
        this.currentMoveIndex = index
        return true
    }

    isAtStartPosition() {
        return this.currentMoveIndex === -1
    }

    isComplete() {
        const validMoves = this.moves.filter(m => m.valid)
        return validMoves.length >= this.puzzle.halfMoveCount
    }

    _revalidateMoves() {
        const chess = new Chess()
        chess.load(this.chess.fen())
        let foundInvalid = false
        for (const move of this.moves) {
            if (foundInvalid) {
                move.valid = false
                continue
            }
            try {
                chess.move({ from: move.from, to: move.to, promotion: move.promotion })
                move.valid = true
            } catch {
                move.valid = false
                foundInvalid = true
            }
        }
    }

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
