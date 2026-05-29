# Inventory & Reward Progression Loop Implementation Plan

> **For Antigravity:** REQUIRED WORKFLOW: Use `.agent/workflows/execute-plan.md` to execute this plan in single-flow mode.

**Goal:** Create a sustainable and engaging core gameplay loop by implementing a data-driven Tiem Tap Hoa (Shop) popup, refactoring the sidebar into a compact navigation dock, building a reusable click-blocking Cozy Popup system, filtering StickerBook tray by ownership, adding a premium sticker to bootstrap database data, and adding automated Play Mode integration tests.

**Architecture:** Use VContainer for DI, DOTween for premium elastic UI transitions, and an event-driven MVP presenter pattern. We'll build a reusable `CozyPopup` base class to safely manage canvas-dim raycast blocking and scale transitions, and integrate in-world interactive clicks via OnMouseDown with EventSystem UI checks.

**Tech Stack:** Unity 6000.3.11f1, VContainer, DOTween, UniTask, TextMeshPro.

---

## Technical Tasks

### Task 1: Data Model Expansion, Save Normalization & Database Validations
Add prices and unlocked sticker status to core structs/classes, normalize save data safely, update database validation methods, update database editor windows, and bootstrap data helpers.

**Files:**
- Modify: [SaveData.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Core/SaveData.cs)
- Modify: [CropTemplate.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Settings/CropTemplate.cs)
- Modify: [StickerTemplate.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Settings/StickerTemplate.cs)
- Modify: [SaveService.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Services/SaveService.cs:37-52)
- Modify: [CropDatabase.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Settings/CropDatabase.cs:17-64)
- Modify: [StickerDatabase.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Settings/StickerDatabase.cs:17-64)
- Modify: [CropEditorWindow.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CropEditorWindow.cs)
- Modify: [StickerEditorWindow.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/StickerEditorWindow.cs)
- Modify: [StickerDatabaseUtility.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/StickerDatabaseUtility.cs:54-90)
- Test: [CozyLifeSimValidation.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimValidation.cs)

**Step 1: Write failing test in CozyLifeSimValidation.cs**
Assert that crops and stickers have non-zero prices, and `SaveData` has `UnlockedStickerIds` containing default values.
```csharp
// Inside RunTests()
var saveData = new SaveData();
if (saveData.UnlockedStickerIds == null || saveData.UnlockedStickerIds.Count == 0) throw new System.Exception("UnlockedStickerIds is null or empty");
```

**Step 2: Run test to verify it fails**
Run logic validation in Editor. Expect compile/run failure because the fields don't exist yet.

**Step 3: Write minimal implementation**
- In [SaveData.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Core/SaveData.cs):
  ```csharp
  public List<int> UnlockedStickerIds = new List<int> { 1, 2 }; // Default stickers 1 and 2 unlocked
  ```
- In [CropTemplate.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Settings/CropTemplate.cs):
  ```csharp
  public int BuyPrice = 5;
  public int SellPrice = 15;
  ```
- In [StickerTemplate.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Settings/StickerTemplate.cs):
  ```csharp
  public int BuyPrice = 50;
  ```
- In [SaveService.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Services/SaveService.cs) inside `NormalizeSaveData()`:
  ```csharp
  if (ActiveSave.UnlockedStickerIds == null)
  {
      ActiveSave.UnlockedStickerIds = new List<int>();
  }
  if (!ActiveSave.UnlockedStickerIds.Contains(1)) ActiveSave.UnlockedStickerIds.Add(1);
  if (!ActiveSave.UnlockedStickerIds.Contains(2)) ActiveSave.UnlockedStickerIds.Add(2);
  ```
- In `StickerDatabaseUtility.cs`:
  Inside `BootstrapDefaultStickers(StickerDatabase database)`, explicitly check and ensure that IDs 1, 2, and 3 exist in the database Stickers list. If any ID is missing, add it dynamically (so that existing project databases on disk also get ID 3 bootstrapped correctly instead of being skipped!).
  Reuse the same safe fallback sprite lookup pattern for `chickenSprite` in case `Chicken-White-256.png` is not imported yet.
  ```csharp
  var bunnySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Animals/Bunny-Pink-256.png");
  var bearSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Animals/Bear-256.png");
  var chickenSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Animals/Chicken-White-256.png");

  // Fallback: If designated package sprites are null, auto-discover any available Sprite in the project
  if (bunnySprite == null || bearSprite == null || chickenSprite == null)
  {
      string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite");
      if (spriteGuids != null && spriteGuids.Length > 0)
      {
          var fallbackSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(spriteGuids[0]));
          if (bunnySprite == null) bunnySprite = fallbackSprite;
          if (bearSprite == null) bearSprite = fallbackSprite;
          if (chickenSprite == null) chickenSprite = fallbackSprite;
      }
  }

  if (!database.Stickers.Exists(x => x.StickerId == 1)) database.Stickers.Add(new StickerTemplate(1, "Bunny Pink", bunnySprite, bunnySprite));
  if (!database.Stickers.Exists(x => x.StickerId == 2)) database.Stickers.Add(new StickerTemplate(2, "Bear", bearSprite, bearSprite));
  if (!database.Stickers.Exists(x => x.StickerId == 3)) database.Stickers.Add(new StickerTemplate(3, "Chicken White", chickenSprite, chickenSprite));
  ```
