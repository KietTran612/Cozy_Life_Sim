# Implementation Plan - Wire Vietnamese Heritage UI Assets

## Goal Description

We have generated and imported all 54 Vietnamese Heritage and UI assets into `Assets/CozyLifeSim/Textures/Heritage/`. Currently, the UI panels (such as the Shop Popup, Quest Popup, Dialogue Popup, and Sidebar) use solid colors or basic placeholder textures. 

This plan details how we will wire these newly generated UI assets to their respective UI components programmatically in [CozySceneSetupWindow.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs). Because all assets are 1024x1024, we will also configure 9-slice borders (sprite borders) and max texture size restrictions on the UI frames to prevent stretching and optimize VRAM usage. We will also implement a robust, two-pass programmatic auto-trimming mechanism for single sprites while disabling it for sliced frame/panel sprites.

---

## User Review Required

> [!IMPORTANT]
> **Memory Optimization & `isReadable` Enforcement:**
> - To prevent VRAM bloat, **`isReadable` must be set to `false`** for all 54 assets after import/trimming.
> - The verification suite will contain an automated check to assert `isReadable == false` for all 54 `.meta` files.
>
> **Auto-Trimming Policy & Safe 9-Slicing:**
> - **Trim Policy Map (covers all 54 assets):**
>   - **`autoTrim = false` (No Trim)**:
>     - **Sliced UI Panels & Frames**: `UI_Panel_Frame_Wood.png`, `UI_Dialogue_Bubble.png`, `UI_Scrapbook_Notebook_Open.png`, `UI_Tab_Button_Bg.png`, `UI_Quest_Item_Bg.png`, `Scrapbook_StickyNote_Yellow.png`, `UI_Banner_LevelUp.png`. (Transparency margins are functional elements for alignment and slicing).
>     - **Full-page Scrapbook Backgrounds**: `Scrapbook_Bg_GridPaper.png`, `Scrapbook_Bg_OldPaper.png`, `Scrapbook_Bg_PastelPink.png`.
>     - **Scrapbook Cover**: `UI_Scrapbook_Cover.png`.
>   - **`autoTrim = true` (Trim to Alpha Bounds)**:
>     - **Crops**: `Crop_*` (Lotus/Rice/Sugarcane seeds, sprouts, mature stages - 9 assets).
>     - **Animals**: `Animal_*` (CalicoCat, WaterBuffalo - 2 assets).
>     - **Stickers**: `Sticker_*` (BanhMiCart, ConicalHat, Cyclo, StarLantern, SugarcaneJuice - 5 assets).
>     - **NPC Portraits**: `NPC_Portrait_*` (CoBa, Grandma - 2 assets).
>     - **World Interactables & Characters**: `World_*` (World_Animal_Pen, World_Quest_Board, World_Shop_Stall, World_Soil_Plot, World_Character_CoBa_Idle, World_Character_Grandma_Idle - 6 assets).
>     - **UI Icons, Buttons, Stamps, Effects & Indicators**: `UI_Button_*` (Close_X, Page_Arrow - 2 assets), `UI_Icon_*` (Autosave, Quest, QuestType_Harvest, QuestType_Pet, QuestType_Water, Shop, Status_Water, Trash_Can - 8 assets), `System_Icon_*` (Crops, Level_Star, Lock, Seeds - 4 assets), `System_Coin_Vietnamese.png` (1 asset), `Effect_*` (Sparkle, Water_Droplets - 2 assets), `UI_Quest_Stamp_Completed.png` (1 asset), `UI_Dialogue_Indicator.png` (1 asset) (19 assets total).
> - In code, clamp border values dynamically to ensure `border.x + border.z < rect.width` and `border.y + border.w < rect.height`. If this constraint is violated (indicating a configuration or asset dimensions error), we will **log an Editor warning and trigger a scene validation failure** instead of silently ignoring it. The validation suite will independently recompute expected borders based on the `spriteRect` and fail if any sliced asset ends up with a zero border.
>
> **Stable Two-Pass Reimport Flow:**
> - To read pixels for auto-trimming, the importer must run a strict two-pass flow:
>   1. Set `isReadable = true` and `textureCompression = Uncompressed`, then call `SaveAndReimport()`.
>   2. Load the `Texture2D`, scan pixels to find the bounding box `spriteRect`.
>   3. Apply `spriteRect`, `spriteBorder`, the designated `maxTextureSize`, and **set `isReadable = false`** with original compression, then call `SaveAndReimport()`.
>
> **Max Size Policies by Asset Class:**
> - **1024 (UI Panels & Large Frames):** `UI_Panel_Frame_Wood.png`, `UI_Dialogue_Bubble.png`, `UI_Scrapbook_Notebook_Open.png`, `UI_Scrapbook_Cover.png`.
> - **512 (Characters, Stickers, Animals, Large UI items & Scrapbook Backgrounds):** `Scrapbook_Bg_*`, `World_Shop_Stall.png`, `World_Quest_Board.png`, `World_Character_*`, `World_Soil_Plot.png`, `World_Animal_Pen.png`, `Animal_*`, `Sticker_*`, `NPC_Portrait_*`, `UI_Quest_Item_Bg.png`, `Scrapbook_StickyNote_Yellow.png`, `UI_Banner_LevelUp.png`.
> - **256 (Crops, Buttons, Small Icons, HUD, Effects, Stamps):** `Crop_*`, `UI_Button_*`, `UI_Icon_*`, `System_Icon_*`, `System_Coin_Vietnamese.png`, `Effect_*`, `UI_Quest_Stamp_Completed.png`, `UI_Dialogue_Indicator.png`, `UI_Tab_Button_Bg.png`, `UI_Icon_Status_Water.png`.
> - The validation suite will programmatically check that each asset's importer settings match these policies.
>
> **Runtime Component Refactoring:**
> - `CozyDialoguePopup`: Add serialized image `_dialogueIndicator` field and blinking animation.
> - `QuestHudWidget` & `QuestPopup`: Both widgets will be updated to replace their plain `TextMeshProUGUI` templates with a custom `CozyQuestItemWidget` prefab containing background, type icon, and completed stamp.
> - `CropWidget`: Add `_waterStatusIcon` Image to show watered state.
> - `InventoryHudWidget`: Add `_autosaveIcon` Image to show save state.
> - `ProgressionHudWidget`: Add `_levelStarImage` to show star background.
> - `CozySceneSetupWindow`: Setup agricultural overlays, dialogue bubble backdrops, icons, etc.
>
> **Verification Suite & Idempotency:**
> - Update `CozyLifeSimSceneGameplayValidation.cs` with strict asset wiring assertions.
> - Check meta file changes on first scene setup, and verify **exactly 0 new diffs** on the second run (for scene, code, and `.meta` files).
> - We will compare the actual content diff (e.g. `git diff`) and write output files to an ignored temporary path (`C:\tmp\cozy-life-sim-diff-pass1.txt`) to avoid dirtying the git worktree with untracked diff files.

