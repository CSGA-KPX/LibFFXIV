namespace LibFFXIV.Network.SpecializedPacket
open Microsoft.FSharp.Core.Operators.Checked
open System
open System.IO
open System.Text
open LibFFXIV.Network.Utils

type TradeLog() = 
    inherit PacketParserBase()
    member val ItemId      : uint32 = 0u with get, set
    member val Price       : uint32 = 0u with get, set
    member val TimeStamp   : uint32 = 0u with get, set
    member val Count       : uint32 = 0u with get, set
    member val IsHQ        : bool   = false with get, set
    member val Unknown     : uint32 = 0u with get, set
    member val BuyerName   : string = "" with get, set
    
    new (chunk : byte[]) as x = 
        new TradeLog()
        then
            Logger.Trace<TradeLog>("TradeLog : {0}", chunk |> HexString.ToHex)
            use r = XIVBinaryReader.FromBytes(chunk)
            x.ItemId      <- r.ReadUInt32()
            x.Price       <- r.ReadUInt32()
            x.TimeStamp   <- r.ReadUInt32()
            x.Count       <- r.ReadUInt32()
            x.IsHQ        <- r.ReadByte() = 1uy
            x.Unknown     <- r.ReadInt16() |> uint32
            x.BuyerName   <- 
                let bytes = r.ReadBytes(34)
                Encoding.UTF8.GetString(bytes.[0 .. (Array.findIndex ((=) 0uy) bytes) - 1])
    static member RecordSize = 104/2


type TradeLogPacket = 
    inherit PacketParserBase
    val ItemID : uint32
    val Records: TradeLog []

    new (r : XIVBinaryReader) = 
        let itemId = r.ReadUInt32()
        let (chunks, tail) = r.ReadRestBytesAsChunk(TradeLog.RecordSize, true)
        if tail.IsSome then
            Logger.Fatal<TradeLogPacket>("TradeLogPacket should not have tail bytes.")
        {
            ItemID  = itemId
            Records = chunks |> Array.map (fun x -> new TradeLog(x))
        }

(*
[<CLIMutableAttribute>]
type TradeLogRecord = 
    {
        ItemID      : uint32
        Price       : uint32
        TimeStamp   : uint32
        Count       : uint32
        IsHQ        : bool
        Unknown     : uint32
        BuyerName   : string
    }
    static member ParseFromBytes(bytes : byte[]) = 
        [|
            let recordSize = 104/2
            let chunks = 
                bytes 
                |> Array.chunkBySize recordSize
                |> Array.filter (fun x -> x.Length = recordSize)
                |> Array.filter (fun x -> IsByteArrayNotAllZero(x))
            for chunk in chunks do
                use ms = new MemoryStream(chunk)
                Logger.Log<TradeLogRecord>(chunk |> HexString.ToHex)
                use r  = new BinaryReader(ms)
                yield {
                    ItemID      = r.ReadUInt32()
                    Price       = r.ReadUInt32()
                    TimeStamp   = r.ReadUInt32()
                    Count       = r.ReadUInt32()
                    IsHQ        = r.ReadByte() = 1uy
                    Unknown     = r.ReadInt16() |> uint32
                    BuyerName  = 
                                    let bytes = r.ReadBytes(34)
                                    Encoding.UTF8.GetString(bytes.[0 .. (Array.findIndex ((=) 0uy) bytes) - 1])
                }
        |]

type TradeLogPacket = 
    {
        ItemID : uint32
        Records: TradeLogRecord []
    }
    static member ParseFromBytes(r : XIVBinaryReader) = 
        let itemId = r.ReadUInt32()
        let restBytes = 
            let bs = r.BaseStream
            let len = bs.Length - bs.Position
            r.ReadBytes(len |> int)
        let records= TradeLogRecord.ParseFromBytes(restBytes)
        {
            ItemID  = itemId
            Records = records
        }*)