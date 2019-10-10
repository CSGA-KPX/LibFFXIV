namespace LibFFXIV.Network.SpecializedPacket
open System
open LibFFXIV.Network.Utils

type ChatType = 
    | LogKindError = 0us
    | ServerDebug = 1us
    | ServerUrgent = 2us
    | ServerNotice = 3us
    | Unused4 = 4us
    | Unused5 = 5us
    | Unused6 = 6us
    | Unused7 = 7us
    | Unused8 = 8us
    | Unused9 = 9us
    | Say = 10us
    | Shout = 11us
    | Tell = 12us
    | TellReceive = 13us
    | Party = 14us
    | Alliance = 15us
    | LS1 = 16us
    | LS2 = 17us
    | LS3 = 18us
    | LS4 = 19us
    | LS5 = 20us
    | LS6 = 21us
    | LS7 = 22us
    | LS8 = 23us
    | FreeCompany = 24us
    | Unused25 = 25us
    | Unused26 = 26us
    | NoviceNetwork = 27us
    | CustomEmote = 28us
    | StandardEmote = 29us
    | Yell = 30us
    | Unknown31 = 31us
    | PartyUnk2 = 32us
    | Unused33 = 33us
    | Unused34 = 34us
    | Unused35 = 35us
    | Unused36 = 36us
    | Unused37 = 37us
    | Unused38 = 38us
    | Unused39 = 39us
    | Unused40 = 40us
    | BattleDamage = 41us
    | BattleFailed = 42us
    | BattleActions = 43us
    | BattleItems = 44us
    | BattleHealing = 45us
    | BattleBeneficial = 46us
    | BattleDetrimental = 47us
    | BattleUnk48 = 48us
    | BattleUnk49 = 49us
    | Unused50 = 50us
    | Unused51 = 51us
    | Unused52 = 52us
    | Unused53 = 53us
    | Unused54 = 54us
    | Unused55 = 55us
    | Echo = 56us
    | SystemMessage = 57us
    | SystemErrorMessage = 58us
    | BattleSystem = 59us
    | GatheringSystem = 60us
    | NPCMessage = 61us
    | LootMessage = 62us
    | Unused63 = 63us
    | CharProgress = 64us
    | Loot = 65us
    | Crafting = 66us
    | Gathering = 67us
    | NPCAnnouncement = 68us
    | FCAnnouncement = 69us
    | FCLogin = 70us
    | RetainerSale = 71us
    | PartySearch = 72us
    | PCSign = 73us
    | DiceRoll = 74us
    | NoviceNetworkNotice = 75us
    | Unknown76 = 76us
    | Unused77 = 77us
    | Unused78 = 78us
    | Unused79 = 79us
    | GMTell = 80us
    | GMSay = 81us
    | GMShout = 82us
    | GMYell = 83us
    | GMParty = 84us
    | GMFreeCompany = 85us
    | GMLS1 = 86us
    | GMLS2 = 87us
    | GMLS3 = 88us
    | GMLS4 = 89us
    | GMLS5 = 90us
    | GMLS6 = 91us
    | GMLS7 = 92us
    | GMLS8 = 93us
    | GMNoviceNetwork = 94us
    | Unused95 = 95us
    | Unused96 = 96us
    | Unused97 = 97us
    | Unused98 = 98us
    | Unused99 = 99us
    | Unused100 = 100us


type Chat = 
    inherit PacketParserBase
    val UserId  : string
    val Unknown : uint32
    val ServerId: uint16
    val ChatType: uint16
    val UserName: string //  32 byte fixed
    val Text    : string //2048 bytes fixed

    new (r : XIVBinaryReader) = 
        {
            UserId  = r.ReadHexString(8)
            Unknown = r.ReadUInt32()
            ServerId= r.ReadUInt16()
            ChatType= r.ReadUInt16()
            UserName= r.ReadFixedUTF8(32)
            Text    = r.ReadFixedUTF8(2048)
        }
(*
type Chat = 
    {
        UserID  : uint64
        Unknown : uint32
        ServerID: uint16
        ChatType: uint16
        Username: string //  32 byte fixed
        Text    : string //2048 bytes fixed
    }

    static member ParseFromBytes(r : XIVBinaryReader) = 
        {
            UserID  = r.ReadUInt64()
            Unknown = r.ReadUInt32()
            ServerID= r.ReadUInt16()
            ChatType= r.ReadUInt16()
            Username= r.ReadFixedUTF8(32)
            Text    = r.ReadFixedUTF8(2048)
        }*)