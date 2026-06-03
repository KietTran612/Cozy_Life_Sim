# Visual Layout Validator Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a complete visual layout validation pass and fix asset fitting so every image-backed UI/world element has a predictable size, aspect behavior, and screen placement.

**Architecture:** Keep existing heritage texture importer rules, but stop relying on asset pixel bounds for final display size. Add explicit visual size policies for UI `Image` components, popup imagery, gameplay panels, HUD/sidebar icons, scrapbook elements, and world `SpriteRenderer` objects, then validate those policies in `CozyLifeSimSceneGameplayValidation`.

**Tech Stack:** Unity 6000.3.11f1, C#, uGUI `Image`/`RectTransform`, `LayoutElement`, `SpriteRenderer`, Unity MCP screenshots, existing `CozySceneSetupWindow` partials and scene validation partials.

---

## Scope And Acceptance Criteria

- All places that use sprites/images are covered: HUD, sidebar, farm, animal pen, sticker book, inventory tray, scrapbook/diary, quest HUD, quest popup, shop popup, dialogue popup, diary input popup, feedback toast, startup overlay, world objects, and runtime-spawned/dynamically-updated item widgets.
- `Image.Type.Simple` artwork/icons preserve aspect unless the element is a deliberate fill/progress graphic.
- `Image.Type.Sliced` frames/panels remain stretchable and are not forced to preserve aspect.
- `Image.Type.Filled` progress/fill graphics keep fill behavior and are not forced to preserve aspect.
- World `SpriteRenderer` objects fit target world bounds computed from `sprite.bounds`, not fixed `transform.localScale`.
- Scene validation catches measurable visual failures: offscreen key elements, wrong aspect policy, oversized/undersized key visuals, and severe overlap among main gameplay regions.
- Snapshot review is required for visual acceptance, but full validation suite is not required unless the user explicitly approves it.

## Files

- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.Helpers.cs`
  - Add helper methods for image aspect policy and world sprite fitting.
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.Gameplay.cs`
  - Apply explicit size/aspect rules for farm, animal, sticker book, inventory tray, diary note, and sticker template visuals.
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.Popups.cs`
  - Apply explicit size/aspect rules for quest, shop, dialogue, diary input, close buttons, popup item rows, and popup decorative images.
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.WorldAndUI.cs`
  - Apply explicit size/aspect rules for HUD, sidebar, progression, startup overlay, world NPC, quest board, and shop stall.
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimSceneGameplayValidation.UI.cs`
  - Add visual layout validation for UI images, popups, gameplay regions, HUD/sidebar, offscreen checks, and overlap checks.
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimSceneGameplayValidation.cs`
  - Call the new visual layout validation method from the existing scene validation route.
- Create: `Assets/CozyLifeSim/Scripts/Editor/CozyVisualSnapshotUtility.cs`
  - Add a targeted editor utility method for capturing Game view UI-overlay snapshots to `C:\tmp` when Unity MCP camera snapshots do not include Screen Space Overlay UI.
- Modify: `Assets/CozyLifeSim/Scripts/UI/CropWidget.cs`
  - Preserve aspect when runtime crop sprites are assigned.
- Modify: `Assets/CozyLifeSim/Scripts/UI/AnimalWidget.cs`
  - Preserve aspect when runtime animal and heart sprites are assigned.
- Modify: `Assets/CozyLifeSim/Scripts/UI/CozySticker.cs`
  - Preserve aspect when sticker main/shadow sprites are assigned dynamically.
- Modify: `Assets/CozyLifeSim/Scripts/UI/CozyQuestItemWidget.cs`
  - Apply aspect policy for row backgrounds, type icons, and completed stamps.
- Modify: `Assets/CozyLifeSim/Scripts/UI/ShopItemWidget.cs`
  - Preserve aspect when shop item icons are assigned.
- Modify: `Assets/CozyLifeSim/Scripts/UI/CozyDialoguePopup.cs`
  - Preserve aspect when dialogue portrait sprites are assigned.
- Modify: `Assets/CozyLifeSim/Scripts/UI/StickerBookPage.cs`
  - Keep scrapbook page backgrounds sliced/stretchable when background sprites are changed.
- Modify: `Assets/CozyLifeSim/Scripts/UI/CozyJuiceUtility.cs`
  - Keep coin fly template/simple images aspect-safe.

