module LibFFXIV.Network.Utils
open System
open System.Reflection
open System.Text

type HexString = 
    //This is rarely used. No need for faster method
    static member ToBytes (s : string) = 
      s
      |> Seq.windowed 2
      |> Seq.mapi (fun i j -> (i,j))
      |> Seq.filter (fun (i,j) -> i % 2=0)
      |> Seq.map (fun (_,j) -> Byte.Parse(new System.String(j),System.Globalization.NumberStyles.AllowHexSpecifier))
      |> Array.ofSeq

    // from https://blogs.msdn.microsoft.com/blambert/2009/02/22/blambertcodesnip-fast-byte-array-to-hex-string-conversion/
    static member private HexTable =
        [|  "00"; "01"; "02"; "03"; "04"; "05"; "06"; "07"; "08"; "09"; "0A"; "0B"; "0C"; "0D"; "0E"; "0F";
            "10"; "11"; "12"; "13"; "14"; "15"; "16"; "17"; "18"; "19"; "1A"; "1B"; "1C"; "1D"; "1E"; "1F";
            "20"; "21"; "22"; "23"; "24"; "25"; "26"; "27"; "28"; "29"; "2A"; "2B"; "2C"; "2D"; "2E"; "2F";
            "30"; "31"; "32"; "33"; "34"; "35"; "36"; "37"; "38"; "39"; "3A"; "3B"; "3C"; "3D"; "3E"; "3F";
            "40"; "41"; "42"; "43"; "44"; "45"; "46"; "47"; "48"; "49"; "4A"; "4B"; "4C"; "4D"; "4E"; "4F";
            "50"; "51"; "52"; "53"; "54"; "55"; "56"; "57"; "58"; "59"; "5A"; "5B"; "5C"; "5D"; "5E"; "5F";
            "60"; "61"; "62"; "63"; "64"; "65"; "66"; "67"; "68"; "69"; "6A"; "6B"; "6C"; "6D"; "6E"; "6F";
            "70"; "71"; "72"; "73"; "74"; "75"; "76"; "77"; "78"; "79"; "7A"; "7B"; "7C"; "7D"; "7E"; "7F";
            "80"; "81"; "82"; "83"; "84"; "85"; "86"; "87"; "88"; "89"; "8A"; "8B"; "8C"; "8D"; "8E"; "8F";
            "90"; "91"; "92"; "93"; "94"; "95"; "96"; "97"; "98"; "99"; "9A"; "9B"; "9C"; "9D"; "9E"; "9F";
            "A0"; "A1"; "A2"; "A3"; "A4"; "A5"; "A6"; "A7"; "A8"; "A9"; "AA"; "AB"; "AC"; "AD"; "AE"; "AF";
            "B0"; "B1"; "B2"; "B3"; "B4"; "B5"; "B6"; "B7"; "B8"; "B9"; "BA"; "BB"; "BC"; "BD"; "BE"; "BF";
            "C0"; "C1"; "C2"; "C3"; "C4"; "C5"; "C6"; "C7"; "C8"; "C9"; "CA"; "CB"; "CC"; "CD"; "CE"; "CF";
            "D0"; "D1"; "D2"; "D3"; "D4"; "D5"; "D6"; "D7"; "D8"; "D9"; "DA"; "DB"; "DC"; "DD"; "DE"; "DF";
            "E0"; "E1"; "E2"; "E3"; "E4"; "E5"; "E6"; "E7"; "E8"; "E9"; "EA"; "EB"; "EC"; "ED"; "EE"; "EF";
            "F0"; "F1"; "F2"; "F3"; "F4"; "F5"; "F6"; "F7"; "F8"; "F9"; "FA"; "FB"; "FC"; "FD"; "FE"; "FF"  |]
    static member ToHex (bytes : byte[]) = 
        let hexTable = HexString.HexTable
        let sb = new Text.StringBuilder(bytes.Length * 2)
        for b in bytes do 
            sb.Append(hexTable.[(int)b]) |> ignore
        sb.ToString()

