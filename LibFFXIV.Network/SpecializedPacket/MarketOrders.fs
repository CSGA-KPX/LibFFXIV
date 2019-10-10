namespace LibFFXIV.Network.SpecializedPacket
open LibFFXIV.Network.Utils

type MarketOrder() = 
    inherit PacketParserBase()
    member val OrderId     : uint64 = 0UL with set, get
    member val RetainerId  : string = "" with set, get
    member val UserId      : string = "" with set, get
    //道具制作者签名
    member val SignUserId  : string = "" with set, get
    member val Price       : uint32 = 0u with set, get
    member val Unknown2    : uint32 = 0u with set, get
    member val Count       : uint32 = 0u with set, get
    member val ItemId      : uint32 = 0u with set, get
    ///最后访问雇员的日期
    member val TimeStamp   : uint32 = 0u with set, get
    //24 byte unknown
    member val Unknown3    : byte []= [||] with set, get
    //32 byte zero-ter UTF8雇员名称
    member val Name        : string = "" with set, get
    // 1 byte
    member val IsHQ        : bool   = false with set, get
    // 1 byte
    member val MeldCount   : byte   = 0uy with set, get
    // 1 byte
    member val Market      : byte   = 0uy with set, get
    // 1 byte
    member val Unknown4    : byte   = 0uy with set, get
    
    member val Unknown5    : byte []= [||] with set, get
    member val Unknown6    : byte []= [||] with set, get
    
    new (data : byte []) as x  = 
        new MarketOrder() 
        then
            Logger.Trace<MarketOrder>(data |> HexString.ToHex)
            let r  = XIVBinaryReader.FromBytes(data)
            x.OrderId     <- r.ReadUInt64()
            x.RetainerId  <- r.ReadHexString(8)
            x.UserId      <- r.ReadHexString(8)
            x.SignUserId  <- r.ReadHexString(8)
            x.Price       <- r.ReadUInt32()
            x.Unknown2    <- r.ReadUInt32()
            x.Count       <- r.ReadUInt32()
            x.ItemId      <- r.ReadUInt32()
            x.TimeStamp   <- r.ReadUInt32()
            x.Unknown3    <- r.ReadBytes(24)
            x.Name        <- r.ReadFixedUTF8(32)
            x.Unknown5    <- r.ReadBytes(32)
            x.IsHQ        <- r.ReadByte() = 1uy
            x.MeldCount   <- r.ReadByte()
            x.Unknown4    <- r.ReadByte()
            x.Market      <- r.ReadByte()
            x.Unknown6    <- r.ReadRestBytes()

    static member RecordSize = 304/2

type MarketOrderPacket = 
    inherit PacketParserBase
    val Records : MarketOrder []
    val NextIdx : byte
    val CurrIdx : byte
    val Unknown : byte [] // 6bytes

    new (r : XIVBinaryReader) = 
        let (chks, rst) = r.ReadRestBytesAsChunk(MarketOrder.RecordSize, true)

        if rst.IsNone then
            Logger.Trace<MarketOrderPacket>("Parse error! tail is none.")

        let records = 
            [|
                for chk in chks do
                    yield new MarketOrder(chk)
            |]
        
        {
            Records = records
            NextIdx = rst.Value.[0]
            CurrIdx = rst.Value.[1]
            Unknown = rst.Value.[2..]
        }

(*
[<CLIMutableAttribute>]
type MarketRecord = 
    {
        OrderID     : uint32
        Unknown1    : uint32
        RetainerID  : uint64
        UserID      : uint64
        SignUserID  : uint64 //道具制作者签名
        Price       : uint32
        Unknown2    : uint32
        Count       : uint32
        Itemid      : uint32
        ///最后访问雇员的日期
        TimeStamp   : uint32
        Unknown3    : byte [] //24 byte unknown
        Name        : string  //32 byte zero-ter UTF8雇员名称
        IsHQ        : bool    // 1 byte
        MeldCount   : byte    // 1 byte
        Market      : byte    // 1 byte
        Unknown4    : byte    // 1 byte
    }

    static member private zerosA = ""

    static member ParseFromBytes(data : byte []) = 
        Logger.Log<MarketRecord>(data |> HexString.ToHex)
        use r  = XIVBinaryReader.FromBytes(data)
        {
            OrderID      = r.ReadUInt32()
            Unknown1     = r.ReadUInt32()
            RetainerID   = r.ReadUInt64()
            UserID       = r.ReadUInt64()
            SignUserID   = r.ReadUInt64()
            Price     = r.ReadUInt32()
            Unknown2  = r.ReadUInt32()
            Count     = r.ReadUInt32()
            Itemid    = r.ReadUInt32()
            TimeStamp = r.ReadUInt32()
            Unknown3  = r.ReadBytes(24)
            Name      = r.ReadFixedUTF8(32)
            IsHQ      = 
                let unknown5 = r.ReadBytes(32)
                if (unknown5 |> Array.sum) <> 0uy then 
                    System.Console.WriteLine("MarketRecord Unknown5 = {0}", HexString.ToHex(unknown5))
                r.ReadByte() = 1uy
            MeldCount = r.ReadByte()
            Unknown4  = r.ReadByte()
            Market    = 
                let market = r.ReadByte()
                let unknown6 = r.ReadRestBytes()
                if (unknown6 |> Array.sum) <> 0uy then 
                    System.Console.WriteLine("MarketRecord Unknown6 = {0}", HexString.ToHex(unknown6))
                market
        }

type MarketPacket =
    {
        Records : MarketRecord []
        NextIdx : byte
        CurrIdx : byte
        Unknown : byte [] // 6bytes
    }

    static member private logger = NLog.LogManager.GetCurrentClassLogger()

    static member private recordSize = 304/2 //112 detla = 40

    static member ParseFromBytes(r : XIVBinaryReader) = 
        let (chks, rst) = r.ReadRestBytesAsChunk(MarketPacket.recordSize, true)

        if rst.IsNone then
            let errormsg = sprintf "Must have tail bytes!"
            MarketPacket.logger.Error(errormsg)
            failwithf "%s" errormsg

        let records = 
            [|
                for chk in chks do
                    yield MarketRecord.ParseFromBytes(chk)
            |]
        
        {
            Records = records
            NextIdx = rst.Value.[0]
            CurrIdx = rst.Value.[1]
            Unknown = rst.Value.[2..]
        }
        *)