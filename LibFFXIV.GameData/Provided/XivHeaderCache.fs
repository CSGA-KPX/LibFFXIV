namespace LibFFXIV.GameData.Provider

open System.IO
open System.IO.Compression
open System.Collections.Generic
open System.Text.RegularExpressions

open LibFFXIV.GameData
open LibFFXIV.GameData.Raw


[<Sealed>]
/// Cache sheet headers to speed intellisense lookup.
type XivHeaderCache() =
    static let hdrParseRegex =
        Regex(@"[\(\[]([0-9]+)[\)\]]", RegexOptions.Compiled)

    let mutable cacheState = ""
    let hdrCache = Dictionary<string, TypedHeaderItem []>()

    /// returns true if rebuilt, otherwise false.
    member x.TryBuild(lang : XivLanguage, archive : string, prefix : string) =
        let curStats = $"{lang}%s{archive}%s{prefix}"

        if cacheState <> curStats then
            hdrCache.Clear()
            cacheState <- curStats

            if not <| File.Exists(archive) then
                let fullPath = Path.GetFullPath(archive)
                failwithf $"the specified file %s{fullPath} does not exist."

            use file =
                File.Open(archive, FileMode.Open, FileAccess.Read, FileShare.Read)

            // Cache all table header
            use zip =
                new ZipArchive(file, ZipArchiveMode.Read)

            use col =
                new ZippedXivCollection(lang, zip, prefix)

            for name in col.GetAllSheetNames() do
                if not <| name.Contains("/") then
                    let hdr = col.GetSheet(name).Header.Headers
                    let typed = x.ParseHeaders(hdr |> Seq.toArray)
                    hdrCache.[name] <- typed

            true
        else
            false

    member x.Headers = hdrCache :> IReadOnlyDictionary<_, _>

    member private x.ParseArrayIndex(name : string) =
        let mutable matchCount = -1
        let indexes = ResizeArray<int>()

        let baseName = hdrParseRegex.Replace(name, "")

        let formatTemplate =
            hdrParseRegex.Replace(
                name.Replace("{", "{{").Replace("}", "}}"),
                MatchEvaluator
                    (fun m ->
                        matchCount <- matchCount + 1
                        indexes.Add(m.Groups.[1].Value |> int)
                        $"[{{%i{matchCount}}}]")
            )

        baseName, formatTemplate, indexes.ToArray()

    member private x.ClearHeaderTypeName(hdrs : XivHeaderItem []) =
        // this method modifies input array
        for i = 0 to hdrs.Length - 1 do
            let hdr = hdrs.[i]

            if hdr.TypeName.Contains("&") then
                let idx = hdr.TypeName.IndexOf('&') - 1

                hdrs.[i] <-
                    { hdr with
                          TypeName = hdr.TypeName.[0..idx] }

    member private x.ParseHeaders(hdrs : XivHeaderItem []) =
        x.ClearHeaderTypeName(hdrs)

        let ret =
            ResizeArray<TypedHeaderItem>(hdrs.Length)

        let indexed = Dictionary<string, _>()

        for hdr in hdrs do
            match hdr.ColumnName with
            | "#" -> ()
            | "" ->
                let idx =
                    hdr.OrignalKeyName
                    |> int
                    |> XivHeaderIndex.RawIndex

                NoName(idx, hdr.TypeName) |> ret.Add
            | name ->
                let baseName, tmpl, indexes = x.ParseArrayIndex(name)

                if indexes.Length = 0 then
                    Normal(hdr.ColumnName, hdr.TypeName) |> ret.Add
                else
                    if not <| indexed.ContainsKey(tmpl) then
                        indexed.[tmpl] <-
                            {| BaseName = baseName
                               TypeName = hdr.TypeName
                               Indexes = ResizeArray<int []>() |}

                    indexed.[tmpl].Indexes.Add(indexes)

        for kv in indexed do
            let dimension = kv.Value.Indexes.[0].Length
            let mins = Array.zeroCreate dimension
            let maxs = Array.zeroCreate dimension

            kv.Value.Indexes
            |> Seq.iter
                (fun item ->
                    item
                    |> Seq.iteri
                        (fun idx value ->
                            mins.[idx] <- min value mins.[idx]
                            maxs.[idx] <- max value maxs.[idx]))

            let ranges =
                Array.map2 (fun min max -> { From = min; To = max }) mins maxs

            match ranges with
            | [| r |] -> Array1D(kv.Value.BaseName, kv.Key, kv.Value.TypeName, r)
            | [| r1; r2 |] -> Array2D(kv.Value.BaseName, kv.Key, kv.Value.TypeName, (r1, r2))
            | _ -> failwithf $"Wrong array dimension of %s{kv.Value.BaseName}, expected 1D/2D "
            |> ret.Add

        ret.ToArray()
