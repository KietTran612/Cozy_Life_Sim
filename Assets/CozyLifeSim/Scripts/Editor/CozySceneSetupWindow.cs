using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using CozyLifeSim.UI;
using CozyLifeSim.UI.Style;

namespace CozyLifeSim.Editor
{
    public class CozySceneSetupWindow : EditorWindow
    {
        private UIStyleConfig _styleConfig;

        [MenuItem("Tools/CozySim/Setup Test Scene")]
        public static void ShowWindow()
        {
            GetWindow<CozySceneSetupWindow>("Cozy Scene Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Cozy Life Sim - Scene Scaffolding & Wiring Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _styleConfig = (UIStyleConfig)EditorGUILayout.ObjectField("Default Style Config", _styleConfig, typeof(UIStyleConfig), false);

            if (_styleConfig == null)
            {
                // Auto search asset database
                string[] guids = AssetDatabase.FindAssets("t:UIStyleConfig");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _styleConfig = AssetDatabase.LoadAssetAtPath<UIStyleConfig>(path);
                }
                else
                {
                    // Auto-create a default UIStyleConfig asset if it doesn't exist
                    string dir = "Assets/CozyLifeSim/Settings";
                    if (!System.IO.Directory.Exists(dir))
                    {
                        System.IO.Directory.CreateDirectory(dir);
                        AssetDatabase.Refresh();
                    }
                    string assetPath = dir + "/CozyUIStyleConfig.asset";
                    _styleConfig = ScriptableObject.CreateInstance<UIStyleConfig>();
                    AssetDatabase.CreateAsset(_styleConfig, assetPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    Debug.Log($"<color=green>[CozySim]</color> Created new default UIStyleConfig at {assetPath}");
                }
            }

            if (GUILayout.Button("Generate Test Scene Hierarchy & Wiring", GUILayout.Height(40)))
            {
                GenerateScene();
            }
        }

        private void GenerateScene()
        {
            // 0. Setup Main Camera
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject camGo = GameObject.Find("Main Camera");
                if (camGo == null)
                {
                    camGo = new GameObject("Main Camera");
                    camera = camGo.AddComponent<Camera>();
                    camGo.tag = "MainCamera";
                }
                else
                {
                    camera = camGo.GetComponent<Camera>();
                    if (camera == null)
                    {
                        camera = camGo.AddComponent<Camera>();
                    }
                }
            }

            if (camera != null)
            {
                camera.transform.position = new Vector3(0f, 0f, -10f);
                camera.transform.rotation = Quaternion.identity;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.18f, 0.18f, 0.22f); // Dark cozy background
            }

            // 1. Setup GameLifetimeScope
            GameObject lifetimeScopeGo = GameObject.Find("GameLifetimeScope");
            if (lifetimeScopeGo == null)
            {
                lifetimeScopeGo = new GameObject("GameLifetimeScope");
            }

            GameLifetimeScope lifetimeScope = lifetimeScopeGo.GetComponent<GameLifetimeScope>();
            if (lifetimeScope == null)
            {
                lifetimeScope = lifetimeScopeGo.AddComponent<GameLifetimeScope>();
            }

            // Find and wire up QuestDatabase, CropDatabase, and UIStyleConfig to GameLifetimeScope
            CozyLifeSim.UI.Settings.QuestDatabase questDb = null;
            string[] dbGuids = AssetDatabase.FindAssets("t:QuestDatabase");
            if (dbGuids == null || dbGuids.Length == 0)
            {
                dbGuids = AssetDatabase.FindAssets("QuestDatabase");
            }
            if (dbGuids != null && dbGuids.Length > 0)
            {
                string dbPath = AssetDatabase.GUIDToAssetPath(dbGuids[0]);
                questDb = AssetDatabase.LoadAssetAtPath<CozyLifeSim.UI.Settings.QuestDatabase>(dbPath);
            }

            CozyLifeSim.UI.Settings.CropDatabase cropDb = CropDatabaseUtility.LoadOrCreateDatabase();

            SerializedObject soScope = new SerializedObject(lifetimeScope);
            if (_styleConfig != null)
            {
                SerializedProperty propStyle = soScope.FindProperty("_defaultStyleConfig");
                if (propStyle != null)
                {
                    propStyle.objectReferenceValue = _styleConfig;
                }
            }
            if (questDb != null)
            {
                SerializedProperty propQuestDb = soScope.FindProperty("_questDatabase");
                if (propQuestDb != null)
                {
                    propQuestDb.objectReferenceValue = questDb;
                }
            }
            if (cropDb != null)
            {
                SerializedProperty propCropDb = soScope.FindProperty("_cropDatabase");
                if (propCropDb != null)
                {
                    propCropDb.objectReferenceValue = cropDb;
                }
            }
            soScope.ApplyModifiedProperties();

