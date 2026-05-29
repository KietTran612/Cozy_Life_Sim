# Runtime Loop Polish & Validation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the current Farm / Animal / Sticker / Quest sidebar prototype behave as one smooth playable loop and verify it through clean Unity MCP runtime validation.

**Architecture:** Keep the existing VContainer service/presenter/widget architecture. Improve validation first, then apply only targeted runtime/UI fixes that are required by the accepted loop. Runtime validation should prove both live interaction state and PlayerPrefs-backed persistence without permanently modifying the user's save data.

**Tech Stack:** Unity 6000.3.11f1, C#, UGUI, TextMeshPro, VContainer, DOTween, UniTask, UnityEditor, Unity MCP.

---

## User Review Required

> [!NOTE]
> This plan intentionally avoids broad redesign. It only covers the accepted Task 22 loop:
> Generate scene -> enter Play Mode -> plant/water/harvest -> inventory updates -> quest progress -> pet animal -> place sticker -> save/load verification -> clean validation logs.

---

## Current Problems To Address

1. `CozyLifeSimMcpGameplayLoopValidation.RunGameplayLoopValidation()` currently executes steps synchronously even though the file already contains `Tick()` and timeout logic. Task 22 should use a real `EditorApplication.update` step runner.
2. Save/load validation must be explicit. Calling `ISaveService.Load()` alone does not rebuild all quest runtime state; persistence should be verified through freshly constructed services loaded from PlayerPrefs.
3. Default quests require 3 water actions, 2 harvest actions, and 5 pet actions. The accepted Task 22 loop should complete the Water quest and verify partial persistence for Harvest `1/2` and Pet `1/5`.
4. Validation logs currently mix prefixes and include Unicode icons that can appear as mojibake in some terminals. Logs should use ASCII-safe, prominent prefixes.
5. The `QuestDatabase null` fallback warning is expected in one logic test path, but it currently appears as ordinary warning noise.

---

## Files To Touch

### Editor Validation

- Create: [CozyValidationLog.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozyValidationLog.cs)
- Modify: [CozyLifeSimMcpGameplayLoopValidation.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimMcpGameplayLoopValidation.cs)
- Modify: [CozyLifeSimValidation.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimValidation.cs)
- Modify: [CozyLifeSimSceneGameplayValidation.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimSceneGameplayValidation.cs)
- Modify: [QuestService.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Services/QuestService.cs)

### Runtime/UI, Only If Validation Shows A Real Issue

- Modify if needed: [CropWidget.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/CropWidget.cs)
- Modify if needed: [AnimalWidget.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/AnimalWidget.cs)
- Modify if needed: [StickerBook.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/StickerBook.cs)
- Modify if needed: [CozySticker.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/CozySticker.cs)
- Modify if needed: [QuestHudWidget.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/QuestHudWidget.cs)

### Project Tracking

- Modify: [task.md](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/task.md)
- Modify: [current-handoff.md](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/current-handoff.md)
- Modify: [index.md](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/index.md)

---

## Non-Goals

- Do not redesign the whole UI.
- Do not add new database editor windows.
- Do not add new gameplay systems beyond the accepted loop.
- Do not change VContainer architecture unless validation exposes a direct runtime failure.
- Do not force all default quests to complete in this task. Complete Water quest, then verify persisted partial progress for Harvest and Pet.

---

## Task 1: Create Shared ASCII-Safe Validation Logger

**Files:**
- Create: `Assets/CozyLifeSim/Scripts/Editor/CozyValidationLog.cs`

- [ ] **Step 1: Add the logger**

Create `CozyValidationLog.cs`:

