module rec LibFFXIV.GameData.Raw.ProviderImplementation.Internal

#nowarn "25"

open System

open ProviderImplementation.ProvidedTypes

open LibFFXIV.GameData.Raw


let private ProvideCellTypeCore (tp : TypeProviderForNamespaces) (typeName : string) =
    let cellType =
        ProvidedTypeDefinition(
            asm,
            ns,
            sprintf "Cell_%s" typeName,
            Some typeof<TypedCell>,
            hideObjectMethods = true,
            nonNullable = true
        )

    if hdrCache.ContainsKey(typeName) then
        cellType.AddMemberDelayed
            (fun () -> // this是最后一个，因为定义是empty所以只有一个this
                ProvidedMethod(
                    methodName = "AsRow",
                    parameters = List.empty,
                    returnType = ProvideRowType tp typeName,
                    invokeCode =
                        (fun [ cell ] -> // this是最后一个，因为定义是empty所以只有一个this
                            <@@ let cell = (%%cell : TypedCell)
                                let key = cell.AsInt()

                                let sht =
                                    cell.Row.Sheet.Collection.GetSheet(typeName)

                                sht.Item(key) @@>)
                ))

    cellType

let ProvideCellType (tp : TypeProviderForNamespaces) (typeName : string) =
    pTypeCache.GetOrAdd(
        sprintf "Cell_%s" typeName,
        (fun _ ->
            let pt = ProvideCellTypeCore tp typeName
            pt)
    )
    |> fun pt ->
        tp.AddNamespace(ns, [ pt ])
        pt

let private ProvideRowTypeCore (tp : TypeProviderForNamespaces) (shtName : string) =
    let tyRowType =
        ProvidedTypeDefinition(
            asm,
            ns,
            sprintf "Row_%s" shtName,
            Some typeof<XivRow>,
            hideObjectMethods = true,
            nonNullable = true
        )

    let mutable ret = List.empty<_>

    for hdr in hdrCache.[shtName] do
        match hdr with
        | TypedHeaderItem.NoName(colIdx, typeName) ->
            let prop =
                ProvidedProperty(
                    propertyName = sprintf "UnknownCol_%i" colIdx,
                    propertyType = ProvideCellType tp typeName,
                    getterCode = (fun [ row ] -> <@@ TypedCell((%%row : XivRow), colIdx) @@>)
                )

            let xmldoc =
                sprintf "字段 %s.[%i] : %s" shtName colIdx typeName

            prop.AddXmlDoc(xmldoc)
            ret <- prop :: ret
        | TypedHeaderItem.Normal(colName, typeName) ->
            let prop =
                ProvidedProperty(
                    propertyName = colName,
                    propertyType = ProvideCellType tp typeName,
                    getterCode = (fun [ row ] -> <@@ TypedCell((%%row : XivRow), colName) @@>)
                )

            let xmldoc =
                sprintf "字段 %s->%s : %s" shtName colName typeName

            prop.AddXmlDoc(xmldoc)
            ret <- prop :: ret
        | TypedHeaderItem.Array(name, tmpl, typeName, ranges) ->
            let prop =
                ProvidedProperty(
                    propertyName = "KEY_" + name,
                    propertyType = typeof<string>,
                    getterCode = (fun _ -> <@@ tmpl @@>)
                )

            let doc =
                Text
                    .StringBuilder() // 诡异：XmlDoc中\r\n无效，\r\n\r\n才能用
                    .AppendFormat("字段模板 {0}->{1} : {2}\r\n\r\n", shtName, tmpl, typeName)

            for i = 0 to ranges.Length - 1 do
                let min, max = ranges.[i]

                doc.AppendFormat("索引 {0} : {1} -> {2}\r\n\r\n", i, min, max)
                |> ignore

            prop.AddXmlDoc(doc.ToString())
            ret <- prop :: ret

    tyRowType.AddMembers ret

    tyRowType

let private ProvideRowType (tp : TypeProviderForNamespaces) (shtName : string) =
    let typeName = sprintf "Row_%s" shtName

    pTypeCache.GetOrAdd(
        typeName,
        (fun _ ->
            let pt = ProvideRowTypeCore tp shtName
            pt)
    )
    |> fun pt ->
        tp.AddNamespace(ns, [ pt ])
        pt

/// 生成指定表的类型，结果不缓存
let private ProvideSheetTypeCore (tp : TypeProviderForNamespaces) (shtName) =
    let tySheetType =
        ProvidedTypeDefinition(
            asm,
            ns,
            sprintf "Sht_%s" shtName,
            Some typeof<XivSheet>,
            hideObjectMethods = true,
            nonNullable = true
        )

    let rowType = ProvideRowTypeCore tp shtName

    let rowSeqType =
        typedefof<seq<_>>.MakeGenericType (rowType)

    let rowsProp =
        ProvidedProperty(
            propertyName = "Rows",
            propertyType = rowSeqType,
            getterCode = (fun [ this ] -> <@@ (%%this : XivSheet) :> seq<XivRow> @@>)
        )

    rowsProp.AddXmlDoc(sprintf "Get typed rows of %s" shtName)

    tySheetType.AddMember rowType
    tySheetType.AddMember rowsProp

    tySheetType

/// 从缓存中获取或生成指定表的类型
let ProvideSheetType (tp : TypeProviderForNamespaces) (shtName) =
    pTypeCache.GetOrAdd(
        shtName,
        let pt = ProvideSheetTypeCore tp shtName
        pt
    )
    |> fun pt ->
        tp.AddNamespace(ns, [ pt ])
        pt