## Phase Breakdown

Task 41 must be implemented in small phases. Do not implement all clusters in one pass.

### Phase 41.1: Visual Validator Foundation

**Goal:** Add reusable fitting helpers, visual validator skeleton, cluster method structure, and non-invasive baseline checks.

**Touches:** `CozySceneSetupWindow.Helpers.cs`, `CozyLifeSimSceneGameplayValidation.cs`, `CozyLifeSimSceneGameplayValidation.UI.cs`.

**Exit criteria:**
- Scene validation compiles.
- Cluster methods exist and log/fail by cluster.
- No broad scene layout changes yet.

### Phase 41.2: Home Screen Visual Pass

**Goal:** Fix and validate the first visible screen before any popup work.

**Clusters:** Cluster 1 Home Screen Shell, Cluster 2 Home Gameplay Widgets, Cluster 3 Home World Objects And Camera.

**Touches:** `CozySceneSetupWindow.Gameplay.cs`, `CozySceneSetupWindow.WorldAndUI.cs`, scene visual validation methods for home clusters.

**Exit criteria:**
- Home shell, gameplay widgets, world objects, and camera checks pass.
- Unity MCP `screenshot_game` confirms world/camera framing is acceptable.
- No popup implementation changes are bundled into this phase.

### Phase 41.3: Popup Visual Pass

**Goal:** Fix and validate each popup independently after the home screen is stable.

**Clusters:** Cluster 4 Quest HUD And Quest Popup, Cluster 5 Shop Popup, Cluster 6 Dialogue Popup, Cluster 7 Diary Input Popup And Scrapbook/Diary Runtime Items.

**Touches:** `CozySceneSetupWindow.Popups.cs`, popup-related scene validation methods, and popup template setup.

**Exit criteria:**
- Quest, Shop, Dialogue, and Diary/Scrapbook visual checks pass independently.
- Popup snapshots or accepted visual notes are captured cluster by cluster.

### Phase 41.4: Runtime Dynamic Sprite Policy

**Goal:** Ensure runtime sprite assignments preserve the same visual policy as generated scene objects.

**Touches:** `CropWidget.cs`, `AnimalWidget.cs`, `CozySticker.cs`, `CozyQuestItemWidget.cs`, `ShopItemWidget.cs`, `CozyDialoguePopup.cs`, `StickerBookPage.cs`, `CozyJuiceUtility.cs`.

**Exit criteria:**
- Runtime assignments reapply correct `Image.type` and `preserveAspect`.
- No gameplay/service/presenter behavior changes.
- Scene validation and targeted runtime visual review still pass.

### Phase 41.5: Snapshot Utility, Final Aggregate, And Handoff

**Goal:** Add overlay snapshot route, run final aggregate visual validation, check idempotency, and update handoff.

**Touches:** `CozyVisualSnapshotUtility.cs`, `docs/plans/task.md`, `docs/plans/current-handoff.md`.

**Exit criteria:**
- Overlay-inclusive snapshot path works or any limitation is documented.
- Cluster 8 Final Aggregate Visual Pass succeeds.
- Scene setup idempotency succeeds.
- Full validation suite is not run unless the user explicitly approves it.

## Visual Size Policy

Use these initial target ranges unless visual review shows they need adjustment:

- World `NPC_BaNgoai`: target rendered height 3.1-3.8 world units, bottom placed below gameplay center, not covering farm/animal/book UI regions.
- World `Quest_Board`: target rendered height 1.2-1.8 world units.
- World `Shop_Stall`: target rendered height 1.4-2.0 world units.
- `Farm_Plot`: 300-380 px wide, 380-520 px high; crop visual 110-180 px.
- `Animal_Pen`: 300-380 px wide, 380-520 px high; animal visual 110-180 px; heart feedback 32-56 px.
- `StickerBook_Panel`: 420-560 px wide/high, sliced background, page controls 36-64 px.
- `Inventory_Tray`: tray cells 90-130 px, sticker visual 70-105 px, count text stays inside cell.
- HUD icons: 22-36 px, preserve aspect; XP bar is filled/simple rectangle and does not preserve aspect.
- Sidebar buttons: 48-72 px high; sidebar icons 28-44 px, preserve aspect.
- Popup content panels: sliced, not preserve aspect; close icons 28-48 px, preserve aspect.
- Quest row popup/HUD icons and stamps: 28-56 px, preserve aspect.
- Dialogue portrait/indicator/bubble imagery: portrait/indicator simple images preserve aspect; bubble panel is sliced.
- Diary note and scrapbook backgrounds: sliced/full panel rules per asset type; note text must remain inside note bounds.
- Runtime-spawned shop, quest, sticker, diary, crop, animal, and dialogue images must reapply the same aspect policy whenever their sprite changes.

