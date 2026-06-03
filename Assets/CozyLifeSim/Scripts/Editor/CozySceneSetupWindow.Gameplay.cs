using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CozyLifeSim.UI;

namespace CozyLifeSim.Editor
{
    public partial class CozySceneSetupWindow
    {
        private void ConfigureFarmPlot(
            RectTransform gameplayArea,
            Sprite defaultSprite,
            Sprite seedSprite,
            Sprite sproutSprite,
            Sprite matureSprite,
            Sprite wateringCanSprite,
            ref bool isSceneDirty)
        {
            // 6. Setup Farm Plot (Trong trot)
            RectTransform farmPlot = SetupPanel(gameplayArea, "Farm_Plot", ref isSceneDirty);
            SafeSetSizeDelta(farmPlot, new Vector2(350f, 500f), ref isSceneDirty);
            CropWidget cropWidget = farmPlot.gameObject.GetComponent<CropWidget>();
            if (cropWidget == null)
            {
                cropWidget = farmPlot.gameObject.AddComponent<CropWidget>();
                isSceneDirty = true;
            }

            Image cropVisual = SetupImage(farmPlot, "Crop_Visual", ref isSceneDirty);
            ConfigureSimpleImage(cropVisual, true, ref isSceneDirty);
            ConfigureFixedVisualRect(cropVisual.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(150f, 150f), Vector2.zero, ref isSceneDirty);

            TextMeshProUGUI timerText = SetupText(farmPlot, "Timer_Text", "EMPTY SOIL", "", ref isSceneDirty);
            Button plantBtn = SetupButton(farmPlot, "Plant_Button", "Plant Seed", ref isSceneDirty);
            Button waterBtn = SetupButton(farmPlot, "Water_Button", "Water", ref isSceneDirty);
            Button harvestBtn = SetupButton(farmPlot, "Harvest_Button", "Harvest", ref isSceneDirty);
            RectTransform wateringCan = SetupPanel(farmPlot, "Watering_Can", ref isSceneDirty);

            // Note: Since wateringCan state is checked in playmode, we only set active if state changes
            if (wateringCan.gameObject.activeSelf)
            {
                wateringCan.gameObject.SetActive(false);
                isSceneDirty = true;
            }

            // Create Water_Status_Icon child
            RectTransform waterStatusIcon = SetupPanel(farmPlot, "Water_Status_Icon", ref isSceneDirty);
            LayoutElement waterStatusLayout = waterStatusIcon.gameObject.GetComponent<LayoutElement>();
            if (waterStatusLayout == null)
            {
                waterStatusLayout = waterStatusIcon.gameObject.AddComponent<LayoutElement>();
                isSceneDirty = true;
            }
            if (!waterStatusLayout.ignoreLayout)
            {
                waterStatusLayout.ignoreLayout = true;
                isSceneDirty = true;
            }

            Image waterStatusImg = waterStatusIcon.gameObject.GetComponent<Image>();
            if (waterStatusImg == null)
            {
                waterStatusImg = waterStatusIcon.gameObject.AddComponent<Image>();
                isSceneDirty = true;
            }
            var waterStatusSprite = LoadSprite("UI_Icon_Status_Water");
            if (waterStatusImg.sprite != waterStatusSprite)
            {
                waterStatusImg.sprite = waterStatusSprite;
                isSceneDirty = true;
            }
            ConfigureSimpleImage(waterStatusImg, true, ref isSceneDirty);
            ConfigureFixedVisualRect(waterStatusIcon, new Vector2(0.5f, 0.5f), new Vector2(34f, 34f), new Vector2(0f, 60f), ref isSceneDirty);

            // Vertical layout for Farm components
            VerticalLayoutGroup farmLayout = farmPlot.gameObject.GetComponent<VerticalLayoutGroup>();
            if (farmLayout == null)
            {
                farmLayout = farmPlot.gameObject.AddComponent<VerticalLayoutGroup>();
                isSceneDirty = true;
            }
            if (farmLayout.childAlignment != TextAnchor.MiddleCenter) { farmLayout.childAlignment = TextAnchor.MiddleCenter; isSceneDirty = true; }
            if (!Mathf.Approximately(farmLayout.spacing, 15f)) { farmLayout.spacing = 15f; isSceneDirty = true; }
            if (farmLayout.childControlHeight) { farmLayout.childControlHeight = false; isSceneDirty = true; }
            if (farmLayout.childControlWidth) { farmLayout.childControlWidth = false; isSceneDirty = true; }

            // Setup watering can image visual
            Image wateringCanImg = wateringCan.gameObject.GetComponent<Image>();
            if (wateringCanImg == null)
            {
                wateringCanImg = wateringCan.gameObject.AddComponent<Image>();
                isSceneDirty = true;
            }
            Sprite expectedWCanSprite = wateringCanSprite != null ? wateringCanSprite : defaultSprite;
            if (wateringCanImg.sprite != expectedWCanSprite)
            {
                wateringCanImg.sprite = expectedWCanSprite;
                isSceneDirty = true;
            }
            ConfigureSimpleImage(wateringCanImg, true, ref isSceneDirty);
            ConfigureFixedVisualRect(wateringCan, new Vector2(0.5f, 0.5f), new Vector2(72f, 72f), Vector2.zero, ref isSceneDirty);

            // Auto-wire CropWidget via SerializedObject
            SerializedObject soCrop = new SerializedObject(cropWidget);
            bool cropDirty = false;
            SafeSetInt(soCrop.FindProperty("_cropId"), 1, ref cropDirty);
            SafeSetFloat(soCrop.FindProperty("_stageDurationSeconds"), 5f, ref cropDirty);
            SafeSetObjectReference(soCrop.FindProperty("_cropVisual"), cropVisual, ref cropDirty);
            SafeSetObjectReference(soCrop.FindProperty("_timerText"), timerText, ref cropDirty);
            SafeSetObjectReference(soCrop.FindProperty("_waterButton"), waterBtn, ref cropDirty);
            SafeSetObjectReference(soCrop.FindProperty("_wateringCan"), wateringCan, ref cropDirty);
            SafeSetObjectReference(soCrop.FindProperty("_plantButton"), plantBtn, ref cropDirty);
            SafeSetObjectReference(soCrop.FindProperty("_harvestButton"), harvestBtn, ref cropDirty);
            SafeSetObjectReference(soCrop.FindProperty("_waterStatusIcon"), waterStatusImg, ref cropDirty);

            SafeSetObjectReference(soCrop.FindProperty("_seedSprite"), seedSprite != null ? seedSprite : defaultSprite, ref cropDirty);
            SafeSetObjectReference(soCrop.FindProperty("_sproutSprite"), sproutSprite != null ? sproutSprite : defaultSprite, ref cropDirty);
            SafeSetObjectReference(soCrop.FindProperty("_matureSprite"), matureSprite != null ? matureSprite : defaultSprite, ref cropDirty);
            SafeSetObjectReference(soCrop.FindProperty("_harvestSprite"), matureSprite != null ? matureSprite : defaultSprite, ref cropDirty);

            if (cropDirty)
            {
                soCrop.ApplyModifiedProperties();
                isSceneDirty = true;
            }
        }

