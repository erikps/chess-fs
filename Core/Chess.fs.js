import { join, toText, interpolate, toConsole } from "../Web/src/.fable/fable-library.3.2.9/String.js";
import { Record, toString, Union } from "../Web/src/.fable/fable-library.3.2.9/Types.js";
import { list_type, option_type, int32_type, record_type, bool_type, union_type } from "../Web/src/.fable/fable-library.3.2.9/Reflection.js";
import { int32ToString, equals } from "../Web/src/.fable/fable-library.3.2.9/Util.js";
import { ofSeq, length, filter, tail, cons, forAll, head, isEmpty, choose, allPairs, map as map_1, empty, item, mapIndexed } from "../Web/src/.fable/fable-library.3.2.9/List.js";
import { reverse, map, delay, toList } from "../Web/src/.fable/fable-library.3.2.9/Seq.js";
import { rangeDouble } from "../Web/src/.fable/fable-library.3.2.9/Range.js";
import { op_UnaryNegation_Int32 } from "../Web/src/.fable/fable-library.3.2.9/Int32.js";
import { map as map_2, value as value_1 } from "../Web/src/.fable/fable-library.3.2.9/Option.js";
import { map as map_3, opt, op_EqualsGreater, stringParser, digitParser, bind, constant, op_LessBarGreater } from "./Parser.fs.js";

function log(x) {
    toConsole(interpolate("%A%P()", [x]));
}

function logM(m, x) {
    toConsole(interpolate("%s%P(): %A%P()", [m, x]));
}

export class Rank extends Union {
    constructor(tag, ...fields) {
        super();
        this.tag = (tag | 0);
        this.fields = fields;
    }
    cases() {
        return ["Pawn", "Knight", "Bishop", "Rook", "King", "Queen"];
    }
}

export function Rank$reflection() {
    return union_type("Chess.Rank", [], Rank, () => [[], [], [], [], [], []]);
}

export class Color extends Union {
    constructor(tag, ...fields) {
        super();
        this.tag = (tag | 0);
        this.fields = fields;
    }
    cases() {
        return ["White", "Black"];
    }
}

export function Color$reflection() {
    return union_type("Chess.Color", [], Color, () => [[], []]);
}

export function Color__get_invert(this$) {
    if (equals(this$, new Color(0))) {
        return new Color(1);
    }
    else {
        return new Color(0);
    }
}

export class Piece extends Record {
    constructor(rank, color, hasMoved) {
        super();
        this.rank = rank;
        this.color = color;
        this.hasMoved = hasMoved;
    }
    toString() {
        const this$ = this;
        return toText(interpolate("(%s%P() %s%P())", [toString(this$.color), toString(this$.rank)]));
    }
}

export function Piece$reflection() {
    return record_type("Chess.Piece", [], Piece, () => [["rank", Rank$reflection()], ["color", Color$reflection()], ["hasMoved", bool_type]]);
}

export class Position extends Record {
    constructor(row, col) {
        super();
        this.row = (row | 0);
        this.col = (col | 0);
    }
    toString() {
        const this$ = this;
        return toText(interpolate("(%d%P(),%d%P())", [this$.col, this$.row]));
    }
}

export function Position$reflection() {
    return record_type("Chess.Position", [], Position, () => [["row", int32_type], ["col", int32_type]]);
}

export function Position_get_zero() {
    return new Position(0, 0);
}

export function Position__get_inBounds(this$) {
    if (((this$.row >= 0) ? (this$.row < 8) : false) ? (this$.col >= 0) : false) {
        return this$.col < 8;
    }
    else {
        return false;
    }
}

export function Position__get_abs(this$) {
    return new Position(Math.abs(this$.row), Math.abs(this$.col));
}

export function Position_op_Addition_Z4FCD0E80(this$, other) {
    return new Position(this$.row + other.row, this$.col + other.col);
}

export function Position_op_Subtraction_Z4FCD0E80(this$, other) {
    return new Position(this$.row - other.row, this$.col - other.col);
}

export function Position__get_normalised(this$) {
    return new Position((this$.row === 0) ? 0 : (~(~(this$.row / Math.abs(this$.row)))), (this$.col === 0) ? 0 : (~(~(this$.col / Math.abs(this$.col)))));
}

