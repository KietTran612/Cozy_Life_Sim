using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CozyLifeSim.UI;

namespace CozyLifeSim.Editor
{
    public static partial class CozyLifeSimSceneGameplayValidation
    {
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

        private static T GetReference<T>(SerializedObject so, string propertyName) where T : UnityEngine.Object
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

        private static void ValidateScrapbookAndDialogues(List<string> errors, List<string> passes)
        {
            StickerBook stickerBook = FindSceneComponent<StickerBook>("StickerBook_Panel");
            if (stickerBook != null)
            {
                SerializedObject soBook = new SerializedObject(stickerBook);
                ValidateObjectReference(soBook, "_changeStyleButton", "StickerBook style change button", errors, passes);
                ValidateObjectReference(soBook, "_addDiaryNoteButton", "StickerBook add note button", errors, passes);
                ValidateObjectReference(soBook, "_diaryNotePrefabTemplate", "StickerBook diary note template", errors, passes);
                ValidateObjectReference(soBook, "_diaryInputPopup", "StickerBook diary input popup", errors, passes);

                SerializedProperty pagesProp = soBook.FindProperty("_pages");
                if (pagesProp != null)
                {
                    for (int i = 0; i < pagesProp.arraySize; i++)
                    {
                        var pageVal = pagesProp.GetArrayElementAtIndex(i).objectReferenceValue as StickerBookPage;
                        if (pageVal == null)
                        {
                            errors.Add($"StickerBookPage reference at index {i} is null.");
                        }
                        else
                        {
                            passes.Add($"StickerBookPage at index {i} is assigned.");
                            SerializedObject soPage = new SerializedObject(pageVal);
                            ValidateObjectReference(soPage, "_diaryNotePrefabTemplate", $"StickerBookPage at index {i} diary note template", errors, passes);
                            ValidateObjectReference(soPage, "_backgroundImage", $"StickerBookPage at index {i} page background image", errors, passes);
                        }
                    }
                }
            }

            CozyDialoguePopup dialoguePopup = FindSceneComponent<CozyDialoguePopup>("Dialogue_Popup");
            if (dialoguePopup == null)
            {
                errors.Add("CozyDialoguePopup is missing from the scene.");
            }
            else
            {
                passes.Add("CozyDialoguePopup exists in scene.");
                if (!dialoguePopup.gameObject.activeSelf)
                {
                    errors.Add("CozyDialoguePopup parent GameObject must start active (dialogue system dynamic visibility is handled on Content_Panel).");
                }
                else
                {
                    passes.Add("CozyDialoguePopup parent GameObject is active.");
                }

                SerializedObject soDiag = new SerializedObject(dialoguePopup);
                ValidateObjectReference(soDiag, "_contentPanel", "CozyDialoguePopup._contentPanel", errors, passes);
                ValidateObjectReference(soDiag, "_portrait", "CozyDialoguePopup._portrait", errors, passes);
                ValidateObjectReference(soDiag, "_nameText", "CozyDialoguePopup._nameText", errors, passes);
                ValidateObjectReference(soDiag, "_dialogueText", "CozyDialoguePopup._dialogueText", errors, passes);
                ValidateObjectReference(soDiag, "_nextButton", "CozyDialoguePopup._nextButton", errors, passes);

                RectTransform contentPanel = GetReference<RectTransform>(soDiag, "_contentPanel");
                if (contentPanel != null && contentPanel.gameObject.activeSelf)
                {
                    errors.Add("CozyDialoguePopup Content_Panel should start inactive in the scene.");
                }
                else if (contentPanel != null)
                {
                    passes.Add("CozyDialoguePopup Content_Panel starts inactive.");
                }
            }

            List<CozyNPCWidget> npcWidgets = new List<CozyNPCWidget>();
            foreach (var mb in FindSceneObjects<MonoBehaviour>())
            {
                if (mb is CozyNPCWidget widget)
                {
                    npcWidgets.Add(widget);
                }
            }

            if (npcWidgets.Count == 0)
            {
                errors.Add("No CozyNPCWidget click widgets found in the scene.");
            }
            else
            {
                passes.Add($"Found {npcWidgets.Count} CozyNPCWidget click widgets.");
                foreach (var npc in npcWidgets)
                {
                    SerializedObject soNpc = new SerializedObject(npc);
                    SerializedProperty dataProp = soNpc.FindProperty("_npcData");
                    if (dataProp == null)
                    {
                        errors.Add($"CozyNPCWidget on '{npc.gameObject.name}' is missing '_npcData' field.");
                        continue;
                    }

                    string npcName = dataProp.FindPropertyRelative("NpcName")?.stringValue;
                    if (string.IsNullOrWhiteSpace(npcName))
                    {
                        errors.Add($"CozyNPCWidget on '{npc.gameObject.name}' has empty NpcName.");
                    }
                    else
                    {
                        passes.Add($"CozyNPCWidget '{npcName}' has valid name.");
                    }

                    SerializedProperty dialoguesProp = dataProp.FindPropertyRelative("Dialogues");
                    if (dialoguesProp == null || dialoguesProp.arraySize == 0)
                    {
                        errors.Add($"CozyNPCWidget '{npcName}' on '{npc.gameObject.name}' has 0 dialogues.");
                    }
                    else
                    {
                        passes.Add($"CozyNPCWidget '{npcName}' has {dialoguesProp.arraySize} dialogues.");
                        for (int i = 0; i < dialoguesProp.arraySize; i++)
                        {
                            string line = dialoguesProp.GetArrayElementAtIndex(i).FindPropertyRelative("Line")?.stringValue;
                            if (string.IsNullOrWhiteSpace(line))
                            {
                                errors.Add($"CozyNPCWidget '{npcName}' has empty dialogue line at index {i}.");
                            }
                            else
                            {
                                passes.Add($"CozyNPCWidget '{npcName}' dialogue line {i} is non-empty.");
                            }
                        }
                    }
                }
            }
        }

