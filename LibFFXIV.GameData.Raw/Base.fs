namespace LibFFXIV.GameData.Raw
open System

type ISheetStroage<'T> = 
    /// 指定语言的表是否准在
    abstract SheetExists : string * XivLanguage -> bool
    abstract GetSheetHeader : string * XivLanguage -> XivHeader
    abstract GetSheetData : string * XivLanguage -> 'T
    abstract GetSheetNames : unit -> seq<string>

type XivRow(sheet : IXivSheet, data : string []) = 

    static let adjustId(id, includeKey : bool option) = 
        let includeKey = defaultArg includeKey false
        if includeKey then
            id
        else
            id + 1

    member x.Sheet = sheet

    member val Key = XivKey.FromString(data.[0])

    member x.RawData = data

    member x.As<'T when 'T :> IConvertible>(id : int, ?includeKey : bool) = 
        let id = adjustId(id, includeKey)
        let t = sheet.Header.GetFieldType(id)
        if t = "int64" then
            let str = data.[id]
            let chunk = str.Split([|',';' '|], StringSplitOptions.RemoveEmptyEntries) |> Array.map (int64)
            let i64 = 
                chunk.[0] + (chunk.[1] <<< 16) + (chunk.[2] <<< 32) + (chunk.[3] <<< 48)
            Convert.ChangeType(i64, typeof<'T>) :?> 'T
        else
            Convert.ChangeType(data.[id], typeof<'T>) :?> 'T

    member x.As<'T when 'T :> IConvertible>(name : string) = 
        x.As<'T>(sheet.Header.GetIndex(name), true)

    member x.AsArray<'T when 'T :> IConvertible>(prefix, len) = 
        [|
            for i = 0 to len - 1 do 
                let key = sprintf "%s[%i]" prefix i
                yield (x.As<'T>(key))
        |]

    member internal x.AsRowRef(id : int, ?includeKey : bool) = 
        let id = adjustId(id, includeKey)
        let str= data.[id]
        let t  = sheet.Header.GetFieldType(id)
        if sheet.Collection.SheetExists(t) then
            {Sheet = t; Key = str |> int32}
        else
            failwithf "Sheet not found in collectio: %s" t

    member internal x.AsRowRef(name : string) = 
        x.AsRowRef(sheet.Header.GetIndex(name), true)

    member x.AsRow(id : int, ?includeKey : bool) = 
        let r = x.AsRowRef(adjustId(id, includeKey), true)
        sheet.Collection.GetSheet(r.Sheet).[r.Key]

    member x.AsRow(str : string) =
        x.AsRow(sheet.Header.GetIndex(str), true)

    member x.AsRowArray(prefix, len) = 
        [|
            for i = 0 to len - 1 do 
                let key = sprintf "%s[%i]" prefix i
                yield (x.AsRowRef(key))
        |]
        |> Array.map (fun r -> sheet.Collection.GetSheet(r.Sheet).[r.Key])

and IXivSheet = 
    inherit Collections.Generic.IEnumerable<XivRow>
    abstract IsMultiRow : bool
    abstract Header : XivHeader
    abstract Item : int -> XivRow
    abstract Item : int * int -> XivRow
    abstract Collection : IXivCollection
    abstract Name : string
    abstract ContainsKey : XivKey -> bool

and IXivCollection = 
    inherit Collections.Generic.IEnumerable<IXivSheet>
    abstract Language : XivLanguage
    abstract SheetExists : name : string -> bool
    abstract GetSheet : name : string -> IXivSheet
    abstract GetSheet : name : string * names : string[] -> IXivSheet
    abstract GetSheet : name : string * ids : int[] -> IXivSheet

type IXivCollection<'T> = 
    inherit IXivCollection
    abstract SheetStroage : ISheetStroage<'T>