export class CastleSide extends Union {
    constructor(tag, ...fields) {
        super();
        this.tag = (tag | 0);
        this.fields = fields;
    }
    cases() {
        return ["QueenSide", "KingSide"];
    }
}

export function CastleSide$reflection() {
    return union_type("Chess.CastleSide", [], CastleSide, () => [[], []]);
}

export class NormalMove extends Record {
    constructor(from, dest) {
        super();
        this.from = from;
        this.dest = dest;
    }
}

export function NormalMove$reflection() {
    return record_type("Chess.NormalMove", [], NormalMove, () => [["from", Position$reflection()], ["dest", Position$reflection()]]);
}

export class EnPassantMove extends Record {
    constructor(from) {
        super();
        this.from = from;
    }
}

export function EnPassantMove$reflection() {
    return record_type("Chess.EnPassantMove", [], EnPassantMove, () => [["from", Position$reflection()]]);
}

export class Move extends Union {
    constructor(tag, ...fields) {
        super();
        this.tag = (tag | 0);
        this.fields = fields;
    }
    cases() {
        return ["Normal", "EnPassant", "Castle"];
    }
}

export function Move$reflection() {
    return union_type("Chess.Move", [], Move, () => [[["Item", NormalMove$reflection()]], [["Item", EnPassantMove$reflection()]], [["Item", CastleSide$reflection()]]]);
}

export class MoveRecord extends Record {
    constructor(move, moved, captured) {
        super();
        this.move = move;
        this.moved = moved;
        this.captured = captured;
    }
}

export function MoveRecord$reflection() {
    return record_type("Chess.MoveRecord", [], MoveRecord, () => [["move", Move$reflection()], ["moved", Piece$reflection()], ["captured", option_type(Piece$reflection())]]);
}

export class GameState extends Record {
    constructor(board, history, toMove, capturedPieces) {
        super();
        this.board = board;
        this.history = history;
        this.toMove = toMove;
        this.capturedPieces = capturedPieces;
    }
}

export function GameState$reflection() {
    return record_type("Chess.GameState", [], GameState, () => [["board", list_type(list_type(option_type(Piece$reflection())))], ["history", list_type(MoveRecord$reflection())], ["toMove", Color$reflection()], ["capturedPieces", list_type(Piece$reflection())]]);
}

export function rankString(piece) {
    const matchValue = piece.rank;
    switch (matchValue.tag) {
        case 1: {
            return "N";
        }
        case 2: {
            return "B";
        }
        case 3: {
            return "R";
        }
        case 5: {
            return "Q";
        }
        case 4: {
            return "K";
        }
        default: {
            return "";
        }
    }
}

export class AtomicMove extends Record {
    constructor(square, position) {
        super();
        this.square = square;
        this.position = position;
    }
}

export function AtomicMove$reflection() {
    return record_type("Chess.AtomicMove", [], AtomicMove, () => [["square", option_type(Piece$reflection())], ["position", Position$reflection()]]);
}

export class AtomicRecord extends Record {
    constructor(move, replaced) {
        super();
        this.move = move;
        this.replaced = replaced;
    }
}

export function AtomicRecord$reflection() {
    return record_type("Chess.AtomicRecord", [], AtomicRecord, () => [["move", AtomicMove$reflection()], ["replaced", option_type(Piece$reflection())]]);
}

export class MoveRecord$0027 extends Record {
    constructor(moves, captured) {
        super();
        this.moves = moves;
        this.captured = captured;
    }
}

export function MoveRecord$0027$reflection() {
    return record_type("Chess.MoveRecord\u0027", [], MoveRecord$0027, () => [["moves", AtomicMove$reflection()], ["captured", option_type(Piece$reflection())]]);
}

function setSquare(_arg1, s, board) {
    const row = _arg1.row | 0;
    const col = _arg1.col | 0;
    const setAtIndex = (array, index, element) => mapIndexed((i, a) => {
        if (i === index) {
            return element;
        }
        else {
            return a;
        }
    }, array);
    return setAtIndex(board, row, setAtIndex(item(row, board), col, s));
}

export function getSquare(pos, board) {
    return item(pos.col, item(pos.row, board));
}

