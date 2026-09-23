// The FParsec twin of bjorsec's tests/golden.bjo: the same failing and
// succeeding inputs, printed with the same "===== label ... ===== end" framing.
//
// On failure FParsec's own error text is printed (it already renders the line,
// the caret and the "Expecting:" list, though its header reads "Error in Ln: 1
// Col: 4" where bjorsec's reads "Parse error at line 1, column 4:").
module Golden

open FParsec
open Grammar

let private render (r: ParserResult<'a, unit>) : string =
    match r with
    | Success (v, _, _) -> sprintf "OK %s" (string v)
    | Failure (msg, _, _) -> msg.TrimEnd('\n')

let private show (label: string) (out: string) =
    printfn "===== %s" label
    printfn "%s" out
    printfn "===== end"

let private porString : Parser<string, unit> = (pstring "hello") <|> (pstring "world")

let private choiceAbc : Parser<char, unit> = choice [ pchar 'a'; pchar 'b'; pchar 'c' ]

let private minusOp : Parser<(int -> int -> int), unit> =
    pchar '-' >>% (fun a b -> a - b)

let main () : int =
    // The calculator, on failing inputs.
    show "calc: 1 +" (render (run calculator "1 +"))
    show "calc: 1 + * 2" (render (run calculator "1 + * 2"))
    show "calc: (1 + 2" (render (run calculator "(1 + 2"))
    show "calc: empty" (render (run calculator ""))
    show "calc: line 3" (render (run calculator "1 +\n2 +\n* 3"))

    show "por strings on help" (render (run porString "help"))
    show "choice pchar on z" (render (run choiceAbc "z"))
    show "choice pchar on eof" (render (run choiceAbc ""))

    show "plabel" (render (run (pchar 'a' <?> "a greeting") "z"))
    show "pfail" (render (run (fail "nope") "x"))
    show "followed-by" (render (run (pchar 'a' .>> followedBy (pchar 'b')) "ac"))
    show "not-followed-by" (render (run (notFollowedBy (pchar 'b')) "a"))
    show "many-till" (render (run (manyTill anyChar (pchar '!')) "abc"))
    show "eof" (render (run eof "ab"))
    show "pint" (render (run pint32 "x"))
    show "pdouble" (render (run pfloat "x"))

    show "unexpected emoji" (render (run (pchar 'a') "\U0001F600"))
    show "unexpected tab" (render (run (pchar 'a') "\t"))
    show "unexpected newline" (render (run (pchar 'a') "\n"))

    show "attempt backtracks in por"
        (render (run ((attempt (pchar 'a' >>. pchar 'b')) <|> pchar 'a') "ac"))

    show "choice stops after a moving failure"
        (render (run (choice [ pchar 'a' >>. pchar 'b'; pchar 'a' ]) "ax"))
    show "choice merges earlier then stops"
        (render (run (choice [ pchar 'q'; pchar 'a' >>. pchar 'b'; pchar 'a' ]) "ax"))

    // Success values.
    show "ok: calc 1 + 2 * 3" (render (run calculator "1 + 2 * 3"))
    show "ok: calc -2 * 3" (render (run calculator "-2 * 3"))
    show "ok: por hello" (render (run porString "hello"))
    show "ok: pint -42" (render (run pint32 "-42"))
    show "ok: pdouble 3.5e2" (render (run pfloat "3.5e2"))

    // chainr1 over 100000 terms, right associative: 1-1-...-1 = 0.
    let bigInput = String.concat "-" (List.replicate 100000 "1")
    show "ok: chainr1 100000 terms" (render (run (chainr1 pint32 minusOp) bigInput))
    0