---

## Proposed Changes

### 1. Editor Tools & Asset Importer

#### [MODIFY] [CozyAssetImporterUtility.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozyAssetImporterUtility.cs)
- Implement `ConfigureAsSpriteWithBorder(string assetPath, Vector4 border, int maxTextureSize = 1024, bool autoTrim = true)` using the stable two-pass flow:
  - **Pass 1:** Enable read/write and set compression to uncompressed. Reimport.
  - Find bounding box of alpha > 0 pixels if `autoTrim` is true. Let `spriteRect` be the trimmed rect.
  - If `autoTrim` is false or no pixels found, let `spriteRect` be full size.
  - Clamp borders:
    ```csharp
    float clampX = border.x + border.z;
    float clampY = border.y + border.w;
    Vector4 safeBorder = border;
    if (clampX >= spriteRect.width || clampY >= spriteRect.height)
    {
        Debug.LogWarning($"[CozyAssetImporter] Invalid border {border} for sprite '{assetPath}' (rect size: {spriteRect.width}x{spriteRect.height}). Border disabled to prevent broken sliced meshes.");
        safeBorder = Vector4.zero;
    }
    ```
  - **Pass 2:** Set `isReadable = false`, apply `spriteRect`, `safeBorder`, `maxTextureSize`, and restore original compression settings. Reimport.

