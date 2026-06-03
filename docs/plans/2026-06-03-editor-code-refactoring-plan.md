# Refactoring Plan: Splitting Long Editor Classes

This plan outlines the strategy to refactor and split the long editor C# script files into modular, readable files using C# **partial classes**. This maintains full serialization compatibility, setup idempotency, and compilation safety without modifying class structures or public APIs.

## User Review Required

- Existing class declarations must be updated to use the `partial` keyword:
  - `CozySceneSetupWindow.cs` (line 12) -> `public partial class CozySceneSetupWindow : EditorWindow`
  - `CozyLifeSimSceneGameplayValidation.cs` (line 11) -> `public static partial class CozyLifeSimSceneGameplayValidation`
- All new files must use the `CozyLifeSim.Editor` namespace and be located inside `Assets/CozyLifeSim/Scripts/Editor/`.
- No behavior changes or new public APIs will be introduced.
- We must not manually create Unity `.meta` files. After writing the new C# partial scripts, we must wait for Unity to automatically compile, import, and generate corresponding `.meta` files.

## Extraction Decisions for `CozySceneSetupWindow`

`GenerateScene()` will be split by extracting blocks into private/internal helper methods:
1. **Delegate/Helper `loadSprite`**: The local method `Sprite loadSprite(string name)` will be refactored into a private method `private Sprite LoadSprite(string name)` inside the `CozySceneSetupWindow` class itself. This allows all partial class files to call `LoadSprite` directly without passing delegates.
2. **Local State `isSceneDirty`**: All extracted configuration methods (e.g., `ConfigureFarmPlot`) will take `ref bool isSceneDirty` as a parameter to track and accumulate scene modifications correctly.
3. **Hierarchy Roots & Databases**: Extracted methods will receive required local variables (e.g. `canvas`, `uiRoot`, `headerPanel`, `gameplayArea`, `questDb`, `cropDb`, `animalDb`, `stickerDb`) as parameter arguments.

---

## Proposed Changes

### 1. CozySceneSetupWindow [MODIFY/NEW]
We will split the class `CozySceneSetupWindow` across 5 files:

#### [MODIFY] [CozySceneSetupWindow.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs)
* Change declaration: `public partial class CozySceneSetupWindow : EditorWindow`.
* Refactor local `loadSprite` into a class-level private helper `LoadSprite(string name)`.
* Keep main MenuItem triggers (`GenerateSceneSilent()`, `ShowWindow()`), window `OnGUI()`, player/build settings configuration, and the master `GenerateScene()` method which calls the extracted methods.

#### [NEW] [CozySceneSetupWindow.Helpers.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.Helpers.cs)
* Class declaration: `public partial class CozySceneSetupWindow`.
* Contains standard UI rendering helpers:
  * `SetupPanel()`, `SetupText()`, `SetupImage()`, `SetupButton()`, `SetupButtonIcon()`
  * RectTransform transformations (`StretchToFill()`, `SafeSetAnchor()`, `SafeSetSizeDelta()`, `SafeSetAnchoredPosition()`, `SafeSetPivot()`)
  * Serialization safety helpers (`SafeSetObjectReference()`, `SafeSetInt()`, `SafeSetFloat()`, `SafeSetString()`)
  * `ClearChildren()`

#### [NEW] [CozySceneSetupWindow.Gameplay.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.Gameplay.cs)
* Class declaration: `public partial class CozySceneSetupWindow`.
* Contains widget configuration methods:
  * `ConfigureFarmPlot(RectTransform gameplayArea, Sprite defaultSprite, Sprite seedSprite, Sprite sproutSprite, Sprite matureSprite, Sprite wateringCanSprite, ref bool isSceneDirty)`
  * `ConfigureAnimalPen(RectTransform gameplayArea, Sprite defaultSprite, Sprite chickenSprite, Sprite heartSprite, RectTransform prefabsHolder, ref bool isSceneDirty)`
  * `ConfigureStickerBook(RectTransform gameplayArea, RectTransform prefabsHolder, RectTransform inventoryTray, CozySticker genericSticker, CozyDiaryNote diaryNoteComponent, RectTransform diaryInputPopup, ref bool isSceneDirty)`
  * `ConfigureInventoryTray(RectTransform uiRoot, ref bool isSceneDirty)`

#### [NEW] [CozySceneSetupWindow.Popups.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.Popups.cs)
* Class declaration: `public partial class CozySceneSetupWindow`.
* Contains popup canvas configurations:
  * `ConfigureQuestPopup(RectTransform uiRoot, Color dimColor, CozyQuestItemWidget questItemWidgetComponent, ref bool isSceneDirty)`
  * `ConfigureShopPopup(RectTransform uiRoot, Color dimColor, Sprite seedSprite, Sprite chickenSprite, ref bool isSceneDirty)`
  * `ConfigureDialoguePopup(RectTransform uiRoot, ref bool isSceneDirty)`
  * `ConfigureDiaryInputPopup(RectTransform uiRoot, Color dimColor, ref bool isSceneDirty)`
  * `ConfigureCloseButton()`, `SetupQuestItemWidgetTemplate()`

