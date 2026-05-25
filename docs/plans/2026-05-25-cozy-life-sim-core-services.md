# Cozy Life Sim Core Services & MVP Integration Plan

> **For Antigravity:** REQUIRED WORKFLOW: Use `.agent/workflows/execute-plan.md` to execute this plan in single-flow mode.

**Goal:** Implement the Core Services Layer (SaveSystem, Inventory, Scrapbook Memory, Quests) using VContainer (DI) and integrate the existing prototype widgets (Crop, Animal, Stickers) using the Event-Driven MVP (Model-View-Presenter) pattern to establish a fully playable, persistent game loop.

**Architecture:** Create pure interface abstractions in the `CozyLifeSim.Core` assembly, with concrete service implementations in the `CozyLifeSim.UI` assembly under a new `Services` namespace. Use VContainer to register and inject services as singletons. Address save-state forking by centering data ownership in `ISaveService.ActiveSave`, implement MonoBehaviour injection helper calls, and mediate all widget updates via clean Presenter events.

**Tech Stack:** Unity 2D, VContainer, UniTask, UGUI, DOTween.

---

### Task 10.5: Prerequisite - Resolve CozySticker compilation errors

**Files:**
- Modify: `Assets/CozyLifeSim/Scripts/UI/CozySticker.cs`

**Step 1: Rewrite DOAnchorPos using standard DOTween.To**
To resolve compiler errors caused by unresolved RectTransform extensions without relying on custom DOTween asmdef compilations, implement a safe and direct manual translation helper inside `CozySticker.cs` using the standard `DOTween.To` method (identical to the pattern successfully employed in `AnimalWidget.cs`).

Replace the `DOAnchorPos` calls with this explicit utility.
Open `Assets/CozyLifeSim/Scripts/UI/CozySticker.cs` and replace lines 43-47, 98, 101-104, and 125-128 with the standard `TweenAnchorPos` calls.

Add this static helper at the bottom of the class:
```csharp
        private static Tween TweenAnchorPos(RectTransform rect, Vector2 endValue, float duration)
        {
            return DOTween.To(
                () => rect.anchoredPosition,
                pos => rect.anchoredPosition = pos,
                endValue,
                duration);
        }
```

Then update all anchor transitions in `CozySticker.cs`:
- In `OnBeginDrag`:
```csharp
            if (_shadowOffset != null)
            {
                _shadowTween = TweenAnchorPos(_shadowOffset, new Vector2(-10f, -15f), 0.15f).SetEase(Ease.OutQuad);
            }
```
- In `OnEndDrag`:
```csharp
                transform.SetParent(_originalParent, true);
                _rectTransform.localScale = Vector3.one;
                
                // Snap back to tray
                TweenAnchorPos(_rectTransform, _startPosition, 0.3f).SetEase(Ease.OutQuad);
                _scaleTween = transform.DOScale(1.0f, 0.3f).SetEase(Ease.OutQuad);

                if (_shadowOffset != null)
                {
                    _shadowTween = TweenAnchorPos(_shadowOffset, Vector2.zero, 0.3f).SetEase(Ease.OutQuad);
                }
```
- In `FinalizePlacement`:
```csharp
            // Return to physical state
            _scaleTween = transform.DOScale(1.0f, 0.2f).SetEase(Ease.OutQuad);
            if (_shadowOffset != null)
            {
                _shadowTween = TweenAnchorPos(_shadowOffset, new Vector2(-3f, -4f), 0.2f).SetEase(Ease.OutQuad);
            }
```

**Step 2: Verification and Commit**
- Compile inside the Unity Editor and ensure the console reports 0 compile errors.
- Stage and commit:
```bash
git add Assets/CozyLifeSim/Scripts/UI/CozySticker.cs
git commit -m "fix: resolve CozySticker compile issues by using standard DOTween.To for RectTransforms"
```

---

### Task 11: Implement Save and Inventory Services in Core and UI Assemblies

**Files:**
- Create: `Assets/CozyLifeSim/Scripts/Core/SaveData.cs`
- Create: `Assets/CozyLifeSim/Scripts/Core/ISaveService.cs`
- Create: `Assets/CozyLifeSim/Scripts/UI/Services/SaveService.cs`
- Create: `Assets/CozyLifeSim/Scripts/Core/IInventoryService.cs`
- Create: `Assets/CozyLifeSim/Scripts/UI/Services/InventoryService.cs`
- Modify: `Assets/CozyLifeSim/Scripts/UI/GameLifetimeScope.cs`