```csharp
using UnityEngine;

namespace CozyLifeSim.Editor
{
    internal static class CozyValidationLog
    {
        public static void Pass(string scope, string message)
        {
            Debug.Log($"<color=green>[{scope}] PASS</color> {message}");
        }

        public static void Warn(string scope, string message)
        {
            Debug.LogWarning($"<color=yellow>[{scope}] WARN</color> {message}");
        }

        public static void ExpectedWarning(string scope, string message)
        {
            Debug.Log($"<color=yellow>[{scope}] EXPECTED WARNING</color> {message}");
        }

        public static void Fail(string scope, string message)
        {
            Debug.LogError($"<color=red>[{scope}] FAIL</color> {message}");
        }

        public static void Summary(string scope, int passed, int failed, int expectedWarnings = 0)
        {
            string status = failed == 0 ? "PASSED" : "FAILED";
            string color = failed == 0 ? "green" : "red";
            string warningText = expectedWarnings > 0 ? $", {expectedWarnings} expected warnings" : string.Empty;
            Debug.Log(
                "\n===================================================================================\n" +
                $"<size=14><b>[{scope}] <color={color}>{status}</color></b></size>\n" +
                $"<b>Summary:</b> {passed} passed, {failed} failed{warningText}\n" +
                "===================================================================================\n");
        }
    }
}
```

- [ ] **Step 2: Wait for Unity import**

Wait for Unity to generate `CozyValidationLog.cs.meta`. Do not manually create the `.meta` file.

- [ ] **Step 3: Compile check**

Run Unity MCP `editor_get_status` until `isCompiling=false`, then check console errors. Expected: `0` compiler errors.

---

## Task 2: Clean Logic Validation Logs And Expected Quest Fallback

**Files:**
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimValidation.cs`
- Modify: `Assets/CozyLifeSim/Scripts/UI/Services/QuestService.cs`

- [ ] **Step 1: Replace Unicode/icon summary logs**

Replace the final success/failure banners with `CozyValidationLog.Summary("CozySim Logic", passCount, failCount, expectedWarningCount)`.

Implementation requirement:
- Introduce local counters:
  - `int passCount = 0;`
  - `int expectedWarningCount = 0;`
- Increment `passCount` after each successful test block.
- Use ASCII text only in source strings.

- [ ] **Step 2: Avoid noisy fallback warning in the normal persistence path**

The existing `QuestService(saveService, invService, null)` test intentionally hits fallback. Keep one explicit fallback test, but make the normal save/load persistence test use an in-memory `QuestDatabase`:

```csharp
var questDb = ScriptableObject.CreateInstance<CozyLifeSim.UI.Settings.QuestDatabase>();
questDb.Quests.Add(new QuestTemplate(1, "Water 3 Crops", 3, 50, QuestType.WaterCrops));
questDb.Quests.Add(new QuestTemplate(2, "Harvest 2 Mature Crops", 2, 80, QuestType.HarvestCrops));
questDb.Quests.Add(new QuestTemplate(3, "Pet the Breathing Chicken 5 times", 5, 40, QuestType.PetAnimal));

QuestService questService = new QuestService(saveService, invService, questDb);
```

Destroy the in-memory database with `Object.DestroyImmediate(questDb)` before the method exits.

- [ ] **Step 3: Add explicit expected fallback test**

Add a small test block that constructs `QuestService(saveService, invService, null)` only to verify fallback behavior:

```csharp
QuestService fallbackQuestService = new QuestService(saveService, invService, null, false);
if (fallbackQuestService.ActiveQuests.Count != 3)
{
    throw new System.Exception("Fallback quest service should contain 3 default quests.");
}

expectedWarningCount++;
CozyValidationLog.ExpectedWarning("CozySim Logic", "QuestDatabase null fallback was intentionally exercised.");
```

Add optional parameter `bool logFallbackWarning = true` to `QuestService` constructor and guard the fallback warning:

```csharp
public QuestService(ISaveService saveService, IInventoryService inventoryService, QuestDatabase questDatabase, bool logFallbackWarning = true)
```

This is backward-compatible with all current call sites.

Inside the fallback branch:

```csharp
if (logFallbackWarning)
{
    UnityEngine.Debug.LogWarning("[CozySim] QuestDatabase is null or empty. Falling back to default hardcoded quests.");
}
```

Use `new QuestService(saveService, invService, null, false)` in tests that intentionally exercise fallback and immediately log `CozyValidationLog.ExpectedWarning(...)`.

- [ ] **Step 4: Run logic validation**

Run `Tools/CozySim/Run Logic Verification Tests` via Unity MCP.

Expected:
- All logic tests pass.
- No mojibake/icon text appears in new logs.
- Any fallback behavior is shown as `EXPECTED WARNING`, not as unexplained noise.

---

## Task 3: Convert MCP Gameplay Validation To Async Step Runner

**Files:**
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimMcpGameplayLoopValidation.cs`

