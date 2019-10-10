namespace LibFFXIV.Network.SpecializedPacket
open System
open LibFFXIV.Network.Utils

type LinkshellListRecord = 
    {
        UserId   : string
        ServerId : uint16
        UserName : string
    }


type LinkshellListPacket = 
    inherit PacketParserBase
    val Header  : byte []
    val Records : LinkshellListRecord []

    new (r : XIVBinaryReader) = 
        let header = r.ReadBytes(8)
        let chunks, tail = r.ReadRestBytesAsChunk(72, false)

        let r = 
            [|
                if BitConverter.ToUInt64(header, 0) <> 0UL && (chunks.Length <> 0)  then
                    for chunk in chunks do
                        use r = XIVBinaryReader.FromBytes(chunk)
                        let userid = r.ReadHexString(8)
                        let serverid = 
                            r.ReadBytes(16) |> ignore
                            r.ReadUInt16()
                        let username =
                            r.ReadBytes(7) |> ignore
                            r.ReadFixedUTF8(32)
                        if userid <> "0000000000000000" then
                            yield {UserId = userid; ServerId = serverid; UserName = username}
            |]
        {Header = header; Records = r}

(*
type LinkshellListPacket = 
    {
        Header  : byte []
        Records : LinkshellListRecord []
    }

    static member ParseFromBytes(r : XIVBinaryReader) = 
        let header = r.ReadBytes(8)
        let chunks, tail = r.ReadRestBytesAsChunk(72, false)

        let r = 
            [|
                if BitConverter.ToUInt64(header, 0) <> 0UL && (chunks.Length <> 0)  then
                    for chunk in chunks do
                        use r = XIVBinaryReader.FromBytes(chunk)
                        let userid = r.ReadUInt64()
                        let serverid = 
                            r.ReadBytes(16) |> ignore
                            r.ReadUInt16()
                        let username =
                            r.ReadBytes(7) |> ignore
                            r.ReadFixedUTF8(32)
                        if userid <> 0UL then
                            yield {UserID = userid; ServerID = serverid; UserName = username}
            |]
        {Header = header; Records = r}*)