**Step 1: Write `SaveData` and sub-DTOs in Core Assembly**
Create `Assets/CozyLifeSim/Scripts/Core/SaveData.cs` to hold all game state. Include `QuestProgressData` to ensure partial progress is preserved:
```csharp
using System;
using System.Collections.Generic;

namespace CozyLifeSim.Core
{
    [Serializable]
    public struct QuestProgressData
    {
        public int QuestId;
        public int CurrentCount;

        public QuestProgressData(int questId, int currentCount)
        {
            QuestId = questId;
            CurrentCount = currentCount;
        }
    }

    [Serializable]
    public class SaveData
    {
        public int Coins = 100;
        public int Seeds = 5;
        public int Crops = 0;
        public List<StickerPlacedData> PlacedStickers = new List<StickerPlacedData>();
        public List<int> CompletedQuestIds = new List<int>();
        public List<QuestProgressData> ActiveQuestProgress = new List<QuestProgressData>();
    }
}
```

**Step 2: Write Singleton-based `ISaveService` Interface in Core Assembly**
Ensure that `ISaveService` acts as a Single Source of Truth owning the single `ActiveSave` reference to prevent forking:
```csharp
namespace CozyLifeSim.Core
{
    public interface ISaveService
    {
        SaveData ActiveSave { get; }
        void Save();
        void Load();
    }
}
```

**Step 3: Write `SaveService` Implementation with Save Normalization in UI Assembly**
Create `Assets/CozyLifeSim/Scripts/UI/Services/SaveService.cs`. Build a robust `Normalize()` pass to prevent NullReferenceExceptions during legacy data migrations:
```csharp
using System.Collections.Generic;
using UnityEngine;
using CozyLifeSim.Core;

namespace CozyLifeSim.UI.Services
{
    public class SaveService : ISaveService
    {
        private const string SaveKey = "CozyLifeSim_SaveGame";
        public SaveData ActiveSave { get; private set; }

        public SaveService()
        {
            Load();
        }

        public void Load()
        {
            if (PlayerPrefs.HasKey(SaveKey))
            {
                string json = PlayerPrefs.GetString(SaveKey);
                try
                {
                    ActiveSave = JsonUtility.FromJson<SaveData>(json);
                }
                catch (System.Exception) { }
            }

            if (ActiveSave == null)
            {
                ActiveSave = new SaveData();
            }

            NormalizeSaveData();
        }

        private void NormalizeSaveData()
        {
            // Safeguard lists against null deserialization from older saves
            if (ActiveSave.PlacedStickers == null)
            {
                ActiveSave.PlacedStickers = new List<StickerPlacedData>();
            }
            if (ActiveSave.CompletedQuestIds == null)
            {
                ActiveSave.CompletedQuestIds = new List<int>();
            }
            if (ActiveSave.ActiveQuestProgress == null)
            {
                ActiveSave.ActiveQuestProgress = new List<QuestProgressData>();
            }
        }

        public void Save()
        {
            if (ActiveSave == null) return;
            string json = JsonUtility.ToJson(ActiveSave);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }
    }
}
```

**Step 4: Write `IInventoryService` Interface in Core Assembly**
Create `Assets/CozyLifeSim/Scripts/Core/IInventoryService.cs`:
```csharp
using System;

namespace CozyLifeSim.Core
{
    public interface IInventoryService
    {
        int Coins { get; }
        int Seeds { get; }
        int Crops { get; }

        event Action<int> OnCoinsChanged;
        event Action<int> OnSeedsChanged;
        event Action<int> OnCropsChanged;

        void AddCoins(int amount);
        bool ConsumeCoins(int amount);
        void AddSeeds(int amount);
        bool ConsumeSeeds(int amount);
        void AddCrops(int amount);
        bool ConsumeCrops(int amount);
    }
}
```

**Step 5: Write `InventoryService` (consuming the shared `ISaveService` singleton) in UI Assembly**
Create `Assets/CozyLifeSim/Scripts/UI/Services/InventoryService.cs`:
```csharp
using System;
using CozyLifeSim.Core;

namespace CozyLifeSim.UI.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly ISaveService _saveService;
        private SaveData ActiveSave => _saveService.ActiveSave;

        public int Coins => ActiveSave.Coins;
        public int Seeds => ActiveSave.Seeds;
        public int Crops => ActiveSave.Crops;

        public event Action<int> OnCoinsChanged;
        public event Action<int> OnSeedsChanged;
        public event Action<int> OnCropsChanged;

        public InventoryService(ISaveService saveService)
        {
            _saveService = saveService;
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            ActiveSave.Coins += amount;
            OnCoinsChanged?.Invoke(ActiveSave.Coins);
            _saveService.Save();
        }

        public bool ConsumeCoins(int amount)
        {
            if (amount <= 0 || ActiveSave.Coins < amount) return false;
            ActiveSave.Coins -= amount;
            OnCoinsChanged?.Invoke(ActiveSave.Coins);
            _saveService.Save();
            return true;
        }

        public void AddSeeds(int amount)
        {
            if (amount <= 0) return;
            ActiveSave.Seeds += amount;
            OnSeedsChanged?.Invoke(ActiveSave.Seeds);
            _saveService.Save();
        }

        public bool ConsumeSeeds(int amount)
        {
            if (amount <= 0 || ActiveSave.Seeds < amount) return false;
            ActiveSave.Seeds -= amount;
            OnSeedsChanged?.Invoke(ActiveSave.Seeds);
            _saveService.Save();
            return true;
        }

        public void AddCrops(int amount)
        {
            if (amount <= 0) return;
            ActiveSave.Crops += amount;
            OnCropsChanged?.Invoke(ActiveSave.Crops);
            _saveService.Save();
        }

        public bool ConsumeCrops(int amount)
        {
            if (amount <= 0 || ActiveSave.Crops < amount) return false;
            ActiveSave.Crops -= amount;
            OnCropsChanged?.Invoke(ActiveSave.Crops);
            _saveService.Save();
            return true;
        }
    }
}
```

