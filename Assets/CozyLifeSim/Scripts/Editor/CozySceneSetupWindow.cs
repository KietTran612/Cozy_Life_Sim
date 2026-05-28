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

            if (_styleConfig != null)
            {
                SerializedObject soScope = new SerializedObject(lifetimeScope);
                SerializedProperty propStyle = soScope.FindProperty("_defaultStyleConfig");
                if (propStyle != null)
                {
                    propStyle.objectReferenceValue = _styleConfig;
                    soScope.ApplyModifiedProperties();
                }
            }

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