const initialBoard = (() => {
    const colToRank = (colIndex) => {
        if (!((colIndex >= 0) ? (colIndex < 8) : false)) {
            debugger;
        }
        switch (colIndex) {
            case 0:
            case 7: {
                return new Rank(3);
            }
            case 1:
            case 6: {
                return new Rank(1);
            }
            case 2:
            case 5: {
                return new Rank(2);
            }
            case 3: {
                return new Rank(5);
            }
            case 4: {
                return new Rank(4);
            }
            default: {
                throw (new Error("Invalid column number."));
            }
        }
    };
    const positionToPiece = (rowIndex, colIndex_1) => {
        if (!((colIndex_1 >= 0) ? (colIndex_1 < 8) : false)) {
            debugger;
        }
        if (!((rowIndex >= 0) ? (rowIndex < 8) : false)) {
            debugger;
        }
        const createPiece = (rank, color) => (new Piece(rank, color, false));
        switch (rowIndex) {
            case 0: {
                return createPiece(colToRank(colIndex_1), new Color(0));
            }
            case 1: {
                return createPiece(new Rank(0), new Color(0));
            }
            case 6: {
                return createPiece(new Rank(0), new Color(1));
            }
            case 7: {
                return createPiece(colToRank(colIndex_1), new Color(1));
            }
            default: {
                return void 0;
            }
        }
    };
    return toList(delay(() => map((row) => toList(delay(() => map((col) => positionToPiece(row, col), toList(rangeDouble(0, 1, 7))))), toList(rangeDouble(0, 1, 7)))));
})();

export const initialGameState = new GameState(initialBoard, empty(), new Color(0), empty());

export const allPositions = map_1((tupledArg) => {
    const col = tupledArg[0] | 0;
    const row = tupledArg[1] | 0;
    return new Position(row, col);
}, allPairs(toList(rangeDouble(0, 1, 7)), toList(rangeDouble(0, 1, 7))));

export function findPieces(color, rank, state) {
    return choose((tupledArg) => {
        let piece;
        const square = tupledArg[0];
        const pos_1 = tupledArg[1];
        let pattern_matching_result;
        if (square != null) {
            if ((piece = square, equals(piece.color, color) ? equals(piece.rank, rank) : false)) {
                pattern_matching_result = 0;
            }
            else {
                pattern_matching_result = 1;
            }
        }
        else {
            pattern_matching_result = 1;
        }
        switch (pattern_matching_result) {
            case 0: {
                return pos_1;
            }
            case 1: {
                return void 0;
            }
        }
    }, map_1((pos) => [getSquare(pos, state.board), pos], allPositions));
}

function MoveModule_isObstructed(direction, dest, from, state) {
    if (!(Position__get_inBounds(from) ? Position__get_inBounds(dest) : false)) {
        debugger;
    }
    const checkNextSquare = (pos_mut) => {
        checkNextSquare:
        while (true) {
            const pos = pos_mut;
            toConsole(interpolate("%A%P()", [pos]));
            if (Position__get_inBounds(pos)) {
                if (equals(pos, dest)) {
                    return true;
                }
                else {
                    const matchValue = getSquare(pos, state.board);
                    if (matchValue == null) {
                        pos_mut = Position_op_Addition_Z4FCD0E80(pos, direction);
                        continue checkNextSquare;
                    }
                    else {
                        return false;
                    }
                }
            }
            else {
                return false;
            }
            break;
        }
    };
    return !checkNextSquare(Position_op_Addition_Z4FCD0E80(from, direction));
}

function MoveModule_isDiagonalMove(from, dest, state) {
    const difference = Position_op_Subtraction_Z4FCD0E80(dest, from);
    const obstructed = () => MoveModule_isObstructed(Position__get_normalised(difference), dest, from, state);
    if (Position__get_abs(difference).col === Position__get_abs(difference).row) {
        return !obstructed();
    }
    else {
        return false;
    }
}

function MoveModule_isStraightMove(from, dest, state) {
    const difference = Position_op_Subtraction_Z4FCD0E80(dest, from);
    const obstructed = () => MoveModule_isObstructed(Position__get_normalised(difference), dest, from, state);
    if ((difference.col === 0) ? true : (difference.row === 0)) {
        return !obstructed();
    }
    else {
        return false;
    }
}

function MoveModule_isKingMove(from, dest) {
    const d = Position_op_Subtraction_Z4FCD0E80(from, dest);
    if ((Position__get_abs(d).col === 1) ? true : (Position__get_abs(d).col === 0)) {
        if (Position__get_abs(d).row === 1) {
            return true;
        }
        else {
            return Position__get_abs(d).row === 0;
        }
    }
    else {
        return false;
    }
}

