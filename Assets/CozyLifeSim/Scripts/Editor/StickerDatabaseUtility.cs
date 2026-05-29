using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using CozyLifeSim.UI.Settings;

namespace CozyLifeSim.Editor
{
    public static class StickerDatabaseUtility
    {
        public static StickerDatabase LoadOrCreateDatabase()
        {
            StickerDatabase database = null;
            string[] guids = AssetDatabase.FindAssets("t:StickerDatabase");
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                database = AssetDatabase.LoadAssetAtPath<StickerDatabase>(path);
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
                database = ScriptableObject.CreateInstance<StickerDatabase>();
                AssetDatabase.CreateAsset(database, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"<color=green>[CozySim]</color> Created new StickerDatabase asset at {assetPath}");

                // Ping newly created database asset
                EditorApplication.delayCall += () =>
                {
                    if (database != null)
                    {
                        EditorGUIUtility.PingObject(database);
                        Selection.activeObject = database;
                    }
                };
            }

            if (database != null)
            {
                BootstrapDefaultStickers(database);
            }

            return database;
        }

        public static void BootstrapDefaultStickers(StickerDatabase database)
        {
            if (database == null) return;

            if (database.Stickers == null)
            {
                database.Stickers = new List<StickerTemplate>();
            }

            if (database.Stickers.Count == 0)
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

                database.Stickers.Add(new StickerTemplate(1, "Bunny Pink", bunnySprite, bunnySprite));
                database.Stickers.Add(new StickerTemplate(2, "Bear", bearSprite, bearSprite));
                
                if (AssetDatabase.Contains(database))
                {
                    EditorUtility.SetDirty(database);
                    AssetDatabase.SaveAssets();
                }
                Debug.Log("<color=green>[CozySim]</color> Bootstrapped default 'Bunny Pink' and 'Bear' stickers inside database (with safety fallbacks).");
            }
        }
    }
}