**Step 6: Register Services in `GameLifetimeScope.cs`**
Modify `Assets/CozyLifeSim/Scripts/UI/GameLifetimeScope.cs`:
```csharp
using UnityEngine;
using VContainer;
using VContainer.Unity;
using CozyLifeSim.Core;
using CozyLifeSim.UI.Style;
using CozyLifeSim.UI.Services;

namespace CozyLifeSim.UI
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private UIStyleConfig _defaultStyleConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            // Register Style Service
            builder.Register<IStyleService>(container => new StyleService(_defaultStyleConfig), Lifetime.Singleton);

            // Register Save and Inventory Services as singletons
            builder.Register<ISaveService, SaveService>(Lifetime.Singleton);
            builder.Register<IInventoryService, InventoryService>(Lifetime.Singleton);
        }
    }
}
```

**Step 7: Verification and Commit**
- Stage and commit:
```bash
git add Assets/CozyLifeSim/Scripts/Core/SaveData.cs Assets/CozyLifeSim/Scripts/Core/ISaveService.cs Assets/CozyLifeSim/Scripts/UI/Services/SaveService.cs Assets/CozyLifeSim/Scripts/Core/IInventoryService.cs Assets/CozyLifeSim/Scripts/UI/Services/InventoryService.cs Assets/CozyLifeSim/Scripts/UI/GameLifetimeScope.cs
git commit -m "feat: implement persistent, non-forking save and inventory systems with VContainer registration"
```

---

### Task 12: Implement Scrapbook Memory and Quest Services

**Files:**
- Create: `Assets/CozyLifeSim/Scripts/Core/IMemoryService.cs`
- Create: `Assets/CozyLifeSim/Scripts/UI/Services/MemoryService.cs`
- Create: `Assets/CozyLifeSim/Scripts/Core/QuestData.cs`
- Create: `Assets/CozyLifeSim/Scripts/Core/IQuestService.cs`
- Create: `Assets/CozyLifeSim/Scripts/UI/Services/QuestService.cs`
- Modify: `Assets/CozyLifeSim/Scripts/UI/GameLifetimeScope.cs`

**Step 1: Write `IMemoryService` Interface in Core Assembly**
Create `Assets/CozyLifeSim/Scripts/Core/IMemoryService.cs`:
```csharp
using System.Collections.Generic;

namespace CozyLifeSim.Core
{
    public interface IMemoryService
    {
        IReadOnlyList<StickerPlacedData> PlacedStickers { get; }
        void PlaceSticker(StickerPlacedData sticker);
        void RemoveSticker(int stickerId, int pageIndex);
    }
}
```

**Step 2: Write `MemoryService` (modifying shared save singleton) in UI Assembly**
Create `Assets/CozyLifeSim/Scripts/UI/Services/MemoryService.cs`:
```csharp
using System.Collections.Generic;
using CozyLifeSim.Core;

namespace CozyLifeSim.UI.Services
{
    public class MemoryService : IMemoryService
    {
        private readonly ISaveService _saveService;
        private SaveData ActiveSave => _saveService.ActiveSave;

        public IReadOnlyList<StickerPlacedData> PlacedStickers => ActiveSave.PlacedStickers;

        public MemoryService(ISaveService saveService)
        {
            _saveService = saveService;
        }

        public void PlaceSticker(StickerPlacedData sticker)
        {
            // Remove existing placement of same sticker on this page
            ActiveSave.PlacedStickers.RemoveAll(x => x.StickerId == sticker.StickerId && x.PageIndex == sticker.PageIndex);
            ActiveSave.PlacedStickers.Add(sticker);
            _saveService.Save();
        }

        public void RemoveSticker(int stickerId, int pageIndex)
        {
            ActiveSave.PlacedStickers.RemoveAll(x => x.StickerId == stickerId && x.PageIndex == pageIndex);
            _saveService.Save();
        }
    }
}
```

