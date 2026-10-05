# bjorsec: the reference

[Reference](#reference)

## Overview

Parser combinators after FParsec: small parsers, combined into grammars, with errors that say where and what.

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

A `(Parser %a)` reads from the front of some text and answers an
`%a`, or fails. Small parsers that read one thing — a character
(`pchar`), a literal (`pstring`), a number (`pint`) — are
combined into bigger ones: in sequence (`pright`, `ppipe2`),
as alternatives (`por`, `choice`), repeated (`many`,
`sep-by`), with operators (`chainl1`). A grammar is a
parser like any other, and `run-parser` runs one on a string.

**Consuming.** A parser moves through the text as it reads, and
one that fails may already have moved. Whether it did is what everything
here turns on. `por` tries its second parser only when the first
failed *without* reading anything: once a parser has read part of
something, its error is the one worth reporting, not the next
alternative’s complaint that it does not fit either. When a grammar
does need to back up and try again, it says so with `attempt`.
`pstring`, `pint` and `pdouble` back up by themselves,
so a choice between literals needs no `attempt`.

**Errors.** A failure records where it happened and what was
expected there. Two failures at the same place combine, so a choice
reports every alternative it tried: `Expecting: 'yes' or 'no'`.
`plabel` names what a parser expects in the reader’s words,
`pfail` says something else entirely, and the error printed by
`run-parser/message` shows the line with a caret under the column.
Columns count characters, not bytes.

**The end.** `run-parser` stops where the grammar stops and
does not mind text after it. To insist on all of it, end the grammar
with `eof`.

**Recursive grammars.** A grammar of `defun`s may call itself
freely. One built of `def`s needs a parser to refer to before it is
defined: `pforward` makes the reference, `pforwarded` uses it,
and `pforward-set!` fills it in. `plazy` is the other way.

**Copies and slices.** Text a parser answers is copied out of the
input, except from `many-satisfy/slice` and
`many-satisfy1/slice`, which answer slices of it: faster for long
runs, but each one keeps the whole input alive while it is held.

See also: [`run-parser`](#run-parser), [`por`](#por), [`attempt`](#attempt), [`Parser`](#parser)

The [guide](index.md) walks through the library in order:
reading text, combining parsers, alternatives and backing up, errors,
recursive grammars, and a whole calculator.

## Reference

[**Types**](#types)

- [`ErrorItem`](#erroritem)
- [`ErrorItems`](#erroritems)
- [`Failure`](#failure)
- [`ParseError`](#parseerror)
- [`Parser`](#parser)
- [`ParserRef`](#parserref)
- [`Position`](#position)
- [`State`](#state)

[**Functions**](#functions)

- [`any-of`](#any-of)
- [`attempt`](#attempt)
- [`between`](#between)
- [`chainl1`](#chainl1)
- [`chainr1`](#chainr1)
- [`choice`](#choice)
- [`error->string`](#error-string)
- [`followed-by`](#followed-by)
- [`look-ahead`](#look-ahead)
- [`make-state`](#make-state)
- [`many`](#many)
- [`many-chars`](#many-chars)
- [`many-chars1`](#many-chars1)
- [`many-fold`](#many-fold)
- [`many-satisfy`](#many-satisfy)
- [`many-satisfy/slice`](#many-satisfyslice)
- [`many-satisfy1`](#many-satisfy1)
- [`many-satisfy1/slice`](#many-satisfy1slice)
- [`many-till`](#many-till)
- [`many1`](#many1)
- [`none-of`](#none-of)
- [`not-followed-by`](#not-followed-by)
- [`opt`](#opt)
- [`opt-or`](#opt-or)
- [`optional`](#optional)
- [`parse-error-position`](#parse-error-position)
- [`pbind`](#pbind)
- [`pboth`](#pboth)
- [`pchar`](#pchar)
- [`pchar/ci`](#pcharci)
- [`pfail`](#pfail)
- [`pforward`](#pforward)
- [`pforward-set!`](#pforward-set)
- [`pforwarded`](#pforwarded)
- [`plabel`](#plabel)
- [`plazy`](#plazy)
- [`pleft`](#pleft)
- [`pmap`](#pmap)
- [`por`](#por)
- [`ppipe2`](#ppipe2)
- [`ppipe3`](#ppipe3)
- [`prepeat`](#prepeat)
- [`preturn`](#preturn)
- [`pright`](#pright)
- [`psequence`](#psequence)
- [`pstring`](#pstring)
- [`pstring/ci`](#pstringci)
- [`pzero`](#pzero)
- [`run-parser`](#run-parser)
- [`run-parser/message`](#run-parsermessage)
- [`satisfy`](#satisfy)
- [`sep-by`](#sep-by)
- [`sep-by1`](#sep-by1)
- [`sep-end-by`](#sep-end-by)
- [`sep-end-by1`](#sep-end-by1)
- [`skip-many`](#skip-many)
- [`skip-many1`](#skip-many1)
- [`state-position`](#state-position)

[**Values**](#values)

- [`alphanumeric`](#alphanumeric)
- [`any-char`](#any-char)
- [`digit`](#digit)
- [`eof`](#eof)
- [`letter`](#letter)
- [`pdouble`](#pdouble)
- [`pint`](#pint)
- [`pnewline`](#pnewline)
- [`pposition`](#pposition)
- [`spaces`](#spaces)
- [`spaces1`](#spaces1)

### Types

#### `Parser`

type alias

A parser: reads from a State and answers a value or a Failure.

<dl>
<dt><code>%a</code></dt>
<dd>

What it answers.

</dd>
</dl>

A parser is a function, so a parser written by hand is a
`(fun (st) ...)` that reads `st` and answers `(Ok v)` or
`(Err f)`. Most grammars never write one: the combinators here
build them.

See also: [`run-parser`](#run-parser), [`make-state`](#make-state)

#### `State`

record

Where a parser is in its input. Made by make-state; run-parser makes one for you.

<dl>
<dt><code>text</code></dt>
<dd>

The input.

</dd>
<dt><code>pos</code></dt>
<dd>

The cursor: where the next character is read.

</dd>
<dt><code>line</code></dt>
<dd>

The line pos is on, from 1.

</dd>
<dt><code>line-start</code></dt>
<dd>

Where that line begins, for the column.

</dd>
</dl>

See also: [`make-state`](#make-state), [`state-position`](#state-position)

#### `Position`

record

A line and a column, both from 1. The column counts characters.

<dl>
<dt><code>line</code></dt>
<dd>

The line, from 1.

</dd>
<dt><code>column</code></dt>
<dd>

The column, from 1, in characters.

</dd>
</dl>

See also: [`pposition`](#pposition), [`parse-error-position`](#parse-error-position)

#### `Failure`

record

How a parser failed: where, and what was expected there. run-parser turns it into a ParseError.

See also: [`ParseError`](#parseerror), [`run-parser`](#run-parser)

#### `ParseError`

record

A failed parse: the input, where it failed, and what was expected.

<dl>
<dt><code>source</code></dt>
<dd>

The input.

</dd>
<dt><code>err-pos</code></dt>
<dd>

Where the parse failed.

</dd>
<dt><code>err-line</code></dt>
<dd>

The line of err-pos, from 1.

</dd>
<dt><code>err-line-start</code></dt>
<dd>

Where that line begins.

</dd>
<dt><code>items</code></dt>
<dd>

What was expected and found there; read it through error->string.

</dd>
</dl>

`->str` of a `ParseError` is `error->string`, so
`println` shows the whole message.

See also: [`error->string`](#error-string), [`parse-error-position`](#parse-error-position)

#### `ErrorItem`

union

One thing an error says.

<dl>
<dt><code>Expected</code></dt>
<dd>

Something that would have been accepted: 'x', an integer.

</dd>
<dt><code>Unexpected</code></dt>
<dd>

Something found that should not be there.

</dd>
<dt><code>ErrorMessage</code></dt>
<dd>

A sentence of its own, from pfail.

</dd>
</dl>

#### `ErrorItems`

union

What an error says, gathered from every parser that failed at its position.

See also: [`error->string`](#error-string)

#### `ParserRef`

record

A parser to be filled in later, for a grammar of defs that refers to itself.

<dl>
<dt><code>%a</code></dt>
<dd>

What the parser answers.

</dd>
<dt><code>target</code></dt>
<dd>

The parser, once pforward-set! has set it.

</dd>
</dl>

See also: [`pforward`](#pforward), [`pforwarded`](#pforwarded), [`pforward-set!`](#pforward-set)

### Functions

#### `run-parser`

function

```bjolang
(: run-parser (-> (Parser %a) string (Result ParseError %a)))
(run-parser p text)
```

Runs a parser on a string.

<dl>
<dt><code>p</code></dt>
<dd>

The parser.

</dd>
<dt><code>text</code></dt>
<dd>

The input.

</dd>
<dt><em>returns</em></dt>
<dd>

What p answers, or where and why it failed.

</dd>
</dl>

```bjolang
(run-parser pint "42")     ; (Ok 42)
(run-parser pint "42abc")  ; (Ok 42): the rest is not looked at
(run-parser (pleft pint eof) "42abc")
;; (Err ...): Expecting: end of input
```

See also: [`run-parser/message`](#run-parsermessage), [`eof`](#eof)

#### `run-parser/message`

function

```bjolang
(: run-parser/message (-> (Parser %a) string (Result string %a)))
(run-parser/message p text)
```

Runs a parser on a string, with a failure as the message error->string writes.

<dl>
<dt><code>p</code></dt>
<dd>

The parser.

</dd>
<dt><code>text</code></dt>
<dd>

The input.

</dd>
<dt><em>returns</em></dt>
<dd>

What p answers, or the error as text.

</dd>
</dl>

```bjolang
(run-parser/message (pstring "yes") "no")
;; (Err "Parse error at line 1, column 1:\nno\n^\nExpecting: 'yes'\nUnexpected: 'n'")
```

See also: [`run-parser`](#run-parser)

#### `error->string`

function

```bjolang
(: error->string (-> ParseError string))
(error->string e)
```

An error as a message: where, the line with a caret under the column, and what was expected and found.

<dl>
<dt><code>e</code></dt>
<dd>

The error.

</dd>
<dt><em>returns</em></dt>
<dd>

The message, several lines long.

</dd>
</dl>

```bjolang
(match (run-parser (por (pstring "yes") (pstring "no")) "maybe")
  ((Err e) (println (error->string e)))
  ((Ok _) unit))
;; Parse error at line 1, column 1:
;; maybe
;; ^
;; Expecting: 'yes' or 'no'
;; Unexpected: 'm'
```

See also: [`run-parser/message`](#run-parsermessage), [`parse-error-position`](#parse-error-position)

#### `parse-error-position`

function

```bjolang
(: parse-error-position (-> ParseError Position))
(parse-error-position e)
```

Where an error is.

<dl>
<dt><code>e</code></dt>
<dd>

The error.

</dd>
<dt><em>returns</em></dt>
<dd>

Its line and column.

</dd>
</dl>

```bjolang
(match (run-parser (pright (pstring "ab\n") pint) "ab\nx")
  ((Err e) (println (parse-error-position e)))   ; line 2, column 1
  ((Ok _) unit))
```

#### `make-state`

function

```bjolang
(: make-state (-> string State))
(make-state text)
```

A State at the start of a string, for calling a parser by hand.

<dl>
<dt><code>text</code></dt>
<dd>

The input.

</dd>
<dt><em>returns</em></dt>
<dd>

The state.

</dd>
</dl>

```bjolang
(def st (make-state "12 34"))
(pint st)                ; (Ok 12)
(state-position st)      ; line 1, column 3
```

See also: [`run-parser`](#run-parser), [`state-position`](#state-position)

#### `state-position`

function

```bjolang
(: state-position (-> State Position))
(state-position st)
```

Where a state is.

<dl>
<dt><code>st</code></dt>
<dd>

The state.

</dd>
<dt><em>returns</em></dt>
<dd>

Its line and column.

</dd>
</dl>

See also: [`pposition`](#pposition), [`make-state`](#make-state)

#### `preturn`

function

```bjolang
(: preturn (-> %a (Parser %a)))
(preturn v)
```

Succeeds with a value, reading nothing.

<dl>
<dt><code>v</code></dt>
<dd>

The value.

</dd>
<dt><em>returns</em></dt>
<dd>

A parser answering v.

</dd>
</dl>

```bjolang
(run-parser (preturn 7) "anything")   ; (Ok 7)
```

#### `pzero`

function

```bjolang
(: pzero (Parser %a))
(pzero st)
```

Fails, reading nothing and expecting nothing.

<dl>
<dt><code>st</code></dt>
<dd>

The state: pzero is a parser, used as one rather than called.

</dd>
<dt><em>returns</em></dt>
<dd>

A failure with nothing to say.

</dd>
</dl>

See also: [`pfail`](#pfail), [`choice`](#choice)

#### `pfail`

function

```bjolang
(: pfail (-> string (Parser %a)))
(pfail msg)
```

Fails with a message of its own.

<dl>
<dt><code>msg</code></dt>
<dd>

What the error says.

</dd>
<dt><em>returns</em></dt>
<dd>

A parser that always fails.

</dd>
</dl>

```bjolang
(pbind pint (fun (n) (if (< n 256) (preturn n) (pfail "a byte is at most 255"))))
```

See also: [`plabel`](#plabel)

#### `pbind`

function

```bjolang
(: pbind (-> (Parser %a) (-> %a (Parser %b)) (Parser %b)))
(pbind p f)
```

Runs p, and then the parser f makes from what p answered.

<dl>
<dt><code>p</code></dt>
<dd>

The first parser.

</dd>
<dt><code>f</code></dt>
<dd>

Makes the second parser from p's value.

</dd>
<dt><em>returns</em></dt>
<dd>

The second parser's value.

</dd>
</dl>

```bjolang
;; A count, then that many letters: "3abc"
(pbind pint (fun (n) (prepeat n letter)))
```

See also: [`pmap`](#pmap), [`ppipe2`](#ppipe2)

#### `pmap`

function

```bjolang
(: pmap (-> (-> %a %b) (Parser %a) (Parser %b)))
(pmap f p)
```

Changes what a parser answers.

<dl>
<dt><code>f</code></dt>
<dd>

Applied to the value.

</dd>
<dt><code>p</code></dt>
<dd>

The parser.

</dd>
<dt><em>returns</em></dt>
<dd>

f of p's value.

</dd>
</dl>

```bjolang
(run-parser (pmap string-length (many-satisfy char-alphabetic?)) "hello!")   ; (Ok 5)
```

#### `pright`

function

```bjolang
(: pright (-> (Parser %a) (Parser %b) (Parser %b)))
(pright p q)
```

p, then q, answering q’s value.

<dl>
<dt><code>p</code></dt>
<dd>

Read and dropped.

</dd>
<dt><code>q</code></dt>
<dd>

Answers.

</dd>
<dt><em>returns</em></dt>
<dd>

q's value.

</dd>
</dl>

```bjolang
(run-parser (pright (pchar #\$) pint) "$42")   ; (Ok 42)
```

See also: [`pleft`](#pleft), [`between`](#between)

#### `pleft`

function

```bjolang
(: pleft (-> (Parser %a) (Parser %b) (Parser %a)))
(pleft p q)
```

p, then q, answering p’s value.

<dl>
<dt><code>p</code></dt>
<dd>

Answers.

</dd>
<dt><code>q</code></dt>
<dd>

Read and dropped.

</dd>
<dt><em>returns</em></dt>
<dd>

p's value.

</dd>
</dl>

```bjolang
(run-parser (pleft pint (pchar #\%)) "42%")   ; (Ok 42)
```

See also: [`pright`](#pright)

#### `pboth`

function

```bjolang
(: pboth (-> (Parser %a) (Parser %b) (Parser (Tuple %a %b))))
(pboth p q)
```

p, then q, answering both.

<dl>
<dt><code>p</code></dt>
<dd>

The first.

</dd>
<dt><code>q</code></dt>
<dd>

The second.

</dd>
<dt><em>returns</em></dt>
<dd>

Both values, as a tuple.

</dd>
</dl>

```bjolang
(run-parser (pboth letter digit) "a1")   ; (Ok (Tuple #\a #\1))
```

See also: [`ppipe2`](#ppipe2)

#### `ppipe2`

function

```bjolang
(: ppipe2 (-> (Parser %a) (Parser %b) (-> %a %b %c) (Parser %c)))
(ppipe2 p q f)
```

p, then q, combined by f.

<dl>
<dt><code>p</code></dt>
<dd>

The first.

</dd>
<dt><code>q</code></dt>
<dd>

The second.

</dd>
<dt><code>f</code></dt>
<dd>

Makes the result from both values.

</dd>
<dt><em>returns</em></dt>
<dd>

f of both values.

</dd>
</dl>

```bjolang
(run-parser (ppipe2 pint (pright (pchar #\x) pint) *) "6x7")   ; (Ok 42)
```

See also: [`ppipe3`](#ppipe3), [`pboth`](#pboth)

#### `ppipe3`

function

```bjolang
(: ppipe3 (-> (Parser %a) (Parser %b) (Parser %c) (-> %a %b %c %d) (Parser %d)))
(ppipe3 p q r f)
```

p, q and r, combined by f.

<dl>
<dt><code>p</code></dt>
<dd>

The first.

</dd>
<dt><code>q</code></dt>
<dd>

The second.

</dd>
<dt><code>r</code></dt>
<dd>

The third.

</dd>
<dt><code>f</code></dt>
<dd>

Makes the result from the three values.

</dd>
<dt><em>returns</em></dt>
<dd>

f of the three values.

</dd>
</dl>

```bjolang
;; 2026-10-05
(ppipe3 pint (pright (pchar #\-) pint) (pright (pchar #\-) pint)
        (fun (y m d) (Tuple y m d)))
```

#### `psequence`

function

```bjolang
(: psequence (-> (List (Parser %a)) (Parser (List %a))))
(psequence ps)
```

Each parser in turn, answering all their values.

<dl>
<dt><code>ps</code></dt>
<dd>

The parsers, in order.

</dd>
<dt><em>returns</em></dt>
<dd>

Their values, in order.

</dd>
</dl>

```bjolang
(run-parser (psequence (list digit letter digit)) "1a2")   ; (Ok '(#\1 #\a #\2))
```

#### `por`

function

```bjolang
(: por (-> (Parser %a) (Parser %a) (Parser %a)))
(por p q)
```

p, or q when p failed without reading anything.

<dl>
<dt><code>p</code></dt>
<dd>

Tried first.

</dd>
<dt><code>q</code></dt>
<dd>

Tried when p failed where it began.

</dd>
<dt><em>returns</em></dt>
<dd>

The value of whichever succeeded.

</dd>
</dl>

```bjolang
(def answer (por (pstring "yes") (pstring "no")))
(run-parser answer "no")      ; (Ok "no")
(run-parser answer "maybe")   ; (Err ...): Expecting: 'yes' or 'no'
```

Two failures at the same position become one error expecting what
either expected. When `p` read something before it failed,
`q` is not tried; see `attempt`.

See also: [`choice`](#choice), [`attempt`](#attempt)

#### `choice`

function

```bjolang
(: choice (-> (List (Parser %a)) (Parser %a)))
(choice ps)
```

The first of several parsers that succeeds, under por’s rule.

<dl>
<dt><code>ps</code></dt>
<dd>

The alternatives, in order.

</dd>
<dt><em>returns</em></dt>
<dd>

The value of the first that succeeds.

</dd>
</dl>

```bjolang
(choice (list (pstring "red") (pstring "green") (pstring "blue")))
;; on "pink": Expecting: 'red', 'green' or 'blue'
```

See also: [`por`](#por)

#### `attempt`

function

```bjolang
(: attempt (-> (Parser %a) (Parser %a)))
(attempt p)
```

Runs p, and on failure moves back to where p began.

<dl>
<dt><code>p</code></dt>
<dd>

The parser.

</dd>
<dt><em>returns</em></dt>
<dd>

p's value.

</dd>
</dl>

```bjolang
(def a-then-b (pright (pchar #\a) (pstring "b")))
(run-parser (por a-then-b (pstring "ac")) "ac")
;; (Err ...): Expecting: 'b'. a-then-b read the a, so "ac" is not tried.
(run-parser (por (attempt a-then-b) (pstring "ac")) "ac")   ; (Ok "ac")
```

This is what lets `por` try another alternative after one that
read part of its input. Use it around the alternative that may read
and then fail, and no wider: an error found inside an `attempt`
is reported at its start.

See also: [`por`](#por), [`look-ahead`](#look-ahead)

#### `look-ahead`

function

```bjolang
(: look-ahead (-> (Parser %a) (Parser %a)))
(look-ahead p)
```

Runs p and moves back to where it began, whether p succeeded or not.

<dl>
<dt><code>p</code></dt>
<dd>

The parser.

</dd>
<dt><em>returns</em></dt>
<dd>

p's value, with nothing read.

</dd>
</dl>

```bjolang
(run-parser (pboth (look-ahead letter) (many-satisfy char-alphabetic?)) "abc")
;; (Ok (Tuple #\a "abc"))
```

See also: [`followed-by`](#followed-by), [`attempt`](#attempt)

#### `followed-by`

function

```bjolang
(: followed-by (-> (Parser %a) string (Parser void)))
(followed-by p what)
```

Succeeds, reading nothing, when p would succeed here.

<dl>
<dt><code>p</code></dt>
<dd>

The parser to test with.

</dd>
<dt><code>what</code></dt>
<dd>

What the error says was expected.

</dd>
<dt><em>returns</em></dt>
<dd>

A parser answering nothing.

</dd>
</dl>

```bjolang
(pleft pint (followed-by (pchar #\;) "';'"))   ; 42 only when a ; comes next
```

See also: [`not-followed-by`](#not-followed-by), [`look-ahead`](#look-ahead)

#### `not-followed-by`

function

```bjolang
(: not-followed-by (-> (Parser %a) string (Parser void)))
(not-followed-by p what)
```

Succeeds, reading nothing, when p would fail here.

<dl>
<dt><code>p</code></dt>
<dd>

The parser to test with.

</dd>
<dt><code>what</code></dt>
<dd>

What the error says was found.

</dd>
<dt><em>returns</em></dt>
<dd>

A parser answering nothing.

</dd>
</dl>

```bjolang
;; The keyword "if", but not the start of "iffy".
(pleft (pstring "if") (not-followed-by alphanumeric "a longer name"))
```

See also: [`followed-by`](#followed-by)

#### `plabel`

function

```bjolang
(: plabel (-> (Parser %a) string (Parser %a)))
(plabel p what)
```

Names what p expects, in the words an error should use.

<dl>
<dt><code>p</code></dt>
<dd>

The parser.

</dd>
<dt><code>what</code></dt>
<dd>

What to call it.

</dd>
<dt><em>returns</em></dt>
<dd>

p, with its expectation renamed.

</dd>
</dl>

```bjolang
(plabel (many-satisfy1 char-alphabetic? "a letter") "a name")
;; on "42": Expecting: a name
```

Only a failure where `p` began is renamed: once `p` has read
something, its own error says more than the label.

See also: [`pfail`](#pfail)

#### `satisfy`

function

```bjolang
(: satisfy (-> (-> char bool) string (Parser char)))
(satisfy pred what)
```

One character that pred accepts.

<dl>
<dt><code>pred</code></dt>
<dd>

Accepts or refuses a character.

</dd>
<dt><code>what</code></dt>
<dd>

What an error calls it.

</dd>
<dt><em>returns</em></dt>
<dd>

The character.

</dd>
</dl>

```bjolang
(run-parser (satisfy char-upper-case? "a capital") "Bob")   ; (Ok #\B)
```

See also: [`many-satisfy`](#many-satisfy), [`any-of`](#any-of)

#### `pchar`

function

```bjolang
(: pchar (-> char (Parser char)))
(pchar c)
```

One particular character.

<dl>
<dt><code>c</code></dt>
<dd>

The character.

</dd>
<dt><em>returns</em></dt>
<dd>

c.

</dd>
</dl>

```bjolang
(run-parser (pchar #\x) "xyz")   ; (Ok #\x)
```

See also: [`pchar/ci`](#pcharci), [`any-of`](#any-of)

#### `pchar/ci`

function

```bjolang
(: pchar/ci (-> char (Parser char)))
(pchar/ci c)
```

One particular character, in either case.

<dl>
<dt><code>c</code></dt>
<dd>

The character.

</dd>
<dt><em>returns</em></dt>
<dd>

The character as the input has it.

</dd>
</dl>

```bjolang
(run-parser (pchar/ci #\x) "XYZ")   ; (Ok #\X)
```

#### `any-of`

function

```bjolang
(: any-of (-> string (Parser char)))
(any-of s)
```

One character of those in a string.

<dl>
<dt><code>s</code></dt>
<dd>

The characters accepted.

</dd>
<dt><em>returns</em></dt>
<dd>

The character.

</dd>
</dl>

```bjolang
(run-parser (any-of "+-*/") "*2")   ; (Ok #\*)
```

See also: [`none-of`](#none-of), [`satisfy`](#satisfy)

#### `none-of`

function

```bjolang
(: none-of (-> string (Parser char)))
(none-of s)
```

One character that is not in a string.

<dl>
<dt><code>s</code></dt>
<dd>

The characters refused.

</dd>
<dt><em>returns</em></dt>
<dd>

The character.

</dd>
</dl>

```bjolang
;; A string literal's body: anything but a quote.
(between (pchar #\") (pchar #\") (many-chars (none-of "\"")))
```

#### `pstring`

function

```bjolang
(: pstring (-> string (Parser string)))
(pstring s)
```

A literal, all of it or nothing: a partial match reads nothing.

<dl>
<dt><code>s</code></dt>
<dd>

The literal.

</dd>
<dt><em>returns</em></dt>
<dd>

s.

</dd>
</dl>

```bjolang
(run-parser (por (pstring "foo") (pstring "far")) "far")   ; (Ok "far")
;; "foo" read the f before it failed, and gave it back.
```

See also: [`pstring/ci`](#pstringci), [`por`](#por)

#### `pstring/ci`

function

```bjolang
(: pstring/ci (-> string (Parser string)))
(pstring/ci s)
```

A literal in any case, all of it or nothing.

<dl>
<dt><code>s</code></dt>
<dd>

The literal.

</dd>
<dt><em>returns</em></dt>
<dd>

The text as the input has it.

</dd>
</dl>

```bjolang
(run-parser (pstring/ci "select") "SELECT *")   ; (Ok "SELECT")
```

#### `many-satisfy`

function

```bjolang
(: many-satisfy (-> (-> char bool) (Parser string)))
(many-satisfy pred)
```

The characters pred accepts, as many as come. Never fails.

<dl>
<dt><code>pred</code></dt>
<dd>

Accepts or refuses a character.

</dd>
<dt><em>returns</em></dt>
<dd>

The run, which may be empty.

</dd>
</dl>

```bjolang
(run-parser (many-satisfy char-numeric?) "2026-10")   ; (Ok "2026")
```

See also: [`many-satisfy1`](#many-satisfy1), [`many-satisfy/slice`](#many-satisfyslice)

#### `many-satisfy1`

function

```bjolang
(: many-satisfy1 (-> (-> char bool) string (Parser string)))
(many-satisfy1 pred what)
```

The characters pred accepts, at least one.

<dl>
<dt><code>pred</code></dt>
<dd>

Accepts or refuses a character.

</dd>
<dt><code>what</code></dt>
<dd>

What an error calls one.

</dd>
<dt><em>returns</em></dt>
<dd>

The run.

</dd>
</dl>

```bjolang
(def identifier (many-satisfy1 char-alphabetic? "a letter"))
```

See also: [`many-satisfy`](#many-satisfy), [`many-satisfy1/slice`](#many-satisfy1slice)

#### `many-satisfy/slice`

function

```bjolang
(: many-satisfy/slice (-> (-> char bool) (Parser string)))
(many-satisfy/slice pred)
```

many-satisfy, answering a slice of the input instead of a copy.

<dl>
<dt><code>pred</code></dt>
<dd>

Accepts or refuses a character.

</dd>
<dt><em>returns</em></dt>
<dd>

The run, sharing the input's bytes.

</dd>
</dl>

```bjolang
;; Lines, looked at and dropped: no line is copied.
(many (pleft (many-satisfy/slice #(not (char=? &1 #\newline))) pnewline))
```

Faster than `many-satisfy` for a run of more than a few bytes,
and it allocates less. A slice keeps the whole input alive for as long
as it is held, so keep one with `string-copy`.

See also: [`many-satisfy`](#many-satisfy), [`many-satisfy1/slice`](#many-satisfy1slice)

#### `many-satisfy1/slice`

function

```bjolang
(: many-satisfy1/slice (-> (-> char bool) string (Parser string)))
(many-satisfy1/slice pred what)
```

many-satisfy1, answering a slice of the input instead of a copy.

<dl>
<dt><code>pred</code></dt>
<dd>

Accepts or refuses a character.

</dd>
<dt><code>what</code></dt>
<dd>

What an error calls one.

</dd>
<dt><em>returns</em></dt>
<dd>

The run, sharing the input's bytes.

</dd>
</dl>

See also: [`many-satisfy1`](#many-satisfy1), [`many-satisfy/slice`](#many-satisfyslice)

#### `many-chars`

function

```bjolang
(: many-chars (-> (Parser char) (Parser string)))
(many-chars p)
```

The characters a parser reads, as many as it reads, as a string.

<dl>
<dt><code>p</code></dt>
<dd>

Reads one character.

</dd>
<dt><em>returns</em></dt>
<dd>

The characters, which may be none.

</dd>
</dl>

```bjolang
;; Escapes as well as plain characters, which many-satisfy cannot do.
(many-chars (por (pright (pchar #\\) any-char) (none-of "\"\\")))
```

See also: [`many-satisfy`](#many-satisfy), [`many-chars1`](#many-chars1)

#### `many-chars1`

function

```bjolang
(: many-chars1 (-> (Parser char) (Parser string)))
(many-chars1 p)
```

many-chars, at least one character.

<dl>
<dt><code>p</code></dt>
<dd>

Reads one character.

</dd>
<dt><em>returns</em></dt>
<dd>

The characters.

</dd>
</dl>

See also: [`many-chars`](#many-chars)

#### `many`

function

```bjolang
(: many (-> (Parser %a) (Parser (List %a))))
(many p)
```

p, as many times as it succeeds.

<dl>
<dt><code>p</code></dt>
<dd>

The parser repeated. It has to read something each time.

</dd>
<dt><em>returns</em></dt>
<dd>

Every value, which may be none.

</dd>
</dl>

```bjolang
(run-parser (many (pleft pint spaces)) "1 2 3")   ; (Ok '(1 2 3))
```

The repetition ends when `p` fails where it began. A failure
after `p` read something fails the whole `many`. A `p`
that succeeds without reading would repeat forever, and panics.

See also: [`many1`](#many1), [`sep-by`](#sep-by), [`many-fold`](#many-fold)

#### `many1`

function

```bjolang
(: many1 (-> (Parser %a) (Parser (List %a))))
(many1 p)
```

p, at least once and as many times as it succeeds.

<dl>
<dt><code>p</code></dt>
<dd>

The parser repeated.

</dd>
<dt><em>returns</em></dt>
<dd>

Every value.

</dd>
</dl>

See also: [`many`](#many)

#### `skip-many`

function

```bjolang
(: skip-many (-> (Parser %a) (Parser void)))
(skip-many p)
```

p, as many times as it succeeds, keeping nothing.

<dl>
<dt><code>p</code></dt>
<dd>

The parser repeated.

</dd>
<dt><em>returns</em></dt>
<dd>

Nothing.

</dd>
</dl>

See also: [`many`](#many), [`skip-many1`](#skip-many1)

#### `skip-many1`

function

```bjolang
(: skip-many1 (-> (Parser %a) (Parser void)))
(skip-many1 p)
```

p, at least once, keeping nothing.

<dl>
<dt><code>p</code></dt>
<dd>

The parser repeated.

</dd>
<dt><em>returns</em></dt>
<dd>

Nothing.

</dd>
</dl>

See also: [`skip-many`](#skip-many)

#### `many-fold`

function

```bjolang
(: many-fold (-> (-> %a %s %s) %s (Parser %a) (Parser %s)))
(many-fold f init p)
```

p, as many times as it succeeds, folding the values as they come.

<dl>
<dt><code>f</code></dt>
<dd>

Combines a value with what was folded so far.

</dd>
<dt><code>init</code></dt>
<dd>

Where the fold starts.

</dd>
<dt><code>p</code></dt>
<dd>

The parser repeated.

</dd>
<dt><em>returns</em></dt>
<dd>

The fold.

</dd>
</dl>

```bjolang
(run-parser (many-fold + 0 (pleft pint spaces)) "1 2 3")   ; (Ok 6)
```

See also: [`many`](#many)

#### `many-till`

function

```bjolang
(: many-till (-> (Parser %a) (Parser %end) (Parser (List %a))))
(many-till p end)
```

p, until end succeeds. end is tried first.

<dl>
<dt><code>p</code></dt>
<dd>

The parser repeated.

</dd>
<dt><code>end</code></dt>
<dd>

What ends it; its value is dropped.

</dd>
<dt><em>returns</em></dt>
<dd>

p's values.

</dd>
</dl>

```bjolang
;; A comment: everything up to */
(pright (pstring "/*") (many-till any-char (pstring "*/")))
```

#### `prepeat`

function

```bjolang
(: prepeat (-> int (Parser %a) (Parser (List %a))))
(prepeat n p)
```

p exactly n times.

<dl>
<dt><code>n</code></dt>
<dd>

How many.

</dd>
<dt><code>p</code></dt>
<dd>

The parser repeated.

</dd>
<dt><em>returns</em></dt>
<dd>

The n values.

</dd>
</dl>

```bjolang
(run-parser (prepeat 3 digit) "12345")   ; (Ok '(#\1 #\2 #\3))
```

#### `sep-by`

function

```bjolang
(: sep-by (-> (Parser %a) (Parser %sep) (Parser (List %a))))
(sep-by p sep)
```

p, separated by sep, zero or more times.

<dl>
<dt><code>p</code></dt>
<dd>

The item.

</dd>
<dt><code>sep</code></dt>
<dd>

The separator; its value is dropped.

</dd>
<dt><em>returns</em></dt>
<dd>

The items.

</dd>
</dl>

```bjolang
(run-parser (sep-by pint (pchar #\,)) "1,2,3")   ; (Ok '(1 2 3))
(run-parser (sep-by pint (pchar #\,)) "")        ; (Ok '())
```

See also: [`sep-by1`](#sep-by1), [`sep-end-by`](#sep-end-by)

#### `sep-by1`

function

```bjolang
(: sep-by1 (-> (Parser %a) (Parser %sep) (Parser (List %a))))
(sep-by1 p sep)
```

p, separated by sep, at least once.

<dl>
<dt><code>p</code></dt>
<dd>

The item.

</dd>
<dt><code>sep</code></dt>
<dd>

The separator.

</dd>
<dt><em>returns</em></dt>
<dd>

The items.

</dd>
</dl>

See also: [`sep-by`](#sep-by)

#### `sep-end-by`

function

```bjolang
(: sep-end-by (-> (Parser %a) (Parser %sep) (Parser (List %a))))
(sep-end-by p sep)
```

sep-by, also accepting a separator after the last item.

<dl>
<dt><code>p</code></dt>
<dd>

The item.

</dd>
<dt><code>sep</code></dt>
<dd>

The separator.

</dd>
<dt><em>returns</em></dt>
<dd>

The items.

</dd>
</dl>

```bjolang
(run-parser (sep-end-by pint (pchar #\;)) "1;2;")   ; (Ok '(1 2))
```

See also: [`sep-by`](#sep-by)

#### `sep-end-by1`

function

```bjolang
(: sep-end-by1 (-> (Parser %a) (Parser %sep) (Parser (List %a))))
(sep-end-by1 p sep)
```

sep-end-by, at least one item.

<dl>
<dt><code>p</code></dt>
<dd>

The item.

</dd>
<dt><code>sep</code></dt>
<dd>

The separator.

</dd>
<dt><em>returns</em></dt>
<dd>

The items.

</dd>
</dl>

See also: [`sep-end-by`](#sep-end-by)

#### `between`

function

```bjolang
(: between (-> (Parser %open) (Parser %close) (Parser %a) (Parser %a)))
(between popen pclose p)
```

p, between an opening and a closing parser.

<dl>
<dt><code>popen</code></dt>
<dd>

Read first and dropped.

</dd>
<dt><code>pclose</code></dt>
<dd>

Read last and dropped.

</dd>
<dt><code>p</code></dt>
<dd>

Answers.

</dd>
<dt><em>returns</em></dt>
<dd>

p's value.

</dd>
</dl>

```bjolang
(run-parser (between (pchar #\() (pchar #\)) pint) "(42)")   ; (Ok 42)
```

#### `opt`

function

```bjolang
(: opt (-> (Parser %a) (Parser (Option %a))))
(opt p)
```

p if it is there.

<dl>
<dt><code>p</code></dt>
<dd>

The parser.

</dd>
<dt><em>returns</em></dt>
<dd>

(Some value), or None when p failed where it began.

</dd>
</dl>

```bjolang
(run-parser (opt (pchar #\-)) "5")   ; (Ok None)
```

See also: [`opt-or`](#opt-or), [`optional`](#optional)

#### `opt-or`

function

```bjolang
(: opt-or (-> (Parser %a) %a (Parser %a)))
(opt-or p fallback)
```

p if it is there, and otherwise a fallback.

<dl>
<dt><code>p</code></dt>
<dd>

The parser.

</dd>
<dt><code>fallback</code></dt>
<dd>

The value when p is not there.

</dd>
<dt><em>returns</em></dt>
<dd>

p's value or the fallback.

</dd>
</dl>

```bjolang
(run-parser (opt-or pint 1) "x")   ; (Ok 1)
```

#### `optional`

function

```bjolang
(: optional (-> (Parser %a) (Parser void)))
(optional p)
```

p if it is there, keeping nothing.

<dl>
<dt><code>p</code></dt>
<dd>

The parser.

</dd>
<dt><em>returns</em></dt>
<dd>

Nothing.

</dd>
</dl>

See also: [`opt`](#opt)

#### `chainl1`

function

```bjolang
(: chainl1 (-> (Parser %a) (Parser (-> %a %a %a)) (Parser %a)))
(chainl1 p op)
```

One or more p, joined by operators, grouped from the left.

<dl>
<dt><code>p</code></dt>
<dd>

An operand.

</dd>
<dt><code>op</code></dt>
<dd>

An operator, answering how to combine two operands.

</dd>
<dt><em>returns</em></dt>
<dd>

The operands, combined.

</dd>
</dl>

```bjolang
(def minus (pright (pchar #\-) (preturn -)))
(run-parser (chainl1 pint minus) "10-3-2")   ; (Ok 5): (10-3)-2
```

Precedence is one `chainl1` per level, the tighter inside the
looser: a sum is a `chainl1` of products, and a product a
`chainl1` of factors.

See also: [`chainr1`](#chainr1)

#### `chainr1`

function

```bjolang
(: chainr1 (-> (Parser %a) (Parser (-> %a %a %a)) (Parser %a)))
(chainr1 p op)
```

One or more p, joined by operators, grouped from the right.

<dl>
<dt><code>p</code></dt>
<dd>

An operand.

</dd>
<dt><code>op</code></dt>
<dd>

An operator, answering how to combine two operands.

</dd>
<dt><em>returns</em></dt>
<dd>

The operands, combined.

</dd>
</dl>

```bjolang
(def minus (pright (pchar #\-) (preturn -)))
(run-parser (chainr1 pint minus) "10-3-2")   ; (Ok 9): 10-(3-2)
```

See also: [`chainl1`](#chainl1)

#### `pforward`

function

```bjolang
(: pforward (-> (ParserRef %a)))
(pforward)
```

A parser to refer to before it is defined.

<dl>
<dt><em>returns</em></dt>
<dd>

An empty reference, to fill in with pforward-set!.

</dd>
</dl>

```bjolang
;; Nested lists of integers: [1, [2, 3]]
(type (: Nested (Union (: Num int) (: Nest (List Nested)))))

(: value-ref (ParserRef Nested))
(def value-ref (pforward))
(def value (pforwarded value-ref))
(def nested
  (pmap Nest (between (pchar #\[) (pchar #\]) (sep-by value (pleft (pchar #\,) spaces)))))
(pforward-set! value-ref (por (pmap Num pint) nested))
```

See also: [`pforwarded`](#pforwarded), [`pforward-set!`](#pforward-set), [`plazy`](#plazy)

#### `pforwarded`

function

```bjolang
(: pforwarded (-> (ParserRef %a) (Parser %a)))
(pforwarded r)
```

The parser a reference holds, whenever it is run.

<dl>
<dt><code>r</code></dt>
<dd>

The reference.

</dd>
<dt><em>returns</em></dt>
<dd>

A parser that runs what r holds at the time.

</dd>
</dl>

See also: [`pforward`](#pforward)

#### `pforward-set!`

function

```bjolang
(: pforward-set! (-> (ParserRef %a) (Parser %a) void))
(pforward-set! r p)
```

Fills in a reference.

<dl>
<dt><code>r</code></dt>
<dd>

The reference.

</dd>
<dt><code>p</code></dt>
<dd>

The parser it stands for.

</dd>
</dl>

See also: [`pforward`](#pforward)

#### `plazy`

function

```bjolang
(: plazy (-> (-> (Parser %a)) (Parser %a)))
(plazy thunk)
```

A parser made the first time it is needed, by a function.

<dl>
<dt><code>thunk</code></dt>
<dd>

Makes the parser. Called once.

</dd>
<dt><em>returns</em></dt>
<dd>

The parser it makes.

</dd>
</dl>

```bjolang
;; A grammar of functions. Building (expr) would build (expr) again,
;; forever; plazy builds the inner one when it is first run.
(: expr (-> (Parser int)))
(defun (expr)
  (por pint (plazy (fun () (between (pchar #\() (pchar #\)) (expr))))))

(run-parser (expr) "((7))")   ; (Ok 7)
```

See also: [`pforward`](#pforward)

### Values

#### `pposition`

value

```bjolang
(: pposition (Parser Position))
```

Where the parser is, reading nothing.

```bjolang
;; Each word with where it starts.
(many (pleft (pboth pposition (many-satisfy1 char-alphabetic? "a word")) spaces))
```

See also: [`Position`](#position), [`state-position`](#state-position)

#### `any-char`

value

```bjolang
(: any-char (Parser char))
```

Any one character. Fails only at the end of the input.

See also: [`satisfy`](#satisfy), [`many-till`](#many-till)

#### `spaces`

value

```bjolang
(: spaces (Parser Unit))
```

Spaces, tabs and line breaks, as many as come. Never fails.

```bjolang
(pleft pint spaces)   ; a number and whatever space follows it
```

See also: [`spaces1`](#spaces1)

#### `spaces1`

value

```bjolang
(: spaces1 (Parser void))
```

At least one whitespace character.

See also: [`spaces`](#spaces)

#### `pnewline`

value

```bjolang
(: pnewline (Parser char))
```

A line break: “\\n”, “\\r\\n” or “\\r”. Answers #\\newline whichever it was.

See also: [`spaces`](#spaces)

#### `letter`

value

```bjolang
(: letter (Parser char))
```

One letter, by char-alphabetic?.

See also: [`digit`](#digit), [`alphanumeric`](#alphanumeric)

#### `digit`

value

```bjolang
(: digit (Parser char))
```

One ASCII digit, 0 to 9.

See also: [`pint`](#pint), [`letter`](#letter)

#### `alphanumeric`

value

```bjolang
(: alphanumeric (Parser char))
```

One letter or ASCII digit.

See also: [`letter`](#letter), [`digit`](#digit)

#### `eof`

value

```bjolang
(: eof (Parser void))
```

Succeeds only at the end of the input.

```bjolang
(run-parser (pleft pint eof) "42")    ; (Ok 42)
(run-parser (pleft pint eof) "42x")   ; (Err ...): Expecting: end of input
```

See also: [`run-parser`](#run-parser)

#### `pint`

value

```bjolang
(: pint (Parser int))
```

An integer: an optional sign and ASCII digits.

```bjolang
(run-parser pint "-17 apples")   ; (Ok -17)
```

A sign with no digits after it reads nothing, so `pint` needs no
`attempt` in a choice. A number too large for an `int`
throws, as `string->int` does: it is not a parse error.

See also: [`pdouble`](#pdouble), [`digit`](#digit)

#### `pdouble`

value

```bjolang
(: pdouble (Parser double))
```

A number with an optional fraction and exponent: 3, -0.5, 6.02e23.

```bjolang
(run-parser pdouble "6.02e23")   ; (Ok 6.02e23)
(run-parser pdouble "1.x")       ; (Ok 1.0), and ".x" is left
```

It needs a digit before any point, so `.5` is not a number here,
and a point or an exponent with no digit after it is left for what
comes next.

See also: [`pint`](#pint)
