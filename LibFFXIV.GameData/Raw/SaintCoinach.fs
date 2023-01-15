namespace LibFFXIV.GameData.Raw.SaintCoinach

open System
open System.IO
open System.IO.Compression
open System.Text

open Newtonsoft.Json
open Newtonsoft.Json.Linq

open LibFFXIV.GameData.Raw


[<AutoOpen>]
module private helper =
    type JObject with
        member x.TryToObject<'T>(propName: string) =
            if x.ContainsKey(propName) then
                Some <| x.[propName].ToObject<'T>()
            else
                None

type LinkCond = { Key: string; Value: int }

type ComplexLinkData =
    { Sheets: string []
      Project: string option
      Key: string option
      Cond: LinkCond option }

type ConverterType =
    /// 颜色
    | Color
    /// 指定行
    | Generic
    /// 图表，直接抛异常
    | Icon
    /// 查找多个表的id
    | MultiRef of targets: string []
    /// 链接目标表
    | Link of target: string
    // TomestoneOrItemReferenceConverter.cs
    // 多表查id：TomestonesItem>item
    | Tomestone
    // 放弃
    | ComplexLink of links: ComplexLinkData []

    member x.TargetType =
        match x with
        | Color -> "Color"
        | Icon -> "Image"
        | Link sht -> sht
        | Tomestone -> "Item"
        | MultiRef _
        | Generic
        | ComplexLink _ -> "Row"

    static member FromJObject(o: JToken) : ConverterType option =
        if isNull o then
            None
        else
            let o = o :?> JObject

            let ret =
                match o.["type"].ToObject<string>() with
                | "color" -> ConverterType.Color
                | "generic" -> ConverterType.Generic
                | "icon" -> ConverterType.Icon
                | "tomestone" -> ConverterType.Tomestone
                | "multiref" -> ConverterType.MultiRef(o.["targets"].ToObject<string []>())
                | "link" -> ConverterType.Link(o.["target"].ToObject<string>())
                | "complexlink" ->
                    [| let arr = o.["links"] :?> JArray

                       for item in arr do
                           let item = item :?> JObject

                           let sheets =
                               if item.ContainsKey("sheet") then
                                   item.["sheet"].ToObject<string>() |> Array.singleton
                               else
                                   item.["sheets"].ToObject<string []>()

                           let project = item.TryToObject<string>("project")
                           let key = item.TryToObject<string>("key")
                           let cond = item.TryToObject<LinkCond>("when")

                           { Sheets = sheets
                             Project = project
                             Key = key
                             Cond = cond } |]
                    |> ConverterType.ComplexLink
                | value -> invalidArg "ConverterType" $"converterType={value}"

            Some ret

[<JsonConverter(typeof<DataDefintionConverter>)>]
type DataDefintion =
    | SimpleData of index: int * name: string * converter: Option<ConverterType>
    | GroupData of index: int * members: DataDefintion []
    | RepeatData of index: int * count: int * def: DataDefintion

and DataDefintionConverter() =
    inherit JsonConverter<DataDefintion>()

    override x.WriteJson(_: JsonWriter, _: DataDefintion, _: JsonSerializer) =
        raise<unit> <| NotImplementedException()

    override x.ReadJson(r: JsonReader, _, _, _, _) =
        let o = JObject.Load(r)

        let index = o.TryToObject<int32>("index") |> Option.defaultValue (Int32.MinValue) |> ((+) 1) // offset 1 by primary key

        if o.ContainsKey("type") then
            match o.["type"].ToObject<string>() with
            | "repeat" ->
                let count = o.["count"].ToObject<int>()
                let def = o.["definition"].ToObject<DataDefintion>()
                RepeatData(index, count, def)
            | "group" -> GroupData(index, o.["members"].ToObject<DataDefintion []>())
            | value -> invalidArg "DataDefintion" $"dataDefintion={value}"
        else
            let name = o.["name"].ToObject<string>()
            let converter = ConverterType.FromJObject(o.GetValue("converter"))
            SimpleData(index, name, converter)

type SheetDefinition =
    { Sheet: string
      DefaultColumn: string
      IsGenericReferenceTarget: bool
      Definitions: DataDefintion [] }

// 来自json的数据，TypeName统一重设为"Unknown-JSON overrode"
// 看了下源代码，在使用模式下并不依赖这个字段，主要影响TypeProvider（也不会用吧）

[<Sealed>]
type JsonParser =
    static member ParseJson(s: Stream) =
        use r = new JsonTextReader(new StreamReader(s))
        let obj = JObject.Load(r).ToObject<SheetDefinition>()
        JsonParser.ParseDefs(obj.Definitions)

    static member private ParseDefs(defs: seq<DataDefintion>) =
        let out = ResizeArray<int * string>()
        let colIds = ResizeArray<string>([ "key" ])
        let colNames = ResizeArray<string>([ "#" ])
        let mutable currIdx = 0

        let rec dataWalker (data: DataDefintion) (postfix: string) (root: bool) =
            let rootId refId =
                if root then
                    if refId < 0 then
                        currIdx <- 1
                    else
                        currIdx <- refId

            match data with
            | SimpleData (idx, name, _) ->
                rootId idx
                out.Add(currIdx, name + postfix)
                colIds.Add((currIdx).ToString())
                colNames.Add(name + postfix)
                currIdx <- currIdx + 1
            | GroupData (idx, members) ->
                rootId idx

                for m in members do
                    dataWalker m postfix false
            | RepeatData (idx, count, def) ->
                rootId idx

                for i = 0 to count - 1 do
                    let postfix = $"[{i}]{postfix}"
                    dataWalker def postfix false

        for def in defs do
            dataWalker def "" true

        {| Ids = colIds
           Names = colNames
           Cols = out |}
