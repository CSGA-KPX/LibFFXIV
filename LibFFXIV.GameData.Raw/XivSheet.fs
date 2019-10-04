namespace LibFFXIV.GameData.Raw
open System
open System.Collections
open System.Collections.Generic

type XivSheet (name : string, col : Base.IXivCollection) as x = 
    static let headetLength = 3
    let mutable hdr  = None
    let mutable data = None

    do
        let csv = col.GetCsvParser(name)
        let header = 
            let s = csv |> Seq.take headetLength |> Seq.toArray
            Array.map3 (fun a b c -> new Base.XivHeaderItem(a, b, c)) s.[0] s.[1] s.[2]

        hdr  <- Some(new Base.XivHeader(header))
        data <-
            [|
                for fields in csv |> Seq.skip 3 do 
                    let row = new Base.XivRow(x :> Base.IXivSheet, fields)
                    yield row.Key, row
            |]
            |> readOnlyDict
            |> Some

    interface Base.IXivSheet with
        member x.Header = hdr.Value
        member x.Collection = col
        member x.Name = name
        member x.Item k = data.Value.[k]

        member x.GetEnumerator() = 
            data.Value.GetEnumerator()

        member x.GetEnumerator() = 
            data.Value.GetEnumerator() :>IEnumerator

/// Parses only selected column, to save memory
type XivSheetLimited (name : string, col : Base.IXivCollection, includeNames : string [], includeIds : int []) as x = 
    static let headetLength = 3
    let mutable hdr  = None
    let mutable data = None

    do
        let csv = col.GetCsvParser(name)
        let headerItems = 
            let s = csv |> Seq.take headetLength |> Seq.toArray
            Array.map3 (fun a b c -> new Base.XivHeaderItem(a, b, c)) s.[0] s.[1] s.[2]
        let ids   = 
            [|
                yield 0 // key column
                let oldHeader   = new Base.XivHeader(headerItems)
                yield! includeIds
                for name in includeNames do 
                    yield oldHeader.GetIndex(name)
            |]
            |> Array.distinct
            |> Array.sort
            
        hdr <- Some(new Base.XivHeader(ids |> Array.map (fun i -> headerItems.[i])))
        data <-
            [|
                for fields in csv |> Seq.skip 3 do 
                    let selected = 
                        [|
                            for i in ids do 
                                yield fields.[i]
                        |]
                    let row = new Base.XivRow(x :> Base.IXivSheet, selected)
                    yield row.Key, row
            |]
            |> readOnlyDict
            |> Some

    interface Base.IXivSheet with
        member x.Header = hdr.Value
        member x.Collection = col
        member x.Name = name
        member x.Item k = data.Value.[k]

        member x.GetEnumerator() = 
            data.Value.GetEnumerator()

        member x.GetEnumerator() = 
            data.Value.GetEnumerator() :>IEnumerator

type XivCollection(lang : Base.XivLanguage) = 
    static let archive = 
        let ResName = "LibFFXIV.GameData.Raw.raw-exd-all.zip"
        let assembly = Reflection.Assembly.GetExecutingAssembly()
        let stream = assembly.GetManifestResourceStream(ResName)
        new IO.Compression.ZipArchive(stream, IO.Compression.ZipArchiveMode.Read)

    static let entriesCache = 
        [|
            for e in archive.Entries do 
                yield e.FullName, e
        |] |> readOnlyDict
    
    let cache = new Dictionary<string, Base.IXivSheet>()

    let getCsvName (name) = 
        String.Join(".", name, "csv")

    let getCsvNameLang (name) = 
        String.Join(".", name, lang.ToString(), "csv")

    interface IDisposable with
        member x.Dispose () = 
            archive.Dispose()

    interface Base.IXivCollection with
        member x.GetSheet(name) = 
            if cache.ContainsKey(name) then
                printfn "sheet cache hit! %s" name
                cache.[name]
            else
                printfn "Init new sheet %s" name
                let sheet = new XivSheet(name, x) :> Base.IXivSheet
                printfn "Init new sheet %s end" name
                cache.Add(name, sheet)
                sheet

        member x.SheetExists (name) = 
            entriesCache.ContainsKey(name)

        member x.IsSheet (str) = 
            let i = x :> Base.IXivCollection
            let n = getCsvName(str)
            let nl = getCsvNameLang(str)
            i.SheetExists(n) || i.SheetExists(nl)

        member x.GetCsvParser(name : string) =
            seq {
                let sheetName = 
                    let i = x :> Base.IXivCollection
                    let n = getCsvName(name)
                    let nl = getCsvNameLang(name)
                    if i.SheetExists(n) then
                        n
                    else
                        nl
                use stream = entriesCache.[sheetName].Open()
                use reader = new Microsoft.VisualBasic.FileIO.TextFieldParser(stream)
                reader.TextFieldType <- Microsoft.VisualBasic.FileIO.FieldType.Delimited
                reader.SetDelimiters(",")
                reader.CommentTokens <- Array.empty
                reader.TrimWhiteSpace <- true
                while not reader.EndOfData do
                    yield reader.ReadFields()
            }

        /// Manual initialize a XivSheetLimited
        member x.GetLimitedSheet(name, ?names, ?ids) = 
            let names = defaultArg names [||]
            let ids   = defaultArg ids   [||]

            printfn "Init limited sheet %s" name
            let sheet = new XivSheetLimited(name, x, names, ids) :> Base.IXivSheet
            printfn "Init limited %s end" name
            cache.Add(name, sheet)
            sheet