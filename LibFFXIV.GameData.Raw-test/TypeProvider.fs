module TypeProvider

open System
open System.IO
open System.IO.Compression
open LibFFXIV.GameData
open LibFFXIV.GameData.Provided

[<Literal>]
let archivePath =
    @"K:\Source\Repos\TheBot\TheBot\bin\Debug\staticData\ffxiv-datamining-cn-master.zip"

[<Literal>]
let archivePrefix = @"ffxiv-datamining-cn-master/"

type TypedXivCollection = XivCollectionProvider<archivePath, "none", archivePrefix>

let file =
    File.Open(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read)

let a =
    new ZipArchive(file, ZipArchiveMode.Read)

let col =
    new TypedXivCollection(XivLanguage.None, a, archivePrefix)

// 测试一般字段和查表
for row in col.IKDRouteTable.Rows do
    row.Route.AsInt() |> ignore
    row.Route.AsRow().Name.AsString() |> ignore

// 测试一维数组
for row in col.Item.Rows do
    let baseparams =
        row.BaseParam.AsRows()
        |> Array.choose
            (fun bp ->
                let n = bp.Name.AsString()
                if n = "" then None else Some n)
    printfn "%s %A" (row.Name.AsString()) baseparams

// 测试二维数组
for row in col.SpecialShop.Rows do 
    printfn "%s >>" (row.Name.AsString())
    row.``Item{Receive}``.AsRows()
    |> Array2D.iteri (fun i0 i1 item ->
        let name = item.Name.AsString()
        if name <> "" then
            printfn "\t %i:%i:%s" i0 i1 (item.Name.AsString()) )
    printfn "%s <<" (row.Name.AsString())
    
printfn "测试完毕"