        private static void ValidateMainCamera(List<string> errors, List<string> passed)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                errors.Add("Scene must have a tagged Main Camera.");
                return;
            }

            if (camera.orthographic)
            {
                passed.Add("Main Camera is orthographic.");
            }
            else
            {
                errors.Add("Main Camera must be orthographic for 2D landscape UI/world presentation.");
            }

            if (Mathf.Approximately(camera.orthographicSize, 5f))
            {
                passed.Add("Main Camera orthographic size is 5.");
            }
            else
            {
                errors.Add($"Main Camera orthographic size must be 5. Actual: {camera.orthographicSize}");
            }

            if (camera.transform.position == new Vector3(0f, 0f, -10f) && camera.transform.rotation == Quaternion.identity)
            {
                passed.Add("Main Camera transform is deterministic.");
            }
            else
            {
                errors.Add("Main Camera must be positioned at (0, 0, -10) with identity rotation.");
            }

            if (camera.clearFlags == CameraClearFlags.SolidColor)
            {
                passed.Add("Main Camera clears to solid color.");
            }
            else
            {
                errors.Add("Main Camera clear flags must be SolidColor.");
            }

            if (Mathf.Approximately(camera.nearClipPlane, 0.3f))
            {
                passed.Add("Main Camera near clip plane is 0.3.");
            }
            else
            {
                errors.Add($"Main Camera near clip plane must be 0.3. Actual: {camera.nearClipPlane}");
            }

            if (Mathf.Approximately(camera.farClipPlane, 1000f))
            {
                passed.Add("Main Camera far clip plane is 1000.");
            }
            else
            {
                errors.Add($"Main Camera far clip plane must be 1000. Actual: {camera.farClipPlane}");
            }

            Color expectedBg = new Color(0.18f, 0.18f, 0.22f);
            if (camera.backgroundColor == expectedBg)
            {
                passed.Add("Main Camera background color is matching standard theme.");
            }
            else
            {
                errors.Add($"Main Camera background color must be {expectedBg}. Actual: {camera.backgroundColor}");
            }

            if (camera.rect == new Rect(0f, 0f, 1f, 1f))
            {
                passed.Add("Main Camera viewport rect is full screen.");
            }
            else
            {
                errors.Add($"Main Camera viewport rect must be (0,0,1,1). Actual: {camera.rect}");
            }

