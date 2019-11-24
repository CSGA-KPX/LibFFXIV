namespace LibFFXIV.GameData.Raw
open System
open System.Collections
open System.Collections.Generic

type XivSheet(name : string, col : IXivCollection) = 
    static let headetLength = 3
    let mutable hdr  = col.GetSheetHeader(name)
    let mutable data = null
    let mutable tracer = None
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
            let o = col.GetSheetData(name)
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
        member x.Collection = col
        member x.Name = name
        member x.Item k = data.[XivKey.FromKey(k)]
        member x.Item (k,a) = data.[{Main = k; Alt = a}]

        member x.GetEnumerator() = 
            data.Values.GetEnumerator()

        member x.GetEnumerator() = 
            data.Values.GetEnumerator() :>IEnumerator

        member x.FieldTracer = tracer

        member x.EnableTracing () = 
            if tracer = None then
                tracer <- Some (HashSet<int>())

type XivCollection(lang : XivLanguage, ?enableTracing : bool, ?disableCaching : bool) = 
    static let archive = 
        let ResName = "LibFFXIV.GameData.Raw.raw-exd-all.zip"
        let assembly = Reflection.Assembly.GetExecutingAssembly()
        let stream = assembly.GetManifestResourceStream(ResName)
        new IO.Compression.ZipArchive(stream, IO.Compression.ZipArchiveMode.Read)

    static let entriesCache = 
        seq {
            for e in archive.Entries do 
                yield e.FullName, e
        } |> readOnlyDict

    let headerLength = 3

    let cache = new Dictionary<string, IXivSheet>()

    let getCsvName (name) = 
        String.Join(".", name, "csv")

    let getCsvNameLang (name) = 
        String.Join(".", name, lang.ToString(), "csv")

    let enableTracing = defaultArg enableTracing false
    let enableCaching = not (defaultArg disableCaching false)

    member x.GetAllSheets() = 
        seq {
            for kv in entriesCache do 
                if kv.Key.Contains(".csv") then
                    let name = kv.Key.Split('.').[0]
                    yield (x :> IXivCollection).GetSheet(name)
        }

    member private x.GetCsvParser(name : string) =
        seq {
            let sheetName = 
                let i = x :> IXivCollection
                let n = getCsvName(name)
                let nl = getCsvNameLang(name)
                if i.SheetExists(n) then
                    n
                else
                    nl
            use stream = entriesCache.[sheetName].Open()
            use tr     = new IO.StreamReader(stream, new Text.UTF8Encoding(true))
            use reader = new NotVisualBasic.FileIO.CsvTextFieldParser(tr)
            reader.SetDelimiter(',')
            reader.TrimWhiteSpace <- true
            reader.HasFieldsEnclosedInQuotes <- true
            while not reader.EndOfData do
                yield reader.ReadFields()
        }

    interface IDisposable with
        member x.Dispose () = 
            archive.Dispose()

    interface IXivCollection with
        member x.GetSheet(name) = 
            let sheet = 
                if cache.ContainsKey(name) then
                    cache.[name]
                else
                    let sheet = new XivSheet(name, x)
                    sheet.InitData()
                    let isheet = sheet :> IXivSheet
                    if enableCaching then cache.Add(name, isheet)
                    isheet
            if enableTracing then sheet.EnableTracing()
            sheet

        member x.SheetExists (name) = 
            entriesCache.ContainsKey(name)

        member x.IsSheet (str) = 
            let i = x :> IXivCollection
            let n = getCsvName(str)
            let nl = getCsvNameLang(str)
            i.SheetExists(n) || i.SheetExists(nl)

        member x.GetSheetHeader(name : string) =
            let csv = x.GetCsvParser(name)
            let s = csv |> Seq.take headerLength |> Seq.toArray

            Array.map3 (fun a b c -> 
                {
                    XivHeaderItem.OrignalKeyName = a
                    XivHeaderItem.ColumnName     = b
                    XivHeaderItem.TypeName       = c
                }    
            ) s.[0] s.[1] s.[2]
            |> XivHeader

        member x.GetSheetData(name : string) =
            x.GetCsvParser(name)
            |> Seq.skip 3

        /// Manual initialize a XivSheetLimited
        member x.GetSheet(name, names) = 
            let sheet = 
                if cache.ContainsKey(name) then
                    cache.[name]
                else
                    let sheet = new XivSheet(name, x)
                    sheet.SelectFields(names = names)
                    sheet.InitData()
                    let isheet = sheet :> IXivSheet
                    if enableCaching then cache.Add(name, isheet)
                    isheet
            if enableTracing then sheet.EnableTracing()
            sheet

        /// Manual initialize a XivSheetLimited
        member x.GetSheet(name, ids) = 
            let sheet = 
                if cache.ContainsKey(name) then
                    cache.[name]
                else
                    let sheet = new XivSheet(name, x)
                    sheet.SelectFields(ids = ids)
                    sheet.InitData()
                    let isheet = sheet :> IXivSheet
                    if enableCaching then cache.Add(name, isheet)
                    isheet
            if enableTracing then sheet.EnableTracing()
            sheet

        member x.DumpTracedFields() = 
            if not enableTracing then
                failwithf "Does not enable tracing!"
            let sb = new Text.StringBuilder()
            for kv in cache do 
                let name = kv.Key
                let ids  = kv.Value.FieldTracer.Value |> Seq.toArray |> Array.sort |> sprintf "%A"
                sb.AppendFormat("    col.GetLimitedSheet(\"{0}\", ids = {1}) |> ignore\r\n", name, ids) |> ignore
            sb.ToString()