**Step 3: Write `QuestData` in Core Assembly**
Create `Assets/CozyLifeSim/Scripts/Core/QuestData.cs`:
```csharp
using System;

namespace CozyLifeSim.Core
{
    [Serializable]
    public class QuestData
    {
        public int QuestId;
        public string Title;
        public int TargetCount;
        public int CurrentCount;
        public int RewardCoins;
        public bool IsCompleted;

        public QuestData(int questId, string title, int targetCount, int rewardCoins)
        {
            QuestId = questId;
            Title = title;
            TargetCount = targetCount;
            RewardCoins = rewardCoins;
            CurrentCount = 0;
            IsCompleted = false;
        }
    }
}
```

**Step 4: Write `IQuestService` Interface in Core Assembly**
Create `Assets/CozyLifeSim/Scripts/Core/IQuestService.cs`:
```csharp
using System;
using System.Collections.Generic;

namespace CozyLifeSim.Core
{
    public interface IQuestService
    {
        IReadOnlyList<QuestData> ActiveQuests { get; }
        event Action<QuestData> OnQuestProgressed;
        event Action<QuestData> OnQuestCompleted;

        void ProgressQuest(int questId, int amount);
    }
}
```

**Step 5: Write `QuestService` (supporting QuestProgressData persistence) in UI Assembly**
Create `Assets/CozyLifeSim/Scripts/UI/Services/QuestService.cs` ensuring partial progress is persisted and restored correctly:
```csharp
using System;
using System.Collections.Generic;
using CozyLifeSim.Core;

namespace CozyLifeSim.UI.Services
{
    public class QuestService : IQuestService
    {
        private readonly ISaveService _saveService;
        private readonly IInventoryService _inventoryService;
        private SaveData ActiveSave => _saveService.ActiveSave;

        private readonly List<QuestData> _quests = new List<QuestData>();
        public IReadOnlyList<QuestData> ActiveQuests => _quests;

        public event Action<QuestData> OnQuestProgressed;
        public event Action<QuestData> OnQuestCompleted;

        public QuestService(ISaveService saveService, IInventoryService inventoryService)
        {
            _saveService = saveService;
            _inventoryService = inventoryService;

            InitializeQuests();
        }

        private void InitializeQuests()
        {
            _quests.Add(new QuestData(1, "Water 3 Crops", 3, 50));
            _quests.Add(new QuestData(2, "Harvest 2 Mature Crops", 2, 80));
            _quests.Add(new QuestData(3, "Pet the Breathing Chicken 5 times", 5, 40));

            // Load completion states and partial progress
            foreach (var quest in _quests)
            {
                if (ActiveSave.CompletedQuestIds.Contains(quest.QuestId))
                {
                    quest.CurrentCount = quest.TargetCount;
                    quest.IsCompleted = true;
                }
                else
                {
                    var progress = ActiveSave.ActiveQuestProgress.Find(x => x.QuestId == quest.QuestId);
                    quest.CurrentCount = Math.Min(progress.CurrentCount, quest.TargetCount);
                }
            }
        }

        public void ProgressQuest(int questId, int amount)
        {
            QuestData quest = _quests.Find(x => x.QuestId == questId);
            if (quest == null || quest.IsCompleted) return;

            quest.CurrentCount = Math.Min(quest.CurrentCount + amount, quest.TargetCount);
            
            // Persist partial progress
            ActiveSave.ActiveQuestProgress.RemoveAll(x => x.QuestId == questId);
            if (quest.CurrentCount < quest.TargetCount)
            {
                ActiveSave.ActiveQuestProgress.Add(new QuestProgressData(questId, quest.CurrentCount));
            }
            
            _saveService.Save();
            OnQuestProgressed?.Invoke(quest);

            if (quest.CurrentCount >= quest.TargetCount)
            {
                quest.IsCompleted = true;
                ActiveSave.CompletedQuestIds.Add(questId);
                ActiveSave.ActiveQuestProgress.RemoveAll(x => x.QuestId == questId);
                _saveService.Save();
                
                // Reward coins
                _inventoryService.AddCoins(quest.RewardCoins);
                OnQuestCompleted?.Invoke(quest);
            }
        }
    }
}
```

**Step 6: Register Memory and Quest Services in `GameLifetimeScope.cs`**
Modify `Assets/CozyLifeSim/Scripts/UI/GameLifetimeScope.cs`:
```csharp
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<IStyleService>(container => new StyleService(_defaultStyleConfig), Lifetime.Singleton);

            builder.Register<ISaveService, SaveService>(Lifetime.Singleton);
            builder.Register<IInventoryService, InventoryService>(Lifetime.Singleton);

            // Register Memory and Quest singletons
            builder.Register<IMemoryService, MemoryService>(Lifetime.Singleton);
            builder.Register<IQuestService, QuestService>(Lifetime.Singleton);
        }
```

