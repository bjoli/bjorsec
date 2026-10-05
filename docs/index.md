# bjorsec

bjorsec is a library of parser combinators for Bjolang, after F#’s FParsec.
A parser is built out of small parsers, each of which reads one thing, and
an error says where the input went wrong and what would have been accepted
there. The [reference](reference.md) has every function, with an
example.

- [Getting it](#getting-it)
- [A first parser](#a-first-parser)
- [Reading text](#reading-text)
- [Putting parsers together](#putting-parsers-together)
- [Repeating](#repeating)
- [Alternatives, and backing up](#alternatives-and-backing-up)
- [Errors worth reading](#errors-worth-reading)
- [Grammars that refer to themselves](#grammars-that-refer-to-themselves)
- [All of it: a calculator](#all-of-it-a-calculator)
- [Speed](#speed)

## Getting it

Add it to your project’s `manifest.bjodat`:

```bjolang
(depends
  (package (name (bjorsec))
           (version (version-at-least "0.3.0"))
           (source (git (url "https://github.com/bjoli/bjorsec")))))
```

and import it:

```bjolang
(import (bjorsec core))
```

## A first parser

A `(Parser %a)` reads from the front of some text and answers an
`%a`. `run-parser` runs one:

```bjolang
(run-parser pint "42")         ; (Ok 42)
(run-parser (pchar #\a) "abc") ; (Ok #\a)
(run-parser pint "x")          ; (Err ...)
```

A parser reads as far as it needs to and no further: `(run-parser pint "42abc")` is `(Ok 42)`. To insist on the whole input, end with
`eof`:

```bjolang
(run-parser (pleft pint eof) "42abc")
;; Parse error at line 1, column 3:
;; 42abc
;;   ^
;; Expecting: end of input
;; Unexpected: 'a'
```

## Reading text

The small parsers read a character, a literal, a run or a number:

```bjolang
(pchar #\=)                               ; one particular character
(pstring "let")                           ; a literal
(any-of "+-*/")                           ; one of these characters
(satisfy char-upper-case? "a capital")    ; any character a test accepts
(many-satisfy1 char-alphabetic? "a word") ; a run of them, as a string
pint pdouble                              ; numbers
spaces                                    ; space, tabs and line breaks
```

## Putting parsers together

Two parsers in a row are combined by what you want from them:

```bjolang
(pright (pchar #\$) pint)                ; "$42" -> 42: keep the right
(pleft pint (pchar #\%))                 ; "42%" -> 42: keep the left
(between (pchar #\() (pchar #\)) pint)   ; "(42)" -> 42
(pboth letter digit)                     ; "a1" -> (Tuple #\a #\1)
(ppipe2 pint (pright (pchar #\x) pint) *) ; "6x7" -> 42
(pmap string-length (many-satisfy char-alphabetic?))   ; "hello" -> 5
```

A token is usually a parser followed by the space after it. Writing that
once keeps the grammar readable:

```bjolang
(: token (-> (Parser %a) (Parser %a)))
(defun (token p) (pleft p spaces))
```

## Repeating

```bjolang
(many (token pint))                        ; "1 2 3" -> '(1 2 3)
(sep-by pint (token (pchar #\,)))          ; "1, 2, 3" -> '(1 2 3)
(many-fold + 0 (token pint))               ; "1 2 3" -> 6, nothing collected
(many-till any-char (pstring "*/"))        ; the body of a comment
```

Operators are `chainl1`: an operand, then any number of operator and
operand, folded from the left. The operator parser answers the function
that combines two operands:

```bjolang
(def minus (pright (pchar #\-) (preturn -)))
(run-parser (chainl1 pint minus) "10-3-2")   ; (Ok 5), as (10-3)-2
```

## Alternatives, and backing up

`por` tries a second parser when the first fails, and a choice that
fails says what all of them wanted:

```bjolang
(def answer (por (pstring "yes") (pstring "no")))
(run-parser answer "maybe")
;; Expecting: 'yes' or 'no'
```

One rule makes the errors good, and it is the one thing to understand about
bjorsec: **`por` tries the second parser only if the first failed
without reading anything**. Once a parser has read part of something, its
error is the one worth reporting:

```bjolang
(def a-then-b (pright (pchar #\a) (pstring "b")))
(run-parser (por a-then-b (pstring "ac")) "ac")
;; Expecting: 'b'. a-then-b read the a, so "ac" is never tried.
```

When an alternative may read and then turn out wrong, wrap it in
`attempt`, which moves back to where it began when it fails:

```bjolang
(run-parser (por (attempt a-then-b) (pstring "ac")) "ac")   ; (Ok "ac")
```

`pstring`, `pint` and `pdouble` already move back by
themselves, so a choice between literals needs no `attempt`:
`(por (pstring "foo") (pstring "far"))` reads `"far"`. Keep
`attempt` around the alternative that needs it and no wider: an error
found inside it is reported where it began.

## Errors worth reading

An error is a position and what was expected there. Name things in the words
your reader uses:

```bjolang
(def name (plabel (many-satisfy1 char-alphabetic? "a letter") "a name"))
(run-parser/message name "42")
;; Parse error at line 1, column 1:
;; 42
;; ^
;; Expecting: a name
;; Unexpected: '4'
```

and check what the grammar alone cannot with `pfail`:

```bjolang
(def byte
  (pbind pint (fun (n) (if (< n 256) (preturn n) (pfail "a byte is at most 255")))))
```

`run-parser/message` gives the message as a string; `run-parser`
gives a `ParseError`, which `println` shows the same way and
`parse-error-position` places.

## Grammars that refer to themselves

An expression contains expressions. A grammar of `def`s refers to a
parser before it is defined with `pforward`:

```bjolang
(: expr-ref (ParserRef int))
(def expr-ref (pforward))

(def factor (por pint (between (pchar #\() (pchar #\)) (pforwarded expr-ref))))
;; ... the rest of the grammar, then:
(pforward-set! expr-ref factor)
```

A grammar of functions uses `plazy` instead, so that building the
parser does not recurse forever:

```bjolang
(defun (expr)
  (por pint (plazy (fun () (between (pchar #\() (pchar #\)) (expr))))))
```

## All of it: a calculator

```bjolang
(import (bjorsec core))

(: token (-> (Parser %a) (Parser %a)))
(defun (token p) (pleft p spaces))

(: op (-> char (-> double double double) (Parser (-> double double double))))
(defun (op c f) (pright (token (pchar c)) (preturn f)))

(: expr-ref (ParserRef double))
(def expr-ref (pforward))

(def factor (por (token pdouble)
                 (between (token (pchar #\()) (token (pchar #\))) (pforwarded expr-ref))))
(def term (chainl1 factor (por (op #\* *) (op #\/ /))))
(def sum (chainl1 term (por (op #\+ +) (op #\- -))))
(def calculator (pright spaces (pleft sum eof)))

(defun (main)
  (pforward-set! expr-ref sum)
  (println (run-parser calculator "2 * (3 + 4)"))   ; (Ok 14)
  (println (run-parser calculator "2 * (3 + 4"))
  ;; Parse error at line 1, column 11:
  ;; 2 * (3 + 4
  ;;           ^
  ;; Expecting: ')'
  ;; Unexpected: end of input
  0)
```

Precedence is one `chainl1` per level: a sum is made of terms, and a
term of factors, so `*` binds tighter than `+`.

## Speed

- **Build a parser once.** Parsers are values. Define a grammar
  with `def` and run it many times, rather than building it again for
  every input.
- **Read runs, not characters.** `many-satisfy` reads a run
  in one step; `many-chars` of a character parser builds the string a
  character at a time. Use the second only when the characters need
  parsing, such as escapes.
- **Slices for long runs.** `many-satisfy/slice` and
  `many-satisfy1/slice` answer a slice of the input instead of a copy:
  a file of 77-byte lines parses 38% faster with half the allocation. A
  slice keeps the whole input alive while it is held, so use them for runs
  that are looked at and dropped, and `string-copy` one to keep it.