### 2. Runtime UI Components

#### [NEW] [CozyQuestItemWidget.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/CozyQuestItemWidget.cs)
- A simple component containing:
  ```csharp
  [SerializeField] private Image _bgImage;
  [SerializeField] private Image _typeIcon;
  [SerializeField] private Image _stampOverlay;
  [SerializeField] private TextMeshProUGUI _questText;
  ```
  and a `Setup(QuestData quest, Sprite bg, Sprite typeIcon, Sprite stamp)` method.

#### [MODIFY] [QuestHudWidget.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/QuestHudWidget.cs)
- Expose serialized sprite references for: `_itemBgSprite`, `_questWaterIcon`, `_questHarvestIcon`, `_questPetIcon`, and `_questCompletedStamp`.
- Replace `TextMeshProUGUI _questItemTemplate` with `CozyQuestItemWidget _questItemTemplate`.
- Instantiate and configure row widgets inside `RefreshQuests()` using the appropriate sprites.

#### [MODIFY] [QuestPopup.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/QuestPopup.cs)
- Expose serialized sprite references for: `_itemBgSprite`, `_questWaterIcon`, `_questHarvestIcon`, `_questPetIcon`, and `_questCompletedStamp`.
- Replace `TextMeshProUGUI _questItemTemplate` with `CozyQuestItemWidget _questItemTemplate`.
- Instantiate and configure row widgets inside `RefreshQuests()` using the appropriate sprites.

#### [MODIFY] [CozyDialoguePopup.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/CozyDialoguePopup.cs)
- Expose `[SerializeField] private Image _dialogueIndicator;`.
- Hide `_dialogueIndicator` when typewriter starts typing. Show it when typing completes or is skipped.
- Initialize it in `Start()` with a loop-animation (e.g. scale yoyo tween using DOTween).

#### [MODIFY] [CropWidget.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/CropWidget.cs)
- Expose `[SerializeField] private Image _waterStatusIcon;`.
- Update visibility in `UpdateVisualState()` depending on whether the crop is watered.

#### [MODIFY] [InventoryHudWidget.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/InventoryHudWidget.cs)
- Expose `[SerializeField] private Image _autosaveIcon;` and `[SerializeField] private Image _coinIcon;` / `_seedsIcon` / `_cropsIcon`.

#### [MODIFY] [ProgressionHudWidget.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/ProgressionHudWidget.cs)
- Expose `[SerializeField] private Image _levelStarImage;`.

### 3. Scene Setup Generator

#### [MODIFY] [CozySceneSetupWindow.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs)
- Load and configure all 54 Heritage and UI sprites from `Assets/CozyLifeSim/Textures/Heritage/` on startup by calling `CozyAssetImporterUtility.ConfigureAsSpriteWithBorder` with proper per-sprite borders, autoTrim settings, and optimized max texture sizes (1024/512/256) per the policy map. This ensures both UI and non-UI sprites are configured correctly before scene wiring.
- **Popups (Shop, Quest, Dialogue, Diary Input)**:
  - Set content panel background image to `UI_Panel_Frame_Wood.png` (Sliced, White color).
  - Set close button image to `UI_Button_Close_X.png` (Max size 256), remove text overlay.
