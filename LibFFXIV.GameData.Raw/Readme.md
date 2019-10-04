#### Build
1. Extract game data with `allrawexd`.
2. Zip the output files.
3. Your zip file layout should be :

```
raw-exd-all.zip
│  Achievement.chs.csv
│  AchievementCategory.chs.csv
│  AchievementKind.chs.csv
│  Action.chs.csv
│  ActionCastTimeline.csv
│  ActionCastVFX.csv
│  ActionCategory.chs.csv
│  ActionComboRoute.chs.csv
│  ActionComboRouteTransient.chs.csv
│  ... ...
```
4. Put `raw-exd-all.zip` in `LibFFXIV.GameData.Raw` folder.
5. Build.

#### Usage
```
CSV layout:

CompanyCraftSequence.csv:
key,0,1,2,3,4,5,6,7,8,9,10,11,12,13
#,ResultItem,,CompanyCraftDraftCategory,CompanyCraftType,CompanyCraftDraft,CompanyCraftPart[0],CompanyCraftPart[1],CompanyCraftPart[2],CompanyCraftPart[3],CompanyCraftPart[4],CompanyCraftPart[5],CompanyCraftPart[6],CompanyCraftPart[7],
int32,Item,int32,CompanyCraftDraftCategory,CompanyCraftType,CompanyCraftDraft,CompanyCraftPart,CompanyCraftPart,CompanyCraftPart,CompanyCraftPart,CompanyCraftPart,CompanyCraftPart,CompanyCraftPart,CompanyCraftPart,uint32
0,0,0,0,0,-1,0,0,0,0,0,0,0,0,0
1,9462,1,0,1,-1,1,0,0,0,0,0,0,0,1
2,9463,1,0,1,14,2,0,0,0,0,0,0,0,2

CompanyCraftPart.csv:
key,0,1,2,3,4,5
#,,CompanyCraftType,CompanyCraftProcess[0],CompanyCraftProcess[1],CompanyCraftProcess[2],
int32,byte,CompanyCraftType,CompanyCraftProcess,CompanyCraftProcess,CompanyCraftProcess,uint16
0,0,0,0,0,0,0
1,1,1,1,2,3,0
2,2,1,50,51,52,0

CompanyCraftProcess.csv:
key,0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35
#,SupplyItem[0],SetQuantity[0],SetsRequired[0],SupplyItem[1],SetQuantity[1],SetsRequired[1],SupplyItem[2],SetQuantity[2],SetsRequired[2],SupplyItem[3],SetQuantity[3],SetsRequired[3],SupplyItem[4],SetQuantity[4],SetsRequired[4],SupplyItem[5],SetQuantity[5],SetsRequired[5],SupplyItem[6],SetQuantity[6],SetsRequired[6],SupplyItem[7],SetQuantity[7],SetsRequired[7],SupplyItem[8],SetQuantity[8],SetsRequired[8],SupplyItem[9],SetQuantity[9],SetsRequired[9],SupplyItem[10],SetQuantity[10],SetsRequired[10],SupplyItem[11],SetQuantity[11],SetsRequired[11]
int32,CompanyCraftSupplyItem,uint16,uint16,CompanyCraftSupplyItem,uint16,uint16,CompanyCraftSupplyItem,uint16,uint16,CompanyCraftSupplyItem,uint16,uint16,CompanyCraftSupplyItem,uint16,uint16,CompanyCraftSupplyItem,uint16,uint16,CompanyCraftSupplyItem,uint16,uint16,CompanyCraftSupplyItem,uint16,uint16,CompanyCraftSupplyItem,uint16,uint16,CompanyCraftSupplyItem,uint16,uint16,CompanyCraftSupplyItem,uint16,uint16,CompanyCraftSupplyItem,uint16,uint16
0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0
1,112,3,3,113,3,3,229,3,3,340,3,3,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0
2,9,3,3,216,3,3,340,3,3,630,3,3,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0

CompanyCraftSupplyItem.csv:
key,0
#,Item
int32,Item
0,0
1,4551
2,5361
3,5364
```

Code:
```F#
let col = new XivCollection(Base.XivLanguage.ChineseSimplified) :> Base.IXivCollection
// initialize item sheet with only Name column to collection cache
// ignore other column to save memory
col.GetLimitedSheet("Item", [|"Name"|]) |> ignore
let ccsSheet = col.GetSheet("CompanyCraftSequence")
let ccs = ccsSheet.[1000]
[|
    for part in ccs.AsRowArray("CompanyCraftPart", 8) do 
        for proc in part.AsRowArray("CompanyCraftProcess", 3) do 
            let items = 
                proc.AsRowArray("SupplyItem", 12, false)
                |> Array.map (fun r -> r.AsRow("Item"))
                |> Array.map (fun r -> r.As<string>("Name"))
            let amount = 
                let setAmount = proc.AsArray<uint16>("SetQuantity", 12)
                let setCount  = proc.AsArray<uint16>("SetsRequired", 12)
                setAmount
                |> Array.map2 (fun a b -> a * b |> int ) setCount
            yield! Array.zip items amount |> Array.filter (fun (a,_) -> a <> "")
|] |> Array.iter (fun (a, b) -> printfn "%s * %i" a b)
```

Result: item * amount in chinese.
```
榆木木材 * 30
黑铁钉 * 30
切石 * 30
烧结砖 * 30
泥岩 * 30
云杉原木 * 30
紫杉木材 * 30
黑铁钉 * 30
砖块 * 30
纤维衬料 * 30
紫檀原木 * 45
胡桃木材 * 30
白钢钉 * 30
灰泥 * 30
清漆 * 18
花岗岩 * 30
大理石 * 30
梣木木材 * 30
胡桃木材 * 30
橡木木材 * 30
白钢钉 * 30
鬼胶 * 18
榆木木材 * 30
紫杉木材 * 30
钴铁钉 * 18
钴铁连接板 * 30
鬼胶 * 18
胡桃木材 * 18
钴铁钉 * 30
钴铁连接板 * 30
切石 * 30
亚麻帆布 * 18
玻璃板 * 9
红木木材 * 21
白钢锭 * 18
白钢折叶 * 18
切石 * 21
玻璃板 * 21
云杉木材 * 24
白钢折叶 * 18
切石 * 18
玻璃板 * 18
黑铁锭 * 75
白钢锭 * 75
绿金锭 * 75
切石 * 75
榆木木材 * 18
白钢锭 * 18
玫瑰金锭 * 12
玻璃板 * 18
白钢铆钉 * 12
玫瑰金锭 * 12
巨蟾蜍革 * 12
棉布帆布 * 12
玻璃板 * 12
红木木材 * 18
黑铁锭 * 21
亚麻线 * 21
亚麻帆布 * 18
```