using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CozyLifeSim.UI;


namespace CozyLifeSim.Editor
{
    public static partial class CozyLifeSimSceneGameplayValidation
    {
        private const string MainScenePath = "Assets/CozyLifeSim/Scenes/Main.unity";

        private static readonly Dictionary<string, string> RequiredSprites = new Dictionary<string, string>
        {
            { "Chicken", "Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Animals/Chicken-White-256.png" },
            { "Heart", "Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Hearts/Heart-Red-256.png" },
            { "Seed", "Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Plants/Acorn-256.png" },
            { "Sprout", "Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Plants/Sapling-256.png" },
            { "Mature", "Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Flowers/Flower-Tulip-Red-256.png" },
            { "WateringCan", "Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Farming/Watering-Can-Pink-256.png" },
            { "Bunny", "Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Animals/Bunny-Pink-256.png" },
            { "Bear", "Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Animals/Bear-256.png" }
        };

        [MenuItem("Tools/CozySim/Run Scene Gameplay Loop Validation")]
        public static void RunValidation()
        {
            List<string> passes = new List<string>();
            List<string> warnings = new List<string>();
            List<string> errors = new List<string>();

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != MainScenePath)
            {
                warnings.Add($"Active scene is '{activeScene.path}'. Expected '{MainScenePath}'. Open Main before validating final gameplay setup.");
            }
            else
            {
                passes.Add("Main scene is active.");
            }

            ValidateMissingScripts(errors, passes);
            ValidateRequiredSprites(warnings, passes);
            ValidateLifetimeAndCanvas(errors, warnings, passes);
            ValidateInventoryHud(errors, passes);
            ValidateFarmLoop(errors, warnings, passes);
            ValidateAnimalLoop(errors, passes);
            ValidateStickerLoop(errors, warnings, passes);
            ValidatePopupAndNavigationDocks(errors, passes);
            ValidateScrapbookAndDialogues(errors, passes);
            ValidateBuildSceneAndOrientation(errors, passes);
            ValidateMainCamera(errors, passes);
            ValidateStartupLoadingOverlay(errors, passes);
            ValidateLandscapeLayout(errors, passes);
            ValidateUISpriteAssignments(errors, passes);
            ValidateTextureImporterMemorySettings(errors, passes);

            PrintResults(passes, warnings, errors);
        }