- [ ] **Step 1: Replace synchronous loop with update-driven runner**

In `RunGameplayLoopValidation()`:
- keep the Play Mode guard;
- keep the already-running guard;
- initialize references and state;
- subscribe once to `EditorApplication.update += Tick`;
- set `_deadline = Time.realtimeSinceStartup + StepTimeoutSeconds`;
- do not run a `for` loop over all steps.

Expected shape:

```csharp
_stepIndex = 0;
_isRunning = true;
_deadline = Time.realtimeSinceStartup + StepTimeoutSeconds;
EditorApplication.update -= Tick;
EditorApplication.update += Tick;
CozyValidationLog.Pass("CozySim RuntimeLoop", "Started runtime validation.");
```

- [ ] **Step 2: Ensure Finish unsubscribes**

At the top of `Finish()`:

```csharp
EditorApplication.update -= Tick;
```

Then restore save backup if needed and print summary through `CozyValidationLog.Summary`.

- [ ] **Step 3: Keep accelerated crop growth explicit**

The validator may keep using private reflection to accelerate crop stages, but log this as accelerated runtime validation:

```csharp
CozyValidationLog.ExpectedWarning("CozySim RuntimeLoop", "Crop growth is accelerated through validation hooks to keep MCP runtime tests deterministic.");
```

This is acceptable because the UI buttons, presenters, services, and save path are still real runtime objects.

- [ ] **Step 4: Run Play Mode validation smoke test**

Via Unity MCP:
1. Run `CozySceneSetupWindow.GenerateSceneSilent`.
2. Enter Play Mode.
3. Invoke `CozyLifeSimMcpGameplayLoopValidation.RunGameplayLoopValidation`.
4. Poll console/logs until validation summary appears.

Expected:
- Runner advances across editor updates.
- It does not hang.
- It prints a single final summary.

---

## Task 4: Add Precise Quest, UI, And Persistence Assertions To Runtime Loop

**Files:**
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimMcpGameplayLoopValidation.cs`

- [ ] **Step 1: Resolve quest service and key UI widgets**

Add fields:

```csharp
private static IQuestService _questService;
private static InventoryHudWidget _inventoryHud;
private static QuestHudWidget _questHud;
```

In `InitializeRuntimeReferences()` resolve `_questService` and find HUD widgets:

```csharp
_questService = scope.Container.Resolve<IQuestService>();
_inventoryHud = FindSceneComponent<InventoryHudWidget>("Header_Panel");
_questHud = FindSceneComponent<QuestHudWidget>("Quest_Content");
```

`CozySceneSetupWindow` attaches `QuestHudWidget` to the `Quest_Content` GameObject, so the lookup must stay exact unless the scene setup is deliberately changed in the same task.

- [ ] **Step 2: Verify water quest completion**

After the third successful watering action, assert:

```csharp
QuestData waterQuest = FindQuest(QuestType.WaterCrops);
if (waterQuest == null || !waterQuest.IsCompleted || waterQuest.CurrentCount != waterQuest.TargetCount)
{
    Fail("Water quest was not completed after 3 watering actions.");
}
```

Expected coins after Water quest completion should include the configured `RewardCoins`.

- [ ] **Step 3: Verify harvest partial progress**

After one harvest, assert:

```csharp
QuestData harvestQuest = FindQuest(QuestType.HarvestCrops);
if (harvestQuest == null || harvestQuest.CurrentCount != 1 || harvestQuest.IsCompleted)
{
    Fail("Harvest quest should persist partial progress 1/2 after one harvest.");
}
```

- [ ] **Step 4: Verify pet partial progress**

After one pet, assert:

```csharp
QuestData petQuest = FindQuest(QuestType.PetAnimal);
if (petQuest == null || petQuest.CurrentCount != 1 || petQuest.IsCompleted)
{
    Fail("Pet quest should persist partial progress 1/5 after one pet.");
}
```

- [ ] **Step 5: Verify sticker persisted data**

After placing one sticker, assert:

```csharp
StickerPlacedData sticker = _memoryService.PlacedStickers[0];
if (sticker.PageIndex != 0 || Mathf.Abs(sticker.PositionX - 24f) > 0.01f || Mathf.Abs(sticker.PositionY + 18f) > 0.01f)
{
    Fail("Sticker placement data did not persist the expected page position.");
}