        private void ConfigureAnimalPen(
            RectTransform gameplayArea,
            Sprite defaultSprite,
            Sprite chickenSprite,
            Sprite heartSprite,
            RectTransform prefabsHolder,
            ref bool isSceneDirty)
        {
            // 7. Setup Animal Pen (Nuoi ga)
            RectTransform animalPen = SetupPanel(gameplayArea, "Animal_Pen", ref isSceneDirty);
            SafeSetSizeDelta(animalPen, new Vector2(350f, 500f), ref isSceneDirty);
            AnimalWidget animalWidget = animalPen.gameObject.GetComponent<AnimalWidget>();
            if (animalWidget == null)
            {
                animalWidget = animalPen.gameObject.AddComponent<AnimalWidget>();
                isSceneDirty = true;
            }

            Image animalVisual = SetupImage(animalPen, "Animal_Visual", ref isSceneDirty);
            ConfigureSimpleImage(animalVisual, true, ref isSceneDirty);
            ConfigureFixedVisualRect(animalVisual.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(150f, 150f), Vector2.zero, ref isSceneDirty);

            Button feedBtn = SetupButton(animalPen, "Feed_Button", "Pet Animal", ref isSceneDirty);
            RectTransform spawnRoot = SetupPanel(animalPen, "Spawn_Root", ref isSceneDirty);

            // Vertical layout for Animal components
            VerticalLayoutGroup animalLayout = animalPen.gameObject.GetComponent<VerticalLayoutGroup>();
            if (animalLayout == null)
            {
                animalLayout = animalPen.gameObject.AddComponent<VerticalLayoutGroup>();
                isSceneDirty = true;
            }
            if (animalLayout.childAlignment != TextAnchor.MiddleCenter) { animalLayout.childAlignment = TextAnchor.MiddleCenter; isSceneDirty = true; }
            if (!Mathf.Approximately(animalLayout.spacing, 15f)) { animalLayout.spacing = 15f; isSceneDirty = true; }
            if (animalLayout.childControlHeight) { animalLayout.childControlHeight = false; isSceneDirty = true; }
            if (animalLayout.childControlWidth) { animalLayout.childControlWidth = false; isSceneDirty = true; }

            // Create Heart template inside Prefabs_Holder for popup animation
            RectTransform heartTemplate = SetupPanel(prefabsHolder, "Heart_Feedback_Template", ref isSceneDirty);
            if (heartTemplate.gameObject.activeSelf)
            {
                heartTemplate.gameObject.SetActive(false);
                isSceneDirty = true;
            }
            Image heartImg = heartTemplate.gameObject.GetComponent<Image>();
            if (heartImg == null)
            {
                heartImg = heartTemplate.gameObject.AddComponent<Image>();
                isSceneDirty = true;
            }
            Sprite expectedHeartSprite = heartSprite != null ? heartSprite : defaultSprite;
            if (heartImg.sprite != expectedHeartSprite)
            {
                heartImg.sprite = expectedHeartSprite;
                isSceneDirty = true;
            }
            ConfigureSimpleImage(heartImg, true, ref isSceneDirty);
            ConfigureFixedVisualRect(heartTemplate, new Vector2(0.5f, 0.5f), new Vector2(44f, 44f), Vector2.zero, ref isSceneDirty);

            // Wire animal widget visual
            Image animalImg = animalVisual.gameObject.GetComponent<Image>();
            if (animalImg != null)
            {
                Sprite expectedAnimalSprite = chickenSprite != null ? chickenSprite : defaultSprite;
                if (animalImg.sprite != expectedAnimalSprite)
                {
                    animalImg.sprite = expectedAnimalSprite;
                    isSceneDirty = true;
                }
                ConfigureSimpleImage(animalImg, true, ref isSceneDirty);
            }

            // Auto-wire AnimalWidget
            SerializedObject soAnimal = new SerializedObject(animalWidget);
            bool animalDirty = false;
            SafeSetInt(soAnimal.FindProperty("_animalId"), 1, ref animalDirty);
            SafeSetObjectReference(soAnimal.FindProperty("_animalVisual"), animalVisual, ref animalDirty);
            SafeSetObjectReference(soAnimal.FindProperty("_petButton"), feedBtn, ref animalDirty);
            SafeSetObjectReference(soAnimal.FindProperty("_heartSpawnRoot"), spawnRoot, ref animalDirty);
            SafeSetObjectReference(soAnimal.FindProperty("_heartPrefab"), heartTemplate, ref animalDirty);
            if (animalDirty)
            {
                soAnimal.ApplyModifiedProperties();
                isSceneDirty = true;
            }
        }

