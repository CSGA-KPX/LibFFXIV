namespace LibFFXIV.GameData.Provider


type TypedHeaderItem =
    | NoName of KeyIdx : int * TypeName : string
    | Normal of ColName : string * TypeName : string
    | Array1D of BaseName : string * Template : string * TypeName : string * Range : CellRange
    | Array2D of BaseName : string * Template : string * TypeName : string * Range : (CellRange * CellRange)

    member x.GetCacheKey (shtName : string) =
        match x with
        // 不同表内同一个类型都是一样的
        | NoName(_, tn) -> sprintf "Cell_%s" tn
        | Normal(_, tn) -> sprintf "Cell_%s" tn
        // 但是不同表里面数组大小不一定一样
        | Array1D(_, _, tn, _) -> sprintf "Cell_%s_%s" shtName tn
        | Array2D(_, _, tn, _) -> sprintf "Cell_%s_%s" shtName tn