---

### Task 13: Implement Event-Driven MVP Presenters and Restore Flows

**Files:**
- Create: `Assets/CozyLifeSim/Scripts/UI/Presenters/FarmPresenter.cs`
- Create: `Assets/CozyLifeSim/Scripts/UI/Presenters/AnimalPresenter.cs`
- Create: `Assets/CozyLifeSim/Scripts/UI/Presenters/StickerBookPresenter.cs`
- Modify: `Assets/CozyLifeSim/Scripts/UI/GameLifetimeScope.cs`

**Step 1: Write `FarmPresenter`**
Create `Assets/CozyLifeSim/Scripts/UI/Presenters/FarmPresenter.cs`:
```csharp
using System;
using VContainer;
using CozyLifeSim.Core;

namespace CozyLifeSim.UI.Presenters
{
    public class FarmPresenter : IDisposable
    {
        private readonly IInventoryService _inventory;
        private readonly IQuestService _quest;

        public event Action<bool> OnPlantAttemptResult;
        public event Action OnCropHarvested;

        [Inject]
        public FarmPresenter(IInventoryService inventory, IQuestService quest)
        {
            _inventory = inventory;
            _quest = quest;
        }

        public bool TryPlantCrop()
        {
            bool success = _inventory.ConsumeSeeds(1);
            OnPlantAttemptResult?.Invoke(success);
            return success;
        }

        public void NotifyCropWatered()
        {
            _quest.ProgressQuest(1, 1);
        }

        public void HarvestCrop()
        {
            _inventory.AddCrops(1);
            _inventory.AddCoins(10);
            _quest.ProgressQuest(2, 1);
            OnCropHarvested?.Invoke();
        }

        public void Dispose() { }
    }
}
```

**Step 2: Write `AnimalPresenter`**
Create `Assets/CozyLifeSim/Scripts/UI/Presenters/AnimalPresenter.cs`:
```csharp
using System;
using VContainer;
using CozyLifeSim.Core;

namespace CozyLifeSim.UI.Presenters
{
    public class AnimalPresenter : IDisposable
    {
        private readonly IInventoryService _inventory;
        private readonly IQuestService _quest;

        public event Action<int> OnPetRewardGiven;

        [Inject]
        public AnimalPresenter(IInventoryService inventory, IQuestService quest)
        {
            _inventory = inventory;
            _quest = quest;
        }

        public void PetAnimal()
        {
            _inventory.AddCoins(5);
            _quest.ProgressQuest(3, 1);
            OnPetRewardGiven?.Invoke(5);
        }

        public void Dispose() { }
    }
}
```

**Step 3: Write `StickerBookPresenter` supporting Save, Remove, and Layout Restore**
Create `Assets/CozyLifeSim/Scripts/UI/Presenters/StickerBookPresenter.cs` with direct query for restore flow:
```csharp
using System;
using System.Collections.Generic;
using VContainer;
using CozyLifeSim.Core;

namespace CozyLifeSim.UI.Presenters
{
    public class StickerBookPresenter : IDisposable
    {
        private readonly IMemoryService _memory;

        [Inject]
        public StickerBookPresenter(IMemoryService memory)
        {
            _memory = memory;
        }

        public IReadOnlyList<StickerPlacedData> GetPlacedStickers()
        {
            return _memory.PlacedStickers;
        }

        public void SaveStickerPosition(int stickerId, int pageIndex, float x, float y, float scale, float rot)
        {
            var data = new StickerPlacedData(stickerId, pageIndex, x, y, scale, rot);
            _memory.PlaceSticker(data);
        }

        public void RemoveSticker(int stickerId, int pageIndex)
        {
            _memory.RemoveSticker(stickerId, pageIndex);
        }

        public void Dispose() { }
    }
}
```

**Step 4: Register Presenters in `GameLifetimeScope.cs`**
Modify `Assets/CozyLifeSim/Scripts/UI/GameLifetimeScope.cs`:
```csharp
        protected override void Configure(IContainerBuilder builder)
        {
            // Services
            builder.Register<IStyleService>(container => new StyleService(_defaultStyleConfig), Lifetime.Singleton);
            builder.Register<ISaveService, SaveService>(Lifetime.Singleton);
            builder.Register<IInventoryService, InventoryService>(Lifetime.Singleton);
            builder.Register<IMemoryService, MemoryService>(Lifetime.Singleton);
            builder.Register<IQuestService, QuestService>(Lifetime.Singleton);

            // Presenters
            builder.Register<FarmPresenter>(Lifetime.Singleton);
            builder.Register<AnimalPresenter>(Lifetime.Singleton);
            builder.Register<StickerBookPresenter>(Lifetime.Singleton);
        }
```

