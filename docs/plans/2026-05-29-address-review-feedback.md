# Implementation Plan - Address Task 22 Review Feedback (Revised 2)

This plan addresses code review comments P1, P2, and P3 regarding Task 22 Runtime Loop Polish & Validation:
- **P1**: Play Mode restore is only partial:
  - UI widgets and active services aren't fully rehydrated/refreshed after save reset or backup restore.
  - Sticker runtime placement changes its parent, scale, rotation, and position anchors permanently in the scene. Without a visual reset, the sticker remains placed on the page and is unavailable in future test runs.
- **P2**: Clarification of interface contract changes, kill active tweens during visual reset, and updating `current-handoff.md` to accurately reflect the Task 22 commit.
- **P3**: Gameplay loop coin assertions are brittle because they hardcode reward amounts instead of querying the resolved quest database template values.

## User Review Required

> [!IMPORTANT]
> **Interface Contract Changes**: This plan adds `ReloadFromSave` and reload event actions directly to the `IQuestService` and `IInventoryService` interfaces. 
> A workspace search via `rg "IQuestService|IInventoryService"` confirms that **only** `QuestService` and `InventoryService` implement these interfaces in this repository (there are no other production implementations or mock classes). Therefore, this is safe and won't break compilation elsewhere.

> [!NOTE]
> **Sticker Visual Reset & Tween Lifecycle**: We will implement a non-reflection `ResetToTray(Transform trayParent, Vector2 trayPosition)` method on `CozySticker.cs` that kills active scale/shadow tweens before restoring its hierarchy parent, backed-up rotation, initial scale, position anchors, shadows, and raycasting states, eliminating Play Mode layout drift.

## Proposed Changes

### Core Services Interfaces

#### [MODIFY] [IQuestService.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Core/IQuestService.cs)
- Add `event Action OnQuestsReloaded;`
- Add `void ReloadFromSave(bool logFallbackWarning = true);`

#### [MODIFY] [IInventoryService.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Core/IInventoryService.cs)
- Add `event Action OnInventoryReloaded;`
- Add `void ReloadFromSave();`

---

### UI Services Implementation

#### [MODIFY] [QuestService.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Services/QuestService.cs)
- Implement `public event Action OnQuestsReloaded;`
- Implement `public void ReloadFromSave(bool logFallbackWarning = true)` which clears internal `_quests`, runs `InitializeQuests(logFallbackWarning)`, and fires `OnQuestsReloaded`.

#### [MODIFY] [InventoryService.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Services/InventoryService.cs)
- Implement `public event Action OnInventoryReloaded;`
- Implement `public void ReloadFromSave()` which triggers change events (`OnCoinsChanged`, `OnSeedsChanged`, `OnCropsChanged`) with current save data values, and invokes `OnInventoryReloaded`.

---

### UI Components & Widgets

#### [MODIFY] [QuestHudWidget.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/QuestHudWidget.cs)
- Subscribe to `OnQuestsReloaded` in `Construct`, routing to a new private handler `OnQuestsReloadedHandler()` that triggers `RefreshQuests()`.
- Unsubscribe in `Construct` (for previous instances) and `OnDestroy()`.

#### [MODIFY] [CozySticker.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/CozySticker.cs)
- Declare private fields to backup initial scale and rotation:
  ```csharp
  private Vector3 _startScale;
  private Quaternion _startRotation;
  ```
- Backup initial scale and rotation inside `EnsureInitialized()`:
  ```csharp
  _startScale = transform.localScale;
  _startRotation = transform.localRotation;
  ```
- Implement `public void ResetToTray(Transform trayParent, Vector2 trayPosition)` to safely kill active tweens and restore state:
  ```csharp
  public void ResetToTray(Transform trayParent, Vector2 trayPosition)
  {
      EnsureInitialized();
      
      _scaleTween?.Kill();
      _shadowTween?.Kill();

      transform.SetParent(trayParent, false);
      _rectTransform.anchoredPosition = trayPosition;
      _originalParent = trayParent;
      _startPosition = trayPosition;
      _pageIndex = 0;
      if (_canvasGroup != null)
      {
          _canvasGroup.blocksRaycasts = true;
          _canvasGroup.alpha = 1.0f;
      }
      transform.localScale = _startScale;
      transform.localRotation = _startRotation;
      if (_shadowOffset != null)
      {
          _shadowOffset.anchoredPosition = Vector2.zero;
      }
  }
  ```

---

### Editor Validation Runner

#### [MODIFY] [CozyLifeSimMcpGameplayLoopValidation.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimMcpGameplayLoopValidation.cs)
- Remove reflection logic from `InitializeRuntimeReferences()`. Instead, call `_questService.ReloadFromSave(false)` and `_inventoryService.ReloadFromSave()`.
- Declare static fields to backup sticker visual layout:
  ```csharp
  private static Transform _originalStickerParent;
  private static Vector2 _originalStickerAnchoredPosition;
  ```
- Backup sticker references inside `InitializeRuntimeReferences()`:
  ```csharp
  if (_stickers.Sticker != null)
  {
      _originalStickerParent = _stickers.Sticker.transform.parent;
      var rect = _stickers.Sticker.GetComponent<RectTransform>();
      if (rect != null)
      {
          _originalStickerAnchoredPosition = rect.anchoredPosition;
      }
  }
  ```
- Dynamically resolve the water quest reward:
  ```csharp
  private static int _waterQuestReward;
  private static int GetWaterQuestReward(IQuestService questService)
  {
      QuestData waterQuest = FindQuest(questService, QuestType.WaterCrops);
      return waterQuest != null ? waterQuest.RewardCoins : 50;
  }
  ```
- Set `_waterQuestReward = GetWaterQuestReward(_questService);` in `InitializeRuntimeReferences()`.
- Update `HarvestCrop()` to assert `expectedCoins = _initialCoins + 10 + _waterQuestReward;`.
- Update `PetChicken()` to assert `expectedCoins = _initialCoins + 10 + 5 + _waterQuestReward;`.
- Update `VerifyFreshServicePersistence()` to assert `expectedCoins = 100 + 10 + 5 + GetWaterQuestReward(freshQuest);`.
- Update `RestoreSaveBackup()` to:
  - Copy save backup and call `_saveService.Save()`.
  - Call `_questService.ReloadFromSave(false)` and `_inventoryService.ReloadFromSave()`.
  - Fully restore sticker visual state:
    ```csharp
    if (_stickers.Sticker != null && _originalStickerParent != null)
    {
        _stickers.Sticker.ResetToTray(_originalStickerParent, _originalStickerAnchoredPosition);
    }
    ```

---

### Handoff Documentation

#### [MODIFY] [current-handoff.md](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/current-handoff.md)
- Update `Last completed commit` to `a50ddc4 feat: standardise log formatting and implement play-mode loop validation runner (Task 22)`.
- Replace the uncommitted files under "Current Uncommitted Scope" with "None. All Task 22 work has been successfully committed."

---

## Verification Plan

### Automated Tests
- Run `CozyLifeSimValidation.RunTests` (Logic verification) to ensure interface changes don't break unit tests.
- Run `CozyLifeSimMcpGameplayLoopValidation.RunGameplayLoopValidation` in Play Mode.
- Verify that standard logging produces Clean ASCII summary logs with no errors.
- Confirm that when the runner finishes, the on-screen UI text widgets for Coins, Seeds, and Crops accurately show original save values instead of the temporary test values.
- Confirm that the sticker is back in the inventory tray and can be successfully validated on subsequent runner executions without restarting Play Mode.