function MoveModule_isKnightMove(from, dest) {
    const d = Position_op_Subtraction_Z4FCD0E80(from, dest);
    if ((Position__get_abs(d).row === 2) ? (Position__get_abs(d).col === 1) : false) {
        return true;
    }
    else if (Position__get_abs(d).col === 2) {
        return Position__get_abs(d).row === 1;
    }
    else {
        return false;
    }
}

function MoveModule_pawnDirection(color) {
    if (equals(color, new Color(0))) {
        return 1;
    }
    else {
        return -1;
    }
}

function MoveModule_isPawnDoubleMove(origin, dest, state) {
    const square = getSquare(origin, state.board);
    if (square == null) {
        return false;
    }
    else {
        const piece = square;
        const delta = Position_op_Subtraction_Z4FCD0E80(dest, origin);
        const direction = MoveModule_pawnDirection(piece.color) | 0;
        const captures = getSquare(dest, state.board) != null;
        let blocked;
        const skippedPos = new Position(dest.row - direction, dest.col);
        blocked = (getSquare(skippedPos, state.board) != null);
        if (((delta.row === (direction * 2)) ? (!piece.hasMoved) : false) ? (!captures) : false) {
            return !blocked;
        }
        else {
            return false;
        }
    }
}

function MoveModule_isAnyPawnMove(origin, dest, state) {
    const sq = getSquare(origin, state.board);
    if (sq == null) {
        return false;
    }
    else {
        const piece = sq;
        const d = Position_op_Subtraction_Z4FCD0E80(dest, origin);
        const direction = MoveModule_pawnDirection(piece.color) | 0;
        const captures = getSquare(dest, state.board) != null;
        const isNormalMove = (d.row === direction) ? (!captures) : false;
        const isCaptureMove = ((Position__get_abs(d).col === 1) ? (d.row === direction) : false) ? captures : false;
        const isDoubleMove = MoveModule_isPawnDoubleMove(origin, dest, state);
        if ((d.col === 0) ? (isNormalMove ? true : isDoubleMove) : false) {
            return true;
        }
        else {
            return isCaptureMove;
        }
    }
}

function MoveModule_enPassantDestination(lastMove, color) {
    return new Position(lastMove.dest.row - MoveModule_pawnDirection(color), lastMove.dest.col);
}