## Visual Validation Test Matrix

Test visual layout by feature clusters. Do not jump straight to all popups at once. Complete and visually review each cluster before moving to the next cluster so failures stay local and easy to diagnose.

### Cluster 1: Home Screen Shell

**Purpose:** Validate the first screen composition before opening any popup.

**Objects:**
- `Canvas/UI_Root/Header_Panel`
- `Canvas/UI_Root/Header_Panel/Coins_Text/Coin_Icon`
- `Canvas/UI_Root/Header_Panel/Seeds_Text/Seeds_Icon`
- `Canvas/UI_Root/Header_Panel/Crops_Text/Crops_Icon`
- `Canvas/UI_Root/Header_Panel/Progression_HUD`
- `Canvas/UI_Root/Header_Panel/Progression_HUD/Level_Text/Level_Star_Icon`
- `Canvas/UI_Root/Header_Panel/Autosave_Icon`
- `Canvas/UI_Root/Gameplay_Area`
- `Canvas/UI_Root/Sidebar_Panel`
- `Canvas/UI_Root/Sidebar_Panel/Quest_Button`
- `Canvas/UI_Root/Sidebar_Panel/Quest_Button/Quest_Icon`
- `Canvas/UI_Root/Sidebar_Panel/Shop_Button`
- `Canvas/UI_Root/Sidebar_Panel/Shop_Button/Shop_Icon`

**Checks:**
- Header, sidebar, and gameplay area are inside the 1920x1080 reference canvas.
- HUD icons are 22-36 px and preserve aspect.
- Sidebar buttons are 48-72 px high, tab backgrounds are sliced, and icons are 28-44 px with preserve aspect.
- Header does not overlap gameplay area.
- Sidebar does not cover farm, animal, or sticker book panels.

**Exit criteria:** Home shell scene validation passes and snapshot review shows a stable first-screen frame.

### Cluster 2: Home Gameplay Widgets

**Purpose:** Validate the core widgets visible on the home screen.

**Objects:**
- `Canvas/UI_Root/Gameplay_Area/Farm_Plot`
- `Canvas/UI_Root/Gameplay_Area/Farm_Plot/Crop_Visual`
- `Canvas/UI_Root/Gameplay_Area/Farm_Plot/Watering_Can`
- `Canvas/UI_Root/Gameplay_Area/Farm_Plot/Water_Status_Icon`
- `Canvas/UI_Root/Gameplay_Area/Animal_Pen`
- `Canvas/UI_Root/Gameplay_Area/Animal_Pen/Animal_Visual`
- `Canvas/Prefabs_Holder/Heart_Feedback_Template`
- `Canvas/UI_Root/Gameplay_Area/StickerBook_Panel`
- `Canvas/UI_Root/Gameplay_Area/StickerBook_Panel/Prev_Button`
- `Canvas/UI_Root/Gameplay_Area/StickerBook_Panel/Next_Button`
- `Canvas/UI_Root/Gameplay_Area/StickerBook_Panel/Trash_Can`
- `Canvas/Prefabs_Holder/Sticker_Template`
- `Canvas/Prefabs_Holder/Sticker_Template/Visual_Image`
- `Canvas/Prefabs_Holder/Sticker_Template/Shadow_Offset`
- `Canvas/UI_Root/Inventory_Tray`

**Checks:**
- Farm, animal, and sticker book panels are each within policy size ranges and do not severely overlap each other.
- Crop, animal, watering can, water status, heart, sticker, arrow, and trash images preserve aspect where they are `Image.Type.Simple`.
- Sticker book background is sliced and does not preserve aspect.
- Inventory tray cell sizes remain within 90-130 px; sticker visuals remain 70-105 px.
- Button text remains inside buttons and does not cover icon-only buttons.

**Exit criteria:** Home gameplay widget checks pass and snapshot review shows no oversized/undersized gameplay asset.

### Cluster 3: Home World Objects And Camera

