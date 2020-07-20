module LibFFXIV.Network.Constants

type Opcodes = 
    | None        = 0xFFFFus
    | TradeLogData = 0x02B4us // 5.15
    | Market       = 0x03CBus // 5.15
    | CharacterNameLookupReply = 0x017Fus // 5.15
    | Chat         = 0x0078us // 5.15
    | LinkshellList = 0x0000us //4.5 ULK才用，不管了
    | PlayerSpawn   = 0x029Dus // 5.15
    | CFNotify       = 0x031Fus //5.15
    | CFNotifyCHN    = 0x02EBus //5.15
    | UnknownInfoUpdate = 0x0179us // 5.15 藏宝图

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

let TargetClientVersion     = "2020.03.18.0000.0000"

type PacketDirection = 
    | In   = 0
    | Out  = 1