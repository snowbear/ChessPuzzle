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
        this.onPlayMoves = null      // callback: () => void — user wants to stop placing and start moving
        this.selectedPiece = null    // { color, type } or null (short codes: 'w'/'b', 'p'/'n' etc)
        this.placementMode = true    // true = placing pieces, false = making moves
        this.enabled = true
    }

    render() {
        this.element.innerHTML = ''

        // Mode toggle button (only when enabled = at start position)
        if (this.enabled) {
            const modeBtn = document.createElement('button')
            modeBtn.className = 'tray-mode-btn'
            if (this.placementMode) {
                modeBtn.textContent = 'Play Moves'
                modeBtn.addEventListener('click', () => {
                    this.placementMode = false
                    this.selectedPiece = null
                    if (this.onPlayMoves) this.onPlayMoves()
                    this.render()
                })
            } else {
                modeBtn.textContent = 'Place Pieces'
                modeBtn.classList.add('active')
                modeBtn.addEventListener('click', () => {
                    this.placementMode = true
                    this.render()
                })
            }
            this.element.appendChild(modeBtn)
        }

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

                const item = document.createElement('div')
                item.className = 'tray-item'
                if (remaining <= 0) item.classList.add('exhausted')
                if (this.selectedPiece &&
                    this.selectedPiece.color === (color === 'white' ? 'w' : 'b') &&
                    this.selectedPiece.type === { king: 'k', queen: 'q', rook: 'r', bishop: 'b', knight: 'n', pawn: 'p' }[type]) {
                    item.classList.add('selected')
                }

                const colorShort = color === 'white' ? 'w' : 'b'
                const typeShort = { king: 'k', queen: 'q', rook: 'r', bishop: 'b', knight: 'n', pawn: 'p' }[type]

                const pieceSymbols = {
                    wk: '\u2654', wq: '\u2655', wr: '\u2656', wb: '\u2657', wn: '\u2658', wp: '\u2659',
                    bk: '\u265A', bq: '\u265B', br: '\u265C', bb: '\u265D', bn: '\u265E', bp: '\u265F'
                }

                item.innerHTML = `
                    <span class="tray-piece">${pieceSymbols[colorShort + typeShort]}</span>
                    <span class="tray-count">${placed}/${constraint.max} (min: ${constraint.min})</span>
                `

                if (remaining > 0 && this.placementMode) {
                    item.addEventListener('click', () => {
                        // Toggle selection
                        if (this.selectedPiece &&
                            this.selectedPiece.color === colorShort &&
                            this.selectedPiece.type === typeShort) {
                            this.selectedPiece = null
                        } else {
                            this.selectedPiece = { color: colorShort, type: typeShort }
                        }
                        if (this.onPieceSelected) {
                            this.onPieceSelected(colorShort, typeShort)
                        }
                        this.render()
                    })
                }
                if (!this.placementMode) {
                    item.style.opacity = '0.5'
                    item.style.cursor = 'default'
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
