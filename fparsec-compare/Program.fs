module Program

[<EntryPoint>]
let main argv =
    match argv with
    | [| "golden" |] -> Golden.main ()
    | [| "bench" |] -> Bench.main ()
    | _ ->
        eprintfn "usage: fparsec-compare [golden|bench]"
        1
