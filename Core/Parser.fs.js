import { some, value as value_1 } from "../Web/src/.fable/fable-library.3.2.9/Option.js";
import { substring } from "../Web/src/.fable/fable-library.3.2.9/String.js";
import { parse } from "../Web/src/.fable/fable-library.3.2.9/Int32.js";

export function map(f, parser) {
    const mappedParser = (source) => {
        const patternInput = parser(source);
        const result = patternInput[0];
        const remainingSource = patternInput[1];
        if (result == null) {
            return [void 0, source];
        }
        else {
            const result_1 = value_1(result);
            return [some(f(result_1)), remainingSource];
        }
    };
    return mappedParser;
}

export function bind(f, parser) {
    const boundParser = (source) => {
        const patternInput = parser(source);
        const result = patternInput[0];
        const remainingSource = patternInput[1];
        if (result == null) {
            return [void 0, source];
        }
        else {
            const result_1 = value_1(result);
            return [f(result_1), remainingSource];
        }
    };
    return boundParser;
}

export function opt(a) {
    const parser = (source) => {
        const patternInput = a(source);
        const result = patternInput[0];
        const rest = patternInput[1];
        if (result == null) {
            return [some(void 0), source];
        }
        else {
            const result_1 = value_1(result);
            return [some(some(result_1)), rest];
        }
    };
    return parser;
}

export function op_EqualsGreater(a, b) {
    const parser = (source) => {
        const patternInput = a(source);
        const result = patternInput[0];
        const remainingSource = patternInput[1];
        if (result == null) {
            return [void 0, source];
        }
        else {
            const result_1 = value_1(result);
            const matchValue = b(remainingSource);
            if (matchValue[0] == null) {
                return [void 0, source];
            }
            else {
                const result$0027 = value_1(matchValue[0]);
                const remainingSource$0027 = matchValue[1];
                return [[result_1, result$0027], remainingSource$0027];
            }
        }
    };
    return parser;
}

export function op_PipeRight2(a, b) {
    return map((tupledArg) => {
        const value = tupledArg[1];
        return value;
    }, op_EqualsGreater(a, b));
}

export function op_PipeLeft2(a, b) {
    return map((tupledArg) => {
        const value = tupledArg[0];
        return value;
    }, op_EqualsGreater(a, b));
}

export function op_LessBarGreater(a, a$0027) {
    const parser = (source) => {
        const result = a(source);
        if (result[0] == null) {
            return a$0027(source);
        }
        else {
            const result_1 = value_1(result[0]);
            const remainingSource = result[1];
            return [some(result_1), remainingSource];
        }
    };
    return parser;
}

export function stringParser(str) {
    const parser = (source) => {
        if (source.toLocaleLowerCase().indexOf(str.toLocaleLowerCase()) === 0) {
            return [str, substring(source, str.length)];
        }
        else {
            return [void 0, source];
        }
    };
    return parser;
}

export const digitParser = (() => {
    const parser = (source) => {
        try {
            return [parse(substring(source, 0, 1), 511, false, 32), substring(source, 1)];
        }
        catch (matchValue) {
            return [void 0, source];
        }
    };
    return parser;
})();

export function constant(str, value) {
    return map((_arg1) => value, stringParser(str));
}

