namespace LibFFXIV.GameData.Raw

open System
open System.Collections
open System.Collections.Generic

type EmbeddedCsvSheet(name : string, col : IXivCollection<seq<string[]>>) =
    let mutable hdr  = col.SheetStroage.GetSheetHeader(name, col.Language)
    let mutable data = null
    let mutable IsMulti = false
    let mutable selectId = None

    member internal x.SelectFields(?names : string [], ?ids : int []) = 
        if not (isNull data) then
            invalidOp "SelectFields called after InitData!"
        selectId <-
            [|
                let names = defaultArg names [||]
                let ids = defaultArg ids [||]
                yield 0 // key column
                yield! ids
                for name in names do 
                    yield hdr.GetIndex(name)
            |]
            |> Array.distinct
            |> Array.sort
            |> Some

        let items = hdr.Headers
        hdr <- XivHeader(selectId.Value |> Array.map (fun i -> items.[i]))

    member internal x.InitData() = 
        let csv = 
            let o = col.SheetStroage.GetSheetData(name, col.Language)
            if selectId.IsSome then
                let ids = selectId.Value
                o |> Seq.map (fun fields -> [|for i in ids do yield fields.[i]|])
            else
                o
        data <- seq {
            for fields in csv do 
                let row = new XivRow(x :> IXivSheet, fields)
                yield row.Key, row
        }
        |> readOnlyDict

        IsMulti <- data.Keys |> Seq.exists (fun key -> key.Alt <> 0)

    interface IXivSheet with
        member x.IsMultiRow = IsMulti
        member x.Header = hdr
        member x.Collection = col :> IXivCollection
        member x.Name = name
        member x.Item k = data.[XivKey.FromKey(k)]
        member x.Item (k,a) = data.[{Main = k; Alt = a}]

        member x.GetEnumerator() = 
            data.Values.GetEnumerator()

        member x.GetEnumerator() = 
            data.Values.GetEnumerator() :>IEnumerator

        member x.ContainsKey(key) = 
            data.ContainsKey(key)

type EmbeddedCsvStroage (archive : IO.Compression.ZipArchive, ?pathPrefix : string) = 
    let prefix = defaultArg pathPrefix ""
    let entriesCache = 
        seq {
            for e in archive.Entries do 
                yield e.FullName, e
        } |> readOnlyDict

    static let headerLength = 3

    static let embeddedCsv =
        let archive = 
                let ResName = "LibFFXIV.GameData.Raw.raw-exd-all.zip"
                let assembly = Reflection.Assembly.GetExecutingAssembly()
                let stream = assembly.GetManifestResourceStream(ResName)
                new IO.Compression.ZipArchive(stream, IO.Compression.ZipArchiveMode.Read)
        EmbeddedCsvStroage(archive) :> ISheetStroage<seq<string []>>

    static member EmbeddedCsv = embeddedCsv

    member private x.GetEntryName(name, lang : XivLanguage) = 
        let csvName = prefix + String.Join(".", name, "csv")
        let csvNameLang = prefix + String.Join(".", name, lang.ToString(), "csv")
        
        if entriesCache.ContainsKey(csvName) then csvName
        elif entriesCache.ContainsKey(csvNameLang) then csvNameLang
        else failwithf "找不到表%s : %s/%s" name csvName csvNameLang

    member private x.GetCsvParser(name : string, lang : XivLanguage) =
        seq {
            let sheetName = x.GetEntryName(name, lang)
            use stream = entriesCache.[sheetName].Open()
            use tr     = new IO.StreamReader(stream, new Text.UTF8Encoding(true))
            use reader = new NotVisualBasic.FileIO.CsvTextFieldParser(tr)
            reader.SetDelimiter(',')
            reader.TrimWhiteSpace <- true
            reader.HasFieldsEnclosedInQuotes <- true
            while not reader.EndOfData do
                yield reader.ReadFields()
        }


    interface ISheetStroage<seq<string []>> with
        member x.GetSheetHeader(name : string, lang : XivLanguage) =
            let csv = x.GetCsvParser(name, lang)
            let s = csv |> Seq.take headerLength |> Seq.toArray

            Array.map3 (fun a b c -> 
                {
                    XivHeaderItem.OrignalKeyName = a
                    XivHeaderItem.ColumnName     = b
                    XivHeaderItem.TypeName       = c
                }    
            ) s.[0] s.[1] s.[2]
            |> XivHeader

        member x.GetSheetData(name : string, lang : XivLanguage) =
            x.GetCsvParser(name, lang)
            |> Seq.skip 3

        member x.SheetExists(name : string, lang : XivLanguage) = 
            try
                x.GetEntryName(name, lang) |> ignore
                true
            with
            | _ -> false

        member x.GetSheetNames() = 
            seq {
                for n in entriesCache.Keys do 
                    if n.EndsWith(".csv") then
                        yield (n.[0 .. n.Length - 4 - 1])
            }

type EmbeddedXivCollection(ss : ISheetStroage<seq<string []>>, lang : XivLanguage, disableCaching : bool) = 
    let cache = new Dictionary<string, IXivSheet>()
    let enableCaching = not disableCaching

    new (lang : XivLanguage, ?disableCaching : bool) = 
        let disable = defaultArg disableCaching false
        EmbeddedXivCollection (EmbeddedCsvStroage.EmbeddedCsv, lang, disable)

    interface IXivCollection<seq<string []>> with
        
        member x.GetEnumerator() = 
            (
                seq {
                for name in ss.GetSheetNames() do 
                    yield (x :> IXivCollection<seq<string []>>).GetSheet(name)
                }
            ).GetEnumerator()

        member x.GetEnumerator() = 
            (x :> IXivCollection<seq<string []>>).GetEnumerator() :> IEnumerator

        member x.SheetStroage = ss

        member x.Language = lang

        member x.SheetExists(name) = ss.SheetExists(name, lang)

        member x.GetSheet(name) = 
            let sheet = 
                if cache.ContainsKey(name) then
                    cache.[name]
                else
                    let sheet = new EmbeddedCsvSheet(name, x)
                    sheet.InitData()
                    let isheet = sheet :> IXivSheet
                    if enableCaching then cache.Add(name, isheet)
                    isheet
            sheet

        /// Manual initialize a XivSheetLimited
        member x.GetSheet(name, names) = 
            let sheet = 
                if cache.ContainsKey(name) then
                    cache.[name]
                else
                    let sheet = new EmbeddedCsvSheet(name, x)
                    sheet.SelectFields(names = names)
                    sheet.InitData()
                    let isheet = sheet :> IXivSheet
                    if enableCaching then cache.Add(name, isheet)
                    isheet
            sheet

        /// Manual initialize a XivSheetLimited
        member x.GetSheet(name, ids) = 
            let sheet = 
                if cache.ContainsKey(name) then
                    cache.[name]
                else
                    let sheet = new EmbeddedCsvSheet(name, x)
                    sheet.SelectFields(ids = ids)
                    sheet.InitData()
                    let isheet = sheet :> IXivSheet
                    if enableCaching then cache.Add(name, isheet)
                    isheet
            sheet