        private void ConfigureStickerBook(
            RectTransform gameplayArea,
            ref bool isSceneDirty,
            out StickerBookPage bookPage0,
            out StickerBookPage bookPage1,
            out StickerBook stickerBook)
        {
            // 8. Setup Sticker Book
            RectTransform stickerBookPanel = SetupPanel(gameplayArea, "StickerBook_Panel", ref isSceneDirty);
            SafeSetSizeDelta(stickerBookPanel, new Vector2(500f, 500f), ref isSceneDirty);
            stickerBook = stickerBookPanel.gameObject.GetComponent<StickerBook>();
            if (stickerBook == null)
            {
                stickerBook = stickerBookPanel.gameObject.AddComponent<StickerBook>();
                isSceneDirty = true;
            }

            Image bookPanelImg = stickerBookPanel.gameObject.GetComponent<Image>();
            if (bookPanelImg == null)
            {
                bookPanelImg = stickerBookPanel.gameObject.AddComponent<Image>();
                isSceneDirty = true;
            }
            var notebookOpenSprite = LoadSprite("UI_Scrapbook_Notebook_Open");
            if (bookPanelImg.sprite != notebookOpenSprite)
            {
                bookPanelImg.sprite = notebookOpenSprite;
                isSceneDirty = true;
            }
            ConfigureSlicedImage(bookPanelImg, ref isSceneDirty);

            Button prevBtn = SetupButton(stickerBookPanel, "Prev_Button", "< Page", ref isSceneDirty);
            Image prevBtnImg = prevBtn.GetComponent<Image>();
            if (prevBtnImg != null)
            {
                var pageArrowSprite = LoadSprite("UI_Button_Page_Arrow");
                if (prevBtnImg.sprite != pageArrowSprite) { prevBtnImg.sprite = pageArrowSprite; isSceneDirty = true; }
                ConfigureSimpleImage(prevBtnImg, true, ref isSceneDirty);
                ConfigureFixedVisualRect(prevBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(48f, 48f), Vector2.zero, ref isSceneDirty);
                var label = prevBtn.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null && label.gameObject.activeSelf) { label.gameObject.SetActive(false); isSceneDirty = true; }
            }

