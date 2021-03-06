namespace LibFFXIV.GameData.Raw.ProviderImplementation

open LibFFXIV.GameData.Raw


type TypedCell(row : XivRow, fieldIdx : int) =

    new(row : XivRow, fieldName : string) =
        let idx = row.Sheet.Header.GetIndex(fieldName) - 1
        TypedCell(row, idx)

    member x.Row = row
    member x.FieldIndex = fieldIdx

    member x.AsInt = row.As<int>(fieldIdx)
    member x.AsUInt = row.As<uint>(fieldIdx)

    member x.AsInt16 = row.As<int16>(fieldIdx)
    member x.AsInt32 = row.As<int32>(fieldIdx)
    member x.AsInt64 = row.As<int64>(fieldIdx)

    member x.AsUInt16 = row.As<uint16>(fieldIdx)
    member x.AsUInt32 = row.As<uint32>(fieldIdx)
    member x.AsUInt64 = row.As<uint64>(fieldIdx)

    member x.AsDouble = row.As<float>(fieldIdx)
    member x.AsSingle = row.As<float32>(fieldIdx)

    member x.AsBool = row.As<bool>(fieldIdx)

    member x.AsString = row.As<string>(fieldIdx)
