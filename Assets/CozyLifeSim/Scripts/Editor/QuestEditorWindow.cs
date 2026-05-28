using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using CozyLifeSim.Core;
using CozyLifeSim.UI.Settings;

namespace CozyLifeSim.Editor
{
    public class QuestEditorWindow : EditorWindow
    {
        private QuestDatabase _database;
        private List<QuestTemplate> _stagingQuests = new List<QuestTemplate>();
        private int _selectedStagingIndex = -1;
        private List<string> _validationErrors = new List<string>();
        private bool _isDirty;
        private Vector2 _sidebarScroll;
        private Vector2 _detailsScroll;

        [MenuItem("Tools/CozySim/Quest Database Editor")]
        public static void ShowWindow()
        {
            GetWindow<QuestEditorWindow>("Quest Database Editor");
        }

        private void OnEnable()
        {
            LoadOrCreateDatabase();
        }

        private void LoadOrCreateDatabase()
        {
            // Auto search asset database for QuestDatabase
            string[] guids = AssetDatabase.FindAssets("t:QuestDatabase");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _database = AssetDatabase.LoadAssetAtPath<QuestDatabase>(path);
            }
            else
            {
                // Auto create default
                string dir = "Assets/CozyLifeSim/Settings";
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    AssetDatabase.Refresh();
                }
                string assetPath = $"{dir}/QuestDatabase.asset";
                _database = ScriptableObject.CreateInstance<QuestDatabase>();
                AssetDatabase.CreateAsset(_database, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"<color=green>[CozySim]</color> Created new QuestDatabase asset at {assetPath}");
            }

            if (_database != null && _database.Quests.Count == 0)
            {
                _database.Quests.Add(new QuestTemplate(1, "Water 3 Crops", 3, 50, QuestType.WaterCrops));
                _database.Quests.Add(new QuestTemplate(2, "Harvest 2 Mature Crops", 2, 80, QuestType.HarvestCrops));
                _database.Quests.Add(new QuestTemplate(3, "Pet the Breathing Chicken 5 times", 5, 40, QuestType.PetAnimal));
                EditorUtility.SetDirty(_database);
                AssetDatabase.SaveAssets();
                Debug.Log("<color=green>[CozySim]</color> Automatically populated empty QuestDatabase with 3 default quests.");
            }

