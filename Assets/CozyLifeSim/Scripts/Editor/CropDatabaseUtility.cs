using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using CozyLifeSim.UI.Settings;

namespace CozyLifeSim.Editor
{
    public static class CropDatabaseUtility
    {
        public static CropDatabase LoadOrCreateDatabase()
        {
            CropDatabase database = null;
            string[] guids = AssetDatabase.FindAssets("t:CropDatabase");
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                database = AssetDatabase.LoadAssetAtPath<CropDatabase>(path);
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
                database = ScriptableObject.CreateInstance<CropDatabase>();
                AssetDatabase.CreateAsset(database, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"<color=green>[CozySim]</color> Created new CropDatabase asset at {assetPath}");

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
                BootstrapDefaultCrop(database);
            }

            return database;
        }

        public static void BootstrapDefaultCrop(CropDatabase database)
        {
            if (database == null) return;

            if (database.Crops == null)
            {
                database.Crops = new List<CropTemplate>();
            }

            if (database.Crops.Count == 0)
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

                database.Crops.Add(new CropTemplate(1, "White Acorn", 5f, seedSprite, sproutSprite, matureSprite, matureSprite));
                if (AssetDatabase.Contains(database))
                {
                    EditorUtility.SetDirty(database);
                    AssetDatabase.SaveAssets();
                }
                Debug.Log("<color=green>[CozySim]</color> Bootstrapped default 'White Acorn' crop inside database (with safety fallbacks).");
            }

            // Safety guard: if templates exist but their Sprites are null (e.g. package assets missing on this machine),
            // auto-repair them using fallback sprites to ensure they can render and participate in validation.
            bool addedAny = false;
            foreach (var crop in database.Crops)
            {
                if (crop != null)
                {
                    if (crop.SeedSprite == null || crop.SproutSprite == null || crop.MatureSprite == null || crop.HarvestSprite == null)
                    {
                        var seedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Plants/Acorn-256.png");
                        var sproutSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Plants/Sapling-256.png");
                        var matureSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Flowers/Flower-Tulip-Red-256.png");

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

                        var builtInSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
                        if (seedSprite == null) seedSprite = builtInSprite;
                        if (sproutSprite == null) sproutSprite = builtInSprite;
                        if (matureSprite == null) matureSprite = builtInSprite;

                        if (crop.SeedSprite == null) crop.SeedSprite = seedSprite;
                        if (crop.SproutSprite == null) crop.SproutSprite = sproutSprite;
                        if (crop.MatureSprite == null) crop.MatureSprite = matureSprite;
                        if (crop.HarvestSprite == null) crop.HarvestSprite = matureSprite;
                        addedAny = true;
                    }
                }
            }

            if (addedAny && AssetDatabase.Contains(database))
            {
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssets();
                Debug.Log("<color=green>[CozySim]</color> Auto-repaired missing crop sprites in database (with safety fallbacks).");
            }
        }
    }
}