- In `CropDatabase.cs`: Validate crop `BuyPrice > 0` and `SellPrice > 0`.
- In `StickerDatabase.cs`: Validate sticker `BuyPrice > 0`.
- In `CropEditorWindow.cs`:
  - When copying template objects to staging list, set:
    `var copy = new CropTemplate(...); copy.BuyPrice = crop.BuyPrice; copy.SellPrice = crop.SellPrice;`
  - In `ValidateStaging()` check `BuyPrice > 0` and `SellPrice > 0`.
  - In the Details GUI, add input fields for `BuyPrice` and `SellPrice` and set dirty on modifications.
  - On save, copy staging prices back into the serialized output:
    `new CropTemplate(c.CropId, c.Name, c.StageDurationSeconds, c.SeedSprite, c.SproutSprite, c.MatureSprite, c.HarvestSprite) { BuyPrice = c.BuyPrice, SellPrice = c.SellPrice }`
- In `StickerEditorWindow.cs`:
  - When copying to staging list, set:
    `var copy = new StickerTemplate(...); copy.BuyPrice = sticker.BuyPrice;`
  - In `ValidateStaging()` check `BuyPrice > 0`.
  - In the Details GUI, add input fields for `BuyPrice` and `SellPrice` (or just BuyPrice for stickers) and set dirty on modifications.
  - On save, copy staging prices back into the serialized output:
    `new StickerTemplate(s.StickerId, s.Name, s.Sprite, s.ShadowSprite) { BuyPrice = s.BuyPrice }`

**Step 4: Run test to verify it passes**
Run logic validation in Editor. Expect PASS.

**Step 5: File Verification Gate**
Wait for Unity compile/import and ensure all `.meta` files exist. **Do not commit** unless explicitly requested by the user.

---

### Task 2: Implement IShopService & ShopService (TDD Flow, Transaction Safety)
Create transaction business logic with strict balance validations to prevent partial mutations. Gracefully aborts and returns false if templates or database databases are missing.

**Files:**
- Create: `Assets/CozyLifeSim/Scripts/Core/IShopService.cs`
- Create: `Assets/CozyLifeSim/Scripts/UI/Services/ShopService.cs`
- Create: `Assets/CozyLifeSim/Scripts/UI/Presenters/ShopPresenter.cs`
- Modify: [GameLifetimeScope.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/GameLifetimeScope.cs)
- Test: [CozyLifeSimValidation.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimValidation.cs)

**Step 1: Write transaction validation test in CozyLifeSimValidation.cs**
```csharp
// Inside RunTests(), test IShopService functionality using a mock ShopService:
// 1. TryBuySeed(1) with 100 coins -> Expect True. Coins = 95, Seeds = 6.
// 2. TryBuySeed(999) (invalid ID) -> Expect False (clean abort, return false, no throw).
// 3. TryBuySticker(1) (already unlocked) -> Expect False (returns false, no coin deduction).
// 4. TryBuySticker(3) with sufficient coins -> Expect True. Coins = 45 (reduced by 50), ID 3 added once to UnlockedStickerIds.
// 5. Repeated TryBuySticker(3) -> Expect False. Coins remain exactly 45.
// 6. Insufficient Coins validation: Set coins to 10. Call TryBuySticker(3) -> Expect False. Coins must remain exactly 10.
// 7. Insufficient Crops validation: Set crops to 0. Call TrySellCrop(1) -> Expect False. Balances remain unchanged.
```

**Step 2: Run test to verify it fails**
Run logic validation. Expect compilation failure because `IShopService` doesn't exist yet.

**Step 3: Write minimal implementation**
- Create `IShopService.cs`.
- Create `ShopService.cs` (performs verification of balances, deducts coins, adds seeds/crops, adds sticker IDs to `UnlockedStickerIds`, and calls `Save()`. Returns false cleanly if cropId or stickerId template is null, if the sticker is already unlocked, or if coins are insufficient).
- Create `ShopPresenter.cs` (injects `IShopService` and `IInventoryService`).
- In [GameLifetimeScope.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/GameLifetimeScope.cs), register standard singleton:
  ```csharp
  builder.Register<IShopService, ShopService>(Lifetime.Singleton);
  builder.Register<ShopPresenter>(Lifetime.Singleton);
  ```

