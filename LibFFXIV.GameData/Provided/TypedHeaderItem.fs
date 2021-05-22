namespace LibFFXIV.GameData.Provider

open LibFFXIV.GameData.Raw


type TypedHeaderItem =
    | NoName of KeyIdx : XivHeaderIndex * TypeName : string
    | Normal of ColName : string * TypeName : string
    | Array1D of BaseName : string * Template : string * TypeName : string * Range : CellRange
    | Array2D of BaseName : string * Template : string * TypeName : string * Range : (CellRange * CellRange)

    member x.GetCacheKey (shtName : string) =
        match x with
        // Simple cell can be reused across sheets.
        | NoName(_, tn) -> sprintf "Cell_%s" tn
        | Normal(_, tn) -> sprintf "Cell_%s" tn
        // DO NOT reuse array cells, create as sheet-specific.
        | Array1D(_, _, tn, _) -> sprintf "Cell1D_%s_%s" shtName tn
        | Array2D(_, _, tn, _) -> sprintf "Cell2D_%s_%s" shtName tn