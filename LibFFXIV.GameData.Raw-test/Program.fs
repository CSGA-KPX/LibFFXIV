﻿module LibFFXIV.GameData.Raw.Testing

open NUnit.Framework
open FsUnit
open FsUnitTyped


[<TestFixture>]
type ItemCollectionText() = 
    let col = new EmbeddedXivCollection(XivLanguage.ChineseSimplified) :> IXivCollection

    [<Test>]
    member x.ItemHeader() = 
        let gil = col.GetSheet("Item").[1]
        gil.As<string>("Adjective") |> should equal "0"
        gil.As<string>("IsCollectable") |> should equal "False"
        gil.As<string>("IsGlamourous") |> should equal "False"

    [<Test>]
    member x.SelectedSheet() = 
        let gil = col.GetSheet("Item", [|"Adjective"; "IsCollectable"; "IsGlamourous"|]).[1]
        gil.As<string>("Adjective") |> should equal "0"
        gil.As<string>("IsCollectable") |> should equal "False"
        gil.As<string>("IsGlamourous") |> should equal "False"

    [<Test>]
    member x.TypeConvert() = 
        let gil = col.GetSheet("Item").[1]
        gil.As<string>("IsCollectable") |> should equal "False"
        gil.As<bool>("IsCollectable") |> should equal false

        gil.As<byte>("Rarity") |> should equal 1uy
        gil.As<sbyte>("Rarity") |> should equal 1y
        gil.As<int16>("Rarity") |> should equal 1s
        gil.As<uint16>("Rarity") |> should equal 1us
        gil.As<int32>("Rarity") |> should equal 1
        gil.As<uint32>("Rarity") |> should equal 1u
        gil.As<int64>("Rarity") |> should equal 1L
        gil.As<uint64>("Rarity") |> should equal 1UL

        gil.AsRow("ItemUICategory").Key.Main |> should equal 63