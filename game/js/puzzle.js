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
        this.revealedFinalPosition = data.revealedFinalPosition || ''
        this.hints = data.hints || []
    }

    getOpenSquares() {
        const open = []
        const squares = this.startPosition.squares || {}
        for (const [sq, state] of Object.entries(squares)) {
            if (state === 'open') open.push(sq)
        }
        return open
    }

    getBlockedSquares() {
        const blocked = []
        const squares = this.startPosition.squares || {}
        for (const [sq, state] of Object.entries(squares)) {
            if (state === 'blocked') blocked.push(sq)
        }
        return blocked
    }

    getInitialFen() {
        return this.startPosition.fen
    }

    getConstraint(color, pieceType) {
        const colorConstraints = this.pieceConstraints[color]
        if (!colorConstraints) return { min: 0, max: 0 }
        return colorConstraints[pieceType] || { min: 0, max: 0 }
    }
}

export async function loadPuzzle(path) {
    const response = await fetch(path)
    if (!response.ok) throw new Error(`Failed to load puzzle: ${path}`)
    const data = await response.json()
    return new Puzzle(data)
}