export function MoveModule_isLegal(move, state) {
    const get$ = (pos) => getSquare(pos, state.board);
    const hasSameColor = (sq1, sq2) => {
        let c2, c1;
        const matchValue = [sq1, sq2];
        let pattern_matching_result;
        if (matchValue[0] != null) {
            if (matchValue[1] != null) {
                if ((c2 = matchValue[1], (c1 = matchValue[0], equals(c1.color, c2.color)))) {
                    pattern_matching_result = 0;
                }
                else {
                    pattern_matching_result = 1;
                }
            }
            else {
                pattern_matching_result = 1;
            }
        }
        else {
            pattern_matching_result = 1;
        }
        switch (pattern_matching_result) {
            case 0: {
                return true;
            }
            case 1: {
                return false;
            }
        }
    };
    const isPseudoLegalMove = (move_1) => {
        let color;
        switch (move_1.tag) {
            case 1: {
                const m_1 = move_1.fields[0];
                const matchValue_2 = state.history;
                if (isEmpty(matchValue_2)) {
                    return false;
                }
                else {
                    const last = head(matchValue_2);
                    const matchValue_3 = last.move;
                    if (matchValue_3.tag === 0) {
                        const lastMove = matchValue_3.fields[0];
                        const prevMoveDelta = Position_op_Subtraction_Z4FCD0E80(lastMove.dest, lastMove.from);
                        const prevMoveDirection = MoveModule_pawnDirection(Color__get_invert(state.toMove)) | 0;
                        const isPrevMoveValid = (equals(last.moved.rank, new Rank(0)) ? (prevMoveDelta.row === (prevMoveDirection * 2)) : false) ? (prevMoveDelta.col === 0) : false;
                        const enPassantDest = new Position(lastMove.dest.row - prevMoveDirection, lastMove.dest.col);
                        const enPassantDelta = Position_op_Subtraction_Z4FCD0E80(enPassantDest, m_1.from);
                        const isEnPassantDeltaValid = (enPassantDelta.row === op_UnaryNegation_Int32(prevMoveDirection)) ? (Math.abs(enPassantDelta.col) === 1) : false;
                        const matchValue_4 = get$(m_1.from);
                        if (matchValue_4 == null) {
                            return false;
                        }
                        else {
                            const piece_2 = matchValue_4;
                            if (isPrevMoveValid ? equals(piece_2.rank, new Rank(0)) : false) {
                                return isEnPassantDeltaValid;
                            }
                            else {
                                return false;
                            }
                        }
                    }
                    else {
                        return false;
                    }
                }
            }
            case 2: {
                const castleSide = move_1.fields[0];
                const didKingMove = forAll((piece_3) => piece_3.hasMoved, choose((pos_1) => getSquare(pos_1, state.board), findPieces(state.toMove, new Rank(4), state)));
                const row = (equals(state.toMove, new Color(0)) ? 0 : 7) | 0;
                const isFree = (column) => (getSquare(new Position(row, column), state.board) == null);
                const rookHasNotMoved = (column_1) => {
                    let c;
                    const matchValue_5 = getSquare(new Position(row, column_1), state.board);
                    let pattern_matching_result_1;
                    if (matchValue_5 != null) {
                        if (matchValue_5.rank.tag === 3) {
                            if (matchValue_5.hasMoved) {
                                pattern_matching_result_1 = 1;
                            }
                            else if ((c = matchValue_5.color, equals(c, state.toMove))) {
                                pattern_matching_result_1 = 0;
                            }
                            else {
                                pattern_matching_result_1 = 1;
                            }
                        }
                        else {
                            pattern_matching_result_1 = 1;
                        }
                    }
                    else {
                        pattern_matching_result_1 = 1;
                    }
                    switch (pattern_matching_result_1) {
                        case 0: {
                            return true;
                        }
                        case 1: {
                            return false;
                        }
                    }
                };
                const isLegalCastle = (freeColumns, rookColumn) => {
                    if (freeColumns.every(isFree)) {
                        return rookHasNotMoved(rookColumn);
                    }
                    else {
                        return false;
                    }
                };
                if (!didKingMove) {
                    if (castleSide.tag === 1) {
                        return isLegalCastle(new Int32Array([5, 6]), 7);
                    }
                    else {
                        return isLegalCastle(new Int32Array([1, 2, 3]), 0);
                    }
                }
                else {
                    return false;
                }
            }
            default: {
                const m = move_1.fields[0];
                const piece = get$(m.from);
                const possibleCapture = get$(m.dest);
                if (piece != null) {
                    if ((color = piece.color, !equals(color, state.toMove))) {
                        const color_1 = piece.color;
                        return false;
                    }
                    else {
                        let pattern_matching_result_2;
                        if (piece != null) {
                            if (hasSameColor(piece, possibleCapture)) {
                                pattern_matching_result_2 = 0;
                            }
                            else {
                                pattern_matching_result_2 = 1;
                            }
                        }
                        else {
                            pattern_matching_result_2 = 1;
                        }
                        switch (pattern_matching_result_2) {
                            case 0: {
                                return false;
                            }
                            case 1: {
                                if (piece != null) {
                                    const piece_1 = piece;
                                    const matchValue_1 = piece_1.rank;
                                    switch (matchValue_1.tag) {
                                        case 1: {
                                            return MoveModule_isKnightMove(m.from, m.dest);
                                        }
                                        case 2: {
                                            return MoveModule_isDiagonalMove(m.from, m.dest, state);
                                        }
                                        case 3: {
                                            return MoveModule_isStraightMove(m.from, m.dest, state);
                                        }
                                        case 5: {
                                            if (MoveModule_isStraightMove(m.from, m.dest, state)) {
                                                return true;
                                            }
                                            else {
                                                return MoveModule_isDiagonalMove(m.from, m.dest, state);
                                            }
                                        }
                                        case 4: {
                                            return MoveModule_isKingMove(m.from, m.dest);
                                        }
                                        default: {
                                            return MoveModule_isAnyPawnMove(m.from, m.dest, state);
                                        }
                                    }
                                }
                                else {
                                    throw (new Error("Match failure"));
                                }
                            }
                        }
                    }
                }
                else {
                    return false;
                }
            }
        }
    };
    return isPseudoLegalMove(move);
}

