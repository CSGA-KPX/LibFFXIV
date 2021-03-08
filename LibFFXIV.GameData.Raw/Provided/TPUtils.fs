[<AutoOpen>]
module private LibFFXIV.GameData.Raw.ProviderImplementation.Utils

open System
open System.IO
open System.IO.Compression
open System.Collections
open System.Collections.Generic
open System.Text.RegularExpressions
open System.Reflection

open ProviderImplementation.ProvidedTypes

open LibFFXIV.GameData.Raw

let ns = "LibFFXIV.GameData.Provided"

let asm = Assembly.GetExecutingAssembly()

type TypedHeaderItem =
    | NoName of KeyIdx : int * TypeName : string
    | Normal of ColName : string * TypeName : string
    | Array of BaseName : string * Template : string * TypeName : string * Range : (int * int) []

let private regex =
    Regex(@"[\(\[]([0-9]+)[\)\]]", RegexOptions.Compiled)

let private ParseArrayIndex(name : string) =
    let mutable matchCount = -1
    let indexes = ResizeArray<int>()

    let baseName = regex.Replace(name, "")

    let formatTemplate =
        regex.Replace(
            name.Replace("{", "{{").Replace("}", "}}"),
            MatchEvaluator
                (fun m ->
                    matchCount <- matchCount + 1
                    indexes.Add(m.Groups.[1].Value |> int)
                    sprintf "[{%i}]" matchCount)
        )

    baseName, formatTemplate, indexes.ToArray()

let ParseHeaders(hdrs : XivHeaderItem []) =
    let ret =
        ResizeArray<TypedHeaderItem>(hdrs.Length)

    let indexed = Dictionary<string, _>()

    for hdr in hdrs do
        match hdr.ColumnName with
        | "#" -> ()
        | "" ->
            NoName(hdr.OrignalKeyName |> int, hdr.TypeName)
            |> ret.Add
        | n when n.Contains("(") || n.Contains("[") ->
            let baseName, tmpl, indexes = ParseArrayIndex(n)

            if not <| indexed.ContainsKey(tmpl) then
                indexed.[tmpl] <-
                    {| BaseName = baseName
                       TypeName = hdr.TypeName
                       Indexes = ResizeArray<int []>() |}

            indexed.[tmpl].Indexes.Add(indexes)
        | _ -> Normal(hdr.ColumnName, hdr.TypeName) |> ret.Add

    for kv in indexed do
        let dimension = kv.Value.Indexes.[0].Length
        let mins = Array.zeroCreate dimension
        let maxs = Array.zeroCreate dimension

        kv.Value.Indexes
        |> Seq.iter
            (fun item ->
                item
                |> Seq.iteri
                    (fun idx value ->
                        mins.[idx] <- min value mins.[idx]
                        maxs.[idx] <- max value maxs.[idx]))

        let ranges = Array.zip mins maxs

        Array(kv.Value.BaseName, kv.Key, kv.Value.TypeName, ranges)
        |> ret.Add

    ret.ToArray()

let hdrCache =
    Concurrent.ConcurrentDictionary<string, TypedHeaderItem []>()

let pTypeCache =
    Concurrent.ConcurrentDictionary<string, ProvidedTypeDefinition>()

/// 生成对指定档案文件的cache
let BuildHeaderCacheCore(lang, archive, prefix) =
    if hdrCache.IsEmpty then
        if not <| File.Exists(archive) then
            failwithf "指定的zip文件%s不存在" archive

        use file =
            File.Open(archive, FileMode.Open, FileAccess.Read, FileShare.Read)

        // 缓存所有表格数据
        use zip =
            new ZipArchive(file, ZipArchiveMode.Read)

        use col =
            new ZippedXivCollection(lang, zip, prefix)

        for name in col.GetAllSheetNames() do
            if not <| name.Contains("/") then
                let hdr = col.GetSheet(name).Header.Headers
                let typed = ParseHeaders(hdr)
                hdrCache.TryAdd(name, typed) |> ignore

let BuildHeaderCache(lang, archive, prefix) =
    try
        BuildHeaderCacheCore(lang, archive, prefix)
    with e -> printfn "%O" e
