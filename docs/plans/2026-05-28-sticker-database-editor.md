# Sticker Database & Editor Implementation Plan

> **For Antigravity:** REQUIRED WORKFLOW: Use `.agent/workflows/execute-plan.md` to execute this plan in single-flow mode.

**Goal:** Establish a fully data-driven configuration for collectable stickers by building a StickerDatabase ScriptableObject, a professional custom copy-on-write staging Unity Editor Window, and refactoring StickerBook to dynamically spawn inventory tray stickers and restore placed ones from the database.

**Architecture:** Create `StickerTemplate` and `StickerDatabase` inside the `CozyLifeSim.UI` assembly under the `Settings` namespace. Build a staging Editor Window `StickerEditorWindow` under `CozyLifeSim.Editor` with robust validation and automatic "Bunny Pink" and "Bear" bootstrapping. Register `StickerDatabase` safely via VContainer factory and refactor `StickerBook` away from the current `_stickerTemplates` list to a single generic `CozySticker` template plus a database-driven inventory tray. Preserve existing page flip, presenter save/restore, and `FinalizePlacement(..., saveToDisk: false)` restore behavior. If no `StickerDatabase` is wired, `StickerBook` should keep page flipping functional and simply skip dynamic sticker spawning/restoration.

**Tech Stack:** Unity 2D (C#), UGUI, VContainer (DI), TextMeshPro, DOTween, UnityEditor.

---

### Task 1: Implement Sticker Database & Custom Editor Window

**Files:**
- Create: [StickerTemplate.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Settings/StickerTemplate.cs)
- Create: [StickerDatabase.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Settings/StickerDatabase.cs)
- Create: [StickerEditorWindow.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/StickerEditorWindow.cs)

**Step 1: Write StickerTemplate class**
Create `Assets/CozyLifeSim/Scripts/UI/Settings/StickerTemplate.cs`:
```csharp
using System;
using UnityEngine;

namespace CozyLifeSim.UI.Settings
{
    [Serializable]
    public class StickerTemplate
    {
        public int StickerId;
        public string Name;
        public Sprite Sprite;
        public Sprite ShadowSprite;

        public StickerTemplate() { }

        public StickerTemplate(int stickerId, string name, Sprite sprite, Sprite shadowSprite)
        {
            StickerId = stickerId;
            Name = name;
            Sprite = sprite;
            ShadowSprite = shadowSprite;
        }
    }
}
```

**Step 2: Write StickerDatabase ScriptableObject**
Create `Assets/CozyLifeSim/Scripts/UI/Settings/StickerDatabase.cs` with validation:
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace CozyLifeSim.UI.Settings
{
    [CreateAssetMenu(fileName = "StickerDatabase", menuName = "CozySim/Sticker Database")]
    public class StickerDatabase : ScriptableObject
    {
        public List<StickerTemplate> Stickers = new List<StickerTemplate>();

        public StickerTemplate GetSticker(int stickerId)
        {
            if (Stickers == null) return null;
            return Stickers.Find(x => x != null && x.StickerId == stickerId);
        }

        public bool ValidateDatabase(out List<string> errors)
        {
            errors = new List<string>();
            HashSet<int> uniqueIds = new HashSet<int>();

            if (Stickers == null)
            {
                errors.Add("Sticker list is null.");
                return false;
            }

            for (int i = 0; i < Stickers.Count; i++)
            {
                var s = Stickers[i];
                if (s == null)
                {
                    errors.Add($"Sticker at index {i} is null.");
                    continue;
                }

                if (s.StickerId <= 0)
                {
                    errors.Add($"Sticker at index {i} has an invalid ID ({s.StickerId}). ID must be positive.");
                }

                if (uniqueIds.Contains(s.StickerId))
                {
                    errors.Add($"Duplicate Sticker ID found: {s.StickerId} ('{s.Name}')");
                }
                else
                {
                    uniqueIds.Add(s.StickerId);
                }

                if (string.IsNullOrWhiteSpace(s.Name))
                {
                    errors.Add($"Sticker with ID {s.StickerId} has an empty Name.");
                }

                if (s.Sprite == null)
                {
                    errors.Add($"Sticker with ID {s.StickerId} ('{s.Name}') is missing its main Sprite.");
                }
            }

            return errors.Count == 0;
        }
    }
}
```

**Step 3: Write StickerEditorWindow custom Editor**
Create `Assets/CozyLifeSim/Scripts/Editor/StickerEditorWindow.cs` with **automatic default bootstrapping/seeding** of "Bunny Pink" and "Bear" stickers and full `OnGUI` layout drawing:
```csharp
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using CozyLifeSim.UI.Settings;

namespace CozyLifeSim.Editor
{
    public class StickerEditorWindow : EditorWindow
    {
        private StickerDatabase _database;
        private List<StickerTemplate> _stagingStickers = new List<StickerTemplate>();
        private int _selectedStagingIndex = -1;
        private List<string> _validationErrors = new List<string>();
        private bool _isDirty;
        private Vector2 _sidebarScroll;
        private Vector2 _detailsScroll;

        [MenuItem("Tools/CozySim/Sticker Database Editor")]
        public static void ShowWindow()
        {
            GetWindow<StickerEditorWindow>("Sticker Database Editor");
        }

        private void OnEnable()
        {
            LoadOrCreateDatabase();
        }

        private void LoadOrCreateDatabase()
        {
            string[] guids = AssetDatabase.FindAssets("t:StickerDatabase");
            if (guids == null || guids.Length == 0)
            {
                guids = AssetDatabase.FindAssets("StickerDatabase");
            }
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _database = AssetDatabase.LoadAssetAtPath<StickerDatabase>(path);
            }
            else
            {
                string dir = "Assets/CozyLifeSim/Settings";
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    AssetDatabase.Refresh();
                }
                string assetPath = $"{dir}/StickerDatabase.asset";
                _database = ScriptableObject.CreateInstance<StickerDatabase>();
                AssetDatabase.CreateAsset(_database, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"<color=green>[CozySim]</color> Created new StickerDatabase asset at {assetPath}");
            }

            if (_database != null)
            {
                if (_database.Stickers == null)
                {
                    _database.Stickers = new List<StickerTemplate>();
                    EditorUtility.SetDirty(_database);
                    AssetDatabase.SaveAssets();
                }

                // Bootstrap default Sticker database content if empty
                if (_database.Stickers.Count == 0)
                {
                    var bunnySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Animals/Bunny-Pink-256.png");
                    var bearSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Animals/Bear-256.png");

                    // Fallback: If designated package sprites are null, auto-discover any available Sprite in the project
                    if (bunnySprite == null || bearSprite == null)
                    {
                        string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite");
                        if (spriteGuids != null && spriteGuids.Length > 0)
                        {
                            var fallbackSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(spriteGuids[0]));
                            if (bunnySprite == null) bunnySprite = fallbackSprite;
                            if (bearSprite == null) bearSprite = fallbackSprite;
                        }
                    }

                    _database.Stickers.Add(new StickerTemplate(1, "Bunny Pink", bunnySprite, bunnySprite));
                    _database.Stickers.Add(new StickerTemplate(2, "Bear", bearSprite, bearSprite));
                    EditorUtility.SetDirty(_database);
                    AssetDatabase.SaveAssets();
                    Debug.Log("<color=green>[CozySim]</color> Bootstrapped default 'Bunny Pink' and 'Bear' stickers inside database (with safety fallbacks).");
                }

                _stagingStickers.Clear();
                foreach (var sticker in _database.Stickers)
                {
                    if (sticker != null)
                    {
                        _stagingStickers.Add(new StickerTemplate(
                            sticker.StickerId,
                            sticker.Name,
                            sticker.Sprite,
                            sticker.ShadowSprite
                        ));
                    }
                }
                _isDirty = false;
                ValidateStaging();
            }
        }

        private void ValidateStaging()
        {
            _validationErrors.Clear();
            HashSet<int> ids = new HashSet<int>();
            for (int i = 0; i < _stagingStickers.Count; i++)
            {
                var s = _stagingStickers[i];
                if (s == null)
                {
                    _validationErrors.Add($"Index {i}: Sticker entry is null.");
                    continue;
                }

                if (s.StickerId <= 0) _validationErrors.Add($"Index {i}: ID must be positive.");
                if (ids.Contains(s.StickerId)) _validationErrors.Add($"Index {i}: Duplicate ID {s.StickerId}.");
                else ids.Add(s.StickerId);
                if (string.IsNullOrWhiteSpace(s.Name)) _validationErrors.Add($"ID {s.StickerId}: Name cannot be empty.");
                if (s.Sprite == null) _validationErrors.Add($"ID {s.StickerId}: Sprite must be assigned.");
            }
        }

        private void OnGUI()
        {
            if (_database == null)
            {
                EditorGUILayout.HelpBox("Could not load or create Sticker Database.", MessageType.Error);
                return;
            }

            EditorGUILayout.BeginHorizontal();

            // Left Sidebar
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            GUILayout.Label("Stickers List", EditorStyles.boldLabel);
            _sidebarScroll = EditorGUILayout.BeginScrollView(_sidebarScroll, GUILayout.ExpandHeight(true));
            for (int i = 0; i < _stagingStickers.Count; i++)
            {
                var sticker = _stagingStickers[i];
                string displayName = $"[{sticker.StickerId}] {(!string.IsNullOrEmpty(sticker.Name) ? sticker.Name : "Unnamed Sticker")}";
                if (GUILayout.Toggle(_selectedStagingIndex == i, displayName, EditorStyles.miniButton, GUILayout.Height(25)))
                {
                    if (_selectedStagingIndex != i)
                    {
                        _selectedStagingIndex = i;
                        GUI.FocusControl(null);
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Add New Sticker", GUILayout.Height(30)))
            {
                int newId = 1;
                while (_stagingStickers.Exists(x => x.StickerId == newId)) newId++;
                _stagingStickers.Add(new StickerTemplate(newId, "New Sticker", null, null));
                _selectedStagingIndex = _stagingStickers.Count - 1;
                _isDirty = true;
                ValidateStaging();
            }
            EditorGUILayout.EndVertical();

            // Right Details Panel
            EditorGUILayout.BeginVertical();
            if (_selectedStagingIndex >= 0 && _selectedStagingIndex < _stagingStickers.Count)
            {
                var selected = _stagingStickers[_selectedStagingIndex];
                GUILayout.Label($"Sticker Details: ID {selected.StickerId}", EditorStyles.boldLabel);
                _detailsScroll = EditorGUILayout.BeginScrollView(_detailsScroll);

                int oldId = selected.StickerId;
                selected.StickerId = EditorGUILayout.IntField("Sticker ID (Unique)", selected.StickerId);
                if (selected.StickerId != oldId) { _isDirty = true; ValidateStaging(); }

                string oldName = selected.Name;
                selected.Name = EditorGUILayout.TextField("Name", selected.Name);
                if (selected.Name != oldName) { _isDirty = true; ValidateStaging(); }

                Sprite oldSprite = selected.Sprite;
                selected.Sprite = (Sprite)EditorGUILayout.ObjectField("Main Sprite", selected.Sprite, typeof(Sprite), false);
                if (selected.Sprite != oldSprite) { _isDirty = true; ValidateStaging(); }

                Sprite oldShadow = selected.ShadowSprite;
                selected.ShadowSprite = (Sprite)EditorGUILayout.ObjectField("Shadow/Silhouette Sprite", selected.ShadowSprite, typeof(Sprite), false);
                if (selected.ShadowSprite != oldShadow) { _isDirty = true; ValidateStaging(); }

                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space();
                if (GUILayout.Button("Delete Sticker", GUILayout.Height(30)))
                {
                    _stagingStickers.RemoveAt(_selectedStagingIndex);
                    _selectedStagingIndex = _stagingStickers.Count - 1;
                    _isDirty = true;
                    ValidateStaging();
                }
            }
            else
            {
                GUILayout.Label("Select a Sticker from the list to edit", EditorStyles.centeredGreyMiniLabel);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            // Bottom Save/Revert Area
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (_validationErrors.Count > 0)
            {
                GUILayout.Label("<color=red>Validation Warnings:</color>", new GUIStyle(EditorStyles.label) { richText = true });
                foreach (var err in _validationErrors) GUILayout.Label($"- {err}");
            }
            else
            {
                GUILayout.Label("<color=green>Database state is valid and aligned.</color>", new GUIStyle(EditorStyles.label) { richText = true });
            }

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = _isDirty && _validationErrors.Count == 0;
            if (GUILayout.Button("Save Changes", GUILayout.Height(40)))
            {
                ValidateStaging();
                if (_validationErrors.Count > 0)
                {
                    EditorUtility.DisplayDialog("Validation Error", "Cannot save StickerDatabase because there are configuration errors.", "OK");
                }
                else
                {
                    Undo.RecordObject(_database, "Save Sticker Database changes");
                    _database.Stickers.Clear();
                    foreach (var s in _stagingStickers)
                    {
                        _database.Stickers.Add(new StickerTemplate(s.StickerId, s.Name, s.Sprite, s.ShadowSprite));
                    }
                    EditorUtility.SetDirty(_database);
                    AssetDatabase.SaveAssets();
                    _isDirty = false;
                    Debug.Log("<color=green>[CozySim]</color> Sticker Database saved successfully!");
                    ShowNotification(new GUIContent("Database Saved!"));
                }
            }
            GUI.enabled = _isDirty;
            if (GUILayout.Button("Revert Changes", GUILayout.Height(40)))
            {
                LoadOrCreateDatabase();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
    }
}
```

---

### Task 2: Register Sticker Database in GameLifetimeScope Safely (100% VContainer-Safe Nullable Factory)

**Files:**
- Modify: [GameLifetimeScope.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/GameLifetimeScope.cs)

**Step 1: Add StickerDatabase Field and Safe Registration**
To prevent VContainer from failing dependency resolution when a database ScriptableObject field has not been wired in the Scene yet (is `null`), always register the database through a nullable-safe lambda factory.
Open `Assets/CozyLifeSim/Scripts/UI/GameLifetimeScope.cs`, add `using CozyLifeSim.UI.Settings;` at the top of the file, replace the fully qualified `CozyLifeSim.UI.Settings.QuestDatabase` field with `QuestDatabase` if desired, then declare the sticker database field and register it before presenters are registered:
```csharp
        [SerializeField] private QuestDatabase _questDatabase;
        [SerializeField] private StickerDatabase _stickerDatabase;

        protected override void Configure(IContainerBuilder builder)
        {
            // Null-safe singleton register: always resolves to _stickerDatabase (even if null) without exception
            builder.Register<StickerDatabase>(resolver => _stickerDatabase, Lifetime.Singleton);
            // Keep the existing service and presenter registrations unchanged.
        }
```

Keep the existing `QuestService`, `FarmPresenter`, `AnimalPresenter`, and `StickerBookPresenter` registrations in the same relative order. The sticker database registration only supplies an optional data asset; it must not replace or wrap `StickerBookPresenter`.

---

### Task 3: Refactor CozySticker and StickerBook supporting dual Presenter + Optional Database Injection

**Files:**
- Modify: [CozySticker.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/CozySticker.cs)
- Modify: [StickerBook.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/StickerBook.cs)

**Step 1: Refactor CozySticker Setup Graphic Binding**
Do not remove the current presenter injection, drag/drop logic, `TweenAnchorPos`, or `FinalizePlacement(Transform pageParent, Vector2 pageAnchoredPosition, int pageIndex, bool saveToDisk = true)` signature. Add a serialized `_visualImage` field and a `Setup(...)` method so database-spawned stickers can bind their graphics without replacing the root slot image.
Open `Assets/CozyLifeSim/Scripts/UI/CozySticker.cs` and add:
```csharp
namespace CozyLifeSim.UI
{
    public class CozySticker : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image _visualImage; // Optional serialized target

        public void Setup(int stickerId, Sprite mainSprite, Sprite shadowSprite)
        {
            _stickerId = stickerId;

            // Safely find the designated "Visual_Image" child first rather than replacing root slot image
            Image targetImg = _visualImage;
            if (targetImg == null)
            {
                Transform visualChild = transform.Find("Visual_Image");
                targetImg = visualChild != null ? visualChild.GetComponent<Image>() : GetComponent<Image>();
            }

            if (targetImg != null)
            {
                targetImg.sprite = mainSprite;
            }

            if (_shadowOffset != null)
            {
                var shadowImg = _shadowOffset.GetComponent<Image>();
                if (shadowImg != null)
                {
                    shadowImg.sprite = shadowSprite != null ? shadowSprite : mainSprite;
                    shadowImg.color = new Color(0f, 0f, 0f, 0.3f);
                }
            }
        }
```

Also update the scene setup wiring for the generic sticker template to assign `_visualImage` to the `Visual_Image` child. Keep `_shadowOffset` and `_canvasGroup` wired as they are now. `Setup(...)` must not call presenter methods or save data; it only assigns ID and sprites.

**Step 2: Refactor StickerBook**
1. Add `using CozyLifeSim.UI.Settings;` at the top of the file to import the new settings namespace.
2. Inject both `StickerBookPresenter` and `StickerDatabase` (making `StickerDatabase` optional).
3. Keep `scope.Container.Inject(this)` inside `Start()`.
4. Replace the current serialized `List<CozySticker> _stickerTemplates` with `_inventoryTrayRoot` and `_stickerPrefabTemplate`.
5. Preserve the existing private page-flip fields (`_currentPageIndex`, `_isTransitioning`, `_flipSequence`) plus `NextPage`, `PrevPage`, `FlipPage`, `UpdateNavigationButtons`, and button listener cleanup logic.
6. Refactor `RestoreStickers()` to query `StickerDatabase` by `StickerId` and safely instantiate the single generic `_stickerPrefabTemplate`, calling `Setup(...)` dynamically.
Open `Assets/CozyLifeSim/Scripts/UI/StickerBook.cs` and make these targeted changes:
```csharp
using CozyLifeSim.UI.Settings;

namespace CozyLifeSim.UI
{
    public class StickerBook : MonoBehaviour
    {
        [SerializeField] private List<StickerBookPage> _pages;
        [SerializeField] private RectTransform _flipPageIndicator;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _prevButton;
        
        [Header("Dynamic Templates")]
        [SerializeField] private Transform _inventoryTrayRoot;
        [SerializeField] private CozySticker _stickerPrefabTemplate; // Single generic prefab reference

        private StickerBookPresenter _presenter;
        private StickerDatabase _stickerDatabase;
        private readonly List<CozySticker> _spawnedInventoryStickers = new List<CozySticker>();

        [Inject]
        public void Construct(StickerBookPresenter presenter, StickerDatabase stickerDatabase = null)
        {
            _presenter = presenter;
            _stickerDatabase = stickerDatabase;
        }

        private void Start()
        {
            // Maintain critical self-injection
            if (Application.isPlaying)
            {
                var scope = LifetimeScope.Find<GameLifetimeScope>();
                if (scope != null && scope.Container != null)
                {
                    scope.Container.Inject(this);
                }
            }

            if (_pages == null || _pages.Count == 0)
            {
                if (_nextButton != null) _nextButton.interactable = false;
                if (_prevButton != null) _prevButton.interactable = false;
                return;
            }

            for (int i = 0; i < _pages.Count; i++)
            {
                if (_pages[i] != null)
                {
                    _pages[i].gameObject.SetActive(i == _currentPageIndex);
                }
            }

            if (_nextButton != null) _nextButton.onClick.AddListener(NextPage);
            if (_prevButton != null) _prevButton.onClick.AddListener(PrevPage);
            UpdateNavigationButtons();


            // Spawn inventory tray dynamically
            SpawnDynamicStickers();

            // Restore dynamically placed stickers using single template configured from the database
            RestoreStickers();
        }

        private void SpawnDynamicStickers()
        {
            if (_stickerDatabase == null || _inventoryTrayRoot == null || _stickerPrefabTemplate == null) return;
            if (_stickerDatabase.Stickers == null) return;

            foreach (var spawned in _spawnedInventoryStickers)
            {
                if (spawned != null)
                {
                    Destroy(spawned.gameObject);
                }
            }
            _spawnedInventoryStickers.Clear();

            // Hide the original editor setup template
            _stickerPrefabTemplate.gameObject.SetActive(false);

            foreach (var template in _stickerDatabase.Stickers)
            {
                if (template == null || template.Sprite == null) continue;

                var sticker = Instantiate(_stickerPrefabTemplate, _inventoryTrayRoot);
                sticker.gameObject.SetActive(true);

                // Setup sticker values dynamically
                sticker.Setup(template.StickerId, template.Sprite, template.ShadowSprite);
                _spawnedInventoryStickers.Add(sticker);
            }
        }

        private void RestoreStickers()
        {
            // Retrieve placed data and restore them dynamically using database sprites
            if (_presenter == null || _stickerDatabase == null || _stickerPrefabTemplate == null || _pages == null) return;
            if (_stickerDatabase.Stickers == null) return;

            var placedList = _presenter.GetPlacedStickers();
            foreach (var item in placedList)
            {
                StickerBookPage targetPage = _pages.Find(p => p != null && p.PageIndex == item.PageIndex);
                if (targetPage == null) continue;

                var template = _stickerDatabase.GetSticker(item.StickerId);
                if (template == null || template.Sprite == null) continue;

                CozySticker spawned = Instantiate(_stickerPrefabTemplate, targetPage.transform);
                spawned.gameObject.SetActive(true);
                
                // Configure graphics dynamically from database
                spawned.Setup(template.StickerId, template.Sprite, template.ShadowSprite);

                RectTransform rect = spawned.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchoredPosition = new Vector2(item.PositionX, item.PositionY);
                    rect.localScale = new Vector3(item.Scale, item.Scale, 1.0f);
                    rect.localRotation = Quaternion.Euler(0f, 0f, item.Rotation);
                    
                    // Finalize positioning with saveToDisk set to FALSE to eliminate PlayerPrefs save loops during loading
                    spawned.FinalizePlacement(targetPage.transform, rect.anchoredPosition, item.PageIndex, false);
                }
            }
        }

        private void OnDestroy()
        {
            _flipSequence?.Kill();
            if (_nextButton != null) _nextButton.onClick.RemoveListener(NextPage);
            if (_prevButton != null) _prevButton.onClick.RemoveListener(PrevPage);
        }
    }
}
```

---

### Task 4: Upgrade CozySceneSetupWindow to Scaffold Sticker Database

**Files:**
- Modify: [CozySceneSetupWindow.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs)

**Step 1: Auto-discover, Create, Bootstrap, and Wire Sticker Database**
Open `Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs` and modify `GenerateScene()` near the existing QuestDatabase lookup to also find or create `StickerDatabase`. Do not require the user to open `StickerEditorWindow` before running scene setup.
```csharp
            CozyLifeSim.UI.Settings.StickerDatabase stickerDb = null;
            string[] stickerGuids = AssetDatabase.FindAssets("t:StickerDatabase");
            if (stickerGuids == null || stickerGuids.Length == 0) stickerGuids = AssetDatabase.FindAssets("StickerDatabase");
            if (stickerGuids != null && stickerGuids.Length > 0)
            {
                stickerDb = AssetDatabase.LoadAssetAtPath<CozyLifeSim.UI.Settings.StickerDatabase>(AssetDatabase.GUIDToAssetPath(stickerGuids[0]));
            }
            else
            {
                string stickerDir = "Assets/CozyLifeSim/Settings";
                if (!System.IO.Directory.Exists(stickerDir))
                {
                    System.IO.Directory.CreateDirectory(stickerDir);
                    AssetDatabase.Refresh();
                }

                string stickerAssetPath = $"{stickerDir}/StickerDatabase.asset";
                stickerDb = ScriptableObject.CreateInstance<CozyLifeSim.UI.Settings.StickerDatabase>();
                AssetDatabase.CreateAsset(stickerDb, stickerAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            if (stickerDb != null && (stickerDb.Stickers == null || stickerDb.Stickers.Count == 0))
            {
                if (stickerDb.Stickers == null)
                {
                    stickerDb.Stickers = new List<CozyLifeSim.UI.Settings.StickerTemplate>();
                }

                Sprite bunnySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Animals/Bunny-Pink-256.png");
                Sprite bearSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Animals/Bear-256.png");
                if (bunnySprite == null || bearSprite == null)
                {
                    string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite");
                    if (spriteGuids != null && spriteGuids.Length > 0)
                    {
                        Sprite fallbackSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(spriteGuids[0]));
                        if (bunnySprite == null) bunnySprite = fallbackSprite;
                        if (bearSprite == null) bearSprite = fallbackSprite;
                    }
                }

                stickerDb.Stickers.Add(new CozyLifeSim.UI.Settings.StickerTemplate(1, "Bunny Pink", bunnySprite, bunnySprite));
                stickerDb.Stickers.Add(new CozyLifeSim.UI.Settings.StickerTemplate(2, "Bear", bearSprite, bearSprite));
                EditorUtility.SetDirty(stickerDb);
                AssetDatabase.SaveAssets();
            }

            SerializedObject soScope = new SerializedObject(lifetimeScope);
            // Keep the existing UIStyleConfig and QuestDatabase property wiring in this same SerializedObject block.
            SerializedProperty propStickerDb = soScope.FindProperty("_stickerDatabase");
            if (propStickerDb != null)
            {
                propStickerDb.objectReferenceValue = stickerDb;
            }
            soScope.ApplyModifiedProperties();
```

**Step 2: Replace static sticker-template list scaffolding with a single template**
The current scene setup creates two live stickers in `Inventory_Tray` and wires them into `StickerBook._stickerTemplates`. After refactoring `StickerBook`, that field no longer exists. Replace that block with:
1. Create or reuse one generic sticker template named `Sticker_Template` under the existing hidden `Prefabs_Holder`.
2. Give it the same `CozySticker`, `CanvasGroup`, `Shadow_Offset`, and `Visual_Image` child setup currently used by generated stickers.
3. Keep `Sticker_Template` inactive in edit mode after wiring is complete. It is a clone source, not a visible tray item.
4. Wire `_shadowOffset`, `_canvasGroup`, and `_visualImage` on the template's `CozySticker`.
5. Wire `StickerBook._inventoryTrayRoot` to `Inventory_Tray`.
6. Wire `StickerBook._stickerPrefabTemplate` to the generic `Sticker_Template`.
7. Use the same `SerializedObject soBook` that already wires `_pages`, `_flipPageIndicator`, `_nextButton`, and `_prevButton` to also wire `_inventoryTrayRoot` and `_stickerPrefabTemplate`, then call `ApplyModifiedProperties()` once for the book.
8. Do not wire or reference `_stickerTemplates`; it should be removed from `StickerBook`.
9. Destroy obsolete generated `Sticker_0` / `Sticker_1` children from `Inventory_Tray` so database-driven spawning does not duplicate them.

Use null-checked `SerializedProperty` assignments, matching the existing defensive style in this file, so old scenes do not break while Unity refreshes serialized fields.

---

## Verification Plan

### Automated Verification
- Wait for Unity to finish compiling and importing the newly created scripts.
- Ensure that the Unity Editor Console has zero warnings or compiler errors.
- Verify that Unity has automatically generated corresponding `.meta` files for all newly added files (`StickerTemplate.cs`, `StickerDatabase.cs`, `StickerEditorWindow.cs`) in the workspace, and that they are ready to be included in any commit.
- Run `Tools/CozySim/Run Logic Verification Tests` and check that all persistence and services logic continue to compile and pass flawlessly.
- Regenerate the test scene with `Tools/CozySim/Setup Test Scene` and confirm no `SerializedObject.FindProperty(...).objectReferenceValue` call throws because of renamed fields.

### Manual Verification
1. **Sticker Editor:** Open `Tools -> CozySim -> Sticker Database Editor`. Verify that a staging list appears, default "Bunny Pink" and "Bear" stickers are bootstrapped (with safety fallbacks if package sprites are missing), changes are validated, and saving updates `StickerDatabase.asset` perfectly.
2. **Dynamic Gameplay:**
   - In `Sticker Database Editor`, add a new custom sticker. Enter playmode and verify the new sticker dynamically appears in the inventory tray and is correctly restored in placement without any scene modification!
   - Drag a database-spawned sticker onto a page, restart playmode, and verify it restores from `StickerDatabase` using `FinalizePlacement(..., false)` without creating duplicate save entries.
