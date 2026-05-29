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
            _database = StickerDatabaseUtility.LoadOrCreateDatabase();

            if (_database != null)
            {

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

        private static void LogEditorError(string title, IEnumerable<string> details)
        {
            string detailText = details != null ? string.Join("\n", details) : string.Empty;
            Debug.LogError(
                "\n===================================================================================\n" +
                $"<size=14><b><color=red>[CozySim Sticker Editor: {title}]</color></b></size>\n" +
                "-----------------------------------------------------------------------------------\n" +
                detailText +
                "\n===================================================================================");
        }

        private static void LogEditorSuccess(string message)
        {
            Debug.Log(
                "\n===================================================================================\n" +
                $"<size=14><b><color=green>[CozySim Sticker Editor]</color></b></size> {message}\n" +
                "===================================================================================");
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
                GUILayout.Label("<color=green>✓ Database state is valid and aligned.</color>", new GUIStyle(EditorStyles.label) { richText = true });
            }

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = _isDirty && _validationErrors.Count == 0;
            if (GUILayout.Button("Save Changes", GUILayout.Height(40)))
            {
                ValidateStaging();
                if (_validationErrors.Count > 0)
                {
                    LogEditorError("Save Blocked", _validationErrors);
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
                    LogEditorSuccess("Sticker Database saved successfully.");
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
