module App

open Elmish

open Chess


// Apply the moves, given in SAN format to the game state. If one of the moves is not possible, None is returned.
let applySanMoves (moves: string list) (state: GameState) : GameState option =
    let mutable state = Some state

    for move in moves do
        state <-
            match state with
            | Some state ->
                let move = Move.fromAlgebraic move state
        
                match move with
                | Some move -> Move.apply move state
                | None -> None
            | None -> None
    state

// CSS helper to conditionally apply a class name
let optionalClass predicate className = if predicate then className else ""



// Return the gamestate as it was n steps ago
let revertMoves n state =
    let mutable state = state

    for i in 0 .. n - 1 do
        state <- Move.revertLast state

    state

let pieceToImage piece =
    let rank =
        match piece.rank with
        | Pawn -> "pawn"
        | Knight -> "knight"
        | Bishop -> "bishop"
        | Rook -> "rook"
        | Queen -> "queen"
        | King -> "king"

    let color =
        if piece.color = Black then
            "black"
        else
            "white"

    $"images/%s{color}_%s{rank}.svg"


(*
    MODEL
*)

type Index = int

type Msg =
    | SanInput of string
    | SetSide of Color
    | SquarePressed of Position
    | UpdateViewedMove of Index

type Model =
    { state: GameState
      selectedPosition: Position option
      side: Color
      // TODO: reuse this property to mean the side being played, introduce "viewSwapped" bool to implement current behaviour with.
      sanInput: string
      currentlyViewedMove: Index }

let init () : Model * Msg Cmd =

    let state =
        initialGameState
        |> applySanMoves [ "e4"; "a5"; "e5"; "d5" ]

    { state = state.Value
      selectedPosition = None
      side = White
      sanInput = ""
      currentlyViewedMove = state.Value.history.Length - 1 },
    Cmd.none

(*
    UPDATE
*)

let update (msg: Msg) (model: Model) : Model * Msg Cmd =
    // apply the move if possible, and then, if applied, clear the SAN input and the currently selected square.
    let applyMoveIfPossible move =
        match move with
        | Some move ->
            let newState = Move.apply move model.state

            match newState with
            | Some newState ->
                { model with
                    state = newState
                    sanInput = ""
                    selectedPosition = None },
                Cmd.ofMsg (UpdateViewedMove(model.state.history.Length))
            | None -> { model with selectedPosition = None }, Cmd.none
        | None -> model, Cmd.none

    match msg with
    | SanInput string ->
        let move =
            Move.fromAlgebraic string model.state

        applyMoveIfPossible move

    | SetSide color -> { model with side = color }, Cmd.none
    | SquarePressed justPressedPosition ->
        let latestMoveIndex =
            model.state.history.Length - 1

        if model.currentlyViewedMove = latestMoveIndex then
            match model.selectedPosition with
            | Some previousPosition when previousPosition = justPressedPosition ->
                { model with selectedPosition = None }, Cmd.none // Clicking the same position again deselects it
            | Some previousPosition ->
                applyMoveIfPossible (
                    Some(Move.fromInputPositions previousPosition justPressedPosition model.state.board)
                )
            | None -> { model with selectedPosition = Some justPressedPosition }, Cmd.none
        else
            model, Cmd.ofMsg (UpdateViewedMove latestMoveIndex)
    | UpdateViewedMove index -> { model with currentlyViewedMove = index }, Cmd.none

(*
    VIEW
*)

open Feliz

let boardView model dispatch =

    let currentMoveIndex =
        model.state.history.Length
        - model.currentlyViewedMove
        - 1

    let currentlyViewedState =
        revertMoves currentMoveIndex model.state 
        
    let viewSquare square pos =
        let className =   
            sprintf "square %s %s"
                (if Some pos = model.selectedPosition then
                     "highlight"
                 else
                     "")
                (if (pos.row + pos.col) % 2 <> 0 then
                     "black"
                 else
                     "white")

        let pieceIcon =
            match square with
            | Some piece ->
                let pieceClass =
                    $"piece %s{piece.color.ToString().ToLower()}-piece"

                Html.img [ 
                    prop.className pieceClass
                    prop.draggable false
                    prop.src(pieceToImage piece) 
                    ]
            | None -> Html.div "" 

        Html.div [ 
            prop.className className
            prop.onClick(fun _ -> SquarePressed pos |> dispatch) 
            prop.children [pieceIcon]
            ]

    let renderedBoard =
        let relativeRowIndex col =
            if model.side = Black then
                col
            else
                7 - col

        allPositions
        |> List.map (fun { col = col; row = row } ->
            { col = row
              row = relativeRowIndex col })
        |> List.map (fun p -> viewSquare (getSquare p currentlyViewedState.board) p)

    Html.div [ prop.className "board" ; prop.children renderedBoard ] 

let historyView (model: Model) dispatch =

    let moveRecordView (record: MoveRecord) index =
        let san =
            Move.toAlgebraic record model.state
        
        let san =
            match san with
            | Some san -> san
            | None -> "ERROR"
        
        let name = $"%d{index + 1}.%s{san}\t"

        let selected =
            optionalClass (index = model.currentlyViewedMove) " selected"


        Html.button [ 
                prop.onClick (fun _ -> UpdateViewedMove index |> dispatch)
                prop.className $"history-tile%s{selected}"
                prop.children [Html.text name]
                 ] 
    let history =
        model.state.history
        |> Seq.rev
        |> List.ofSeq
        |> List.mapi (fun i move -> moveRecordView move i)

    Html.div [ prop.className "history" ; prop.children history ] 

let view (model: Model) (dispatch: Msg -> unit) =
    let handleInput (value: string)  = SanInput value |> dispatch

    // Input for standard algebraic notation
    let sanInput =
        Html.input [ 
                prop.onChange handleInput
                prop.className "list-item"
                prop.value model.sanInput
                prop.type' "text" 
            ]

    // Button to switch the the board around
    let switchBoardButton =
            Html.button [
                prop.className "list-item"
                prop.onClick(fun _ -> SetSide model.side.invert |> dispatch) 
                prop.children [Html.div "Swap Board" ] ]

    let toMoveLabel =
        let text = $"%s{model.state.toMove.ToString()} to move!"
        Html.div [ prop.className "list-item" ; prop.children [ Html.text text] ]
        

    let inputSection =
        Html.div [ 
            prop.className "input-section" 
            prop.children [
                sanInput
                Html.div switchBoardButton
                toMoveLabel
            ]
        ]

    Html.div [ 
        prop.className "container" 
        prop.children [
            Html.div [] // placeholder for the grid
            Html.div [
                boardView model dispatch
                inputSection
            ]
            historyView model dispatch
        ]
    ]

open Elmish.React

Program.mkProgram init update view
|> Program.withReactBatched "app"
|> Program.run
