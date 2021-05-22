namespace LibFFXIV.GameData.Raw

open System
open System.Collections.Generic


type XivHeaderItem =
    {
      /// Raw column name
      ///
      /// \#, 0, 1, 2, 3 ...
      OrignalKeyName : string
      /// Suggested column name
      ///
      /// as the second row in csv.
      ColumnName : string
      /// Suggested column type name
      ///
      /// as the third row in csv.
      TypeName : string }

[<Struct>]
type XivHeaderIndex =
    /// Raw column index
    ///
    /// Same as OrignalKeyName except \#
    | RawIndex of raw : int
    /// Index associated with XivHeader class
    | HeaderIndex of hdr : int

    /// <summary>
    /// Convert XivHeaderIndex to int index.
    /// </summary>
    member x.RealIndex =
        match x with
        | RawIndex idx -> idx + 1
        | HeaderIndex idx -> idx

[<Sealed>]
type XivHeader(items : XivHeaderItem []) =
    // #,Name,Name,Name,Name,Name
    let nameToId =
        [| for i = 0 to items.Length - 1 do
               let item = items.[i]

               if not <| String.IsNullOrEmpty(item.ColumnName) then
                   yield (item.ColumnName, i) |]
        |> readOnlyDict

    // int32,str,str,str,str,str
    let idToType =
        [| for i = 0 to items.Length - 1 do
               let item = items.[i]

               if item.TypeName.StartsWith("bit") then
                   // remove bit&01 bit&02
                   yield "bit"
               else
                   yield item.TypeName |]


    /// <summary>
    /// Get header index of given column name.
    ///
    /// This index contains the \# column.
    ///
    /// \#->0, 0->1, 1->2...
    /// </summary>
    /// <param name="col">Column name</param>
    member internal x.GetIndex(col) =
        try
            HeaderIndex(nameToId.[col])
        with :? KeyNotFoundException as e ->
            printfn "Unknown column name : %s" col
            printfn "Known names are：%s" (String.Join(" ", nameToId.Keys))
            reraise ()

    /// <summary>
    /// Get type name of given header index.
    /// </summary>
    /// <param name="idx">Header index</param>
    member x.GetFieldType(idx : XivHeaderIndex) =
        let t = idToType.[idx.RealIndex]

        if t.ToLowerInvariant() = "row" then
            items.[idx.RealIndex].ColumnName
        else
            t

    /// <summary>
    /// Get type name of given column name.
    /// </summary>
    /// <param name="col"></param>
    member x.GetFieldType(col) = idToType.[x.GetIndex(col).RealIndex]

    /// <summary>
    /// Get header items.
    /// </summary>
    member x.Headers = items :> IReadOnlyList<_>