**Purpose:** Validate camera framing and non-UI `SpriteRenderer` objects.

**Objects:**
- `Main Camera`
- `NPC_BaNgoai`
- `Quest_Board`
- `Shop_Stall`

**Checks:**
- Main Camera keeps full-screen viewport and expected orthographic behavior.
- `NPC_BaNgoai` rendered height is 3.1-3.8 world units and does not cover more than 45% of the camera vertical world span.
- `Quest_Board` rendered height is 1.2-1.8 world units.
- `Shop_Stall` rendered height is 1.4-2.0 world units.
- Each world object center is inside the camera frustum.
- World objects do not hide the main home UI composition in the camera snapshot.

**Exit criteria:** Unity MCP `screenshot_game` confirms camera/world framing is acceptable before popup work begins.

### Cluster 4: Quest HUD And Quest Popup

**Purpose:** Validate quest presentation both in compact HUD and popup form.

**Objects:**
- `QuestHudWidget` generated row template/rows.
- `Canvas/UI_Root/Quest_Popup`
- `Canvas/UI_Root/Quest_Popup/Content_Panel`
- Quest popup close button.
- Quest popup row background, type icon, quest text, and completed stamp.

**Checks:**
- Quest HUD rows do not block home interactions and stay within HUD bounds.
- Quest popup content panel is sliced and inside screen bounds.
- Quest row background is sliced/no preserve aspect.
- Quest type icons and completed stamps are 28-56 px and preserve aspect.
- Quest row text stays inside row bounds and does not overlap stamps/icons.

**Exit criteria:** Quest HUD and Quest Popup checks pass before moving to Shop Popup.

### Cluster 5: Shop Popup

**Purpose:** Validate data-driven shop item visuals and tabs.

**Objects:**
- `Canvas/UI_Root/Shop_Popup`
- `Canvas/UI_Root/Shop_Popup/Content_Panel`
- Shop close button.
- Shop tabs/buttons.
- Runtime `ShopItemWidget` rows/cards for seeds, stickers, crops, locks, and coins.

**Checks:**
- Shop content panel is sliced and inside screen bounds.
- Shop item icons preserve aspect after `ShopItemWidget.Setup(...)`.
- Coin/lock icons preserve aspect and stay within item card bounds.
- Item names/prices do not overlap icons or action buttons.
- Disabled/locked states do not resize or shift item cards.

**Exit criteria:** Shop Popup checks pass before moving to Dialogue Popup.

### Cluster 6: Dialogue Popup

**Purpose:** Validate narrative popup imagery and text layout.

**Objects:**
- `Canvas/UI_Root/Dialogue_Popup`
- `Canvas/UI_Root/Dialogue_Popup/Content_Panel`
- Dialogue portrait image.
- Dialogue indicator image.
- Speaker/name text and dialogue text.

**Checks:**
- Dialogue bubble background is sliced and inside screen bounds.
- Portrait and indicator preserve aspect.
- Typewriter text area remains inside bubble and does not overlap portrait/indicator.
- Dialogue popup starts hidden by content panel state but can still validate serialized references.

**Exit criteria:** Dialogue Popup checks pass before moving to Diary/Scrapbook popup work.

### Cluster 7: Diary Input Popup And Scrapbook/Diary Runtime Items

**Purpose:** Validate custom diary note UI and scrapbook image-backed elements.

**Objects:**
- `Canvas/UI_Root/Diary_Input_Popup`
- `Canvas/UI_Root/Diary_Input_Popup/Content_Panel`
- Diary input field background.
- Diary close/save buttons.
- `Canvas/Prefabs_Holder/Diary_Note_Template`
- Runtime `StickerBookPage` background image.
- Runtime diary note instances spawned on scrapbook pages.

**Checks:**
- Diary popup content panel is sliced and inside screen bounds.
- Diary note template is sliced/no preserve aspect and text remains inside note bounds.
- Scrapbook page backgrounds remain sliced/no preserve aspect after style changes.
- Runtime diary notes do not exceed page bounds after creation or drag/drop.

**Exit criteria:** Diary and scrapbook checks pass before final aggregate validation.

### Cluster 8: Final Aggregate Visual Pass

**Purpose:** Catch regressions introduced by interactions between clusters.

