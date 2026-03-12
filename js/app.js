import { Chess } from '../node_modules/chess.js/dist/esm/chess.js'
import { Chessboard, COLOR, FEN } from '../node_modules/cm-chessboard/src/Chessboard.js'

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
