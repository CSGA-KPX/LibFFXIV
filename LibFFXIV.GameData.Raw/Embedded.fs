namespace LibFFXIV.GameData.Raw

open System
open System.Collections
open System.Collections.Generic


type internal EmbeddedCsvRows(name : string, col : IXivCollection<seq<string[]>>) as x = 
    let mutable hdr  = col.SheetStroage.GetSheetHeader(name, col.Language)

    member val internal DataSeq = 
        seq {
            let csv = col.SheetStroage.GetSheetData(name, col.Language)
            for fields in csv do 
                let row = new XivRow(x :> IXivSheet, fields)
                yield row
        }

    interface IXivSheet with
        member x.IsMultiRow = raise<bool> (NotSupportedException())
        member x.Header = hdr
        member x.Collection = col :> IXivCollection
        member x.Name = name

        member x.Item (k : XivKey) = raise<XivRow> (NotSupportedException())
        member x.Item (k : int) = raise<XivRow> (NotSupportedException())
        member x.Item (k,a) = raise (NotSupportedException())
        
        member x.GetEnumerator() = 
            x.DataSeq.GetEnumerator()

        member x.GetEnumerator() = 
            x.DataSeq.GetEnumerator() :>IEnumerator

        member x.ContainsKey(key) = raise <| NotSupportedException()

type EmbeddedCsvSheet(name : string, col : IXivCollection<seq<string[]>>) =
    let mutable hdr  = col.SheetStroage.GetSheetHeader(name, col.Language)
    let mutable data : IReadOnlyDictionary<_,_> = null
    let mutable IsMulti = false
    let mutable selectId = None

    let getItem (key : XivKey) = 
        if data.ContainsKey(key) then data.[key]
        else raise <| KeyNotFoundException(sprintf "无法在%s中找到Key=%A" name key)

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
        member x.Item k = getItem(XivKey.FromKey(k))
        member x.Item (k,a) = getItem({Main = k; Alt = a})
        member x.Item key = getItem(key)

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

    static member GetEmbeddedCsv() =
        let archive = 
                let ResName = "LibFFXIV.GameData.Raw.raw-exd-all.zip"
                let assembly = Reflection.Assembly.GetExecutingAssembly()
                let stream = assembly.GetManifestResourceStream(ResName)
                new IO.Compression.ZipArchive(stream, IO.Compression.ZipArchiveMode.Read)
        EmbeddedCsvStroage(archive, "ffxiv-datamining-cn-master/") :> ISheetStroage<seq<string []>>

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
            use reader = new CsvParser.CsvReader(stream, Text.Encoding.UTF8)

            while reader.MoveNext() do 
                yield reader.Current |> Seq.toArray
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

type EmbeddedXivCollection(ss : ISheetStroage<seq<string []>>, lang : XivLanguage) = 
    let cache = new Dictionary<string, IXivSheet>()

    new (lang : XivLanguage) = 
        new EmbeddedXivCollection (EmbeddedCsvStroage.GetEmbeddedCsv(), lang)

    interface IXivCollection<seq<string []>> with
        
        /// 调用后清除缓存
        member x.Dispose() = cache.Clear()

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

        member x.GetRows(name : string) = 
            EmbeddedCsvRows(name, x).DataSeq

        member x.ClearCache() = cache.Clear()

        member x.GetSheet(name) = 
            let sheet = 
                if cache.ContainsKey(name) then
                    cache.[name]
                else
                    let sheet = new EmbeddedCsvSheet(name, x)
                    sheet.InitData()
                    let isheet = sheet :> IXivSheet
                    cache.Add(name, isheet)
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
                    cache.Add(name, isheet)
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
                    cache.Add(name, isheet)
                    isheet
            sheet