if (Mathf.Abs(sticker.Scale - 1.0f) > 0.001f)
{
    Fail($"Sticker scale should persist as 1.0, got {sticker.Scale:0.###}.");
}
```

- [ ] **Step 6: Add helper methods**

Add:

```csharp
private static QuestData FindQuest(QuestType type)
{
    if (_questService == null) return null;
    foreach (QuestData quest in _questService.ActiveQuests)
    {
        if (quest.Type == type)
        {
            return quest;
        }
    }

    return null;
}
```

---

## Task 5: Verify PlayerPrefs Persistence With Fresh Services

**Files:**
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimMcpGameplayLoopValidation.cs`

- [ ] **Step 1: Add persistence verification step before restore**

Insert a step before `Restore test save`:

```csharp
new Step("Verify PlayerPrefs persistence with fresh services", VerifyFreshServicePersistence),
new Step("Restore test save", RestoreTestSave)
```

- [ ] **Step 2: Implement fresh service verification**

Create a new `SaveService`, `InventoryService`, `MemoryService`, and `QuestService` so the test proves data was loaded from PlayerPrefs:

```csharp
private static bool VerifyFreshServicePersistence()
{
    if (HasErrors()) return true;

    var freshSave = new CozyLifeSim.UI.Services.SaveService();
    var freshInventory = new CozyLifeSim.UI.Services.InventoryService(freshSave);
    var freshMemory = new CozyLifeSim.UI.Services.MemoryService(freshSave);
    var freshQuest = new CozyLifeSim.UI.Services.QuestService(freshSave, freshInventory, GetQuestDatabase(), false);

    if (freshInventory.Seeds != 4)
    {
        Fail($"Fresh save should restore 4 seeds, got {freshInventory.Seeds}.");
    }

    if (freshInventory.Crops != 1)
    {
        Fail($"Fresh save should restore 1 crop, got {freshInventory.Crops}.");
    }

    if (freshMemory.PlacedStickers.Count != 1)
    {
        Fail($"Fresh save should restore 1 placed sticker, got {freshMemory.PlacedStickers.Count}.");
    }

    QuestData waterQuest = FindQuest(freshQuest, QuestType.WaterCrops);
    QuestData harvestQuest = FindQuest(freshQuest, QuestType.HarvestCrops);
    QuestData petQuest = FindQuest(freshQuest, QuestType.PetAnimal);

    if (waterQuest == null || !waterQuest.IsCompleted)
    {
        Fail("Fresh quest service should restore completed Water quest.");
    }

    if (harvestQuest == null || harvestQuest.CurrentCount != 1 || harvestQuest.IsCompleted)
    {
        Fail("Fresh quest service should restore Harvest quest partial progress 1/2.");
    }

    if (petQuest == null || petQuest.CurrentCount != 1 || petQuest.IsCompleted)
    {
        Fail("Fresh quest service should restore Pet quest partial progress 1/5.");
    }

    return true;
}
```

Add overload:

```csharp
private static QuestData FindQuest(IQuestService service, QuestType type)
```

- [ ] **Step 3: Load the real scene QuestDatabase**

Add helper:

```csharp
private static CozyLifeSim.UI.Settings.QuestDatabase GetQuestDatabase()
{
    GameLifetimeScope scope = LifetimeScope.Find<GameLifetimeScope>();
    if (scope == null) return null;

    SerializedObject so = new SerializedObject(scope);
    SerializedProperty property = so.FindProperty("_questDatabase");
    return property == null ? null : property.objectReferenceValue as CozyLifeSim.UI.Settings.QuestDatabase;
}
```

Use `logFallbackWarning: false` only in validation-created fresh services so expected fallback does not create warning noise.

