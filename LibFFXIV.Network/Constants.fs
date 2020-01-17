module LibFFXIV.Network.Constants

type Opcodes = 
    | None        = 0xFFFFus
    | TradeLogInfo = 0x0125us // 4.5 unused
    | TradeLogData = 0x0140us // 5.0
    | Market       = 0x013Cus // 5.0
    | CharacterNameLookupReply = 0x0199us // 5.0
    | Chat         = 0x0104us // 5.0
    | LinkshellList = 0x0000us //4.5
    | PlayerSpawn   = 0x017Fus // 5.0
    | CFNotify       = 0x0078us

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
  | LimsaLominsa =  1
  | Gridania     =  2
  | Uldah        =  3
  | Ishgard      =  4
  | Kugane       =  7
  | Crystarium   = 10

let TargetClientVersion     = "2019.12.12.0000.0000"

type PacketDirection = 
    | In   = 0
    | Out  = 1