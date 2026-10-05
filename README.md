# bjorsec

Parser combinators for Bjolang, after F#'s FParsec: small parsers, combined
into grammars, with errors that say where the input went wrong and what would
have been accepted there.

```bjolang
(import (bjorsec core))

;; A list of integers: [1, 2, 3]
(: ints (Parser (List int)))
(def ints
  (between (pchar #\[) (pchar #\])
           (sep-by (pleft pint spaces) (pleft (pchar #\,) spaces))))

(run-parser ints "[1, 2, 3]")   ; (Ok '(1 2 3))
(println (run-parser ints "[1, x]"))
;; Parse error at line 1, column 5:
;; [1, x]
;;     ^
;; Expecting: an integer
;; Unexpected: 'x'
```

## Getting it

In your project's `manifest.bjodat`:

```bjolang
(depends
  (package (name (bjorsec))
           (version (version-at-least "0.3.0"))
           (source (git (url "https://github.com/bjoli/bjorsec")))))
```

## Documentation

- [The guide](docs/index.md): reading text, combining parsers, alternatives
  and backing up, errors, recursive grammars, and a whole calculator.
- [The reference](docs/reference.md): every function, with an example.

The pages are written in [samizdat](https://github.com/bjoli/samizdat), in
`docs/*.sz`, and the `.md` files are made from them. After changing a page, or
the docs in `src/core.bjodoc`, `bjo build` here and then, in samizdat:

```sh
bjo run examples/markdown.bjo /path/to/bjorsec/docs/index.sz
bjo run examples/markdown.bjo /path/to/bjorsec/docs/reference.sz
```

## Tests

```sh
bjo run tests/demo.bjo     # the combinators, one by one
bjo run tests/docs.bjo     # every example in the guide and the reference
bjo run tests/golden.bjo   # exact error messages, to diff against a recorded run
```

`fparsec-compare/` runs the same inputs, and the same benchmark as
`bench.bjo`, through FParsec.

## License

MPL-2.0; see [LICENSE](LICENSE).
