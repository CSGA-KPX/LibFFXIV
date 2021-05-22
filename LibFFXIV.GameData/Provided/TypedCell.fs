namespace LibFFXIV.GameData.Provider


open System

open LibFFXIV.GameData.Raw

[<Sealed>]
type TypedCell(row : XivRow, idx : XivHeaderIndex) =

    new(row : XivRow, fieldName : string) =
        let idx = row.Sheet.Header.GetIndex(fieldName)
        TypedCell(row, idx)

    /// Get the untyped XivRow
    member x.Row = row
    
    /// XivHeaderIndex of this cell
    member x.Index = idx

    member x.AsInt () = row.As<int>(idx)
    member x.AsUInt () = row.As<uint>(idx)

    member x.AsInt16 () = row.As<int16>(idx)
    member x.AsInt32 () = row.As<int32>(idx)
    member x.AsInt64 () = row.As<int64>(idx)

    member x.AsUInt16 () = row.As<uint16>(idx)
    member x.AsUInt32 () = row.As<uint32>(idx)
    member x.AsUInt64 () = row.As<uint64>(idx)

    member x.AsDouble () = row.As<float>(idx)
    member x.AsSingle () = row.As<float32>(idx)

    member x.AsBool () = row.As<bool>(idx)

    member x.AsString () = row.As<string>(idx)