- **Sidebar**:
  - Set background panel to `UI_Panel_Frame_Wood.png` (Sliced).
  - Set Tab Button backgrounds to `UI_Tab_Button_Bg.png` (Sliced) and assign corresponding icons (`UI_Icon_Quest.png`, `UI_Icon_Shop.png`, `UI_Scrapbook_Cover.png`).
- **Scrapbook / StickerBook**:
  - Set background to `UI_Scrapbook_Notebook_Open.png` (Sliced).
  - Set next/prev arrow buttons to `UI_Button_Page_Arrow.png` (Max size 256).
  - Populate `_backgroundStyles` array on page0/page1.
  - Set diary note template image to `Scrapbook_StickyNote_Yellow.png` (Sliced, border 64) and trash can icon to `UI_Icon_Trash_Can.png`.
- **Dialogue UI**:
  - Set bubble background image to `UI_Dialogue_Bubble.png` (Sliced).
  - Set blinking wait indicator image to `UI_Dialogue_Indicator.png` (Max size 256).
- **HUD & Icons**:
  - Set HUD coin display frame to `System_Coin_Vietnamese.png`.
  - Set HUD seeds frame to `System_Icon_Seeds.png`.
  - Set HUD crops frame to `System_Icon_Crops.png`.
  - Set HUD star image to `System_Icon_Level_Star.png`.
  - Set HUD autosave image to `UI_Icon_Autosave.png`.
- **Farming Overlay**:
  - Setup a child `Water_Status_Icon` under `Farm_Plot` with image sprite `UI_Icon_Status_Water.png`. Set it as `_waterStatusIcon` on `CropWidget`.
- **Quest HUD Prefab Setup**:
  - Automatically instantiate `CozyQuestItemWidget` prefab row template inside `Quest_Popup` / `QuestHudWidget` and wire its background, type icons, and completed stamp.

### 4. Verification Suite

#### [MODIFY] [CozyLifeSimSceneGameplayValidation.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimSceneGameplayValidation.cs)
- Keep all existing CuteKawaii fallback check methods to ensure compatibility.
- Add `ValidateUISpriteAssignments(List<string> errors, List<string> passes)` to verify all new UI Image objects in the hierarchy are assigned the correct trimmed/sliced sprites.
- Add `ValidateTextureImporterMemorySettings(List<string> errors, List<string> passes)`:
  - Assert that `isReadable == false` for all 54 assets.
  - Assert that `maxTextureSize` matches the policy map (1024, 512, or 256) for each asset category.
  - Assert that sliced assets have a non-zero `spriteBorder` matching their configuration.
  - Assert that non-trimmed assets (where `autoTrim` is false per the policy map) have `spriteRect` dimensions equal to `(0, 0, 1024, 1024)`.
  - Assert that trimmed assets (where `autoTrim` is true per the policy map) have `spriteRect` exactly equal to their computed alpha bounding box and within the full `(0, 0, 1024, 1024)` bounds.
  - Assert that no sliced UI asset has a zero border when it is expected to have one (independent recalculation to catch clamp check failures).

---

## Verification Plan

### Automated Tests
- Run `Tools/CozySim/Run Scene Gameplay Loop Validation`.
- Run `Tools/CozySim/Run Logic Verification Tests`.
- Capture first setup git status content diff: `git diff > C:\tmp\cozy-life-sim-diff-pass1.txt`
- Run Rebuild Scene again -> Capture second setup git status content diff: `git diff > C:\tmp\cozy-life-sim-diff-pass2.txt`
- Compare both diff content files: `Compare-Object (Get-Content C:\tmp\cozy-life-sim-diff-pass1.txt) (Get-Content C:\tmp\cozy-life-sim-diff-pass2.txt)` -> Assert they are **100% identical** (meaning the second run introduced exactly 0 new changes to scene, code, or `.meta` files).

### Manual Verification
- Play game in Editor.
- Verify dialogue popups type text and show/blink the golden lotus bud indicator.
- Verify quest item widget displays cozy row backgrounds, type icons, and stamp overlays when completed.
