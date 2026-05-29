using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CozyLifeSim.UI;

namespace CozyLifeSim.Editor
{
    public static class CozyLifeSimSceneGameplayValidation
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
            ValidateRequiredSprites(errors, passes);
            ValidateLifetimeAndCanvas(errors, warnings, passes);
            ValidateInventoryHud(errors, passes);
            ValidateFarmLoop(errors, warnings, passes);
            ValidateAnimalLoop(errors, passes);
            ValidateStickerLoop(errors, warnings, passes);
            ValidatePopupAndNavigationDocks(errors, passes);

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

        private static void ValidateRequiredSprites(List<string> errors, List<string> passes)
        {
            foreach (KeyValuePair<string, string> pair in RequiredSprites)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pair.Value);
                if (sprite == null)
                {
                    errors.Add($"Required sprite '{pair.Key}' could not be loaded at '{pair.Value}'.");
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

        private static void ValidateInventoryHud(List<string> errors, List<string> passes)
        {
            InventoryHudWidget hud = FindSceneComponent<InventoryHudWidget>("Header_Panel");
            if (hud == null)
            {
                errors.Add("InventoryHudWidget is missing from Header_Panel.");
                return;
            }

            SerializedObject so = new SerializedObject(hud);
            ValidateObjectReference(so, "_coinsText", "Inventory HUD coins text", errors, passes);
            ValidateObjectReference(so, "_seedsText", "Inventory HUD seeds text", errors, passes);
            ValidateObjectReference(so, "_cropsText", "Inventory HUD crops text", errors, passes);
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
                else if (wateringCanImage.sprite != expected)
                {
                    errors.Add("Watering_Can Image sprite is not Watering-Can-Pink-256.png.");
                }
                else
                {
                    passes.Add("Watering_Can uses the CuteKawaii watering can sprite.");
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

        private static void ValidatePopupAndNavigationDocks(List<string> errors, List<string> passes)
        {
            RectTransform sidebar = FindSceneComponent<RectTransform>("Sidebar_Panel");
            if (sidebar == null)
            {
                errors.Add("Sidebar_Panel navigation dock is missing.");
            }
            else
            {
                passes.Add("Sidebar_Panel navigation dock exists.");
                if (sidebar.GetComponent<CozySidebar>() == null)
                {
                    errors.Add("Sidebar_Panel is missing CozySidebar.");
                }
                else
                {
                    passes.Add("Sidebar_Panel has CozySidebar.");
                }

                ValidateChildButton(sidebar.transform, "Quest_Button", errors, passes);
                ValidateChildButton(sidebar.transform, "Shop_Button", errors, passes);
                ValidateSidebarChildren(sidebar.transform, errors, passes);
            }

            QuestPopup questPopup = FindSceneComponent<QuestPopup>("Quest_Popup");
            ShopPopup shopPopup = FindSceneComponent<ShopPopup>("Shop_Popup");
            ValidatePopup("Quest_Popup", questPopup, errors, passes);
            ValidatePopup("Shop_Popup", shopPopup, errors, passes);
            ValidateInteractiveObject("Quest_Board", questPopup, errors, passes);
            ValidateInteractiveObject("Shop_Stall", shopPopup, errors, passes);
        }

        private static void ValidatePopup(string label, CozyPopup popup, List<string> errors, List<string> passes)
        {
            if (popup == null)
            {
                errors.Add($"{label} popup component is missing.");
                return;
            }

            passes.Add($"{label} popup component exists.");
            SerializedObject so = new SerializedObject(popup);
            ValidateObjectReference(so, "_contentPanel", $"{label} content panel", errors, passes);
            ValidateObjectReference(so, "_backgroundDim", $"{label} background dim CanvasGroup", errors, passes);
            ValidateObjectReference(so, "_closeButton", $"{label} close button", errors, passes);

            CanvasGroup dimGroup = GetReference<CanvasGroup>(so, "_backgroundDim");
            if (dimGroup != null)
            {
                Image dimImage = dimGroup.GetComponent<Image>();
                if (dimImage == null)
                {
                    errors.Add($"{label} background dim is missing Image.");
                }
                else if (!dimImage.raycastTarget)
                {
                    errors.Add($"{label} background dim Image must block raycasts.");
                }
                else
                {
                    passes.Add($"{label} background dim Image blocks raycasts.");
                }

                if (!dimGroup.blocksRaycasts)
                {
                    errors.Add($"{label} background dim CanvasGroup.blocksRaycasts should be enabled in the serialized setup.");
                }
                else
                {
                    passes.Add($"{label} background dim CanvasGroup blocks raycasts.");
                }
            }
        }

        private static void ValidateInteractiveObject(string objectName, CozyPopup expectedPopup, List<string> errors, List<string> passes)
        {
            CozyInteractiveObject interactive = FindSceneComponent<CozyInteractiveObject>(objectName);
            if (interactive == null)
            {
                errors.Add($"{objectName} is missing CozyInteractiveObject.");
                return;
            }

            passes.Add($"{objectName} has CozyInteractiveObject.");

            if (interactive.GetComponent<BoxCollider2D>() == null)
            {
                errors.Add($"{objectName} is missing BoxCollider2D.");
            }
            else
            {
                passes.Add($"{objectName} has BoxCollider2D.");
            }

            SpriteRenderer renderer = interactive.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                errors.Add($"{objectName} is missing SpriteRenderer visual.");
            }
            else if (renderer.sprite == null)
            {
                errors.Add($"{objectName} SpriteRenderer has no sprite assigned.");
            }
            else
            {
                passes.Add($"{objectName} has a visible SpriteRenderer.");
            }

            SerializedObject so = new SerializedObject(interactive);
            SerializedProperty targetPopup = so.FindProperty("_targetPopup");
            if (targetPopup == null)
            {
                errors.Add($"{objectName} target popup serialized property was not found.");
            }
            else if (targetPopup.objectReferenceValue == null)
            {
                errors.Add($"{objectName} target popup is not assigned.");
            }
            else if (expectedPopup != null && targetPopup.objectReferenceValue != expectedPopup)
            {
                errors.Add($"{objectName} target popup points to the wrong popup.");
            }
            else
            {
                passes.Add($"{objectName} target popup is assigned correctly.");
            }
        }

        private static void ValidateSidebarChildren(Transform sidebar, List<string> errors, List<string> passes)
        {
            HashSet<string> allowedChildren = new HashSet<string> { "Quest_Button", "Shop_Button" };
            bool hasUnexpectedChild = false;
            for (int i = 0; i < sidebar.childCount; i++)
            {
                Transform child = sidebar.GetChild(i);
                if (!allowedChildren.Contains(child.name))
                {
                    hasUnexpectedChild = true;
                    errors.Add($"Sidebar_Panel contains legacy or unexpected child '{child.name}'. Regenerate should leave only Quest_Button and Shop_Button.");
                }
            }

            if (!hasUnexpectedChild && sidebar.childCount == allowedChildren.Count)
            {
                passes.Add("Sidebar_Panel contains only the expected navigation buttons.");
            }
        }

        private static void ValidateChildButton(Transform root, string childName, List<string> errors, List<string> passes)
        {
            Transform child = root.Find(childName);
            if (child == null)
            {
                errors.Add($"{childName} is missing from Sidebar_Panel.");
                return;
            }

            if (child.GetComponent<Button>() == null)
            {
                errors.Add($"{childName} is missing Button.");
            }
            else
            {
                passes.Add($"{childName} Button exists.");
            }
        }

        private static void ValidateStickerTemplate(
            CozySticker sticker,
            int expectedId,
            string expectedSpriteKey,
            List<string> errors,
            List<string> warnings,
            List<string> passes)
        {
            if (sticker == null)
            {
                errors.Add($"Sticker template {expectedId} is null.");
                return;
            }

            SerializedObject so = new SerializedObject(sticker);
            SerializedProperty stickerId = so.FindProperty("_stickerId");
            if (stickerId == null || stickerId.intValue != expectedId)
            {
                errors.Add($"Sticker template '{sticker.name}' has wrong sticker id. Expected {expectedId}.");
            }
            else
            {
                passes.Add($"Sticker template '{sticker.name}' has id {expectedId}.");
            }

            ValidateObjectReference(so, "_shadowOffset", $"Sticker '{sticker.name}' shadow offset", errors, passes);
            ValidateObjectReference(so, "_canvasGroup", $"Sticker '{sticker.name}' CanvasGroup", errors, passes);

            Image visual = FindChildImage(sticker.transform, "Visual_Image");
            Image shadow = FindChildImage(sticker.transform, "Shadow_Offset");
            ValidateImageSprite(visual, $"{sticker.name}/Visual_Image", expectedSpriteKey, errors, passes);
            ValidateImageSprite(shadow, $"{sticker.name}/Shadow_Offset", expectedSpriteKey, errors, passes);

            if (shadow != null && shadow.color.a > 0.4f)
            {
                warnings.Add($"Sticker '{sticker.name}' shadow alpha is {shadow.color.a:0.00}. Expected a subtle silhouette around 0.30.");
            }
        }

        private static void ValidateObjectReference(
            SerializedObject so,
            string propertyName,
            string label,
            List<string> errors,
            List<string> passes)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property == null)
            {
                errors.Add($"{label}: serialized property '{propertyName}' was not found.");
            }
            else if (property.objectReferenceValue == null)
            {
                errors.Add($"{label} is not assigned.");
            }
            else
            {
                passes.Add($"{label} is assigned.");
            }
        }

