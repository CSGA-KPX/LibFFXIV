namespace LibFFXIV.GameData.LiteDB.Caching
open LiteDB
open LibFFXIV.GameData.Raw
open System
open System.IO

[<CLIMutable>]
type CachedSheetHeader = 
    {
        Id     : string
        Header : XivHeaderItem []
    }

[<CLIMutable>]
type CachedSheetRow = 
    {
        [<BsonId(false)>]
        Id    : string
        Sheet : string
        Data  : string []
    }

    static member GetKey(sheet, key) = 
        sprintf "%s:%i" sheet key

    static member BuildRecord(sheet, key, data) = 
        {
            Id = CachedSheetRow.GetKey(sheet, key)
            Sheet = sheet
            Data = data
        }


[<AutoOpen>]
module internal LiteCollectionHelper = 
    let mapper = new FSharp.FSharpBsonMapper()
    let db = new LiteDatabase("cache.db", new LiteDB.FSharp.FSharpBsonMapper())
    let sheets = db.GetCollection<CachedSheetHeader>()
    let rows   = db.GetCollection<CachedSheetRow>()

type LiteDBSheet(name : string, col : IXivCollection) = 
    let header = 
        new XivHeader(sheets.FindById(new BsonValue(name)).Header)

    interface IXivSheet with
        member x.Header = header
        member x.Item id = 
            let dbKey = CachedSheetRow.GetKey(name, id)
            let fields = rows.FindById(new BsonValue(dbKey)).Data
            new XivRow(x, fields)
        member x.Collection = col
        member x.Name = name
        member x.FieldTracer = None
        member x.EnableTracing() = raise <| new NotSupportedException()

        member x.GetEnumerator() = 
            let all = rows.Find(Query.EQ("Sheet", new BsonValue(name)))
            seq {
                for row in all do 
                    let row = new XivRow(x, row.Data)
                    let kv = new Collections.Generic.KeyValuePair<int, XivRow>(row.Key, row)
                    yield kv
            } :?> Collections.Generic.IEnumerator<Collections.Generic.KeyValuePair<int, XivRow>>

        member x.GetEnumerator() = 
            (x :> IXivSheet).GetEnumerator() :> Collections.IEnumerator

type LiteDBXivCollection() = 


    member x.RebuildCache() =
        //clear all sheets
        for sheet in db.GetCollectionNames() |> Seq.toArray do 
            db.DropCollection(sheet) |> ignore
        db.Shrink() |> ignore
        rows.EnsureIndex("Sheet")  |> ignore
        let col = new XivCollection(XivLanguage.ChineseSimplified, false, true)
        for sheet in col.GetAllSheets() do 
            printfn "Caching %s" sheet.Name
            sheets.Insert({Id = sheet.Name; Header = sheet.Header.AllHeaders}) |> ignore
            let rowseq = 
                seq {
                    for kv in sheet do     
                        yield CachedSheetRow.BuildRecord(sheet.Name, kv.Key, kv.Value.RawFields)
                }
            rows.InsertBulk(rowseq) |> ignore

    interface IXivCollection with
        member x.GetSheet(name) = new LiteDBSheet(name, x) :> IXivSheet

        member x.IsSheet (str) = (x :> IXivCollection).SheetExists(str)

        member x.SheetExists (name) = sheets.Exists(Query.EQ("_id", new BsonValue(name)))

        member x.GetSelectedSheet(name, ?names, ?ids) = raise <| NotSupportedException()
        member x.GetCsvParser (name) = raise <| NotSupportedException()
        member x.DumpTracedFields() = raise <| NotSupportedException()