**Step 4: Run test to verify it passes**
Run logic validation in Editor. Expect PASS.

**Step 5: File Verification Gate**
Wait for Unity compilation. Ensure no syntax errors are reported. Do not commit unless requested.

---

### Task 3: Build Reusable Cozy Popup Base System (`CozyPopup.cs` with Tween Reentrancy)
Implement the base modal popup supporting elastic DOTween animations, click-blocking dark dimming overlay, and reentrancy tween safety.

**Files:**
- Create: `Assets/CozyLifeSim/Scripts/UI/CozyPopup.cs`

**Step 1: Create CozyPopup.cs**
- Implement standard `Open()` and `Close()` methods. Open fades backgroundDim overlay from 0 to 1 (duration `0.2s`) and scales the main content panel from `0.8f` to `1.0f` using `Ease.OutBack` (`0.3s`). Close runs the reverse animations, then deactivates the GameObject. Add virtual hooks `OnOpen()` and `OnClose()`.
- Note: The dim backing is a CanvasGroup component attached to a child GameObject that has a full-screen Canvas `Image` component (`Color = new Color(0, 0, 0, 0.4f)` and `Raycast Target = true` to block clicks).
- **Tween Safety & Reentrancy:**
  - Store private fields: `private Tween _fadeTween;` and `private Tween _scaleTween;`.
  - At the start of `Open()` and `Close()`, immediately call `_fadeTween?.Kill();` and `_scaleTween?.Kill();` to cancel any active transitions.
  - Set `_isOpen` consistently at the start of `Open()` and `Close()`.
  - In `OnDestroy()`, call `_fadeTween?.Kill();`, `_scaleTween?.Kill();`, and unsubscribe `_closeButton` click listener.

**Step 2: Verification**
Wait for Unity compilation. Ensure no syntax errors are reported.

---

### Task 4: Refactor Sidebar Navigation Dock & StickerBook Tray
Refactor `CozySidebar` to be a simple vertical dock of buttons, and filter `StickerBook` dynamically by ownership.

**Files:**
- Modify: [CozySidebar.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/CozySidebar.cs)
- Modify: [StickerBook.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/StickerBook.cs)
- Create: `Assets/CozyLifeSim/Scripts/UI/QuestPopup.cs`

**Step 1: Refactor CozySidebar.cs**
Remove sliding panel panels and index pages. Maintain only buttons and click events triggering popups.

**Step 2: Refactor StickerBook.cs**
- Inject `IShopService` in `Construct()`.
- Inside `SpawnDynamicStickers()`, only spawn a sticker from the database if `shopService.IsStickerUnlocked(sticker.StickerId)` is true.
- In `Start()`, subscribe to `shopService.OnShopTransactionSuccess` to trigger a re-spawn/refresh of the sticker tray. Unsubscribe on `OnDestroy()`.

**Step 3: Create QuestPopup.cs**
Inherits from `CozyPopup`. Houses the dynamic Quest elements originally generated in `QuestHudWidget`. Communicates directly with `IQuestService` (no QuestPresenter class needed, matching design diagram simplification).

---

### Task 5: Implement ShopPopup UI
Create the data-driven Buy Seeds, Buy Premium Stickers, and Sell Crops interface.

**Files:**
- Create: `Assets/CozyLifeSim/Scripts/UI/ShopPopup.cs`
- Create: `Assets/CozyLifeSim/Scripts/UI/ShopItemWidget.cs` (Reusable component card for products)

**Step 1: Create ShopPopup.cs & ShopItemWidget.cs**
Inherits `CozyPopup`. Direct inject of `ShopPresenter`, `CropDatabase`, `StickerDatabase`, and `IInventoryService` to query values, while all transactional mutator calls go through `ShopPresenter`.
Features grid scroll lists for Seeds, Stickers, and Crops.
Bind grid items to `ShopPresenter` transaction actions. Subscribe to `IInventoryService` value changes to dynamically update prices, current inventory balances, and button interactable state.

**Step 2: Verification**
Verify script compile integrity. Do not commit unless requested.

---

### Task 6: Implement In-World Clicking (Interactive Objects)
Add physical collider touch handlers to scene elements.

**Files:**
- Create: `Assets/CozyLifeSim/Scripts/UI/CozyInteractiveObject.cs`

