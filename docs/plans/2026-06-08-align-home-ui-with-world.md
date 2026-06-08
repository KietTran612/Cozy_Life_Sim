# Plan: Align Home UI with World Objects

This plan describes how we will adjust the Home UI panels (`Farm_Plot`, `Animal_Pen`, `StickerBook_Panel`) under the main `Canvas` to align dynamically with their corresponding world space objects (`FARM_RiceField_Block`, `ANIMAL_Buffalo`, etc.) in the scene `Main.unity`.

## User Review Required

> [!IMPORTANT]
> - The panel background images for `Farm_Plot` and `Animal_Pen` will be made transparent (`Color.clear`) to reveal the detailed world sprites under them, while their interactive visuals (`Crop_Visual`, `Animal_Visual`) will remain fully visible.
> - The `HorizontalLayoutGroup` component on `Gameplay_Area` will be removed to allow the three panels to be positioned freely at arbitrary screen positions.
> - A C# layout projection calculation will run at runtime inside `CozyLandscapeLayoutController` (triggered when screen size/aspect ratio changes) to project world space coordinates onto Canvas space in a resolution-independent and 100% idempotent manner:
>   `scaleFactor = canvasHeight / (2 * camera.orthographicSize)`
>   `canvasLocalPos = worldPosition * scaleFactor`
>   `uiPosition = parentRect.InverseTransformPoint(canvasRect.TransformPoint(canvasLocalPos))`

## Proposed Changes

### UI & Layout

#### [MODIFY] [CozyLandscapeLayoutController.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/CozyLandscapeLayoutController.cs)
- Add null-safe world-to-UI projection logic in `Apply()`.
- Dynamically position `Farm_Plot` to overlay on top of `FARM_RiceField_Block`'s projected coordinates.
- Dynamically position `Animal_Pen` to overlay on top of `ANIMAL_Buffalo`'s projected coordinates.
- Position `StickerBook_Panel` responsively on the right side of the screen (e.g. anchored at `(0.65, 0.5)` with pivot `(0.5, 0.5)` and offset `(0, 0)`).

### Editor & Scene Generation

#### [MODIFY] [CozySceneSetupWindow.Gameplay.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.Gameplay.cs)
- Modify the `Gameplay_Area` configuration to destroy/remove the `HorizontalLayoutGroup` if it exists.
- Set the background image color of `Farm_Plot` and `Animal_Pen` to transparent (`Color.clear`).
- Set their anchors and pivots to `(0.5, 0.5)`.

#### [MODIFY] [CozySceneSetupWindow.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs)
- Call the alignment logic directly during scene generation to position the panels correctly in the editor so there is no mismatch in edit mode.

### Validation

#### [MODIFY] [CozyLifeSimSceneGameplayValidation.VisualLayout.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimSceneGameplayValidation.VisualLayout.cs)
- Remove the strict `ValidateNoOverlap` check between `Farm_Plot` and `Animal_Pen` because they now intentionally overlap/intersect due to their world alignment.
- Update `ValidateHomeScreenShell` or `ValidateHomeGameplayWidgets` as needed.

---

## Verification Plan

### Automated Tests
- Run scene validation `CozyLifeSim.Editor.CozyLifeSimSceneGameplayValidation.RunValidation` to verify that all checks pass cleanly.
- Ensure that running `GenerateSceneSilent` consecutively produces exactly 0 git diffs on scene files, achieving 100% idempotency.

### Manual Verification
- Review the game view to confirm that buttons, labels, and timer overlays sit perfectly on top of the corresponding world objects.