            // 2. Setup Canvas
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGo = new GameObject("Canvas");
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            // Setup EventSystem
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // 3. Setup UI_Root
            RectTransform uiRoot = SetupPanel(canvas.transform, "UI_Root");
            StretchToFill(uiRoot);

            // 4. Setup Header Panel
            RectTransform headerPanel = SetupPanel(uiRoot, "Header_Panel");
            headerPanel.anchorMin = new Vector2(0f, 0.9f);
            headerPanel.anchorMax = new Vector2(1f, 1f);
            headerPanel.sizeDelta = Vector2.zero;
            headerPanel.anchoredPosition = Vector2.zero;

            TextMeshProUGUI coinsText = SetupText(headerPanel, "Coins_Text", "Coins: 100", "Header_Text");
            TextMeshProUGUI seedsText = SetupText(headerPanel, "Seeds_Text", "Seeds: 5", "Header_Text");
            TextMeshProUGUI cropsText = SetupText(headerPanel, "Crops_Text", "Crops: 0", "Header_Text");

            // Attach InventoryHudWidget to Header_Panel
            InventoryHudWidget inventoryHud = headerPanel.gameObject.GetComponent<InventoryHudWidget>();
            if (inventoryHud == null)
            {
                inventoryHud = headerPanel.gameObject.AddComponent<InventoryHudWidget>();
            }
            SerializedObject soHud = new SerializedObject(inventoryHud);
            soHud.FindProperty("_coinsText").objectReferenceValue = coinsText;
            soHud.FindProperty("_seedsText").objectReferenceValue = seedsText;
            soHud.FindProperty("_cropsText").objectReferenceValue = cropsText;
            soHud.ApplyModifiedProperties();

            // Horizontal layout for header panel
            HorizontalLayoutGroup headerLayout = headerPanel.gameObject.GetComponent<HorizontalLayoutGroup>();
            if (headerLayout == null)
            {
                headerLayout = headerPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
            }
            headerLayout.childAlignment = TextAnchor.MiddleCenter;
            headerLayout.spacing = 100f;
            headerLayout.childControlHeight = true;
            headerLayout.childControlWidth = true;

            // 5. Setup Gameplay Area
            RectTransform gameplayArea = SetupPanel(uiRoot, "Gameplay_Area");
            gameplayArea.anchorMin = new Vector2(0f, 0.25f);
            gameplayArea.anchorMax = new Vector2(1f, 0.9f);
            gameplayArea.sizeDelta = Vector2.zero;
            gameplayArea.anchoredPosition = Vector2.zero;

            // Horizontal layout for gameplay areas
            HorizontalLayoutGroup gameplayLayout = gameplayArea.gameObject.GetComponent<HorizontalLayoutGroup>();
            if (gameplayLayout == null)
            {
                gameplayLayout = gameplayArea.gameObject.AddComponent<HorizontalLayoutGroup>();
            }
            gameplayLayout.childAlignment = TextAnchor.MiddleCenter;
            gameplayLayout.spacing = 80f;
            gameplayLayout.childControlHeight = false;
            gameplayLayout.childControlWidth = false;

            // 6. Setup Farm Plot (Trồng trọt)
            RectTransform farmPlot = SetupPanel(gameplayArea, "Farm_Plot");
            farmPlot.sizeDelta = new Vector2(350f, 500f);
            CropWidget cropWidget = farmPlot.gameObject.GetComponent<CropWidget>();
            if (cropWidget == null)
            {
                cropWidget = farmPlot.gameObject.AddComponent<CropWidget>();
            }

            Image cropVisual = SetupImage(farmPlot, "Crop_Visual");
            TextMeshProUGUI timerText = SetupText(farmPlot, "Timer_Text", "EMPTY SOIL", "");
            Button plantBtn = SetupButton(farmPlot, "Plant_Button", "Plant Seed");
            Button waterBtn = SetupButton(farmPlot, "Water_Button", "Water");
            Button harvestBtn = SetupButton(farmPlot, "Harvest_Button", "Harvest");
            RectTransform wateringCan = SetupPanel(farmPlot, "Watering_Can");
            wateringCan.gameObject.SetActive(false);

            // Vertical layout for Farm components
            VerticalLayoutGroup farmLayout = farmPlot.gameObject.GetComponent<VerticalLayoutGroup>();
            if (farmLayout == null)
            {
                farmLayout = farmPlot.gameObject.AddComponent<VerticalLayoutGroup>();
            }
            farmLayout.childAlignment = TextAnchor.MiddleCenter;
            farmLayout.spacing = 15f;
            farmLayout.childControlHeight = false;
            farmLayout.childControlWidth = false;

