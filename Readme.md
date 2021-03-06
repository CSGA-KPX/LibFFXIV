# LibFFXIV
私人FF14数据访问库。

## LibFFXIV.Network
已放弃，不再更新。

Deprecated.

## LibFFXIV.GameData.Raw
提供对SaintCoinach导出的压缩文件的无类型（手动）访问。

```fsharp
open System
open System.IO
open System.IO.Compression
open LibFFXIV.GameData.Raw

[<Literal>]
let archivePath = @"K:\Source\Repos\TheBot\TheBot\bin\Debug\staticData\ffxiv-datamining-cn-master.zip"

[<Literal>]
let archivePrefix = @"ffxiv-datamining-cn-master/"

let file = File.Open(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read)
let a = new ZipArchive(file, ZipArchiveMode.Read)
let col = new ZippedXivCollection(XivLanguage.None, a, archivePrefix)

for row in col.GetSheet("IKDRouteTable") do 
    let key = row.Key.Main
    let route = row.AsRow("Route").As<string>("Name")
    printfn "%i -> %A" key route
```


## LibFFXIV.GameData.Provided
通过类型提供器（Type Provider）对LibFFXIV.GameData.Raw进行简单包装。

备注：
1. Type Provider是F#语言特性，此部分内容在其他.NET语言中不生效。
2. 定义文件改变不一定触发重新编译，相关旧代码不会报错，建议每次更新后进行Rebuild检查能否编译。

```fsharp
open System
open System.IO
open System.IO.Compression
open LibFFXIV.GameData.Raw
open LibFFXIV.GameData.Provided


[<Literal>]
let archivePath = @"K:\Source\Repos\TheBot\TheBot\bin\Debug\staticData\ffxiv-datamining-cn-master.zip"

[<Literal>]
let archivePrefix = @"ffxiv-datamining-cn-master/"

type TypedXivCollection =
    XivCollectionProvider<archivePath, "none", archivePrefix>

let file = File.Open(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read)
let a = new ZipArchive(file, ZipArchiveMode.Read)
let col = new TypedXivCollection(XivLanguage.None, a, archivePrefix)

#time

// props generated by TP
for row in col.IKDRouteTable.Rows do 
    let key = row.Key.Main
    let route = row.Route.AsRow().Name.AsString() // methods and props generated by TP
    printfn "%i -> %A" key route
```
