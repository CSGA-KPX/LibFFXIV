namespace LibFFXIV.GameData.Raw
open System
open System.Collections
open System.Collections.Generic

type XivSheet (name : string, col : IXivCollection) as x = 
    static let headetLength = 3
    let mutable hdr  = None
    let mutable data = None
    let mutable tracer = None
    let mutable IsMulti = false
    do
        let csv = col.GetCsvParser(name)
        let header = 
            let s = csv |> Seq.take headetLength |> Seq.toArray
            Array.map3 (fun a b c -> 
                {
                    XivHeaderItem.OrignalKeyName = a
                    XivHeaderItem.ColumnName     = b
                    XivHeaderItem.TypeName       = c
                }    
            ) s.[0] s.[1] s.[2]

        hdr  <- Some(new XivHeader(header))
        data <-
            seq {
                for fields in csv |> Seq.skip 3 do 
                    let row = new XivRow(x :> IXivSheet, fields)
                    if row.Key.AltKey <> 0 && (not IsMulti) then IsMulti <- true
                    yield row.Key, row
            }
            |> readOnlyDict
            |> Some

    interface IXivSheet with
        member x.IsMultiRow = IsMulti
        member x.Header = hdr.Value
        member x.Collection = col
        member x.Name = name
        member x.Item k = data.Value.[XivKey.FromKey(k)]
        member x.Item (k,a) = data.Value.[{Key = k; AltKey = a}]

        member x.GetEnumerator() = 
            data.Value.Values.GetEnumerator()

        member x.GetEnumerator() = 
            data.Value.Values.GetEnumerator() :>IEnumerator

        member x.FieldTracer = tracer

        member x.EnableTracing () = 
            if tracer = None then
                tracer <- Some (new HashSet<int>())

/// Parses only selected column, to save memory
type XivSelectedSheet (name : string, col : IXivCollection, includeNames : string [], includeIds : int []) as x = 
    static let headetLength = 3
    let mutable hdr  = None
    let mutable data = None
    let mutable tracer = None
    let mutable IsMulti = false
    do
        let csv = col.GetCsvParser(name)
        let headerItems = 
            let s = csv |> Seq.take headetLength |> Seq.toArray
            Array.map3 (fun a b c -> 
                {
                    XivHeaderItem.OrignalKeyName = a
                    XivHeaderItem.ColumnName     = b
                    XivHeaderItem.TypeName       = c
                }    
            ) s.[0] s.[1] s.[2]
        
        let ids   = 
            [|
                yield 0 // key column
                let oldHeader   = new XivHeader(headerItems)
                yield! includeIds
                for name in includeNames do 
                    yield oldHeader.GetIndex(name)
            |]
            |> Array.distinct
            |> Array.sort
            
        hdr <- Some(new XivHeader(ids |> Array.map (fun i -> headerItems.[i])))

        data <-
            seq {
                for fields in csv |> Seq.skip 3 do 
                    let selected = 
                        [|
                            for i in ids do 
                                yield fields.[i]
                        |]
                    let row = new XivRow(x :> IXivSheet, selected)
                    if row.Key.AltKey <> 0 && (not IsMulti) then IsMulti <- true
                    yield row.Key, row
            }
            |> readOnlyDict
            |> Some

    interface IXivSheet with
        member x.IsMultiRow = IsMulti
        member x.Header = hdr.Value
        member x.Collection = col
        member x.Name = name
        member x.Item k = data.Value.[XivKey.FromKey(k)]
        member x.Item (k,a) = data.Value.[{Key = k; AltKey = a}]

        member x.GetEnumerator() = 
            data.Value.Values.GetEnumerator()

        member x.GetEnumerator() = 
            data.Value.Values.GetEnumerator() :>IEnumerator

        member x.FieldTracer = tracer

        member x.EnableTracing () = 
            if tracer = None then
                tracer <- Some (new HashSet<int>())

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

    interface IDisposable with
        member x.Dispose () = 
            archive.Dispose()

    interface IXivCollection with
        member x.GetSheet(name) = 
            let sheet = 
                if cache.ContainsKey(name) then
                    cache.[name]
                else
                    printfn "debug : init sheet %s" name
                    let sheet = new XivSheet(name, x) :> IXivSheet
                    if enableCaching then cache.Add(name, sheet)
                    sheet
            if enableTracing then sheet.EnableTracing()
            sheet

        member x.SheetExists (name) = 
            entriesCache.ContainsKey(name)

        member x.IsSheet (str) = 
            let i = x :> IXivCollection
            let n = getCsvName(str)
            let nl = getCsvNameLang(str)
            i.SheetExists(n) || i.SheetExists(nl)

        member x.GetCsvParser(name : string) =
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
                //reader.TextFieldType <- Microsoft.VisualBasic.FileIO.FieldType.Delimited
                reader.SetDelimiter(',')
                //reader.CommentTokens <- Array.empty
                reader.TrimWhiteSpace <- true
                reader.HasFieldsEnclosedInQuotes <- true
                while not reader.EndOfData do
                    yield reader.ReadFields()
            }

        /// Manual initialize a XivSheetLimited, overrides sheet cache if exists.
        member x.GetSheet(name, names) = 
            printfn "debug : init select sheet %s" name
            let sheet = new XivSelectedSheet(name, x, names, [||]) :> IXivSheet

            if enableCaching then cache.Add(name, sheet)

            if enableTracing then sheet.EnableTracing()
            sheet

        /// Manual initialize a XivSheetLimited, overrides sheet cache if exists.
        member x.GetSheet(name, ids) = 
            printfn "debug : init select sheet %s" name
            let sheet = new XivSelectedSheet(name, x, [||], ids) :> IXivSheet

            if enableCaching then cache.Add(name, sheet)

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