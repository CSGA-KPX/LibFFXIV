﻿namespace LibFFXIV.GameData.Raw

open System
open System.IO.Compression

open LibFFXIV.GameData.Raw


type ZippedXivCollection(lang, zip : ZipArchive, ?pathPrefix : string) =
    inherit XivCollection(lang)

    let headerLength = 3

    let entriesCache =
        seq {
            for e in zip.Entries do
                yield e.FullName, e
        }
        |> readOnlyDict

    let prefix = defaultArg pathPrefix ""

    let getFileName(name) =
        let woLang = prefix + String.Join(".", name, "csv")

        let whLang =
            prefix
            + String.Join(".", name, lang.ToString(), "csv")

        if entriesCache.ContainsKey(woLang)
        then woLang
        elif entriesCache.ContainsKey(whLang)
        then whLang
        else failwithf "找不到表%s : %s/%s" name woLang whLang

    override x.GetSheetCore (name, fields, ids) =
        let csv =
            seq {
                use fs = entriesCache.[getFileName name].Open()

                use csv =
                    new CsvParser.CsvReader(fs, Text.Encoding.UTF8)

                while csv.MoveNext() do
                    yield csv.Current |> Seq.toArray
            }

        let mutable header =
            let s =
                csv |> Seq.take headerLength |> Seq.toArray

            Array.map3
                (fun a b c ->
                    { XivHeaderItem.OrignalKeyName = a
                      XivHeaderItem.ColumnName = b
                      XivHeaderItem.TypeName = c })
                s.[0]
                s.[1]
                s.[2]
            |> XivHeader

        let selected =
            [| yield 0 // key column
               yield! ids

               for name in fields do
                   yield header.GetIndex(name) |]
            |> Array.distinct

        if selected.Length > 1 then
            header <-
                XivHeader(
                    selected
                    |> Array.map (fun i -> header.Headers.[i])
                )

        let sheet = XivSheet(name, x, header)

        csv
        |> Seq.skip 3
        |> Seq.map
            (fun fields ->
                if selected.Length > 1 then
                    let fields =
                        [| for i in selected do
                            yield fields.[i] |]

                    XivRow(sheet, fields)
                else
                    XivRow(sheet, fields))
        |> sheet.SetRowSource

        sheet

    override x.GetAllSheetNames () =
        entriesCache.Keys
        |> Seq.filter (fun path -> path.EndsWith(".csv"))
        |> Seq.map (fun path -> 
            path.[0 .. path.IndexOf(".") - 1].Replace(prefix, ""))

    override x.SheetExists (name) =
        try
            getFileName name |> ignore
            true
        with _ -> false