export function MoveModule_apply(move, state) {
    let board_4, lastMovedPiece, lastMove;
    if (MoveModule_isLegal(move, state)) {
        switch (move.tag) {
            case 1: {
                const currentMove = move.fields[0];
                const matchValue = state.history;
                let pattern_matching_result, lastMove_1, lastMovedPiece_1;
                if (!isEmpty(matchValue)) {
                    if (head(matchValue).move.tag === 0) {
                        if ((lastMovedPiece = head(matchValue).moved, (lastMove = head(matchValue).move.fields[0], equals(lastMovedPiece.rank, new Rank(0)) ? (Position__get_abs(Position_op_Subtraction_Z4FCD0E80(lastMove.dest, lastMove.from)).row === 2) : false))) {
                            pattern_matching_result = 0;
                            lastMove_1 = head(matchValue).move.fields[0];
                            lastMovedPiece_1 = head(matchValue).moved;
                        }
                        else {
                            pattern_matching_result = 1;
                        }
                    }
                    else {
                        pattern_matching_result = 1;
                    }
                }
                else {
                    pattern_matching_result = 1;
                }
                switch (pattern_matching_result) {
                    case 0: {
                        const matchValue_1 = getSquare(currentMove.from, state.board);
                        if (matchValue_1 != null) {
                            const movingPawn = matchValue_1;
                            const captured_1 = lastMovedPiece_1;
                            const newRecord = new MoveRecord(new Move(1, currentMove), movingPawn, lastMovedPiece_1);
                            const board_6 = setSquare(currentMove.from, void 0, (board_4 = setSquare(lastMove_1.dest, void 0, state.board), setSquare(MoveModule_enPassantDestination(lastMove_1, lastMovedPiece_1.color), movingPawn, board_4)));
                            return new GameState(board_6, cons(newRecord, state.history), Color__get_invert(state.toMove), cons(captured_1, state.capturedPieces));
                        }
                        else {
                            return void 0;
                        }
                    }
                    case 1: {
                        return void 0;
                    }
                }
            }
            case 2: {
                const s_4 = move.fields[0];
                const row = (equals(state.toMove, new Color(0)) ? 0 : 7) | 0;
                const kingOrigin = new Position(row, 4);
                let rookOrigin;
                const col = (equals(s_4, new CastleSide(0)) ? 0 : 7) | 0;
                rookOrigin = (new Position(row, col));
                let kingDestination;
                const col_1 = (equals(s_4, new CastleSide(0)) ? 2 : 6) | 0;
                kingDestination = (new Position(row, col_1));
                let rookDestination;
                const col_2 = (equals(s_4, new CastleSide(0)) ? 3 : 5) | 0;
                rookDestination = (new Position(row, col_2));
                const king = getSquare(kingOrigin, state.board);
                const rook = getSquare(rookOrigin, state.board);
                const board_11 = setSquare(rookDestination, rook, setSquare(kingDestination, king, setSquare(rookOrigin, void 0, setSquare(kingOrigin, void 0, state.board))));
                const record_1 = new MoveRecord(move, value_1(king), void 0);
                return new GameState(board_11, cons(record_1, state.history), Color__get_invert(state.toMove), state.capturedPieces);
            }
            default: {
                const from = move.fields[0].from;
                const dest = move.fields[0].dest;
                const captured = getSquare(dest, state.board);
                const movedPiece = map_2((p) => (new Piece(p.rank, p.color, true)), getSquare(from, state.board));
                const board_2 = setSquare(dest, movedPiece, setSquare(from, void 0, state.board));
                if (movedPiece == null) {
                    return void 0;
                }
                else {
                    const moved = movedPiece;
                    const record = new MoveRecord(move, moved, captured);
                    let capturedPieces;
                    if (captured == null) {
                        capturedPieces = state.capturedPieces;
                    }
                    else {
                        const piece = captured;
                        capturedPieces = cons(piece, state.capturedPieces);
                    }
                    const newState = new GameState(board_2, cons(record, state.history), Color__get_invert(state.toMove), capturedPieces);
                    return newState;
                }
            }
        }
    }
    else {
        return void 0;
    }
}

