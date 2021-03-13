namespace LibFFXIV.GameData.Provider

#nowarn "25"

open System.IO.Compression
open System.Collections.Generic
open System.Text
open System.Reflection

open ProviderImplementation.ProvidedTypes

open LibFFXIV.GameData
open LibFFXIV.GameData.Raw


[<Sealed>]
type ProviderContext(hdrCache : IReadOnlyDictionary<string, TypedHeaderItem []>) =
    let mainNS = "LibFFXIV.GameData.Provided"
    let internalNS = "LibFFXIV.GameData.Provided"

    let asm = Assembly.GetExecutingAssembly()

    let mainCache = Dictionary<string, ProvidedTypeDefinition>()

    let internalCache = Dictionary<string, ProvidedTypeDefinition>()

    let mutable internalList = List.empty<_> 

    member x.ProvideFor (tp : TypeProviderForNamespaces, providerName) =
        let mutable registedTypes = HashSet<ProvidedTypeDefinition>()

        let tpType =
            if not <| mainCache.ContainsKey("MAIN") then
                mainCache.["MAIN"] <- x.CreateCollectionType(providerName)
            mainCache.["MAIN"]

        tp.AddNamespace(mainNS, [ tpType ])

        if internalList.Length <> internalCache.Count then
            internalList <- internalCache.Values |> Seq.toList

        tp.AddNamespace(internalNS, internalList)

        tpType

    member x.GetSheetType (shtName : string) =
        let key = sprintf "Sheet_%s" shtName
        if not <| internalCache.ContainsKey(key) then
            internalCache.[key] <- x.CreateSheetType(shtName)
        internalCache.[key]

    member x.GetRowType (shtName : string) =
        let key = sprintf "Row_%s" shtName
        if not <| internalCache.ContainsKey(key) then
            internalCache.[key] <- x.CreateRowType(shtName)
        internalCache.[key]

    member x.GetCellType (shtName : string, hdr : TypedHeaderItem) =
        let key = hdr.GetCacheKey(shtName)
        if not <| internalCache.ContainsKey(key) then
            internalCache.[key] <- x.CreateCellType(shtName, hdr)
        internalCache.[key]

    member private x.CreateCollectionType (typeName : string) =
        let tpType =
            ProvidedTypeDefinition(
                asm,
                mainNS,
                typeName,
                Some typeof<XivCollection>,
                hideObjectMethods = true,
                nonNullable = true
            )

        ProvidedConstructor(
            [ ProvidedParameter("col", typeof<XivCollection>) ],
            invokeCode = fun [ col ] -> <@@ (%%col : XivCollection) @@>
        )
        |> tpType.AddMember

        ProvidedConstructor(
            [ ProvidedParameter("lang", typeof<XivLanguage>)
              ProvidedParameter("archive", typeof<ZipArchive>)
              ProvidedParameter("prefix", typeof<string>) ],
            invokeCode =
                fun [ lang; archive; prefix ] ->
                    <@@ let lang = (%%lang : XivLanguage)
                        let archive = (%%archive : ZipArchive)
                        let prefix = (%%prefix : string)
                        new ZippedXivCollection(lang, archive, prefix) @@>
        )
        |> tpType.AddMember

        // 创建衍生类型

        let deps = ResizeArray(hdrCache.Count)

        for shtName in hdrCache.Keys do
            let tySheetType = x.GetSheetType(shtName)
            deps.Add(tySheetType)

            let p =
                ProvidedProperty(
                    propertyName = shtName,
                    propertyType = tySheetType,
                    getterCode = (fun [ this ] -> <@@ (%%this : XivCollection).GetSheet(shtName) @@>)
                )

            tpType.AddMember p

        tpType

    member private x.CreateSheetType (shtName : string) =
        let tySheetType =
            ProvidedTypeDefinition(
                asm,
                internalNS,
                sprintf "Sheet_%s" shtName,
                Some typeof<XivSheet>,
                hideObjectMethods = true,
                nonNullable = true
            )

        let rowType = x.GetRowType(shtName)

        let rowSeqType =
            typedefof<seq<_>>.MakeGenericType (rowType)

        let rowsProp =
            ProvidedProperty(
                propertyName = "TypedRows",
                propertyType = rowSeqType,
                getterCode = (fun [ this ] -> <@@ (%%this : XivSheet) :> seq<XivRow> @@>)
            )

        rowsProp.AddXmlDoc(sprintf "Get typed rows of %s" shtName)

        tySheetType.AddMember rowsProp

        ProvidedMethod(
            methodName = "GetItemTyped",
            parameters = [ProvidedParameter("", typeof<XivKey>)],
            returnType = rowType,
            invokeCode = (fun [ this; key] -> <@@ (%%this : XivSheet).GetItem((%%key : XivKey)) @@>)
        )
        |> tySheetType.AddMember

        ProvidedMethod(
            methodName = "GetItemTyped",
            parameters = [ProvidedParameter("", typeof<int>)],
            returnType = rowType,
            invokeCode = (fun [ this; key ] -> <@@ (%%this : XivSheet).GetItem((%%key : int)) @@>)
        )
        |> tySheetType.AddMember

        tySheetType

    member private x.CreateRowType (shtName : string) =
        let tyRowType =
            ProvidedTypeDefinition(
                asm,
                internalNS,
                sprintf "Row_%s" shtName,
                Some typeof<XivRow>,
                hideObjectMethods = true,
                nonNullable = true
            )

        let mutable ret = List.empty<_>

        for hdr in hdrCache.[shtName] do
            let prop =
                match hdr with
                | TypedHeaderItem.NoName(colIdx, typeName) ->
                    let prop =
                        ProvidedProperty(
                            propertyName = sprintf "UNK_%i" colIdx,
                            propertyType = x.GetCellType(shtName, hdr),
                            getterCode = (fun [ row ] -> <@@ TypedCell((%%row : XivRow), colIdx) @@>)
                        )

                    prop.AddXmlDoc(sprintf "字段 %s.[%i] : %s" shtName colIdx typeName)

                    prop
                | TypedHeaderItem.Normal(colName, typeName) ->
                    let prop =
                        ProvidedProperty(
                            propertyName = colName,
                            propertyType = x.GetCellType(shtName, hdr),
                            getterCode = (fun [ row ] -> <@@ TypedCell((%%row : XivRow), colName) @@>)
                        )

                    prop.AddXmlDoc(sprintf "字段 %s->%s : %s" shtName colName typeName)

                    prop
                | TypedHeaderItem.Array1D(name, tmpl, typeName, r0) ->
                    let f0, t0 = r0.From, r0.To

                    let prop =
                        ProvidedProperty(
                            propertyName = name,
                            propertyType = x.GetCellType(shtName, hdr),
                            getterCode = (fun [ row ] -> <@@ TypedArrayCell1D((%%row : XivRow), tmpl, f0, t0) @@>)
                        )

                    let doc =
                        StringBuilder() // 诡异：XmlDoc中\r\n无效，\r\n\r\n才能用
                            .AppendFormat("字段模板 {0}->{1} : {2}\r\n\r\n", shtName, tmpl, typeName)
                            .AppendFormat("范围 {0} -> {1}\r\n\r\n", r0.From, r0.To)

                    prop.AddXmlDoc(doc.ToString())
                    prop
                | TypedHeaderItem.Array2D(name, tmpl, typeName, (r0, r1)) ->
                    let f0, t0 = r0.From, r0.To
                    let f1, t1 = r1.From, r1.To

                    let prop =
                        ProvidedProperty(
                            propertyName = name,
                            propertyType = x.GetCellType(shtName, hdr),
                            getterCode =
                                (fun [ row ] -> <@@ TypedArrayCell2D((%%row : XivRow), tmpl, f0, t0, f1, t1) @@>)
                        )

                    let doc =
                        StringBuilder() // 诡异：XmlDoc中\r\n无效，\r\n\r\n才能用
                            .AppendFormat("字段模板 {0}->{1} : {2}\r\n\r\n", shtName, tmpl, typeName)
                            .AppendFormat("范围0 : {0} -> {1}\r\n\r\n", r0.From, r0.To)
                            .AppendFormat("范围1 : {0} -> {1}\r\n\r\n", r1.From, r1.To)

                    prop.AddXmlDoc(doc.ToString())
                    prop

            ret <- prop :: ret

        tyRowType.AddMembers ret

        tyRowType

    member private x.CreateCellType (shtName : string, hdr : TypedHeaderItem) =
        match hdr with
        | TypedHeaderItem.NoName(_, typeName) ->
            let cellType =
                ProvidedTypeDefinition(
                    asm,
                    internalNS,
                    hdr.GetCacheKey(shtName),
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
                            returnType = x.GetRowType(typeName),
                            invokeCode =
                                (fun [ cell ] -> // this是最后一个，因为定义是empty所以只有一个this
                                    <@@ let cell = (%%cell : TypedCell)
                                        let key = cell.AsInt()

                                        let sht =
                                            cell.Row.Sheet.Collection.GetSheet(typeName)

                                        sht.GetItem(key) @@>)
                        ))

            cellType
        | TypedHeaderItem.Normal(_, typeName) ->
            let cellType =
                ProvidedTypeDefinition(
                    asm,
                    internalNS,
                    hdr.GetCacheKey(shtName),
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
                            returnType = x.GetRowType(typeName),
                            invokeCode =
                                (fun [ cell ] -> // this是最后一个，因为定义是empty所以只有一个this
                                    <@@ let cell = (%%cell : TypedCell)
                                        let key = cell.AsInt()

                                        let sht =
                                            cell.Row.Sheet.Collection.GetSheet(typeName)

                                        sht.GetItem(key) @@>)
                        ))

            cellType
        | TypedHeaderItem.Array1D(_, _, typeName, _) ->
            let cellType =
                ProvidedTypeDefinition(
                    asm,
                    internalNS,
                    hdr.GetCacheKey(shtName),
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
                            returnType = x.GetRowType(typeName).MakeArrayType(),
                            invokeCode =
                                (fun [ cell ] -> // this是最后一个，因为定义是empty所以只有一个this
                                    <@@ let cell = (%%cell : TypedArrayCell1D)

                                        let sht =
                                            cell.Row.Sheet.Collection.GetSheet(typeName)

                                        cell.AsInts()
                                        |> Array.map (fun key -> sht.GetItem(key)) @@>)
                        ))

            cellType
        | TypedHeaderItem.Array2D(_, _, typeName, _) ->
            let cellType =
                ProvidedTypeDefinition(
                    asm,
                    internalNS,
                    hdr.GetCacheKey(shtName),
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
                            returnType = x.GetRowType(typeName).MakeArrayType(2),
                            invokeCode =
                                (fun [ cell ] -> // this是最后一个，因为定义是empty所以只有一个this
                                    <@@ let cell = (%%cell : TypedArrayCell2D)

                                        let sht =
                                            cell.Row.Sheet.Collection.GetSheet(typeName)

                                        cell.AsInts()
                                        |> Array2D.map (fun key -> sht.GetItem(key)) @@>)
                        ))

            cellType
