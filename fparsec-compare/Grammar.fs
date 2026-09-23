// The calculator grammar, shared by the benchmark and the golden tests, written
// to mirror bjorsec's bench.bjo / golden.bjo exactly:
//
//   token p    = pleft p spaces
//   sym c      = token (pchar c)
//   number     = token (plabel pdouble "a number")
//   factor     = number | '(' expr ')'
//   mul-op     = ('*' | '/') as a (float -> float -> float)
//   add-op     = ('+' | '-') as a (float -> float -> float)
//   term       = chainl1 factor mul-op
//   sum        = chainl1 term add-op
//   calculator = spaces >>. sum .>> eof
module Grammar

open FParsec

// token p = pleft p spaces
let token (p: Parser<'a, unit>) : Parser<'a, unit> = p .>> spaces

// sym c = token (pchar c)
let sym c : Parser<char, unit> = token (pchar c)

// number = token (plabel pdouble "a number")
let number : Parser<float, unit> = token (pfloat <?> "a number")

// A forward reference so that factor can name expr, as ParserRef does.
let factor, factorRef = createParserForwardedToRef<float, unit>()

let mulOp : Parser<(float -> float -> float), unit> =
    (sym '*' >>% (fun a b -> a * b)) <|> (sym '/' >>% (fun a b -> a / b))

let addOp : Parser<(float -> float -> float), unit> =
    (sym '+' >>% (fun a b -> a + b)) <|> (sym '-' >>% (fun a b -> a - b))

let term : Parser<float, unit> = chainl1 factor mulOp
let sum : Parser<float, unit> = chainl1 term addOp

do factorRef.Value <- (number <|> between (sym '(') (sym ')') sum)

// calculator = pright spaces (pleft sum eof)
let calculator : Parser<float, unit> = spaces >>. sum .>> eof
