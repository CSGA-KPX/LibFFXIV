module LibFFXIV.Network.Constants

type Opcodes = 
    | LinkshellList = 0x0000us //4.5 ULK才用，不管了
    | None        = 0xFFFFus
    | TradeLogData = 0x031Dus // 5.21
    | Market       = 0x0175us // 5.21
    | CharacterNameLookupReply = 0x0172us // 5.21
    | Chat         = 0x01F3us // 5.21
    | PlayerSpawn   = 0x03A0us // 5.21
    | CFNotify       = 0x0075us //5.21 Sample = 012100004820020800000100A402000000000000000000000000000000000000
    | CFNotifyCHN    = 0x007Eus //5.21 Sample = A4020000040000000000000000000000
    | UnknownInfoUpdate = 0x025Eus // 5.21 藏宝图

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

let TargetClientVersion     = "2020.08.06.0000.0000"

type PacketDirection = 
    | In   = 0
    | Out  = 1