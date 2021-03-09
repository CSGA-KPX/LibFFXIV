namespace LibFFXIV.GameData.Raw.ProviderImplementation

open System

open LibFFXIV.GameData.Raw


type TypedCell(row : XivRow, fieldIdx : int) =

    new(row : XivRow, fieldName : string) =
        let idx = row.Sheet.Header.GetIndex(fieldName) - 1
        TypedCell(row, idx)

    member x.Row = row
    member x.FieldIndex = fieldIdx

    member x.AsInt () = row.As<int>(fieldIdx)
    member x.AsUInt () = row.As<uint>(fieldIdx)

    member x.AsInt16 () = row.As<int16>(fieldIdx)
    member x.AsInt32 () = row.As<int32>(fieldIdx)
    member x.AsInt64 () = row.As<int64>(fieldIdx)

    member x.AsUInt16 () = row.As<uint16>(fieldIdx)
    member x.AsUInt32 () = row.As<uint32>(fieldIdx)
    member x.AsUInt64 () = row.As<uint64>(fieldIdx)

    member x.AsDouble () = row.As<float>(fieldIdx)
    member x.AsSingle () = row.As<float32>(fieldIdx)

    member x.AsBool () = row.As<bool>(fieldIdx)

    member x.AsString () = row.As<string>(fieldIdx)

[<Struct>]
type CellRange =
    { From : int
      To : int }

type TypedArrayCell1D(row : XivRow, template : string, from0, to0) =

    member x.Row = row
    member x.FieldTemplate = template

    member internal x.GetFields () =
        if to0 < from0 then
            invalidArg "range" "范围非法"

        if not <| row.Sheet.MetaInfo.ContainsKey(template) then
            row.Sheet.MetaInfo.Add(
                template,
                [| for i = from0 to to0 do
                       yield String.Format(template, i) |]
            )

        row.Sheet.MetaInfo.[template] :?> string []

    member private x.GetItems<'T when 'T :> System.IConvertible> () =
        x.GetFields()
        |> Array.map (fun name -> row.As<'T>(name))

    member x.AsInts () = x.GetItems<int>()
    member x.AsUInts () = x.GetItems<uint>()

    member x.AsInt16s () = x.GetItems<int16>()
    member x.AsInt32s () = x.GetItems<int32>()
    member x.AsInt64s () = x.GetItems<int64>()

    member x.AsUInt16s () = x.GetItems<uint16>()
    member x.AsUInt32s () = x.GetItems<uint32>()
    member x.AsUInt64s () = x.GetItems<uint64>()

    member x.AsDoubles () = x.GetItems<float>()
    member x.AsSingles () = x.GetItems<float32>()

    member x.AsBools () = x.GetItems<bool>()

    member x.AsStrings () = x.GetItems<string>()

type TypedArrayCell2D(row : XivRow, template : string, from0, to0, from1, to1) =

    member x.Row = row
    member x.FieldTemplate = template

    member internal x.GetFields () =
        if to0 < from0 || to1 < from1 then
            invalidArg "range" "范围非法"

        if not <| row.Sheet.MetaInfo.ContainsKey(template) then
            let tmplArray =
                Array2D.initBased
                    from0
                    from1
                    (to0 - from0 + 1)
                    (to1 - from1 + 1)
                    (fun idx0 idx1 -> String.Format(template, idx0, idx1))
            row.Sheet.MetaInfo.Add(template, tmplArray)

        row.Sheet.MetaInfo.[template] :?> string [,]

    member private x.GetItems<'T when 'T :> System.IConvertible> () =
        x.GetFields()
        |> Array2D.map (fun name -> row.As<'T>(name))

    member x.AsInts () = x.GetItems<int>()
    member x.AsUInts () = x.GetItems<uint>()

    member x.AsInt16s () = x.GetItems<int16>()
    member x.AsInt32s () = x.GetItems<int32>()
    member x.AsInt64s () = x.GetItems<int64>()

    member x.AsUInt16s () = x.GetItems<uint16>()
    member x.AsUInt32s () = x.GetItems<uint32>()
    member x.AsUInt64s () = x.GetItems<uint64>()

    member x.AsDoubles () = x.GetItems<float>()
    member x.AsSingles () = x.GetItems<float32>()

    member x.AsBools () = x.GetItems<bool>()

    member x.AsStrings () = x.GetItems<string>()