**Checks:**
- Run full visual layout scene validation route.
- Capture one home screen snapshot.
- Capture at least one popup snapshot after opening Quest, Shop, Dialogue, and Diary popups.
- Confirm no cluster has regressed.

**Exit criteria:** All cluster checks pass, scene setup idempotency passes, and handoff records any intentionally skipped non-visual validation as `not run - not relevant to this change`.

## Task 1: Phase 41.1 Visual Fitting Helpers

**Files:**
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.Helpers.cs`

- [ ] **Step 1: Add UI image policy helpers**

Add helpers near existing `SetupImage`, `ConfigureCloseButton`, and `SetupButtonIcon`:

```csharp
private static void ConfigureSimpleImage(Image image, bool preserveAspect, ref bool isDirty)
{
    if (image == null) return;

    if (image.type != Image.Type.Simple)
    {
        image.type = Image.Type.Simple;
        isDirty = true;
    }

    if (image.preserveAspect != preserveAspect)
    {
        image.preserveAspect = preserveAspect;
        isDirty = true;
    }

    if (image.color != Color.white)
    {
        image.color = Color.white;
        isDirty = true;
    }
}

private static void ConfigureSlicedImage(Image image, ref bool isDirty)
{
    if (image == null) return;

    if (image.type != Image.Type.Sliced)
    {
        image.type = Image.Type.Sliced;
        isDirty = true;
    }

    if (image.preserveAspect)
    {
        image.preserveAspect = false;
        isDirty = true;
    }

    if (image.color != Color.white)
    {
        image.color = Color.white;
        isDirty = true;
    }
}
```

- [ ] **Step 2: Add RectTransform size helper for visual children**

```csharp
private static void ConfigureFixedVisualRect(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position, ref bool isDirty)
{
    if (rect == null) return;

    SafeSetAnchor(rect, anchor, anchor, ref isDirty);
    SafeSetPivot(rect, new Vector2(0.5f, 0.5f), ref isDirty);
    SafeSetSizeDelta(rect, size, ref isDirty);
    SafeSetAnchoredPosition(rect, position, ref isDirty);
}
```

- [ ] **Step 3: Add world sprite bounds fitting helper**

```csharp
private static void FitSpriteRendererToHeight(SpriteRenderer renderer, float targetHeight, ref bool isDirty)
{
    if (renderer == null || renderer.sprite == null || targetHeight <= 0f) return;

    float spriteHeight = renderer.sprite.bounds.size.y;
    if (spriteHeight <= 0f) return;

    float scale = targetHeight / spriteHeight;
    Vector3 targetScale = new Vector3(scale, scale, 1f);
    if (Vector3.Distance(renderer.transform.localScale, targetScale) > 0.001f)
    {
        renderer.transform.localScale = targetScale;
        isDirty = true;
    }
}
```

- [ ] **Step 4: Update existing helper call sites**

Update `ConfigureCloseButton` and `SetupButtonIcon` to use `ConfigureSimpleImage(..., true, ...)`. Keep `SetupImage` generic and do not force aspect there, because callers need different policies.

## Task 2: Phase 41.2 And 41.3 Apply Size Policy In Scene Setup

**Files:**
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.Gameplay.cs`
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.Popups.cs`
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.WorldAndUI.cs`

Implement this task in two separate passes:

1. Phase 41.2 Home pass: apply Step 1 Gameplay panels and Step 3 HUD/sidebar/world, then run home cluster validation and snapshot review.
2. Phase 41.3 Popup pass: apply Step 2 Popups, then validate Quest, Shop, Dialogue, and Diary/Scrapbook popup clusters one by one.

Do not bundle popup changes into the home pass.

- [ ] **Step 1: Gameplay panels**

Set fixed visual rects and image policy for:

- `Farm_Plot/Crop_Visual`: 150x150, preserve aspect.
- `Farm_Plot/Watering_Can`: 72x72, preserve aspect.
- `Farm_Plot/Water_Status_Icon`: 34x34, preserve aspect, ignore layout.
- `Animal_Pen/Animal_Visual`: 150x150, preserve aspect.
- `Heart_Feedback_Template`: 44x44, preserve aspect.
- `StickerBook_Panel`: sliced, no preserve aspect.
- `Prev_Button`, `Next_Button`, `Trash_Can`: preserve aspect icons with explicit sizes.
- `Sticker_Template/Visual_Image` and `Shadow_Offset`: 92x92 and preserve aspect.
- `Diary_Note_Template`: sliced, no preserve aspect.

