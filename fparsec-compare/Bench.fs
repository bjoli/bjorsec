// The FParsec twin of bjorsec's bench.bjo: parse "1 + 1 + ... + 1" (20000
// terms), 5 warm-up parses then 20 timed parses, reporting each run's time and
// allocated bytes, then the total and the bytes per parse.
module Bench

open System
open System.Diagnostics
open FParsec
open Grammar

let private input : string =
    String.concat " + " (List.replicate 20000 "1")

let main () : int =
    printfn "Warming up JIT..."
    for _ in 1 .. 5 do
        run calculator input |> ignore

    printfn "Parsing 20000 elements 20 times..."
    let sw = Stopwatch.StartNew()
    let mutable acc = 0.0
    let mutable bytes = 0L

    for i in 0 .. 19 do
        let before = GC.GetAllocatedBytesForCurrentThread()
        let t0 = Stopwatch.GetTimestamp()
        let result = run calculator input
        let t1 = Stopwatch.GetTimestamp()
        let used = GC.GetAllocatedBytesForCurrentThread() - before
        bytes <- bytes + used

        match result with
        | Success (v, _, _) -> acc <- acc + v
        | Failure (msg, _, _) -> printf "%s" msg

        let ms = (t1 - t0) * 1000L / Stopwatch.Frequency
        printfn "run %d: %d ms, %d B" i ms used

    sw.Stop()
    printfn "FParsec inner loop: %d ms total, %d bytes (%d per parse) (sum: %s)"
        sw.ElapsedMilliseconds bytes (bytes / 20L) (string acc)
    0