export function MoveModule_revertLast(state) {
    const matchValue = state.history;
    if (isEmpty(matchValue)) {
        return state;
    }
    else {
        const lastMoveRecord = head(matchValue);
        const matchValue_1 = lastMoveRecord.move;
        if (matchValue_1.tag === 0) {
            const move = matchValue_1.fields[0];
            const board_2 = setSquare(move.dest, lastMoveRecord.captured, setSquare(move.from, lastMoveRecord.moved, state.board));
            const toMove = Color__get_invert(state.toMove);
            return new GameState(board_2, tail(state.history), toMove, state.capturedPieces);
        }
        else {
            return state;
        }
    }
}

export function MoveModule_fromAlgebraic(str, state) {
    const reversed = join("", map((value) => value, reverse(str.split(""))));
    const interpret = (rank, possibleCol, possibleRow, dest, isCapture) => {
        let row_1, col_1, col, row;
        const positions = findPieces(state.toMove, rank, state);
        const legalFromPositions = filter((pos) => MoveModule_isLegal(new Move(0, new NormalMove(pos, dest)), state), positions);
        let move;
        const createNormalMove = (from) => (new Move(0, new NormalMove(from, dest)));
        if (length(legalFromPositions) === 1) {
            move = createNormalMove(head(legalFromPositions));
        }
        else {
            const matchValue = [possibleCol, possibleRow];
            move = ((matchValue[0] == null) ? ((matchValue[1] != null) ? ((row_1 = (matchValue[1] | 0), void 0)) : (void 0)) : ((matchValue[1] == null) ? ((col_1 = (matchValue[0] | 0), void 0)) : ((col = (matchValue[0] | 0), (row = (matchValue[1] | 0), createNormalMove(new Position(row, col)))))));
        }
        return move;
    };
    const rankSymbolParser = op_LessBarGreater(op_LessBarGreater(op_LessBarGreater(op_LessBarGreater(op_LessBarGreater(op_LessBarGreater(constant("N", new Rank(1)), constant("B", new Rank(2))), constant("R", new Rank(3))), constant("Q", new Rank(5))), constant("K", new Rank(4))), constant("P", new Rank(0))), constant("", new Rank(0)));
    const columnParser = op_LessBarGreater(op_LessBarGreater(op_LessBarGreater(op_LessBarGreater(op_LessBarGreater(op_LessBarGreater(op_LessBarGreater(constant("a", 0), constant("b", 1)), constant("c", 2)), constant("d", 3)), constant("e", 4)), constant("f", 5)), constant("g", 6)), constant("h", 7));
    const rowParser = bind((digit) => (((digit > 0) ? (digit < 8) : false) ? (digit - 1) : (void 0)), digitParser);
    const captureParser = stringParser("x");
    const optionalSquareParser = op_EqualsGreater(opt(rowParser), opt(columnParser));
    const squareParser = map_3((tupledArg) => {
        const row_2 = tupledArg[0] | 0;
        const col_2 = tupledArg[1] | 0;
        return new Position(row_2, col_2);
    }, op_EqualsGreater(rowParser, columnParser));
    const parser_2 = bind((tupledArg_1) => {
        const _arg1 = tupledArg_1[0];
        const rank_1 = tupledArg_1[1];
        const row_3 = _arg1[1][0];
        const pos_1 = _arg1[0][0];
        const col_3 = _arg1[1][1];
        const capture = _arg1[0][1];
        return interpret(rank_1, col_3, row_3, pos_1, capture != null);
    }, op_EqualsGreater(op_EqualsGreater(op_EqualsGreater(squareParser, opt(captureParser)), optionalSquareParser), rankSymbolParser));
    const res = parser_2(reversed)[0];
    return res;
}

