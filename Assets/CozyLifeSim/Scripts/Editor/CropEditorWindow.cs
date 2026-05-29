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
            _database = CropDatabaseUtility.LoadOrCreateDatabase();

            if (_database != null)
            {
                _stagingCrops.Clear();
                if (_database.Crops != null)
                {
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

            // Aesthetically pleasing top title banner
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                margin = new RectOffset(10, 10, 10, 10)
            };
            GUILayout.Label("Cozy Life Sim - Crop Database Designer", headerStyle);

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
