# Cozy Life Sim Core Architecture Implementation Plan

> **For Antigravity:** REQUIRED WORKFLOW: Use `.agent/workflows/execute-plan.md` to execute this plan in single-flow mode.

**Goal:** Establish the foundational high-performance C# framework for "Xóm Nhỏ Tuổi Thơ" using VContainer (DI), UniTask (allocation-free async), and UGUI Style & Prefab Swapping with real-time Editor previews.

**Architecture:** Build a clean layered MVC/MVP structure where services are registered via interfaces in VContainer. Structural UI customization is resolved using runtime Prefab Swapping, coupled with an `[ExecuteAlways]` preview script using `HideFlags.DontSave` to ensure comfortable WYSIWYG UI editing directly within the Unity Editor.

**Tech Stack:** Unity 2D (C#), VContainer, UniTask, TextMeshPro, UGUI, DOTween.

---

### Task 0: Initialize Unity 6000.3.11f1 Project and Install Packages

**Files:**
- Create: `Packages/manifest.json`
- Create: `ProjectSettings/ProjectVersion.txt`

**Step 1: Specify Unity Version**
Create `ProjectSettings/ProjectVersion.txt` to lock the project to Unity 6000.3.11f1:
```text
m_EditorVersion: 6000.3.11f1
m_EditorVersionWithRevision: 6000.3.11f1 (e3d548395551)
```

**Step 2: Configure UPM manifest.json with Dependencies**
Configure packages in `Packages/manifest.json` to include TextMeshPro, VContainer, and UniTask via git URLs:
```json
{
  "dependencies": {
    "com.unity.modules.ui": "1.0.0",
    "com.unity.modules.tilemap": "1.0.0",
    "com.unity.textmeshpro": "3.0.9",
    "jp.hadashia.vcontainer": "https://github.com/hadashiA/VContainer.git?path=src/VContainer/Assets/VContainer",
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
  }
}
```

**Step 3: Install and Configure DOTween**
Since DOTween is imported via the Asset Store or standard package DLLs rather than OpenUPM:
1. Import DOTween into the project (placed under `Assets/Plugins/Demigiant/DOTween`).
2. Open the DOTween Utility Panel (`Tools -> Demigiant -> DOTween Utility Panel`).
3. Click "Setup DOTween..." to compile the DLLs.
4. Ensure `DOTween` generates or is visible to Assembly Definitions so that `CozyLifeSim.UI` can reference it (under `Assets/Plugins/Demigiant/DOTween/DOTween.asmdef` if generated, or standard visual scripts).

**Step 4: Setup Project Directory Structure**
Create the base sub-folders under Assets for modularization:
- `Assets/CozyLifeSim/Scripts/Core`
- `Assets/CozyLifeSim/Scripts/UI`
- `Assets/CozyLifeSim/Scripts/UI/Style`
- `Assets/CozyLifeSim/Scripts/Editor`

**Step 5: Commit**
```bash
git add ProjectSettings/ProjectVersion.txt Packages/manifest.json
git commit -m "chore: initialize Unity 6000.3.11f1 project and packages"
```

---

### Task 1: Setup Assembly Definitions and Directory Structure

**Files:**
- Create: `Assets/CozyLifeSim/Scripts/Core/CozyLifeSim.Core.asmdef`
- Create: `Assets/CozyLifeSim/Scripts/UI/CozyLifeSim.UI.asmdef`
- Create: `Assets/CozyLifeSim/Scripts/Editor/CozyLifeSim.Editor.asmdef`

**Step 1: Create Core Assembly Definition**
The Core layer holds only C# pure domain models, data DTO structs (optimized value types like `StickerPlacedData`, `CropState`), and core interfaces. It does NOT reference TextMeshPro or presentation layers.
Create `Assets/CozyLifeSim/Scripts/Core/CozyLifeSim.Core.asmdef`:
```json
{
    "name": "CozyLifeSim.Core",
    "rootNamespace": "CozyLifeSim.Core",
    "references": [
        "UniTask"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

**Step 2: Create UI Assembly Definition**
The UI layer holds style configuration, theme adapters, and UGUI view structures. It references `CozyLifeSim.Core`, `VContainer`, `UniTask`, and `Unity.TextMeshPro`.
Create `Assets/CozyLifeSim/Scripts/UI/CozyLifeSim.UI.asmdef`:
```json
{
    "name": "CozyLifeSim.UI",
    "rootNamespace": "CozyLifeSim.UI",
    "references": [
        "CozyLifeSim.Core",
        "VContainer",
        "UniTask",
        "Unity.TextMeshPro"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

**Step 3: Create Editor-only Assembly Definition**
Create `Assets/CozyLifeSim/Scripts/Editor/CozyLifeSim.Editor.asmdef`:
```json
{
    "name": "CozyLifeSim.Editor",
    "rootNamespace": "CozyLifeSim.Editor",
    "references": [
        "CozyLifeSim.Core",
        "CozyLifeSim.UI"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

**Step 4: Commit**
```bash
git add Assets/CozyLifeSim/Scripts/Core/CozyLifeSim.Core.asmdef Assets/CozyLifeSim/Scripts/UI/CozyLifeSim.UI.asmdef Assets/CozyLifeSim/Scripts/Editor/CozyLifeSim.Editor.asmdef
git commit -m "feat: setup assembly boundaries"
```

---

### Task 2: Implement Style Config and TextStyle Abstractions in UI Assembly

**Files:**
- Create: `Assets/CozyLifeSim/Scripts/UI/Style/TextStyle.cs`
- Create: `Assets/CozyLifeSim/Scripts/UI/Style/UIStyleConfig.cs`
- Create: `Assets/CozyLifeSim/Scripts/UI/Style/IStyleService.cs`

**Step 1: Write `TextStyle` class**
TextStyle is a presentation config holding heap-allocated class references (`string` and `TMP_FontAsset`). Define it as a clean standard class rather than an aligned struct.
Create `Assets/CozyLifeSim/Scripts/UI/Style/TextStyle.cs`:
```csharp
using System;
using UnityEngine;
using TMPro;

namespace CozyLifeSim.UI.Style
{
    [Serializable]
    public class TextStyle
    {
        public string StyleKey;
        public TMP_FontAsset FontAsset;
        public Color Color;
        public float FontSizeOffset;
    }
}
```

**Step 2: Write `UIStyleConfig` ScriptableObject**
Create `Assets/CozyLifeSim/Scripts/UI/Style/UIStyleConfig.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace CozyLifeSim.UI.Style
{
    [CreateAssetMenu(fileName = "CozyUIStyleConfig", menuName = "CozySim/UI Style Config")]
    public class UIStyleConfig : ScriptableObject
    {
        [Header("Typography Palette")]
        public List<TextStyle> TextStyles = new List<TextStyle>();

        [Header("Structural Theme Prefabs")]
        public GameObject PrimaryButtonPrefab;
        public GameObject PanelBackgroundPrefab;

        public TextStyle GetTextStyle(string key)
        {
            return TextStyles.Find(x => x != null && x.StyleKey == key);
        }
    }
}
```

**Step 3: Write `IStyleService` interface**
Create `Assets/CozyLifeSim/Scripts/UI/Style/IStyleService.cs`:
```csharp
using System;

namespace CozyLifeSim.UI.Style
{
    public interface IStyleService
    {
        UIStyleConfig CurrentConfig { get; }
        event Action OnStyleChanged;
        void ApplyTheme(UIStyleConfig newConfig);
    }
}
```

**Step 4: Commit**
```bash
git add Assets/CozyLifeSim/Scripts/UI/Style/
git commit -m "feat: implement TextStyle and UIStyleConfig ScriptableObject"
```

---

### Task 3: Implement VContainer GameLifetimeScope and StyleService

**Files:**
- Create: `Assets/CozyLifeSim/Scripts/UI/Style/StyleService.cs`
- Create: `Assets/CozyLifeSim/Scripts/UI/GameLifetimeScope.cs`

**Step 1: Implement `StyleService`**
Create `Assets/CozyLifeSim/Scripts/UI/Style/StyleService.cs`:
```csharp
using System;

namespace CozyLifeSim.UI.Style
{
    public class StyleService : IStyleService
    {
        public UIStyleConfig CurrentConfig { get; private set; }
        public event Action OnStyleChanged;

        public StyleService(UIStyleConfig initialConfig)
        {
            CurrentConfig = initialConfig;
        }

        public void ApplyTheme(UIStyleConfig newConfig)
        {
            if (newConfig == null || CurrentConfig == newConfig) return;
            CurrentConfig = newConfig;
            OnStyleChanged?.Invoke();
        }
    }
}
```

**Step 2: Implement `GameLifetimeScope` in UI Assembly**
Create `Assets/CozyLifeSim/Scripts/UI/GameLifetimeScope.cs`:
```csharp
using UnityEngine;
using VContainer;
using VContainer.Unity;
using CozyLifeSim.UI.Style;

namespace CozyLifeSim.UI
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private UIStyleConfig _defaultStyleConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            // Register Style Service as Singleton in Presentation boundary
            builder.Register<IStyleService>(container => new StyleService(_defaultStyleConfig), Lifetime.Singleton);
        }
    }
}
```

**Step 3: Commit**
```bash
git add Assets/CozyLifeSim/Scripts/UI/Style/StyleService.cs Assets/CozyLifeSim/Scripts/UI/GameLifetimeScope.cs
git commit -m "feat: implement presentation VContainer Scope and StyleService"
```

---

### Task 4: Implement `UIStyleElement` with Font Size Caching

**Files:**
- Create: `Assets/CozyLifeSim/Scripts/UI/UIStyleElement.cs`

**Step 1: Write `UIStyleElement` with cached original font size**
To prevent cumulative growth bug (`fontSize += offset`), cache the original size during `Awake()` and use an absolute setter.
Create `Assets/CozyLifeSim/Scripts/UI/UIStyleElement.cs`:
```csharp
using UnityEngine;
using TMPro;
using VContainer;
using CozyLifeSim.UI.Style;