            // Find default sprites
            Sprite defaultSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            if (defaultSprite == null)
            {
                string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite");
                if (spriteGuids.Length > 0)
                {
                    defaultSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(spriteGuids[0]));
                }
            }

            // Load cute premium assets from packages
            Sprite seedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Plants/Acorn-256.png");
            Sprite sproutSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Plants/Sapling-256.png");
            Sprite matureSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Flowers/Flower-Tulip-Red-256.png");
            Sprite chickenSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Animals/Chicken-White-256.png");
            Sprite heartSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Hearts/Heart-Red-256.png");
            Sprite wateringCanSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Farming/Watering-Can-Pink-256.png");

            // Setup watering can image visual
            Image wateringCanImg = wateringCan.gameObject.GetComponent<Image>();
            if (wateringCanImg == null)
            {
                wateringCanImg = wateringCan.gameObject.AddComponent<Image>();
            }
            if (wateringCanSprite != null) wateringCanImg.sprite = wateringCanSprite;
            else if (defaultSprite != null) wateringCanImg.sprite = defaultSprite;

            // Auto-wire CropWidget via SerializedObject
            SerializedObject soCrop = new SerializedObject(cropWidget);
            soCrop.FindProperty("_cropId").intValue = 1;
            soCrop.FindProperty("_stageDurationSeconds").floatValue = 5f;
            soCrop.FindProperty("_cropVisual").objectReferenceValue = cropVisual;
            soCrop.FindProperty("_timerText").objectReferenceValue = timerText;
            soCrop.FindProperty("_waterButton").objectReferenceValue = waterBtn;
            soCrop.FindProperty("_wateringCan").objectReferenceValue = wateringCan;
            soCrop.FindProperty("_plantButton").objectReferenceValue = plantBtn;
            soCrop.FindProperty("_harvestButton").objectReferenceValue = harvestBtn;
            
            soCrop.FindProperty("_seedSprite").objectReferenceValue = seedSprite != null ? seedSprite : defaultSprite;
            soCrop.FindProperty("_sproutSprite").objectReferenceValue = sproutSprite != null ? sproutSprite : defaultSprite;
            soCrop.FindProperty("_matureSprite").objectReferenceValue = matureSprite != null ? matureSprite : defaultSprite;
            soCrop.FindProperty("_harvestSprite").objectReferenceValue = matureSprite != null ? matureSprite : defaultSprite;
            
            soCrop.ApplyModifiedProperties();

            // 7. Setup Animal Pen (Nuôi gà)
            RectTransform animalPen = SetupPanel(gameplayArea, "Animal_Pen");
            animalPen.sizeDelta = new Vector2(350f, 500f);
            AnimalWidget animalWidget = animalPen.gameObject.GetComponent<AnimalWidget>();
            if (animalWidget == null)
            {
                animalWidget = animalPen.gameObject.AddComponent<AnimalWidget>();
            }

            Image chickenVisual = SetupImage(animalPen, "Chicken_Visual");
            if (chickenSprite != null) chickenVisual.sprite = chickenSprite;
            else if (defaultSprite != null) chickenVisual.sprite = defaultSprite;

            Button interactBtn = SetupButton(animalPen, "Interaction_Button", "Pet Chicken");
            RectTransform spawnRoot = SetupPanel(animalPen, "Spawn_Root");

            VerticalLayoutGroup animalLayout = animalPen.gameObject.GetComponent<VerticalLayoutGroup>();
            if (animalLayout == null)
            {
                animalLayout = animalPen.gameObject.AddComponent<VerticalLayoutGroup>();
            }
            animalLayout.childAlignment = TextAnchor.MiddleCenter;
            animalLayout.spacing = 15f;
            animalLayout.childControlHeight = false;
            animalLayout.childControlWidth = false;

            // Setup Prefabs Holder under Canvas
            RectTransform prefabsHolder = SetupPanel(canvas.transform, "Prefabs_Holder");
            prefabsHolder.gameObject.SetActive(false); // keep it hidden/inactive

            // Create Heart_Feedback_Template
            RectTransform heartTemplate = SetupPanel(prefabsHolder, "Heart_Feedback_Template");
            Image heartImg = heartTemplate.gameObject.GetComponent<Image>();
            if (heartImg == null)
            {
                heartImg = heartTemplate.gameObject.AddComponent<Image>();
            }
            if (heartSprite != null)
            {
                heartImg.sprite = heartSprite;
                heartImg.color = Color.white; // Keep white color to display original red sprite perfectly
            }
            else
            {
                heartImg.color = Color.red; // Solid red fallback
                if (defaultSprite != null) heartImg.sprite = defaultSprite;
            }

