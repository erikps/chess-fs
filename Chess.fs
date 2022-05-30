module Chess

open System
open System.Text.RegularExpressions

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
    member this.invert = if this = White then Black else White

type Piece =
    { rank: Rank
      color: Color
      hasMoved: bool }
    override this.ToString() =
        sprintf "%s; %s" (this.color.ToString()) (this.rank.ToString())

type Square = Piece option
type Board = Square list list

type Position =
    { row: int
      col: int }
    static member zero = { col = 0; row = 0 }

    override this.ToString() =
        sprintf "col: %d; row: %d;" this.col this.row

    member this.inBounds =
        this.row >= 0
        && this.row < 8
        && this.col >= 0
        && this.col < 8

    member this.abs =
        { row = Math.Abs this.row
          col = Math.Abs this.col }

    member this.difference other =
        { row = this.row - other.row
          col = this.col - other.col }

type CastleSide =
    | QueenSide
    | KingSide

type NormalMove = { from: Position; dest: Position }

type EnPassantMove =
    { from: Position
      dest: Position
      capturePosition: Position }

type Move =
    | Normal of NormalMove
    | EnPassant of EnPassantMove
    | Castle of CastleSide

type MoveRecord = { move: Move; captured: Piece option }

type GameState =
    { board: Board
      history: MoveRecord list
      toMove: Color }



let private setPiece ({ row = row; col = col }: Position) (s: Square) (board: Board) : Board =

    let setAtIndex (array: 'a list) index (element: 'a) : 'a list =
        array
        |> List.mapi (fun i a -> if i = index then element else a)

    setAtIndex board row (setAtIndex (board[row]) col s)

let getPiece pos (board: Board) = board[pos.row][pos.col]

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
      toMove = White }

let private allPositions =
    List.allPairs [ 0..7 ] [ 0..7 ]
    |> List.map (fun (col, row) -> { col = col; row = row })

let findPiece color rank state =
    // Returns a list of all positions with the given piece.
    allPositions
    |> List.map (fun pos -> (getPiece pos state.board, pos))
    |> List.choose (fun (square, pos) ->
        match square with
        | Some piece when piece.color = color && piece.rank = rank -> Some pos
        | _ -> None)

