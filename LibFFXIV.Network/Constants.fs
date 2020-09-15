module LibFFXIV.Network.Constants

type Opcodes = 
    | LinkshellList = 0x0000us //4.5 ULK才用，不管了
    | None        = 0xFFFFus
    | TradeLogData = 0x03BEus // 5.25
    | Market       = 0x021Fus // 5.25
    | CharacterNameLookupReply = 0x0130us // 5.25
    | Chat         = 0x0106us // 5.25
    | PlayerSpawn   = 0x035Eus // 5.25
    | CFNotify       = 0x0357us //5.25 Sample = 012100004820020800000100A402000000000000000000000000000000000000
    | CFNotifyCHN    = 0x0389us //5.25 Sample = A4020000040000000000000000000000
    | UnknownInfoUpdate = 0x028Bus // 5.25 藏宝图

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

let TargetClientVersion     = "2020.09.08.0000.0000"

type PacketDirection = 
    | In   = 0
    | Out  = 1