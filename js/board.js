import { Chessboard, COLOR, INPUT_EVENT_TYPE, FEN } from
    '../node_modules/cm-chessboard/src/Chessboard.js'
import { Markers, MARKER_TYPE } from
    '../node_modules/cm-chessboard/src/extensions/markers/Markers.js'
import { PromotionDialog, PROMOTION_DIALOG_RESULT_TYPE } from
    '../node_modules/cm-chessboard/src/extensions/promotion-dialog/PromotionDialog.js'

const ASSETS_URL = 'node_modules/cm-chessboard/assets/'

function toCmPiece(color, type) {
    return color + type  // 'w' + 'p' = 'wp'
}

export class BoardView {
    constructor(elementId, options = {}) {
        this.elementId = elementId
        this.onPieceDrop = options.onPieceDrop || null      // (from, to) => bool
        this.onPiecePickup = options.onPiecePickup || null  // (square) => bool
        this.onSquareClick = options.onSquareClick || null   // (square) => void
        this.board = null
        this._squareSelectEnabled = false
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
        await new Promise(r => setTimeout(r, 100))
    }

    setPosition(fen, animated = true) {
        this.board.setPosition(fen, animated)
    }

    enableMoveInput() {
        if (this.board.isMoveInputEnabled()) return
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
                case INPUT_EVENT_TYPE.moveInputCanceled:
                    break
                case INPUT_EVENT_TYPE.moveInputFinished:
                    break
            }
        })
    }

    disableMoveInput() {
        this.board.disableMoveInput()
    }

    /**
     * Enable click-on-square detection for piece placement.
     * Uses a custom pointerdown handler that walks up the DOM to find
     * the data-square attribute (needed because marker overlays may
     * intercept clicks before reaching the square rect).
     */
    enableSquareSelect() {
        if (this._squareSelectEnabled) return
        this._squareSelectEnabled = true
        const container = document.getElementById(this.elementId)
        this._squareClickHandler = (e) => {
            // Walk up DOM to find element with data-square
            let el = e.target
            let square = null
            while (el && el !== container) {
                const ds = el.getAttribute && el.getAttribute('data-square')
                if (ds) { square = ds; break }
                el = el.parentElement
            }
            if (!square) return
            const piece = this.board.getPiece(square)
            if (!piece && this.onSquareClick) {
                this.onSquareClick(square)
            }
        }
        container.addEventListener('pointerdown', this._squareClickHandler)
    }

    disableSquareSelect() {
        if (!this._squareSelectEnabled) return
        this._squareSelectEnabled = false
        const container = document.getElementById(this.elementId)
        container.removeEventListener('pointerdown', this._squareClickHandler)
        this._squareClickHandler = null
    }

    addMarker(square, type = MARKER_TYPE.frame) {
        this.board.addMarker(type, square)
    }

    removeMarkers(square, type) {
        this.board.removeMarkers(type, square)
    }

    markOpenSquares(squares) {
        for (const sq of squares) {
            this.board.addMarker(MARKER_TYPE.frame, sq)
        }
    }

    markBlockedSquares(squares) {
        for (const sq of squares) {
            this.board.addMarker(MARKER_TYPE.frameDanger, sq)
        }
    }

    clearMarkers() {
        this.board.removeMarkers()
    }

    setPiece(square, color, type) {
        this.board.setPiece(square, toCmPiece(color, type), true)
    }

    removePiece(square) {
        this.board.setPiece(square, null)
    }

    showPromotionDialog(square, color) {
        return new Promise((resolve) => {
            this.board.showPromotionDialog(square, color === 'w' ? COLOR.white : COLOR.black, (result) => {
                if (result.type === PROMOTION_DIALOG_RESULT_TYPE.pieceSelected) {
                    resolve(result.piece[1])
                } else {
                    resolve(null)
                }
            })
        })
    }
}

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

    showRevealedPosition(revealedFinalPosition) {
        this.board.setPosition(FEN.empty, false)
        const colorMap = { white: 'w', black: 'b' }
        const typeMap = { pawn: 'p', knight: 'n', bishop: 'b', rook: 'r', queen: 'q', king: 'k' }
        for (const [sq, piece] of Object.entries(revealedFinalPosition)) {
            const cmPiece = toCmPiece(colorMap[piece.color], typeMap[piece.type])
            this.board.setPiece(sq, cmPiece, false)
        }
    }

    markMismatches(squares) {
        for (const sq of squares) {
            this.board.addMarker(MARKER_TYPE.frameDanger, sq)
        }
    }

    clearMarkers() {
        this.board.removeMarkers()
    }
}