            CanvasGroup heartGroup = heartTemplate.gameObject.GetComponent<CanvasGroup>();
            if (heartGroup == null)
            {
                heartGroup = heartTemplate.gameObject.AddComponent<CanvasGroup>();
            }

            // Auto-wire AnimalWidget
            SerializedObject soAnimal = new SerializedObject(animalWidget);
            soAnimal.FindProperty("_interactionButton").objectReferenceValue = interactBtn;
            soAnimal.FindProperty("_spawnRoot").objectReferenceValue = spawnRoot;
            soAnimal.FindProperty("_heartPrefab").objectReferenceValue = heartTemplate;
            soAnimal.ApplyModifiedProperties();

            // 8. Setup Sticker Book
            RectTransform stickerBookPanel = SetupPanel(gameplayArea, "StickerBook_Panel");
            stickerBookPanel.sizeDelta = new Vector2(500f, 500f);
            StickerBook stickerBook = stickerBookPanel.gameObject.GetComponent<StickerBook>();
            if (stickerBook == null)
            {
                stickerBook = stickerBookPanel.gameObject.AddComponent<StickerBook>();
            }

            Button prevBtn = SetupButton(stickerBookPanel, "Prev_Button", "< Page");
            Button nextBtn = SetupButton(stickerBookPanel, "Next_Button", "Page >");
            RectTransform flipIndicator = SetupPanel(stickerBookPanel, "Flip_Page_Indicator");
            flipIndicator.gameObject.SetActive(false);

            RectTransform page0 = SetupPanel(stickerBookPanel, "Page_0");
            StickerBookPage bookPage0 = page0.gameObject.GetComponent<StickerBookPage>();
            if (bookPage0 == null)
            {
                bookPage0 = page0.gameObject.AddComponent<StickerBookPage>();
            }
            SerializedObject soPage0 = new SerializedObject(bookPage0);
            soPage0.FindProperty("_pageIndex").intValue = 0;
            soPage0.ApplyModifiedProperties();

            RectTransform page1 = SetupPanel(stickerBookPanel, "Page_1");
            StickerBookPage bookPage1 = page1.gameObject.GetComponent<StickerBookPage>();
            if (bookPage1 == null)
            {
                bookPage1 = page1.gameObject.AddComponent<StickerBookPage>();
            }
            SerializedObject soPage1 = new SerializedObject(bookPage1);
            soPage1.FindProperty("_pageIndex").intValue = 1;
            soPage1.ApplyModifiedProperties();

            // Setup Layout for pages
            VerticalLayoutGroup bookLayout = stickerBookPanel.gameObject.GetComponent<VerticalLayoutGroup>();
            if (bookLayout == null)
            {
                bookLayout = stickerBookPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            }
            bookLayout.childAlignment = TextAnchor.MiddleCenter;
            bookLayout.spacing = 10f;
            bookLayout.childControlHeight = false;
            bookLayout.childControlWidth = false;

            // Auto-wire StickerBook
            SerializedObject soBook = new SerializedObject(stickerBook);
            soBook.FindProperty("_flipPageIndicator").objectReferenceValue = flipIndicator;
            soBook.FindProperty("_nextButton").objectReferenceValue = nextBtn;
            soBook.FindProperty("_prevButton").objectReferenceValue = prevBtn;
            
            // Wire pages list
            SerializedProperty pagesListProp = soBook.FindProperty("_pages");
            if (pagesListProp != null)
            {
                pagesListProp.ClearArray();
                pagesListProp.InsertArrayElementAtIndex(0);
                pagesListProp.GetArrayElementAtIndex(0).objectReferenceValue = bookPage0;
                pagesListProp.InsertArrayElementAtIndex(1);
                pagesListProp.GetArrayElementAtIndex(1).objectReferenceValue = bookPage1;
            }
            soBook.ApplyModifiedProperties();

            soBook.ApplyModifiedProperties();

            // 8.5. Setup Sidebar Panel (Tabbed Sidebar Architecture)
            RectTransform sidebarPanel = SetupPanel(uiRoot, "Sidebar_Panel");
            sidebarPanel.anchorMin = new Vector2(0.75f, 0.45f);
            sidebarPanel.anchorMax = new Vector2(0.97f, 0.90f);
            sidebarPanel.sizeDelta = Vector2.zero;
            sidebarPanel.anchoredPosition = Vector2.zero;

