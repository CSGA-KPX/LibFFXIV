[<AutoOpen>]
module private LibFFXIV.GameData.Raw.ProviderImplementation.Utils

open System
open System.IO
open System.IO.Compression
open System.Collections
open System.Reflection

open ProviderImplementation.ProvidedTypes

open FSharp.Core.CompilerServices

open LibFFXIV.GameData.Raw

let ns = "LibFFXIV.GameData.Provided"

let asm = Assembly.GetExecutingAssembly()

let hdrCache =
    Concurrent.ConcurrentDictionary<string, XivHeaderItem []>()

let pTypeCache =
    Concurrent.ConcurrentDictionary<string, ProvidedTypeDefinition>()

/// 生成对指定档案文件的cache
let BuildHeaderCacheCore(lang, archive, prefix) =
    if hdrCache.IsEmpty then
        if not <| File.Exists(archive) then
            failwithf "指定的zip文件%s不存在" archive

        use file = File.Open(archive, FileMode.Open, FileAccess.Read, FileShare.Read)

        // 缓存所有表格数据
        use zip =
            new ZipArchive(file, ZipArchiveMode.Read)

        use col =
            new ZippedXivCollection(lang, zip, prefix)

        for name in col.GetAllSheetNames() do
            if not <| name.Contains("/") then
                let hdr = col.GetSheet(name).Header.Headers
                hdrCache.TryAdd(name, hdr) |> ignore

let BuildHeaderCache(lang, archive, prefix) = 
    try BuildHeaderCacheCore(lang, archive, prefix) 
    with 
    | e -> printfn "%O" e