---

### Task 14: Connect UI Widgets, Implement Full Planting Game Loop & Sticker Restore

**Files:**
- Modify: `Assets/CozyLifeSim/Scripts/UI/CropWidget.cs`
- Modify: `Assets/CozyLifeSim/Scripts/UI/AnimalWidget.cs`
- Modify: `Assets/CozyLifeSim/Scripts/UI/CozySticker.cs`
- Modify: `Assets/CozyLifeSim/Scripts/UI/StickerBookPage.cs`
- Modify: `Assets/CozyLifeSim/Scripts/UI/StickerBook.cs`

**Step 1: Write Full Planting Game Loop in `CropWidget.cs`**
Add an `Empty Soil` (state `-1`) visual representation. Clicking the plot in `Empty Soil` triggers `TryPlantCrop`. Trigger the watering quest progress strictly upon completing the watering action (inside `CompleteWatering`), preventing progress for aborted/interrupted animations. Ensure correct namespace imports (`using VContainer; using VContainer.Unity; using CozyLifeSim.UI.Presenters;`):

Modify `Assets/CozyLifeSim/Scripts/UI/CropWidget.cs`:
- Inject `FarmPresenter` via `Construct()`.
- Implement VContainer MonoBehaviour auto-injection pattern inside `Start()`.
- Add `_plantButton` and `_harvestButton` references.
- In `Start()`:
  - Find GameLifetimeScope and run `Inject(this)`.
  - Set `_state` default `GrowthStage = -1` (Empty Soil).
  - Bind click listeners.
- Replace/Extend visual update:
  - If `GrowthStage == -1`, show `_plantButton` and hide visual image;
  - If `GrowthStage >= 0`, hide `_plantButton` and show growth stage visual.
  - Make `_harvestButton.interactable = _state.GrowthStage >= 3`.
- In `OnDestroy()`, remove plant/harvest listeners cleanly.

Detailed implementation lines:
```csharp
using System;
using System.Threading;
using CozyLifeSim.Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer; // Required DI import
using VContainer.Unity; // Required LifetimeScope import
using CozyLifeSim.UI.Presenters; // Required Presenter import

namespace CozyLifeSim.UI
{
    public class CropWidget : MonoBehaviour
    {
        // ... (Fields from previous steps)
        
        private FarmPresenter _presenter;
        [SerializeField] private Button _plantButton;
        [SerializeField] private Button _harvestButton;

        [Inject]
        public void Construct(FarmPresenter presenter)
        {
            _presenter = presenter;
        }

        private void Start()
        {
            // Auto Injection helper
            if (Application.isPlaying)
            {
                var scope = LifetimeScope.Find<GameLifetimeScope>();
                if (scope != null && scope.Container != null)
                {
                    scope.Container.Inject(this);
                }
            }

            // Start in empty soil stage (-1)
            _state = new CropState(_cropId, -1, Mathf.Max(1f, _stageDurationSeconds), false);
            UpdateVisuals();

            if (_wateringCan != null) _wateringCan.gameObject.SetActive(false);
            if (_waterButton != null) _waterButton.onClick.AddListener(Irrigate);
            if (_plantButton != null) _plantButton.onClick.AddListener(PlantSeed);
            if (_harvestButton != null) _harvestButton.onClick.AddListener(HarvestCrop);

            _cts = new CancellationTokenSource();
            RunGrowthTimer(_cts.Token).Forget();
        }

        private void PlantSeed()
        {
            if (_state.GrowthStage != -1 || _presenter == null) return;

            if (_presenter.TryPlantCrop())
            {
                _state = new CropState(_cropId, 0, Mathf.Max(1f, _stageDurationSeconds), false);
                UpdateVisuals();
            }
        }

        private void HarvestCrop()
        {
            if (_state.GrowthStage < 3 || _presenter == null) return;

            _presenter.HarvestCrop();
            
            // Reset plot back to empty soil
            _state = new CropState(_cropId, -1, Mathf.Max(1f, _stageDurationSeconds), false);
            UpdateVisuals();
        }

        private void CompleteWatering()
        {
            _state.IsWatered = true;
            _isWatering = false;

            if (_cropVisual != null)
            {
                _cropFeedbackTween?.Kill();
                _cropFeedbackTween = _cropVisual.transform.DOShakePosition(0.4f, 8f, 15);
            }

            // Notify quest watered progress ONLY on successful complete watering completion
            if (_presenter != null)
            {
                _presenter.NotifyCropWatered();
            }

            UpdateVisuals();
        }
    }
}
```