            // Set up transparent background
            Image sidebarImg = sidebarPanel.gameObject.GetComponent<Image>();
            if (sidebarImg == null)
            {
                sidebarImg = sidebarPanel.gameObject.AddComponent<Image>();
            }
            sidebarImg.color = new Color(0f, 0f, 0f, 0.25f); // Transparent dark backing
            sidebarImg.raycastTarget = false; // Allow clicks to pass through empty panel areas

            // Explicitly destroy any existing HorizontalLayoutGroup on sidebarPanel from previous setup versions to allow manual anchoring
            HorizontalLayoutGroup oldLayout = sidebarPanel.gameObject.GetComponent<HorizontalLayoutGroup>();
            if (oldLayout != null)
            {
                DestroyImmediate(oldLayout);
            }

            // A. Create Tabs Container (Vertical Stack on the left side)
            RectTransform tabsContainer = SetupPanel(sidebarPanel, "Tabs_Container");
            tabsContainer.anchorMin = new Vector2(0f, 0f);
            tabsContainer.anchorMax = new Vector2(0f, 1f);
            tabsContainer.pivot = new Vector2(0f, 1f);
            tabsContainer.sizeDelta = new Vector2(65f, 0f);
            tabsContainer.anchoredPosition = Vector2.zero;

            // Remove background image from tabs container to make it clean
            Image tabsContImg = tabsContainer.gameObject.GetComponent<Image>();
            if (tabsContImg != null)
            {
                DestroyImmediate(tabsContImg);
            }

            VerticalLayoutGroup tabsLayout = tabsContainer.gameObject.GetComponent<VerticalLayoutGroup>();
            if (tabsLayout == null)
            {
                tabsLayout = tabsContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            }
            tabsLayout.childAlignment = TextAnchor.UpperCenter;
            tabsLayout.spacing = 15f;
            tabsLayout.padding = new RectOffset(5, 5, 10, 10);
            tabsLayout.childForceExpandHeight = false;
            tabsLayout.childControlHeight = false;
            tabsLayout.childControlWidth = true;

            // Create buttons inside Tabs Container
            Button toggleBtn = SetupButton(tabsContainer, "Toggle_Button", ">");
            RectTransform toggleBtnRect = toggleBtn.GetComponent<RectTransform>();
            toggleBtnRect.sizeDelta = new Vector2(55f, 55f);
            TextMeshProUGUI toggleBtnText = toggleBtn.GetComponentInChildren<TextMeshProUGUI>();

            Button tabBtn0 = SetupButton(tabsContainer, "Tab_Btn_0", "Q");
            RectTransform tabBtn0Rect = tabBtn0.GetComponent<RectTransform>();
            tabBtn0Rect.sizeDelta = new Vector2(55f, 55f);

            Button tabBtn1 = SetupButton(tabsContainer, "Tab_Btn_1", "S");
            RectTransform tabBtn1Rect = tabBtn1.GetComponent<RectTransform>();
            tabBtn1Rect.sizeDelta = new Vector2(55f, 55f);

            // B. Create Content Container (Stretches to occupy the remaining space on the right, with a 65px left offset)
            RectTransform contentContainer = SetupPanel(sidebarPanel, "Content_Container");
            contentContainer.anchorMin = new Vector2(0f, 0f);
            contentContainer.anchorMax = new Vector2(1f, 1f);
            contentContainer.offsetMin = new Vector2(65f, 0f); // Left offset is 65px (width of Tabs_Container)
            contentContainer.offsetMax = new Vector2(0f, 0f);  // Top, Right, Bottom offsets are 0

            // Remove Image to allow clicks to pass through empty spots
            Image contentContImg = contentContainer.gameObject.GetComponent<Image>();
            if (contentContImg != null)
            {
                DestroyImmediate(contentContImg);
            }

            // C. Setup Quest Content under Content Container
            RectTransform questContent = SetupPanel(contentContainer, "Quest_Content");
            StretchToFill(questContent);

            Image questContentImg = questContent.gameObject.GetComponent<Image>();
            if (questContentImg != null)
            {
                DestroyImmediate(questContentImg);
            }

            VerticalLayoutGroup questLayout = questContent.gameObject.GetComponent<VerticalLayoutGroup>();
            if (questLayout == null)
            {
                questLayout = questContent.gameObject.AddComponent<VerticalLayoutGroup>();
            }
            questLayout.childAlignment = TextAnchor.UpperLeft;
            questLayout.spacing = 15f;
            questLayout.padding = new RectOffset(15, 15, 15, 15);
            questLayout.childControlHeight = true;
            questLayout.childControlWidth = true;

