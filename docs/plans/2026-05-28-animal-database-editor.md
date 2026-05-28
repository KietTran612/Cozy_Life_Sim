# Animal Database & Editor Implementation Plan

> **For Antigravity:** REQUIRED WORKFLOW: Use `.agent/workflows/execute-plan.md` to execute this plan in single-flow mode.

**Goal:** Establish a fully data-driven configuration for livestock animals by building an AnimalDatabase ScriptableObject, a professional custom copy-on-write staging Unity Editor Window, and refactoring AnimalWidget to load scale breathing and pet interaction configs dynamically from the database.

**Architecture:** Create `AnimalTemplate` and `AnimalDatabase` inside the `CozyLifeSim.UI` assembly under the `Settings` namespace. Build a staging Editor Window `AnimalEditorWindow` under `CozyLifeSim.Editor` with robust validation and automatic "Breathing Chicken" bootstrapping. Register `AnimalDatabase` safely via VContainer factory and refactor `AnimalWidget` to read templates dynamically and handle tweening safely with double-click protection.

**Tech Stack:** Unity 2D (C#), UGUI, VContainer (DI), TextMeshPro, DOTween, UnityEditor.

---

### Task 1: Implement Animal Database & Custom Editor Window

**Files:**
- Create: [AnimalTemplate.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Settings/AnimalTemplate.cs)
- Create: [AnimalDatabase.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Settings/AnimalDatabase.cs)
- Create: [AnimalEditorWindow.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/AnimalEditorWindow.cs)

**Step 1: Write AnimalTemplate class**
Create `Assets/CozyLifeSim/Scripts/UI/Settings/AnimalTemplate.cs`:
```csharp
using System;
using UnityEngine;

namespace CozyLifeSim.UI.Settings
{
    [Serializable]
    public class AnimalTemplate
    {
        public int AnimalId;
        public string Name;
        public Sprite Sprite;
        public float BreathScaleY = 1.03f;
        public float BreathDuration = 1.5f;
        public float PetJumpHeight = 25f;
        public float PetJumpDuration = 0.4f;
        public Sprite HeartFeedbackSprite;

        public AnimalTemplate() { }

        public AnimalTemplate(int animalId, string name, Sprite sprite, float breathScaleY, float breathDuration, float petJumpHeight, float petJumpDuration, Sprite heartFeedbackSprite)
        {
            AnimalId = animalId;
            Name = name;
            Sprite = sprite;
            BreathScaleY = breathScaleY;
            BreathDuration = breathDuration;
            PetJumpHeight = petJumpHeight;
            PetJumpDuration = petJumpDuration;
            HeartFeedbackSprite = heartFeedbackSprite;
        }
    }
}
```

**Step 2: Write AnimalDatabase ScriptableObject**
Create `Assets/CozyLifeSim/Scripts/UI/Settings/AnimalDatabase.cs` with validation:
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace CozyLifeSim.UI.Settings
{
    [CreateAssetMenu(fileName = "AnimalDatabase", menuName = "CozySim/Animal Database")]
    public class AnimalDatabase : ScriptableObject
    {
        public List<AnimalTemplate> Animals = new List<AnimalTemplate>();

        public AnimalTemplate GetAnimal(int animalId)
        {
            return Animals.Find(x => x != null && x.AnimalId == animalId);
        }

        public bool ValidateDatabase(out List<string> errors)
        {
            errors = new List<string>();
            HashSet<int> uniqueIds = new HashSet<int>();

            if (Animals == null)
            {
                errors.Add("Animal list is null.");
                return false;
            }

            for (int i = 0; i < Animals.Count; i++)
            {
                var a = Animals[i];
                if (a == null)
                {
                    errors.Add($"Animal at index {i} is null.");
                    continue;
                }

                if (uniqueIds.Contains(a.AnimalId))
                {
                    errors.Add($"Duplicate Animal ID found: {a.AnimalId} ('{a.Name}')");
                }
                else
                {
                    uniqueIds.Add(a.AnimalId);
                }

                if (string.IsNullOrWhiteSpace(a.Name))
                {
                    errors.Add($"Animal with ID {a.AnimalId} has an empty Name.");
                }

                if (a.Sprite == null)
                {
                    errors.Add($"Animal with ID {a.AnimalId} ('{a.Name}') is missing its main Sprite.");
                }

                if (a.BreathScaleY <= 1.0f)
                {
                    errors.Add($"Animal with ID {a.AnimalId} ('{a.Name}') has an invalid breath scale ({a.BreathScaleY}). Must be greater than 1.0f.");
                }

                if (a.BreathDuration <= 0f)
                {
                    errors.Add($"Animal with ID {a.AnimalId} ('{a.Name}') has an invalid breath duration ({a.BreathDuration}s). Must be > 0.");
                }

                if (a.PetJumpHeight < 0f)
                {
                    errors.Add($"Animal with ID {a.AnimalId} ('{a.Name}') has a negative jump height ({a.PetJumpHeight}). Height must be non-negative.");
                }

                if (a.PetJumpDuration <= 0f)
                {
                    errors.Add($"Animal with ID {a.AnimalId} ('{a.Name}') has an invalid jump duration ({a.PetJumpDuration}s). Duration must be greater than zero.");
                }
            }

            return errors.Count == 0;
        }
    }
}
```

**Step 3: Write AnimalEditorWindow custom Editor**
Create `Assets/CozyLifeSim/Scripts/Editor/AnimalEditorWindow.cs` with **automatic default bootstrapping/seeding** of the first "Breathing Chicken" animal and full `OnGUI` layout drawing:
```csharp
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using CozyLifeSim.UI.Settings;

namespace CozyLifeSim.Editor
{
    public class AnimalEditorWindow : EditorWindow
    {
        private AnimalDatabase _database;
        private List<AnimalTemplate> _stagingAnimals = new List<AnimalTemplate>();
        private int _selectedStagingIndex = -1;
        private List<string> _validationErrors = new List<string>();
        private bool _isDirty;
        private Vector2 _sidebarScroll;
        private Vector2 _detailsScroll;

        [MenuItem("Tools/CozySim/Animal Database Editor")]
        public static void ShowWindow()
        {
            GetWindow<AnimalEditorWindow>("Animal Database Editor");
        }

        private void OnEnable()
        {
            LoadOrCreateDatabase();
        }

        private void LoadOrCreateDatabase()
        {
            string[] guids = AssetDatabase.FindAssets("t:AnimalDatabase");
            if (guids == null || guids.Length == 0)
            {
                guids = AssetDatabase.FindAssets("AnimalDatabase");
            }
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _database = AssetDatabase.LoadAssetAtPath<AnimalDatabase>(path);
            }
            else
            {
                string dir = "Assets/CozyLifeSim/Settings";
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    AssetDatabase.Refresh();
                }
                string assetPath = $"{dir}/AnimalDatabase.asset";
                _database = ScriptableObject.CreateInstance<AnimalDatabase>();
                AssetDatabase.CreateAsset(_database, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"<color=green>[CozySim]</color> Created new AnimalDatabase asset at {assetPath}");
            }

            if (_database != null)
            {
                // Bootstrap default Animal database content if empty
                if (_database.Animals.Count == 0)
                {
                    var chickenSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Animals/Chicken-White-256.png");
                    var heartSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Hearts/Heart-Red-256.png");

                    // Fallback: If designated package sprites are null, auto-discover any available Sprite in the project
                    if (chickenSprite == null || heartSprite == null)
                    {
                        string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite");
                        if (spriteGuids != null && spriteGuids.Length > 0)
                        {
                            var fallbackSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(spriteGuids[0]));
                            if (chickenSprite == null) chickenSprite = fallbackSprite;
                            if (heartSprite == null) heartSprite = fallbackSprite;
                        }
                    }

                    _database.Animals.Add(new AnimalTemplate(1, "Breathing Chicken", chickenSprite, 1.03f, 1.5f, 25f, 0.4f, heartSprite));
                    EditorUtility.SetDirty(_database);
                    AssetDatabase.SaveAssets();
                    Debug.Log("<color=green>[CozySim]</color> Bootstrapped default 'Breathing Chicken' inside database (with safety fallbacks).");
                }

                _stagingAnimals.Clear();
                foreach (var animal in _database.Animals)
                {
                    if (animal != null)
                    {
                        _stagingAnimals.Add(new AnimalTemplate(
                            animal.AnimalId,
                            animal.Name,
                            animal.Sprite,
                            animal.BreathScaleY,
                            animal.BreathDuration,
                            animal.PetJumpHeight,
                            animal.PetJumpDuration,
                            animal.HeartFeedbackSprite
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
            for (int i = 0; i < _stagingAnimals.Count; i++)
            {
                var a = _stagingAnimals[i];
                if (a.AnimalId <= 0) _validationErrors.Add($"Index {i}: ID must be positive.");
                if (ids.Contains(a.AnimalId)) _validationErrors.Add($"Index {i}: Duplicate ID {a.AnimalId}.");
                else ids.Add(a.AnimalId);
                if (string.IsNullOrWhiteSpace(a.Name)) _validationErrors.Add($"ID {a.AnimalId}: Name cannot be empty.");
                if (a.Sprite == null) _validationErrors.Add($"ID {a.AnimalId}: Main Sprite must be assigned.");
                if (a.BreathScaleY <= 1.0f) _validationErrors.Add($"ID {a.AnimalId}: Breath Scale must be > 1.0.");
                if (a.BreathDuration <= 0f) _validationErrors.Add($"ID {a.AnimalId}: Breath duration must be > 0.");
                if (a.PetJumpHeight < 0f) _validationErrors.Add($"ID {a.AnimalId}: Pet jump height must be >= 0.");
                if (a.PetJumpDuration <= 0f) _validationErrors.Add($"ID {a.AnimalId}: Pet jump duration must be > 0.");
            }
        }

        private void OnGUI()
        {
            if (_database == null)
            {
                EditorGUILayout.HelpBox("Could not load or create Animal Database.", MessageType.Error);
                return;
            }

            EditorGUILayout.BeginHorizontal();

            // Left Sidebar
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            GUILayout.Label("Animals List", EditorStyles.boldLabel);
            _sidebarScroll = EditorGUILayout.BeginScrollView(_sidebarScroll, GUILayout.ExpandHeight(true));
            for (int i = 0; i < _stagingAnimals.Count; i++)
            {
                var animal = _stagingAnimals[i];
                string displayName = $"[{animal.AnimalId}] {(!string.IsNullOrEmpty(animal.Name) ? animal.Name : "Unnamed Animal")}";
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

            if (GUILayout.Button("Add New Animal", GUILayout.Height(30)))
            {
                int newId = 1;
                while (_stagingAnimals.Exists(x => x.AnimalId == newId)) newId++;
                _stagingAnimals.Add(new AnimalTemplate(newId, "New Animal", null, 1.03f, 1.5f, 25f, 0.4f, null));
                _selectedStagingIndex = _stagingAnimals.Count - 1;
                _isDirty = true;
                ValidateStaging();
            }
            EditorGUILayout.EndVertical();

            // Right Details Panel
            EditorGUILayout.BeginVertical();
            if (_selectedStagingIndex >= 0 && _selectedStagingIndex < _stagingAnimals.Count)
            {
                var selected = _stagingAnimals[_selectedStagingIndex];
                GUILayout.Label($"Animal Details: ID {selected.AnimalId}", EditorStyles.boldLabel);
                _detailsScroll = EditorGUILayout.BeginScrollView(_detailsScroll);

                int oldId = selected.AnimalId;
                selected.AnimalId = EditorGUILayout.IntField("Animal ID (Unique)", selected.AnimalId);
                if (selected.AnimalId != oldId) { _isDirty = true; ValidateStaging(); }

                string oldName = selected.Name;
                selected.Name = EditorGUILayout.TextField("Name", selected.Name);
                if (selected.Name != oldName) { _isDirty = true; ValidateStaging(); }

                Sprite oldSprite = selected.Sprite;
                selected.Sprite = (Sprite)EditorGUILayout.ObjectField("Main Sprite", selected.Sprite, typeof(Sprite), false);
                if (selected.Sprite != oldSprite) { _isDirty = true; ValidateStaging(); }

                float oldBScale = selected.BreathScaleY;
                selected.BreathScaleY = EditorGUILayout.FloatField("Breath Scale Y", selected.BreathScaleY);
                if (selected.BreathScaleY != oldBScale) { _isDirty = true; ValidateStaging(); }

                float oldBDur = selected.BreathDuration;
                selected.BreathDuration = EditorGUILayout.FloatField("Breath Duration (s)", selected.BreathDuration);
                if (selected.BreathDuration != oldBDur) { _isDirty = true; ValidateStaging(); }

                float oldJHeight = selected.PetJumpHeight;
                selected.PetJumpHeight = EditorGUILayout.FloatField("Pet Jump Height", selected.PetJumpHeight);
                if (selected.PetJumpHeight != oldJHeight) { _isDirty = true; ValidateStaging(); }

                float oldJDur = selected.PetJumpDuration;
                selected.PetJumpDuration = EditorGUILayout.FloatField("Pet Jump Duration (s)", selected.PetJumpDuration);
                if (selected.PetJumpDuration != oldJDur) { _isDirty = true; ValidateStaging(); }

                Sprite oldHeart = selected.HeartFeedbackSprite;
                selected.HeartFeedbackSprite = (Sprite)EditorGUILayout.ObjectField("Heart Pop Sprite", selected.HeartFeedbackSprite, typeof(Sprite), false);
                if (selected.HeartFeedbackSprite != oldHeart) { _isDirty = true; ValidateStaging(); }

                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space();
                if (GUILayout.Button("Delete Animal", GUILayout.Height(30)))
                {
                    _stagingAnimals.RemoveAt(_selectedStagingIndex);
                    _selectedStagingIndex = _stagingAnimals.Count - 1;
                    _isDirty = true;
                    ValidateStaging();
                }
            }
            else
            {
                GUILayout.Label("Select an Animal from the list to edit", EditorStyles.centeredGreyMiniLabel);
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
                GUILayout.Label("<color=green>✓ Database state is valid and aligned.</color>", new GUIStyle(EditorStyles.label) { richText = true });
            }

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = _isDirty && _validationErrors.Count == 0;
            if (GUILayout.Button("Save Changes", GUILayout.Height(40)))
            {
                _database.Animals.Clear();
                foreach (var s in _stagingAnimals)
                {
                    _database.Animals.Add(new AnimalTemplate(s.AnimalId, s.Name, s.Sprite, s.BreathScaleY, s.BreathDuration, s.PetJumpHeight, s.PetJumpDuration, s.HeartFeedbackSprite));
                }
                EditorUtility.SetDirty(_database);
                AssetDatabase.SaveAssets();
                _isDirty = false;
                Debug.Log("<color=green>[CozySim]</color> Animal Database saved successfully!");
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

### Task 2: Register Animal Database in GameLifetimeScope Safely (100% VContainer-Safe Nullable Factory)

**Files:**
- Modify: [GameLifetimeScope.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/GameLifetimeScope.cs)

**Step 1: Add AnimalDatabase Field and Safe Registration**
To prevent VContainer from failing dependency resolution when a database ScriptableObject field has not been wired in the Scene yet (is `null`), always register the database through a nullable-safe lambda factory.
Open `Assets/CozyLifeSim/Scripts/UI/GameLifetimeScope.cs`, add `using CozyLifeSim.UI.Settings;` at the top of the file, and declare the field + register it:
```csharp
        [SerializeField] private AnimalDatabase _animalDatabase;

        protected override void Configure(IContainerBuilder builder)
        {
            // Null-safe singleton register: always resolves to _animalDatabase (even if null) without exception
            builder.Register<AnimalDatabase>(resolver => _animalDatabase, Lifetime.Singleton);
            // ... (rest remains unchanged)
        }
```

---

### Task 3: Refactor AnimalWidget supporting dual Presenter + Optional Database Injection

**Files:**
- Modify: [AnimalWidget.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/AnimalWidget.cs)

**Step 1: Refactor AnimalWidget**
1. Add `using CozyLifeSim.UI.Settings;` at the top of the file to import the new settings namespace.
2. Inject both `AnimalPresenter` and `AnimalDatabase` (making `AnimalDatabase` optional).
3. Keep the critical `scope.Container.Inject(this)` inside `Start()`.
4. Treat `BreathScaleY` as a multiplier on top of Cached `_baseScale` instead of setting an absolute scale.
5. Add an `_animalVisual` image reference and apply `AnimalTemplate.Sprite` at runtime so the editor controls the animal's visible artwork.
6. Keep the click guard `_isPetting` check and safely register spawned heart sequence for destroy cleanup.
Open `Assets/CozyLifeSim/Scripts/UI/AnimalWidget.cs` and replace serialization fields & pet interactions:
```csharp
using CozyLifeSim.UI.Settings; // [P2 Fix] Import new Settings namespace

namespace CozyLifeSim.UI
{
    public class AnimalWidget : MonoBehaviour
    {
        [SerializeField] private int _animalId = 1;
        [SerializeField] private Image _animalVisual;
        [SerializeField] private Button _interactionButton;
        [SerializeField] private RectTransform _heartPrefab;
        [SerializeField] private Transform _spawnRoot;

        private AnimalPresenter _presenter;
        private AnimalDatabase _animalDatabase;
        private AnimalTemplate _animalTemplate;
        private Tween _breathTween;
        private Sequence _petSequence;
        private Vector3 _baseScale;
        private bool _isPetting = false; // Keep double-click guard
        private readonly List<Sequence> _heartSequences = new List<Sequence>(); // Tween cleanup registry

        [Inject]
        public void Construct(AnimalPresenter presenter, AnimalDatabase animalDatabase = null)
        {
            _presenter = presenter;
            _animalDatabase = animalDatabase;
            _animalTemplate = _animalDatabase != null ? _animalDatabase.GetAnimal(_animalId) : null;
        }

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        private void Start()
        {
            // Maintain critical runtime self-injection before setting up breathing values
            if (Application.isPlaying)
            {
                var scope = LifetimeScope.Find<GameLifetimeScope>();
                if (scope != null && scope.Container != null)
                {
                    scope.Container.Inject(this);
                }
            }

            // BreathScaleY acts as multiplier according to _baseScale.
            // Clamp runtime values as a final safety net in case an asset is edited outside the custom editor.
            float multiplier = Mathf.Max(1.001f, _animalTemplate != null ? _animalTemplate.BreathScaleY : 1.03f);
            float duration = Mathf.Max(0.05f, _animalTemplate != null ? _animalTemplate.BreathDuration : 1.5f);

            if (_animalVisual != null && _animalTemplate != null && _animalTemplate.Sprite != null)
            {
                _animalVisual.sprite = _animalTemplate.Sprite;
            }

            _breathTween = transform.DOScaleY(_baseScale.y * multiplier, duration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            if (_interactionButton != null)
            {
                _interactionButton.onClick.AddListener(PetAnimal);
            }
        }

        private void PetAnimal()
        {
            if (_isPetting) return; // Block click spam
            _isPetting = true;

            _breathTween?.Pause();
            _petSequence?.Kill(true);

            // Correctly invoke presenter to increment pet quest progress & reward coins
            _presenter?.PetAnimal();

            _petSequence = DOTween.Sequence();
            RectTransform rect = transform as RectTransform;

            // Clamp runtime values as a final safety net in case an asset is edited outside the custom editor.
            float jumpHeight = Mathf.Max(0f, _animalTemplate != null ? _animalTemplate.PetJumpHeight : 25f);
            float jumpDur = Mathf.Max(0.05f, _animalTemplate != null ? _animalTemplate.PetJumpDuration : 0.4f);

            // Avoid using the DOAnchorPosY extension which causes compile errors
            if (rect != null)
            {
                Vector2 startPos = rect.anchoredPosition;
                _petSequence.Append(TweenAnchoredY(rect, startPos.y + jumpHeight, jumpDur * 0.5f).SetEase(Ease.OutQuad));
                _petSequence.Append(TweenAnchoredY(rect, startPos.y, jumpDur * 0.5f).SetEase(Ease.InQuad));
            }
            else
            {
                // UI jump height is authored in anchored pixels. For non-UI transforms, convert to a small world-space jump.
                _petSequence.Append(transform.DOJump(transform.position, jumpHeight * 0.01f, 1, jumpDur));
            }

            if (_heartPrefab != null && _spawnRoot != null)
            {
                RectTransform heart = Instantiate(_heartPrefab, _spawnRoot);
                heart.localPosition = Vector2.zero;
                heart.localScale = Vector3.zero;

                if (_animalTemplate != null && _animalTemplate.HeartFeedbackSprite != null)
                {
                    var img = heart.GetComponent<Image>();
                    if (img != null) img.sprite = _animalTemplate.HeartFeedbackSprite;
                }

                Sequence heartSeq = DOTween.Sequence();
                _heartSequences.Add(heartSeq); // Register sequence for safe destruction cleanup

                heartSeq.Append(heart.DOScale(1.2f, 0.2f).SetEase(Ease.OutBack));
                heartSeq.Join(DOTween.To(() => heart.localPosition, pos => heart.localPosition = pos, new Vector3(0f, 60f, 0f), 0.6f).SetEase(Ease.OutQuad));

                if (!heart.TryGetComponent<CanvasGroup>(out var canvasGroup))
                {
                    canvasGroup = heart.gameObject.AddComponent<CanvasGroup>();
                }
                heartSeq.Insert(0.4f, canvasGroup.DOFade(0.0f, 0.2f));
                heartSeq.OnComplete(() => {
                    _heartSequences.Remove(heartSeq);
                    Destroy(heart.gameObject);
                });
            }

            _petSequence.OnComplete(() =>
            {
                transform.localScale = _baseScale;
                _isPetting = false;
                _breathTween?.Play();
            });
        }

        // Keep the existing compile-safe UGUI helper; avoid DOTween's RectTransform extension methods.
        private static Tween TweenAnchoredY(RectTransform rect, float endValue, float duration)
        {
            return DOTween.To(
                () => rect.anchoredPosition.y,
                y =>
                {
                    Vector2 position = rect.anchoredPosition;
                    position.y = y;
                    rect.anchoredPosition = position;
                },
                endValue,
                duration);
        }

        private void OnDestroy()
        {
            _breathTween?.Kill();
            _petSequence?.Kill();
            foreach (var seq in _heartSequences) seq?.Kill(); // Safe Tween cleanup on destroy
            _heartSequences.Clear();
            if (_interactionButton != null)
            {
                _interactionButton.onClick.RemoveListener(PetAnimal);
            }
        }
    }
}
```

---

### Task 4: Upgrade CozySceneSetupWindow to Scaffold Animal Database

**Files:**
- Modify: [CozySceneSetupWindow.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs)

**Step 1: Auto-discover and Wire Animal Database**
Open `Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs` and modify `GenerateScene()` to wire `AnimalDatabase`:
```csharp
            CozyLifeSim.UI.Settings.AnimalDatabase animalDb = null;
            string[] animalGuids = AssetDatabase.FindAssets("t:AnimalDatabase");
            if (animalGuids == null || animalGuids.Length == 0) animalGuids = AssetDatabase.FindAssets("AnimalDatabase");
            if (animalGuids != null && animalGuids.Length > 0)
            {
                animalDb = AssetDatabase.LoadAssetAtPath<CozyLifeSim.UI.Settings.AnimalDatabase>(AssetDatabase.GUIDToAssetPath(animalGuids[0]));
            }

            SerializedObject soScope = new SerializedObject(lifetimeScope);
            // ... (wire default configs)
            if (animalDb != null) soScope.FindProperty("_animalDatabase").objectReferenceValue = animalDb;
            soScope.ApplyModifiedProperties();

            // Also wire the generated Chicken_Visual Image into AnimalWidget._animalVisual so AnimalDatabase.Sprite drives the runtime artwork.
            SerializedObject soAnimal = new SerializedObject(animalWidget);
            soAnimal.FindProperty("_animalVisual").objectReferenceValue = chickenVisual;
            soAnimal.ApplyModifiedProperties();
```

---

## Verification Plan

### Automated Verification
- Wait for Unity to finish compiling and importing the newly created scripts.
- Ensure that the Unity Editor Console has zero warnings or compiler errors.
- Verify that Unity has automatically generated corresponding `.meta` files for all newly added files (`AnimalTemplate.cs`, `AnimalDatabase.cs`, `AnimalEditorWindow.cs`) in the workspace, and that they are ready to be included in any commit.
- Run `Tools/CozySim/Run Logic Verification Tests` and check that all persistence and services logic continue to compile and pass flawlessly.

### Manual Verification
1. **Animal Editor:** Open `Tools -> CozySim -> Animal Database Editor`. Verify that a staging list appears, default "Breathing Chicken" is bootstrapped (with safety fallbacks if package sprites are missing), changes are validated, and saving updates `AnimalDatabase.asset` perfectly.
2. **Dynamic Gameplay:**
   - In `Animal Database Editor`, change the Chicken's Main Sprite. Enter playmode and verify the animal visual changes to the database sprite.
   - In `Animal Database Editor`, change the Chicken's Breath Duration to `3s`. Enter playmode and verify the chicken breathes in 3-second intervals instead of 1.5s.
   - Assign a custom Heart Pop Sprite in the database, interact with the Chicken, verify that custom sprite appears instead of the default heart, and confirm quest pet progress increments.
   - Clear the Heart Pop Sprite, interact again, and verify the existing prefab heart visual still appears without errors.
