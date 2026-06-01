using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using CozyLifeSim.UI.Settings;

namespace CozyLifeSim.Editor
{
    public static class AnimalDatabaseUtility
    {
        public static AnimalDatabase LoadOrCreateDatabase()
        {
            AnimalDatabase database = null;
            string[] guids = AssetDatabase.FindAssets("t:AnimalDatabase");
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                database = AssetDatabase.LoadAssetAtPath<AnimalDatabase>(path);
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
                database = ScriptableObject.CreateInstance<AnimalDatabase>();
                AssetDatabase.CreateAsset(database, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"<color=green>[CozySim]</color> Created new AnimalDatabase asset at {assetPath}");

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
                BootstrapDefaultAnimal(database);
            }

            return database;
        }

        public static void BootstrapDefaultAnimal(AnimalDatabase database)
        {
            if (database == null) return;

            if (database.Animals == null)
            {
                database.Animals = new List<AnimalTemplate>();
            }

            var chickenSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Animals/Chicken-White-256.png");
            var heartSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Hearts/Heart-Red-256.png");
            var catSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Animals/Cat-Orange-256.png");
            var bearSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Animals/Bear-256.png");

            // Fallback: If designated package sprites are null, auto-discover any available Sprite in the project
            if (chickenSprite == null || heartSprite == null || catSprite == null || bearSprite == null)
            {
                string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite");
                if (spriteGuids != null && spriteGuids.Length > 0)
                {
                    var fallbackSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(spriteGuids[0]));
                    if (chickenSprite == null) chickenSprite = fallbackSprite;
                    if (heartSprite == null) heartSprite = fallbackSprite;
                    if (catSprite == null) catSprite = fallbackSprite;
                    if (bearSprite == null) bearSprite = fallbackSprite;
                }
            }

            var builtInSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            if (chickenSprite == null) chickenSprite = builtInSprite;
            if (heartSprite == null) heartSprite = builtInSprite;
            if (catSprite == null) catSprite = builtInSprite;
            if (bearSprite == null) bearSprite = builtInSprite;

            bool addedAny = false;

            if (!database.Animals.Exists(x => x != null && x.AnimalId == 1))
            {
                database.Animals.Add(new AnimalTemplate(1, "Breathing Chicken", chickenSprite, 1.03f, 1.5f, 25f, 0.4f, heartSprite) { RequiredLevel = 1 });
                addedAny = true;
            }
            if (!database.Animals.Exists(x => x != null && x.AnimalId == 2))
            {
                database.Animals.Add(new AnimalTemplate(2, "Meo Tam The", catSprite, 1.03f, 1.5f, 25f, 0.4f, heartSprite) { RequiredLevel = 1 });
                addedAny = true;
            }
            if (!database.Animals.Exists(x => x != null && x.AnimalId == 3))
            {
                database.Animals.Add(new AnimalTemplate(3, "Trau Nuoc", bearSprite, 1.03f, 1.5f, 25f, 0.4f, heartSprite) { RequiredLevel = 3 });
                addedAny = true;
            }

            // Safety guard: if templates exist but their Sprites are null (e.g. package assets missing on this machine),
            // auto-repair them using fallback sprites to ensure they can render and participate in validation.
            foreach (var animal in database.Animals)
            {
                if (animal != null)
                {
                    if (animal.Sprite == null || animal.HeartFeedbackSprite == null)
                    {
                        Sprite sp = chickenSprite, ht = heartSprite;
                        if (animal.AnimalId == 1) { sp = chickenSprite; }
                        else if (animal.AnimalId == 2) { sp = catSprite; }
                        else if (animal.AnimalId == 3) { sp = bearSprite; }

                        if (animal.Sprite == null) animal.Sprite = sp;
                        if (animal.HeartFeedbackSprite == null) animal.HeartFeedbackSprite = ht;
                        addedAny = true;
                    }
                }
            }

            if (addedAny && AssetDatabase.Contains(database))
            {
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssets();
                Debug.Log("<color=green>[CozySim]</color> Bootstrapped/Auto-repaired animals inside database (with safety fallbacks).");
            }
        }
    }
}