            // Title inside Quest Content
            TextMeshProUGUI questTitleText = SetupText(questContent, "Quest_Title", "ACTIVE QUESTS", "Header_Text");
            questTitleText.fontSize = 24f;
            questTitleText.fontStyle = FontStyles.Bold;
            questTitleText.raycastTarget = false;

            // Explicitly destroy obsolete static Quest_Item_0, Quest_Item_1, Quest_Item_2 to avoid cluttered hierarchy
            for (int i = 0; i < 3; i++)
            {
                Transform obsoleteChild = questContent.Find($"Quest_Item_{i}");
                if (obsoleteChild != null)
                {
                    DestroyImmediate(obsoleteChild.gameObject);
                }
            }

            // Create a single clean dynamic Quest_Item_Template
            TextMeshProUGUI questTemplateText = SetupText(questContent, "Quest_Item_Template", "• Loading Quest...", "");
            questTemplateText.fontSize = 20f;
            questTemplateText.alignment = TextAlignmentOptions.Left;
            questTemplateText.raycastTarget = false;

            // Attach QuestHudWidget and wire it up
            QuestHudWidget questHud = questContent.gameObject.GetComponent<QuestHudWidget>();
            if (questHud == null)
            {
                questHud = questContent.gameObject.AddComponent<QuestHudWidget>();
            }

            SerializedObject soQuestHud = new SerializedObject(questHud);
            soQuestHud.FindProperty("_titleText").objectReferenceValue = questTitleText;
            
            SerializedProperty templateProp = soQuestHud.FindProperty("_questItemTemplate");
            if (templateProp != null)
            {
                templateProp.objectReferenceValue = questTemplateText;
            }
            soQuestHud.ApplyModifiedProperties();

            // D. Setup Stats Content (Placeholder Demo Tab) under Content Container
            RectTransform statsContent = SetupPanel(contentContainer, "Stats_Content");
            StretchToFill(statsContent);

            Image statsContentImg = statsContent.gameObject.GetComponent<Image>();
            if (statsContentImg != null)
            {
                DestroyImmediate(statsContentImg);
            }

            VerticalLayoutGroup statsLayout = statsContent.gameObject.GetComponent<VerticalLayoutGroup>();
            if (statsLayout == null)
            {
                statsLayout = statsContent.gameObject.AddComponent<VerticalLayoutGroup>();
            }
            statsLayout.childAlignment = TextAnchor.UpperLeft;
            statsLayout.spacing = 15f;
            statsLayout.padding = new RectOffset(15, 15, 15, 15);
            statsLayout.childControlHeight = true;
            statsLayout.childControlWidth = true;

            // Title inside Stats Content
            TextMeshProUGUI statsTitleText = SetupText(statsContent, "Stats_Title", "GAME STATS", "Header_Text");
            statsTitleText.fontSize = 24f;
            statsTitleText.fontStyle = FontStyles.Bold;
            statsTitleText.raycastTarget = false;

            TextMeshProUGUI statsItem0 = SetupText(statsContent, "Stats_Item_0", "• Time Played: 00:05:12", "");
            statsItem0.fontSize = 20f;
            statsItem0.alignment = TextAlignmentOptions.Left;
            statsItem0.raycastTarget = false;

            TextMeshProUGUI statsItem1 = SetupText(statsContent, "Stats_Item_1", "• Animals Pet: 12", "");
            statsItem1.fontSize = 20f;
            statsItem1.alignment = TextAlignmentOptions.Left;
            statsItem1.raycastTarget = false;

            // E. Attach CozySidebar component and wire it up
            CozySidebar sidebar = sidebarPanel.gameObject.GetComponent<CozySidebar>();
            if (sidebar == null)
            {
                sidebar = sidebarPanel.gameObject.AddComponent<CozySidebar>();
            }

            SerializedObject soSidebar = new SerializedObject(sidebar);
            soSidebar.FindProperty("_slidingPanel").objectReferenceValue = sidebarPanel;
            soSidebar.FindProperty("_toggleButton").objectReferenceValue = toggleBtn;
            soSidebar.FindProperty("_toggleButtonText").objectReferenceValue = toggleBtnText;

            // Wire Tab Buttons
            SerializedProperty tabButtonsProp = soSidebar.FindProperty("_tabButtons");
            if (tabButtonsProp != null)
            {
                tabButtonsProp.ClearArray();
                tabButtonsProp.InsertArrayElementAtIndex(0);
                tabButtonsProp.GetArrayElementAtIndex(0).objectReferenceValue = tabBtn0;
                tabButtonsProp.InsertArrayElementAtIndex(1);
                tabButtonsProp.GetArrayElementAtIndex(1).objectReferenceValue = tabBtn1;
            }

