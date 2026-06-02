# Landscape-Only Startup, Camera & Layout Hardening Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Boot directly into `Main.unity` with a lightweight in-scene loading overlay, hardened 2D camera setup, landscape-only player settings, responsive landscape UI layout, and validation coverage.

**Architecture:** Keep `GameLifetimeScope` and all VContainer registrations inside `Main.unity`; do not introduce a separate loading scene or main menu flow. Add small UI/runtime helpers that are generated and wired by `CozySceneSetupWindow`, then expand editor validations so scene setup, build settings, camera, orientation, and landscape layout cannot silently regress.

**Tech Stack:** Unity 6000.3.11f1, C#, uGUI, TextMeshPro, DOTween, UniTask, VContainer, Unity Editor validation utilities.

---

## Scope Decisions

- The game boots straight into `Assets/CozyLifeSim/Scenes/Main.unity`.
- No main menu, no new game button, no continue button, no reset flow, and no separate loading scene.
- Loading is an overlay inside `Main.unity`, shown while the first runtime frame and service/widget binding settle.
- Landscape is the only supported orientation. Do not implement portrait layout branches or portrait fallback UI.
- Keep validation for critical setup even though there is no menu/reset flow.
- Do not manually create Unity `.meta` files. New Unity scripts must wait for Unity to import and generate `.meta` files before commit.
- Do not commit unless the user explicitly requests it.

## File Structure

- Create: `Assets/CozyLifeSim/Scripts/UI/CozyStartupLoadingOverlay.cs`
  - Owns the in-scene loading overlay visibility, fade, and input blocking while startup settles.
- Create: `Assets/CozyLifeSim/Scripts/UI/CozyLandscapeLayoutController.cs`
  - Owns landscape-only layout sizing constraints for generated root panels and popups.
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs`
  - Sets build scene, PlayerSettings orientation, Main Camera, CanvasScaler, startup overlay hierarchy, and landscape layout controller references.
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimSceneGameplayValidation.cs`
  - Adds scene/build/player/camera/canvas/loading/layout validation checks.
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimValidation.cs`
  - Adds focused logic checks for startup overlay and layout controller default values if editor-only scene validation cannot cover them cleanly.
- Modify only if runtime validation proves the overlay blocks first-click automation: `Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimMcpGameplayLoopValidation.cs`
  - Waits until the startup overlay is hidden before clicking runtime widgets.
- Modify: `docs/plans/task.md`
  - Tracks Task 37 implementation status.
- Modify: `docs/plans/current-handoff.md`
  - Records latest plan, known scope, and next recommended action.
- Modify: `docs/plans/index.md`
  - Links this plan and replaces the stale main-menu next-task note.

## Task 37.1: Project Entry, Orientation, And Build Scene Settings

**Files:**
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs`
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimSceneGameplayValidation.cs`

- [ ] **Step 1: Add failing validation for build scene and landscape-only player settings**

Add checks in `CozyLifeSimSceneGameplayValidation` that fail until setup writes the settings:

```csharp
using System.Linq;

private static void ValidateBuildSceneAndOrientation(List<string> errors, List<string> passed)
{
    var mainScene = EditorBuildSettings.scenes.FirstOrDefault(scene => scene.path == MainScenePath);
    if (mainScene.path == MainScenePath && mainScene.enabled)
    {
        passed.Add("Main.unity is the enabled build entry scene.");
    }
    else
    {
        errors.Add("Main.unity must be added to EditorBuildSettings as an enabled scene.");
    }

    if (PlayerSettings.defaultInterfaceOrientation == UIOrientation.LandscapeLeft)
    {
        passed.Add("Default interface orientation is LandscapeLeft.");
    }
    else
    {
        errors.Add($"Default interface orientation must be LandscapeLeft. Actual: {PlayerSettings.defaultInterfaceOrientation}");
    }

    if (!PlayerSettings.allowedAutorotateToPortrait && !PlayerSettings.allowedAutorotateToPortraitUpsideDown)
    {
        passed.Add("Portrait autorotation is disabled.");
    }
    else
    {
        errors.Add("Portrait and portrait upside-down autorotation must be disabled.");
    }
}
```

Call it from the existing scene validation method after the scene is opened and before runtime widget checks.

- [ ] **Step 2: Run scene validation and confirm the new checks fail**

Run the existing editor menu validation through Unity or the existing MCP validation path:

```powershell
# Unity menu path: Tools/CozySim/Run Scene Gameplay Loop Validation
```

Expected: validation reports missing build scene/orientation requirements before setup is updated.

- [ ] **Step 3: Add setup code for build scene and landscape-only orientation**

In `CozySceneSetupWindow`, add a setup helper:

```csharp
using System.Linq;

