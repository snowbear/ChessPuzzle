import { Chess } from '../node_modules/chess.js/dist/esm/chess.js'
import { Chessboard, COLOR, FEN } from '../node_modules/cm-chessboard/src/Chessboard.js'
import { loadPuzzle } from './puzzle.js'

const game = new Chess()
console.log('chess.js loaded, starting FEN:', game.fen())

const board = new Chessboard(document.getElementById('main-board'), {
    position: FEN.start,
    orientation: COLOR.white,
    assetsUrl: 'node_modules/cm-chessboard/assets/',
    style: {
        cssClass: 'default',
        showCoordinates: true,
    }
})

console.log('cm-chessboard loaded')

const puzzle = await loadPuzzle('puzzles/puzzle-001.json')
console.log('Puzzle loaded:', puzzle.metadata.title)
console.log('Open squares:', puzzle.getOpenSquares())
console.log('Hints:', puzzle.hints.length)
