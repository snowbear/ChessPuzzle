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
const PIECE_MAP = {
    'p': { color: 'b', type: 'p' }, 'n': { color: 'b', type: 'n' },
    'b': { color: 'b', type: 'b' }, 'r': { color: 'b', type: 'r' },
    'q': { color: 'b', type: 'q' }, 'k': { color: 'b', type: 'k' },
    'P': { color: 'w', type: 'p' }, 'N': { color: 'w', type: 'n' },
    'B': { color: 'w', type: 'b' }, 'R': { color: 'w', type: 'r' },
    'Q': { color: 'w', type: 'q' }, 'K': { color: 'w', type: 'k' },
}

function parseRevealedSquares(fen) {
    const board = fen.split(' ')[0]
    const result = []
    let index = 0
    for (const ch of board) {
        if (ch === '/') continue
        if (ch >= '1' && ch <= '8') { index += parseInt(ch); continue }
        const file = String.fromCharCode('a'.charCodeAt(0) + (index % 8))
        const rank = 8 - Math.floor(index / 8)
        const piece = PIECE_MAP[ch]
        if (piece) result.push({ square: file + rank, color: piece.color, type: piece.type })
        index++
    }
    return result
}

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
        const revealed = parseRevealedSquares(puzzle.revealedFinalPosition)
        const files = 'abcdefgh'
        for (const { square, color, type } of revealed) {
            const actual = finalChess.get(square)
            if (!actual || actual.color !== color || actual.type !== type) {
                positionMismatches.push(square)
            }
        }
    }

    // 2. Check all hints
    const hintResults = evaluateAllHints(puzzle.hints, baseFen, moves)

    // 3. Overall success
    const success = positionMismatches.length === 0 && hintResults.every(r => r.pass)

    return { success, hintResults, positionMismatches }
}
