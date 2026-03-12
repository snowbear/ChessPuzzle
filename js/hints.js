import { Chess } from '../node_modules/chess.js/dist/esm/chess.js'

const PIECE_SHORT = { pawn: 'p', knight: 'n', bishop: 'b', rook: 'r', queen: 'q', king: 'k' }

/**
 * Check if a single move satisfies all constraints.
 * move: chess.js verbose Move object
 * constraints: hint constraints object
 * chess: chess.js instance AFTER the move was made (for check detection)
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
        // chess.js move flags: 'k' = kingside castle, 'q' = queenside castle
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
 */
export function evaluateHint(hint, baseFen, moves) {
    const validMoves = moves.filter(m => m.valid)

    if (hint.scope === 'final') {
        const chess = new Chess()
        chess.load(baseFen)
        for (const m of validMoves) {
            try { chess.move({ from: m.from, to: m.to, promotion: m.promotion }) }
            catch { return { pass: false, hint } }
        }
        return { pass: finalMatchesConstraints(hint.constraints, chess), hint }
    }

    if (hint.scope === 'any') {
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

    return { pass: false, hint }
}

export function evaluateAllHints(hints, baseFen, moves) {
    return hints.map(hint => evaluateHint(hint, baseFen, moves))
}
