module LibFFXIV.Network.Constants

type Opcodes = 
    | None        = 0xFFFFus
    | TradeLogInfo = 0x0125us // 4.5
    | TradeLogData = 0x012Aus // 4.5
    | Market       = 0x0126us // 4.5
    | CharacterNameLookupReply = 0x018Eus // 4.5
    | Chat         = 0x00F7us // 4.5
    | LinkshellList = 0x0104us //4.5
    | PlayerSpawn   = 0x0175us // 4.5

type PacketTypes = 
    | None             = 0x0000us
    | ClientHelloWorld = 0x0001us
    | ServerHelloWorld = 0x0002us
    | GameMessage      = 0x0003us
    | KeepAliveRequest = 0x0007us
    | KeepAliveResponse= 0x0008us
    | ClientHandShake  = 0x0009us
    | ServerHandShake  = 0x000Aus       


type MarketArea = 
  | LimsaLominsa = 0x0001
  | Gridania     = 0x0002
  | Uldah        = 0x0003
  | Ishgard      = 0x0004
  | Kugane       = 0x0007

let TargetClientVersion     = "2019.08.21.0000.0000"

type PacketDirection = 
    | In   = 0
    | Out  = 1