- [ ] **Step 2: Popups**

Set popup image policies for:

- `Quest_Popup/Content_Panel`, `Shop_Popup/Content_Panel`, `Diary_Input_Popup/Content_Panel`: sliced panels, no preserve aspect.
- `Dialogue_Popup/Content_Panel`: sliced bubble, no preserve aspect.
- All popup close buttons: simple image, preserve aspect, 36-44 px.
- Quest row background: sliced, no preserve aspect.
- Quest row type icon and completed stamp: simple image, preserve aspect.
- Shop item icons, seed/crop/sticker icons, lock icons, and coin icons: simple image, preserve aspect.
- Dialogue indicator: simple image, preserve aspect.

- [ ] **Step 3: HUD/sidebar/world**

Set image/world policies for:

- `Coins_Text/Coin_Icon`, `Seeds_Text/Seeds_Icon`, `Crops_Text/Crops_Icon`, `Autosave_Icon`, `Level_Star_Icon`: simple image, preserve aspect.
- XP fill/background: no preserve aspect.
- Sidebar tab backgrounds: sliced, no preserve aspect.
- Sidebar tab icons: simple image, preserve aspect.
- `NPC_BaNgoai`: use `FitSpriteRendererToHeight(renderer, 3.4f, ref isSceneDirty)`.
- `Quest_Board`: use `FitSpriteRendererToHeight(renderer, 1.5f, ref isSceneDirty)`.
- `Shop_Stall`: use `FitSpriteRendererToHeight(renderer, 1.7f, ref isSceneDirty)`.

## Task 3: Phase 41.1, 41.2, And 41.3 Visual Layout Validator

**Files:**
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimSceneGameplayValidation.UI.cs`
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimSceneGameplayValidation.cs`

- [ ] **Step 1: Add validator entry point**

Add `ValidateVisualLayout(List<string> errors, List<string> warnings, List<string> passes)` and call it from `RunValidation()` after `ValidateUISpriteAssignments(...)`.

Inside `ValidateVisualLayout`, call cluster-specific methods in this order:

```csharp
ValidateHomeScreenShell(errors, warnings, passes);
ValidateHomeGameplayWidgets(errors, warnings, passes);
ValidateHomeWorldObjectsAndCamera(errors, warnings, passes);
ValidateQuestHudAndPopup(errors, warnings, passes);
ValidateShopPopup(errors, warnings, passes);
ValidateDialoguePopup(errors, warnings, passes);
ValidateDiaryAndScrapbook(errors, warnings, passes);
ValidateAggregateVisualLayout(errors, warnings, passes);
```

Each method should only validate its own cluster from the Visual Validation Test Matrix so failures identify the affected screen area.

Implementation order:

1. Phase 41.1: create helper methods and cluster method shells.
2. Phase 41.2: fill Cluster 1, Cluster 2, and Cluster 3 checks.
3. Phase 41.3: fill Cluster 4, Cluster 5, Cluster 6, and Cluster 7 checks.
4. Phase 41.5: fill Cluster 8 aggregate checks after all earlier clusters pass.

- [ ] **Step 2: Validate image aspect policy**

Implement helpers:

```csharp
private static void ValidateImagePolicy(string path, Image.Type expectedType, bool expectedPreserveAspect, List<string> errors, List<string> passes)
{
    Image image = FindSceneImage(path);
    if (image == null)
    {
        errors.Add($"Visual image '{path}' is missing.");
        return;
    }

    if (image.type != expectedType)
    {
        errors.Add($"Visual image '{path}' type is {image.type}, expected {expectedType}.");
        return;
    }

    if (image.preserveAspect != expectedPreserveAspect)
    {
        errors.Add($"Visual image '{path}' preserveAspect is {image.preserveAspect}, expected {expectedPreserveAspect}.");
        return;
    }

    passes.Add($"Visual image '{path}' has expected type/aspect policy.");
}
```

Check all sprite/image users listed in the Visual Validation Test Matrix, including every popup cluster.

- [ ] **Step 3: Validate RectTransform size ranges**

Implement `ValidateRectSizeRange(path, minWidth, maxWidth, minHeight, maxHeight, ...)` for gameplay panels, icons, sidebar, HUD, popup close buttons, popup row icons, sticker visuals, diary notes, and dialogue indicators.

