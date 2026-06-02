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

            var banhMiSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Foods/Bread-256.png");
            var nuocMiaSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Foods/Drink-Soda-Blue-256.png");
            var nonLaSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Equipments/Helmet-Straw-256.png");
            var xichLoSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Equipments/Wagon-256.png");
            var longDenSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Lights/Lantern-Red-256.png");

            // Fallback: If designated package sprites are null, auto-discover any available Sprite in the project
            if (bunnySprite == null || bearSprite == null || chickenSprite == null ||
                banhMiSprite == null || nuocMiaSprite == null || nonLaSprite == null || xichLoSprite == null || longDenSprite == null)
            {
                string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite");
                if (spriteGuids != null && spriteGuids.Length > 0)
                {
                    var fallbackSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(spriteGuids[0]));
                    if (bunnySprite == null) bunnySprite = fallbackSprite;
                    if (bearSprite == null) bearSprite = fallbackSprite;
                    if (chickenSprite == null) chickenSprite = fallbackSprite;
                    if (banhMiSprite == null) banhMiSprite = fallbackSprite;
                    if (nuocMiaSprite == null) nuocMiaSprite = fallbackSprite;
                    if (nonLaSprite == null) nonLaSprite = fallbackSprite;
                    if (xichLoSprite == null) xichLoSprite = fallbackSprite;
                    if (longDenSprite == null) longDenSprite = fallbackSprite;
                }
            }

            var builtInSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            if (bunnySprite == null) bunnySprite = builtInSprite;
            if (bearSprite == null) bearSprite = builtInSprite;
            if (chickenSprite == null) chickenSprite = builtInSprite;
            if (banhMiSprite == null) banhMiSprite = builtInSprite;
            if (nuocMiaSprite == null) nuocMiaSprite = builtInSprite;
            if (nonLaSprite == null) nonLaSprite = builtInSprite;
            if (xichLoSprite == null) xichLoSprite = builtInSprite;
            if (longDenSprite == null) longDenSprite = builtInSprite;

            if (!database.Stickers.Exists(x => x != null && x.StickerId == 1))
            {
                database.Stickers.Add(new StickerTemplate(1, "Bunny Pink", bunnySprite, bunnySprite) { BuyPrice = 50, RequiredLevel = 1 });
                addedAny = true;
            }
            if (!database.Stickers.Exists(x => x != null && x.StickerId == 2))
            {
                database.Stickers.Add(new StickerTemplate(2, "Bear", bearSprite, bearSprite) { BuyPrice = 50, RequiredLevel = 1 });
                addedAny = true;
            }
            if (!database.Stickers.Exists(x => x != null && x.StickerId == 3))
            {
                database.Stickers.Add(new StickerTemplate(3, "Chicken White", chickenSprite, chickenSprite) { BuyPrice = 50, RequiredLevel = 1 });
                addedAny = true;
            }
            if (!database.Stickers.Exists(x => x != null && x.StickerId == 4))
            {
                database.Stickers.Add(new StickerTemplate(4, "Xe Banh Mi", banhMiSprite, banhMiSprite) { BuyPrice = 60, RequiredLevel = 1 });
                addedAny = true;
            }
            if (!database.Stickers.Exists(x => x != null && x.StickerId == 5))
            {
                database.Stickers.Add(new StickerTemplate(5, "Ly Nuoc Mia", nuocMiaSprite, nuocMiaSprite) { BuyPrice = 40, RequiredLevel = 1 });
                addedAny = true;
            }
            if (!database.Stickers.Exists(x => x != null && x.StickerId == 6))
            {
                database.Stickers.Add(new StickerTemplate(6, "Chiec Non La", nonLaSprite, nonLaSprite) { BuyPrice = 50, RequiredLevel = 2 });
                addedAny = true;
            }
            if (!database.Stickers.Exists(x => x != null && x.StickerId == 7))
            {
                database.Stickers.Add(new StickerTemplate(7, "Chiec Xich Lo", xichLoSprite, xichLoSprite) { BuyPrice = 120, RequiredLevel = 3 });
                addedAny = true;
            }
            if (!database.Stickers.Exists(x => x != null && x.StickerId == 8))
            {
                database.Stickers.Add(new StickerTemplate(8, "Long Den Trung Thu", longDenSprite, longDenSprite) { BuyPrice = 80, RequiredLevel = 2 });
                addedAny = true;
            }

            // Idempotent Heritage Upgrade for Stickers (IDs 4 to 8)
            var stickerHeritagePaths = new Dictionary<int, string>
            {
                { 4, "Assets/CozyLifeSim/Textures/Heritage/Sticker_BanhMiCart.png" },
                { 5, "Assets/CozyLifeSim/Textures/Heritage/Sticker_SugarcaneJuice.png" },
                { 6, "Assets/CozyLifeSim/Textures/Heritage/Sticker_ConicalHat.png" },
                { 7, "Assets/CozyLifeSim/Textures/Heritage/Sticker_Cyclo.png" },
                { 8, "Assets/CozyLifeSim/Textures/Heritage/Sticker_StarLantern.png" }
            };

            foreach (var pair in stickerHeritagePaths)
            {
                string filePath = pair.Value;
                int stickerId = pair.Key;
                if (File.Exists(filePath))
                {
                    CozyAssetImporterUtility.ConfigureAsSprite(filePath);
                    Sprite realSprite = AssetDatabase.LoadAssetAtPath<Sprite>(filePath);
                    if (realSprite != null)
                    {
                        var sticker = database.Stickers.Find(x => x != null && x.StickerId == stickerId);
                        if (sticker != null)
                        {
                            if (sticker.Sprite != realSprite || sticker.ShadowSprite != realSprite)
                            {
                                sticker.Sprite = realSprite;
                                sticker.ShadowSprite = realSprite;
                                addedAny = true;
                            }
                        }
                    }
                }
            }

            // Safety guard: if templates exist but their Sprites are null (e.g. package assets missing on this machine),
            // auto-repair them using fallback sprites to ensure they can spawn and participate in validation.
            foreach (var sticker in database.Stickers)
            {
                if (sticker != null)
                {
                    if (sticker.Sprite == null)
                    {
                        if (sticker.StickerId == 1) sticker.Sprite = bunnySprite;
                        else if (sticker.StickerId == 2) sticker.Sprite = bearSprite;
                        else if (sticker.StickerId == 3) sticker.Sprite = chickenSprite;
                        else if (sticker.StickerId == 4) sticker.Sprite = banhMiSprite;
                        else if (sticker.StickerId == 5) sticker.Sprite = nuocMiaSprite;
                        else if (sticker.StickerId == 6) sticker.Sprite = nonLaSprite;
                        else if (sticker.StickerId == 7) sticker.Sprite = xichLoSprite;
                        else if (sticker.StickerId == 8) sticker.Sprite = longDenSprite;
                        else sticker.Sprite = bunnySprite;
                        addedAny = true;
                    }
                    if (sticker.ShadowSprite == null)
                    {
                        if (sticker.StickerId == 1) sticker.ShadowSprite = bunnySprite;
                        else if (sticker.StickerId == 2) sticker.ShadowSprite = bearSprite;
                        else if (sticker.StickerId == 3) sticker.ShadowSprite = chickenSprite;
                        else if (sticker.StickerId == 4) sticker.ShadowSprite = banhMiSprite;
                        else if (sticker.StickerId == 5) sticker.ShadowSprite = nuocMiaSprite;
                        else if (sticker.StickerId == 6) sticker.ShadowSprite = nonLaSprite;
                        else if (sticker.StickerId == 7) sticker.ShadowSprite = xichLoSprite;
                        else if (sticker.StickerId == 8) sticker.ShadowSprite = longDenSprite;
                        else sticker.ShadowSprite = bunnySprite;
                        addedAny = true;
                    }
                }
            }

            if (addedAny)
            {
                if (AssetDatabase.Contains(database))
                {
                    EditorUtility.SetDirty(database);
                    AssetDatabase.SaveAssets();
                }
                Debug.Log("<color=green>[CozySim]</color> Bootstrapped and auto-repaired missing stickers inside database (with safety fallbacks).");
            }
        }

    }
}