        private static void ValidateMissingScripts(List<string> errors, List<string> passes)
        {
            int missingCount = 0;
            foreach (GameObject go in FindSceneObjects<GameObject>())
            {
                Component[] components = go.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == null)
                    {
                        missingCount++;
                        errors.Add($"Missing script on GameObject '{GetPath(go.transform)}'.");
                    }
                }
            }

            if (missingCount == 0)
            {
                passes.Add("No missing scripts found in loaded scene objects.");
            }
        }

        private static void ValidateRequiredSprites(List<string> warnings, List<string> passes)
        {
            foreach (KeyValuePair<string, string> pair in RequiredSprites)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pair.Value);
                if (sprite == null)
                {
                    warnings.Add($"Optional CuteKawaii sprite '{pair.Key}' could not be loaded at '{pair.Value}'. Scene setup should use fallback sprites on this machine.");
                }
                else
                {
                    passes.Add($"Required sprite '{pair.Key}' is importable.");
                }
            }
        }

        private static void ValidateLifetimeAndCanvas(List<string> errors, List<string> warnings, List<string> passes)
        {
            GameLifetimeScope lifetimeScope = FindSceneComponent<GameLifetimeScope>("GameLifetimeScope");
            if (lifetimeScope == null)
            {
                errors.Add("GameLifetimeScope is missing.");
            }
            else
            {
                passes.Add("GameLifetimeScope exists.");
                SerializedObject so = new SerializedObject(lifetimeScope);
                SerializedProperty styleConfig = so.FindProperty("_defaultStyleConfig");
                if (styleConfig == null || styleConfig.objectReferenceValue == null)
                {
                    warnings.Add("GameLifetimeScope._defaultStyleConfig is not assigned. UI style injection will run, but no theme config will apply.");
                }
                else
                {
                    passes.Add("GameLifetimeScope default style config is assigned.");
                }

                // Check _feedbackToast
                SerializedProperty feedbackToastProp = so.FindProperty("_feedbackToast");
                if (feedbackToastProp == null || feedbackToastProp.objectReferenceValue == null)
                {
                    errors.Add("GameLifetimeScope._feedbackToast is not assigned.");
                }
                else
                {
                    passes.Add("GameLifetimeScope._feedbackToast is assigned.");
                }
            }

            CozyFeedbackToast toast = FindSceneComponent<CozyFeedbackToast>("FeedbackToast");
            if (toast == null)
            {
                errors.Add("FeedbackToast object with CozyFeedbackToast component is missing from scene.");
            }
            else
            {
                passes.Add("FeedbackToast exists in scene.");
                SerializedObject soToast = new SerializedObject(toast);
                ValidateObjectReference(soToast, "_canvasGroup", "CozyFeedbackToast._canvasGroup", errors, passes);
                ValidateObjectReference(soToast, "_contentPanel", "CozyFeedbackToast._contentPanel", errors, passes);
                ValidateObjectReference(soToast, "_messageText", "CozyFeedbackToast._messageText", errors, passes);

                CanvasGroup cg = GetReference<CanvasGroup>(soToast, "_canvasGroup");
                if (cg != null && cg.blocksRaycasts)
                {
                    errors.Add("CozyFeedbackToast CanvasGroup.blocksRaycasts should be false to prevent blocking clicks.");
                }
                else if (cg != null)
                {
                    passes.Add("CozyFeedbackToast CanvasGroup blocksRaycasts is false.");
                }
            }

            if (FindSceneComponent<Canvas>("Canvas") == null)
            {
                errors.Add("Canvas is missing.");
            }
            else
            {
                passes.Add("Canvas exists.");
                Canvas canvas = FindSceneComponent<Canvas>("Canvas");
                if (canvas.GetComponent<GraphicRaycaster>() == null)
                {
                    errors.Add("Canvas is missing GraphicRaycaster for UI raycast blocking.");
                }
                else
                {
                    passes.Add("Canvas has GraphicRaycaster.");
                }
            }

            if (FindSceneComponent<UnityEngine.EventSystems.EventSystem>("EventSystem") == null)
            {
                errors.Add("EventSystem is missing.");
            }
            else
            {
                passes.Add("EventSystem exists.");
            }
        }

        private static void ValidateFarmLoop(List<string> errors, List<string> warnings, List<string> passes)
        {
            CropWidget crop = FindSceneComponent<CropWidget>("Farm_Plot");
            if (crop == null)
            {
                errors.Add("CropWidget is missing from Farm_Plot.");
                return;
            }

            SerializedObject so = new SerializedObject(crop);
            ValidateObjectReference(so, "_cropVisual", "Crop visual Image", errors, passes);
            ValidateObjectReference(so, "_timerText", "Crop timer text", errors, passes);
            ValidateObjectReference(so, "_waterButton", "Water button", errors, passes);
            ValidateObjectReference(so, "_wateringCan", "Watering can RectTransform", errors, passes);
            ValidateObjectReference(so, "_plantButton", "Plant button", errors, passes);
            ValidateObjectReference(so, "_harvestButton", "Harvest button", errors, passes);

            ValidateSpriteReference(so, "_seedSprite", "Seed sprite", "Seed", errors, passes);
            ValidateSpriteReference(so, "_sproutSprite", "Sprout sprite", "Sprout", errors, passes);
            ValidateSpriteReference(so, "_matureSprite", "Mature sprite", "Mature", errors, passes);
            ValidateSpriteReference(so, "_harvestSprite", "Harvest sprite", "Mature", errors, passes);

            ValidateButtonTarget("_plantButton", so, errors, passes);
            ValidateButtonTarget("_waterButton", so, errors, passes);
            ValidateButtonTarget("_harvestButton", so, errors, passes);

            RectTransform wateringCan = GetReference<RectTransform>(so, "_wateringCan");
            if (wateringCan != null)
            {
                Image wateringCanImage = wateringCan.GetComponent<Image>();
                Sprite expected = LoadRequiredSprite("WateringCan");
                if (wateringCanImage == null)
                {
                    errors.Add("Watering_Can does not have an Image component.");
                }
                else if (expected != null && wateringCanImage.sprite != expected)
                {
                    errors.Add("Watering_Can Image sprite is not Watering-Can-Pink-256.png.");
                }
                else
                {
                    passes.Add(expected != null ? "Watering_Can uses the CuteKawaii watering can sprite." : "Watering_Can has a fallback sprite assigned.");
                }

                if (wateringCan.gameObject.activeSelf)
                {
                    warnings.Add("Watering_Can is active before Play Mode. It is expected to start hidden and animate during watering.");
                }
            }
        }

        private static void ValidateAnimalLoop(List<string> errors, List<string> passes)
        {
            AnimalWidget animal = FindSceneComponent<AnimalWidget>("Animal_Pen");
            if (animal == null)
            {
                errors.Add("AnimalWidget is missing from Animal_Pen.");
                return;
            }

            SerializedObject so = new SerializedObject(animal);
            ValidateObjectReference(so, "_interactionButton", "Animal interaction button", errors, passes);
            ValidateObjectReference(so, "_spawnRoot", "Animal heart spawn root", errors, passes);
            ValidateObjectReference(so, "_heartPrefab", "Animal heart prefab", errors, passes);
            ValidateButtonTarget("_interactionButton", so, errors, passes);

            Image chickenImage = FindSceneComponent<Image>("Chicken_Visual");
            ValidateImageSprite(chickenImage, "Chicken_Visual", "Chicken", errors, passes);

            RectTransform heartPrefab = GetReference<RectTransform>(so, "_heartPrefab");
            if (heartPrefab != null)
            {
                ValidateImageSprite(heartPrefab.GetComponent<Image>(), "Heart_Feedback_Template", "Heart", errors, passes);
                if (heartPrefab.GetComponent<CanvasGroup>() == null)
                {
                    errors.Add("Heart_Feedback_Template is missing CanvasGroup.");
                }
                else
                {
                    passes.Add("Heart_Feedback_Template has CanvasGroup.");
                }
            }
        }

        private static void ValidateStickerLoop(List<string> errors, List<string> warnings, List<string> passes)
        {
            StickerBook stickerBook = FindSceneComponent<StickerBook>("StickerBook_Panel");
            if (stickerBook == null)
            {
                errors.Add("StickerBook is missing from StickerBook_Panel.");
                return;
            }

            SerializedObject so = new SerializedObject(stickerBook);
            ValidateObjectReference(so, "_flipPageIndicator", "StickerBook flip page indicator", errors, passes);
            ValidateObjectReference(so, "_nextButton", "StickerBook next button", errors, passes);
            ValidateObjectReference(so, "_prevButton", "StickerBook previous button", errors, passes);
            ValidateObjectReference(so, "_inventoryTrayRoot", "StickerBook inventory tray root", errors, passes);
            ValidateObjectReference(so, "_stickerPrefabTemplate", "StickerBook generic sticker prefab template", errors, passes);
            ValidateButtonTarget("_nextButton", so, errors, passes);
            ValidateButtonTarget("_prevButton", so, errors, passes);

            SerializedProperty pages = so.FindProperty("_pages");
            if (pages == null || pages.arraySize < 2)
            {
                errors.Add("StickerBook must have at least two pages assigned.");
            }
            else
            {
                passes.Add("StickerBook has page references.");
            }

            // Check GameLifetimeScope has _stickerDatabase
            GameLifetimeScope lifetimeScope = FindSceneComponent<GameLifetimeScope>("GameLifetimeScope");
            if (lifetimeScope != null)
            {
                SerializedObject soScope = new SerializedObject(lifetimeScope);
                ValidateObjectReference(soScope, "_stickerDatabase", "GameLifetimeScope sticker database", errors, passes);
            }

            // Check generic template integrity
            CozySticker template = GetReference<CozySticker>(so, "_stickerPrefabTemplate");
            if (template == null)
            {
                errors.Add("StickerBook is missing a valid _stickerPrefabTemplate reference.");
            }
            else
            {
                passes.Add("StickerBook has a valid generic sticker template reference.");
                
                SerializedObject soTemplate = new SerializedObject(template);
                ValidateObjectReference(soTemplate, "_shadowOffset", "Sticker template shadow offset", errors, passes);
                ValidateObjectReference(soTemplate, "_canvasGroup", "Sticker template CanvasGroup", errors, passes);
                ValidateObjectReference(soTemplate, "_visualImage", "Sticker template visual image", errors, passes);
            }
        }

        private static void ValidateBuildSceneAndOrientation(List<string> errors, List<string> passed)
        {
            var scenes = EditorBuildSettings.scenes;
            if (scenes.Length > 0 && scenes[0].path == MainScenePath && scenes[0].enabled)
            {
                passed.Add("Main.unity is the enabled build entry scene at index 0.");
            }
            else
            {
                errors.Add("Main.unity must be the first enabled scene in EditorBuildSettings (index 0).");
            }

            if (PlayerSettings.defaultInterfaceOrientation == UIOrientation.LandscapeLeft)
            {
                passed.Add("Default interface orientation is LandscapeLeft.");
            }
            else
            {
                errors.Add($"Default interface orientation must be LandscapeLeft. Actual: {PlayerSettings.defaultInterfaceOrientation}");
            }

            if (!PlayerSettings.allowedAutorotateToPortrait && !PlayerSettings.allowedAutorotateToPortraitUpsideDown)
            {
                passed.Add("Portrait autorotation is disabled.");
            }
            else
            {
                errors.Add("Portrait and portrait upside-down autorotation must be disabled.");
            }
        }

        private static void PrintResults(List<string> passes, List<string> warnings, List<string> errors)
        {
            foreach (string pass in passes)
            {
                CozyValidationLog.Pass("CozySim Scene", pass);
            }

            foreach (string warning in warnings)
            {
                CozyValidationLog.Warn("CozySim Scene", warning);
            }

            foreach (string error in errors)
            {
                CozyValidationLog.Fail("CozySim Scene", error);
            }

            PrintManualChecklist();

            CozyValidationLog.Summary("CozySim Scene", passes.Count, errors.Count);
        }

        private static void PrintManualChecklist()
        {
            Debug.Log(
                "<color=cyan>[Runtime Playtest Checklist]</color>\n" +
                "1. Start Play Mode from Assets/CozyLifeSim/Scenes/Main.unity.\n" +
                "2. Confirm HUD shows coins, seeds, crops, and level.\n" +
                "3. Open shop and verify locked/unaffordable items explain why.\n" +
                "4. Buy one seed and one sticker when affordable.\n" +
                "5. Try a blocked farm action and confirm the feedback toast appears.\n" +
                "6. Plant, water through all stages, harvest, and sell a crop.\n" +
                "7. Complete at least one quest and confirm dialogue appears.\n" +
                "8. Open scrapbook, place a sticker, cycle page style, add a diary note, drag it, and delete it.\n" +
                "9. Stop and restart Play Mode; confirm persisted state is coherent.\n" +
                "10. Run scene validation after playtest; confirm no scene wiring errors.");
        }

        private static T FindSceneComponent<T>(string objectName) where T : Component
        {
            foreach (T component in FindSceneObjects<T>())
            {
                if (component.gameObject.name == objectName)
                {
                    return component;
                }
            }

            return null;
        }

        private static IEnumerable<T> FindSceneObjects<T>() where T : UnityEngine.Object
        {
            foreach (T item in Resources.FindObjectsOfTypeAll<T>())
            {
                if (item == null || EditorUtility.IsPersistent(item))
                {
                    continue;
                }

                GameObject go = item as GameObject;
                if (go == null && item is Component component)
                {
                    go = component.gameObject;
                }

                if (go == null || !go.scene.IsValid() || !go.scene.isLoaded)
                {
                    continue;
                }

                yield return item;
            }
        }

        private static string GetPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }
    }
}