            if (camera.GetComponent<AudioListener>() != null || Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length > 0)
            {
                passed.Add("Main Camera has AudioListener.");
            }
            else
            {
                errors.Add("Main Camera must have AudioListener (or one must exist in the scene).");
            }
        }

        private static void ValidateStartupLoadingOverlay(List<string> errors, List<string> passed)
        {
            var overlay = FindSceneComponent<CozyStartupLoadingOverlay>("Startup_Loading_Overlay");
            if (overlay == null)
            {
                errors.Add("Startup_Loading_Overlay must exist under Popup_Root and have CozyStartupLoadingOverlay.");
                return;
            }

            passed.Add("Startup loading overlay has CozyStartupLoadingOverlay.");

            var canvasGroup = overlay.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                passed.Add("Startup loading overlay has CanvasGroup.");
            }
            else
            {
                errors.Add("Startup_Loading_Overlay must have CanvasGroup for fade/input blocking.");
            }

            SerializedObject soOverlay = new SerializedObject(overlay);
            ValidateObjectReference(soOverlay, "canvasGroup", "CozyStartupLoadingOverlay canvas group", errors, passed);
            ValidateObjectReference(soOverlay, "messageText", "CozyStartupLoadingOverlay message text", errors, passed);
        }

        private static void ValidateLandscapeLayout(List<string> errors, List<string> passed)
        {
            var canvas = FindSceneComponent<Canvas>("Canvas");
            var scaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
            if (scaler == null)
            {
                errors.Add("Root Canvas must have CanvasScaler.");
                return;
            }

            if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize &&
                scaler.referenceResolution == new Vector2(1920f, 1080f) &&
                scaler.screenMatchMode == CanvasScaler.ScreenMatchMode.MatchWidthOrHeight)
            {
                passed.Add("CanvasScaler is configured for landscape reference resolution.");
            }
            else
            {
                errors.Add("CanvasScaler must use ScaleWithScreenSize, 1920x1080, MatchWidthOrHeight.");
            }

            var controller = FindSceneComponent<CozyLandscapeLayoutController>("Landscape_Layout_Controller");
            if (controller != null)
            {
                passed.Add("Landscape layout controller exists.");
                SerializedObject soLayout = new SerializedObject(controller);
                ValidateObjectReference(soLayout, "header", "Landscape layout header", errors, passed);
                ValidateObjectReference(soLayout, "gameplayArea", "Landscape layout gameplay area", errors, passed);
                ValidateObjectReference(soLayout, "farmPlot", "Landscape layout farm plot", errors, passed);
                ValidateObjectReference(soLayout, "animalPen", "Landscape layout animal pen", errors, passed);
                ValidateObjectReference(soLayout, "stickerBookPanel", "Landscape layout sticker book panel", errors, passed);
                ValidateObjectReference(soLayout, "inventoryTray", "Landscape layout inventory tray", errors, passed);
                ValidateObjectReference(soLayout, "sidebar", "Landscape layout sidebar", errors, passed);
                ValidateObjectReference(soLayout, "popupRoot", "Landscape layout popup root", errors, passed);
            }
            else
            {
                errors.Add("Scene must include CozyLandscapeLayoutController.");
            }
        }

        private static void ValidateUISpriteAssignments(List<string> errors, List<string> passed)
        {
            // Helper to get image component
            Image GetImage(string path)
            {
                if (path.StartsWith("Canvas/"))
                {
                    var canvasGo = GameObject.Find("Canvas");
                    if (canvasGo != null)
                    {
                        string[] parts = path.Substring(7).Split('/');
                        Transform current = canvasGo.transform;
                        foreach (string part in parts)
                        {
                            current = current.Find(part);
                            if (current == null) break;
                        }
                        return current != null ? current.GetComponent<Image>() : null;
                    }
                }
                var go = GameObject.Find(path);
                return go != null ? go.GetComponent<Image>() : null;
            }

            void CheckImageSprite(string path, string expectedSpriteName, Image.Type expectedType, List<string> err, List<string> pass)
            {
                var img = GetImage(path);
                if (img == null)
                {
                    err.Add($"UI Image at path '{path}' was not found.");
                    return;
                }
                if (img.sprite == null)
                {
                    err.Add($"UI Image '{path}' has no sprite assigned.");
                    return;
                }
                if (img.sprite.name != expectedSpriteName)
                {
                    err.Add($"UI Image '{path}' sprite name is '{img.sprite.name}', expected '{expectedSpriteName}'.");
                    return;
                }
                if (img.type != expectedType)
                {
                    err.Add($"UI Image '{path}' type is '{img.type}', expected '{expectedType}'.");
                    return;
                }
                pass.Add($"UI Image '{path}' is correctly assigned '{expectedSpriteName}' ({expectedType}).");
            }

            CheckImageSprite("Canvas/UI_Root/Quest_Popup/Content_Panel", "UI_Panel_Frame_Wood", Image.Type.Sliced, errors, passed);
            CheckImageSprite("Canvas/UI_Root/Shop_Popup/Content_Panel", "UI_Panel_Frame_Wood", Image.Type.Sliced, errors, passed);
            CheckImageSprite("Canvas/UI_Root/Diary_Input_Popup/Content_Panel", "UI_Panel_Frame_Wood", Image.Type.Sliced, errors, passed);
            CheckImageSprite("Canvas/UI_Root/Dialogue_Popup/Content_Panel", "UI_Dialogue_Bubble", Image.Type.Sliced, errors, passed);
            CheckImageSprite("Canvas/UI_Root/Sidebar_Panel", "UI_Panel_Frame_Wood", Image.Type.Sliced, errors, passed);
            CheckImageSprite("Canvas/UI_Root/Sidebar_Panel/Quest_Button", "UI_Tab_Button_Bg", Image.Type.Sliced, errors, passed);
            CheckImageSprite("Canvas/UI_Root/Sidebar_Panel/Shop_Button", "UI_Tab_Button_Bg", Image.Type.Sliced, errors, passed);
            CheckImageSprite("Canvas/UI_Root/Sidebar_Panel/Quest_Button/Quest_Icon", "UI_Icon_Quest", Image.Type.Simple, errors, passed);
            CheckImageSprite("Canvas/UI_Root/Sidebar_Panel/Shop_Button/Shop_Icon", "UI_Icon_Shop", Image.Type.Simple, errors, passed);
            CheckImageSprite("Canvas/UI_Root/Gameplay_Area/StickerBook_Panel", "UI_Scrapbook_Notebook_Open", Image.Type.Sliced, errors, passed);
            CheckImageSprite("Canvas/UI_Root/Gameplay_Area/StickerBook_Panel/Prev_Button", "UI_Button_Page_Arrow", Image.Type.Simple, errors, passed);
            CheckImageSprite("Canvas/UI_Root/Gameplay_Area/StickerBook_Panel/Next_Button", "UI_Button_Page_Arrow", Image.Type.Simple, errors, passed);
            CheckImageSprite("Canvas/Prefabs_Holder/Diary_Note_Template", "Scrapbook_StickyNote_Yellow", Image.Type.Sliced, errors, passed);
            CheckImageSprite("Canvas/UI_Root/Gameplay_Area/StickerBook_Panel/Trash_Can", "UI_Icon_Trash_Can", Image.Type.Simple, errors, passed);
            CheckImageSprite("Canvas/UI_Root/Dialogue_Popup/Content_Panel/Dialogue_Indicator", "UI_Dialogue_Indicator", Image.Type.Simple, errors, passed);
            CheckImageSprite("Canvas/UI_Root/Gameplay_Area/Farm_Plot/Water_Status_Icon", "UI_Icon_Status_Water", Image.Type.Simple, errors, passed);

            // Validate QuestPopup serialized fields
            var questPopup = FindSceneComponent<QuestPopup>("Quest_Popup");
            if (questPopup != null)
            {
                var so = new SerializedObject(questPopup);
                var itemBgProp = so.FindProperty("_itemBgSprite");
                var waterIconProp = so.FindProperty("_questWaterIcon");
                var harvestIconProp = so.FindProperty("_questHarvestIcon");
                var petIconProp = so.FindProperty("_questPetIcon");
                var completedStampProp = so.FindProperty("_questCompletedStamp");

                void CheckSerializedSprite(SerializedProperty prop, string expectedName, string fieldName)
                {
                    if (prop == null || prop.objectReferenceValue == null)
                    {
                        errors.Add($"QuestPopup field '{fieldName}' is not assigned.");
                    }
                    else if (prop.objectReferenceValue.name != expectedName)
                    {
                        errors.Add($"QuestPopup field '{fieldName}' is assigned '{prop.objectReferenceValue.name}', expected '{expectedName}'.");
                    }
                    else
                    {
                        passed.Add($"QuestPopup field '{fieldName}' is correctly assigned '{expectedName}'.");
                    }
                }

                CheckSerializedSprite(itemBgProp, "UI_Quest_Item_Bg", "_itemBgSprite");
                CheckSerializedSprite(waterIconProp, "UI_Icon_QuestType_Water", "_questWaterIcon");
                CheckSerializedSprite(harvestIconProp, "UI_Icon_QuestType_Harvest", "_questHarvestIcon");
                CheckSerializedSprite(petIconProp, "UI_Icon_QuestType_Pet", "_questPetIcon");
                CheckSerializedSprite(completedStampProp, "UI_Quest_Stamp_Completed", "_questCompletedStamp");
            }

            // Validate InventoryHudWidget serialized fields
            var inventoryHud = FindSceneComponent<InventoryHudWidget>("Header_Panel");
            if (inventoryHud != null)
            {
                var so = new SerializedObject(inventoryHud);
                void CheckImageField(string fieldName, string expectedSpriteName, Image.Type expectedType)
                {
                    var imgProp = so.FindProperty(fieldName);
                    if (imgProp == null || imgProp.objectReferenceValue == null)
                    {
                        errors.Add($"InventoryHudWidget field '{fieldName}' is not assigned.");
                        return;
                    }
                    var img = imgProp.objectReferenceValue as Image;
                    if (img == null)
                    {
                        errors.Add($"InventoryHudWidget field '{fieldName}' is not an Image.");
                        return;
                    }
                    if (img.sprite == null)
                    {
                        errors.Add($"InventoryHudWidget field '{fieldName}' Image has no sprite assigned.");
                    }
                    else if (img.sprite.name != expectedSpriteName)
                    {
                        errors.Add($"InventoryHudWidget field '{fieldName}' Image sprite is '{img.sprite.name}', expected '{expectedSpriteName}'.");
                    }
                    else if (img.type != expectedType)
                    {
                        errors.Add($"InventoryHudWidget field '{fieldName}' Image type is '{img.type}', expected '{expectedType}'.");
                    }
                    else
                    {
                        passed.Add($"InventoryHudWidget field '{fieldName}' is correctly assigned '{expectedSpriteName}' ({expectedType}).");
                    }
                }

                CheckImageField("_autosaveIcon", "UI_Icon_Autosave", Image.Type.Simple);
                CheckImageField("_coinIcon", "System_Coin_Vietnamese", Image.Type.Simple);
                CheckImageField("_seedsIcon", "System_Icon_Seeds", Image.Type.Simple);
                CheckImageField("_cropsIcon", "System_Icon_Crops", Image.Type.Simple);
            }

            // Validate ProgressionHudWidget serialized fields
            var progressionHud = FindSceneComponent<ProgressionHudWidget>("Progression_HUD");
            if (progressionHud != null)
            {
                var so = new SerializedObject(progressionHud);
                var imgProp = so.FindProperty("_levelStarImage");
                if (imgProp == null || imgProp.objectReferenceValue == null)
                {
                    errors.Add("ProgressionHudWidget field '_levelStarImage' is not assigned.");
                }
                else
                {
                    var img = imgProp.objectReferenceValue as Image;
                    if (img == null)
                    {
                        errors.Add("ProgressionHudWidget field '_levelStarImage' is not an Image.");
                    }
                    else if (img.sprite == null)
                    {
                        errors.Add("ProgressionHudWidget field '_levelStarImage' Image has no sprite assigned.");
                    }
                    else if (img.sprite.name != "System_Icon_Level_Star")
                    {
                        errors.Add($"ProgressionHudWidget field '_levelStarImage' Image sprite is '{img.sprite.name}', expected 'System_Icon_Level_Star'.");
                    }
                    else if (img.type != Image.Type.Simple)
                    {
                        errors.Add($"ProgressionHudWidget field '_levelStarImage' Image type is '{img.type}', expected 'Simple'.");
                    }
                    else
                    {
                        passed.Add("ProgressionHudWidget field '_levelStarImage' is correctly assigned 'System_Icon_Level_Star' (Simple).");
                    }
                }
            }
        }

    }
}
