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
......
```

#### Filtering Field 
Some sheets are very complex and consume a lot of memory when parsing,

You can parse only selected fields to save memory.

Fields accessed during runtime can be dumped by `IXivCollection.DumpTracedFields()`

```
Without filtering:
00:00:01.8959287  : 136.031250 MB

Select Name from Item
col.GetSelectedSheet("Item", [|"Name"|])
00:00:01.4900443  : 46.406250 MB

Only requied fields: (no improvements because Item is the only complex sheet here)
col.GetSelectedSheet("CompanyCraftSequence", ids = [|6; 7; 8; 9; 10; 11; 12; 13|]) |> ignore
col.GetSelectedSheet("CompanyCraftPart", ids = [|3; 4; 5|]) |> ignore
col.GetSelectedSheet("CompanyCraftProcess", ids = [|1; 2; 3; 4; 5; 6; 7; 8; 9; 10; 11; 12; 13; 14; 15; 16; 17; 18; 19; 20; 21; 22;
23; 24; 25; 26; 27; 28; 29; 30; 31; 32; 33; 34; 35; 36|]) |> ignore
col.GetSelectedSheet("CompanyCraftSupplyItem", ids = [|1|]) |> ignore
col.GetSelectedSheet("Item", ids = [|10|]) |> ignore
00:00:01.5279136  : 46.554688 MB


```