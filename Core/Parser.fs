module Parser


type Parser<'a> = string -> 'a option * string

let map f (parser: Parser<'a>) : Parser<'b> =
    // Map the result of the parser to another value of any type
    let mappedParser source =
        let result, remainingSource = parser source

        match result with
        | Some result -> Some(f result), remainingSource
        | None -> None, source

    mappedParser

let bind f (parser: Parser<'a>) : 'b Parser =
    let boundParser source =
        let result, remainingSource = parser source

        match result with
        | Some result -> f result, remainingSource
        | None -> None, source

    boundParser

let opt (a: 'a Parser) : 'a option Parser =
    let parser source =
        let result, rest = a source

        match result with
        | Some result -> Some(Some result), rest
        | None -> Some None, source

    parser

let (=>) (a: 'a Parser) (b: 'b Parser) : ('a * 'b) Parser =
    // Apply the first parser and then the second parser. If one of them failes, the entire parser fails.
    let parser (source: string) =
        let result, remainingSource = a source

        match result with
        | Some result ->
            match b remainingSource with
            | Some result', remainingSource' -> Some(result, result'), remainingSource'
            | None, _ -> None, source
        | None -> None, source

    parser

let (||>) (a: 'a Parser) (b: 'b Parser) : 'b Parser = map (fun (_, value) -> value) (a => b)

let (<||) (a: 'a Parser) (b: 'b Parser) : 'a Parser = map (fun (value, _) -> value) (a => b)


let (<|>) (a: 'a Parser) (a': 'a Parser) : 'a Parser =
    // Aplly parser a, if that fails, apply parser a'.
    let parser (source: string) =
        let result = a source

        match result with
        | Some result, remainingSource -> Some result, remainingSource
        | None, _ -> a' source

    parser


let stringParser (str: string) : string Parser =
    // Parses the string provided

    let parser (source: string) =
        if source.ToLower().StartsWith(str.ToLower()) then
            Some str, source.Substring(str.Length)
        else
            None, source

    parser

let digitParser: int Parser =
    // Parses a digit
    let parser (source: string) =
        try
            Some(System.Int32.Parse(source.Substring(0, 1))), source.Substring(1)
        with
        | _ -> None, source

    parser

let constant str (value: 'a) : Parser<'a> = map (fun _ -> value) (stringParser str)