            Button nextBtn = SetupButton(stickerBookPanel, "Next_Button", "Page >", ref isSceneDirty);
            Image nextBtnImg = nextBtn.GetComponent<Image>();
            if (nextBtnImg != null)
            {
                var pageArrowSprite = LoadSprite("UI_Button_Page_Arrow");
                if (nextBtnImg.sprite != pageArrowSprite) { nextBtnImg.sprite = pageArrowSprite; isSceneDirty = true; }
                ConfigureSimpleImage(nextBtnImg, true, ref isSceneDirty);
                ConfigureFixedVisualRect(nextBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(48f, 48f), Vector2.zero, ref isSceneDirty);
                var label = nextBtn.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null && label.gameObject.activeSelf) { label.gameObject.SetActive(false); isSceneDirty = true; }
            }

            Button styleBtn = SetupButton(stickerBookPanel, "Style_Button", "Style", ref isSceneDirty);
            Button addNoteBtn = SetupButton(stickerBookPanel, "Add_Note_Button", "Note", ref isSceneDirty);
            SafeSetSizeDelta(styleBtn.GetComponent<RectTransform>(), new Vector2(90f, 35f), ref isSceneDirty);
            SafeSetSizeDelta(addNoteBtn.GetComponent<RectTransform>(), new Vector2(90f, 35f), ref isSceneDirty);

            // Create Trash_Can child under StickerBook_Panel
            RectTransform trashCanRect = SetupPanel(stickerBookPanel, "Trash_Can", ref isSceneDirty);
            Image trashCanImg = trashCanRect.gameObject.GetComponent<Image>();
            if (trashCanImg == null)
            {
                trashCanImg = trashCanRect.gameObject.AddComponent<Image>();
                isSceneDirty = true;
            }
            var trashCanSprite = LoadSprite("UI_Icon_Trash_Can");
            if (trashCanImg.sprite != trashCanSprite)
            {
                trashCanImg.sprite = trashCanSprite;
                isSceneDirty = true;
            }
            ConfigureSimpleImage(trashCanImg, true, ref isSceneDirty);
            ConfigureFixedVisualRect(trashCanRect, new Vector2(0.5f, 0f), new Vector2(50f, 50f), new Vector2(0f, 15f), ref isSceneDirty);

            LayoutElement trashCanLayout = trashCanRect.gameObject.GetComponent<LayoutElement>();
            if (trashCanLayout == null)
            {
                trashCanLayout = trashCanRect.gameObject.AddComponent<LayoutElement>();
                isSceneDirty = true;
            }
            if (!trashCanLayout.ignoreLayout)
            {
                trashCanLayout.ignoreLayout = true;
                isSceneDirty = true;
            }

            RectTransform flipIndicator = SetupPanel(stickerBookPanel, "Flip_Page_Indicator", ref isSceneDirty);
            if (flipIndicator.gameObject.activeSelf)
            {
                flipIndicator.gameObject.SetActive(false);
                isSceneDirty = true;
            }

            RectTransform page0 = SetupPanel(stickerBookPanel, "Page_0", ref isSceneDirty);
            bookPage0 = page0.gameObject.GetComponent<StickerBookPage>();
            if (bookPage0 == null)
            {
                bookPage0 = page0.gameObject.AddComponent<StickerBookPage>();
                isSceneDirty = true;
            }
            SerializedObject soPage0 = new SerializedObject(bookPage0);
            bool page0Dirty = false;
            SafeSetInt(soPage0.FindProperty("_pageIndex"), 0, ref page0Dirty);
            SafeSetObjectReference(soPage0.FindProperty("_backgroundImage"), page0.GetComponent<Image>(), ref page0Dirty);
            SafeSetInt(soPage0.FindProperty("_fallbackStyleCount"), 3, ref page0Dirty);

