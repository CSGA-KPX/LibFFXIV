namespace LibFFXIV.GameData.Testing.TypeProvider

open System
open System.IO
open System.IO.Compression

open LibFFXIV.GameData
open LibFFXIV.GameData.Provided

open NUnit.Framework
open FsUnit
open FsUnitTyped
open LibFFXIV.GameData.Testing.TestResource


type TypedXivCollection = XivCollectionProvider<archivePath, "none", archivePrefix>

[<TestFixture>]
type ProviderTest() =
    let col = new TypedXivCollection(XivLanguage.None, archivePath, archivePrefix)

    interface IDisposable with
        member x.Dispose() = col.Dispose()

    [<Test>]
    member x.TestNormalColumnAndRowLookup() =
        // 测试一般字段和查表
        for row in col.IKDRouteTable do
            row.Route.AsInt() |> ignore
            row.Route.AsRow().Name.AsString() |> ignore

    [<Test>]
    member x.TestArrayColumnAndRowLookup() =
        for row in col.SpecialShop do
            printfn "%s >>" (row.Name.AsString())

            // 测试一维数组
            let achivevments = row.AchievementUnlock.AsRows() |> Array.map (fun row -> row.Name.AsString())

            printfn "\t %A" achivevments

            let patchNumbers = row.PatchNumber.AsInts()
            printfn "\t %A" patchNumbers

            // 测试二维数组
            row.``Item{Receive}``.AsRows()
            |> Array2D.iteri (fun i0 i1 item ->
                let name = item.Name.AsString()

                if name <> "" then
                    printfn "	 %i:%i:%s" i0 i1 (item.Name.AsString()))

            printfn "%s <<" (row.Name.AsString())
