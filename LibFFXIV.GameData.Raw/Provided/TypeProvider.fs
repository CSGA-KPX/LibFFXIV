namespace LibFFXIV.GameData.Raw.ProviderImplementation

#nowarn "25"

open System.IO.Compression
open System.Reflection

open ProviderImplementation.ProvidedTypes

open FSharp.Core.CompilerServices

open LibFFXIV.GameData
open LibFFXIV.GameData.Raw


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

    do
        colProvType.DefineStaticParameters(
            tpParameters,
            fun (typeName : string) (args : obj []) ->
                BuildHeaderCache(XivLanguage.FromString(args.[1] :?> string), args.[0] :?> string, args.[2] :?> string)

                pTypeCache.GetOrAdd(
                    typeName,
                    fun _ ->
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

                        for shtName in hdrCache.Keys do
                            let tySheetType = Internal.ProvideSheetType x shtName

                            let p =
                                ProvidedProperty(
                                    propertyName = shtName,
                                    propertyType = tySheetType,
                                    getterCode = (fun [ this ] -> <@@ (%%this : XivCollection).GetSheet(shtName) @@>)
                                )

                            tpType.AddMember p

                        tpType
                )
        )

    do x.AddNamespace(ns, [ colProvType ])

[<assembly : TypeProviderAssembly>]
do ()