- [ ] **Step 4: Validate offscreen and severe overlap**

Use `RectTransformUtility.CalculateRelativeRectTransformBounds(root, target)` for UI elements and fail if key rects exceed the root canvas bounds by more than a small tolerance. Check severe overlap among:

- `Farm_Plot`, `Animal_Pen`, `StickerBook_Panel`.
- Header/HUD vs gameplay area.
- Sidebar vs main gameplay objects.
- Popup content panels vs screen bounds.

- [ ] **Step 5: Validate world renderer bounds and camera frustum**

Validate `NPC_BaNgoai`, `Quest_Board`, and `Shop_Stall`:

- rendered bounds height within policy range;
- bounds center visible in `Main Camera`;
- no object covers more than 45% of the camera vertical world span (`camera.orthographicSize * 2f`).

## Task 4: Phase 41.4 Runtime Dynamic Image Policy

**Files:**
- Modify: `Assets/CozyLifeSim/Scripts/UI/CropWidget.cs`
- Modify: `Assets/CozyLifeSim/Scripts/UI/AnimalWidget.cs`
- Modify: `Assets/CozyLifeSim/Scripts/UI/CozySticker.cs`
- Modify: `Assets/CozyLifeSim/Scripts/UI/CozyQuestItemWidget.cs`
- Modify: `Assets/CozyLifeSim/Scripts/UI/ShopItemWidget.cs`
- Modify: `Assets/CozyLifeSim/Scripts/UI/CozyDialoguePopup.cs`
- Modify: `Assets/CozyLifeSim/Scripts/UI/StickerBookPage.cs`
- Modify: `Assets/CozyLifeSim/Scripts/UI/CozyJuiceUtility.cs`

- [ ] **Step 1: Add small local image-policy helpers where runtime sprites are assigned**

For each runtime component that assigns `Image.sprite`, add a private helper with the narrow policy needed by that component. Keep helpers local to avoid creating a shared runtime dependency for editor-only scene setup code.

Example for simple icon/art images:

```csharp
private static void ConfigureSimpleArtImage(Image image)
{
    if (image == null) return;
    image.type = Image.Type.Simple;
    image.preserveAspect = true;
    image.color = Color.white;
}
```

Example for sliced backgrounds:

```csharp
private static void ConfigureSlicedBackgroundImage(Image image)
{
    if (image == null) return;
    image.type = Image.Type.Sliced;
    image.preserveAspect = false;
}
```

- [ ] **Step 2: Apply policy after every runtime sprite assignment**

Update these assignments:

- `CropWidget`: after `_cropVisual.sprite = spriteToUse`, configure `_cropVisual` as simple art with preserve aspect.
- `AnimalWidget`: after `_animalVisual.sprite = _animalTemplate.Sprite`, configure `_animalVisual`; after custom heart sprite assignment, configure the spawned heart image.
- `CozySticker`: after main and shadow sprite assignments, configure both images as simple art with preserve aspect.
- `CozyQuestItemWidget`: configure background as sliced/no preserve aspect; type icon and stamp as simple/preserve aspect.
- `ShopItemWidget`: configure `_itemIcon` as simple/preserve aspect after setup.
- `CozyDialoguePopup`: configure `_portrait` as simple/preserve aspect after portrait assignment.
- `StickerBookPage`: configure `_backgroundImage` as sliced/no preserve aspect when using a scrapbook background; preserve existing fallback behavior when no sprite is assigned.
- `CozyJuiceUtility`: configure coin fly template image as simple/preserve aspect.

- [ ] **Step 3: Keep runtime behavior unchanged**

Do not alter save data, service calls, button listeners, tween timing, presenter contracts, or gameplay logic. This task only changes visual image policy after sprite assignment.

## Task 5: Phase 41.5 Snapshot Review Utility

**Files:**
- Create: `Assets/CozyLifeSim/Scripts/Editor/CozyVisualSnapshotUtility.cs`

- [ ] **Step 1: Use Unity MCP for world/camera snapshot**

Use `screenshot_game` with 1280x720 from `Main Camera`. Save any temporary snapshot under `.agent/scratch/` or `C:\tmp`; do not commit it.

- [ ] **Step 2: Add a Unity-side UI overlay snapshot route**