---

## Task 6: Apply Targeted Runtime/UI Fixes Only If Needed

**Files:** modify only the specific runtime/UI file that fails validation.

- [ ] **Step 1: Farm fixes, if needed**

Touch `CropWidget.cs` only for issues such as:
- button active/interactable state does not match `_state`;
- `CompleteWatering()` can leave `_isWatering=true`;
- watering can remains active after sequence kill;
- quest progress fires more than once per successful watering.

- [ ] **Step 2: Animal fixes, if needed**

Touch `AnimalWidget.cs` only for issues such as:
- `_isPetting` can remain true after a killed sequence;
- heart feedback does not spawn under `_spawnRoot`;
- destroyed heart sequences remain in `_heartSequences`;
- repeat click breaks reward or feedback state.

- [ ] **Step 3: Sticker fixes, if needed**

Touch `StickerBook.cs` / `CozySticker.cs` only for issues such as:
- dynamic inventory stickers fail to spawn from database;
- restored stickers duplicate save entries;
- persisted scale is not `1.0f`;
- invalid drops no longer snap back correctly.

- [ ] **Step 4: Quest HUD fixes, if needed**

Touch `QuestHudWidget.cs` only for issues such as:
- text does not refresh after progress/completion;
- completed state is unreadable;
- raycast targets block sticker drag/drop.

Do not perform broad polish unrelated to the failing assertion.

---

## Task 7: Verification And Handoff

**Files:**
- Modify: `docs/plans/task.md`
- Modify: `docs/plans/current-handoff.md`
- Modify: `docs/plans/index.md`

- [ ] **Step 1: Run Unity MCP verification**

Required sequence:

1. `CozySceneSetupWindow.GenerateSceneSilent`
2. `CozyLifeSimValidation.RunTests`
3. `CozyLifeSimSceneGameplayValidation.RunValidation`
4. Enter Play Mode
5. `CozyLifeSimMcpGameplayLoopValidation.RunGameplayLoopValidation`
6. Poll console until runtime loop summary appears
7. Check console unexpected errors
8. Exit Play Mode

Expected final state:
- Logic validation passes.
- Scene validation passes.
- MCP runtime loop validation passes.
- Console has `0` unexpected errors.
- Expected warnings, if any, are explicitly logged as `EXPECTED WARNING`.
- Original save is restored after runtime validation.

- [ ] **Step 2: Update task tracker after implementation**

When implementation is complete and verified, update `docs/plans/task.md`:

```markdown
| **Task 22: Runtime Loop Polish & Validation** | [x] | Completed accelerated MCP runtime loop validation with save/load persistence, clean validation logs, and targeted UI/runtime fixes. |
```

If only the plan is written and implementation has not started, keep Task 22 as `[ ]`.

- [ ] **Step 3: Update current handoff**

After implementation, update `docs/plans/current-handoff.md` with:
- Task 22 completed scope;
- latest commit if committed;
- exact Unity MCP verification results;
- expected warnings;
- remaining recommended next task.

- [ ] **Step 4: Commit only when requested**

Do not commit automatically. Commit only after the user explicitly asks.

---

## Acceptance Criteria

Task 22 is complete only when all of the following are true:

- `Tools/CozySim/Setup Test Scene Silent` succeeds.
- `Tools/CozySim/Run Logic Verification Tests` passes.
- `Tools/CozySim/Run Scene Gameplay Loop Validation` passes.
- `Tools/CozySim/Run MCP Gameplay Loop Validation` passes in Play Mode.
- Runtime validation confirms:
  - planting consumes 1 seed;
  - 3 watering actions complete the Water quest;
  - 1 harvest adds crop/coins and persists Harvest quest partial progress `1/2`;
  - 1 pet adds coins and persists Pet quest partial progress `1/5`;
  - 1 sticker placement persists with `Scale=1.0f`;
  - fresh services loaded from PlayerPrefs see the same inventory, quest, and sticker state;
  - the original save backup is restored at the end.
- New validation logs are ASCII-safe and easy to scan.
- Console has `0` unexpected errors after the full verification sequence.
