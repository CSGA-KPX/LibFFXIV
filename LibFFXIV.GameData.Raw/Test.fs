module internal Test
open System
open LibFFXIV.GameData.Raw

let Test() = 
    let col = new XivCollection(Base.XivLanguage.ChineseSimplified) :> Base.IXivCollection
    col.GetLimitedSheet("Item", [|"Name"|]) |> ignore
    let ccsSheet = col.GetSheet("CompanyCraftSequence")
    let ccs = ccsSheet.[1000] // 小型商店外墙
    [|
        for part in ccs.AsRowArray("CompanyCraftPart", 8) do 
            for proc in part.AsRowArray("CompanyCraftProcess", 3) do 
                let items = 
                    proc.AsRowArray("SupplyItem", 12, false)
                    |> Array.map (fun r -> r.AsRow("Item"))
                    |> Array.map (fun r -> r.As<string>("Name"))
                let amount = 
                    let setAmount = proc.AsArray<uint16>("SetQuantity", 12)
                    let setCount  = proc.AsArray<uint16>("SetsRequired", 12)
                    setAmount
                    |> Array.map2 (fun a b -> a * b |> int ) setCount
                yield! Array.zip items amount |> Array.filter (fun (a,_) -> a <> "")
    |] |> Array.iter (fun (a, b) -> printfn "%s * %i" a b)
    Console.ReadLine() |> ignore

[<EntryPoint>]
let main args = 
    Test()
    //GC.Collect()
    Console.ReadLine() |> ignore
    0