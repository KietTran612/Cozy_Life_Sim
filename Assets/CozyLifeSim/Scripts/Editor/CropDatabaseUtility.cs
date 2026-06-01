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

            var seedSprite1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Plants/Acorn-256.png");
            var sproutSprite1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Plants/Sapling-256.png");
            var matureSprite1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Flowers/Flower-Tulip-Red-256.png");

            var seedSprite2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Plants/Seed-Pink-256.png");
            var sproutSprite2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Plants/Sprout-256.png");
            var matureSprite2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Plants/Bamboo-256.png");

            var seedSprite3 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Plants/Seed-256.png");
            var sproutSprite3 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Plants/Grass-256.png");
            var matureSprite3 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Plants/Wheat-256.png");

            var seedSprite4 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Plants/Seed-Blue-256.png");
            var sproutSprite4 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Plants/Leaf-Ivy-256.png");
            var matureSprite4 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Flowers/Flower-Lotus-256.png");
            if (matureSprite4 == null) matureSprite4 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Flowers/Flower-Tulip-Pink-256.png");

            // Fallback: If designated package sprites are null, auto-discover any available Sprite in the project
            if (seedSprite1 == null || sproutSprite1 == null || matureSprite1 == null ||
                seedSprite2 == null || sproutSprite2 == null || matureSprite2 == null ||
                seedSprite3 == null || sproutSprite3 == null || matureSprite3 == null ||
                seedSprite4 == null || sproutSprite4 == null || matureSprite4 == null)
            {
                string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite");
                if (spriteGuids != null && spriteGuids.Length > 0)
                {
                    var fallbackSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(spriteGuids[0]));
                    if (seedSprite1 == null) seedSprite1 = fallbackSprite;
                    if (sproutSprite1 == null) sproutSprite1 = fallbackSprite;
                    if (matureSprite1 == null) matureSprite1 = fallbackSprite;

                    if (seedSprite2 == null) seedSprite2 = fallbackSprite;
                    if (sproutSprite2 == null) sproutSprite2 = fallbackSprite;
                    if (matureSprite2 == null) matureSprite2 = fallbackSprite;

                    if (seedSprite3 == null) seedSprite3 = fallbackSprite;
                    if (sproutSprite3 == null) sproutSprite3 = fallbackSprite;
                    if (matureSprite3 == null) matureSprite3 = fallbackSprite;

                    if (seedSprite4 == null) seedSprite4 = fallbackSprite;
                    if (sproutSprite4 == null) sproutSprite4 = fallbackSprite;
                    if (matureSprite4 == null) matureSprite4 = fallbackSprite;
                }
            }

            var builtInSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            if (seedSprite1 == null) seedSprite1 = builtInSprite;
            if (sproutSprite1 == null) sproutSprite1 = builtInSprite;
            if (matureSprite1 == null) matureSprite1 = builtInSprite;

            if (seedSprite2 == null) seedSprite2 = builtInSprite;
            if (sproutSprite2 == null) sproutSprite2 = builtInSprite;
            if (matureSprite2 == null) matureSprite2 = builtInSprite;

            if (seedSprite3 == null) seedSprite3 = builtInSprite;
            if (sproutSprite3 == null) sproutSprite3 = builtInSprite;
            if (matureSprite3 == null) matureSprite3 = builtInSprite;

            if (seedSprite4 == null) seedSprite4 = builtInSprite;
            if (sproutSprite4 == null) sproutSprite4 = builtInSprite;
            if (matureSprite4 == null) matureSprite4 = builtInSprite;

            bool addedAny = false;

            if (!database.Crops.Exists(x => x != null && x.CropId == 1))
            {
                database.Crops.Add(new CropTemplate(1, "White Acorn", 5f, seedSprite1, sproutSprite1, matureSprite1, matureSprite1) { BuyPrice = 5, SellPrice = 15, RequiredLevel = 1 });
                addedAny = true;
            }
            if (!database.Crops.Exists(x => x != null && x.CropId == 2))
            {
                database.Crops.Add(new CropTemplate(2, "Cay Mia Ngot", 8f, seedSprite2, sproutSprite2, matureSprite2, matureSprite2) { BuyPrice = 15, SellPrice = 35, RequiredLevel = 1 });
                addedAny = true;
            }
            if (!database.Crops.Exists(x => x != null && x.CropId == 3))
            {
                database.Crops.Add(new CropTemplate(3, "Lua Nuoc", 15f, seedSprite3, sproutSprite3, matureSprite3, matureSprite3) { BuyPrice = 25, SellPrice = 60, RequiredLevel = 2 });
                addedAny = true;
            }
            if (!database.Crops.Exists(x => x != null && x.CropId == 4))
            {
                database.Crops.Add(new CropTemplate(4, "Hoa Sen", 25f, seedSprite4, sproutSprite4, matureSprite4, matureSprite4) { BuyPrice = 50, SellPrice = 120, RequiredLevel = 3 });
                addedAny = true;
            }

            // Safety guard: if templates exist but their Sprites are null (e.g. package assets missing on this machine),
            // auto-repair them using fallback sprites to ensure they can render and participate in validation.
            foreach (var crop in database.Crops)
            {
                if (crop != null)
                {
                    if (crop.SeedSprite == null || crop.SproutSprite == null || crop.MatureSprite == null || crop.HarvestSprite == null)
                    {
                        Sprite sd = seedSprite1, sp = sproutSprite1, mt = matureSprite1;
                        if (crop.CropId == 1) { sd = seedSprite1; sp = sproutSprite1; mt = matureSprite1; }
                        else if (crop.CropId == 2) { sd = seedSprite2; sp = sproutSprite2; mt = matureSprite2; }
                        else if (crop.CropId == 3) { sd = seedSprite3; sp = sproutSprite3; mt = matureSprite3; }
                        else if (crop.CropId == 4) { sd = seedSprite4; sp = sproutSprite4; mt = matureSprite4; }

                        if (crop.SeedSprite == null) crop.SeedSprite = sd;
                        if (crop.SproutSprite == null) crop.SproutSprite = sp;
                        if (crop.MatureSprite == null) crop.MatureSprite = mt;
                        if (crop.HarvestSprite == null) crop.HarvestSprite = mt;
                        addedAny = true;
                    }
                }
            }

            if (addedAny && AssetDatabase.Contains(database))
            {
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssets();
                Debug.Log("<color=green>[CozySim]</color> Bootstrapped/Auto-repaired crops inside database (with safety fallbacks).");
            }
        }
    }
}
