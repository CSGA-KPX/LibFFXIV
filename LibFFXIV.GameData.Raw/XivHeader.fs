namespace LibFFXIV.GameData.Raw
open System

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

    member x.Headers = items

    override x.ToString() = 
        let sb = new Text.StringBuilder()
        for kv in nameToId do 
            sb.AppendFormat("nameToId {0} -> {1}\r\n", kv.Key, kv.Value) |> ignore
        for k in idToType do 
            sb.AppendFormat("idToType {0}\r\n", k) |> ignore
        sb.ToString()
