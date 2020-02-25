module LibFFXIV.Network.Constants

type Opcodes = 
    | None        = 0xFFFFus
    | TradeLogData = 0x0370us // 5.11
    | Market       = 0x039Bus // 5.11
    | CharacterNameLookupReply = 0x00F2us // 5.11
    | Chat         = 0x00BBus // 5.11
    | LinkshellList = 0x0000us //4.5 ULK才用，不管了
    | PlayerSpawn   = 0x00A7us // 5.11
    | CFNotify       = 0x0377us //5.11
    | CFNotifyCHN    = 0x0081us //5.11

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

let TargetClientVersion     = "2020.02.14.0000.0000"

type PacketDirection = 
    | In   = 0
    | Out  = 1