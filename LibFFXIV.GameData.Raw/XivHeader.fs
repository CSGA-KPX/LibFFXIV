namespace LibFFXIV.GameData.Raw
open System
open System.Collections.Generic

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
    
    member x.GetIndex(str) =
        try
            nameToId.[str]
        with
        | :? KeyNotFoundException as e -> 
            printfn "找不到Key : %s" str
            printfn "已知Key有：%s" (String.Join(" ", nameToId.Keys))
            reraise()

    member x.GetFieldType(id) = 
        let t = idToType.[id]
        if t.ToLowerInvariant() = "row" then
            items.[id].ColumnName
        else
            t

    member x.GetFieldType(str) = idToType.[ nameToId.[str] ]

    member x.Headers = items
