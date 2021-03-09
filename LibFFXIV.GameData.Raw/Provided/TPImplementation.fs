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
                    propertyName = sprintf "UNKNOWN_%i" colIdx,
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

        | TypedHeaderItem.Array1D(name, tmpl, typeName, r0) ->
            let cellType =
                ProvidedTypeDefinition(
                    asm,
                    ns,
                    sprintf "Cell_%s_%s" shtName typeName,
                    Some typeof<TypedArrayCell1D>,
                    hideObjectMethods = true,
                    nonNullable = true
                )

            if hdrCache.ContainsKey(typeName) then
                cellType.AddMemberDelayed
                    (fun () -> // this是最后一个，因为定义是empty所以只有一个this
                        ProvidedMethod(
                            methodName = "AsRows",
                            parameters = List.empty,
                            returnType = (ProvideRowType tp typeName).MakeArrayType(),
                            invokeCode =
                                (fun [ cell ] -> // this是最后一个，因为定义是empty所以只有一个this
                                    <@@ let cell = (%%cell : TypedArrayCell1D)
                                        let sht =
                                            cell.Row.Sheet.Collection.GetSheet(typeName)

                                        cell.AsInts()
                                        |> Array.map (fun key -> sht.Item(key))@@>)
                        ))

            let f0, t0 = r0.From, r0.To
            let prop =
                ProvidedProperty(
                    propertyName = name,
                    propertyType = cellType,
                    getterCode = (fun [ row ] -> <@@ TypedArrayCell1D((%%row : XivRow), tmpl, f0, t0) @@>)
                )

            let doc =
                Text
                    .StringBuilder() // 诡异：XmlDoc中\r\n无效，\r\n\r\n才能用
                    .AppendFormat("字段模板 {0}->{1} : {2}\r\n\r\n", shtName, tmpl, typeName)
                    .AppendFormat("范围 {0} -> {1}\r\n\r\n", r0.From, r0.To)

            prop.AddXmlDoc(doc.ToString())
            tp.AddNamespace(ns, [cellType])
            ret <- prop :: ret

        | TypedHeaderItem.Array2D(name, tmpl, typeName, (r0, r1)) ->
            let cellType =
                ProvidedTypeDefinition(
                    asm,
                    ns,
                    sprintf "Cell_%s_%s" shtName typeName,
                    Some typeof<TypedArrayCell2D>,
                    hideObjectMethods = true,
                    nonNullable = true
                )

            if hdrCache.ContainsKey(typeName) then
                cellType.AddMemberDelayed
                    (fun () -> // this是最后一个，因为定义是empty所以只有一个this
                        ProvidedMethod(
                            methodName = "AsRows",
                            parameters = List.empty,
                            returnType = (ProvideRowType tp typeName).MakeArrayType(2),
                            invokeCode =
                                (fun [ cell ] -> // this是最后一个，因为定义是empty所以只有一个this
                                    <@@ let cell = (%%cell : TypedArrayCell2D)
                                        let sht =
                                            cell.Row.Sheet.Collection.GetSheet(typeName)

                                        cell.AsInts()
                                        |> Array2D.map (fun key -> sht.Item(key))@@>)
                        ))

            let f0, t0 = r0.From, r0.To
            let f1, t1 = r1.From, r1.To
            let prop =
                ProvidedProperty(
                    propertyName = name,
                    propertyType = cellType,
                    getterCode = (fun [ row ] -> <@@ TypedArrayCell2D((%%row : XivRow), tmpl, f0, t0, f1, t1) @@>)
                )

            let doc =
                Text
                    .StringBuilder() // 诡异：XmlDoc中\r\n无效，\r\n\r\n才能用
                    .AppendFormat("字段模板 {0}->{1} : {2}\r\n\r\n", shtName, tmpl, typeName)
                    .AppendFormat("范围0 : {0} -> {1}\r\n\r\n", r0.From, r0.To)
                    .AppendFormat("范围1 : {0} -> {1}\r\n\r\n", r1.From, r1.To)

            prop.AddXmlDoc(doc.ToString())
            tp.AddNamespace(ns, [cellType])
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
