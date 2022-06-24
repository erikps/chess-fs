module Chess


open System
open System.Text.RegularExpressions

let private log x = printfn $"%A{x}"
let private logM m x = printfn $"%s{m}: %A{x}"

type public Rank =
    | Pawn
    | Knight
    | Bishop
    | Rook
    | King
    | Queen

type Color =
    | White
    | Black
    member this.invert =
        if this = White then Black else White

type Piece =
    { rank: Rank
      color: Color
      hasMoved: bool }
    override this.ToString() =
        $"(%s{this.color.ToString()} %s{this.rank.ToString()})"

type Square = Piece option
type Board = Square list list

type Position =
    { row: int
      col: int }
    static member zero = { col = 0; row = 0 }

    override this.ToString() = $"(%d{this.col},%d{this.row})"

    member this.inBounds =
        this.row >= 0
        && this.row < 8
        && this.col >= 0
        && this.col < 8

    member this.abs =
        { row = Math.Abs this.row
          col = Math.Abs this.col }

    static member (+)(this, other) =
        { row = this.row + other.row
          col = this.col + other.col }

    static member (-)(this, other) =
        { row = this.row - other.row
          col = this.col - other.col }

    member this.normalised =
        { row =
            if this.row = 0 then
                0
            else
                this.row / (Math.Abs this.row)
          col =
            if this.col = 0 then
                0
            else
                this.col / (Math.Abs this.col) }

type Direction = unit // TODO: implement a type that is the result of a difference between two positions

type CastleSide =
    | QueenSide
    | KingSide

type NormalMove = { from: Position; dest: Position }

type EnPassantMove = { from: Position }

type Move =
    | Normal of NormalMove
    | EnPassant of EnPassantMove
    | Castle of CastleSide

type MoveRecord =
    { move: Move
      moved: Piece
      captured: Piece option }

type GameState =
    { board: Board
      history: MoveRecord list
      toMove: Color
      capturedPieces: Piece list }


let rankString piece =
    match piece.rank with
    | Pawn -> ""
    | Knight -> "N"
    | Bishop -> "B"
    | Rook -> "R"
    | Queen -> "Q"
    | King -> "K"


type AtomicMove = { square: Square; position: Position }
type AtomicRecord = { move: AtomicMove; replaced: Square }

type MoveRecord' =
    { moves: AtomicMove
      captured: Piece option }

let private setSquare ({ row = row; col = col }: Position) (s: Square) (board: Board) : Board =

    let setAtIndex (array: 'a list) index (element: 'a) : 'a list =
        array
        |> List.mapi (fun i a -> if i = index then element else a)

    setAtIndex board row (setAtIndex board.[row] col s)

let getSquare pos (board: Board) = board.[pos.row].[pos.col]

let private initialBoard: Board =
    // Generate the initial chess board with pieces.
    let colToRank colIndex =
        // Convert the x-coordinate to the Rank of the piece that starts on the first / last square of that file.
        assert (colIndex >= 0 && colIndex < 8)

        match colIndex with
        | 0
        | 7 -> Rook
        | 1
        | 6 -> Knight
        | 2
        | 5 -> Bishop
        | 3 -> Queen
        | 4 -> King
        | _ -> raise (Exception "Invalid column number.")

    let positionToPiece rowIndex colIndex : Square =
        // Maps the position on the board to the
        assert (colIndex >= 0 && colIndex < 8)
        assert (rowIndex >= 0 && rowIndex < 8)

        let createPiece rank color =
            Some(
                { rank = rank
                  color = color
                  hasMoved = false }
            )

        match rowIndex with
        | 0 -> createPiece (colToRank colIndex) White
        | 1 -> createPiece Pawn White
        | 6 -> createPiece Pawn Black
        | 7 -> createPiece (colToRank colIndex) Black
        | _ -> None

    [ for row in [ 0..7 ] do
          [ for col in [ 0..7 ] do
                (positionToPiece row col) ] ]

let initialGameState: GameState =
    { board = initialBoard
      history = []
      toMove = White
      capturedPieces = [] }

let allPositions =
    List.allPairs [ 0..7 ] [ 0..7 ]
    |> List.map (fun (col, row) -> { col = col; row = row })

let findPieces color rank state =
    // Returns a list of all positions with the given piece.
    allPositions
    |> List.map (fun pos -> (getSquare pos state.board, pos))
    |> List.choose (fun (square, pos) ->
        match square with
        | Some piece when piece.color = color && piece.rank = rank -> Some pos
        | _ -> None)

