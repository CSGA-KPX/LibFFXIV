namespace LibFFXIV.Network.BasePacket
open LibFFXIV.Network.Utils
open LibFFXIV.Network.Constants

type FFXIVGamePacket = 
    inherit PacketParserBase

    val Magic     : uint16
    val Opcode    : Opcodes
    val Unknown1  : uint32
    val TimeStamp : System.DateTimeOffset
    val Unknown2  : uint32
    val Data      : XIVBinaryReader

    new (r : XIVBinaryReader) = 
        {
            inherit PacketParserBase()
            Magic    = r.ReadUInt16()
            Opcode   = LanguagePrimitives.EnumOfValue<uint16, Opcodes>(r.ReadUInt16())
            Unknown1 = r.ReadUInt32()
            TimeStamp= r.ReadTimeStampSec()
            Unknown2 = r.ReadUInt32()
            Data     = r
        }

    override x.ToString() = 
        let opHex = HexString.ToHex (System.BitConverter.GetBytes(x.Opcode |> uint16) |> Array.rev )

        System.String.Format("TS:{0} OP:{1} Data:{2}", x.TimeStamp.ToLocalTime(), opHex, x.Data.PeekRestBytes() |> HexString.ToHex)

(*
type FFXIVGamePacket = 
    {
        Magic     : uint16
        Opcode    : uint16
        Unknown1  : uint32
        TimeStamp : System.DateTimeOffset
        Unknown2  : uint32
        Data      : ByteArray
    }


    static member ParseFromBytes(data : ByteArray) = 
        use r = data.GetReader()
        {
            Magic    = r.ReadUInt16()
            Opcode   = r.ReadUInt16()
            Unknown1 = r.ReadUInt32()
            TimeStamp= r.ReadTimeStampSec()
            Unknown2 = r.ReadUInt32()
            Data     = new ByteArray(r.ReadRestBytes())
        }

    static member private logger = NLog.LogManager.GetCurrentClassLogger()
*)