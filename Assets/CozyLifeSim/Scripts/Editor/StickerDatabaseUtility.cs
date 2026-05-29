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

            bool addedAny = false;
            var bunnySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Animals/Bunny-Pink-256.png");
            var bearSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Animals/Bear-256.png");
            var chickenSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Animals/Chicken-White-256.png");

            // Fallback: If designated package sprites are null, auto-discover any available Sprite in the project
            if (bunnySprite == null || bearSprite == null || chickenSprite == null)
            {
                string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite");
                if (spriteGuids != null && spriteGuids.Length > 0)
                {
                    var fallbackSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(spriteGuids[0]));
                    if (bunnySprite == null) bunnySprite = fallbackSprite;
                    if (bearSprite == null) bearSprite = fallbackSprite;
                    if (chickenSprite == null) chickenSprite = fallbackSprite;
                }
            }

            if (!database.Stickers.Exists(x => x != null && x.StickerId == 1))
            {
                database.Stickers.Add(new StickerTemplate(1, "Bunny Pink", bunnySprite, bunnySprite) { BuyPrice = 50 });
                addedAny = true;
            }
            if (!database.Stickers.Exists(x => x != null && x.StickerId == 2))
            {
                database.Stickers.Add(new StickerTemplate(2, "Bear", bearSprite, bearSprite) { BuyPrice = 50 });
                addedAny = true;
            }
            if (!database.Stickers.Exists(x => x != null && x.StickerId == 3))
            {
                database.Stickers.Add(new StickerTemplate(3, "Chicken White", chickenSprite, chickenSprite) { BuyPrice = 50 });
                addedAny = true;
            }

            if (addedAny)
            {
                if (AssetDatabase.Contains(database))
                {
                    EditorUtility.SetDirty(database);
                    AssetDatabase.SaveAssets();
                }
                Debug.Log("<color=green>[CozySim]</color> Bootstrapped missing stickers inside database (with safety fallbacks).");
            }
        }

    }
}