            // Wire background styles on page 0
            SerializedProperty stylesProp0 = soPage0.FindProperty("_backgroundStyles");
            if (stylesProp0 != null)
            {
                Sprite gridPaper = LoadSprite("Scrapbook_Bg_GridPaper");
                Sprite oldPaper = LoadSprite("Scrapbook_Bg_OldPaper");
                Sprite pastelPink = LoadSprite("Scrapbook_Bg_PastelPink");
                bool matches = (stylesProp0.arraySize == 3) &&
                               (stylesProp0.GetArrayElementAtIndex(0).objectReferenceValue == gridPaper) &&
                               (stylesProp0.GetArrayElementAtIndex(1).objectReferenceValue == oldPaper) &&
                               (stylesProp0.GetArrayElementAtIndex(2).objectReferenceValue == pastelPink);
                if (!matches)
                {
                    stylesProp0.ClearArray();
                    stylesProp0.InsertArrayElementAtIndex(0);
                    stylesProp0.GetArrayElementAtIndex(0).objectReferenceValue = gridPaper;
                    stylesProp0.InsertArrayElementAtIndex(1);
                    stylesProp0.GetArrayElementAtIndex(1).objectReferenceValue = oldPaper;
                    stylesProp0.InsertArrayElementAtIndex(2);
                    stylesProp0.GetArrayElementAtIndex(2).objectReferenceValue = pastelPink;
                    page0Dirty = true;
                }
            }
            if (page0Dirty)
            {
                soPage0.ApplyModifiedProperties();
                isSceneDirty = true;
            }

            RectTransform page1 = SetupPanel(stickerBookPanel, "Page_1", ref isSceneDirty);
            bookPage1 = page1.gameObject.GetComponent<StickerBookPage>();
            if (bookPage1 == null)
            {
                bookPage1 = page1.gameObject.AddComponent<StickerBookPage>();
                isSceneDirty = true;
            }
            SerializedObject soPage1 = new SerializedObject(bookPage1);
            bool page1Dirty = false;
            SafeSetInt(soPage1.FindProperty("_pageIndex"), 1, ref page1Dirty);
            SafeSetObjectReference(soPage1.FindProperty("_backgroundImage"), page1.GetComponent<Image>(), ref page1Dirty);
            SafeSetInt(soPage1.FindProperty("_fallbackStyleCount"), 3, ref page1Dirty);

            // Wire background styles on page 1
            SerializedProperty stylesProp1 = soPage1.FindProperty("_backgroundStyles");
            if (stylesProp1 != null)
            {
                Sprite gridPaper = LoadSprite("Scrapbook_Bg_GridPaper");
                Sprite oldPaper = LoadSprite("Scrapbook_Bg_OldPaper");
                Sprite pastelPink = LoadSprite("Scrapbook_Bg_PastelPink");
                bool matches = (stylesProp1.arraySize == 3) &&
                               (stylesProp1.GetArrayElementAtIndex(0).objectReferenceValue == gridPaper) &&
                               (stylesProp1.GetArrayElementAtIndex(1).objectReferenceValue == oldPaper) &&
                               (stylesProp1.GetArrayElementAtIndex(2).objectReferenceValue == pastelPink);
                if (!matches)
                {
                    stylesProp1.ClearArray();
                    stylesProp1.InsertArrayElementAtIndex(0);
                    stylesProp1.GetArrayElementAtIndex(0).objectReferenceValue = gridPaper;
                    stylesProp1.InsertArrayElementAtIndex(1);
                    stylesProp1.GetArrayElementAtIndex(1).objectReferenceValue = oldPaper;
                    stylesProp1.InsertArrayElementAtIndex(2);
                    stylesProp1.GetArrayElementAtIndex(2).objectReferenceValue = pastelPink;
                    page1Dirty = true;
                }
            }
            if (page1Dirty)
            {
                soPage1.ApplyModifiedProperties();
                isSceneDirty = true;
            }

            // Setup Layout for pages
            VerticalLayoutGroup bookLayout = stickerBookPanel.gameObject.GetComponent<VerticalLayoutGroup>();
            if (bookLayout == null)
            {
                bookLayout = stickerBookPanel.gameObject.AddComponent<VerticalLayoutGroup>();
                isSceneDirty = true;
            }
            if (bookLayout.childAlignment != TextAnchor.MiddleCenter) { bookLayout.childAlignment = TextAnchor.MiddleCenter; isSceneDirty = true; }
            if (!Mathf.Approximately(bookLayout.spacing, 10f)) { bookLayout.spacing = 10f; isSceneDirty = true; }
            if (bookLayout.childControlHeight) { bookLayout.childControlHeight = false; isSceneDirty = true; }
            if (bookLayout.childControlWidth) { bookLayout.childControlWidth = false; isSceneDirty = true; }

