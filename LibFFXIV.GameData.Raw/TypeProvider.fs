namespace LibFFXIV.GameData.Raw.ProviderImplementation

#nowarn "25"

open System
open System.IO
open System.IO.Compression
open System.Collections
open System.Reflection

open ProviderImplementation.ProvidedTypes

open FSharp.Core.CompilerServices

open LibFFXIV.GameData.Raw


type TypedCell(row : XivRow, fieldName : string) =

    member x.AsInt = row.As<int>(fieldName)
    member x.AsUInt = row.As<uint>(fieldName)

    member x.AsInt16 = row.As<int16>(fieldName)
    member x.AsInt32 = row.As<int32>(fieldName)
    member x.AsInt64 = row.As<int64>(fieldName)

    member x.AsUInt16 = row.As<uint16>(fieldName)
    member x.AsUInt32 = row.As<uint32>(fieldName)
    member x.AsUInt64 = row.As<uint64>(fieldName)

    member x.AsDouble = row.As<float>(fieldName)
    member x.AsSingle = row.As<float32>(fieldName)

    member x.AsBool = row.As<bool>(fieldName)

    member x.AsString = row.As<string>(fieldName)


[<AutoOpen>]
module private Cache =
    let hdrCache =
        Concurrent.ConcurrentDictionary<string, XivHeaderItem []>()

    let BuildCache(lang, archive, prefix) =
        if hdrCache.IsEmpty then
            if not <| File.Exists(archive) then failwithf "指定的zip文件%s不存在" archive

            // 缓存所有表格数据
            use zip =
                new ZipArchive(File.OpenRead(archive), ZipArchiveMode.Read)

            use col =
                new ZippedXivCollection(lang, zip, prefix)

            for name in col.GetAllSheetNames() do
                let hdr = col.GetSheet(name).Header.Headers
                hdrCache.TryAdd(name, hdr) |> ignore

[<TypeProvider>]
type XivCollectionProvider(cfg : TypeProviderConfig) as x =
    inherit TypeProviderForNamespaces(cfg)

    let ns = "LibFFXIV.GameData.Provided"
    let asm = Assembly.GetExecutingAssembly()

    let colProvType =
        ProvidedTypeDefinition(asm, ns, "XivCollectionProvider", None, hideObjectMethods = true, nonNullable = true)

    let tpParameters =
        [ ProvidedStaticParameter("Archive", typeof<string>)
          ProvidedStaticParameter("Language", typeof<string>)
          ProvidedStaticParameter("Prefix", typeof<string>) ]

    let tpParametersFunction(typeName : string) (args : obj []) =
        BuildCache(XivLanguage.FromString(args.[1] :?> string), args.[0] :?> string, args.[2] :?> string)

        let tpType =
            ProvidedTypeDefinition(
                asm,
                ns,
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

        for kv in hdrCache do
            let shtName = kv.Key

            let tySheetType =
                ProvidedTypeDefinition(
                    asm,
                    ns,
                    sprintf "Sht_%s" shtName,
                    Some typeof<XivSheet>,
                    hideObjectMethods = true,
                    nonNullable = true
                )

            let tyRowType =
                ProvidedTypeDefinition(
                    asm,
                    ns,
                    sprintf "Row_%s" shtName,
                    Some typeof<XivRow>,
                    hideObjectMethods = true,
                    nonNullable = true
                )

            // 生成Row上的获取方法
            (fun () ->
                let mutable ret = List.empty<_>

                for hdr in kv.Value do
                    if hdr.ColumnName <> "" then
                        let colName = hdr.ColumnName // <@@ @@>内只能使用简单类型

                        let prop =
                            ProvidedProperty(
                                propertyName = colName,
                                propertyType = typeof<TypedCell>,
                                getterCode = (fun [ row ] -> <@@ TypedCell((%%row : XivRow), colName) @@>)
                            )

                        let xmldoc =
                            sprintf "字段 %s->%s : %s" shtName colName hdr.TypeName

                        prop.AddXmlDoc(xmldoc)
                        ret <- prop :: ret

                ret)
            |> tyRowType.AddMembersDelayed

            // Sht_ShtName上的Rows方法
            let typeRowSeqType =
                typedefof<seq<_>>.MakeGenericType(tyRowType)

            let rowsProp =
                ProvidedProperty(
                    propertyName = "Rows",
                    propertyType = typeRowSeqType,
                    getterCode = (fun [ this ] -> <@@ (%%this : XivSheet) :> seq<XivRow> @@>)
                )

            rowsProp.AddXmlDoc(sprintf "Get typed rows of %s" shtName)
            tySheetType.AddMember rowsProp

            x.AddNamespace(ns, [ tyRowType; tySheetType ])

            let p =
                ProvidedProperty(
                    propertyName = shtName,
                    propertyType = tySheetType,
                    getterCode = (fun [ this ] -> <@@ (%%this : XivCollection).GetSheet(shtName) @@>)
                )

            p.AddXmlDoc(sprintf "GetSheet of %s" shtName)
            tpType.AddMember p

        tpType

    do
        colProvType.DefineStaticParameters(tpParameters, tpParametersFunction)
        x.AddNamespace(ns, [ colProvType ])

[<assembly : TypeProviderAssembly>]
do ()