Ensure `UpdateVisuals` hides visuals when stage is `-1` and enables proper buttons.

**Step 2: Inject `AnimalPresenter` and Setup Auto-Injection in `AnimalWidget.cs`**
Modify `Assets/CozyLifeSim/Scripts/UI/AnimalWidget.cs`. Add namespace imports (`using VContainer; using VContainer.Unity; using CozyLifeSim.UI.Presenters;`):
```csharp
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using VContainer; // Required DI import
using VContainer.Unity; // Required LifetimeScope import
using CozyLifeSim.UI.Presenters; // Required Presenter import

namespace CozyLifeSim.UI
{
    public class AnimalWidget : MonoBehaviour
    {
        // ... (Fields)
        private AnimalPresenter _presenter;

        [Inject]
        public void Construct(AnimalPresenter presenter)
        {
            _presenter = presenter;
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                var scope = LifetimeScope.Find<GameLifetimeScope>();
                if (scope != null && scope.Container != null)
                {
                    scope.Container.Inject(this);
                }
            }
            
            _baseScale = transform.localScale;
            _breathTween = transform.DOScaleY(_baseScale.y * 1.03f, 1.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            if (_interactionButton != null)
            {
                _interactionButton.onClick.AddListener(PetAnimal);
            }
        }
        
        private void PetAnimal()
        {
            // ... (Petting checks & tweens)
            _presenter?.PetAnimal();
            // ...
        }
    }
}
```

**Step 3: Modify `StickerBookPage` to preserve PageIndex public property**
Modify `Assets/CozyLifeSim/Scripts/UI/StickerBookPage.cs`. Explicitly add/preserve the `PageIndex` property so restore lookup methods work cleanly:
```csharp
using UnityEngine;
using UnityEngine.UI;

namespace CozyLifeSim.UI
{
    [RequireComponent(typeof(Image))]
    public class StickerBookPage : MonoBehaviour
    {
        [SerializeField] private int _pageIndex;
        public int PageIndex => _pageIndex;

        public bool TryPlaceSticker(CozySticker sticker, Vector2 screenPosition, Camera eventCamera)
        {
            RectTransform pageRect = GetComponent<RectTransform>();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(pageRect, screenPosition, eventCamera, out Vector2 localPoint))
            {
                if (pageRect.rect.Contains(localPoint))
                {
                    sticker.FinalizePlacement(transform, localPoint, _pageIndex);
                    return true;
                }
            }
            return false;
        }
    }
}
```

**Step 4: Update `CozySticker.cs` to accept optional `saveToDisk` parameter and preserve components in Awake()**
Modify `Assets/CozyLifeSim/Scripts/UI/CozySticker.cs`. Keep initializations in `Awake()` so that `EnsureInitialized()` components like `_rectTransform` are immediately available upon `Instantiate` calls in the restore loop. Add namespace imports (`using VContainer; using VContainer.Unity; using CozyLifeSim.UI.Presenters;`):
```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using VContainer; // Required DI import
using VContainer.Unity; // Required LifetimeScope import
using CozyLifeSim.UI.Presenters; // Required Presenter import

namespace CozyLifeSim.UI
{
    public class CozySticker : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        // ... (Fields)
        private StickerBookPresenter _presenter;
        private int _pageIndex;

        [Inject]
        public void Construct(StickerBookPresenter presenter)
        {
            _presenter = presenter;
        }

        private void Awake()
        {
            // CRITICAL: Cache essential RectTransform components IMMEDIATELY on Awake
            // This guarantees they are loaded before Restoration loops call FinalizePlacement()
            _rectTransform = GetComponent<RectTransform>();
            _startPosition = _rectTransform.anchoredPosition;
            _originalParent = transform.parent;
            _canvas = GetComponentInParent<Canvas>();
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                var scope = LifetimeScope.Find<GameLifetimeScope>();
                if (scope != null && scope.Container != null)
                {
                    scope.Container.Inject(this);
                }
            }
        }

        public void FinalizePlacement(Transform pageParent, Vector2 pageAnchoredPosition, int pageIndex, bool saveToDisk = true)
        {
            _pageIndex = pageIndex;
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.alpha = 1.0f;
            }

            transform.SetParent(pageParent, true);
            _rectTransform.anchoredPosition = pageAnchoredPosition;
            
            _originalParent = pageParent;
            _startPosition = pageAnchoredPosition;

            _scaleTween = transform.DOScale(1.0f, 0.2f).SetEase(Ease.OutQuad);
            if (_shadowOffset != null)
            {
                _shadowTween = TweenAnchorPos(_shadowOffset, new Vector2(-3f, -4f), 0.2f).SetEase(Ease.OutQuad);
            }

            // Only save when not initializing from restore loop
            if (saveToDisk)
            {
                _presenter?.SaveStickerPosition(_stickerId, _pageIndex, pageAnchoredPosition.x, pageAnchoredPosition.y, transform.localScale.x, transform.localRotation.eulerAngles.z);
            }
        }
    }
}
```