            // Wire Tab Contents
            SerializedProperty tabContentsProp = soSidebar.FindProperty("_tabContents");
            if (tabContentsProp != null)
            {
                tabContentsProp.ClearArray();
                tabContentsProp.InsertArrayElementAtIndex(0);
                tabContentsProp.GetArrayElementAtIndex(0).objectReferenceValue = questContent;
                tabContentsProp.InsertArrayElementAtIndex(1);
                tabContentsProp.GetArrayElementAtIndex(1).objectReferenceValue = statsContent;
            }
            soSidebar.ApplyModifiedProperties();

            // 9. Setup Inventory Tray
            RectTransform inventoryTray = SetupPanel(uiRoot, "Inventory_Tray");
            inventoryTray.anchorMin = new Vector2(0f, 0f);
            inventoryTray.anchorMax = new Vector2(1f, 0.25f);
            inventoryTray.sizeDelta = Vector2.zero;
            inventoryTray.anchoredPosition = Vector2.zero;

            GridLayoutGroup trayGrid = inventoryTray.gameObject.GetComponent<GridLayoutGroup>();
            if (trayGrid == null)
            {
                trayGrid = inventoryTray.gameObject.AddComponent<GridLayoutGroup>();
            }
            trayGrid.cellSize = new Vector2(120f, 120f);
            trayGrid.spacing = new Vector2(20f, 20f);
            trayGrid.childAlignment = TextAnchor.MiddleCenter;

            // Create Draggable Stickers inside Inventory Tray
            List<CozySticker> spawnedTemplates = new List<CozySticker>();
            string[] stickerAssetPaths = new string[]
            {
                "Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Animals/Bunny-Pink-256.png",
                "Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Animals/Bear-256.png"
            };

            for (int i = 0; i < 2; i++)
            {
                string stickerName = $"Sticker_{i}";
                RectTransform stickerItem = SetupPanel(inventoryTray, stickerName);
                stickerItem.sizeDelta = new Vector2(100f, 100f);

                CozySticker sticker = stickerItem.gameObject.GetComponent<CozySticker>();
                if (sticker == null)
                {
                    sticker = stickerItem.gameObject.AddComponent<CozySticker>();
                }

                RectTransform shadowOffset = SetupPanel(stickerItem, "Shadow_Offset");
                Image shadowImg = shadowOffset.gameObject.GetComponent<Image>();
                if (shadowImg == null)
                {
                    shadowImg = shadowOffset.gameObject.AddComponent<Image>();
                }
                shadowImg.color = new Color(0f, 0f, 0f, 0.3f); // semi-transparent black shadow

                Image visualImg = SetupImage(stickerItem, "Visual_Image");
                
                Sprite stickerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(stickerAssetPaths[i % stickerAssetPaths.Length]);
                if (stickerSprite != null)
                {
                    visualImg.sprite = stickerSprite;
                    shadowImg.sprite = stickerSprite;
                }
                else if (defaultSprite != null)
                {
                    visualImg.sprite = defaultSprite;
                    shadowImg.sprite = defaultSprite;
                }

                CanvasGroup group = stickerItem.gameObject.GetComponent<CanvasGroup>();
                if (group == null)
                {
                    group = stickerItem.gameObject.AddComponent<CanvasGroup>();
                }

                // Wire CozySticker fields
                SerializedObject soSticker = new SerializedObject(sticker);
                soSticker.FindProperty("_stickerId").intValue = i + 1;
                soSticker.FindProperty("_shadowOffset").objectReferenceValue = shadowOffset;
                soSticker.FindProperty("_canvasGroup").objectReferenceValue = group;
                soSticker.ApplyModifiedProperties();

                spawnedTemplates.Add(sticker);
            }

            // Wire templates list back to StickerBook
            SerializedObject soBookUpdate = new SerializedObject(stickerBook);
            SerializedProperty templatesProp = soBookUpdate.FindProperty("_stickerTemplates");
            if (templatesProp != null)
            {
                templatesProp.ClearArray();
                for (int i = 0; i < spawnedTemplates.Count; i++)
                {
                    templatesProp.InsertArrayElementAtIndex(i);
                    templatesProp.GetArrayElementAtIndex(i).objectReferenceValue = spawnedTemplates[i];
                }
            }
            soBookUpdate.ApplyModifiedProperties();

            // Log completion
            EditorUtility.SetDirty(canvas.gameObject);
            if (lifetimeScopeGo != null) EditorUtility.SetDirty(lifetimeScopeGo);
            if (!Application.isPlaying)
            {
                var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
                bool saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
                Debug.Log($"<color=green>[CozySim]</color> Active Scene saved to disk: {activeScene.path} (Success: {saved})");
            }
            
