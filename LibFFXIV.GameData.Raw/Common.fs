namespace LibFFXIV.GameData.Raw
open System

[<RequireQualifiedAccess>]
type XivLanguage = 
    | None
    | Japanese
    | English
    | German
    | French
    | ChineseSimplified
    | ChineseTraditional
    | Korean

    override x.ToString() = 
        match x with
        | None      -> ""
        | Japanese  -> "ja"
        | English   -> "en"
        | German    -> "de"
        | French    -> "fr"
        | Korean    -> "kr"
        | ChineseSimplified  -> "chs"
        | ChineseTraditional -> "cht"

[<Struct>]
type XivKey = 
    {
        Main : int
        Alt  : int
    }

    static member FromKey(k) = {Main = k; Alt = 0}
    static member FromString(str: string) = 
        let v = str.Split('.')
        let k = v.[0] |> Int32.Parse
        let a = 
            if v.Length = 2 then
                v.[1] |> Int32.Parse
            else
                0
        {Main = k; Alt = a}

[<Struct>]
type XivSheetReference =    
    {
        Key   : int
        Sheet : string
    }