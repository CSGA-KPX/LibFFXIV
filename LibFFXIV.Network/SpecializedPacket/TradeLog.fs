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
            let tailHex = HexString.ToHex(tail.Value)
            if tailHex <> "00000000" then
                let msg = sprintf "TradeLogPacket should not have tail bytes. %s" tailHex
                Logger.Warn<TradeLogPacket>(msg)
        {
            ItemID  = itemId
            Records = chunks |> Array.map (fun x -> new TradeLog(x))
        }