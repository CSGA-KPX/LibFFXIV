namespace LibFFXIV.Network.BasePacket
open LibFFXIV.Network.Utils
open LibFFXIV.Network.Constants

type FFXIVSubPacket =
    inherit PacketParserBase
    val Size        : uint32
    val SourceId    : uint32
    val TargetId    : uint32
    val Type        : PacketTypes
    val Unknown     : uint16
    val Data        : XIVBinaryReader

    new (b : ByteArray)= 
        let r = b.GetReader()
        let headerSize = 0x10
        let size = r.ReadUInt32() 
        {
            inherit PacketParserBase()
            Size     = size
            SourceId = r.ReadUInt32()
            TargetId = r.ReadUInt32()
            Type     = LanguagePrimitives.EnumOfValue<uint16, PacketTypes>(r.ReadUInt16())
            Unknown  = r.ReadUInt16()
            Data     = 
                if (r.BytesLeft |> int) <> ((size|>int)-headerSize) then
                    Logger.Fatal<FFXIVSubPacket>("SubPacket未结束, Data={0}", b.ToString())
                r
        }

    member x.IsGamePacket() = 
        x.Type = PacketTypes.GameMessage

(*
type FFXIVSubPacket = 
    {
        Size     : uint32
        SourceId : uint32
        TargetId : uint32
        Type     : uint16
        Unknown  : uint16
        Data     : ByteArray
    }
    static member private logger = NLog.LogManager.GetCurrentClassLogger()
    
    member x.IsGamePacket() = 
        x.Type = 0x0003us

    member x.TryGetGamePacket() = 
        if x.IsGamePacket() then
            FFXIVGamePacket.ParseFromBytes(x.Data)
        else
            failwithf "This subpacket is not game packet"

    static member Parse (data : ByteArray) = 
        [|
            use r = data.GetReader()
            while not (r.IsEnd()) do
                let size = r.ReadUInt32()
                let toRead = (int size) - 0x10
                yield { Size     = size
                        SourceId = r.ReadUInt32()
                        TargetId = r.ReadUInt32()
                        Type     = r.ReadUInt16()
                        Unknown  = r.ReadUInt16()
                        Data     = new ByteArray(r.ReadBytes(toRead)) }

            if not (r.IsEnd()) then
                printfn "Subpacket Not END!!!!"
        |]*)