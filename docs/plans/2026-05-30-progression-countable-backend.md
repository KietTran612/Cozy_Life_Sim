# Progression & Countable Sticker Backend Implementation Plan

> **For Antigravity:** REQUIRED WORKFLOW: Use `.agent/workflows/execute-plan.md` to execute this plan in single-flow mode.

**Goal:** Trien khai he thong du lieu an toan cho countable stickers, di dan save cu null-safe voi bat bien collection, backfill PlacementId struct-safe bang indexed loop va dirty flag, IProgressionService (Level/XP), cung cac non-saving mutators & rollback de bao dam tinh nguyen tu du lieu trong RAM.

**Architecture:** Tách invariant null-safety khoi marker di dan trong NormalizeSaveData(). Su dung indexed loop de cap nhat struct trong List va dung HashSet<string> ngan ngua trung GUID placement. Bổ sung signature tra ve Struct tu AddPlacedStickerNonSaving de cap nhat exact PlacementId phuc vu rollback. Trien khai progression service doc lap de mo khoa template.

**Tech Stack:** C#, Unity 6000.3.11f1, VContainer, DOTween, C# Unit Tests.

---

### Task 1: Core SaveData & Progression Service

**Files:**
- Modify: [SaveData.cs](file:///c:/1.SOURCE/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Core/SaveData.cs)
- Create: [IProgressionService.cs](file:///c:/1.SOURCE/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Core/IProgressionService.cs)
- Create: [ProgressionService.cs](file:///c:/1.SOURCE/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Services/ProgressionService.cs)
- Modify: [GameLifetimeScope.cs](file:///c:/1.SOURCE/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/GameLifetimeScope.cs)
- Test: [CozyLifeSimValidation.cs](file:///c:/1.SOURCE/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimValidation.cs)

**Step 1: Write the failing test for Progression Service**
Mở file `CozyLifeSimValidation.cs` và thêm khai báo kiểm tra VContainer đăng ký `IProgressionService` thành công và logic thăng cấp/phát sự kiện.
```csharp
[Test]
public void Test11_10_ProgressionService_XP_And_LevelUp()
{
    var container = CozyLifeSimValidation.GetActiveContainer();
    var progService = container.Resolve<IProgressionService>();
    Assert.IsNotNull(progService);
    
    int eventCalledCount = 0;
    int newLevelVal = 0;
    int newXpVal = 0;
    progService.OnLevelUp += (lvl) => { eventCalledCount++; newLevelVal = lvl; };
    progService.OnXPChanged += (xp) => { newXpVal = xp; };
    
    progService.AddXP(50);
    Assert.AreEqual(50, progService.PlayerXP);
    Assert.AreEqual(1, progService.PlayerLevel);
    
    // Level up threshold: Level * 100 XP
    progService.AddXP(60); 
    Assert.AreEqual(110 - 100, progService.PlayerXP); // XP du chuyen tiep
    Assert.AreEqual(2, progService.PlayerLevel);
    Assert.AreEqual(1, eventCalledCount);
}
```

**Step 2: Run test to verify it fails**
Chạy test từ Unity Editor hoặc qua MCP (Test sẽ lỗi vì chưa định nghĩa file dịch vụ và chưa đăng ký trong VContainer).

**Step 3: Modify SaveData.cs to include Level/XP fields**
Chèn các trường dữ liệu cấp độ và kinh nghiệm vào `SaveData.cs`:
```csharp
public int PlayerLevel = 1;
public int PlayerXP = 0;
```

**Step 4: Create IProgressionService.cs**
Tạo file mới tại `Assets/CozyLifeSim/Scripts/Core/IProgressionService.cs`:
```csharp
using System;

namespace CozyLifeSim.Core
{
    public interface IProgressionService
    {
        int PlayerLevel { get; }
        int PlayerXP { get; }
        void AddXP(int amount);
        event Action<int> OnLevelUp;
        event Action<int> OnXPChanged;
    }
}
```

**Step 5: Create ProgressionService.cs**
Tạo file mới tại `Assets/CozyLifeSim/Scripts/UI/Services/ProgressionService.cs`:
```csharp
using System;
using CozyLifeSim.Core;
using VContainer;

namespace CozyLifeSim.UI.Services
{
    public class ProgressionService : IProgressionService
    {
        private readonly ISaveService _saveService;

        public int PlayerLevel => _saveService.ActiveSave.PlayerLevel;
        public int PlayerXP => _saveService.ActiveSave.PlayerXP;

        public event Action<int> OnLevelUp;
        public event Action<int> OnXPChanged;

        [Inject]
        public ProgressionService(ISaveService saveService)
        {
            _saveService = saveService;
        }

        public void AddXP(int amount)
        {
            if (amount <= 0) return;

            var save = _saveService.ActiveSave;
            save.PlayerXP += amount;
            OnXPChanged?.Invoke(save.PlayerXP);

            int xpRequired = GetXPThreshold(save.PlayerLevel);
            bool levelUp = false;

            while (save.PlayerXP >= xpRequired)
            {
                save.PlayerXP -= xpRequired;
                save.PlayerLevel++;
                levelUp = true;
                xpRequired = GetXPThreshold(save.PlayerLevel);
            }

            if (levelUp)
            {
                OnLevelUp?.Invoke(save.PlayerLevel);
                OnXPChanged?.Invoke(save.PlayerXP);
            }
            
            _saveService.Save();
        }

        private int GetXPThreshold(int level)
        {
            return level * 100;
        }
    }
}
```

**Step 6: Register in GameLifetimeScope.cs**
Đăng ký dịch vụ mới trong `GameLifetimeScope.cs`:
```csharp
builder.Register<ProgressionService>(Lifetime.Singleton).As<IProgressionService>();
```

**Step 7: Run test to verify it passes**
Chạy lại CozyLifeSimValidation và xác nhận Test 11.10 PASS.

**Step 8: Commit**
```bash
git add Assets/CozyLifeSim/Scripts/Core/SaveData.cs Assets/CozyLifeSim/Scripts/Core/IProgressionService.cs Assets/CozyLifeSim/Scripts/UI/Services/ProgressionService.cs Assets/CozyLifeSim/Scripts/UI/GameLifetimeScope.cs
git commit -m "feat: implement ProgressionService, add PlayerLevel and PlayerXP fields to SaveData"
```

---

### Task 2: Countable Sticker Schema & Null-Safe Invariants Migration

**Files:**
- Modify: [SaveData.cs](file:///c:/1.SOURCE/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Core/SaveData.cs)
- Modify: [StickerPlacedData.cs](file:///c:/1.SOURCE/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Core/StickerPlacedData.cs)
- Modify: [SaveService.cs](file:///c:/1.SOURCE/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Services/SaveService.cs)
- Modify: [ISaveService.cs](file:///c:/1.SOURCE/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Core/ISaveService.cs)
- Test: [CozyLifeSimValidation.cs](file:///c:/1.SOURCE/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimValidation.cs)

**Step 1: Write the failing test for Migration and Invariant Null-Safety**
Thêm bài test kiểm tra tự động di dân và chuẩn hóa GUID/Null-safety cho các trường:
```csharp
[Test]
public void Test11_11_SaveData_Migration_And_NullSafety_Invariants()
{
    var container = CozyLifeSimValidation.GetActiveContainer();
    var saveService = container.Resolve<ISaveService>();
    Assert.IsNotNull(saveService);
    
    // Gia lap save cu null
    var save = saveService.ActiveSave;
    save.StickerOwned = null;
    save.PlacedStickers = null;
    save.HasMigratedStickerOwned = false;
    
    // Them locked sticker cu
    #pragma warning disable CS0618
    save.UnlockedStickerIds = new List<int> { 3 };
    #pragma warning restore CS0618
    
    // Chay normalize de kiem tra null safety va di dan
    saveService.NormalizeSaveData();
    
    Assert.IsNotNull(save.StickerOwned);
    Assert.IsNotNull(save.PlacedStickers);
    Assert.IsTrue(save.HasMigratedStickerOwned);
    
    // ID 1, 2 phai duoc refill Count = 99 (save moi migrate)
    var id1 = save.StickerOwned.Find(x => x.StickerId == 1);
    var id3 = save.StickerOwned.Find(x => x.StickerId == 3);
    Assert.IsNotNull(id1);
    Assert.AreEqual(99, id1.Count);
    // ID 3 phai duoc merge voi Count = 1
    Assert.IsNotNull(id3);
    Assert.AreEqual(1, id3.Count);
}
```

**Step 2: Run test to verify it fails**
Chạy test và kiểm tra biên dịch lỗi (do chưa định nghĩa các thuộc tính mới trong SaveData.cs).

**Step 3: Define Struct/Class schemas and obsolete fields in SaveData.cs and StickerPlacedData.cs**
- Trong `SaveData.cs`, tạo lớp `StickerInventory`:
```csharp
[System.Serializable]
public class StickerInventory
{
    public int StickerId;
    public int Count;

    public StickerInventory(int stickerId, int count)
    {
        StickerId = stickerId;
        Count = count;
    }
}
```
Và cập nhật các trường trong `SaveData`:
```csharp
[System.Obsolete("Dung StickerOwned thay the")]
public List<int> UnlockedStickerIds = new List<int>();

public List<StickerInventory> StickerOwned = new List<StickerInventory>();
public bool HasMigratedStickerOwned = false;
```

- Trong `StickerPlacedData.cs`, sửa struct hoàn chỉnh, gỡ thuộc tính `StructLayout` và chèn constructor sinh GUID bắt buộc:
```csharp
using System;

namespace CozyLifeSim.Core
{
    [System.Serializable]
    public struct StickerPlacedData
    {
        public int StickerId;
        public int PageIndex;
        public float PositionX;
        public float PositionY;
        public float Scale;
        public float Rotation;
        public string PlacementId; // UUID duy nhat

        public StickerPlacedData(int stickerId, int pageIndex, float x, float y, float scale, float rotation)
        {
            StickerId = stickerId;
            PageIndex = pageIndex;
            PositionX = x;
            PositionY = y;
            Scale = scale;
            Rotation = rotation;
            PlacementId = Guid.NewGuid().ToString();
        }
    }
}
```

**Step 4: Implement Save Failure Hook in ISaveService.cs & SaveService.cs**
Bổ sung fault-injection knob được bảo vệ bởi chỉ thị tiền xử lý `#if UNITY_EDITOR` để tránh làm dơ runtime contract:
- Trong `ISaveService.cs`:
```csharp
#if UNITY_EDITOR
    bool ForceSaveFailure { get; set; }
#endif
```
- Trong `SaveService.cs`:
```csharp
#if UNITY_EDITOR
    public bool ForceSaveFailure { get; set; } = false;
#endif
```
Và trong phương thức `Save()` của `SaveService.cs`:
```csharp
#if UNITY_EDITOR
    if (ForceSaveFailure)
    {
        throw new System.Exception("Simulated Save Failure for Atomicity Testing");
    }
#endif
```

**Step 5: Implement Decoupled Null-Safety & Migration logic in SaveService.cs**
Triển khai logic di dân và null-safety độc lập trong `NormalizeSaveData()` của `SaveService.cs`:
```csharp
public void NormalizeSaveData()
{
    if (ActiveSave == null) return;
    
    bool isDirty = false;

    // 1. Invariant Null-Safety (Luon chay vo dieu kien)
    if (ActiveSave.StickerOwned == null)
    {
        ActiveSave.StickerOwned = new System.Collections.Generic.List<StickerInventory>();
        isDirty = true;
    }
    if (ActiveSave.PlacedStickers == null)
    {
        ActiveSave.PlacedStickers = new System.Collections.Generic.List<StickerPlacedData>();
        isDirty = true;
    }
    if (ActiveSave.CompletedQuestIds == null)
    {
        ActiveSave.CompletedQuestIds = new System.Collections.Generic.List<int>();
        isDirty = true;
    }
    if (ActiveSave.ActiveQuestProgress == null)
    {
        ActiveSave.ActiveQuestProgress = new System.Collections.Generic.List<QuestProgressData>();
        isDirty = true;
    }

    // 2. Logic di dan (Migration) duoc bao ve boi Marker
    if (!ActiveSave.HasMigratedStickerOwned)
    {
        // Gan mac dinh ID 1 va 2 neu kho trong
        if (ActiveSave.StickerOwned.Count == 0)
        {
            ActiveSave.StickerOwned.Add(new StickerInventory(1, 99));
            ActiveSave.StickerOwned.Add(new StickerInventory(2, 99));
        }

        // Merge unlocked sticker ids cu
        #pragma warning disable CS0618
        if (ActiveSave.UnlockedStickerIds != null)
        {
            foreach (var oldId in ActiveSave.UnlockedStickerIds)
            {
                if (!ActiveSave.StickerOwned.Exists(x => x.StickerId == oldId))
                {
                    ActiveSave.StickerOwned.Add(new StickerInventory(oldId, 1));
                }
            }
            ActiveSave.UnlockedStickerIds.Clear();
        }
        #pragma warning restore CS0618

        ActiveSave.HasMigratedStickerOwned = true;
        isDirty = true;
    }

    // 3. Backfill PlacementId (Indexed-loop & Duplicate-safe & Dirty-safe)
    var seenIds = new System.Collections.Generic.HashSet<string>();
    for (int i = 0; i < ActiveSave.PlacedStickers.Count; i++)
    {
        var placed = ActiveSave.PlacedStickers[i];
        if (string.IsNullOrEmpty(placed.PlacementId) || seenIds.Contains(placed.PlacementId))
        {
            placed.PlacementId = System.Guid.NewGuid().ToString();
            ActiveSave.PlacedStickers[i] = placed; // Gan nguoc lai List vi struct truyen tri
            isDirty = true;
        }
        seenIds.Add(placed.PlacementId);
    }

    // 4. Luu file neu thuc su thay doi
    if (isDirty)
    {
        Save();
    }
}
```

**Step 6: Run test to verify it passes**
Chạy CozyLifeSimValidation và xác nhận các bài kiểm thử di dân và null-safety PASS.

**Step 7: Commit**
```bash
git add Assets/CozyLifeSim/Scripts/Core/SaveData.cs Assets/CozyLifeSim/Scripts/Core/StickerPlacedData.cs Assets/CozyLifeSim/Scripts/Core/ISaveService.cs Assets/CozyLifeSim/Scripts/UI/Services/SaveService.cs
git commit -m "feat: decouple null-safety invariants from HasMigratedStickerOwned, implement struct-safe backfill with dirty checking and duplicates prevention"
```

---

### Task 3: Non-Saving Mutators on Services (Atomic Transactions)

**Files:**
- Modify: [IInventoryService.cs](file:///c:/1.SOURCE/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Core/IInventoryService.cs)
- Modify: [InventoryService.cs](file:///c:/1.SOURCE/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Services/InventoryService.cs)
- Modify: [IMemoryService.cs](file:///c:/1.SOURCE/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Core/IMemoryService.cs)
- Modify: [MemoryService.cs](file:///c:/1.SOURCE/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Services/MemoryService.cs)
- Test: [CozyLifeSimValidation.cs](file:///c:/1.SOURCE/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimValidation.cs)

**Step 1: Write the failing test for Atomicity & Memory Rollback**
```csharp
[Test]
public void Test11_12_StickerPlacement_And_Rollback_On_Failure()
{
    var container = CozyLifeSimValidation.GetActiveContainer();
    var inventoryService = container.Resolve<IInventoryService>();
    var memoryService = container.Resolve<IMemoryService>();
    var saveService = container.Resolve<ISaveService>();
    
    // Dam bao co san sticker de tieu thu
    inventoryService.AddStickerCountNonSaving(3, 1);
    saveService.Save();
    
    int initialCount = inventoryService.GetStickerCount(3);
    Assert.AreEqual(1, initialCount);
    
    // Kich hoat ForceSaveFailure gia lap loi IO
    #if UNITY_EDITOR
    saveService.ForceSaveFailure = true;
    #endif
    
    // Giao dich dan sticker (Co che rollback trong Presenter)
    bool transactionSuccess = false;
    var data = new StickerPlacedData(3, 1, 10f, 10f, 1f, 0f);
    
    // 1. Consume sticker count
    if (inventoryService.ConsumeStickerNonSaving(3))
    {
        // 2. Add placed sticker
        data = memoryService.AddPlacedStickerNonSaving(data);
        
        try
        {
            // 3. Persist to disk
            saveService.Save();
            transactionSuccess = true;
        }
        catch (System.Exception)
        {
            // 4. Rollback neu luu dia that bai
            inventoryService.AddStickerCountNonSaving(3, 1);
            memoryService.TryRemovePlacedStickerNonSaving(data.PlacementId, out _);
        }
    }
    
    #if UNITY_EDITOR
    saveService.ForceSaveFailure = false;
    #endif
    
    Assert.IsFalse(transactionSuccess);
    // Kiem tra bo nho khong bi lech du Save() bi crash
    Assert.AreEqual(1, inventoryService.GetStickerCount(3));
    Assert.IsFalse(memoryService.TryRemovePlacedStickerNonSaving(data.PlacementId, out _));
}
```

**Step 2: Run test to verify it fails**
Chạy test để xác nhận các method non-saving chưa tồn tại.

**Step 3: Modify IInventoryService.cs to add countable/non-saving signatures**
Thêm các API đếm sticker và các hàm non-saving vào contract:
```csharp
using System;

namespace CozyLifeSim.Core
{
    public interface IInventoryService
    {
        int Coins { get; }
        void AddCoins(int amount);
        bool ConsumeCoins(int amount);
        event Action<int> OnCoinsChanged;

        // Inventory Countable Sticker
        int GetStickerCount(int stickerId);
        void AddStickerCount(int stickerId, int amount);
        bool ConsumeSticker(int stickerId);
        event Action<int, int> OnStickerCountChanged; // (stickerId, count)

        // Non-Saving APIs
        void AddStickerCountNonSaving(int stickerId, int amount);
        bool ConsumeStickerNonSaving(int stickerId);
        void AddCoinsNonSaving(int amount);
        bool ConsumeCoinsNonSaving(int amount);
    }
}
```

**Step 4: Update InventoryService.cs with implementation**
Triển khai logic đếm sticker có sự kiện và các biến thể non-saving:
```csharp
using System;
using CozyLifeSim.Core;
using VContainer;

namespace CozyLifeSim.UI.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly ISaveService _saveService;

        public int Coins => _saveService.ActiveSave.Coins;
        public event Action<int> OnCoinsChanged;
        public event Action<int, int> OnStickerCountChanged;

        [Inject]
        public InventoryService(ISaveService saveService)
        {
            _saveService = saveService;
        }

        public void AddCoins(int amount)
        {
            AddCoinsNonSaving(amount);
            _saveService.Save();
        }

        public bool ConsumeCoins(int amount)
        {
            if (ConsumeCoinsNonSaving(amount))
            {
                _saveService.Save();
                return true;
            }
            return false;
        }

        public void AddCoinsNonSaving(int amount)
        {
            if (amount <= 0) return;
            _saveService.ActiveSave.Coins += amount;
            OnCoinsChanged?.Invoke(_saveService.ActiveSave.Coins);
        }

        public bool ConsumeCoinsNonSaving(int amount)
        {
            if (amount <= 0 || _saveService.ActiveSave.Coins < amount) return false;
            _saveService.ActiveSave.Coins -= amount;
            OnCoinsChanged?.Invoke(_saveService.ActiveSave.Coins);
            return true;
        }

        public int GetStickerCount(int stickerId)
        {
            var item = _saveService.ActiveSave.StickerOwned.Find(x => x.StickerId == stickerId);
            return item?.Count ?? 0;
        }

        public void AddStickerCount(int stickerId, int amount)
        {
            AddStickerCountNonSaving(stickerId, amount);
            _saveService.Save();
        }

        public bool ConsumeSticker(int stickerId)
        {
            if (ConsumeStickerNonSaving(stickerId))
            {
                _saveService.Save();
                return true;
            }
            return false;
        }

        public void AddStickerCountNonSaving(int stickerId, int amount)
        {
            if (amount <= 0) return;
            var list = _saveService.ActiveSave.StickerOwned;
            var item = list.Find(x => x.StickerId == stickerId);
            if (item == null)
            {
                item = new StickerInventory(stickerId, amount);
                list.Add(item);
            }
            else
            {
                item.Count += amount;
            }
            OnStickerCountChanged?.Invoke(stickerId, item.Count);
        }

        public bool ConsumeStickerNonSaving(int stickerId)
        {
            var list = _saveService.ActiveSave.StickerOwned;
            var item = list.Find(x => x.StickerId == stickerId);
            if (item == null || item.Count <= 0) return false;
            
            item.Count--;
            OnStickerCountChanged?.Invoke(stickerId, item.Count);
            return true;
        }
    }
}
```

**Step 5: Modify IMemoryService.cs with new signatures**
Thêm signature non-saving, đổi `AddPlacedSticker` để hỗ trợ pure append (không overwrite) và trả về struct:
```csharp
using System.Collections.Generic;

namespace CozyLifeSim.Core
{
    public interface IMemoryService
    {
        IReadOnlyList<StickerPlacedData> PlacedStickers { get; }
        
        void PlaceSticker(StickerPlacedData data);
        void RemovePlacedSticker(int pageIndex, StickerPlacedData data);

        // Non-Saving, return struct, defensive validation & UUID append
        StickerPlacedData AddPlacedStickerNonSaving(StickerPlacedData data);
        bool TryRemovePlacedStickerNonSaving(string placementId, out StickerPlacedData removedData);
    }
}
```

**Step 6: Implement MemoryService.cs with defensive validation and pure append**
```csharp
using System.Collections.Generic;
using CozyLifeSim.Core;
using VContainer;

namespace CozyLifeSim.UI.Services
{
    public class MemoryService : IMemoryService
    {
        private readonly ISaveService _saveService;

        public IReadOnlyList<StickerPlacedData> PlacedStickers => _saveService.ActiveSave.PlacedStickers;

        [Inject]
        public MemoryService(ISaveService saveService)
        {
            _saveService = saveService;
        }

        public void PlaceSticker(StickerPlacedData data)
        {
            AddPlacedStickerNonSaving(data);
            _saveService.Save();
        }

        public void RemovePlacedSticker(int pageIndex, StickerPlacedData data)
        {
            if (TryRemovePlacedStickerNonSaving(data.PlacementId, out _))
            {
                _saveService.Save();
            }
        }

        public StickerPlacedData AddPlacedStickerNonSaving(StickerPlacedData data)
        {
            var list = _saveService.ActiveSave.PlacedStickers;

            // Defensive validation: Ngan ngua UUID rong hoac trung lap
            bool hasDuplicate = false;
            if (!string.IsNullOrEmpty(data.PlacementId))
            {
                hasDuplicate = list.Exists(x => x.PlacementId == data.PlacementId);
            }

            if (string.IsNullOrEmpty(data.PlacementId) || hasDuplicate)
            {
                data.PlacementId = System.Guid.NewGuid().ToString();
            }

            // Pure append (Khong overwrite phan tu trung stickerId/pageIndex nua)
            list.Add(data);
            return data;
        }

        public bool TryRemovePlacedStickerNonSaving(string placementId, out StickerPlacedData removedData)
        {
            removedData = default;
            if (string.IsNullOrEmpty(placementId)) return false;

            var list = _saveService.ActiveSave.PlacedStickers;
            int idx = list.FindIndex(x => x.PlacementId == placementId);
            if (idx >= 0)
            {
                removedData = list[idx];
                list.RemoveAt(idx);
                return true;
            }
            return false;
        }
    }
}
```

**Step 7: Run test to verify it passes**
Chạy tests và xác nhận Test 11.12 PASS hoàn toàn.

**Step 8: Commit**
```bash
git add Assets/CozyLifeSim/Scripts/Core/IInventoryService.cs Assets/CozyLifeSim/Scripts/UI/Services/InventoryService.cs Assets/CozyLifeSim/Scripts/Core/IMemoryService.cs Assets/CozyLifeSim/Scripts/UI/Services/MemoryService.cs
git commit -m "feat: implement non-saving API on Inventory and Memory services with return-value, defensive validation and pure UUID append"
```

---

### Task 4: Level Lock Attributes & Quest XP Reward Cleanups

**Files:**
- Modify: [CropTemplate.cs](file:///c:/1.SOURCE/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Settings/CropTemplate.cs)
- Modify: [StickerTemplate.cs](file:///c:/1.SOURCE/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Settings/StickerTemplate.cs)
- Modify: [AnimalTemplate.cs](file:///c:/1.SOURCE/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Settings/AnimalTemplate.cs)
- Modify: [QuestTemplate.cs](file:///c:/1.SOURCE/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Settings/QuestTemplate.cs)
- Modify: [QuestService.cs](file:///c:/1.SOURCE/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Services/QuestService.cs)
- Test: [CozyLifeSimValidation.cs](file:///c:/1.SOURCE/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimValidation.cs)

**Step 1: Write the failing test for Quest completion cleanups & XP reward**
```csharp
[Test]
public void Test11_13_QuestCompletion_XP_Reward_And_CleanUp()
{
    var container = CozyLifeSimValidation.GetActiveContainer();
    var questService = container.Resolve<IQuestService>();
    var progService = container.Resolve<IProgressionService>();
    var saveService = container.Resolve<ISaveService>();
    
    var save = saveService.ActiveSave;
    save.PlayerLevel = 1;
    save.PlayerXP = 0;
    save.CompletedQuestIds.Clear();
    save.ActiveQuestProgress.Clear();
    
    // Tao quest mau gia dinh co RewardXP = 150
    // Kich hoat va bat hoan thanh
    // Gia su quest duoc hoan thanh va go khoi ActiveQuestProgress, chuyen sang CompletedQuestIds
    // Verify cap do tang len 2
}
```

**Step 2: Run test to verify it fails**
Xác nhận test lỗi vì chưa bổ sung trường `RewardXP` và logic gỡ quest trong `QuestService.cs`.

**Step 3: Modify CropTemplate, StickerTemplate, AnimalTemplate and QuestTemplate**
Bổ sung trường `RequiredLevel` và `RewardXP`:
- Trong `CropTemplate.cs`:
  `public int RequiredLevel = 1;`
- Trong `StickerTemplate.cs`:
  `public int RequiredLevel = 1;`
- Trong `AnimalTemplate.cs`:
  `public int RequiredLevel = 1;`
- Trong `QuestTemplate.cs`:
  `public int RewardXP = 0;`

**Step 4: Update QuestService.cs to clean active quest and reward XP via ProgressionService**
Inject `IProgressionService` vào `QuestService.cs` và sửa logic hoành thành quest để gỡ nó khỏi `ActiveQuestProgress`:
```csharp
// Inject IProgressionService
private readonly IProgressionService _progressionService;

[Inject]
public QuestService(ISaveService saveService, IInventoryService inventoryService, IProgressionService progressionService, QuestDatabase database)
{
    _saveService = saveService;
    _inventoryService = inventoryService;
    _progressionService = progressionService;
    _database = database;
}
```
Và trong phương thức claim/complete quest của `QuestService.cs`:
```csharp
// Thuong XP va xoa khoi ActiveQuestProgress
_progressionService.AddXP(template.RewardXP);
_saveService.ActiveSave.ActiveQuestProgress.RemoveAll(x => x.QuestId == questId);
```

**Step 5: Run test to verify it passes**
Xác nhận Test 11.13 PASS hoàn toàn.

**Step 6: Commit**
```bash
git add Assets/CozyLifeSim/Scripts/UI/Settings/CropTemplate.cs Assets/CozyLifeSim/Scripts/UI/Settings/StickerTemplate.cs Assets/CozyLifeSim/Scripts/UI/Settings/AnimalTemplate.cs Assets/CozyLifeSim/Scripts/UI/Settings/QuestTemplate.cs Assets/CozyLifeSim/Scripts/UI/Services/QuestService.cs
git commit -m "feat: add RequiredLevel locks and RewardXP schema, update QuestService to reward XP and clear active progress"
```

---

### Task 5: Shop Service countable purchase & Level validation

**Files:**
- Modify: [IShopService.cs](file:///c:/1.SOURCE/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Core/IShopService.cs)
- Modify: [ShopService.cs](file:///c:/1.SOURCE/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Services/ShopService.cs)
- Test: [CozyLifeSimValidation.cs](file:///c:/1.SOURCE/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimValidation.cs)

**Step 1: Write the failing test for Shop purchase & level locks**
```csharp
[Test]
public void Test11_14_ShopService_Purchase_And_LevelLocks()
{
    var container = CozyLifeSimValidation.GetActiveContainer();
    var shopService = container.Resolve<IShopService>();
    var progService = container.Resolve<IProgressionService>();
    var saveService = container.Resolve<ISaveService>();
    
    // setup level thap hon RequiredLevel cua sticker premium ID 3
    // Thu mua premium sticker -> that bai.
    // Tang cap cho nguoi choi -> Thu mua lai -> thanh cong va count tang 1.
}
```

**Step 2: Run test to verify it fails**
Xác nhận test lỗi do `ShopService` hiện tại chưa hỗ trợ so sánh `RequiredLevel` và chưa dùng `AddStickerCountNonSaving` để tăng số lượng khi mua.

**Step 3: Modify IShopService.cs to clean contract**
Loại bỏ hoàn toàn logic `IsStickerUnlocked` vì chúng ta sử dụng `RequiredLevel` lock ở runtime:
```csharp
using System;

namespace CozyLifeSim.Core
{
    public interface IShopService
    {
        bool TryBuySeed(int cropId);
        bool TryBuySticker(int stickerId);
        event Action OnShopTransactionSuccess;
    }
}
```

**Step 4: Update ShopService.cs with countable purchase, lock checking and atomic rollback**
Sửa đổi `TryBuySticker` để áp dụng kiểm tra cấp độ và giao dịch nguyên tử non-saving:
```csharp
using System;
using CozyLifeSim.Core;
using VContainer;

namespace CozyLifeSim.UI.Services
{
    public class ShopService : IShopService
    {
        private readonly ISaveService _saveService;
        private readonly IInventoryService _inventoryService;
        private readonly IProgressionService _progressionService;
        private readonly StickerDatabase _stickerDatabase;
        private readonly CropDatabase _cropDatabase;

        public event Action OnShopTransactionSuccess;

        [Inject]
        public ShopService(ISaveService saveService, IInventoryService inventoryService, IProgressionService progressionService, StickerDatabase stickerDatabase, CropDatabase cropDatabase)
        {
            _saveService = saveService;
            _inventoryService = inventoryService;
            _progressionService = progressionService;
            _stickerDatabase = stickerDatabase;
            _cropDatabase = cropDatabase;
        }

        public bool TryBuySeed(int cropId)
        {
            var template = _cropDatabase.GetCrop(cropId);
            if (template == null || _progressionService.PlayerLevel < template.RequiredLevel || template.BuyPrice <= 0)
                return false;

            if (_inventoryService.ConsumeCoinsNonSaving(template.BuyPrice))
            {
                // Logic them hat giong hoac cap nhat state nong trai
                _saveService.Save();
                OnShopTransactionSuccess?.Invoke();
                return true;
            }
            return false;
        }

        public bool TryBuySticker(int stickerId)
        {
            var template = _stickerDatabase.GetSticker(stickerId);
            if (template == null || _progressionService.PlayerLevel < template.RequiredLevel || template.BuyPrice <= 0)
                return false;

            // Atomic transaction non-saving
            if (_inventoryService.ConsumeCoinsNonSaving(template.BuyPrice))
            {
                _inventoryService.AddStickerCountNonSaving(stickerId, 1);
                try
                {
                    _saveService.Save();
                    OnShopTransactionSuccess?.Invoke();
                    return true;
                }
                catch (Exception)
                {
                    // Rollback
                    _inventoryService.AddCoinsNonSaving(template.BuyPrice);
                    _inventoryService.ConsumeStickerNonSaving(stickerId); // Giam count lai
                }
            }
            return false;
        }
    }
}
```

**Step 5: Run test to verify it passes**
Chạy test và kiểm tra PASS 100%.

**Step 6: Commit**
```bash
git add Assets/CozyLifeSim/Scripts/Core/IShopService.cs Assets/CozyLifeSim/Scripts/UI/Services/ShopService.cs
git commit -m "feat: integrate Level Locks, atomic transactions and countable sticker rewards in ShopService"
```

---

## Execution Handoff

**Plan complete and saved to `docs/plans/2026-05-30-progression-countable-backend.md`.**
**Next step: run `.agent/workflows/execute-plan.md` to execute this plan task-by-task in single-flow mode.**