let internal IsByteArrayNotAllZero (bytes : byte[]) =
    bytes
    |> Array.exists (fun x -> x <> 0uy)

type XIVBinaryReader(ms : IO.MemoryStream) = 
    inherit IO.BinaryReader(ms)

    /// <summary>
    /// 是否读取到流末尾
    /// </summary>
    member x.IsEnd() = 
        x.BaseStream.Position = x.BaseStream.Length

    /// <summary>
    /// 读取定长UTF8字符串，以0x00填充至指定长度
    /// </summary>
    /// <param name="len">长度</param>
    member x.ReadFixedUTF8(len : int) = 
        let bytes = x.ReadBytes(len)
        Encoding.UTF8.GetString(bytes).TrimEnd('\x00')

    /// <summary>
    /// 读取指定长度字节数组，而不提升位置
    /// </summary>
    /// <param name="len">长度</param>
    member x.PeekBytes(len : int) = 
        let origPos = x.BaseStream.Position
        let bytes   = x.ReadBytes(len)
        x.BaseStream.Position <- origPos
        bytes

    member x.PeekRestBytes() = 
        x.PeekBytes(x.BytesLeft |> int)

    /// <summary>
    /// 读取指定长度字节为16进制字符串
    /// </summary>
    /// <param name="len">长度</param>
    member x.ReadHexString(len : int) = 
        HexString.ToHex(x.ReadBytes(len))

    /// <summary>
    /// 还剩多少数据
    /// </summary>
    member x.BytesLeft =
        let bs = x.BaseStream
        bs.Length - bs.Position

    /// <summary>
    /// 读取剩余部分为数组
    /// </summary>
    member x.ReadRestBytes() = 
        x.ReadBytes(x.BytesLeft |> int)

    /// <summary>
    /// 读取秒(uint32)单位的时间戳
    /// </summary>
    member x.ReadTimeStampSec() = 
        DateTimeOffset.FromUnixTimeSeconds(x.ReadUInt32() |> int64)

    /// <summary>
    /// 读取毫秒(uint64)单位的时间戳
    /// </summary>
    member x.ReadTimeStampMillisec() = 
        DateTimeOffset.FromUnixTimeMilliseconds(x.ReadUInt64() |> int64)

    /// <summary>
    /// 将剩余字节分块返回
    /// </summary>
    /// <param name="size">分块大小</param>
    /// <param name="filterZeroChunks">是否滤除全零分块</param>
    member x.ReadRestBytesAsChunk(size : int, ?filterZeroChunks : bool) = 
        let needFilter = defaultArg filterZeroChunks false
        let (chunks, tail) = 
            x.ReadRestBytes()
            |> Array.chunkBySize size
            |> Array.partition (fun x -> x.Length = size)

        let tail =  tail |> Array.tryHead 
        let chunks = 
            if needFilter then
                chunks |> Array.filter (fun x -> IsByteArrayNotAllZero(x))
            else
                chunks
        (chunks, tail)

    static member FromBytes (bytes : byte []) =
        let ms = new IO.MemoryStream(bytes)
        new XIVBinaryReader(ms)


type ByteArray(buf : byte[]) = 
    let buf = buf
    let hex = lazy (HexString.ToHex(buf))

    new (hex : string) = 
        new ByteArray(HexString.ToBytes(hex))

    /// 转换到HexString
    override x.ToString() = hex.Force()

    member x.GetBuffer()  = buf

    member x.GetReader()  = 
        XIVBinaryReader.FromBytes(buf)
    
    member x.HexContains(ba : byte[]) = 
        let h = HexString.ToHex(ba)
        hex.Force().Contains(h)

    member x.HexContains(i : int) = 
        x.HexContains(BitConverter.GetBytes(i))

    member x.HexContains(i : int64) = 
        x.HexContains(BitConverter.GetBytes(i))
        
    member x.HexContains(i : uint32) = 
        x.HexContains(BitConverter.GetBytes(i))

    member x.HexContains(i : uint64) = 
        x.HexContains(BitConverter.GetBytes(i))

    member x.HexContains(s : string) = 
        x.HexContains(Encoding.UTF8.GetBytes(s))

    member x.HexContains(i : float) = 
        x.HexContains(BitConverter.GetBytes(i))

    member x.GetSlice(f, t) = 
        let f = defaultArg f 0
        let t = defaultArg t (buf.Length - 1)
        buf.[f .. t]

    member x.Item(idx) = buf.[idx]