#### [NEW] [CozySceneSetupWindow.WorldAndUI.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.WorldAndUI.cs)
* Class declaration: `public partial class CozySceneSetupWindow`.
* Contains sidebar, header layouts, and in-world interactive setups:
  * `ConfigureNPCGrandma(Sprite chickenSprite, Sprite defaultSprite, ref bool isSceneDirty)`
  * `ConfigureInWorldInteractiveObjects(Sprite heartSprite, Sprite seedSprite, Sprite defaultSprite, ref bool isSceneDirty)`
  * `ConfigureWorldClickVisual()`
  * `ConfigureHeaderPanel(RectTransform uiRoot, Sprite coinSprite, Sprite seedsSprite, Sprite cropsSprite, ref bool isSceneDirty)`
  * `ConfigureSidebarPanel(RectTransform uiRoot, ref bool isSceneDirty)`
  * `ConfigureProgressionHud(RectTransform headerPanel, Sprite starSprite, ref bool isSceneDirty)`
  * `ConfigureLandscapeLayoutController(...)`
  * `ConfigureStartupLoadingOverlay(...)`
  * `ConfigureFeedbackToast(...)`

---

### 2. CozyLifeSimSceneGameplayValidation [MODIFY/NEW]
We will split the class `CozyLifeSimSceneGameplayValidation` across 3 files:

#### [MODIFY] [CozyLifeSimSceneGameplayValidation.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimSceneGameplayValidation.cs)
* Change declaration: `public static partial class CozyLifeSimSceneGameplayValidation`.
* Contains the entry point `RunValidation()` and high-level scene structure validation orchestrations.

#### [NEW] [CozyLifeSimSceneGameplayValidation.UI.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimSceneGameplayValidation.UI.cs)
* Class declaration: `public static partial class CozyLifeSimSceneGameplayValidation`.
* Contains existing UI/layout/dialogue validation methods moved from the main class without renaming:
  * `ValidateInventoryHud(List<string> errors, List<string> passes)`
  * `ValidatePopupAndNavigationDocks(List<string> errors, List<string> passes)` (including helper `ValidatePopup`)
  * `ValidateInteractiveObject(string objectName, CozyPopup expectedPopup, List<string> errors, List<string> passes)`
  * `ValidateMainCamera(List<string> errors, List<string> passed)`
  * `ValidateScrapbookAndDialogues(List<string> errors, List<string> passes)`
  * `ValidateStartupLoadingOverlay(List<string> errors, List<string> passed)`
  * `ValidateLandscapeLayout(List<string> errors, List<string> passed)`
  * `ValidateUISpriteAssignments(List<string> errors, List<string> passed)`
  * Associated layout helper validation methods: `ValidateSidebarChildren`, `ValidateChildButton`, `ValidateStickerTemplate`, `ValidateObjectReference`, `ValidateSpriteReference`, `ValidateImageSprite`, `ValidateButtonTarget`


#### [NEW] [CozyLifeSimSceneGameplayValidation.Textures.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimSceneGameplayValidation.Textures.cs)
* Class declaration: `public static partial class CozyLifeSimSceneGameplayValidation`.
* Contains:
  * `ValidateTextureImporterMemorySettings()` (checks all Heritage texture parameters: isReadable, maxTextureSize, borders, trim matches)
  * `ComputeAlphaBounds()`
  * Rulesets (`GetExpectedBorder()`, `GetExpectedMaxSize()`, `GetExpectedTrim()`)

---

## Verification Plan

### Automated Tests
To verify compile integrity and correctness, we will run the actual Unity test routes and verify their output:
1. **Scene Validation Check**: Run `CozyLifeSimSceneGameplayValidation.RunValidation` to verify that all 323 scene checks pass cleanly.
2. **Logic Verification Check**: Run `CozyLifeSimValidation.RunTests` to ensure all 31 core logic tests pass successfully.
3. **Play Mode Loop Check**: Run `CozyLifeSimMcpGameplayLoopValidation.RunGameplayLoopValidation` in Play Mode.
4. **Idempotency Setup Check**: Run `Tools/CozySim/Setup Test Scene Silent` consecutively and ensure `git status` reports exactly `0` changes in `Main.unity`.

### Unity Compile & Meta File Acceptance
- Once files are created/modified, wait for Unity compilation to finish cleanly.
- Verify that Unity successfully auto-generated `.meta` files for the newly created partial scripts.
- If the user explicitly requests a commit, the commit must include all split `.cs` source code files and their corresponding auto-generated Unity `.meta` files.
- Confirm there are `0` compiler errors or warnings in the Unity console logs.


