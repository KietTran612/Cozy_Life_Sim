# Crop Database & Editor Implementation Plan

> **For Antigravity:** REQUIRED WORKFLOW: Use `.agent/workflows/execute-plan.md` to execute this plan in single-flow mode.

**Goal:** Establish a fully data-driven configuration for agricultural crops by building a CropDatabase ScriptableObject, a professional custom copy-on-write staging Unity Editor Window, and refactoring CropWidget to load duration and sprite configs dynamically from the database.

**Architecture:** Create `CropTemplate` and `CropDatabase` inside the `CozyLifeSim.UI` assembly under a new `Settings` namespace. Build a staging Editor Window `CropEditorWindow` under `CozyLifeSim.Editor` with robust validation and automatic "White Acorn" bootstrapping. Register `CropDatabase` safely via VContainer factory and refactor `CropWidget` to read templates dynamically.

**Tech Stack:** Unity 2D (C#), UGUI, VContainer (DI), TextMeshPro, DOTween, UnityEditor.

---

### Task 1: Implement Crop Database & Custom Editor Window

**Files:**
- Create: [CropTemplate.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Settings/CropTemplate.cs)
- Create: [CropDatabase.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Settings/CropDatabase.cs)
- Create: [CropEditorWindow.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CropEditorWindow.cs)

**Step 1: Write CropTemplate class**
Create the serializable crop definition inside the `CozyLifeSim.UI` assembly to hold sprites and duration settings.
Create `Assets/CozyLifeSim/Scripts/UI/Settings/CropTemplate.cs`:
```csharp
using System;
using UnityEngine;

namespace CozyLifeSim.UI.Settings
{
    [Serializable]
    public class CropTemplate
    {
        public int CropId;
        public string Name;
        public float StageDurationSeconds;
        public Sprite SeedSprite;
        public Sprite SproutSprite;
        public Sprite MatureSprite;
        public Sprite HarvestSprite;

        public CropTemplate() { }

        public CropTemplate(int cropId, string name, float stageDurationSeconds, Sprite seedSprite, Sprite sproutSprite, Sprite matureSprite, Sprite harvestSprite)
        {
            CropId = cropId;
            Name = name;
            StageDurationSeconds = stageDurationSeconds;
            SeedSprite = seedSprite;
            SproutSprite = sproutSprite;
            MatureSprite = matureSprite;
            HarvestSprite = harvestSprite;
        }
    }
}
```

**Step 2: Write CropDatabase ScriptableObject**
Create `Assets/CozyLifeSim/Scripts/UI/Settings/CropDatabase.cs` with validation:
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace CozyLifeSim.UI.Settings
{
    [CreateAssetMenu(fileName = "CropDatabase", menuName = "CozySim/Crop Database")]
    public class CropDatabase : ScriptableObject
    {
        public List<CropTemplate> Crops = new List<CropTemplate>();

        public CropTemplate GetCrop(int cropId)
        {
            return Crops.Find(x => x != null && x.CropId == cropId);
        }

        public bool ValidateDatabase(out List<string> errors)
        {
            errors = new List<string>();
            HashSet<int> uniqueIds = new HashSet<int>();

            if (Crops == null)
            {
                errors.Add("Crop list is null.");
                return false;
            }

            for (int i = 0; i < Crops.Count; i++)
            {
                var c = Crops[i];
                if (c == null)
                {
                    errors.Add($"Crop at index {i} is null.");
                    continue;
                }

                if (uniqueIds.Contains(c.CropId))
                {
                    errors.Add($"Duplicate Crop ID found: {c.CropId} ('{c.Name}')");
                }
                else
                {
                    uniqueIds.Add(c.CropId);
                }

                if (string.IsNullOrWhiteSpace(c.Name))
                {
                    errors.Add($"Crop with ID {c.CropId} has an empty Name.");
                }

                if (c.StageDurationSeconds <= 0f)
                {
                    errors.Add($"Crop with ID {c.CropId} ('{c.Name}') has an invalid duration ({c.StageDurationSeconds}s). Duration must be greater than zero.");
                }

                if (c.SeedSprite == null || c.SproutSprite == null || c.MatureSprite == null || c.HarvestSprite == null)
                {
                    errors.Add($"Crop with ID {c.CropId} ('{c.Name}') is missing one or more Stage Sprites.");
                }
            }

            return errors.Count == 0;
        }
    }
}
```

**Step 3: Write CropEditorWindow staging Editor**
Create a copy-on-write custom editor under `CozyLifeSim.Editor`. Incorporate **automatic default bootstrapping/seeding** of the first "White Acorn" crop when creating a new database asset.
Create `Assets/CozyLifeSim/Scripts/Editor/CropEditorWindow.cs`:
```csharp
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using CozyLifeSim.UI.Settings;

namespace CozyLifeSim.Editor
{
    public class CropEditorWindow : EditorWindow
    {
        private CropDatabase _database;
        private List<CropTemplate> _stagingCrops = new List<CropTemplate>();
        private int _selectedStagingIndex = -1;
        private List<string> _validationErrors = new List<string>();
        private bool _isDirty;
        private Vector2 _sidebarScroll;
        private Vector2 _detailsScroll;

        [MenuItem("Tools/CozySim/Crop Database Editor")]
        public static void ShowWindow()
        {
            GetWindow<CropEditorWindow>("Crop Database Editor");
        }

        private void OnEnable()
        {
            LoadOrCreateDatabase();
        }

        private void LoadOrCreateDatabase()
        {
            string[] guids = AssetDatabase.FindAssets("t:CropDatabase");
            if (guids == null || guids.Length == 0)
            {
                guids = AssetDatabase.FindAssets("CropDatabase");
            }
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _database = AssetDatabase.LoadAssetAtPath<CropDatabase>(path);
            }
            else
            {
                string dir = "Assets/CozyLifeSim/Settings";
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    AssetDatabase.Refresh();
                }
                string assetPath = $"{dir}/CropDatabase.asset";
                _database = ScriptableObject.CreateInstance<CropDatabase>();
                AssetDatabase.CreateAsset(_database, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"<color=green>[CozySim]</color> Created new CropDatabase asset at {assetPath}");
            }

            if (_database != null)
            {
                // Bootstrap default Crop database content if empty
                if (_database.Crops.Count == 0)
                {
                    var seedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Plants/Acorn-256.png");
                    var sproutSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Plants/Sapling-256.png");
                    var matureSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Flowers/Flower-Tulip-Red-256.png");

                    // Fallback: If designated package sprites are null, auto-discover any available Sprite in the project
                    if (seedSprite == null || sproutSprite == null || matureSprite == null)
                    {
                        string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite");
                        if (spriteGuids != null && spriteGuids.Length > 0)
                        {
                            var fallbackSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(spriteGuids[0]));
                            if (seedSprite == null) seedSprite = fallbackSprite;
                            if (sproutSprite == null) sproutSprite = fallbackSprite;
                            if (matureSprite == null) matureSprite = fallbackSprite;
                        }
                    }

                    _database.Crops.Add(new CropTemplate(1, "White Acorn", 5f, seedSprite, sproutSprite, matureSprite, matureSprite));
                    EditorUtility.SetDirty(_database);
                    AssetDatabase.SaveAssets();
                    Debug.Log("<color=green>[CozySim]</color> Bootstrapped default 'White Acorn' crop inside database (with safety fallbacks).");
                }

                _stagingCrops.Clear();
                foreach (var crop in _database.Crops)
                {
                    if (crop != null)
                    {
                        _stagingCrops.Add(new CropTemplate(
                            crop.CropId,
                            crop.Name,
                            crop.StageDurationSeconds,
                            crop.SeedSprite,
                            crop.SproutSprite,
                            crop.MatureSprite,
                            crop.HarvestSprite
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
            for (int i = 0; i < _stagingCrops.Count; i++)
            {
                var c = _stagingCrops[i];
                if (c.CropId <= 0) _validationErrors.Add($"Index {i}: ID must be positive.");
                if (ids.Contains(c.CropId)) _validationErrors.Add($"Index {i}: Duplicate ID {c.CropId}.");
                else ids.Add(c.CropId);
                if (string.IsNullOrWhiteSpace(c.Name)) _validationErrors.Add($"ID {c.CropId}: Name cannot be empty.");
                if (c.StageDurationSeconds <= 0f) _validationErrors.Add($"ID {c.CropId}: Stage duration must be > 0.");
                if (c.SeedSprite == null || c.SproutSprite == null || c.MatureSprite == null || c.HarvestSprite == null)
                    _validationErrors.Add($"ID {c.CropId}: Missing stage sprites.");
            }
        }

        private void OnGUI()
        {
            if (_database == null)
            {
                EditorGUILayout.HelpBox("Could not load or create Crop Database.", MessageType.Error);
                return;
            }

            EditorGUILayout.BeginHorizontal();

            // Left Sidebar
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            GUILayout.Label("Crops List", EditorStyles.boldLabel);
            _sidebarScroll = EditorGUILayout.BeginScrollView(_sidebarScroll, GUILayout.ExpandHeight(true));
            for (int i = 0; i < _stagingCrops.Count; i++)
            {
                var crop = _stagingCrops[i];
                string displayName = $"[{crop.CropId}] {(!string.IsNullOrEmpty(crop.Name) ? crop.Name : "Unnamed Crop")}";
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

            if (GUILayout.Button("Add New Crop", GUILayout.Height(30)))
            {
                int newId = 1;
                while (_stagingCrops.Exists(x => x.CropId == newId)) newId++;
                _stagingCrops.Add(new CropTemplate(newId, "New Crop", 5f, null, null, null, null));
                _selectedStagingIndex = _stagingCrops.Count - 1;
                _isDirty = true;
                ValidateStaging();
            }
            EditorGUILayout.EndVertical();

            // Right Details Panel
            EditorGUILayout.BeginVertical();
            if (_selectedStagingIndex >= 0 && _selectedStagingIndex < _stagingCrops.Count)
            {
                var selected = _stagingCrops[_selectedStagingIndex];
                GUILayout.Label($"Crop Details: ID {selected.CropId}", EditorStyles.boldLabel);
                _detailsScroll = EditorGUILayout.BeginScrollView(_detailsScroll);

                int oldId = selected.CropId;
                selected.CropId = EditorGUILayout.IntField("Crop ID (Unique)", selected.CropId);
                if (selected.CropId != oldId) { _isDirty = true; ValidateStaging(); }

                string oldName = selected.Name;
                selected.Name = EditorGUILayout.TextField("Name", selected.Name);
                if (selected.Name != oldName) { _isDirty = true; ValidateStaging(); }

                float oldDur = selected.StageDurationSeconds;
                selected.StageDurationSeconds = EditorGUILayout.FloatField("Stage Duration (s)", selected.StageDurationSeconds);
                if (selected.StageDurationSeconds != oldDur) { _isDirty = true; ValidateStaging(); }

                EditorGUILayout.Space();
                GUILayout.Label("Sprites Configurations", EditorStyles.boldLabel);
                
                Sprite oldSeed = selected.SeedSprite;
                selected.SeedSprite = (Sprite)EditorGUILayout.ObjectField("Seed Sprite", selected.SeedSprite, typeof(Sprite), false);
                if (selected.SeedSprite != oldSeed) { _isDirty = true; ValidateStaging(); }

                Sprite oldSprout = selected.SproutSprite;
                selected.SproutSprite = (Sprite)EditorGUILayout.ObjectField("Sprout Sprite", selected.SproutSprite, typeof(Sprite), false);
                if (selected.SproutSprite != oldSprout) { _isDirty = true; ValidateStaging(); }

                Sprite oldMature = selected.MatureSprite;
                selected.MatureSprite = (Sprite)EditorGUILayout.ObjectField("Mature Sprite", selected.MatureSprite, typeof(Sprite), false);
                if (selected.MatureSprite != oldMature) { _isDirty = true; ValidateStaging(); }

                Sprite oldHarvest = selected.HarvestSprite;
                selected.HarvestSprite = (Sprite)EditorGUILayout.ObjectField("Harvest Sprite", selected.HarvestSprite, typeof(Sprite), false);
                if (selected.HarvestSprite != oldHarvest) { _isDirty = true; ValidateStaging(); }

                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space();
                if (GUILayout.Button("Delete Crop", GUILayout.Height(30)))
                {
                    _stagingCrops.RemoveAt(_selectedStagingIndex);
                    _selectedStagingIndex = _stagingCrops.Count - 1;
                    _isDirty = true;
                    ValidateStaging();
                }
            }
            else
            {
                GUILayout.Label("Select a Crop from the list to edit", EditorStyles.centeredGreyMiniLabel);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            // Bottom Validation / Save Panel
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (_validationErrors.Count > 0)
            {
                GUILayout.Label("<color=red>Validation Warnings:</color>", new GUIStyle(EditorStyles.label) { richText = true });
                foreach (var err in _validationErrors) GUILayout.Label($"- {err}");
            }
            else
            {
                GUILayout.Label("<color=green>✓ Database state is valid and aligned.</color>", new GUIStyle(EditorStyles.label) { richText = true });
            }

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = _isDirty && _validationErrors.Count == 0;
            if (GUILayout.Button("Save Changes", GUILayout.Height(40)))
            {
                _database.Crops.Clear();
                foreach (var s in _stagingCrops)
                {
                    _database.Crops.Add(new CropTemplate(s.CropId, s.Name, s.StageDurationSeconds, s.SeedSprite, s.SproutSprite, s.MatureSprite, s.HarvestSprite));
                }
                EditorUtility.SetDirty(_database);
                AssetDatabase.SaveAssets();
                _isDirty = false;
                Debug.Log("<color=green>[CozySim]</color> Crop Database saved successfully!");
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

### Task 2: Register Crop Database in GameLifetimeScope Safely (100% VContainer-Safe Nullable Factory)

**Files:**
- Modify: [GameLifetimeScope.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/GameLifetimeScope.cs)

**Step 1: Add CropDatabase Field and Safe Registration**
To prevent VContainer from failing dependency resolution when a database ScriptableObject field has not been wired in the Scene yet (is `null`), always register the database through a nullable-safe lambda factory.
Open `Assets/CozyLifeSim/Scripts/UI/GameLifetimeScope.cs`, add `using CozyLifeSim.UI.Settings;` at the top of the file, and declare the field + register it:
```csharp
        [SerializeField] private CropDatabase _cropDatabase;

        protected override void Configure(IContainerBuilder builder)
        {
            // Null-safe singleton register: always resolves to _cropDatabase (even if null) without exception
            builder.Register<CropDatabase>(resolver => _cropDatabase, Lifetime.Singleton);
            // ... (rest remains unchanged)
        }
```

---

### Task 3: Refactor CropWidget supporting dual Presenter + Optional Database Injection and Caching

**Files:**
- Modify: [CropWidget.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/CropWidget.cs)

**Step 1: Refactor CropWidget**
1. Add `using CozyLifeSim.UI.Settings;` at the top of the file to import the new settings namespace.
2. Inject both `FarmPresenter` and `CropDatabase` (making `CropDatabase` optional).
3. Ensure the serialized fallback fields (`_stageDurationSeconds`, `_seedSprite`, etc.) are retained in the class so the widget falls back cleanly if no database is assigned.
4. Add a `private float GetStageDuration()` helper method and use it in `Start()`, `PlantSeed()`, `HarvestCrop()`, and `AdvanceStage()` so the database duration updates correctly on replanting and advancing.
5. Ensure the essential self-injection call `scope.Container.Inject(this)` is kept intact at the top of the `Start()` method.
6. Start the crop growth loops at `-1` to retain the dynamic "empty soil" initial state.
7. Keep the plant button listener bound to `PlantSeed` (matching the current method name perfectly).

Open `Assets/CozyLifeSim/Scripts/UI/CropWidget.cs` and replace serialization fields & methods:
```csharp
using CozyLifeSim.UI.Settings; // [P2 Fix] Import new Settings namespace

namespace CozyLifeSim.UI
{
    public class CropWidget : MonoBehaviour
    {
        [SerializeField] private int _cropId;
        // [P1 Fix] KEEP all serialized fallback fields intact in class
        [SerializeField] private float _stageDurationSeconds = 5f; 

        [Header("UI Visuals")]
        [SerializeField] private Image _cropVisual;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private Button _waterButton;
        [SerializeField] private RectTransform _wateringCan;
        [SerializeField] private Button _plantButton;
        [SerializeField] private Button _harvestButton;

        [Header("Stage Sprites")]
        [SerializeField] private Sprite _seedSprite;
        [SerializeField] private Sprite _sproutSprite;
        [SerializeField] private Sprite _matureSprite;
        [SerializeField] private Sprite _harvestSprite;

        private FarmPresenter _presenter;
        private CropDatabase _cropDatabase;
        private CropTemplate _cropTemplate;
        private CropState _state;
        private CancellationTokenSource _cts;
        private bool _isWatering = false;

        [Inject]
        public void Construct(FarmPresenter presenter, CropDatabase cropDatabase = null)
        {
            _presenter = presenter;
            _cropDatabase = cropDatabase;
            _cropTemplate = _cropDatabase != null ? _cropDatabase.GetCrop(_cropId) : null;
        }

        // [P1 Fix] Add duration helper dynamically querying database or local fallback (clamped to >= 1s for safety)
        private float GetStageDuration()
        {
            float rawDuration = _cropTemplate != null ? _cropTemplate.StageDurationSeconds : _stageDurationSeconds;
            return Mathf.Max(1f, rawDuration);
        }

        private void Start()
        {
            // Maintain critical runtime self-injection before setting up crop template configuration
            if (Application.isPlaying)
            {
                var scope = LifetimeScope.Find<GameLifetimeScope>();
                if (scope != null && scope.Container != null)
                {
                    scope.Container.Inject(this);
                }
            }

            // [P1 Fix] Retrieve stage duration via safe GetStageDuration helper
            float duration = GetStageDuration();
            _state = new CropState(_cropId, -1, duration, false);
            UpdateVisuals();

            if (_waterButton != null) _waterButton.onClick.AddListener(Irrigate);
            // Call PlantSeed (correct method name) rather than PlantCrop
            if (_plantButton != null) _plantButton.onClick.AddListener(PlantSeed);
            if (_harvestButton != null) _harvestButton.onClick.AddListener(HarvestCrop);

            _cts = new CancellationTokenSource();
            RunGrowthTimer(_cts.Token).Forget();
        }

        // [P1 Fix] Update duration in PlantSeed to query database dynamic value safely
        private void PlantSeed()
        {
            if (_state.GrowthStage != -1 || _presenter == null) return;

            if (_presenter.TryPlantCrop())
            {
                _state = new CropState(_cropId, 0, GetStageDuration(), false);
                UpdateVisuals();
            }
        }

        // [P1 Fix] Update duration in HarvestCrop to query database dynamic value safely
        private void HarvestCrop()
        {
            if (_state.GrowthStage < 3 || _presenter == null) return;

            _presenter.HarvestCrop();

            // Reset plot back to empty soil (-1)
            _state = new CropState(_cropId, -1, GetStageDuration(), false);
            UpdateVisuals();
        }

        // [P1 Fix] Update duration in AdvanceStage to query database dynamic value safely
        private void AdvanceStage()
        {
            int nextStage = Mathf.Min(_state.GrowthStage + 1, 3);
            _state = new CropState(_state.CropId, nextStage, GetStageDuration(), false);

            if (_cropVisual != null)
            {
                _cropFeedbackTween?.Kill();
                _cropFeedbackTween = _cropVisual.transform
                    .DOPunchScale(new Vector3(0.3f, 0.3f, 0f), 0.5f, 10, 1f);
            }
        }

        private void UpdateVisuals()
        {
            if (_cropVisual != null)
            {
                if (_state.GrowthStage == -1)
                {
                    _cropVisual.sprite = null; // empty soil
                    _cropVisual.enabled = false;
                }
                else
                {
                    _cropVisual.enabled = true;
                    if (_cropTemplate != null)
                    {
                        _cropVisual.sprite = _state.GrowthStage switch
                        {
                            0 => _cropTemplate.SeedSprite,
                            1 => _cropTemplate.SproutSprite,
                            2 => _cropTemplate.MatureSprite,
                            3 => _cropTemplate.HarvestSprite,
                            _ => _cropTemplate.SeedSprite
                        };
                    }
                    else
                    {
                        // Fallback sprites
                        _cropVisual.sprite = _state.GrowthStage switch
                        {
                            0 => _seedSprite,
                            1 => _sproutSprite,
                            2 => _matureSprite,
                            3 => _harvestSprite,
                            _ => _seedSprite
                        };
                    }
                }
            }
        }
    }
}
```

---

### Task 4: Upgrade CozySceneSetupWindow to Scaffold Crop Database

**Files:**
- Modify: [CozySceneSetupWindow.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs)

**Step 1: Auto-discover and Wire Crop Database**
Open `Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs` and modify `GenerateScene()` to wire `CropDatabase`:
```csharp
            CozyLifeSim.UI.Settings.CropDatabase cropDb = null;
            string[] cropGuids = AssetDatabase.FindAssets("t:CropDatabase");
            if (cropGuids == null || cropGuids.Length == 0) cropGuids = AssetDatabase.FindAssets("CropDatabase");
            if (cropGuids != null && cropGuids.Length > 0)
            {
                cropDb = AssetDatabase.LoadAssetAtPath<CozyLifeSim.UI.Settings.CropDatabase>(AssetDatabase.GUIDToAssetPath(cropGuids[0]));
            }

            SerializedObject soScope = new SerializedObject(lifetimeScope);
            // ... (wire default configs)
            if (cropDb != null) soScope.FindProperty("_cropDatabase").objectReferenceValue = cropDb;
            soScope.ApplyModifiedProperties();
```

---

## Verification Plan

### Automated Verification
- Wait for Unity to finish compiling and importing the newly created scripts.
- Ensure that the Unity Editor Console has zero warnings or compiler errors.
- Verify that Unity has automatically generated corresponding `.meta` files for all newly added files (`CropTemplate.cs`, `CropDatabase.cs`, `CropEditorWindow.cs`) in the workspace, and that they are ready to be included in any commit.
- Run `Tools/CozySim/Run Logic Verification Tests` and check that all persistence and services logic continue to compile and pass flawlessly.

### Manual Verification
1. **Crop Editor:** Open `Tools -> CozySim -> Crop Database Editor`. Verify that a staging list appears, default "White Acorn" is bootstrapped (with safety fallbacks if package sprites are missing), changes are validated, and saving updates `CropDatabase.asset` perfectly.
2. **Dynamic Gameplay:**
   - In `Crop Database Editor`, change the White Acorn duration to `3s`. Enter playmode and verify the crop grows in 3-second intervals instead of 5s, confirming the stage duration helper works correctly in Start, Plant, Advance, and Harvest actions.