Create `CozyVisualSnapshotUtility` with a menu-callable static method:

```csharp
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CozyLifeSim.Editor
{
    public static class CozyVisualSnapshotUtility
    {
        private const string SnapshotPath = "C:/tmp/cozy-life-sim-gameview-snapshot.png";

        [MenuItem("Tools/CozySim/Capture Visual Layout Snapshot")]
        public static void CaptureVisualLayoutSnapshot()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SnapshotPath));
            ScreenCapture.CaptureScreenshot(SnapshotPath);
            Debug.Log($"[CozySim Snapshot] Requested Game view snapshot at {SnapshotPath}");
        }
    }
}
```

Use Unity MCP `editor_invoke_method` to call this method when an overlay-inclusive Game view snapshot is needed. The output path is outside the repo and must not be committed.

- [ ] **Step 3: Cluster-by-cluster manual visual acceptance**

Review snapshots in the same order as the Visual Validation Test Matrix:

- Cluster 1 Home Screen Shell.
- Cluster 2 Home Gameplay Widgets.
- Cluster 3 Home World Objects And Camera.
- Cluster 4 Quest HUD And Quest Popup.
- Cluster 5 Shop Popup.
- Cluster 6 Dialogue Popup.
- Cluster 7 Diary Input Popup And Scrapbook/Diary Runtime Items.
- Cluster 8 Final Aggregate Visual Pass.

For each cluster, confirm:

- no policy-covered asset is outside its accepted size range;
- no key UI text/button/image overlaps incoherently;
- popup content fits within landscape screen bounds when the cluster includes a popup;
- world objects are visible and framed without hiding the main UI when the cluster includes world objects.

Do not proceed to the next cluster until the current cluster's automated visual checks and snapshot review pass or the remaining issue is explicitly recorded as an accepted warning.

## Task 6: Phase 41.5 Targeted Verification And Handoff

**Files:**
- Modify: `docs/plans/task.md`
- Modify: `docs/plans/current-handoff.md`

- [ ] **Step 1: Compile/log check**

Use Unity MCP when available to check compile/import and Console/Editor logs after script changes. Because `Assets/CozyLifeSim/Scripts/Editor/CozyVisualSnapshotUtility.cs` is a new Unity-tracked file, wait for Unity to generate `Assets/CozyLifeSim/Scripts/Editor/CozyVisualSnapshotUtility.cs.meta`; do not create the `.meta` manually.

- [ ] **Step 2: Run targeted scene validation**

Run `Tools/CozySim/Run Scene Gameplay Loop Validation`. Expected: existing scene checks plus new visual layout checks pass.

Expected visual log shape:

```text
[PASS] Cluster 1 Home Screen Shell ...
[PASS] Cluster 2 Home Gameplay Widgets ...
[PASS] Cluster 3 Home World Objects And Camera ...
[PASS] Cluster 4 Quest HUD And Quest Popup ...
[PASS] Cluster 5 Shop Popup ...
[PASS] Cluster 6 Dialogue Popup ...
[PASS] Cluster 7 Diary And Scrapbook ...
[PASS] Cluster 8 Final Aggregate Visual Pass ...
```

- [ ] **Step 3: Run idempotency check**

Because `CozySceneSetupWindow` is modified, run scene setup twice and verify the second run introduces exactly 0 new scene/importer diffs.

- [ ] **Step 4: Do not run full validation without user approval**

Per `AGENTS.md`, do not run full logic + scene + Play Mode + idempotency suite unless the user explicitly approves full validation.

- [ ] **Step 5: Update handoff**

Record:

- completed visual policy and validator coverage;
- targeted verification results;
- snapshot path only if it is a temporary review artifact;
- skipped validations as `not run - not relevant to this change`;
- any remaining visual warnings.

## Plan Self-Review

- Spec coverage: Covers asset fitting, popup coverage, all image-backed UI/world areas, visual validator, snapshot review, and targeted verification.
- Red-flag scan: No unresolved vague implementation markers remain; snapshot helper output is explicitly bounded to `C:\tmp` and is not committed.
- Type consistency: Uses existing Unity types and project classes: `Image`, `RectTransform`, `SpriteRenderer`, `CozySceneSetupWindow`, and `CozyLifeSimSceneGameplayValidation`.
- Scope check: Focused on visual layout validation and fitting, not new gameplay behavior.