namespace CozyLifeSim.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class UIStyleElement : MonoBehaviour
    {
        [SerializeField] private string _styleKey;
        private IStyleService _styleService;
        private TextMeshProUGUI _textComponent;
        private float _baseFontSize;
        private bool _isInitialized;

        private void Awake()
        {
            _textComponent = GetComponent<TextMeshProUGUI>();
            if (_textComponent != null)
            {
                _baseFontSize = _textComponent.fontSize;
                _isInitialized = true;
            }
        }

        [Inject]
        public void Construct(IStyleService styleService)
        {
            _styleService = styleService;
            _styleService.OnStyleChanged += ApplyStyle;
            ApplyStyle();
        }

        private void ApplyStyle()
        {
            if (!_isInitialized || _styleService == null || _styleService.CurrentConfig == null || _textComponent == null) return;
            
            var textStyle = _styleService.CurrentConfig.GetTextStyle(_styleKey);
            if (textStyle != null)
            {
                _textComponent.font = textStyle.FontAsset;
                _textComponent.color = textStyle.Color;
                // Fix cumulative growth bug: always add offset to cached base font size
                _textComponent.fontSize = _baseFontSize + textStyle.FontSizeOffset;
            }
        }

        private void OnDestroy()
        {
            if (_styleService != null)
            {
                _styleService.OnStyleChanged -= ApplyStyle;
            }
        }
    }
}
```

**Step 2: Commit**
```bash
git add Assets/CozyLifeSim/Scripts/UI/UIStyleElement.cs
git commit -m "feat: implement UIStyleElement with absolute font-size cache fix"
```

---

### Task 5: Implement `CozyWidgetPlaceholder` with Explicit Instance Separation

**Files:**
- Create: `Assets/CozyLifeSim/Scripts/UI/CozyWidgetPlaceholder.cs`

**Step 1: Write `CozyWidgetPlaceholder` separating preview and runtime instances**
Ensure previous runtime instances are properly cleaned up in Play Mode when themes change, and preview instances are cleaned up immediately in Edit Mode.
Create `Assets/CozyLifeSim/Scripts/UI/CozyWidgetPlaceholder.cs`:
```csharp
using UnityEngine;
using VContainer;
using CozyLifeSim.UI.Style;

