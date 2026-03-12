/**
 * Move history display with back/forward navigation.
 */
export class MoveHistory {
    constructor(elementId, gameState) {
        this.element = document.getElementById(elementId)
        this.gameState = gameState
        this.onMoveSelected = null  // callback: (moveIndex) => void
    }

    render() {
        this.element.innerHTML = ''

        // "Start" position entry
        const startSpan = document.createElement('span')
        startSpan.className = 'move'
        if (this.gameState.currentMoveIndex === -1) startSpan.classList.add('active')
        startSpan.textContent = 'Start'
        startSpan.addEventListener('click', () => {
            if (this.onMoveSelected) this.onMoveSelected(-1)
        })
        this.element.appendChild(startSpan)

        for (let i = 0; i < this.gameState.moves.length; i++) {
            const move = this.gameState.moves[i]
            const span = document.createElement('span')
            span.className = 'move'
            if (i === this.gameState.currentMoveIndex) span.classList.add('active')
            if (!move.valid) span.classList.add('invalid')

            const moveNum = Math.floor(i / 2) + 1
            const isWhite = i % 2 === 0
            const prefix = isWhite ? `${moveNum}. ` : ''

            span.textContent = ` ${prefix}${move.san}`
            span.addEventListener('click', () => {
                if (this.onMoveSelected) this.onMoveSelected(i)
            })
            this.element.appendChild(span)
        }
    }
}