type XIVArray<'T> (cap : int) = 
    let arr = Array.zeroCreate<'T option> cap
    let mutable networkCompleted = false

    new () = new XIVArray<'T>(100)

    member x.AddSlice(curr, next, data : 'T []) = 
        data 
        |> Array.iteri (fun idx data ->
            let i = idx + curr
            arr.[idx + curr] <- Some(data))
        if next = 0 then
            networkCompleted <- true

    member x.IsCompleted = 
        let firstNone = arr |> Array.tryFindIndex (fun x -> x.IsNone)
        let  lastSome = arr |> Array.findIndexBack (fun x -> x.IsSome)

        let isFull = firstNone.IsNone

        if networkCompleted then
            //Network packet end, check for gaps
            // SSSSSNNNN OK
            // SNSNSNNNN NG
            if firstNone.IsSome then
                let hasGap = (firstNone.Value - lastSome) <> 1
                not hasGap
            else
                isFull
        else
            isFull

    member x.First = 
        let i = arr |> Array.tryFind (fun x -> x.IsSome)
        if i.IsSome then
            Some(i.Value.Value)
        else
            None

    member x.Data = 
        arr
        |> Array.filter  (fun x -> x.IsSome)
        |> Array.map (fun x -> x.Value)

    member x.Reset() = 
        networkCompleted <- false
        Array.fill arr 0 cap None


[<AbstractClass>]
type PacketParserBase() = 

    override x.ToString() = 
        let sb = new System.Text.StringBuilder()
        for p in x.GetType().GetProperties(BindingFlags.Public ||| BindingFlags.Instance) do 
            if p.PropertyType = typeof<byte []> then
                let hex = p.GetValue(x, null) :?> byte [] |> HexString.ToHex
                sb.AppendFormat("{0} = {1}{2}", p.Name, hex, System.Environment.NewLine) |> ignore
            else
                sb.AppendFormat("{0} = {1}{2}", p.Name, p.GetValue(x, null), System.Environment.NewLine) |> ignore
        sb.ToString()

open System.Collections.Generic
type Logger = 
    static member private loggers = new Dictionary<Type, NLog.Logger>()
    static member private getLogger<'T>() = 
        let t = typeof<'T>
        if not <| Logger.loggers.ContainsKey(t) then
            let logger = NLog.LogManager.GetLogger("Parser:" + t.Name)
            Logger.loggers.Add(t, NLog.LogManager.GetLogger("Parser:" + t.Name))
            logger
        else
            Logger.loggers.[t]

    static member Trace<'T>(message : string, [<ParamArray>] args : Object []) = 
        Logger.getLogger<'T>().Trace(message, args)

    static member Debug<'T>(message : string, [<ParamArray>] args : Object []) = 
        Logger.getLogger<'T>().Debug(message, args)

    static member Info<'T>(message : string, [<ParamArray>] args : Object []) = 
        Logger.getLogger<'T>().Info(message, args)

    static member Warn<'T>(message : string, [<ParamArray>] args : Object []) = 
        Logger.getLogger<'T>().Warn(message, args)

    static member Error<'T>(message : string, [<ParamArray>] args : Object []) = 
        Logger.getLogger<'T>().Error(message, args)

    static member Fatal<'T>(message : string, [<ParamArray>] args : Object []) = 
        Logger.getLogger<'T>().Fatal(message, args)