namespace CozyLifeSim.UI
{
    [ExecuteAlways]
    public class CozyWidgetPlaceholder : MonoBehaviour
    {
        public enum WidgetType
        {
            PrimaryButton,
            PanelBackground
        }

        [SerializeField] private WidgetType _widgetType;
        [SerializeField] private UIStyleConfig _editorOnlyConfig; // Used in editor mode to preview
        
        private IStyleService _styleService;
        private GameObject _previewInstance;
        private GameObject _runtimeInstance;

        [Inject]
        public void Construct(IStyleService styleService)
        {
            _styleService = styleService;
            _styleService.OnStyleChanged += RefreshWidget;
            RefreshWidget();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                GenerateEditorPreview();
            }
        }

        private void GenerateEditorPreview()
        {
            ClearPreview();
            if (_editorOnlyConfig == null) return;

            GameObject prefab = GetPrefabFromConfig(_editorOnlyConfig);
            if (prefab == null) return;

            _previewInstance = Instantiate(prefab, transform);
            // HideFlags.DontSave prevents this object from being written into Scene/Prefab asset files
            _previewInstance.hideFlags = HideFlags.DontSave;
        }

        private void RefreshWidget()
        {
            if (!Application.isPlaying) return;

            // Runtime Swapping
            ClearRuntimeInstance();
            if (_styleService == null || _styleService.CurrentConfig == null) return;

            GameObject prefab = GetPrefabFromConfig(_styleService.CurrentConfig);
            if (prefab == null) return;

            // Spawn actual theme prefab at runtime
            _runtimeInstance = Instantiate(prefab, transform);
            
            // Transfer RectTransform constraints
            RectTransform sourceRect = GetComponent<RectTransform>();
            RectTransform targetRect = _runtimeInstance.GetComponent<RectTransform>();
            if (sourceRect != null && targetRect != null)
            {
                targetRect.anchorMin = sourceRect.anchorMin;
                targetRect.anchorMax = sourceRect.anchorMax;
                targetRect.anchoredPosition = sourceRect.anchoredPosition;
                targetRect.sizeDelta = sourceRect.sizeDelta;
                targetRect.pivot = sourceRect.pivot;
                targetRect.localScale = sourceRect.localScale;
            }
        }

        private GameObject GetPrefabFromConfig(UIStyleConfig config)
        {
            return _widgetType switch
            {
                WidgetType.PrimaryButton => config.PrimaryButtonPrefab,
                WidgetType.PanelBackground => config.PanelBackgroundPrefab,
                _ => null
            };
        }

        private void ClearPreview()
        {
            if (_previewInstance != null)
            {
                DestroyImmediate(_previewInstance);
                _previewInstance = null;
            }

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.gameObject.hideFlags == HideFlags.DontSave)
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private void ClearRuntimeInstance()
        {
            if (_runtimeInstance != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_runtimeInstance);
                }
                else
                {
                    DestroyImmediate(_runtimeInstance);
                }
                _runtimeInstance = null;
            }
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                ClearPreview();
            }
        }

        private void OnDestroy()
        {
            if (_styleService != null)
            {
                _styleService.OnStyleChanged -= RefreshWidget;
            }
            ClearRuntimeInstance();
        }
    }
}
```

**Step 2: Commit**
```bash
git add Assets/CozyLifeSim/Scripts/UI/CozyWidgetPlaceholder.cs
git commit -m "feat: implement CozyWidgetPlaceholder with separate instance lifecycle"
```

