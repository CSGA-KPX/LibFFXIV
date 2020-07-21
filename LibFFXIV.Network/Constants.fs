module LibFFXIV.Network.Constants

type Opcodes = 
    | None        = 0xFFFFus
    | TradeLogData = 0x02CEus // 5.2
    | Market       = 0x025Bus // 5.2
    | CharacterNameLookupReply = 0x0344us // 5.2
    | Chat         = 0x0376us // 5.2
    | LinkshellList = 0x0000us //4.5 ULK才用，不管了
    | PlayerSpawn   = 0x03BAus // 5.2
    | CFNotify       = 0x01C0us //5.2 Sample = 012100004820020800000100A402000000000000000000000000000000000000
    | CFNotifyCHN    = 0x016Fus //5.2 Sample = A4020000040000000000000000000000
    | UnknownInfoUpdate = 0x0142us // 5.2 藏宝图

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

let TargetClientVersion     = "2020.07.16.0000.0000"

type PacketDirection = 
    | In   = 0
    | Out  = 1