export function MoveModule_toAlgebraic(record, state) {
    const colSymbol = (col) => {
        switch (col) {
            case 0: {
                return "a";
            }
            case 1: {
                return "b";
            }
            case 2: {
                return "c";
            }
            case 3: {
                return "d";
            }
            case 4: {
                return "e";
            }
            case 5: {
                return "f";
            }
            case 6: {
                return "g";
            }
            case 7: {
                return "h";
            }
            default: {
                throw (new Error("Invalid column index."));
            }
        }
    };
    const captureSymbol = (record.captured != null) ? "x" : "";
    const matchValue = record.move;
    switch (matchValue.tag) {
        case 1: {
            const move_1 = matchValue.fields[0];
            const fromCol = colSymbol(move_1.from.col);
            const matchValue_1 = state.history;
            let pattern_matching_result, move_2, moved;
            if (!isEmpty(matchValue_1)) {
                if (!isEmpty(tail(matchValue_1))) {
                    if (head(tail(matchValue_1)).move.tag === 0) {
                        pattern_matching_result = 0;
                        move_2 = head(tail(matchValue_1)).move.fields[0];
                        moved = head(tail(matchValue_1)).moved;
                    }
                    else {
                        pattern_matching_result = 1;
                    }
                }
                else {
                    pattern_matching_result = 1;
                }
            }
            else {
                pattern_matching_result = 1;
            }
            switch (pattern_matching_result) {
                case 0: {
                    const destination = MoveModule_enPassantDestination(move_2, moved.color);
                    return toText(interpolate("%P()x%P()%P()", [fromCol, colSymbol(destination.col), destination.row + 1]));
                }
                case 1: {
                    return void 0;
                }
            }
        }
        case 2: {
            const side = matchValue.fields[0];
            return equals(side, new CastleSide(0)) ? "O-O-O" : "O-O";
        }
        default: {
            const move = matchValue.fields[0];
            const possibleConflictingPieces = equals(record.moved.rank, new Rank(0)) ? empty() : filter((pos_1) => MoveModule_isLegal(new Move(0, new NormalMove(pos_1, move.dest)), state), filter((pos) => (!equals(pos, move.from)), findPieces(record.moved.color, record.moved.rank, state)));
            const possibleConflictingPiecesWithSameCol = filter((_arg1) => {
                const row = _arg1.row | 0;
                return row === move.from.row;
            }, possibleConflictingPieces);
            const possibleConflictingPiecesWithSameRow = filter((_arg2) => {
                const col_1 = _arg2.col | 0;
                return col_1 === move.from.col;
            }, possibleConflictingPiecesWithSameCol);
            const colRequired = (equals(record.moved.rank, new Rank(0)) ? (record.captured != null) : false) ? true : (length(possibleConflictingPiecesWithSameCol) < length(possibleConflictingPieces));
            const rowRequired = (!equals(record.moved.rank, new Rank(0))) ? (length(possibleConflictingPiecesWithSameRow) < length(possibleConflictingPieces)) : false;
            const fromColStr = colRequired ? colSymbol(move.from.col) : "";
            let fromRowStr;
            if (rowRequired) {
                let copyOfStruct = move.from.row + 1;
                fromRowStr = int32ToString(copyOfStruct);
            }
            else {
                fromRowStr = "";
            }
            const fromStr = toText(interpolate("%s%P()%s%P()", [fromColStr, fromRowStr]));
            return toText(interpolate("%s%P()%s%P()%s%P()%s%P()%d%P()", [rankString(record.moved), fromStr, captureSymbol, colSymbol(move.dest.col), move.dest.row + 1]));
        }
    }
}

export function MoveModule_createTranscript(state) {
    return choose((x) => MoveModule_toAlgebraic(x, state), ofSeq(reverse(state.history)));
}

export function MoveModule_fromInputPositions(origin, destination, board) {
    const square = getSquare(origin, board);
    const delta = Position__get_abs(Position_op_Subtraction_Z4FCD0E80(destination, origin));
    const normal = () => (new Move(0, new NormalMove(origin, destination)));
    if (square == null) {
        return normal();
    }
    else {
        const piece = square;
        let pattern_matching_result;
        if (piece.rank.tag === 4) {
            if ((delta.col > 1) ? true : (delta.row > 1)) {
                pattern_matching_result = 0;
            }
            else {
                pattern_matching_result = 1;
            }
        }
        else {
            pattern_matching_result = 1;
        }
        switch (pattern_matching_result) {
            case 0: {
                return new Move(2, (destination.col < 4) ? (new CastleSide(0)) : (new CastleSide(1)));
            }
            case 1: {
                let pattern_matching_result_1;
                if (piece.rank.tag === 0) {
                    if ((delta.col !== 0) ? (getSquare(destination, board) == null) : false) {
                        pattern_matching_result_1 = 0;
                    }
                    else {
                        pattern_matching_result_1 = 1;
                    }
                }
                else {
                    pattern_matching_result_1 = 1;
                }
                switch (pattern_matching_result_1) {
                    case 0: {
                        return new Move(1, new EnPassantMove(origin));
                    }
                    case 1: {
                        return normal();
                    }
                }
            }
        }
    }
}

