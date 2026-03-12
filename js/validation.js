import { Chess } from '../node_modules/chess.js/dist/esm/chess.js'
import { evaluateAllHints } from './hints.js'


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
    if (puzzle.revealedFinalPosition) {
        const revealedChess = new Chess()
        revealedChess.load(puzzle.revealedFinalPosition)
        const files = 'abcdefgh'
        for (let r = 1; r <= 8; r++) {
            for (const f of files) {
                const sq = f + r
                const expected = revealedChess.get(sq)
                if (!expected) continue
                const actual = finalChess.get(sq)
                if (!actual || actual.color !== expected.color || actual.type !== expected.type) {
                    positionMismatches.push(sq)
                }
            }
        }
    }

    // 2. Check all hints
    const hintResults = evaluateAllHints(puzzle.hints, baseFen, moves)

    // 3. Overall success
    const success = positionMismatches.length === 0 && hintResults.every(r => r.pass)

    return { success, hintResults, positionMismatches }
}
