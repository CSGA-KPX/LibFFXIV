namespace LibFFXIV.GameData.Provider

#nowarn "25"

open System.Reflection

open ProviderImplementation.ProvidedTypes

open FSharp.Core.CompilerServices

open LibFFXIV.GameData


[<Sealed>]
[<TypeProvider>]
type XivCollectionProvider(cfg: TypeProviderConfig) as x =
    inherit TypeProviderForNamespaces(cfg)

    let ns = "LibFFXIV.GameData.Provided"
    let asm = Assembly.GetExecutingAssembly()

    let hdrCache = XivHeaderCache()
    let mutable ctx = ProviderContext(hdrCache.Headers)

    let colProvType =
        ProvidedTypeDefinition(asm, ns, "XivCollectionProvider", None, hideObjectMethods = true, nonNullable = true)

    let tpParameters =
        [ ProvidedStaticParameter("Archive", typeof<string>)
          ProvidedStaticParameter("Language", typeof<string>)
          ProvidedStaticParameter("Prefix", typeof<string>) ]

    do
        colProvType.DefineStaticParameters(
            tpParameters,
            fun (typeName: string) (args: obj []) ->
                let lang = XivLanguage.FromString(args.[1] :?> string)
                let archive = args.[0] :?> string
                let prefix = args.[2] :?> string

                if hdrCache.TryBuild(lang, archive, prefix) then
                    ctx <- ProviderContext(hdrCache.Headers)

                ctx.ProvideFor(x, typeName)
        )

    do x.AddNamespace(ns, [ colProvType ])

[<assembly: TypeProviderAssembly>]
do ()
