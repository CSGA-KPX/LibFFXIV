namespace LibFFXIV.Network.SpecializedPacket
open System
open System.Text
open LibFFXIV.Network.Utils


type PlayerSpawn = 
    inherit PacketParserBase

    val  CurrentServerId    : uint16
    val  OriginalServerId   : uint16
    val  PlayerName         : string
    val  FreeCompanyName    : string

    static member private nonUtf8Replacement = "NONUTF8"
    static member private worldVisit = [| 0uy; 0uy; 0uy; 0uy; 0uy; 0uy|]
    static member private failableUTF8 = Encoding.GetEncoding("utf-8", new EncoderExceptionFallback(), new DecoderReplacementFallback(PlayerSpawn.nonUtf8Replacement))

    new (r : XIVBinaryReader)= 
        r.ReadBytes(4) |> ignore
        let csi = r.ReadUInt16()
        let osi = r.ReadUInt16()
        //x.Logger.Info("csi:{0}, osi:{1}", csi, osi)
        r.ReadBytes(552) |> ignore
        //x.Logger.Info("pname {0}", r.PeekBytes(32) |> HexString.ToHex)
        let pname  = r.ReadFixedUTF8(32)
        r.ReadBytes(26) |> ignore
        let fcname = 
            //x.Logger.Info("fcname {0}", r.PeekBytes(6) |> HexString.ToHex)
            let data = r.ReadBytes(6)
            if data = PlayerSpawn.worldVisit then
                "放浪神加护"
            elif data.[0] = 0uy then
                "无部队"
            else
                PlayerSpawn.failableUTF8.GetString(data).TrimEnd('\x00')
        {
            inherit PacketParserBase()
            CurrentServerId     = csi
            OriginalServerId    = osi
            PlayerName          = pname
            FreeCompanyName     = fcname
        }

