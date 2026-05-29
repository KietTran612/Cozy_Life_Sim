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
            _database = AnimalDatabaseUtility.LoadOrCreateDatabase();

            if (_database != null)
            {
                _stagingAnimals.Clear();
                if (_database.Animals != null)
                {
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

            // Aesthetically pleasing top title banner
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                margin = new RectOffset(10, 10, 10, 10)
            };
            GUILayout.Label("Cozy Life Sim - Animal Database Designer", headerStyle);

            // Display Dirty Indicator, Sync info and Ping Button
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label($"Editing Asset: {AssetDatabase.GetAssetPath(_database)}", EditorStyles.miniLabel);
            
            if (GUILayout.Button("Ping Asset", EditorStyles.miniButton, GUILayout.Width(80)))
            {
                EditorGUIUtility.PingObject(_database);
                Selection.activeObject = _database;
            }

            if (_isDirty)
            {
                GUI.color = Color.yellow;
                GUILayout.Label("* Unsaved Draft Changes", EditorStyles.boldLabel);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

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