**Step 5: Implement Sticker Scrapbook Restore Flow and Imports in `StickerBook.cs`**
Modify `Assets/CozyLifeSim/Scripts/UI/StickerBook.cs`. Ensure correct imports (`using VContainer; using VContainer.Unity; using CozyLifeSim.UI.Presenters;`):
- Inject `StickerBookPresenter` via `Construct()`.
- Add auto-injection block in `Start()`.
- Read existing stickers from `_presenter.GetPlacedStickers()` and instantiate/reposition sticker prefabs (map `StickerId` to sticker templates) onto the correct page.
- Wrap positioning calls inside standard `if (rect != null)` checks to eliminate NullReferenceException risks:

Inside `StickerBook.cs`:
```csharp
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;
using VContainer; // Required DI import
using VContainer.Unity; // Required LifetimeScope import
using CozyLifeSim.UI.Presenters; // Required Presenter import

namespace CozyLifeSim.UI
{
    public class StickerBook : MonoBehaviour
    {
        [SerializeField] private List<StickerBookPage> _pages;
        [SerializeField] private RectTransform _flipPageIndicator;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _prevButton;
        
        [SerializeField] private List<CozySticker> _stickerTemplates; // Prefab references mapping StickerId
        private StickerBookPresenter _presenter;

        [Inject]
        public void Construct(StickerBookPresenter presenter)
        {
            _presenter = presenter;
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                var scope = LifetimeScope.Find<GameLifetimeScope>();
                if (scope != null && scope.Container != null)
                {
                    scope.Container.Inject(this);
                }
            }

            if (_pages == null || _pages.Count == 0)
            {
                if (_nextButton != null) _nextButton.interactable = false;
                if (_prevButton != null) _prevButton.interactable = false;
                return;
            }

            // Set initial page states
            for (int i = 0; i < _pages.Count; i++)
            {
                if (_pages[i] != null)
                {
                    _pages[i].gameObject.SetActive(i == _currentPageIndex);
                }
            }

            if (_nextButton != null) _nextButton.onClick.AddListener(NextPage);
            if (_prevButton != null) _prevButton.onClick.AddListener(PrevPage);
            
            UpdateNavigationButtons();

            // Restore sticker layouts from database on load
            RestoreStickers();
        }

        private void RestoreStickers()
        {
            if (_presenter == null || _stickerTemplates == null) return;

            var placedList = _presenter.GetPlacedStickers();
            foreach (var item in placedList)
            {
                // Find matching page index
                StickerBookPage targetPage = _pages.Find(p => p.PageIndex == item.PageIndex);
                if (targetPage == null) continue;

                // Find template prefab
                CozySticker prefab = _stickerTemplates.Find(t => t.StickerId == item.StickerId);
                if (prefab == null) continue;

                // Spawn and position safely
                CozySticker spawned = Instantiate(prefab, targetPage.transform);
                RectTransform rect = spawned.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchoredPosition = new Vector2(item.PositionX, item.PositionY);
                    rect.localScale = new Vector3(item.Scale, item.Scale, 1.0f);
                    rect.localRotation = Quaternion.Euler(0f, 0f, item.Rotation);
                    
                    // Finalize positioning with saveToDisk set to FALSE to eliminate PlayerPrefs loops
                    spawned.FinalizePlacement(targetPage.transform, rect.anchoredPosition, item.PageIndex, false);
                }
            }
        }
    }
}
```

---

## Verification Plan

### Automated Verification
- Open Unity 6, wait for compiler execution, and verify 0 assembly/compiler errors.

### Manual Verification
1. **P2 Compilation Check:** Confirm `CozySticker.cs` compiles cleanly using `TweenAnchorPos` without DOAnchorPos extension methods.
2. **P0 Shared Save Session:** Pet the chicken (Quest 3 progress = 1) and harvest a crop (+10 coins). Verify that both partial quest progress and ví tiền save together in Unity PlayerPrefs without overriding each other.
3. **P0 Injection Check:** Open runtime scene, click elements, and confirm VContainer Construction injects `FarmPresenter`, `AnimalPresenter`, and `StickerBookPresenter` without NullReference errors.
4. **P1 Full Farm Loop:** Gieo hạt (tiêu hao 1 seed) -> Đất đổi sang Seed -> Tưới nước -> Hạt lớn thành Mầm -> Cây trưởng thành -> Thu hoạch (+1 crop, reset đất trống).
5. **P1 Sticker Layout Persistence:** Place sticker in book, turn pages, close book. Play again and confirm sticker template spawned at exact layout coordinates.