            // Sync from Database to Staging copy-on-write nháp
            SyncToStaging();
        }

        private void SyncToStaging()
        {
            _stagingQuests.Clear();
            _selectedStagingIndex = -1;
            _isDirty = false;

            if (_database != null && _database.Quests != null)
            {
                foreach (var q in _database.Quests)
                {
                    if (q != null)
                    {
                        // Clone DTO object to keep it entirely in-memory staged copy
                        _stagingQuests.Add(new QuestTemplate(q.QuestId, q.Title, q.TargetCount, q.RewardCoins, q.Type));
                    }
                }
            }

            if (_stagingQuests.Count > 0)
            {
                _selectedStagingIndex = 0;
            }

            RunStagingValidation();
        }

        private void RunStagingValidation()
        {
            _validationErrors.Clear();
            HashSet<int> ids = new HashSet<int>();

            for (int i = 0; i < _stagingQuests.Count; i++)
            {
                var q = _stagingQuests[i];
                if (q == null) continue;

                if (ids.Contains(q.QuestId))
                {
                    _validationErrors.Add($"Duplicate Quest ID found: {q.QuestId} ('{q.Title}')");
                }
                else
                {
                    ids.Add(q.QuestId);
                }

                if (string.IsNullOrWhiteSpace(q.Title))
                {
                    _validationErrors.Add($"Quest at index {i} has an empty Title.");
                }

                if (q.TargetCount <= 0)
                {
                    _validationErrors.Add($"Quest ID {q.QuestId} has invalid target count ({q.TargetCount}). Must be > 0.");
                }

                if (q.RewardCoins < 0)
                {
                    _validationErrors.Add($"Quest ID {q.QuestId} has negative reward coins ({q.RewardCoins}).");
                }
            }
        }

        private void SaveStagedToDatabase()
        {
            RunStagingValidation();
            if (_validationErrors.Count > 0)
            {
                EditorUtility.DisplayDialog("Validation Error", "Cannot save database because there are configuration errors! Check the red banner inside the Editor Window.", "OK");
                return;
            }

            if (_database == null) return;

            // Record undo state for active ScriptableObject
            Undo.RecordObject(_database, "Save Quest Database changes");

            _database.Quests.Clear();
            foreach (var q in _stagingQuests)
            {
                _database.Quests.Add(new QuestTemplate(q.QuestId, q.Title, q.TargetCount, q.RewardCoins, q.Type));
            }

            EditorUtility.SetDirty(_database);
            AssetDatabase.SaveAssets();
            _isDirty = false;
            
            Debug.Log("<color=green>[CozySim]</color> QuestDatabase changes saved and written to disk successfully!");
            ShowNotification(new GUIContent("Database Saved!"));
        }

        private void OnGUI()
        {
            if (_database == null)
            {
                EditorGUILayout.HelpBox("Failed to load or create QuestDatabase asset.", MessageType.Error);
                if (GUILayout.Button("Retry Load")) LoadOrCreateDatabase();
                return;
            }

            // Aesthetically pleasing top title banner
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                margin = new RectOffset(10, 10, 10, 10)
            };
            GUILayout.Label("Cozy Life Sim - Quest Database Designer", headerStyle);

            // Display Dirty Indicator & Sync info
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Editing Asset: {AssetDatabase.GetAssetPath(_database)}", EditorStyles.miniLabel);
            if (_isDirty)
            {
                GUI.color = Color.yellow;
                GUILayout.Label("* Unsaved Draft Changes", EditorStyles.boldLabel);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Display red validation warnings if there are staging errors
            if (_validationErrors.Count > 0)
            {
                GUI.backgroundColor = new Color(1f, 0.3f, 0.3f, 0.4f);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                
                GUIStyle errorHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
                errorHeaderStyle.normal.textColor = Color.red;
                GUILayout.Label("CONFIGURATION WARNINGS:", errorHeaderStyle);

                foreach (var err in _validationErrors)
                {
                    GUILayout.Label($"• {err}", EditorStyles.wordWrappedLabel);
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            // Main Editor Panel layout (2-Column split pane)
            EditorGUILayout.BeginHorizontal();

            // Left Column: Quests List sidebar (Width 220px)
            EditorGUILayout.BeginVertical(GUILayout.Width(220));
            GUILayout.Label("Active Templates", EditorStyles.boldLabel);
            
            _sidebarScroll = EditorGUILayout.BeginScrollView(_sidebarScroll, EditorStyles.helpBox);
            for (int i = 0; i < _stagingQuests.Count; i++)
            {
                var q = _stagingQuests[i];
                string labelName = $"ID {q.QuestId}: {(string.IsNullOrEmpty(q.Title) ? "[No Name]" : q.Title)}";
                
                GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
                if (i == _selectedStagingIndex)
                {
                    btnStyle.normal.textColor = Color.cyan;
                    btnStyle.fontStyle = FontStyle.Bold;
                }

                if (GUILayout.Button(labelName, btnStyle, GUILayout.Height(30)))
                {
                    _selectedStagingIndex = i;
                    GUI.FocusControl(null); // Clear active field focus
                }
            }
            EditorGUILayout.EndScrollView();

            // Sidebar Controls: Add & Delete buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Quest", GUILayout.Height(28)))
            {
                // Generate next unique ID by default
                int nextId = 1;
                foreach (var q in _stagingQuests)
                {
                    if (q.QuestId >= nextId) nextId = q.QuestId + 1;
                }
                _stagingQuests.Add(new QuestTemplate(nextId, "New Custom Quest", 5, 50, QuestType.WaterCrops));
                _selectedStagingIndex = _stagingQuests.Count - 1;
                _isDirty = true;
                RunStagingValidation();
            }
            if (GUILayout.Button("Delete Selected", GUILayout.Height(28)))
            {
                if (_selectedStagingIndex >= 0 && _selectedStagingIndex < _stagingQuests.Count)
                {
                    var deletedQuest = _stagingQuests[_selectedStagingIndex];
                    if (EditorUtility.DisplayDialog("Delete Quest Template?", $"Are you sure you want to permanently remove '{deletedQuest.Title}' (ID {deletedQuest.QuestId})? This will discard progress for this quest ID in active player saves.", "Delete", "Cancel"))
                    {
                        _stagingQuests.RemoveAt(_selectedStagingIndex);
                        _selectedStagingIndex = _stagingQuests.Count > 0 ? 0 : -1;
                        _isDirty = true;
                        RunStagingValidation();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // Divider spacer
            GUILayout.Space(15);

            // Right Column: Properties Details form
            EditorGUILayout.BeginVertical();
            GUILayout.Label("Template Specifications", EditorStyles.boldLabel);

            _detailsScroll = EditorGUILayout.BeginScrollView(_detailsScroll, EditorStyles.helpBox);
            if (_selectedStagingIndex >= 0 && _selectedStagingIndex < _stagingQuests.Count)
            {
                var q = _stagingQuests[_selectedStagingIndex];

                EditorGUI.BeginChangeCheck();

                int newId = EditorGUILayout.IntField("Quest Unique ID", q.QuestId);
                if (newId != q.QuestId)
                {
                    // ID Change Warning Dialog
                    if (EditorUtility.DisplayDialog("Modify Quest ID?", "Warning: Changing an existing Quest ID will cause active players to lose progress for this quest, or finished quests to map onto new templates. Are you sure?", "Change ID", "Revert"))
                    {
                        q.QuestId = newId;
                    }
                }

                q.Title = EditorGUILayout.TextField("Quest Title", q.Title);
                q.TargetCount = EditorGUILayout.IntField("Goal Target Count", q.TargetCount);
                q.RewardCoins = EditorGUILayout.IntField("Coins Reward", q.RewardCoins);
                q.Type = (QuestType)EditorGUILayout.EnumPopup("Progress Event Type", q.Type);

                if (EditorGUI.EndChangeCheck())
                {
                    _isDirty = true;
                    RunStagingValidation();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Select a Quest template from the sidebar or click 'Add Quest' to define settings.", MessageType.Info);
            }
            EditorGUILayout.EndScrollView();

            // Bottom Form controls: Save & Revert draft
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = _isDirty && _validationErrors.Count == 0;
            if (GUILayout.Button("Save to Database", GUILayout.Height(35)))
            {
                SaveStagedToDatabase();
            }
            GUI.enabled = _isDirty;
            if (GUILayout.Button("Revert Changes", GUILayout.Height(35)))
            {
                if (EditorUtility.DisplayDialog("Revert Staging Draft?", "Are you sure you want to discard all your unsaved modifications? This will re-load the exact database state from disk.", "Revert", "Cancel"))
                {
                    SyncToStaging();
                }
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }
    }
}