            // Auto-wire StickerBook
            SerializedObject soBook = new SerializedObject(stickerBook);
            bool bookDirty = false;
            SafeSetObjectReference(soBook.FindProperty("_flipPageIndicator"), flipIndicator, ref bookDirty);
            SafeSetObjectReference(soBook.FindProperty("_nextButton"), nextBtn, ref bookDirty);
            SafeSetObjectReference(soBook.FindProperty("_prevButton"), prevBtn, ref bookDirty);
            SafeSetObjectReference(soBook.FindProperty("_changeStyleButton"), styleBtn, ref bookDirty);
            SafeSetObjectReference(soBook.FindProperty("_addDiaryNoteButton"), addNoteBtn, ref bookDirty);

            // Wire pages list
            SerializedProperty pagesListProp = soBook.FindProperty("_pages");
            if (pagesListProp != null)
            {
                bool matches = (pagesListProp.arraySize == 2) &&
                               (pagesListProp.GetArrayElementAtIndex(0).objectReferenceValue == bookPage0) &&
                               (pagesListProp.GetArrayElementAtIndex(1).objectReferenceValue == bookPage1);
                if (!matches)
                {
                    pagesListProp.ClearArray();
                    pagesListProp.InsertArrayElementAtIndex(0);
                    pagesListProp.GetArrayElementAtIndex(0).objectReferenceValue = bookPage0;
                    pagesListProp.InsertArrayElementAtIndex(1);
                    pagesListProp.GetArrayElementAtIndex(1).objectReferenceValue = bookPage1;
                    bookDirty = true;
                }
            }
            if (bookDirty)
            {
                soBook.ApplyModifiedProperties();
                isSceneDirty = true;
            }
        }

        private void ConfigureInventoryTray(
            RectTransform uiRoot,
            RectTransform prefabsHolder,
            Sprite defaultSprite,
            StickerBookPage bookPage0,
            StickerBookPage bookPage1,
            StickerBook stickerBook,
            CozyDiaryInputPopup diaryInputPopup,
            ref bool isSceneDirty)
        {
            // 9. Setup Inventory Tray
            RectTransform inventoryTray = SetupPanel(uiRoot, "Inventory_Tray", ref isSceneDirty);
            SafeSetAnchor(inventoryTray, new Vector2(0f, 0f), new Vector2(1f, 0.25f), ref isSceneDirty);
            SafeSetSizeDelta(inventoryTray, Vector2.zero, ref isSceneDirty);
            SafeSetAnchoredPosition(inventoryTray, Vector2.zero, ref isSceneDirty);

            GridLayoutGroup trayGrid = inventoryTray.gameObject.GetComponent<GridLayoutGroup>();
            if (trayGrid == null)
            {
                trayGrid = inventoryTray.gameObject.AddComponent<GridLayoutGroup>();
                isSceneDirty = true;
            }
            if (trayGrid.cellSize != new Vector2(120f, 120f)) { trayGrid.cellSize = new Vector2(120f, 120f); isSceneDirty = true; }
            if (trayGrid.spacing != new Vector2(20f, 20f)) { trayGrid.spacing = new Vector2(20f, 20f); isSceneDirty = true; }
            if (trayGrid.childAlignment != TextAnchor.MiddleCenter) { trayGrid.childAlignment = TextAnchor.MiddleCenter; isSceneDirty = true; }

            // Explicitly destroy obsolete Sticker_0 and Sticker_1 children from Inventory_Tray
            for (int i = 0; i < 2; i++)
            {
                Transform obsoleteChild = inventoryTray.Find($"Sticker_{i}");
                if (obsoleteChild != null)
                {
                    DestroyImmediate(obsoleteChild.gameObject);
                    isSceneDirty = true;
                }
            }

            // Create a single generic Sticker_Template clone source under Prefabs_Holder
            RectTransform stickerTemplate = SetupPanel(prefabsHolder, "Sticker_Template", ref isSceneDirty);
            SafeSetSizeDelta(stickerTemplate, new Vector2(100f, 100f), ref isSceneDirty);
            if (stickerTemplate.gameObject.activeSelf)
            {
                stickerTemplate.gameObject.SetActive(false);
                isSceneDirty = true;
            }

            CozySticker genericSticker = stickerTemplate.gameObject.GetComponent<CozySticker>();
            if (genericSticker == null)
            {
                genericSticker = stickerTemplate.gameObject.AddComponent<CozySticker>();
                isSceneDirty = true;
            }

            RectTransform shadowOffset = SetupPanel(stickerTemplate, "Shadow_Offset", ref isSceneDirty);
            Image shadowImg = shadowOffset.gameObject.GetComponent<Image>();
            if (shadowImg == null)
            {
                shadowImg = shadowOffset.gameObject.AddComponent<Image>();
                isSceneDirty = true;
            }
            Color shadowColor = new Color(0f, 0f, 0f, 0.3f);
            if (shadowImg.color != shadowColor)
            {
                shadowImg.color = shadowColor;
                isSceneDirty = true;
            }
            if (defaultSprite != null && shadowImg.sprite != defaultSprite)
            {
                shadowImg.sprite = defaultSprite;
                isSceneDirty = true;
            }
            ConfigureSimpleImage(shadowImg, true, ref isSceneDirty, forceColorWhite: false);
            ConfigureFixedVisualRect(shadowOffset, new Vector2(0.5f, 0.5f), new Vector2(92f, 92f), Vector2.zero, ref isSceneDirty);

            Image visualImg = SetupImage(stickerTemplate, "Visual_Image", ref isSceneDirty);
            if (defaultSprite != null && visualImg.sprite != defaultSprite)
            {
                visualImg.sprite = defaultSprite;
                isSceneDirty = true;
            }
            ConfigureSimpleImage(visualImg, true, ref isSceneDirty);
            ConfigureFixedVisualRect(visualImg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(92f, 92f), Vector2.zero, ref isSceneDirty);

            CanvasGroup group = stickerTemplate.gameObject.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = stickerTemplate.gameObject.AddComponent<CanvasGroup>();
                isSceneDirty = true;
            }

            // Wire generic CozySticker fields
            SerializedObject soSticker = new SerializedObject(genericSticker);
            bool stickerPropDirty = false;
            SafeSetObjectReference(soSticker.FindProperty("_shadowOffset"), shadowOffset, ref stickerPropDirty);
            SafeSetObjectReference(soSticker.FindProperty("_canvasGroup"), group, ref stickerPropDirty);
            SafeSetObjectReference(soSticker.FindProperty("_visualImage"), visualImg, ref stickerPropDirty);

            TextMeshProUGUI countTextText = SetupText(stickerTemplate, "Count_Text", "x1", "", ref isSceneDirty);
            if (!Mathf.Approximately(countTextText.fontSize, 14f)) { countTextText.fontSize = 14f; isSceneDirty = true; }
            if (countTextText.alignment != TextAlignmentOptions.BottomRight) { countTextText.alignment = TextAlignmentOptions.BottomRight; isSceneDirty = true; }
            RectTransform countTextRect = countTextText.GetComponent<RectTransform>();
            SafeSetAnchor(countTextRect, new Vector2(1f, 0f), new Vector2(1f, 0f), ref isSceneDirty);
            SafeSetPivot(countTextRect, new Vector2(1f, 0f), ref isSceneDirty);
            SafeSetAnchoredPosition(countTextRect, new Vector2(-5f, 5f), ref isSceneDirty);
            SafeSetSizeDelta(countTextRect, new Vector2(40f, 20f), ref isSceneDirty);

            SafeSetObjectReference(soSticker.FindProperty("_countText"), countTextText, ref stickerPropDirty);
            if (stickerPropDirty)
            {
                soSticker.ApplyModifiedProperties();
                isSceneDirty = true;
            }

            // Create a diary note template under Prefabs_Holder
            RectTransform diaryNoteTemplate = SetupPanel(prefabsHolder, "Diary_Note_Template", ref isSceneDirty);
            SafeSetSizeDelta(diaryNoteTemplate, new Vector2(150f, 120f), ref isSceneDirty);
            if (diaryNoteTemplate.gameObject.activeSelf)
            {
                diaryNoteTemplate.gameObject.SetActive(false);
                isSceneDirty = true;
            }

            Image diaryNoteImg = diaryNoteTemplate.gameObject.GetComponent<Image>();
            if (diaryNoteImg == null)
            {
                diaryNoteImg = diaryNoteTemplate.gameObject.AddComponent<Image>();
                isSceneDirty = true;
            }
            var stickyNoteSprite = LoadSprite("Scrapbook_StickyNote_Yellow");
            if (diaryNoteImg.sprite != stickyNoteSprite)
            {
                diaryNoteImg.sprite = stickyNoteSprite;
                isSceneDirty = true;
            }
            ConfigureSlicedImage(diaryNoteImg, ref isSceneDirty);

            CanvasGroup diaryNoteGroup = diaryNoteTemplate.gameObject.GetComponent<CanvasGroup>();
            if (diaryNoteGroup == null)
            {
                diaryNoteGroup = diaryNoteTemplate.gameObject.AddComponent<CanvasGroup>();
                isSceneDirty = true;
            }

            CozyDiaryNote diaryNoteComponent = diaryNoteTemplate.gameObject.GetComponent<CozyDiaryNote>();
            if (diaryNoteComponent == null)
            {
                diaryNoteComponent = diaryNoteTemplate.gameObject.AddComponent<CozyDiaryNote>();
                isSceneDirty = true;
            }

            TextMeshProUGUI diaryNoteText = SetupText(diaryNoteTemplate, "Note_Text", "New memory", "", ref isSceneDirty);
            RectTransform diaryNoteTextRect = diaryNoteText.GetComponent<RectTransform>();
            SafeSetAnchor(diaryNoteTextRect, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f), ref isSceneDirty);
            SafeSetSizeDelta(diaryNoteTextRect, Vector2.zero, ref isSceneDirty);
            SafeSetAnchoredPosition(diaryNoteTextRect, Vector2.zero, ref isSceneDirty);
            if (diaryNoteText.alignment != TextAlignmentOptions.TopLeft) { diaryNoteText.alignment = TextAlignmentOptions.TopLeft; isSceneDirty = true; }
            if (!Mathf.Approximately(diaryNoteText.fontSize, 16f)) { diaryNoteText.fontSize = 16f; isSceneDirty = true; }
            if (diaryNoteText.color != Color.black) { diaryNoteText.color = Color.black; isSceneDirty = true; }

            SerializedObject soDiaryNote = new SerializedObject(diaryNoteComponent);
            bool diaryNoteDirty = false;
            SafeSetObjectReference(soDiaryNote.FindProperty("_text"), diaryNoteText, ref diaryNoteDirty);
            SafeSetObjectReference(soDiaryNote.FindProperty("_canvasGroup"), diaryNoteGroup, ref diaryNoteDirty);
            if (diaryNoteDirty)
            {
                soDiaryNote.ApplyModifiedProperties();
                isSceneDirty = true;
            }

            SerializedObject soPage0Diary = new SerializedObject(bookPage0);
            bool page0DiaryDirty = false;
            SafeSetObjectReference(soPage0Diary.FindProperty("_diaryNotePrefabTemplate"), diaryNoteComponent, ref page0DiaryDirty);
            if (page0DiaryDirty)
            {
                soPage0Diary.ApplyModifiedProperties();
                isSceneDirty = true;
            }

            SerializedObject soPage1Diary = new SerializedObject(bookPage1);
            bool page1DiaryDirty = false;
            SafeSetObjectReference(soPage1Diary.FindProperty("_diaryNotePrefabTemplate"), diaryNoteComponent, ref page1DiaryDirty);
            if (page1DiaryDirty)
            {
                soPage1Diary.ApplyModifiedProperties();
                isSceneDirty = true;
            }

            // Wire templates/tray to StickerBook
            SerializedObject soBookUpdate = new SerializedObject(stickerBook);
            bool bookUpDirty = false;
            SerializedProperty trayRootProp = soBookUpdate.FindProperty("_inventoryTrayRoot");
            if (trayRootProp != null)
            {
                SafeSetObjectReference(trayRootProp, inventoryTray, ref bookUpDirty);
            }
            SerializedProperty prefabProp = soBookUpdate.FindProperty("_stickerPrefabTemplate");
            if (prefabProp != null)
            {
                SafeSetObjectReference(prefabProp, genericSticker, ref bookUpDirty);
            }
            SerializedProperty diaryPrefabProp = soBookUpdate.FindProperty("_diaryNotePrefabTemplate");
            if (diaryPrefabProp != null)
            {
                SafeSetObjectReference(diaryPrefabProp, diaryNoteComponent, ref bookUpDirty);
            }
            SerializedProperty diaryPopupProp = soBookUpdate.FindProperty("_diaryInputPopup");
            if (diaryPopupProp != null)
            {
                SafeSetObjectReference(diaryPopupProp, diaryInputPopup, ref bookUpDirty);
            }
            if (bookUpDirty)
            {
                soBookUpdate.ApplyModifiedProperties();
                isSceneDirty = true;
            }
        }
    }
}
