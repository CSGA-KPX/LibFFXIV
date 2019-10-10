namespace LibFFXIV.Network.SpecializedPacket
open LibFFXIV.Network.Utils

type CharacterNameLookupReply = 
    inherit PacketParserBase
    val UserId   : string
    val UserName : string

    new (r : XIVBinaryReader) = 
        {
            UserId   = r.ReadHexString(8)
            UserName = r.ReadFixedUTF8(32)
        }