**Step 1: Implement CozyInteractiveObject.cs**
Detects in-world clicks using standard Unity `OnMouseDown()` message (requires `BoxCollider2D` on the in-world GameObject). When clicked, it calls `Open()` on its designated popup. To prevent clicks from leaking to world objects while a popup is active, `CozyInteractiveObject` incorporates an `EventSystem.current.IsPointerOverGameObject()` guard which immediately ignores mouse down clicks if the mouse pointer is hovering over any UI Graphic (like the dim blocker popup).

---

### Task 7: Update CozySceneSetupWindow & Update Scene Gameplay Validation
Update the editor setup script to generate the Navigation Dock, QuestPopup, ShopPopup, wire up interactive objects, and validate the UI changes.

**Files:**
- Modify: [CozySceneSetupWindow.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs)
- Modify: [CozyLifeSimSceneGameplayValidation.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimSceneGameplayValidation.cs)

**Step 1: Update CozySceneSetupWindow.cs**
- Simplify `Sidebar_Panel` into a clean vertical button tray.
- Generate `Quest_Popup` canvas popup with `QuestPopup` script and dim image blocker.
- Generate `Shop_Popup` canvas popup with `ShopPopup` script and dim image blocker.
- Setup `BoxCollider2D` and `CozyInteractiveObject` on the in-world Quest Board and Shop Stall.
- Serialized reference injection in GameLifetimeScope.

**Step 2: Update CozyLifeSimSceneGameplayValidation.cs**
Add a new method `ValidatePopupAndNavigationDocks()` called inside `RunValidation()`.
Assert:
- An `EventSystem` component exists in the scene.
- The `Canvas` has a `GraphicRaycaster` component (enabling the dim blocker to participate in UI raycasts and the OnMouseDown guard to work).
- The `Sidebar_Panel` exists and contains button triggers.
- The `Quest_Popup` and `Shop_Popup` contain `QuestPopup` and `ShopPopup` scripts, have valid full-screen dim block components with an `Image` component having `Image.raycastTarget == true`, a `CanvasGroup` having `CanvasGroup.blocksRaycasts == true`, content panels, and close buttons.
- In-world Quest Board and Shop Stall have `CozyInteractiveObject` components with `_targetPopup` field assigned (verified via reflection/SerializedObject checking that the target points to expected `QuestPopup` / `ShopPopup` component) and `BoxCollider2D` colliders.

**Step 3: Run scene generator in Editor and Validate**
Run: `CozySceneSetupWindow.GenerateSceneSilent`. Verify hierarchy structure compiles cleanly and creates expected GameObjects.
Run: `CozyLifeSimSceneGameplayValidation.RunValidation`. Verify PASS.

---

### Task 8: Implement Play Mode Integration Tests & Loop Validation
Integrate automated gameplay verification into validation sweeps to enforce buy/sell loops and secure PlayerPrefs restoration.

**Files:**
- Modify: [CozyLifeSimMcpGameplayLoopValidation.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimMcpGameplayLoopValidation.cs)

**Step 1: Add new verification steps**
- Assert starting stats (100 coins, 5 seeds, 0 crops).
- Execute `shopService.TryBuySeed(1)`. Assert coins = 95, seeds = 6.
- Trigger planting, watering, and harvest. Assert coins = 105 (harvest reward gives +10 coins), seeds = 5, crops = 1.
- Execute `shopService.TrySellCrop(1)`. Assert coins = 120 (selling gives +15 coins), crops = 0.
- Execute `shopService.TryBuySticker(3)`. Assert coins = 70 (buying premium sticker ID 3 costs 50 coins), sticker ID 3 is now present in `UnlockedStickerIds`.
- Verify that only unlocked stickers (IDs 1, 2, and 3 after buy) are spawned in the StickerBook tray. Inspect the private list field `_spawnedInventoryStickers` in `StickerBook` using reflection:
  ```csharp
  FieldInfo field = typeof(StickerBook).GetField("_spawnedInventoryStickers", BindingFlags.Instance | BindingFlags.NonPublic);
  var list = field.GetValue(stickerBook) as System.Collections.IList;
  // Assert list count contains exactly 3 spawned templates
  ```
- Update `CopySave()` and `CloneSave()` to copy `UnlockedStickerIds` dynamically during save backup and restoration.
- At `RestoreSaveBackup()`, force rehydrate `UnlockedStickerIds` on active save and refresh the StickerBook tray so premium sticker ID 3 disappears from the UI tray, returning the UI state cleanly to the original save.

**Step 2: Run verification runner**
Enter Play Mode and run loop validation. Verify loop resolves to 100% clean PASS with zero errors, visual drift, or leak warnings.
