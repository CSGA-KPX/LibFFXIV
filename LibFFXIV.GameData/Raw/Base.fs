﻿namespace rec LibFFXIV.GameData.Raw

open System
open System.Collections.Generic

open LibFFXIV.GameData


type XivRow(sheet : XivSheet, data : string []) =

    static let adjustId(id, includeKey : bool option) =
        let includeKey = defaultArg includeKey false
        if includeKey then id else id + 1

    member x.Sheet = sheet

    member val Key = XivKey.FromString(data.[0])

    member x.RawData = data

    member x.As<'T when 'T :> IConvertible> (id : int, ?includeKey : bool) =
        let id = adjustId (id, includeKey)
        let t = sheet.Header.GetFieldType(id)

        if t = "int64" then
            let str = data.[id]

            let chunk =
                str.Split([| ','; ' ' |], StringSplitOptions.RemoveEmptyEntries)
                |> Array.map (int64)

            let i64 =
                chunk.[0]
                + (chunk.[1] <<< 16)
                + (chunk.[2] <<< 32)
                + (chunk.[3] <<< 48)

            Convert.ChangeType(i64, typeof<'T>) :?> 'T
        else
            Convert.ChangeType(data.[id], typeof<'T>) :?> 'T

    member x.As<'T when 'T :> IConvertible> (name : string) =
        x.As<'T>(sheet.Header.GetIndex(name), true)

    member x.AsArray<'T when 'T :> IConvertible> (prefix, len) =
        [| for i = 0 to len - 1 do
               let key = sprintf "%s[%i]" prefix i
               yield (x.As<'T>(key)) |]

    member internal x.AsRowRef (id : int, ?includeKey : bool) =
        let id = adjustId (id, includeKey)
        let str = data.[id]
        let t = sheet.Header.GetFieldType(id)

        if sheet.Collection.SheetExists(t) then
            { Sheet = t; Key = str |> int32 }
        else
            failwithf "Sheet not found in collectio: %s" t

    member internal x.AsRowRef (name : string) =
        x.AsRowRef(sheet.Header.GetIndex(name), true)

    member x.AsRow (id : int, ?includeKey : bool) =
        let r =
            x.AsRowRef(adjustId (id, includeKey), true)

        sheet.Collection.GetSheet(r.Sheet).GetItem(r.Key)

    member x.AsRow (str : string) =
        x.AsRow(sheet.Header.GetIndex(str), true)

    member x.AsRowArray (prefix, len) =
        [| for i = 0 to len - 1 do
               let key = sprintf "%s[%i]" prefix i
               yield (x.AsRowRef(key)) |]
        |> Array.map (fun r -> sheet.Collection.GetSheet(r.Sheet).GetItem(r.Key))

type XivSheet(name, col : XivCollection, hdr) =
    let rowCache = Dictionary<XivKey, XivRow>()
    let mutable cacheInitialized = false
    let mutable rowSeq : seq<XivRow> = Seq.empty

    let metaInfo = Dictionary<string, obj>()

    // 储存临时数据
    member internal x.MetaInfo = metaInfo :> IDictionary<_, _>

    member x.EnsureCached () =
        if not cacheInitialized then
            for row in rowSeq do
                rowCache.Add(row.Key, row)

            cacheInitialized <- true
            rowSeq <- Seq.empty

    member internal x.SetRowSource (seq) = rowSeq <- seq

    member x.Name : string = name

    member x.Collection : XivCollection = col

    member x.Header : XivHeader = hdr

    member x.GetItem (key : XivKey) =
        x.EnsureCached()

        if rowCache.ContainsKey(key) then
            rowCache.[key]
        else
            raise
            <| KeyNotFoundException(sprintf "无法在%s中找到键:%A" name key)

    member x.GetItem (mainIdx : int) =
        x.GetItem({ XivKey.Main = mainIdx; Alt = 0 })

    //member x.GetItem (mainIdx : int, altIdx : int) =
    //    x.GetItem({ XivKey.Main = mainIdx; Alt = altIdx })

    member x.ContainsKey (key) =
        x.EnsureCached()
        rowCache.ContainsKey(key)

    interface IEnumerable<XivRow> with
        member x.GetEnumerator () =
            if cacheInitialized then
                (rowCache.Values |> Seq.map (fun x -> x))
                    .GetEnumerator()
            else
                rowSeq.GetEnumerator()

    interface Collections.IEnumerable with
        member x.GetEnumerator () =
            if cacheInitialized then
                (rowCache.Values |> Seq.map (fun x -> x))
                    .GetEnumerator()
                :> Collections.IEnumerator
            else
                rowSeq.GetEnumerator() :> Collections.IEnumerator

[<AbstractClass>]
type XivCollection(lang) =
    let weakCache =
        Dictionary<string, WeakReference<XivSheet>>()

    member x.Language : XivLanguage = lang

    abstract GetAllSheetNames : unit -> seq<string>

    abstract SheetExists : string -> bool

    abstract GetSheetUncached : name : string -> XivSheet

    member x.GetSheet (name) : XivSheet =

        if weakCache.ContainsKey(name) then
            let succ, sht = weakCache.[name].TryGetTarget()

            if succ then
                sht
            else
                let sht = x.GetSheetUncached(name)
                weakCache.[name].SetTarget(sht)
                sht
        else
            let sht = x.GetSheetUncached(name)
            weakCache.[name] <- WeakReference<_>(sht)
            sht

    /// 释放和本Collection相关的资源
    abstract Dispose : unit -> unit

    default x.Dispose() = weakCache.Clear()

    interface IDisposable with 
        member x.Dispose() = x.Dispose()