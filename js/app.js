import { loadPuzzle } from './puzzle.js'
import { GameState } from './game.js'
import { BoardView, ThumbnailView } from './board.js'

const puzzle = await loadPuzzle('puzzles/puzzle-001.json')
const gameState = new GameState(puzzle)

const boardView = new BoardView('main-board')
await boardView.init()
boardView.setPosition(gameState.getCurrentFen(), false)
boardView.markOpenSquares(puzzle.getOpenSquares())
boardView.markBlockedSquares(puzzle.getBlockedSquares())

const thumbnail = new ThumbnailView('thumbnail-board')
await thumbnail.init()
thumbnail.showRevealedPosition(puzzle.revealedFinalPosition)

console.log('Boards initialized')
