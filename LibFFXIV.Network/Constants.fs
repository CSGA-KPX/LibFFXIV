module LibFFXIV.Network.Constants

type Opcodes = 
    | LinkshellList = 0x0000us //4.5 ULK才用，不管了
    | None        = 0xFFFFus
    | TradeLogData = 0x0187us // 5.3
    | Market       = 0x02A4us // 5.3
    | CharacterNameLookupReply = 0x0150us // 5.3
    | Chat         = 0x00BBus // 5.3
    | PlayerSpawn   = 0x00B5us // 5.3
    | CFNotify       = 0x02A2us //5.3 Sample = 012100004820020800000100A402000000000000000000000000000000000000
    | CFNotifyCHN    = 0x01E3us //5.3 Sample = A4020000040000000000000000000000
    | UnknownInfoUpdate = 0x02B0us // 5.3 藏宝图

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

let TargetClientVersion     = "2020.11.17.0000.0000"

type PacketDirection = 
    | In   = 0
    | Out  = 1