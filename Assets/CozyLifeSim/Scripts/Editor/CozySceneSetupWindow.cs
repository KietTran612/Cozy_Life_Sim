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

        [MenuItem("Tools/CozySim/Setup Test Scene Silent")]
        public static void GenerateSceneSilent()
        {
            string scenePath = "Assets/CozyLifeSim/Scenes/Main.unity";
            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogError($"[CozySim] Scene file not found at {scenePath}");
                return;
            }

            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
            
            CozySceneSetupWindow window = CreateInstance<CozySceneSetupWindow>();
            window.GenerateScene();
            DestroyImmediate(window);
            
            Debug.Log("<color=green>[CozySim]</color> Silent Scene Generation Completed and Saved.");
        }

        private void GenerateScene()
        {
            if (_styleConfig == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:UIStyleConfig");
                if (guids != null && guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _styleConfig = AssetDatabase.LoadAssetAtPath<UIStyleConfig>(path);
                }
            }

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
            CozyLifeSim.UI.Settings.AnimalDatabase animalDb = AnimalDatabaseUtility.LoadOrCreateDatabase();
            
            CozyLifeSim.UI.Settings.StickerDatabase stickerDb = StickerDatabaseUtility.LoadOrCreateDatabase();

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
            if (animalDb != null)
            {
                SerializedProperty propAnimalDb = soScope.FindProperty("_animalDatabase");
                if (propAnimalDb != null)
                {
                    propAnimalDb.objectReferenceValue = animalDb;
                }
            }
            if (stickerDb != null)
            {
                SerializedProperty propStickerDb = soScope.FindProperty("_stickerDatabase");
                if (propStickerDb != null)
                {
                    propStickerDb.objectReferenceValue = stickerDb;
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
            soAnimal.FindProperty("_animalVisual").objectReferenceValue = chickenVisual;
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

            // 8.5. Setup Sidebar Panel (Navigation Dock)
            RectTransform sidebarPanel = SetupPanel(uiRoot, "Sidebar_Panel");
            sidebarPanel.anchorMin = new Vector2(0.92f, 0.45f);
            sidebarPanel.anchorMax = new Vector2(0.98f, 0.90f);
            sidebarPanel.sizeDelta = Vector2.zero;
            sidebarPanel.anchoredPosition = Vector2.zero;

            // Set up transparent background
            Image sidebarImg = sidebarPanel.gameObject.GetComponent<Image>();
            if (sidebarImg == null)
            {
                sidebarImg = sidebarPanel.gameObject.AddComponent<Image>();
            }
            sidebarImg.color = new Color(0f, 0f, 0f, 0.25f);
            sidebarImg.raycastTarget = true;

            // Remove HorizontalLayoutGroup if exists
            HorizontalLayoutGroup oldLayout = sidebarPanel.gameObject.GetComponent<HorizontalLayoutGroup>();
            if (oldLayout != null)
            {
                DestroyImmediate(oldLayout);
            }

            VerticalLayoutGroup sidebarLayout = sidebarPanel.gameObject.GetComponent<VerticalLayoutGroup>();
            if (sidebarLayout == null)
            {
                sidebarLayout = sidebarPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            }
            sidebarLayout.childAlignment = TextAnchor.UpperCenter;
            sidebarLayout.spacing = 20f;
            sidebarLayout.padding = new RectOffset(5, 5, 20, 20);
            sidebarLayout.childForceExpandHeight = false;
            sidebarLayout.childControlHeight = false;
            sidebarLayout.childControlWidth = true;

            ClearChildren(sidebarPanel);

            Button questBtn = SetupButton(sidebarPanel, "Quest_Button", "Q");
            questBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(55f, 55f);

            Button shopBtn = SetupButton(sidebarPanel, "Shop_Button", "S");
            shopBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(55f, 55f);

            // A. Setup Quest Popup
            RectTransform questPopupPanel = SetupPanel(canvas.transform, "Quest_Popup");
            StretchToFill(questPopupPanel);
            questPopupPanel.gameObject.SetActive(false);

            QuestPopup questPopup = questPopupPanel.gameObject.GetComponent<QuestPopup>();
            if (questPopup == null)
            {
                questPopup = questPopupPanel.gameObject.AddComponent<QuestPopup>();
            }

            // Dim Blocker for Quest Popup
            RectTransform qDimPanel = SetupPanel(questPopupPanel, "Background_Dim");
            StretchToFill(qDimPanel);
            Image qDimImg = qDimPanel.gameObject.GetComponent<Image>();
            if (qDimImg == null) qDimImg = qDimPanel.gameObject.AddComponent<Image>();
            qDimImg.color = new Color(0f, 0f, 0f, 0.4f);
            qDimImg.raycastTarget = true;
            CanvasGroup qDimGroup = qDimPanel.gameObject.GetComponent<CanvasGroup>();
            if (qDimGroup == null) qDimGroup = qDimPanel.gameObject.AddComponent<CanvasGroup>();
            qDimGroup.blocksRaycasts = true;

            // Content Panel for Quest Popup
            RectTransform qContentPanel = SetupPanel(questPopupPanel, "Content_Panel");
            qContentPanel.anchorMin = new Vector2(0.5f, 0.5f);
            qContentPanel.anchorMax = new Vector2(0.5f, 0.5f);
            qContentPanel.pivot = new Vector2(0.5f, 0.5f);
            qContentPanel.sizeDelta = new Vector2(500f, 500f);
            qContentPanel.anchoredPosition = Vector2.zero;
            Image qContentImg = qContentPanel.gameObject.GetComponent<Image>();
            if (qContentImg == null) qContentImg = qContentPanel.gameObject.AddComponent<Image>();
            qContentImg.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

            Button qCloseBtn = SetupButton(qContentPanel, "Close_Button", "X");
            RectTransform qCloseRect = qCloseBtn.GetComponent<RectTransform>();
            qCloseRect.anchorMin = new Vector2(1f, 1f);
            qCloseRect.anchorMax = new Vector2(1f, 1f);
            qCloseRect.pivot = new Vector2(1f, 1f);
            qCloseRect.sizeDelta = new Vector2(40f, 40f);
            qCloseRect.anchoredPosition = new Vector2(-10f, -10f);

            TextMeshProUGUI qTitle = SetupText(qContentPanel, "Quest_Title", "ACTIVE QUESTS", "Header_Text");
            qTitle.fontSize = 24f;
            qTitle.fontStyle = FontStyles.Bold;
            qTitle.raycastTarget = false;
            qTitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 200f);

            RectTransform qItemsList = SetupPanel(qContentPanel, "Quest_Items_List");
            qItemsList.anchorMin = new Vector2(0.1f, 0.1f);
            qItemsList.anchorMax = new Vector2(0.9f, 0.8f);
            qItemsList.sizeDelta = Vector2.zero;
            qItemsList.anchoredPosition = Vector2.zero;
            Image qListImg = qItemsList.gameObject.GetComponent<Image>();
            if (qListImg != null) DestroyImmediate(qListImg);
            VerticalLayoutGroup qItemsLayout = qItemsList.gameObject.GetComponent<VerticalLayoutGroup>();
            if (qItemsLayout == null) qItemsLayout = qItemsList.gameObject.AddComponent<VerticalLayoutGroup>();
            qItemsLayout.childAlignment = TextAnchor.UpperLeft;
            qItemsLayout.spacing = 15f;
            qItemsLayout.childForceExpandHeight = false;

            TextMeshProUGUI qTemplate = SetupText(qItemsList, "Quest_Item_Template", "- Loading Quest...", "");
            qTemplate.fontSize = 20f;
            qTemplate.alignment = TextAlignmentOptions.Left;
            qTemplate.raycastTarget = false;

            // Wire QuestPopup
            SerializedObject soQPopup = new SerializedObject(questPopup);
            soQPopup.FindProperty("_contentPanel").objectReferenceValue = qContentPanel;
            soQPopup.FindProperty("_backgroundDim").objectReferenceValue = qDimGroup;
            soQPopup.FindProperty("_closeButton").objectReferenceValue = qCloseBtn;
            soQPopup.FindProperty("_questItemTemplate").objectReferenceValue = qTemplate;
            soQPopup.ApplyModifiedProperties();

            // B. Setup Shop Popup
            RectTransform shopPopupPanel = SetupPanel(canvas.transform, "Shop_Popup");
            StretchToFill(shopPopupPanel);
            shopPopupPanel.gameObject.SetActive(false);

            ShopPopup shopPopup = shopPopupPanel.gameObject.GetComponent<ShopPopup>();
            if (shopPopup == null)
            {
                shopPopup = shopPopupPanel.gameObject.AddComponent<ShopPopup>();
            }

            // Dim Blocker for Shop Popup
            RectTransform sDimPanel = SetupPanel(shopPopupPanel, "Background_Dim");
            StretchToFill(sDimPanel);
            Image sDimImg = sDimPanel.gameObject.GetComponent<Image>();
            if (sDimImg == null) sDimImg = sDimPanel.gameObject.AddComponent<Image>();
            sDimImg.color = new Color(0f, 0f, 0f, 0.4f);
            sDimImg.raycastTarget = true;
            CanvasGroup sDimGroup = sDimPanel.gameObject.GetComponent<CanvasGroup>();
            if (sDimGroup == null) sDimGroup = sDimPanel.gameObject.AddComponent<CanvasGroup>();
            sDimGroup.blocksRaycasts = true;

            // Content Panel for Shop Popup
            RectTransform sContentPanel = SetupPanel(shopPopupPanel, "Content_Panel");
            sContentPanel.anchorMin = new Vector2(0.5f, 0.5f);
            sContentPanel.anchorMax = new Vector2(0.5f, 0.5f);
            sContentPanel.pivot = new Vector2(0.5f, 0.5f);
            sContentPanel.sizeDelta = new Vector2(850f, 600f);
            sContentPanel.anchoredPosition = Vector2.zero;
            Image sContentImg = sContentPanel.gameObject.GetComponent<Image>();
            if (sContentImg == null) sContentImg = sContentPanel.gameObject.AddComponent<Image>();
            sContentImg.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

            Button sCloseBtn = SetupButton(sContentPanel, "Close_Button", "X");
            RectTransform sCloseRect = sCloseBtn.GetComponent<RectTransform>();
            sCloseRect.anchorMin = new Vector2(1f, 1f);
            sCloseRect.anchorMax = new Vector2(1f, 1f);
            sCloseRect.pivot = new Vector2(1f, 1f);
            sCloseRect.sizeDelta = new Vector2(40f, 40f);
            sCloseRect.anchoredPosition = new Vector2(-10f, -10f);

            TextMeshProUGUI sTitle = SetupText(sContentPanel, "Shop_Title", "TIEM TAP HOA (SHOP)", "Header_Text");
            sTitle.fontSize = 24f;
            sTitle.fontStyle = FontStyles.Bold;
            sTitle.raycastTarget = false;
            sTitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 250f);

            TextMeshProUGUI sCoinsText = SetupText(sContentPanel, "Player_Coins_Text", "Coins: 100", "");
            sCoinsText.fontSize = 20f;
            sCoinsText.fontStyle = FontStyles.Bold;
            sCoinsText.raycastTarget = false;
            sCoinsText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 210f);

            // Setup Tab Buttons
            Button seedsTabBtn = SetupButton(sContentPanel, "Tab_Seeds", "Seeds");
            RectTransform seedsTabBtnRect = seedsTabBtn.GetComponent<RectTransform>();
            seedsTabBtnRect.anchoredPosition = new Vector2(-150f, 160f);
            seedsTabBtnRect.sizeDelta = new Vector2(120f, 35f);

            Button stickersTabBtn = SetupButton(sContentPanel, "Tab_Stickers", "Stickers");
            RectTransform stickersTabBtnRect = stickersTabBtn.GetComponent<RectTransform>();
            stickersTabBtnRect.anchoredPosition = new Vector2(0f, 160f);
            stickersTabBtnRect.sizeDelta = new Vector2(120f, 35f);

            Button cropsTabBtn = SetupButton(sContentPanel, "Tab_Crops", "Crops");
            RectTransform cropsTabBtnRect = cropsTabBtn.GetComponent<RectTransform>();
            cropsTabBtnRect.anchoredPosition = new Vector2(150f, 160f);
            cropsTabBtnRect.sizeDelta = new Vector2(120f, 35f);

            // Three categories containers side-by-side -> Changed to stacked tabs
            RectTransform sGridsContainer = SetupPanel(sContentPanel, "Grids_Container");
            sGridsContainer.anchorMin = new Vector2(0.05f, 0.05f);
            sGridsContainer.anchorMax = new Vector2(0.95f, 0.65f);
            sGridsContainer.sizeDelta = Vector2.zero;
            sGridsContainer.anchoredPosition = Vector2.zero;
            Image sGridContImg = sGridsContainer.gameObject.GetComponent<Image>();
            if (sGridContImg != null) DestroyImmediate(sGridContImg);
            HorizontalLayoutGroup sGridsLayout = sGridsContainer.gameObject.GetComponent<HorizontalLayoutGroup>();
            if (sGridsLayout != null) DestroyImmediate(sGridsLayout); // Remove horizontal layout to stack them!

            // Group 1: Seeds
            RectTransform seedsGroup = SetupPanel(sGridsContainer, "Seeds_Group");
            StretchToFill(seedsGroup);
            CanvasGroup seedsCg = seedsGroup.gameObject.GetComponent<CanvasGroup>();
            if (seedsCg == null) seedsCg = seedsGroup.gameObject.AddComponent<CanvasGroup>();

            VerticalLayoutGroup seedsLayout = seedsGroup.gameObject.GetComponent<VerticalLayoutGroup>();
            if (seedsLayout == null) seedsLayout = seedsGroup.gameObject.AddComponent<VerticalLayoutGroup>();
            seedsLayout.spacing = 10f;
            TextMeshProUGUI seedsTitleText = SetupText(seedsGroup, "Seeds_Title", "Seeds (Buy)", "");
            seedsTitleText.fontSize = 18f;
            seedsTitleText.fontStyle = FontStyles.Bold;
            RectTransform seedsGrid = SetupPanel(seedsGroup, "Seeds_Grid");
            GridLayoutGroup seedsGridGroup = seedsGrid.gameObject.GetComponent<GridLayoutGroup>();
            if (seedsGridGroup == null) seedsGridGroup = seedsGrid.gameObject.AddComponent<GridLayoutGroup>();
            seedsGridGroup.cellSize = new Vector2(200f, 80f);
            seedsGridGroup.spacing = new Vector2(5f, 5f);

            // Group 2: Stickers
            RectTransform stickersGroup = SetupPanel(sGridsContainer, "Stickers_Group");
            StretchToFill(stickersGroup);
            CanvasGroup stickersCg = stickersGroup.gameObject.GetComponent<CanvasGroup>();
            if (stickersCg == null) stickersCg = stickersGroup.gameObject.AddComponent<CanvasGroup>();

            VerticalLayoutGroup stickersLayout = stickersGroup.gameObject.GetComponent<VerticalLayoutGroup>();
            if (stickersLayout == null) stickersLayout = stickersGroup.gameObject.AddComponent<VerticalLayoutGroup>();
            stickersLayout.spacing = 10f;
            TextMeshProUGUI stickersTitleText = SetupText(stickersGroup, "Stickers_Title", "Stickers (Buy)", "");
            stickersTitleText.fontSize = 18f;
            stickersTitleText.fontStyle = FontStyles.Bold;
            RectTransform stickersGrid = SetupPanel(stickersGroup, "Stickers_Grid");
            GridLayoutGroup stickersGridGroup = stickersGrid.gameObject.GetComponent<GridLayoutGroup>();
            if (stickersGridGroup == null) stickersGridGroup = stickersGrid.gameObject.AddComponent<GridLayoutGroup>();
            stickersGridGroup.cellSize = new Vector2(200f, 80f);
            stickersGridGroup.spacing = new Vector2(5f, 5f);

            // Group 3: Crops
            RectTransform cropsGroup = SetupPanel(sGridsContainer, "Crops_Group");
            StretchToFill(cropsGroup);
            CanvasGroup cropsCg = cropsGroup.gameObject.GetComponent<CanvasGroup>();
            if (cropsCg == null) cropsCg = cropsGroup.gameObject.AddComponent<CanvasGroup>();

            VerticalLayoutGroup cropsLayout = cropsGroup.gameObject.GetComponent<VerticalLayoutGroup>();
            if (cropsLayout == null) cropsLayout = cropsGroup.gameObject.AddComponent<VerticalLayoutGroup>();
            cropsLayout.spacing = 10f;
            TextMeshProUGUI cropsTitleText = SetupText(cropsGroup, "Crops_Title", "Crops (Sell)", "");
            cropsTitleText.fontSize = 18f;
            cropsTitleText.fontStyle = FontStyles.Bold;
            RectTransform cropsGrid = SetupPanel(cropsGroup, "Crops_Grid");
            GridLayoutGroup cropsGridGroup = cropsGrid.gameObject.GetComponent<GridLayoutGroup>();
            if (cropsGridGroup == null) cropsGridGroup = cropsGrid.gameObject.AddComponent<GridLayoutGroup>();
            cropsGridGroup.cellSize = new Vector2(200f, 80f);
            cropsGridGroup.spacing = new Vector2(5f, 5f);

            // Shop Item Template under Prefabs_Holder
            RectTransform shopItemTemplate = SetupPanel(prefabsHolder, "Shop_Item_Template");
            shopItemTemplate.sizeDelta = new Vector2(200f, 80f);
            shopItemTemplate.gameObject.SetActive(false);
            ShopItemWidget widgetComponent = shopItemTemplate.gameObject.GetComponent<ShopItemWidget>();
            if (widgetComponent == null)
            {
                widgetComponent = shopItemTemplate.gameObject.AddComponent<ShopItemWidget>();
            }

            Image itemIcon = SetupImage(shopItemTemplate, "Item_Icon");
            itemIcon.GetComponent<RectTransform>().sizeDelta = new Vector2(50f, 50f);
            TextMeshProUGUI itemName = SetupText(shopItemTemplate, "Item_Name", "Product Name", "");
            itemName.fontSize = 14f;
            TextMeshProUGUI itemPrice = SetupText(shopItemTemplate, "Item_Price", "50 Coins", "");
            itemPrice.fontSize = 12f;
            Button itemActionBtn = SetupButton(shopItemTemplate, "Action_Button", "Buy");
            itemActionBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(60f, 30f);
            TextMeshProUGUI itemActionTxt = itemActionBtn.GetComponentInChildren<TextMeshProUGUI>();
            itemActionTxt.fontSize = 12f;

            // Wire ShopItemWidget
            SerializedObject soItem = new SerializedObject(widgetComponent);
            soItem.FindProperty("_itemIcon").objectReferenceValue = itemIcon;
            soItem.FindProperty("_itemNameText").objectReferenceValue = itemName;
            soItem.FindProperty("_priceText").objectReferenceValue = itemPrice;
            soItem.FindProperty("_actionButton").objectReferenceValue = itemActionBtn;
            soItem.FindProperty("_actionButtonText").objectReferenceValue = itemActionTxt;
            soItem.ApplyModifiedProperties();

            // Wire ShopPopup
            SerializedObject soSPopup = new SerializedObject(shopPopup);
            soSPopup.FindProperty("_contentPanel").objectReferenceValue = sContentPanel;
            soSPopup.FindProperty("_backgroundDim").objectReferenceValue = sDimGroup;
            soSPopup.FindProperty("_closeButton").objectReferenceValue = sCloseBtn;
            soSPopup.FindProperty("_playerCoinsText").objectReferenceValue = sCoinsText;
            soSPopup.FindProperty("_itemPrefabTemplate").objectReferenceValue = widgetComponent;
            soSPopup.FindProperty("_seedsContainer").objectReferenceValue = seedsGrid;
            soSPopup.FindProperty("_stickersContainer").objectReferenceValue = stickersGrid;
            soSPopup.FindProperty("_cropsContainer").objectReferenceValue = cropsGrid;
            soSPopup.FindProperty("_seedsTabButton").objectReferenceValue = seedsTabBtn;
            soSPopup.FindProperty("_stickersTabButton").objectReferenceValue = stickersTabBtn;
            soSPopup.FindProperty("_cropsTabButton").objectReferenceValue = cropsTabBtn;
            soSPopup.FindProperty("_seedsGroup").objectReferenceValue = seedsGroup;
            soSPopup.FindProperty("_stickersGroup").objectReferenceValue = stickersGroup;
            soSPopup.FindProperty("_cropsGroup").objectReferenceValue = cropsGroup;
            soSPopup.ApplyModifiedProperties();

            // Wire CozySidebar
            CozySidebar sidebar = sidebarPanel.gameObject.GetComponent<CozySidebar>();
            if (sidebar == null)
            {
                sidebar = sidebarPanel.gameObject.AddComponent<CozySidebar>();
            }
            SerializedObject soSidebar = new SerializedObject(sidebar);
            soSidebar.FindProperty("_questPopup").objectReferenceValue = questPopup;
            soSidebar.FindProperty("_shopPopup").objectReferenceValue = shopPopup;
            soSidebar.FindProperty("_questButton").objectReferenceValue = questBtn;
            soSidebar.FindProperty("_shopButton").objectReferenceValue = shopBtn;
            soSidebar.ApplyModifiedProperties();

            // Setup in-world Interactive Objects (Quest Board & Shop Stall)
            GameObject questBoardGo = GameObject.Find("Quest_Board");
            if (questBoardGo == null) questBoardGo = new GameObject("Quest_Board");
            questBoardGo.transform.position = new Vector3(-6f, 0f, 0f);
            ConfigureWorldClickVisual(questBoardGo, heartSprite != null ? heartSprite : defaultSprite, new Color(1f, 0.82f, 0.2f, 1f), new Vector3(1.2f, 1.2f, 1f), 1);
            BoxCollider2D qCollider = questBoardGo.GetComponent<BoxCollider2D>();
            if (qCollider == null) qCollider = questBoardGo.AddComponent<BoxCollider2D>();
            qCollider.size = new Vector2(2f, 2.5f);
            CozyInteractiveObject qInteractive = questBoardGo.GetComponent<CozyInteractiveObject>();
            if (qInteractive == null) qInteractive = questBoardGo.AddComponent<CozyInteractiveObject>();
            SerializedObject soQInteractive = new SerializedObject(qInteractive);
            soQInteractive.FindProperty("_targetPopup").objectReferenceValue = questPopup;
            soQInteractive.ApplyModifiedProperties();

            GameObject shopStallGo = GameObject.Find("Shop_Stall");
            if (shopStallGo == null) shopStallGo = new GameObject("Shop_Stall");
            shopStallGo.transform.position = new Vector3(6f, 0f, 0f);
            ConfigureWorldClickVisual(shopStallGo, seedSprite != null ? seedSprite : defaultSprite, new Color(0.45f, 1f, 0.58f, 1f), new Vector3(1.3f, 1.3f, 1f), 1);
            BoxCollider2D sCollider = shopStallGo.GetComponent<BoxCollider2D>();
            if (sCollider == null) sCollider = shopStallGo.AddComponent<BoxCollider2D>();
            sCollider.size = new Vector2(2.5f, 2.5f);
            CozyInteractiveObject sInteractive = shopStallGo.GetComponent<CozyInteractiveObject>();
            if (sInteractive == null) sInteractive = shopStallGo.AddComponent<CozyInteractiveObject>();
            SerializedObject soSInteractive = new SerializedObject(sInteractive);
            soSInteractive.FindProperty("_targetPopup").objectReferenceValue = shopPopup;
            soSInteractive.ApplyModifiedProperties();

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

            // Explicitly destroy obsolete Sticker_0 and Sticker_1 children from Inventory_Tray
            for (int i = 0; i < 2; i++)
            {
                Transform obsoleteChild = inventoryTray.Find($"Sticker_{i}");
                if (obsoleteChild != null)
                {
                    DestroyImmediate(obsoleteChild.gameObject);
                }
            }

            // Create a single generic Sticker_Template clone source under Prefabs_Holder
            RectTransform stickerTemplate = SetupPanel(prefabsHolder, "Sticker_Template");
            stickerTemplate.sizeDelta = new Vector2(100f, 100f);
            stickerTemplate.gameObject.SetActive(false); // Inactive in edit mode

            CozySticker genericSticker = stickerTemplate.gameObject.GetComponent<CozySticker>();
            if (genericSticker == null)
            {
                genericSticker = stickerTemplate.gameObject.AddComponent<CozySticker>();
            }

            RectTransform shadowOffset = SetupPanel(stickerTemplate, "Shadow_Offset");
            Image shadowImg = shadowOffset.gameObject.GetComponent<Image>();
            if (shadowImg == null)
            {
                shadowImg = shadowOffset.gameObject.AddComponent<Image>();
            }
            shadowImg.color = new Color(0f, 0f, 0f, 0.3f);
            if (defaultSprite != null) shadowImg.sprite = defaultSprite;

            Image visualImg = SetupImage(stickerTemplate, "Visual_Image");
            if (defaultSprite != null) visualImg.sprite = defaultSprite;

            CanvasGroup group = stickerTemplate.gameObject.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = stickerTemplate.gameObject.AddComponent<CanvasGroup>();
            }

            // Wire generic CozySticker fields
            SerializedObject soSticker = new SerializedObject(genericSticker);
            soSticker.FindProperty("_shadowOffset").objectReferenceValue = shadowOffset;
            soSticker.FindProperty("_canvasGroup").objectReferenceValue = group;
            soSticker.FindProperty("_visualImage").objectReferenceValue = visualImg;

            TextMeshProUGUI countTextText = SetupText(stickerTemplate, "Count_Text", "x1", "");
            countTextText.fontSize = 14f;
            countTextText.alignment = TextAlignmentOptions.BottomRight;
            RectTransform countTextRect = countTextText.GetComponent<RectTransform>();
            countTextRect.anchorMin = new Vector2(1f, 0f);
            countTextRect.anchorMax = new Vector2(1f, 0f);
            countTextRect.pivot = new Vector2(1f, 0f);
            countTextRect.anchoredPosition = new Vector2(-5f, 5f);
            countTextRect.sizeDelta = new Vector2(40f, 20f);

            soSticker.FindProperty("_countText").objectReferenceValue = countTextText;
            soSticker.ApplyModifiedProperties();

            // Wire templates/tray to StickerBook
            SerializedObject soBookUpdate = new SerializedObject(stickerBook);
            SerializedProperty trayRootProp = soBookUpdate.FindProperty("_inventoryTrayRoot");
            if (trayRootProp != null)
            {
                trayRootProp.objectReferenceValue = inventoryTray;
            }
            SerializedProperty prefabProp = soBookUpdate.FindProperty("_stickerPrefabTemplate");
            if (prefabProp != null)
            {
                prefabProp.objectReferenceValue = genericSticker;
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

        private void ClearChildren(RectTransform parent)
        {
            if (parent == null) return;

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        private void ConfigureWorldClickVisual(GameObject target, Sprite sprite, Color fallbackColor, Vector3 scale, int sortingOrder)
        {
            if (target == null) return;

            SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = target.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = sprite;
            renderer.color = sprite == null ? fallbackColor : Color.white;
            renderer.sortingOrder = sortingOrder;
            target.transform.localScale = scale;
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

        [MenuItem("Tools/CozySim/Refresh Asset Database")]
        public static void RefreshAssetDatabase()
        {
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log("<color=green>[CozySim]</color> AssetDatabase.Refresh() completed on the main thread!");
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
