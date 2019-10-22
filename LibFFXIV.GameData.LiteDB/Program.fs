module Program

open System

[<EntryPoint>]
let main args = 
    let col = new LibFFXIV.GameData.LiteDB.Caching.LiteDBXivCollection()
    //col.RebuildCache()
    //printfn "Done!"
    let ic = col :> LibFFXIV.GameData.Raw.IXivCollection
    let item = ic.GetSheet("Item")
    for test in item do
        printfn "%s" (test.Value.As<string>("Name"))
    Console.ReadLine() |> ignore
    0