# Implementation Plan: Sticker Database Utility & Scale Integrity Fixes

This implementation plan addresses the two key P2 review feedback issues regarding the Unity Cozy Life Sim project:
1. **Sticker Database Auto-Creation**: Refactor the sticker database discovery to use a shared utility `StickerDatabaseUtility.LoadOrCreateDatabase()`, resolving the dependency on pre-created assets during a clean workspace setup.
2. **Sticker Scale Consistency**: Eliminate scale instability when saving placed stickers during active tweens and guarantee stable 1.0f scale preservation upon game restoration.

---

## User Review Required

> [!NOTE]
> - These changes are internal architectural improvements.
> - No breaking changes are introduced; compilation boundaries and existing validation tests are strictly preserved.

---

## Proposed Changes

### Editor Component

#### [NEW] [StickerDatabaseUtility.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/StickerDatabaseUtility.cs)
- Implement `StickerDatabaseUtility` containing `LoadOrCreateDatabase()` and `BootstrapDefaultStickers(StickerDatabase database)` with safety sprite discover fallbacks.

#### [MODIFY] [CozySceneSetupWindow.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs)
- Update line 150-156 to call `StickerDatabaseUtility.LoadOrCreateDatabase()`.

#### [MODIFY] [StickerEditorWindow.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/StickerEditorWindow.cs)
- Refactor `LoadOrCreateDatabase()` method to delegate asset instantiation/bootstrapping to `StickerDatabaseUtility.LoadOrCreateDatabase()`.

#### [MODIFY] [CozyLifeSimValidation.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimValidation.cs)
- Add **Test 9.5: StickerDatabase In-Memory Bootstrapping and Integrity** to test side-effect-free bootstrapping of the default stickers via `StickerDatabaseUtility.BootstrapDefaultStickers(testDb)`.

---

### UI Component

#### [MODIFY] [CozySticker.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/CozySticker.cs)
- Update `FinalizePlacement` at line 199 to save a fixed `1.0f` scale value instead of `transform.localScale.x`, preventing active lift-tween scales (e.g. 1.1f) from contaminating the persistent storage.

---

## Verification Plan

### Automated Tests
1. **Asset Missing & Clean Workspace Simulation**:
   - **Backup**: Prior to testing, temporarily copy the current state of `Assets/CozyLifeSim/Scenes/Main.unity`, `Assets/CozyLifeSim/Settings/StickerDatabase.asset`, and `Assets/CozyLifeSim/Settings/StickerDatabase.asset.meta` to a temporary backup directory outside of the `Assets/` folder (e.g. at the project root folder).
   - **Move**: Move the active `StickerDatabase.asset` and `.meta` files out of the `Assets/` folder to simulate a clean workspace with missing database assets.
   - **Simulate Setup**: Invoke `CozySceneSetupWindow.GenerateSceneSilent` via Unity MCP.
   - **Verify**: Verify that `StickerDatabaseUtility` automatically recreates `StickerDatabase.asset` in the directory, bootstraps the two default stickers, and successfully auto-wires the reference to `GameLifetimeScope._stickerDatabase`.
   - **Restore**: Completely restore the original `Main.unity`, `StickerDatabase.asset`, and `.meta` files back from the backup folder to their original locations under `Assets/`, overwriting any temporary modifications. This preserves the original uncommitted scene changes and GUID reference integrity with zero reference pollution.
2. **Logic Verification Tests**: 
   - Run `CozyLifeSimValidation.RunTests` via MCP to ensure all logic validations, including the new **Test 9.5: StickerDatabase Bootstrapping** test, pass cleanly.
3. **Scene Gameplay Validation**: 
   - Run `CozyLifeSimSceneGameplayValidation.RunValidation` to verify that **all scene gameplay validations pass cleanly** without reference or compiler warnings (the current baseline validation count is 51).
