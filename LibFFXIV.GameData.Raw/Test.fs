module internal Test
open System
open LibFFXIV.GameData.Raw

let Test() = 
    let t = DateTime.Now
    let col = new XivCollection(Base.XivLanguage.ChineseSimplified, true) :> Base.IXivCollection
    col.GetSelectedSheet("CompanyCraftSequence", ids = [|6; 7; 8; 9; 10; 11; 12; 13|]) |> ignore
    col.GetSelectedSheet("CompanyCraftPart", ids = [|3; 4; 5|]) |> ignore
    col.GetSelectedSheet("CompanyCraftProcess", ids = [|1; 2; 3; 4; 5; 6; 7; 8; 9; 10; 11; 12; 13; 14; 15; 16; 17; 18; 19; 20; 21; 22;
  23; 24; 25; 26; 27; 28; 29; 30; 31; 32; 33; 34; 35; 36|]) |> ignore
    col.GetSelectedSheet("CompanyCraftSupplyItem", ids = [|1|]) |> ignore
    col.GetSelectedSheet("Item", ids = [|10|]) |> ignore
    //col.GetLimitedSheet("Item", [|"Name"|]) |> ignore
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
    let tt = DateTime.Now
    printfn "%O" (tt-t)
    printfn "%s" (col.DumpTracedFields())

[<EntryPoint>]
let main args = 
    Test()
    let peakMemory = 
        (System.Diagnostics.Process.GetCurrentProcess().PeakWorkingSet64 |> float)
            / 1024.0
            / 1024.0
    printfn "Peak memory usage : %f MB" peakMemory

    //GC.Collect()
    Console.ReadLine() |> ignore
    0