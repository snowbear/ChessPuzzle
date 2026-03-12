import { Chess } from '../node_modules/chess.js/dist/esm/chess.js'
import { evaluateAllHints } from './hints.js'

const PIECE_SHORT = { pawn: 'p', knight: 'n', bishop: 'b', rook: 'r', queen: 'q', king: 'k' }

/**
 * Validate the player's solution.
 *
 * Returns {
 *   success: boolean,
 *   hintResults: [{ pass, hint }],
 *   positionMismatches: [square]
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
