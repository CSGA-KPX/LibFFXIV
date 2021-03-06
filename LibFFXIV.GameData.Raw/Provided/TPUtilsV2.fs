module private LibFFXIV.GameData.Raw.ProviderImplementation.UtilsV2

open System
open System.Collections.Generic
open System.IO
open System.IO.Compression
open System.Collections
open System.Reflection

open ProviderImplementation.ProvidedTypes

open FSharp.Core.CompilerServices

open LibFFXIV.GameData.Raw


let ns = "LibFFXIV.GameData.Provided"

let asm = Assembly.GetExecutingAssembly()

/// 查找指定表是否存在
let SheetNameCache = HashSet<string>()

/// 类型缓存
let private providedTypeCache = Dictionary<string, ProvidedTypeDefinition>()

let AddOrGetPT (prefix, name, func : unit -> ProvidedTypeDefinition) = 
    let typeName = sprintf "%s_%s" prefix name
    if not <| providedTypeCache.ContainsKey(typeName) then
        providedTypeCache.[typeName] <- func()
    providedTypeCache.[typeName]
