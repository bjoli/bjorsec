# FParsec twin of the bjorsec benchmark

An F#/FParsec implementation of the same two programs as `bjorsec`:

- `Golden.fs` — the same failing/succeeding inputs as `bjorsec/tests/golden.bjo`.
- `Bench.fs`  — the same calculator benchmark as `bjorsec/bench.bjo` (20000-term
  `1 + 1 + ... + 1`, 5 warm-ups, 20 timed parses, bytes via
  `GC.GetAllocatedBytesForCurrentThread`).

`Grammar.fs` holds the shared grammar, written to mirror `bench.bjo` exactly
(`token`, `sym`, `number`, `factor`, `mul-op`, `add-op`, `term`, `sum`,
`calculator`).

## Build and run

```sh
cd /workspace/bjorsec/fparsec-compare
DOTNET_GCHeapHardLimit=0x80000000 DOTNET_gcServer=0 dotnet build -c Release
DOTNET_GCHeapHardLimit=0x80000000 dotnet bin/Release/net10.0/fparsec-compare.dll golden
DOTNET_GCHeapHardLimit=0x80000000 dotnet bin/Release/net10.0/fparsec-compare.dll bench
```

## Results (same machine, pinned to one CPU, same session)

| program | bytes/parse | total ms (20 parses) | relative |
|---------|-------------|----------------------|----------|
| bjorsec, baseline (commit 4c60fb2) | 11,680,184 | ~1640 | 3.9x FParsec |
| bjorsec, after the optimisations (27806f5) | 1,120,072 | ~1392 | 3.3x FParsec |
| FParsec 1.1.1 | 2,400,296 | ~419 | 1.0x |

**The important line is the middle one.** After the optimisations bjorsec
allocates *less than half* what FParsec does (1.12 MB vs 2.40 MB per parse),
yet is still ~3.3x slower. So allocation is not what separates them — the
remaining gap is the generated code: per-call overhead, the runtime helpers,
and the combinator layering. Cutting allocations was worth ~15%, not 3x.

### Correctness

The golden outputs agree on every position and every success value
(`OK 7`, `OK -6`, `OK hello`, `OK -42`, `OK 350`, `OK 0` for the 100k-term
`chainr1`, `OK a` for the backtracking case). Only the wording of failures
differs, because the two libraries have their own error model:

| input | bjorsec | FParsec |
|-------|---------|---------|
| `"1 +"` | `Parse error at line 1, column 4:` … `Expecting: a number or '('` / `Unexpected: end of input` | `Error in Ln: 1 Col: 4` … `Note: The error occurred at the end of the input stream.` / `Expecting: a number or '('` |
| `"(1 + 2"` | `Expecting: ')'` | `Expecting: ')', '*', '+', '-' or '/'` |
| `followed-by` on `"ac"` | `Expecting: 'b'` / `Unexpected: 'c'` | `Unknown Error(s)` |
| emoji | `Unexpected: '😀'` | `Note: The error occurred at the beginning of the surrogate pair '…'` |

FParsec does not print an `Unexpected:` line in general, and `followedBy`
carries no message of its own.