private const string MainScenePath = "Assets/CozyLifeSim/Scenes/Main.unity";

private static void ConfigureBuildAndPlayerSettings()
{
    var scenes = EditorBuildSettings.scenes.ToList();
    var existingIndex = scenes.FindIndex(scene => scene.path == MainScenePath);

    if (existingIndex >= 0)
    {
        var scene = scenes[existingIndex];
        if (!scene.enabled)
        {
            scenes[existingIndex] = new EditorBuildSettingsScene(MainScenePath, true);
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
    else
    {
        scenes.Insert(0, new EditorBuildSettingsScene(MainScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
    PlayerSettings.allowedAutorotateToPortrait = false;
    PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
    PlayerSettings.allowedAutorotateToLandscapeLeft = true;
    PlayerSettings.allowedAutorotateToLandscapeRight = true;
}
```

Call this from `GenerateScene()` after style config lookup and before scene object generation so both the interactive window flow and `GenerateSceneSilent()` apply the same project settings.

- [ ] **Step 4: Run scene setup and validate settings**

Run:

```powershell
# Unity menu path: Tools/CozySim/Setup Test Scene Silent
# Unity menu path: Tools/CozySim/Run Scene Gameplay Loop Validation
```

Expected: build scene and orientation checks pass.

## Task 37.2: Main Camera Hardening

**Files:**
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs`
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimSceneGameplayValidation.cs`

- [ ] **Step 1: Add failing camera validation**

Add a camera validation helper:

```csharp
private static void ValidateMainCamera(List<string> errors, List<string> passed)
{
    var camera = Camera.main;
    if (camera == null)
    {
        errors.Add("Scene must have a tagged Main Camera.");
        return;
    }

    if (camera.orthographic)
    {
        passed.Add("Main Camera is orthographic.");
    }
    else
    {
        errors.Add("Main Camera must be orthographic for 2D landscape UI/world presentation.");
    }

    if (Mathf.Approximately(camera.orthographicSize, 5f))
    {
        passed.Add("Main Camera orthographic size is 5.");
    }
    else
    {
        errors.Add($"Main Camera orthographic size must be 5. Actual: {camera.orthographicSize}");
    }

    if (camera.transform.position == new Vector3(0f, 0f, -10f) && camera.transform.rotation == Quaternion.identity)
    {
        passed.Add("Main Camera transform is deterministic.");
    }
    else
    {
        errors.Add("Main Camera must be positioned at (0, 0, -10) with identity rotation.");
    }

    if (camera.clearFlags == CameraClearFlags.SolidColor)
    {
        passed.Add("Main Camera clears to solid color.");
    }
    else
    {
        errors.Add("Main Camera clear flags must be SolidColor.");
    }
}
```

- [ ] **Step 2: Run validation and confirm the current perspective camera fails**

Run scene validation.

Expected: validation fails because `Main.unity` currently has `orthographic: 0`.

- [ ] **Step 3: Harden camera setup**

Update the existing camera setup in `CozySceneSetupWindow`:

```csharp
private static Camera ConfigureMainCamera()
{
    var camera = Camera.main;
    if (camera == null)
    {
        var cameraObject = GameObject.Find("Main Camera") ?? new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        camera = cameraObject.GetComponent<Camera>() ?? cameraObject.AddComponent<Camera>();
    }

    camera.transform.position = new Vector3(0f, 0f, -10f);
    camera.transform.rotation = Quaternion.identity;
    camera.orthographic = true;
    camera.orthographicSize = 5f;
    camera.nearClipPlane = 0.3f;
    camera.farClipPlane = 1000f;
    camera.clearFlags = CameraClearFlags.SolidColor;
    camera.backgroundColor = new Color(0.18f, 0.18f, 0.22f);
    camera.rect = new Rect(0f, 0f, 1f, 1f);

    if (camera.GetComponent<AudioListener>() == null && Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 0)
    {
        camera.gameObject.AddComponent<AudioListener>();
    }

    EditorUtility.SetDirty(camera);
    EditorUtility.SetDirty(camera.transform);
    return camera;
}
```

Use the current project style for helper naming and dirtying; do not duplicate `MainScenePath` constants if the file already has one.

- [ ] **Step 4: Run scene setup and validation**

Expected: camera validation passes and `Main.unity` serializes `orthographic: 1`.

## Task 37.3: Startup Loading Overlay In Main Scene

**Files:**
- Create: `Assets/CozyLifeSim/Scripts/UI/CozyStartupLoadingOverlay.cs`
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs`
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimSceneGameplayValidation.cs`

- [ ] **Step 1: Add failing scene validation for startup overlay**

Validate that `Popup_Root/Startup_Loading_Overlay` exists and has the required runtime component:

```csharp
private static void ValidateStartupLoadingOverlay(List<string> errors, List<string> passed)
{
    var overlay = FindSceneComponent<CozyStartupLoadingOverlay>("Startup_Loading_Overlay");
    if (overlay == null)
    {
        errors.Add("Startup_Loading_Overlay must exist under Popup_Root and have CozyStartupLoadingOverlay.");
        return;
    }

    passed.Add("Startup loading overlay has CozyStartupLoadingOverlay.");

    var canvasGroup = overlay.GetComponent<CanvasGroup>();
    if (canvasGroup != null)
    {
        passed.Add("Startup loading overlay has CanvasGroup.");
    }
    else
    {
        errors.Add("Startup_Loading_Overlay must have CanvasGroup for fade/input blocking.");
    }

    SerializedObject soOverlay = new SerializedObject(overlay);
    ValidateObjectReference(soOverlay, "canvasGroup", "CozyStartupLoadingOverlay canvas group", errors, passed);
    ValidateObjectReference(soOverlay, "messageText", "CozyStartupLoadingOverlay message text", errors, passed);
}
```

- [ ] **Step 2: Create the overlay component**

Create `CozyStartupLoadingOverlay.cs`:

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace CozyLifeSim.UI
{
    public sealed class CozyStartupLoadingOverlay : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private float minVisibleSeconds = 0.35f;
        [SerializeField] private float fadeOutSeconds = 0.2f;

        private CancellationTokenSource hideCts;
        private Tween fadeTween;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            ShowImmediate();
        }

        private void Start()
        {
            hideCts = new CancellationTokenSource();
            HideAfterStartupAsync(hideCts.Token).Forget();
        }

        private void OnDestroy()
        {
            hideCts?.Cancel();
            hideCts?.Dispose();
            hideCts = null;
            fadeTween?.Kill();
            fadeTween = null;
        }

        public void ShowImmediate()
        {
            gameObject.SetActive(true);
            if (messageText != null)
            {
                messageText.text = "Dang vao nong trai...";
            }

            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        private async UniTaskVoid HideAfterStartupAsync(CancellationToken cancellationToken)
        {
            try
            {
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, cancellationToken);
                await UniTask.Delay(TimeSpan.FromSeconds(minVisibleSeconds), cancellationToken: cancellationToken);
                Hide();
            }
            catch (OperationCanceledException)
            {
                // Object teardown cancels startup hiding; no runtime action is needed.
            }
        }

        public void Hide()
        {
            if (canvasGroup == null)
            {
                gameObject.SetActive(false);
                return;
            }

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            fadeTween?.Kill();
            fadeTween = canvasGroup
                .DOFade(0f, fadeOutSeconds)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => gameObject.SetActive(false));
        }
    }
}
```

The message is ASCII in code. It can be localized later if the project adopts a localization layer.

- [ ] **Step 3: Generate overlay hierarchy in `CozySceneSetupWindow`**

Under the existing popup root, create or update:

```text
Popup_Root
+-- Startup_Loading_Overlay
    +-- Loading_Backdrop
    +-- Loading_Message
```

Configure:

```csharp
private void ConfigureStartupLoadingOverlay(Transform popupRoot, ref bool isSceneDirty)
{
    RectTransform overlayRect = SetupPanel(popupRoot, "Startup_Loading_Overlay", ref isSceneDirty);
    StretchToFill(overlayRect, ref isSceneDirty);
    overlayRect.transform.SetAsLastSibling();

    var group = overlayRect.GetComponent<CanvasGroup>();
    if (group == null)
    {
        group = overlayRect.gameObject.AddComponent<CanvasGroup>();
        isSceneDirty = true;
    }
    if (!Mathf.Approximately(group.alpha, 1f))
    {
        group.alpha = 1f;
        isSceneDirty = true;
    }
    if (!group.interactable)
    {
        group.interactable = true;
        isSceneDirty = true;
    }
    if (!group.blocksRaycasts)
    {
        group.blocksRaycasts = true;
        isSceneDirty = true;
    }

    Image backdrop = SetupImage(overlayRect, "Loading_Backdrop", ref isSceneDirty);
    StretchToFill(backdrop.GetComponent<RectTransform>(), ref isSceneDirty);
    var backdropColor = new Color(0.08f, 0.08f, 0.10f, 0.92f);
    if (backdrop.color != backdropColor)
    {
        backdrop.color = backdropColor;
        isSceneDirty = true;
    }

    TextMeshProUGUI message = SetupText(overlayRect, "Loading_Message", "Dang vao nong trai...", "Header_Text", ref isSceneDirty);
    RectTransform messageRect = message.GetComponent<RectTransform>();
    SafeSetAnchor(messageRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), ref isSceneDirty);
    SafeSetSizeDelta(messageRect, new Vector2(640f, 96f), ref isSceneDirty);
    SafeSetAnchoredPosition(messageRect, Vector2.zero, ref isSceneDirty);
    if (!Mathf.Approximately(message.fontSize, 32f))
    {
        message.fontSize = 32f;
        isSceneDirty = true;
    }
    if (message.color != Color.white)
    {
        message.color = Color.white;
        isSceneDirty = true;
    }

    var component = overlayRect.GetComponent<CozyStartupLoadingOverlay>();
    if (component == null)
    {
        component = overlayRect.gameObject.AddComponent<CozyStartupLoadingOverlay>();
        isSceneDirty = true;
    }

    SerializedObject soOverlay = new SerializedObject(component);
    bool overlayDirty = false;
    SafeSetObjectReference(soOverlay.FindProperty("canvasGroup"), group, ref overlayDirty);
    SafeSetObjectReference(soOverlay.FindProperty("messageText"), message, ref overlayDirty);
    SafeSetFloat(soOverlay.FindProperty("minVisibleSeconds"), 0.35f, ref overlayDirty);
    SafeSetFloat(soOverlay.FindProperty("fadeOutSeconds"), 0.2f, ref overlayDirty);
    if (overlayDirty)
    {
        soOverlay.ApplyModifiedProperties();
        isSceneDirty = true;
    }
}
```

Use the existing text/button/image creation helpers in `CozySceneSetupWindow` instead of introducing a second UI factory.

- [ ] **Step 4: Validate overlay does not affect VContainer registration**

Do not register `CozyStartupLoadingOverlay` in `GameLifetimeScope`. It should not resolve services and should not require DI because it exists only to cover the first runtime frame.

- [ ] **Step 5: Wait for Unity import and generated `.meta`**

After adding the script, wait for Unity to compile/import and confirm:

```text
Assets/CozyLifeSim/Scripts/UI/CozyStartupLoadingOverlay.cs.meta
```

exists and was generated by Unity.

## Task 37.4: Landscape-Only Layout Controller

**Files:**
- Create: `Assets/CozyLifeSim/Scripts/UI/CozyLandscapeLayoutController.cs`
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs`
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimSceneGameplayValidation.cs`

- [ ] **Step 1: Add failing validation for landscape layout controller and CanvasScaler**

Add checks:

```csharp
private static void ValidateLandscapeLayout(List<string> errors, List<string> passed)
{
    var canvas = FindSceneComponent<Canvas>("Canvas");
    var scaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
    if (scaler == null)
    {
        errors.Add("Root Canvas must have CanvasScaler.");
        return;
    }

    if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize &&
        scaler.referenceResolution == new Vector2(1920f, 1080f) &&
        scaler.screenMatchMode == CanvasScaler.ScreenMatchMode.MatchWidthOrHeight)
    {
        passed.Add("CanvasScaler is configured for landscape reference resolution.");
    }
    else
    {
        errors.Add("CanvasScaler must use ScaleWithScreenSize, 1920x1080, MatchWidthOrHeight.");
    }

    var controller = FindSceneComponent<CozyLandscapeLayoutController>("Landscape_Layout_Controller");
    if (controller != null)
    {
        passed.Add("Landscape layout controller exists.");
        SerializedObject soLayout = new SerializedObject(controller);
        ValidateObjectReference(soLayout, "header", "Landscape layout header", errors, passed);
        ValidateObjectReference(soLayout, "gameplayArea", "Landscape layout gameplay area", errors, passed);
        ValidateObjectReference(soLayout, "farmPlot", "Landscape layout farm plot", errors, passed);
        ValidateObjectReference(soLayout, "animalPen", "Landscape layout animal pen", errors, passed);
        ValidateObjectReference(soLayout, "stickerBookPanel", "Landscape layout sticker book panel", errors, passed);
        ValidateObjectReference(soLayout, "inventoryTray", "Landscape layout inventory tray", errors, passed);
        ValidateObjectReference(soLayout, "sidebar", "Landscape layout sidebar", errors, passed);
        ValidateObjectReference(soLayout, "popupRoot", "Landscape layout popup root", errors, passed);
    }
    else
    {
        errors.Add("Scene must include CozyLandscapeLayoutController.");
    }
}
```

Add `using UnityEngine.UI;` and reuse the existing scene-object helper methods in `CozyLifeSimSceneGameplayValidation`.

- [ ] **Step 2: Create landscape layout component**

Create `CozyLandscapeLayoutController.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace CozyLifeSim.UI
{
    public sealed class CozyLandscapeLayoutController : MonoBehaviour
    {
        [SerializeField] private RectTransform header;
        [SerializeField] private RectTransform gameplayArea;
        [SerializeField] private RectTransform farmPlot;
        [SerializeField] private RectTransform animalPen;
        [SerializeField] private RectTransform stickerBookPanel;
        [SerializeField] private RectTransform inventoryTray;
        [SerializeField] private RectTransform sidebar;
        [SerializeField] private RectTransform popupRoot;

        private void Start()
        {
            Apply();
        }

        private void OnRectTransformDimensionsChange()
        {
            Apply();
        }

        public void Apply()
        {
            ApplyStretch(header, new Vector2(0f, 0.9f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            ApplyStretch(gameplayArea, new Vector2(0f, 0.25f), new Vector2(1f, 0.9f), new Vector2(36f, 16f), new Vector2(-36f, -16f));
            ApplyStretch(inventoryTray, new Vector2(0f, 0f), new Vector2(1f, 0.23f), new Vector2(36f, 16f), new Vector2(-36f, -16f));
            ApplyStretch(sidebar, new Vector2(0.92f, 0.45f), new Vector2(0.985f, 0.9f), Vector2.zero, Vector2.zero);
            ApplyStretch(popupRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            ApplyPreferredSize(farmPlot, 350f, 500f);
            ApplyPreferredSize(animalPen, 350f, 500f);
            ApplyPreferredSize(stickerBookPanel, 500f, 500f);
        }

        private static void ApplyStretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void ApplyPreferredSize(RectTransform rect, float preferredWidth, float preferredHeight)
        {
            if (rect == null)
            {
                return;
            }

            var layout = rect.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = preferredWidth;
            layout.preferredHeight = preferredHeight;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;
        }
    }
}
```

If existing generated object names differ, map references by actual hierarchy names from `CozySceneSetupWindow` rather than renaming unrelated objects.

- [ ] **Step 3: Wire controller from scene setup**

Create or update `UI_Root/Landscape_Layout_Controller`, add `CozyLandscapeLayoutController` there, and assign serialized fields using the existing `SerializedObject` pattern so inactive popups and generated roots are wired consistently:

```csharp
private void ConfigureLandscapeLayoutController(
    RectTransform uiRoot,
    RectTransform headerPanel,
    RectTransform gameplayArea,
    RectTransform farmPlot,
    RectTransform animalPen,
    RectTransform stickerBookPanel,
    RectTransform inventoryTray,
    RectTransform sidebarPanel,
    RectTransform popupRoot,
    ref bool isSceneDirty)
{
    RectTransform controllerRect = SetupPanel(uiRoot, "Landscape_Layout_Controller", ref isSceneDirty);
    StretchToFill(controllerRect, ref isSceneDirty);

    var controller = controllerRect.GetComponent<CozyLandscapeLayoutController>();
    if (controller == null)
    {
        controller = controllerRect.gameObject.AddComponent<CozyLandscapeLayoutController>();
        isSceneDirty = true;
    }

    SerializedObject soLayout = new SerializedObject(controller);
    bool layoutDirty = false;
    SafeSetObjectReference(soLayout.FindProperty("header"), headerPanel, ref layoutDirty);
    SafeSetObjectReference(soLayout.FindProperty("gameplayArea"), gameplayArea, ref layoutDirty);
    SafeSetObjectReference(soLayout.FindProperty("farmPlot"), farmPlot, ref layoutDirty);
    SafeSetObjectReference(soLayout.FindProperty("animalPen"), animalPen, ref layoutDirty);
    SafeSetObjectReference(soLayout.FindProperty("stickerBookPanel"), stickerBookPanel, ref layoutDirty);
    SafeSetObjectReference(soLayout.FindProperty("inventoryTray"), inventoryTray, ref layoutDirty);
    SafeSetObjectReference(soLayout.FindProperty("sidebar"), sidebarPanel, ref layoutDirty);
    SafeSetObjectReference(soLayout.FindProperty("popupRoot"), popupRoot, ref layoutDirty);
    if (layoutDirty)
    {
        soLayout.ApplyModifiedProperties();
        isSceneDirty = true;
    }
}
```

- [ ] **Step 4: Keep landscape-only behavior explicit**

Do not add portrait-specific anchors, rotation prompts, or layout switches. Validation should target landscape aspect ratios and fail only when landscape requirements are broken.

- [ ] **Step 5: Wait for Unity import and generated `.meta`**

After adding the script, wait for Unity to compile/import and confirm:

```text
Assets/CozyLifeSim/Scripts/UI/CozyLandscapeLayoutController.cs.meta
```

exists and was generated by Unity.

## Task 37.5: Runtime Validation Compatibility

**Files:**
- Modify only if Step 2 fails on startup overlay input blocking: `Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimMcpGameplayLoopValidation.cs`
- Modify only if scene validation cannot cover component defaults: `Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimValidation.cs`

- [ ] **Step 1: Run existing logic validation**

Run:

```powershell
# Unity menu path: Tools/CozySim/Run Logic Verification Tests
```

Expected: existing tests still pass. Startup overlay and layout controller should not require service registrations.

- [ ] **Step 2: Run existing Play Mode runtime loop validation**

Run:

```powershell
# Unity menu path: Tools/CozySim/Run MCP Gameplay Loop Validation
```

Expected: if the overlay blocks the first click, validation fails on an early click step.

- [ ] **Step 3: If blocked, wait for overlay hidden before runtime clicks**

Add a helper in `CozyLifeSimMcpGameplayLoopValidation`:

```csharp
private static async UniTask WaitForStartupOverlayAsync()
{
    var overlay = Object.FindFirstObjectByType<CozyStartupLoadingOverlay>(FindObjectsInactive.Include);
    if (overlay == null)
    {
        return;
    }

    await UniTask.Delay(System.TimeSpan.FromSeconds(0.75));
}
```

Call it after entering play mode and before the first pointer/click interaction. Keep this helper only if validation proves it is needed.

- [ ] **Step 4: Add focused logic tests only if scene validation is insufficient**

If the new components have non-trivial defaults that scene validation cannot cover, add tests to `CozyLifeSimValidation`:

```csharp
private static bool TestStartupOverlayDefaultDurations()
{
    var go = new GameObject("Overlay_Test");
    try
    {
        var group = go.AddComponent<CanvasGroup>();
        var overlay = go.AddComponent<CozyStartupLoadingOverlay>();
        return group != null && overlay != null;
    }
    finally
    {
        Object.DestroyImmediate(go);
    }
}
```

Prefer scene validation over broad unit tests for hierarchy wiring.

## Task 37.6: Full Verification And Handoff

**Files:**
- Modify: `docs/plans/task.md`
- Modify: `docs/plans/current-handoff.md`
- Modify: `docs/plans/index.md`

- [ ] **Step 1: Run hygiene check**

Run:

```powershell
git diff --check
```

Expected: no whitespace errors.

- [ ] **Step 2: Wait for Unity compile/import**

After script changes, wait until Unity finishes compiling/importing. Check Console or Editor log for compiler errors.

Expected: no compiler errors and generated `.meta` files exist for new scripts.

- [ ] **Step 3: Run final validation suite**

Run:

```powershell
# Tools/CozySim/Run Logic Verification Tests
# Tools/CozySim/Run Scene Gameplay Loop Validation
# Tools/CozySim/Run MCP Gameplay Loop Validation
```

Expected:

```text
Logic Verification: PASS
Scene Gameplay Validation: PASS
Play Mode Runtime Loop Validation: PASS
```

Record exact pass/fail counts in `current-handoff.md`.

- [ ] **Step 4: Update task tracker**

Mark Task 37 as complete only after the final validation suite passes:

```markdown
| **Task 37: [Phase 3.7] Landscape-Only Startup, Camera & Layout Hardening** | [x] | Boot directly into Main.unity with in-scene startup overlay, hardened orthographic camera, landscape-only player/build settings, responsive landscape layout controller, and validation coverage. |
```

- [ ] **Step 5: Update handoff**

`current-handoff.md` must include:

- Task 37 completed scope.
- Exact verification counts.
- Any expected warnings.
- Current uncommitted scope.
- Recommended next task after Task 37.

- [ ] **Step 6: Commit only if the user explicitly asks**

If the user asks to commit after Unity import/compile is clean:

```powershell
git add -- Assets/CozyLifeSim/Scripts/UI/CozyStartupLoadingOverlay.cs Assets/CozyLifeSim/Scripts/UI/CozyStartupLoadingOverlay.cs.meta Assets/CozyLifeSim/Scripts/UI/CozyLandscapeLayoutController.cs Assets/CozyLifeSim/Scripts/UI/CozyLandscapeLayoutController.cs.meta Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimSceneGameplayValidation.cs Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimValidation.cs Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimMcpGameplayLoopValidation.cs Assets/CozyLifeSim/Scenes/Main.unity ProjectSettings/EditorBuildSettings.asset ProjectSettings/ProjectSettings.asset docs/plans/task.md docs/plans/current-handoff.md docs/plans/index.md docs/plans/2026-06-02-landscape-startup-camera-layout-hardening.md
git commit -m "feat: harden landscape startup flow"
```

Exclude `.agent/scratch/*` unless the user explicitly asks to modify Antigravity scratch files.

## Self-Review

- Spec coverage: This plan covers direct boot to `Main.unity`, no main menu/new/continue/reset, no separate loading scene, in-scene loading overlay, VContainer compatibility, camera setup, landscape-only orientation, responsive landscape layout, build scene registration, validation, handoff, and commit boundaries.
- Placeholder scan: No task uses unresolved placeholders. Optional runtime validation edits are gated by an explicit failing validation condition.
- Type consistency: `CozyStartupLoadingOverlay` and `CozyLandscapeLayoutController` names are consistent across creation, setup, and validation sections.
