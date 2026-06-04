# Scene Setup Visual Layout Fix Plan

## Goal
Resolve visual layout issues where prototype fallback sprites (giant heart, giant acorn) are shown instead of actual assets, farm and animal panels are plain gray/transparent boxes, duplicate legacy UI components clog the layout columns, and serialization bindings for the Animal pen are broken.

---

## Proposed Changes

### 1. [CozyLifeSim.Editor] CozySceneSetupWindow.Gameplay.cs
- **File**: [CozySceneSetupWindow.Gameplay.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.Gameplay.cs)
- **Clear Legacy Children**: Call `ClearChildren(farmPlot)` and `ClearChildren(animalPen)` at the beginning of their respective configuration methods to remove any legacy/obsolete GameObjects (e.g. `Pet_Chicken_Button`, `Chicken_Visual`).
- **Load and Assign Panel Background Sprites**:
  - Load `World_Soil_Plot.png` and assign it to the `Image` component on `Farm_Plot`.
  - Load `World_Animal_Pen.png` and assign it to the `Image` component on `Animal_Pen`.
  - Configure both backgrounds as `Image.Type.Simple` with `preserveAspect = true`.
- **Fix Serializable Bindings**:
  - Update `AnimalWidget` serialization to bind `_interactionButton` (instead of `_petButton`) and `_spawnRoot` (instead of `_heartSpawnRoot`).

### 2. [CozyLifeSim.Editor] CozySceneSetupWindow.WorldAndUI.cs
- **File**: [CozySceneSetupWindow.WorldAndUI.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.WorldAndUI.cs)
- **Load and Assign World Sprites**:
  - Load `World_Quest_Board.png` and assign it to `Quest_Board` (instead of using the fallback `heartSprite`).
  - Load `World_Shop_Stall.png` and assign it to `Shop_Stall` (instead of using the fallback `seedSprite`).
  - Set the renderer colors to `Color.white` to prevent tinting.

---

## Verification Plan

### Manual Verification
1. Run `Tools/CozySim/Setup Test Scene Silent` (or use the Setup window) to regenerate the scene.
2. Open the Game view and verify visually:
   - `Quest_Board` is a beautiful wooden quest board sprite (not a giant heart).
   - `Shop_Stall` is a beautiful shop stall sprite (not a giant acorn).
   - `Farm_Plot` has the `World_Soil_Plot` background.
   - `Animal_Pen` has the `World_Animal_Pen` background.
   - No duplicate buttons or visuals are present inside the `Farm_Plot` or `Animal_Pen` columns.
3. Check `git diff` to ensure no unexpected changes occurred and the layout remains idempotent.

### Automated Tests
- Run `Tools/CozySim/Run Scene Gameplay Loop Validation` and confirm all 429 tests still pass cleanly with 0 errors.