        private static void ValidateSpriteReference(
            SerializedObject so,
            string propertyName,
            string label,
            string expectedSpriteKey,
            List<string> errors,
            List<string> passes)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            Sprite expected = LoadRequiredSprite(expectedSpriteKey);
            if (property == null)
            {
                errors.Add($"{label}: serialized property '{propertyName}' was not found.");
            }
            else if (property.objectReferenceValue == null)
            {
                errors.Add($"{label} is not assigned.");
            }
            else if (expected != null && property.objectReferenceValue != expected)
            {
                errors.Add($"{label} is not assigned to '{RequiredSprites[expectedSpriteKey]}'.");
            }
            else
            {
                passes.Add($"{label} uses expected CuteKawaii sprite.");
            }
        }

        private static void ValidateImageSprite(
            Image image,
            string label,
            string expectedSpriteKey,
            List<string> errors,
            List<string> passes)
        {
            Sprite expected = LoadRequiredSprite(expectedSpriteKey);
            if (image == null)
            {
                errors.Add($"{label} Image component is missing.");
            }
            else if (image.sprite == null)
            {
                errors.Add($"{label} sprite is not assigned.");
            }
            else if (expected != null && image.sprite != expected)
            {
                errors.Add($"{label} does not use '{RequiredSprites[expectedSpriteKey]}'.");
            }
            else
            {
                passes.Add($"{label} uses expected CuteKawaii sprite.");
            }
        }

        private static void ValidateButtonTarget(
            string propertyName,
            SerializedObject owner,
            List<string> errors,
            List<string> passes)
        {
            Button button = GetReference<Button>(owner, propertyName);
            if (button == null)
            {
                return;
            }

            if (button.targetGraphic == null)
            {
                errors.Add($"{button.name} has no target graphic. Click area and button visual transitions may be unreliable.");
            }
            else
            {
                passes.Add($"{button.name} has a target graphic.");
            }
        }

        private static T GetReference<T>(SerializedObject so, string propertyName) where T : Object
        {
            SerializedProperty property = so.FindProperty(propertyName);
            return property == null ? null : property.objectReferenceValue as T;
        }

        private static Sprite LoadRequiredSprite(string key)
        {
            return RequiredSprites.TryGetValue(key, out string path)
                ? AssetDatabase.LoadAssetAtPath<Sprite>(path)
                : null;
        }

        private static Image FindChildImage(Transform root, string childName)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName)
                {
                    return child.GetComponent<Image>();
                }
            }

            return null;
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

        private static IEnumerable<T> FindSceneObjects<T>() where T : Object
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
                "<color=cyan>[Manual Gameplay Loop Checklist]</color>\n" +
                "1. Enter Play Mode in Assets/CozyLifeSim/Scenes/Main.unity.\n" +
                "2. Confirm HUD starts from saved/default Coins, Seeds, and Crops values.\n" +
                "3. Click Plant Seed, then Water. Confirm watering can animates and crop stage/timer advances.\n" +
                "4. Wait for maturity, click Harvest, and confirm Coins/Crops update in HUD and PlayerPrefs persists.\n" +
                "5. Click Pet Chicken. Confirm Coins reward updates and a heart floats from Spawn_Root.\n" +
                "6. Drag Bunny and Bear stickers onto StickerBook pages, flip pages, exit Play Mode, re-enter, and confirm placements restore.");
        }
    }
}
