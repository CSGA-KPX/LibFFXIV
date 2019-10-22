namespace LibFFXIV.GameData.Raw
open System
open LibFFXIV.GameData.Raw

type RowKeyType = int

[<RequireQualifiedAccess>]
type XivLanguage = 
    | None
    | Japanese
    | English
    | German
    | French
    | ChineseSimplified
    | ChineseTraditional
    | Korean

    override x.ToString() = 
        match x with
        | None      -> ""
        | Japanese  -> "ja"
        | English   -> "en"
        | German    -> "de"
        | French    -> "fr"
        | Korean    -> "kr"
        | ChineseSimplified  -> "chs"
        | ChineseTraditional -> "cht"

[<CLIMutable>]
type XivSheetReference =    
    {
        Sheet : string
        Key   : int
    }

[<CLIMutable>]
type XivHeaderItem = 
    {
        OrignalKeyName: string
        ColumnName    : string
        TypeName      : string
    }

type XivHeader(items : XivHeaderItem []) = 
    // #,Name,Name,Name,Name,Name
    let nameToId =
        [|
            for i = 0 to items.Length - 1 do 
                let item = items.[i]
                if not <| String.IsNullOrEmpty(item.ColumnName) then
                    yield (item.ColumnName, i)
        |] |> readOnlyDict

    // int32,str,str,str,str,str
    let idToType =
        [|
            for i = 0 to items.Length - 1 do 
                let item = items.[i]
                if item.TypeName.StartsWith("bit") then
                    // 不需要 bit&01之类的信息
                    yield "bit"
                else
                    yield item.TypeName
        |]
    
    member x.GetIndex(str) = nameToId.[str]

    member x.GetFieldType(id) = idToType.[id]

    member x.GetFieldType(str) = idToType.[ nameToId.[str] ]

    member x.AllHeaders = items

    override x.ToString() = 
        let sb = new Text.StringBuilder()
        for kv in nameToId do 
            sb.AppendFormat("nameToId {0} -> {1}\r\n", kv.Key, kv.Value) |> ignore
        for k in idToType do 
            sb.AppendFormat("idToType {0}\r\n", k) |> ignore
        sb.ToString()

type XivRow(sheet : IXivSheet, data : string []) = 
    static let staticCastDict = 
        [|
            "bit"   , (fun str -> bool.Parse(str) |> box)
            "bool"  , (fun str -> bool.Parse(str) |> box)
            "byte"  , (fun str -> Byte.Parse(str) |> box)
            "int16" , (fun str -> Int16.Parse(str) |> box)
            "int32" , (fun str -> Int32.Parse(str) |> box)
            "sbyte" , (fun str -> SByte.Parse(str) |> box)
            "single", (fun str -> Single.Parse(str) |> box)
            "uint16", (fun str -> UInt16.Parse(str) |> box)
            "uint32", (fun str -> UInt32.Parse(str) |> box)
            "str"   , (fun str -> str |> box)
            "int64" , (fun str ->
                        let chunk = str.Split([|',';' '|], StringSplitOptions.RemoveEmptyEntries) |> Array.map (int64)
                        let i64 = 
                            chunk.[0] + (chunk.[1] <<< 16) + (chunk.[2] <<< 32) + (chunk.[3] <<< 48)
                        i64 |> box)
        |] |> readOnlyDict

    let castValue(i : int32) = 
        let str = data.[i]
        let v   = sheet.Header.GetFieldType(i)
        match v with
        | _ when staticCastDict.ContainsKey(v) ->
            staticCastDict.[v](str)
        | _ when sheet.Collection.IsSheet(v)  -> 
            {Sheet = v; Key = str |> int32} |> box
        | _ ->
            str |> box

    // some sheet has duplicate key, they have xxx.y format
    // e.g. AnimaWeapon5SpiritTalk
    member val Key = data.[0].Split('.').[0] |> int

    override x.ToString() = 
        let sb = new Text.StringBuilder()
        sb.Append("{") |> ignore
        let objs = 
            [|
                for i = 0 to data.Length - 1 do 
                    let column = castValue(i)
                    if column :? XivSheetReference then
                        let r = column :?> XivSheetReference
                        yield sprintf "%s(%i)" r.Sheet r.Key
                    else
                        yield column.ToString()
            |]
        sb.Append(String.Join(",", objs))
          .Append("}")
          .ToString()
        
    member private x.AdjustId(id, includeKey : bool option) = 
        let includeKey = defaultArg includeKey false
        let id = 
            if includeKey then
                id
            else
                id + 1
        if sheet.FieldTracer.IsSome then
            sheet.FieldTracer.Value.Add(id) |> ignore
        id

    member x.RawFields = data

    member x.AsRaw(id, ?includeKey : bool) = 
        let id = x.AdjustId(id, includeKey)
        data.[id]

    member x.AsRaw(str) = 
        x.AsRaw(sheet.Header.GetIndex(str), true)

    member x.As<'T>(id, ?includeKey : bool) =
        let id = x.AdjustId(id, includeKey)
        castValue(id) |> unbox<'T>

    member x.As<'T>(str)= x.As<'T>(sheet.Header.GetIndex(str), true)

    member x.AsArray<'T>(prefix, len) = 
        [|
            for i = 0 to len - 1 do 
                let key = sprintf "%s[%i]" prefix i
                yield (x.As<'T>(key))
        |]

    member x.AsRow(id : int, ?includeKey : bool) = 
        let r = x.As<XivSheetReference>(id, includeKey.Value)
        sheet.Collection.GetSheet(r.Sheet).[r.Key]

    member x.AsRow(str : string) = x.AsRow(sheet.Header.GetIndex(str), true)

    /// ignoreZero : ignore zero key value
    member x.AsRowArray(prefix, len, ?ignoreZero : bool) = 
        let ignoreZero = defaultArg ignoreZero true
        let mutable refs = x.AsArray<XivSheetReference>(prefix, len)
        if ignoreZero then
            refs <- refs |> Array.filter (fun r -> r.Key > 0 )
        refs
        |> Array.map (fun r -> sheet.Collection.GetSheet(r.Sheet).[r.Key])
        

and IXivSheet = 
    inherit Collections.Generic.IEnumerable<Collections.Generic.KeyValuePair<int32, XivRow>>
    abstract Header : XivHeader
    abstract Item : RowKeyType -> XivRow
    abstract Collection : IXivCollection
    abstract Name : string
    abstract FieldTracer : Collections.Generic.HashSet<int> option
    abstract EnableTracing : unit -> unit

and IXivCollection = 
    abstract GetSheet : string -> IXivSheet
    abstract GetSelectedSheet : string * ?names : string[] * ?ids : int [] -> IXivSheet
    abstract IsSheet  : string -> bool
    abstract SheetExists : string -> bool
    abstract GetCsvParser : string -> seq<string []>
    abstract DumpTracedFields : unit -> string
