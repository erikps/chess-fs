// Console application for playing chess.
module Console

open System

let private pieceToString (piece: Chess.Piece) : string =
    let rankString =
        match piece.rank with
        | Chess.Pawn -> "P"
        | Chess.Knight -> "N"
        | Chess.Bishop -> "B"
        | Chess.Rook -> "R"
        | Chess.Queen -> "Q"
        | Chess.King -> "K"

    let colorString =
        match piece.color with
        | Chess.White -> "w"
        | Chess.Black -> "b"

    colorString + rankString


let private printBoard (board: Chess.Board) =
    for row in board do
        for slot in row do
            match slot with
            | Some p -> printf $"%s{pieceToString p} "
            | None -> printf " _ "

        printf "\n"

open Chess

[<EntryPoint>]
let main argv =

    let move = Move.fromAlgebraic "a3" initialGameState

    let move =
        { from = { col = 0; row = 1 }
          dest = { col = 0; row = 2 } }

    let mutable state = initialGameState

    while true do
        printBoard state.board
        printfn "Enter input in SAN: "
        let str = Console.ReadLine()
        let move = Move.fromAlgebraic str state

        match move with
        | Some move ->
            let s = Move.apply move state

            match s with
            | Some s -> state <- s
            | _ -> ()

            ()
        | _ -> ()

    let isLegal = Move.isLegal (Normal move) initialGameState

    printf $"%b{isLegal}"
    0 // return an integer exit code