module Move =

    let private isObstructed direction (dest: Position) (from: Position) state =
        // Is there an obstructing piece in the direction of travel?
        assert (from.inBounds && dest.inBounds) // The positions are both in bounds

        let rec checkNextSquare (pos: Position) =
            printfn $"%A{pos}"

            pos.inBounds
            && (pos = dest
                || match getSquare pos state.board with
                   | Some _ -> false
                   | None -> checkNextSquare (pos + direction))

        not (checkNextSquare (from + direction))


    let private isDiagonalMove (from: Position) (dest: Position) state =
        // Is the move a purely diagonal, i.e. Bishop move

        let difference = dest - from

        let obstructed () =
            isObstructed difference.normalised dest from state

        // Check that the absolutes of row and column are the same. This would mean it's a diagonal move.
        difference.abs.col = difference.abs.row
        && not (obstructed ())

    let private isStraightMove (from: Position) (dest: Position) state =
        // Is the move a purely straight, i.e. Rook move
        let difference = dest - from

        let obstructed () =
            isObstructed difference.normalised dest from state

        (difference.col = 0 || difference.row = 0)
        && not (obstructed ())

    let private isKingMove (from: Position) (dest: Position) =
        let d = from - dest

        // Check that, in each direction, the piece is moved at most 1 square.
        (d.abs.col = 1 || d.abs.col = 0)
        && (d.abs.row = 1 || d.abs.row = 0)

    let private isKnightMove (from: Position) (dest: Position) =
        // The knight can move 2 positions along one axis and then one position along the other.
        let d = from - dest

        (d.abs.row = 2 && d.abs.col = 1)
        || (d.abs.col = 2 && d.abs.row = 1)

    let private pawnDirection color = if color = White then 1 else -1 // The direction the pawn must travel in

    // TODO: The current implementation of the isAnyPawnMove wastes does a lot of stuff that isPawnDoubleMove does again
    let private isPawnDoubleMove origin dest state =
        let square = getSquare origin state.board

        match square with
        | Some piece ->
            let delta = dest - origin
            let direction = pawnDirection piece.color

            let captures =
                (getSquare dest state.board).IsSome

            let blocked =
                let skippedPos =
                    { dest with row = dest.row - direction }

                (getSquare skippedPos state.board).IsSome

            delta.row = (direction * 2) // Does it move 2 squares?
            && not piece.hasMoved // The pawn may only move doubly if it is its first move.
            && not captures // Straight pawn moves cannot capture.
            && not blocked // The pawn cannot "jump" over another piece, no matter its color.
        | None -> false

    let private isAnyPawnMove (origin: Position) (dest: Position) state =
        // Is this a simple pawn move, excluding en passant, but including moving twice, if it was the first move
        let sq = getSquare origin state.board

        match sq with
        | Some piece ->
            let d = dest - origin
            let direction = pawnDirection piece.color

            let captures =
                (getSquare dest state.board).IsSome

            let isNormalMove =
                d.row = direction && not captures // Does the pawn move one square in its direction of travel?

            let isCaptureMove =
                d.abs.col = 1 && d.row = direction && captures // Does pawn move diagonally and is it actually capturing a piece

            let isDoubleMove =
                isPawnDoubleMove origin dest state

            (d.col = 0 && (isNormalMove || isDoubleMove))
            || isCaptureMove

        | None -> false

    let private enPassantDestination lastMove color = // The position of the square skipped by the previous move.
        { lastMove.dest with row = lastMove.dest.row - (pawnDirection color) }



    /// Is the move legal given the current game state? Takes check and checkmate into account.
    /// TODO: Change the return type to a MoveRecord option that already contains the move information so that code duplication between isLegal and apply is prevented

    let isLegal (move: Move) (state: GameState) : bool =
        let get pos = getSquare pos state.board

        let hasSameColor sq1 sq2 =
            match sq1, sq2 with
            | Some c1, Some c2 when c1.color = c2.color -> true
            | _ -> false

        // Is the move legal, disregarding mate, checkmate.
        let isPseudoLegalMove move =
            match move with
            | Normal m ->
                let piece = get m.from
                let possibleCapture = get m.dest

                match piece with
                | None -> false // Only a piece can move, empty space can't.
                | Some { color = color } when color <> state.toMove -> false // The wrong player is trying to move
                | Some _ when hasSameColor piece possibleCapture -> false // Cannot go to a square with a piece of the same color
                | Some piece ->
                    match piece.rank with
                    | Pawn -> isAnyPawnMove m.from m.dest state
                    | Knight -> isKnightMove m.from m.dest
                    | Bishop -> isDiagonalMove m.from m.dest state
                    | Rook -> isStraightMove m.from m.dest state
                    | Queen ->

                        // Both straight and diagonal moves can be legal for the queen
                        isStraightMove m.from m.dest state
                        || isDiagonalMove m.from m.dest state
                    | King -> isKingMove m.from m.dest
            | EnPassant m ->
                // Check if opposing pawn just moved 2 squares
                match state.history with
                | last :: _ ->
                    match last.move with
                    | Normal lastMove ->

                        let prevMoveDelta =
                            lastMove.dest - lastMove.from

                        let prevMoveDirection =
                            pawnDirection state.toMove.invert

                        let isPrevMoveValid = // Was the previous move a pawn double move?
                            last.moved.rank = Pawn
                            && prevMoveDelta.row = prevMoveDirection * 2
                            && prevMoveDelta.col = 0

                        let enPassantDest = // The position the capturing pawn ends up in
                            { lastMove.dest with row = lastMove.dest.row - prevMoveDirection }

                        let enPassantDelta = // The move delta for the planned enPassant move
                            enPassantDest - m.from

                        let isEnPassantDeltaValid =
                            enPassantDelta.row = -prevMoveDirection
                            && Math.Abs enPassantDelta.col = 1

                        // Check that the pawn being moved goes to the right square
                        match get m.from with
                        | Some piece ->
                            isPrevMoveValid
                            && piece.rank = Pawn
                            && isEnPassantDeltaValid

                        | None -> false

                    | _ -> false // The previous move was not a normal move
                | [] -> false // No previous move exists

            | Castle castleSide ->
                let didKingMove =
                    findPieces state.toMove King state
                    |> List.choose (fun pos -> getSquare pos state.board) // Get "all" (i.e. the one) kings of the color of the next move
                    |> List.forall (fun piece -> piece.hasMoved) // Make sure that it has not moved.

                let row =
                    if state.toMove = White then 0 else 7 // The row where the castling takes place

                let isFree column = // Checks if the specified square on the castling row is free
                    getSquare { row = row; col = column } state.board
                    |> Option.isNone


                let rookHasNotMoved column =
                    match getSquare { row = row; col = column } state.board with
                    | Some { rank = Rook
                             hasMoved = false
                             color = c } when c = state.toMove -> true
                    | _ -> false


                let isLegalCastle freeColumns rookColumn =
                    freeColumns |> Array.forall isFree
                    && rookHasNotMoved rookColumn

                not didKingMove // If the king already moved, castle is impossible a priori
                && match castleSide with
                   | QueenSide -> isLegalCastle [| 1; 2; 3 |] 0 // The B, C and D squares need to be free; A column has the rook
                   | KingSide -> isLegalCastle [| 5; 6 |] 7 // The F and G squares; H column has the rook

        isPseudoLegalMove move

    /// Check if the move is a legal one, if so make it and return the new GameState, otherwise return None.
    let apply (move: Move) (state: GameState) : GameState option =
        if isLegal move state then
            // Apply the move and get a new GameState
            match move with
            | Normal { from = from; dest = dest } ->
                let captured = getSquare dest state.board // Check if something was captured, store the captured value

                let movedPiece =
                    getSquare from state.board
                    |> Option.map (fun p -> { p with hasMoved = true }) // The piece being moved, with hasMoved being now true

                let board =
                    state.board
                    |> setSquare from None // Update the place where the piece came from to be empty
                    |> setSquare dest movedPiece // Put the piece into its new position on the board

                match movedPiece with
                | Some moved ->
                    let record =
                        { move = move
                          moved = moved
                          captured = captured } // Create a record for the move

                    let capturedPieces =
                        match captured with
                        | Some piece -> piece :: state.capturedPieces
                        | None -> state.capturedPieces

                    let newState =
                        { history = record :: state.history
                          board = board
                          toMove = state.toMove.invert
                          capturedPieces = capturedPieces }

                    Some newState
                | None -> None
            | EnPassant currentMove ->
                // TODO: implement
                match state.history with
                | { moved = lastMovedPiece
                    move = Normal lastMove } :: _ when
                    lastMovedPiece.rank = Pawn
                    && (lastMove.dest - lastMove.from).abs.row = 2
                    ->
                    match getSquare currentMove.from state.board with
                    | Some movingPawn ->
                        let captured = lastMovedPiece

                        let newRecord =
                            { move = EnPassant currentMove
                              moved = movingPawn
                              captured = Some lastMovedPiece }

                        let board =
                            state.board
                            |> setSquare lastMove.dest None // The captured pawn is removed.
                            |> setSquare (enPassantDestination lastMove lastMovedPiece.color) (Some movingPawn) // Place the moving pawn on its new square.
                            |> setSquare currentMove.from None // Clear the square the currently moving pawn is coming from.

                        Some
                            { history = newRecord :: state.history
                              board = board
                              toMove = state.toMove.invert
                              capturedPieces = captured :: state.capturedPieces }
                    | _ -> None
                | _ -> None

            | Castle s ->
                let row =
                    if state.toMove = White then 0 else 7

                let kingOrigin = { row = row; col = 4 }

                let rookOrigin =
                    let col = if s = QueenSide then 0 else 7
                    { row = row; col = col }

                let kingDestination =
                    let col = if s = QueenSide then 2 else 6
                    { row = row; col = col }

                let rookDestination =
                    let col = if s = QueenSide then 3 else 5
                    { row = row; col = col }

                let king = getSquare kingOrigin state.board
                let rook = getSquare rookOrigin state.board

                let board =
                    state.board
                    |> setSquare kingOrigin None
                    |> setSquare rookOrigin None
                    |> setSquare kingDestination king
                    |> setSquare rookDestination rook

                let record =
                    { move = move
                      captured = None
                      moved = king.Value }

                Some
                    { state with
                        history = record :: state.history
                        board = board
                        toMove = state.toMove.invert }
        else
            None

    // Revert the game state to the state before the last move. If no moves have been made the unchanged game state will be returned.
    let revertLast (state: GameState) : GameState =
        match state.history with
        | lastMoveRecord :: _ ->
            match lastMoveRecord.move with
            | Normal move ->
                let board =
                    state.board
                    |> setSquare move.from (Some lastMoveRecord.moved) // Return the moved piece
                    |> setSquare move.dest lastMoveRecord.captured // Return the potentially captured piece

                { state with
                    board = board
                    toMove = state.toMove.invert
                    history = List.tail state.history }
            | _ -> state
        | [] -> state
    // TODO: currently EnPassant and Castle break reversion because they're not explicitly handled

    open Parser

    /// Parses the SAN (https://en.wikipedia.org/wiki/Algebraic_notation_(chess)) string and returns the described move if it is valid for the current game state.
    let fromAlgebraic (str: string) (state: GameState) : Move option =


        // Reversing the string is a "hack", because the origin row and column are optional, if they are not present, the parser would interpret the destination square as the origin square
        let reversed =
            str
            |> Seq.rev
            |> Seq.map string
            |> String.concat ""

        // Interprets the parsed values into a move, if they are valid given the game state.
        let interpret
            (rank: Rank)
            (possibleCol: int option)
            (possibleRow: int option)
            (dest: Position)
            (isCapture: bool)
            : Move option =
            // Tries to find the position the piece has to come from, potentially using contextual information such as which pieces can make legal moves.
            let positions =
                findPieces state.toMove rank state

            let legalFromPositions = // List of positions of pieces of the rank that can legally move to the destination square.
                positions
                |> List.filter (fun pos -> isLegal (Normal { from = pos; dest = dest }) state)

            let move =
                let createNormalMove from =
                    // Converts the from position to a move using the destination given as an
                    // argument to findFromPos
                    Normal { from = from; dest = dest }

                if legalFromPositions.Length = 1 then
                    // Since there is only one piece of the given rank that can be legally moved to the destination square, no disambiguation is needed.
                    Some(createNormalMove legalFromPositions.Head)
                else // Disambiguation is needed
                    match (possibleCol, possibleRow) with
                    | Some col, Some row -> Some(createNormalMove { col = col; row = row }) // The full from square is provided
                    | None, Some row -> None // Only the row is provided TODO: implement
                    | Some col, None -> None // Only the column is provided TODO: implement
                    | _ -> None // Not enough information is known, since otherwise the move would have been picked up earlier

            move

        let rankSymbolParser: Rank Parser =
            constant "N" Knight
            <|> constant "B" Bishop
            <|> constant "R" Rook
            <|> constant "Q" Queen
            <|> constant "K" King
            <|> constant "P" Pawn
            <|> constant "" Pawn


        // Parses the [a-h] column notation and converts to 0-bases index.
        let columnParser: int Parser =
            constant "a" 0
            <|> constant "b" 1
            <|> constant "c" 2
            <|> constant "d" 3
            <|> constant "e" 4
            <|> constant "f" 5
            <|> constant "g" 6
            <|> constant "h" 7


        // Parses the row and converts it to be 0-based.
        let rowParser =
            bind
                (fun digit ->
                    if digit > 0 && digit < 8 then
                        Some(digit - 1)
                    else
                        None)
                digitParser



        let captureParser = stringParser "x" // "x" indicates that a piece is being captured

        let optionalSquareParser =
            (opt rowParser) => (opt columnParser) // The (optional) row and column of the origin square

        let squareParser =
            rowParser => columnParser
            |> map (fun (row, col) -> { col = col; row = row })

        let parser =
            squareParser
            => opt captureParser
            => optionalSquareParser
            => rankSymbolParser

            |> bind (fun (((pos, capture), (row, col)), rank) -> interpret rank col row pos capture.IsSome)

        let res, _ = parser reversed
        res


    let toAlgebraic (record: MoveRecord) (state: GameState) : string option =

        let colSymbol col =
            match col with
            | 0 -> "a"
            | 1 -> "b"
            | 2 -> "c"
            | 3 -> "d"
            | 4 -> "e"
            | 5 -> "f"
            | 6 -> "g"
            | 7 -> "h"
            | _ -> failwith "Invalid column index."

        let captureSymbol =
            if record.captured.IsSome then
                "x"
            else
                ""


        match record.move with
        | Normal move ->
            let possibleConflictingPieces =
                if record.moved.rank = Pawn then // There is never any ambiguity in simple pawn moves, pawn captures are handled below.
                    []
                else
                    findPieces record.moved.color record.moved.rank state // All pieces with the same rank and color
                    |> List.filter (fun pos -> pos <> move.from) // Filter out the piece itself
                    |> List.filter (fun pos -> isLegal (Normal { move with from = pos }) state) // Filter out non legal moves

            let possibleConflictingPiecesWithSameCol =
                List.filter (fun { row = row } -> row = move.from.row) possibleConflictingPieces

            let possibleConflictingPiecesWithSameRow =
                List.filter (fun { col = col } -> col = move.from.col) possibleConflictingPiecesWithSameCol

            let colRequired = // Is the column required for disambiguation
                (record.moved.rank = Pawn && record.captured.IsSome) // Pawn captures have the column by convention, even if unambiguous
                || possibleConflictingPiecesWithSameCol.Length < possibleConflictingPieces.Length // Does writing the column reduce ambiguity

            let rowRequired = // Is the row required for disambiguation
                (record.moved.rank <> Pawn) // Pawns never require the row for disambiguation, as there is only one possibility
                && possibleConflictingPiecesWithSameRow.Length < possibleConflictingPieces.Length // Does writing the row reduce ambiguity?

            let fromColStr =
                if colRequired then
                    (colSymbol move.from.col)
                else
                    ""

            let fromRowStr =
                if rowRequired then
                    (move.from.row + 1).ToString()
                else
                    ""

            let fromStr =
                $"%s{fromColStr}%s{fromRowStr}"

            Some
                $"%s{rankString record.moved}%s{fromStr}%s{captureSymbol}%s{colSymbol move.dest.col}%d{move.dest.row + 1}"
        | EnPassant move ->
            let fromCol = colSymbol move.from.col


            match state.history with
            | _ :: { move = Normal move; moved = moved } :: _ ->
                let destination =
                    enPassantDestination move moved.color

                Some $"{fromCol}x{colSymbol destination.col}{destination.row + 1}"
            | _ -> None
        | Castle side ->
            Some(
                if side = QueenSide then
                    "O-O-O"
                else
                    "O-O"
            )


    /// Creates a transcript of algebraic notation as a list of strings with each string being a move.
    let createTranscript (state: GameState) : string list =
        state.history
        |> Seq.rev
        |> List.ofSeq
        |> List.choose (fun x -> toAlgebraic x state)


    /// Transforms positions given by the user to a move. Useful for converting mouse-based user input into a "Move" record.
    /// Does not check if the move is legal.
    let fromInputPositions (origin: Position) (destination: Position) (board: Board) : Move =
        let square = getSquare origin board
        let delta = (destination - origin).abs

        let normal () =
            Normal { from = origin; dest = destination }

        match square with
        | Some piece ->
            match piece with
            | { rank = King } when delta.col > 1 || delta.row > 1 -> // If the king moves more than 1 square, he must be trying to castle.
                Castle(
                    if destination.col < 4 then
                        QueenSide
                    else
                        KingSide
                )

            | { rank = Pawn } when
                delta.col <> 0
                && (getSquare destination board).IsNone
                ->
                (EnPassant { from = origin })
            | _ -> normal ()
        | None -> normal ()
