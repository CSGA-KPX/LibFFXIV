﻿namespace LibFFXIV.GameData.Raw

open System
open System.IO.Compression

open LibFFXIV.GameData.Raw


[<Sealed>]
/// Implementation of access to zipped csv's.
/// <param name="lang">Language postfix of csv filenames. use None of not present.</param>
/// <param name="zip">ZipArchive to container.</param>
/// <param name="pathPrefix">path prefix to actual csv file.</param>
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

    let getFileName name =
        let woLang = prefix + String.Join(".", name, "csv")

        let whLang =
            prefix
            + String.Join(".", name, lang.ToString(), "csv")

        if entriesCache.ContainsKey(woLang) then
            woLang
        elif entriesCache.ContainsKey(whLang) then
            whLang
        else
            failwithf $"找不到表%s{name} : %s{woLang}/%s{whLang}"
    
    override x.GetSheetUncached name =
        let csv =
            seq {
                use fs = entriesCache.[getFileName name].Open()

                use csv =
                    new CsvParser.CsvReader(fs, Text.Encoding.UTF8)

                while csv.MoveNext() do
                    yield csv.Current |> Seq.toArray
            }

        let header =
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

        let sheet = XivSheet(name, x, header)

        csv
        |> Seq.skip 3
        |> Seq.map (fun fields -> XivRow(sheet, fields))
        |> sheet.SetRowSource

        sheet

    override x.GetAllSheetNames () =
        entriesCache.Keys
        |> Seq.filter (fun path -> path.EndsWith(".csv"))
        |> Seq.map
            (fun path ->
                path.[0..path.IndexOf(".") - 1]
                    .Replace(prefix, ""))

    override x.SheetExists name =
        try
            getFileName name |> ignore
            true
        with _ -> false

    override x.Dispose () = base.Dispose()