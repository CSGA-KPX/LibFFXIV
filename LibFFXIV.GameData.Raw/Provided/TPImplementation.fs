module rec LibFFXIV.GameData.Raw.ProviderImplementation.Internal

#nowarn "25"

open ProviderImplementation.ProvidedTypes

open LibFFXIV.GameData.Raw


let private ProvideCellTypeCore (tp : TypeProviderForNamespaces) (hdr : XivHeaderItem) =
    let cellType =
        ProvidedTypeDefinition(
            asm,
            ns,
            sprintf "Cell_%s" hdr.TypeName,
            Some typeof<TypedCell>,
            hideObjectMethods = true,
            nonNullable = true
        )

    let doc =
        sprintf "name : %s, id : %s, type : %s" hdr.ColumnName hdr.ColumnName hdr.OrignalKeyName

    cellType.AddXmlDoc(doc)

    if hdrCache.ContainsKey(hdr.TypeName) then
        cellType.AddMemberDelayed
            (fun () ->
                let shtName = hdr.TypeName

                ProvidedMethod(
                    methodName = "AsRow",
                    parameters = List.empty,
                    returnType = ProvideRowType tp shtName,
                    invokeCode =
                        (fun [ cell ] -> // this是最后一个，因为定义是empty所以只有一个this
                            <@@ let cell = (%%cell : TypedCell)
                                let key = cell.AsInt()

                                let sht =
                                    cell.Row.Sheet.Collection.GetSheet(shtName)

                                sht.Item(key) @@>)
                ))

    cellType

let ProvideCellType (tp : TypeProviderForNamespaces) (hdr : XivHeaderItem) =
    let typeName = sprintf "Cell_%s" hdr.TypeName

    pTypeCache.GetOrAdd(
        typeName,
        (fun _ ->
            let pt = ProvideCellTypeCore tp hdr
            tp.AddNamespace(ns, [ pt ])
            pt)
    )

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
        match hdr.ColumnName with
        | "#" -> () // ignore
        | "" ->
            let colIdx = hdr.OrignalKeyName |> int

            let prop =
                ProvidedProperty(
                    propertyName = sprintf "UnknownCol_%i" colIdx,
                    propertyType = ProvideCellType tp hdr,
                    getterCode = (fun [ row ] -> <@@ TypedCell((%%row : XivRow), colIdx) @@>)
                )

            let xmldoc =
                sprintf "字段 %s.[%i] : %s" shtName colIdx hdr.TypeName

            prop.AddXmlDoc(xmldoc)
            ret <- prop :: ret

        | colName ->
            let prop =
                ProvidedProperty(
                    propertyName = colName,
                    propertyType = ProvideCellType tp hdr,
                    getterCode = (fun [ row ] -> <@@ TypedCell((%%row : XivRow), colName) @@>)
                )

            let xmldoc =
                sprintf "字段 %s->%s : %s" shtName colName hdr.TypeName

            prop.AddXmlDoc(xmldoc)
            ret <- prop :: ret

    tyRowType.AddMembers ret

    tyRowType

let private ProvideRowType (tp : TypeProviderForNamespaces) (shtName : string) =
    let typeName = sprintf "Row_%s" shtName

    pTypeCache.GetOrAdd(
        typeName,
        (fun _ ->
            let pt = ProvideRowTypeCore tp shtName
            tp.AddNamespace(ns, [ pt ])
            pt)
    )

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
        tp.AddNamespace(ns, [ pt ])
        pt
    )
