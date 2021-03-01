module LibFFXIV.Network.Constants

type Opcodes = 
    | LinkshellList = 0x0000us //4.5 ULK才用，不管了
    | None        = 0xFFFFus
    | TradeLogData = 0x028Cus // 5.31
    | Market       = 0x015Eus // 5.31
    | CharacterNameLookupReply = 0x03A3us // 5.31
    | Chat         = 0x021Eus // 5.31
    | PlayerSpawn   = 0x024Aus // 5.31
    | CFNotify       = 0x01C0us //5.31 Sample = 012100004820020800000100A402000000000000000000000000000000000000
    | CFNotifyCHN    = 0x0140us //5.31 Sample = A4020000040000000000000000000000
    | UnknownInfoUpdate = 0x01F6us // 5.31 藏宝图

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

let TargetClientVersion     = "2020.12.21.0000.0000"

type PacketDirection = 
    | In   = 0
    | Out  = 1