module Move =
    type private MaybeBuilder() =
        // Helper to enable computation expressions working on option types.
        member this.Bind(value, f) =
            match value with
            | Some x -> (f x)
            | None -> None

        member this.Return value = Some value

    let private maybe = new MaybeBuilder()

    let private isDiagonalMove (from: Position) (dest: Position) =
        // Is the move a purely diagonal, i.e. Bishop move
        let difference = dest.difference from

        // Check that the absolutes of row and column are the same. This would mean it's a diagonal move.
        (difference.abs.col) = (difference.abs.row)

    let private isStraightMove (from: Position) (dest: Position) =
        // Is the move a purely straight, i.e. Rook move
        let difference = from.difference dest
        difference.col = 0 || difference.row = 0

    let private isKingMove (from: Position) (dest: Position) =
        let d = from.difference dest

        // Check that, in each direction, the piece is moved at most 1 square.
        (d.abs.col = 1 || d.abs.col = 0)
        && (d.abs.row = 1 || d.abs.row = 0)

    let private isKnightMove (from: Position) (dest: Position) =
        // The knight can move 2 positions along one axis and then one position along the other.
        let d = from.difference dest

        (d.abs.row = 2 && d.abs.col = 1)
        || (d.abs.col = 2 && d.abs.row = 1)

    let private isPawnMove (from: Position) (dest: Position) piece =
        // Is this a simple pawn move, excluding en passant, but including moving twice, if it was the first move
        let d = dest.difference from
        let direction = if piece.color = White then 1 else -1 // The direction the pawn must travel in
        let isFirstMove = d.row = (direction * 2) && not piece.hasMoved

        let isNormalMove = d.row = direction
        d.col = 0 && (isNormalMove || isFirstMove)


    let isLegal (move: Move) (state: GameState) : bool =
        // Is the move pseudo legal? I.e. legal without taking potential checks into account.
        let get pos = getPiece pos state.board

        let hasSameColor sq1 sq2 =
            let result =
                maybe {
                    let! sq1 = sq1
                    let! sq2 = sq2
                    return sq1.color = sq2.color
                }
            match result with
            | Some x -> x
            | None -> false


        let isPseudoLegalMove =
            match move with
            | Normal normal ->
                let piece = get normal.from
                let possibleCapture = get normal.dest

                match piece with
                | None -> false // Only a piece can move, empty space can't.
                | Some _ when hasSameColor piece possibleCapture -> false // Cannot go to a square with a piece of the same color
                | Some piece ->
                    match piece.rank with
                    | Pawn -> isPawnMove normal.from normal.dest piece
                    | Knight -> isKnightMove normal.from normal.dest
                    | Bishop -> isDiagonalMove normal.from normal.dest
                    | Rook -> isStraightMove normal.from normal.dest
                    | Queen ->
                        isStraightMove normal.from normal.dest
                        || isDiagonalMove normal.from normal.dest
                    | King -> isKingMove normal.from normal.dest

            | _ -> false // TODO: implement other cases

        isPseudoLegalMove

    let apply (move: Move) (state: GameState) : GameState option =
        // Check if the move is a legal one, if so make it and return the new GameState, otherwise return None.
        if isLegal move state then
            let appliedMove: GameState =
                // Apply the move and get a new GameState
                match move with
                | Normal ({ from = from; dest = dest }) ->
                    let captured = getPiece dest state.board // Check if something was captured, store the captured value

                    let movedPiece =
                        getPiece from state.board
                        |> Option.map (fun p -> { p with hasMoved = true }) // The piece being moved, with hasMoved being now true

                    let board =
                        state.board
                        |> setPiece from None // Update the place where the piece came from to be empty
                        |> setPiece dest movedPiece // Put the piece into its new position on the board

                    let record = { move = move; captured = captured } // Create a record for the move

                    { history = record :: state.history
                      board = board
                      toMove = state.toMove.invert }
                | EnPassant (m) -> state // TODO: implement
                | Castle (s) -> state // TODO: implement

            Some(appliedMove)
        else
            None

    let fromAlgebraic (str: string) (state: GameState) : Move option =
        // Constructs the move given in algebraic notation (string).
        // If the string is not valid algebraic notation for the current game state, None is returned.

        let parseRankSymbol (symbol: string) =
            match symbol.ToUpper() with
            | "" -> Some Pawn
            | "N" -> Some Knight
            | "B" -> Some Bishop
            | "R" -> Some Rook
            | "Q" -> Some Queen
            | "K" -> Some King
            | _ -> None

        let parseCol str =
            match str with
            | "a" -> Some 0
            | "b" -> Some 1
            | "c" -> Some 2
            | "d" -> Some 3
            | "e" -> Some 4
            | "f" -> Some 5
            | "g" -> Some 6
            | "h" -> Some 7
            | _ -> None

        let parseRow (str: string) =
            try
                Some((int str) - 1)
            with
            | _ -> None

        let parseCapture (str: String) = str = "x"

        let toPosition col row =
            Option.map2 (fun col row -> { col = col; row = row }) col row


        let findFromPos (fromCol: int option) (fromRow: int option) (dest: Position) (positions: Position list) =
            // Tries to find the position the piece has to come from, potentially using contextual information such as which pieces can make legal moves.

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
                    match (fromCol, fromRow) with
                    | (Some col, Some row) -> Some(createNormalMove { col = col; row = row }) // The full from square is provided
                    | (None, Some row) -> None // Only the row is provided
                    | (Some col, None) -> None // Only the column is provided
                    | _ -> None // Not enough information is known, since otherwise the move would have been picked up earlier

            move

        // Parse the move in algebraic notation. Doesn't ascertain that it is a legal move.
        let m =
            Regex.Match(
                str,
                "(?<rank>[NBRQK]?)(?<col1>[a-h]?)(?<row1>[1-8]?)(?<capture>x?)(?<col2>[a-h])(?<row2>[1-8])"
            )

        let rankSymbol = parseRankSymbol m.Groups["rank"].Value

        let piecePositions =
            rankSymbol
            |> Option.map (fun rankSymbol -> findPiece state.toMove rankSymbol state)

        let fromCol = parseCol m.Groups["col1"].Value
        let fromRow = parseRow m.Groups["row1"].Value
        let destCol = parseCol m.Groups["col2"].Value
        let destRow = parseRow m.Groups["row2"].Value

        let capture = parseCapture m.Groups["capture"].Value

        let move =
            maybe {
                let! rankSymbol = rankSymbol
                let possiblePieces = (findPiece state.toMove rankSymbol state)
                let! c = destCol
                let! r = destRow
                let! pos = findFromPos fromCol fromRow { row = r; col = c } possiblePieces
                return pos
            }

        move

    let toAlgebraic (move: MoveRecord) (state: GameState) : string = "" // TODO: implement

    let createTranscript (state: GameState) : string list =
        // Creates a transcript of algebraic notation as a list of strings with each string being a move.
        state.history
        |> List.map (fun x -> toAlgebraic x state)