            Debug.Log("<color=green>[CozySim]</color> Test Scene Hierarchy and Wiring generated successfully!");
            ShowNotification(new GUIContent("Test Scene generated!"));
        }

        private RectTransform SetupPanel(Transform parent, string name)
        {
            GameObject go = null;
            if (parent != null)
            {
                Transform found = parent.Find(name);
                if (found != null)
                {
                    go = found.gameObject;
                }
            }
            else
            {
                go = GameObject.Find(name);
            }

            if (go == null)
            {
                go = new GameObject(name);
                if (parent != null)
                {
                    go.transform.SetParent(parent, false);
                }
            }
            else if (parent != null && go.transform.parent != parent)
            {
                go.transform.SetParent(parent, false);
            }

            RectTransform rect = go.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = go.AddComponent<RectTransform>();
            }

            // Ensure Image component is present for background panels to raycast/drag drop
            if (name.Contains("Plot") || name.Contains("Pen") || name.Contains("Panel") || name.Contains("Tray") || name.Contains("Page_"))
            {
                Image img = go.GetComponent<Image>();
                if (img == null)
                {
                    img = go.AddComponent<Image>();
                    img.color = new Color(0f, 0f, 0f, 0.15f); // Semi transparent backing
                }
            }

            return rect;
        }

        private TextMeshProUGUI SetupText(Transform parent, string name, string defaultText, string styleKey)
        {
            RectTransform panel = SetupPanel(parent, name);
            TextMeshProUGUI tmp = panel.gameObject.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
            {
                tmp = panel.gameObject.AddComponent<TextMeshProUGUI>();
            }

            // Fallback default font to prevent TMPro NullReferenceException during layout rendering
            if (tmp.font == null)
            {
                tmp.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
                if (tmp.font == null)
                {
                    string[] fontGuids = AssetDatabase.FindAssets("t:TMP_FontAsset");
                    if (fontGuids.Length > 0)
                    {
                        tmp.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(fontGuids[0]));
                    }
                }
            }

            tmp.text = defaultText;
            tmp.alignment = TextAlignmentOptions.Center;

            if (!string.IsNullOrEmpty(styleKey))
            {
                UIStyleElement element = panel.gameObject.GetComponent<UIStyleElement>();
                if (element == null)
                {
                    element = panel.gameObject.AddComponent<UIStyleElement>();
                }
                
                SerializedObject soElement = new SerializedObject(element);
                SerializedProperty propKey = soElement.FindProperty("_styleKey");
                if (propKey != null)
                {
                    propKey.stringValue = styleKey;
                    soElement.ApplyModifiedProperties();
                }
            }

            return tmp;
        }

        private Image SetupImage(Transform parent, string name)
        {
            RectTransform panel = SetupPanel(parent, name);
            Image img = panel.gameObject.GetComponent<Image>();
            if (img == null)
            {
                img = panel.gameObject.AddComponent<Image>();
            }
            img.color = Color.white;
            return img;
        }

        private Button SetupButton(Transform parent, string name, string labelText)
        {
            RectTransform panel = SetupPanel(parent, name);
            Button btn = panel.gameObject.GetComponent<Button>();
            if (btn == null)
            {
                btn = panel.gameObject.AddComponent<Button>();
            }

            // Ensure Image component exists on Button for transition target graphic and raycast target
            Image btnImg = panel.gameObject.GetComponent<Image>();
            if (btnImg == null)
            {
                btnImg = panel.gameObject.AddComponent<Image>();
                btnImg.color = new Color(1f, 1f, 1f, 0.8f); // Default light white background
            }

            // Wire target graphic for proper transitions and click bounds
            btn.targetGraphic = btnImg;

            // Add text label inside button
            TextMeshProUGUI label = panel.gameObject.GetComponentInChildren<TextMeshProUGUI>();
            if (label == null)
            {
                GameObject labelGo = new GameObject("Label");
                labelGo.transform.SetParent(panel, false);
                label = labelGo.AddComponent<TextMeshProUGUI>();
                RectTransform labelRect = label.GetComponent<RectTransform>();
                StretchToFill(labelRect);
            }

            // Fallback default font to prevent TMPro NullReferenceException during layout rendering
            if (label.font == null)
            {
                label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
                if (label.font == null)
                {
                    string[] fontGuids = AssetDatabase.FindAssets("t:TMP_FontAsset");
                    if (fontGuids.Length > 0)
                    {
                        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(fontGuids[0]));
                    }
                }
            }

            label.text = labelText;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 24f;
            label.color = Color.black;

            return btn;
        }

        private void StretchToFill(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }
    }
}
