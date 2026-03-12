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
        this.selectedPiece = null    // { color, type } or null (short codes: 'w'/'b', 'p'/'n' etc)
        this.enabled = true
    }

    render() {
        this.element.innerHTML = ''
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

                if (remaining > 0) {
                    item.addEventListener('click', () => {
                        this.selectedPiece = { color: colorShort, type: typeShort }
                        if (this.onPieceSelected) {
                            this.onPieceSelected(colorShort, typeShort)
                        }
                        this.render()
                    })
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
