using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
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

            ConfigureBuildAndPlayerSettings();

            bool isSceneDirty = false;

            // 0. Setup Main Camera
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject camGo = GameObject.Find("Main Camera") ?? new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                camera = camGo.GetComponent<Camera>() ?? camGo.AddComponent<Camera>();
                isSceneDirty = true;
            }

            if (camera != null)
            {
                Vector3 targetCamPos = new Vector3(0f, 0f, -10f);
                if (Vector3.Distance(camera.transform.position, targetCamPos) > 0.001f)
                {
                    camera.transform.position = targetCamPos;
                    isSceneDirty = true;
                }
                if (camera.transform.rotation != Quaternion.identity)
                {
                    camera.transform.rotation = Quaternion.identity;
                    isSceneDirty = true;
                }
                if (!camera.orthographic)
                {
                    camera.orthographic = true;
                    isSceneDirty = true;
                }
                if (!Mathf.Approximately(camera.orthographicSize, 5f))
                {
                    camera.orthographicSize = 5f;
                    isSceneDirty = true;
                }
                if (!Mathf.Approximately(camera.nearClipPlane, 0.3f))
                {
                    camera.nearClipPlane = 0.3f;
                    isSceneDirty = true;
                }
                if (!Mathf.Approximately(camera.farClipPlane, 1000f))
                {
                    camera.farClipPlane = 1000f;
                    isSceneDirty = true;
                }
                if (camera.clearFlags != CameraClearFlags.SolidColor)
                {
                    camera.clearFlags = CameraClearFlags.SolidColor;
                    isSceneDirty = true;
                }
                Color targetCamBg = new Color(0.18f, 0.18f, 0.22f);
                if (camera.backgroundColor != targetCamBg)
                {
                    camera.backgroundColor = targetCamBg;
                    isSceneDirty = true;
                }
                if (camera.rect != new Rect(0f, 0f, 1f, 1f))
                {
                    camera.rect = new Rect(0f, 0f, 1f, 1f);
                    isSceneDirty = true;
                }

                if (camera.GetComponent<AudioListener>() == null && FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 0)
                {
                    camera.gameObject.AddComponent<AudioListener>();
                    isSceneDirty = true;
                }
            }

            // 1. Setup GameLifetimeScope
            GameObject lifetimeScopeGo = GameObject.Find("GameLifetimeScope");
            if (lifetimeScopeGo == null)
            {
                lifetimeScopeGo = new GameObject("GameLifetimeScope");
                isSceneDirty = true;
            }

            GameLifetimeScope lifetimeScope = lifetimeScopeGo.GetComponent<GameLifetimeScope>();
            if (lifetimeScope == null)
            {
                lifetimeScope = lifetimeScopeGo.AddComponent<GameLifetimeScope>();
                isSceneDirty = true;
            }

            // Find and wire up QuestDatabase, CropDatabase, and UIStyleConfig to GameLifetimeScope
            CozyLifeSim.UI.Settings.QuestDatabase questDb = QuestDatabaseUtility.LoadOrCreateDatabase();

            CozyLifeSim.UI.Settings.CropDatabase cropDb = CropDatabaseUtility.LoadOrCreateDatabase();
            CozyLifeSim.UI.Settings.AnimalDatabase animalDb = AnimalDatabaseUtility.LoadOrCreateDatabase();
            CozyLifeSim.UI.Settings.StickerDatabase stickerDb = StickerDatabaseUtility.LoadOrCreateDatabase();

            SerializedObject soScope = new SerializedObject(lifetimeScope);
            bool scopeDirty = false;
            if (_styleConfig != null)
            {
                SerializedProperty propStyle = soScope.FindProperty("_defaultStyleConfig");
                SafeSetObjectReference(propStyle, _styleConfig, ref scopeDirty);
            }
            if (questDb != null)
            {
                SerializedProperty propQuestDb = soScope.FindProperty("_questDatabase");
                SafeSetObjectReference(propQuestDb, questDb, ref scopeDirty);
            }
            if (cropDb != null)
            {
                SerializedProperty propCropDb = soScope.FindProperty("_cropDatabase");
                SafeSetObjectReference(propCropDb, cropDb, ref scopeDirty);
            }
            if (animalDb != null)
            {
                SerializedProperty propAnimalDb = soScope.FindProperty("_animalDatabase");
                SafeSetObjectReference(propAnimalDb, animalDb, ref scopeDirty);
            }
            if (stickerDb != null)
            {
                SerializedProperty propStickerDb = soScope.FindProperty("_stickerDatabase");
                SafeSetObjectReference(propStickerDb, stickerDb, ref scopeDirty);
            }
            if (scopeDirty)
            {
                soScope.ApplyModifiedProperties();
                isSceneDirty = true;
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
                isSceneDirty = true;
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    isSceneDirty = true;
                }
                Vector2 refRes = new Vector2(1920, 1080);
                if (scaler.referenceResolution != refRes)
                {
                    scaler.referenceResolution = refRes;
                    isSceneDirty = true;
                }
                if (scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.MatchWidthOrHeight)
                {
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    isSceneDirty = true;
                }
                if (!Mathf.Approximately(scaler.matchWidthOrHeight, 0.5f))
                {
                    scaler.matchWidthOrHeight = 0.5f;
                    isSceneDirty = true;
                }
            }

            // Setup EventSystem
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                isSceneDirty = true;
            }

            // 3. Setup UI_Root
            RectTransform uiRoot = SetupPanel(canvas.transform, "UI_Root", ref isSceneDirty);
            StretchToFill(uiRoot, ref isSceneDirty);

            // 4. Setup Header Panel
            RectTransform headerPanel = SetupPanel(uiRoot, "Header_Panel", ref isSceneDirty);
            SafeSetAnchor(headerPanel, new Vector2(0f, 0.9f), new Vector2(1f, 1f), ref isSceneDirty);
            SafeSetSizeDelta(headerPanel, Vector2.zero, ref isSceneDirty);
            SafeSetAnchoredPosition(headerPanel, Vector2.zero, ref isSceneDirty);

            TextMeshProUGUI coinsText = SetupText(headerPanel, "Coins_Text", "Coins: 100", "Header_Text", ref isSceneDirty);
            TextMeshProUGUI seedsText = SetupText(headerPanel, "Seeds_Text", "Seeds: 5", "Header_Text", ref isSceneDirty);
            TextMeshProUGUI cropsText = SetupText(headerPanel, "Crops_Text", "Crops: 0", "Header_Text", ref isSceneDirty);

            // Attach InventoryHudWidget to Header_Panel
            InventoryHudWidget inventoryHud = headerPanel.gameObject.GetComponent<InventoryHudWidget>();
            if (inventoryHud == null)
            {
                inventoryHud = headerPanel.gameObject.AddComponent<InventoryHudWidget>();
                isSceneDirty = true;
            }
            SerializedObject soHud = new SerializedObject(inventoryHud);
            bool hudDirty = false;
            SafeSetObjectReference(soHud.FindProperty("_coinsText"), coinsText, ref hudDirty);
            SafeSetObjectReference(soHud.FindProperty("_seedsText"), seedsText, ref hudDirty);
            SafeSetObjectReference(soHud.FindProperty("_cropsText"), cropsText, ref hudDirty);
            if (hudDirty)
            {
                soHud.ApplyModifiedProperties();
                isSceneDirty = true;
            }

            // Setup Progression_HUD under Header_Panel
            RectTransform progressionHud = SetupPanel(headerPanel, "Progression_HUD", ref isSceneDirty);
            SafeSetSizeDelta(progressionHud, Vector2.zero, ref isSceneDirty);

            HorizontalLayoutGroup progLayout = progressionHud.gameObject.GetComponent<HorizontalLayoutGroup>();
            if (progLayout == null)
            {
                progLayout = progressionHud.gameObject.AddComponent<HorizontalLayoutGroup>();
                isSceneDirty = true;
            }
            if (progLayout.childAlignment != TextAnchor.MiddleCenter) { progLayout.childAlignment = TextAnchor.MiddleCenter; isSceneDirty = true; }
            if (!Mathf.Approximately(progLayout.spacing, 10f)) { progLayout.spacing = 10f; isSceneDirty = true; }
            if (!progLayout.childControlHeight) { progLayout.childControlHeight = true; isSceneDirty = true; }
            if (!progLayout.childControlWidth) { progLayout.childControlWidth = true; isSceneDirty = true; }

            TextMeshProUGUI levelText = SetupText(progressionHud, "Level_Text", "Level: 1", "Header_Text", ref isSceneDirty);

            RectTransform xpBarContainer = SetupPanel(progressionHud, "XP_Bar_Container", ref isSceneDirty);
            SafeSetSizeDelta(xpBarContainer, Vector2.zero, ref isSceneDirty);

            Image xpBarBg = SetupImage(xpBarContainer, "XP_Bar_Bg", ref isSceneDirty);
            StretchToFill(xpBarBg.GetComponent<RectTransform>(), ref isSceneDirty);
            Color bgCol = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            if (xpBarBg.color != bgCol) { xpBarBg.color = bgCol; isSceneDirty = true; }

            Image xpBarFill = SetupImage(xpBarContainer, "XP_Bar_Fill", ref isSceneDirty);
            StretchToFill(xpBarFill.GetComponent<RectTransform>(), ref isSceneDirty);
            Color fillCol = new Color(0.2f, 0.8f, 0.2f, 1f);
            if (xpBarFill.color != fillCol) { xpBarFill.color = fillCol; isSceneDirty = true; }

            if (xpBarFill.type != Image.Type.Filled) { xpBarFill.type = Image.Type.Filled; isSceneDirty = true; }
            if (xpBarFill.fillMethod != Image.FillMethod.Horizontal) { xpBarFill.fillMethod = Image.FillMethod.Horizontal; isSceneDirty = true; }
            if (xpBarFill.fillOrigin != (int)Image.OriginHorizontal.Left) { xpBarFill.fillOrigin = (int)Image.OriginHorizontal.Left; isSceneDirty = true; }

            // Attach ProgressionHudWidget
            ProgressionHudWidget progWidget = progressionHud.gameObject.GetComponent<ProgressionHudWidget>();
            if (progWidget == null)
            {
                progWidget = progressionHud.gameObject.AddComponent<ProgressionHudWidget>();
                isSceneDirty = true;
            }

            SerializedObject soProg = new SerializedObject(progWidget);
            bool progDirty = false;
            SafeSetObjectReference(soProg.FindProperty("_levelText"), levelText, ref progDirty);
            SafeSetObjectReference(soProg.FindProperty("_xpProgressBar"), xpBarFill, ref progDirty);
            if (progDirty)
            {
                soProg.ApplyModifiedProperties();
                isSceneDirty = true;
            }

            // Setup Cozy_Juice_Utility under UI_Root
            RectTransform juiceUtilityRect = SetupPanel(uiRoot, "Cozy_Juice_Utility", ref isSceneDirty);
            CozyJuiceUtility juiceUtility = juiceUtilityRect.gameObject.GetComponent<CozyJuiceUtility>();
            if (juiceUtility == null)
            {
                juiceUtility = juiceUtilityRect.gameObject.AddComponent<CozyJuiceUtility>();
                isSceneDirty = true;
            }

            SerializedObject soJuice = new SerializedObject(juiceUtility);
            bool juiceDirty = false;
            SafeSetObjectReference(soJuice.FindProperty("_coinTargetTransform"), coinsText.GetComponent<RectTransform>(), ref juiceDirty);
            if (juiceDirty)
            {
                soJuice.ApplyModifiedProperties();
                isSceneDirty = true;
            }

            // Wire CozyJuiceUtility to GameLifetimeScope
            SerializedObject soScopeUpdate = new SerializedObject(lifetimeScope);
            bool scopeUpdateDirty = false;
            SafeSetObjectReference(soScopeUpdate.FindProperty("_juiceUtility"), juiceUtility, ref scopeUpdateDirty);
            if (scopeUpdateDirty)
            {
                soScopeUpdate.ApplyModifiedProperties();
                isSceneDirty = true;
            }

            // Horizontal layout for header panel
            HorizontalLayoutGroup headerLayout = headerPanel.gameObject.GetComponent<HorizontalLayoutGroup>();
            if (headerLayout == null)
            {
                headerLayout = headerPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                isSceneDirty = true;
            }
            if (headerLayout.childAlignment != TextAnchor.MiddleCenter) { headerLayout.childAlignment = TextAnchor.MiddleCenter; isSceneDirty = true; }
            if (!Mathf.Approximately(headerLayout.spacing, 100f)) { headerLayout.spacing = 100f; isSceneDirty = true; }
            if (!headerLayout.childControlHeight) { headerLayout.childControlHeight = true; isSceneDirty = true; }
            if (!headerLayout.childControlWidth) { headerLayout.childControlWidth = true; isSceneDirty = true; }

            // 5. Setup Gameplay Area
            RectTransform gameplayArea = SetupPanel(uiRoot, "Gameplay_Area", ref isSceneDirty);
            SafeSetAnchor(gameplayArea, new Vector2(0f, 0.25f), new Vector2(1f, 0.9f), ref isSceneDirty);
            SafeSetSizeDelta(gameplayArea, Vector2.zero, ref isSceneDirty);
            SafeSetAnchoredPosition(gameplayArea, Vector2.zero, ref isSceneDirty);

            // Horizontal layout for gameplay areas
            HorizontalLayoutGroup gameplayLayout = gameplayArea.gameObject.GetComponent<HorizontalLayoutGroup>();
            if (gameplayLayout == null)
            {
                gameplayLayout = gameplayArea.gameObject.AddComponent<HorizontalLayoutGroup>();
                isSceneDirty = true;
            }
            if (gameplayLayout.childAlignment != TextAnchor.MiddleCenter) { gameplayLayout.childAlignment = TextAnchor.MiddleCenter; isSceneDirty = true; }
            if (!Mathf.Approximately(gameplayLayout.spacing, 80f)) { gameplayLayout.spacing = 80f; isSceneDirty = true; }
            if (gameplayLayout.childControlHeight) { gameplayLayout.childControlHeight = false; isSceneDirty = true; }
            if (gameplayLayout.childControlWidth) { gameplayLayout.childControlWidth = false; isSceneDirty = true; }

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
                isSceneDirty = true;
            }
            Sprite expectedWCanSprite = wateringCanSprite != null ? wateringCanSprite : defaultSprite;
            if (wateringCanImg.sprite != expectedWCanSprite)
            {
                wateringCanImg.sprite = expectedWCanSprite;
                isSceneDirty = true;
            }

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

            SafeSetObjectReference(soCrop.FindProperty("_seedSprite"), seedSprite != null ? seedSprite : defaultSprite, ref cropDirty);
            SafeSetObjectReference(soCrop.FindProperty("_sproutSprite"), sproutSprite != null ? sproutSprite : defaultSprite, ref cropDirty);
            SafeSetObjectReference(soCrop.FindProperty("_matureSprite"), matureSprite != null ? matureSprite : defaultSprite, ref cropDirty);
            SafeSetObjectReference(soCrop.FindProperty("_harvestSprite"), matureSprite != null ? matureSprite : defaultSprite, ref cropDirty);

            if (cropDirty)
            {
                soCrop.ApplyModifiedProperties();
                isSceneDirty = true;
            }

            // 7. Setup Animal Pen (Nuoi ga)
            RectTransform animalPen = SetupPanel(gameplayArea, "Animal_Pen", ref isSceneDirty);
            SafeSetSizeDelta(animalPen, new Vector2(350f, 500f), ref isSceneDirty);
            AnimalWidget animalWidget = animalPen.gameObject.GetComponent<AnimalWidget>();
            if (animalWidget == null)
            {
                animalWidget = animalPen.gameObject.AddComponent<AnimalWidget>();
                isSceneDirty = true;
            }

            Image chickenVisual = SetupImage(animalPen, "Chicken_Visual", ref isSceneDirty);
            Sprite expectedChickenSprite = chickenSprite != null ? chickenSprite : defaultSprite;
            if (chickenVisual.sprite != expectedChickenSprite)
            {
                chickenVisual.sprite = expectedChickenSprite;
                isSceneDirty = true;
            }

            Button interactBtn = SetupButton(animalPen, "Interaction_Button", "Pet Chicken", ref isSceneDirty);
            RectTransform spawnRoot = SetupPanel(animalPen, "Spawn_Root", ref isSceneDirty);

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

            // Setup Prefabs Holder under Canvas
            RectTransform prefabsHolder = SetupPanel(canvas.transform, "Prefabs_Holder", ref isSceneDirty);
            if (prefabsHolder.gameObject.activeSelf)
            {
                prefabsHolder.gameObject.SetActive(false);
                isSceneDirty = true;
            }

            // Create Heart_Feedback_Template
            RectTransform heartTemplate = SetupPanel(prefabsHolder, "Heart_Feedback_Template", ref isSceneDirty);
            Image heartImg = heartTemplate.gameObject.GetComponent<Image>();
            if (heartImg == null)
            {
                heartImg = heartTemplate.gameObject.AddComponent<Image>();
                isSceneDirty = true;
            }
            if (heartSprite != null)
            {
                if (heartImg.sprite != heartSprite)
                {
                    heartImg.sprite = heartSprite;
                    isSceneDirty = true;
                }
                if (heartImg.color != Color.white)
                {
                    heartImg.color = Color.white;
                    isSceneDirty = true;
                }
            }
            else
            {
                if (heartImg.color != Color.red)
                {
                    heartImg.color = Color.red;
                    isSceneDirty = true;
                }
                if (heartImg.sprite != defaultSprite)
                {
                    heartImg.sprite = defaultSprite;
                    isSceneDirty = true;
                }
            }

            CanvasGroup heartGroup = heartTemplate.gameObject.GetComponent<CanvasGroup>();
            if (heartGroup == null)
            {
                heartGroup = heartTemplate.gameObject.AddComponent<CanvasGroup>();
                isSceneDirty = true;
            }

            // Auto-wire AnimalWidget
            SerializedObject soAnimal = new SerializedObject(animalWidget);
            bool animalDirty = false;
            SafeSetObjectReference(soAnimal.FindProperty("_animalVisual"), chickenVisual, ref animalDirty);
            SafeSetObjectReference(soAnimal.FindProperty("_interactionButton"), interactBtn, ref animalDirty);
            SafeSetObjectReference(soAnimal.FindProperty("_spawnRoot"), spawnRoot, ref animalDirty);
            SafeSetObjectReference(soAnimal.FindProperty("_heartPrefab"), heartTemplate, ref animalDirty);
            if (animalDirty)
            {
                soAnimal.ApplyModifiedProperties();
                isSceneDirty = true;
            }

            // 8. Setup Sticker Book
            RectTransform stickerBookPanel = SetupPanel(gameplayArea, "StickerBook_Panel", ref isSceneDirty);
            SafeSetSizeDelta(stickerBookPanel, new Vector2(500f, 500f), ref isSceneDirty);
            StickerBook stickerBook = stickerBookPanel.gameObject.GetComponent<StickerBook>();
            if (stickerBook == null)
            {
                stickerBook = stickerBookPanel.gameObject.AddComponent<StickerBook>();
                isSceneDirty = true;
            }

            Button prevBtn = SetupButton(stickerBookPanel, "Prev_Button", "< Page", ref isSceneDirty);
            Button nextBtn = SetupButton(stickerBookPanel, "Next_Button", "Page >", ref isSceneDirty);
            Button styleBtn = SetupButton(stickerBookPanel, "Style_Button", "Style", ref isSceneDirty);
            Button addNoteBtn = SetupButton(stickerBookPanel, "Add_Note_Button", "Note", ref isSceneDirty);
            SafeSetSizeDelta(styleBtn.GetComponent<RectTransform>(), new Vector2(90f, 35f), ref isSceneDirty);
            SafeSetSizeDelta(addNoteBtn.GetComponent<RectTransform>(), new Vector2(90f, 35f), ref isSceneDirty);
            RectTransform flipIndicator = SetupPanel(stickerBookPanel, "Flip_Page_Indicator", ref isSceneDirty);
            if (flipIndicator.gameObject.activeSelf)
            {
                flipIndicator.gameObject.SetActive(false);
                isSceneDirty = true;
            }

            RectTransform page0 = SetupPanel(stickerBookPanel, "Page_0", ref isSceneDirty);
            StickerBookPage bookPage0 = page0.gameObject.GetComponent<StickerBookPage>();
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
            if (page0Dirty)
            {
                soPage0.ApplyModifiedProperties();
                isSceneDirty = true;
            }

            RectTransform page1 = SetupPanel(stickerBookPanel, "Page_1", ref isSceneDirty);
            StickerBookPage bookPage1 = page1.gameObject.GetComponent<StickerBookPage>();
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

            // 8.5. Setup Sidebar Panel (Navigation Dock)
            RectTransform sidebarPanel = SetupPanel(uiRoot, "Sidebar_Panel", ref isSceneDirty);
            SafeSetAnchor(sidebarPanel, new Vector2(0.92f, 0.45f), new Vector2(0.98f, 0.90f), ref isSceneDirty);
            SafeSetSizeDelta(sidebarPanel, Vector2.zero, ref isSceneDirty);
            SafeSetAnchoredPosition(sidebarPanel, Vector2.zero, ref isSceneDirty);

            // Set up transparent background
            Image sidebarImg = sidebarPanel.gameObject.GetComponent<Image>();
            if (sidebarImg == null)
            {
                sidebarImg = sidebarPanel.gameObject.AddComponent<Image>();
                isSceneDirty = true;
            }
            Color sidebarBg = new Color(0f, 0f, 0f, 0.25f);
            if (sidebarImg.color != sidebarBg)
            {
                sidebarImg.color = sidebarBg;
                isSceneDirty = true;
            }
            if (!sidebarImg.raycastTarget)
            {
                sidebarImg.raycastTarget = true;
                isSceneDirty = true;
            }

            // Remove HorizontalLayoutGroup if exists
            HorizontalLayoutGroup oldLayout = sidebarPanel.gameObject.GetComponent<HorizontalLayoutGroup>();
            if (oldLayout != null)
            {
                DestroyImmediate(oldLayout);
                isSceneDirty = true;
            }

            VerticalLayoutGroup sidebarLayout = sidebarPanel.gameObject.GetComponent<VerticalLayoutGroup>();
            if (sidebarLayout == null)
            {
                sidebarLayout = sidebarPanel.gameObject.AddComponent<VerticalLayoutGroup>();
                isSceneDirty = true;
            }
            if (sidebarLayout.childAlignment != TextAnchor.UpperCenter) { sidebarLayout.childAlignment = TextAnchor.UpperCenter; isSceneDirty = true; }
            if (!Mathf.Approximately(sidebarLayout.spacing, 20f)) { sidebarLayout.spacing = 20f; isSceneDirty = true; }
            if (sidebarLayout.padding.left != 5 || sidebarLayout.padding.right != 5 || sidebarLayout.padding.top != 20 || sidebarLayout.padding.bottom != 20)
            {
                sidebarLayout.padding = new RectOffset(5, 5, 20, 20);
                isSceneDirty = true;
            }
            if (sidebarLayout.childForceExpandHeight) { sidebarLayout.childForceExpandHeight = false; isSceneDirty = true; }
            if (sidebarLayout.childControlHeight) { sidebarLayout.childControlHeight = false; isSceneDirty = true; }
            if (!sidebarLayout.childControlWidth) { sidebarLayout.childControlWidth = true; isSceneDirty = true; }

            // Safe Sibling reuse for Sidebar buttons to avoid ClearChildren
            Button questBtn = SetupButton(sidebarPanel, "Quest_Button", "Q", ref isSceneDirty);
            SafeSetSizeDelta(questBtn.GetComponent<RectTransform>(), new Vector2(0f, 55f), ref isSceneDirty);

            Button shopBtn = SetupButton(sidebarPanel, "Shop_Button", "S", ref isSceneDirty);
            SafeSetSizeDelta(shopBtn.GetComponent<RectTransform>(), new Vector2(0f, 55f), ref isSceneDirty);

            // A. Setup Quest Popup
            RectTransform questPopupPanel = SetupPanel(canvas.transform, "Quest_Popup", ref isSceneDirty);
            StretchToFill(questPopupPanel, ref isSceneDirty);
            if (questPopupPanel.gameObject.activeSelf)
            {
                questPopupPanel.gameObject.SetActive(false);
                isSceneDirty = true;
            }

            QuestPopup questPopup = questPopupPanel.gameObject.GetComponent<QuestPopup>();
            if (questPopup == null)
            {
                questPopup = questPopupPanel.gameObject.AddComponent<QuestPopup>();
                isSceneDirty = true;
            }

            // Dim Blocker for Quest Popup
            RectTransform qDimPanel = SetupPanel(questPopupPanel, "Background_Dim", ref isSceneDirty);
            StretchToFill(qDimPanel, ref isSceneDirty);
            Image qDimImg = qDimPanel.gameObject.GetComponent<Image>();
            if (qDimImg == null)
            {
                qDimImg = qDimPanel.gameObject.AddComponent<Image>();
                isSceneDirty = true;
            }
            Color dimColor = new Color(0f, 0f, 0f, 0.4f);
            if (qDimImg.color != dimColor)
            {
                qDimImg.color = dimColor;
                isSceneDirty = true;
            }
            if (!qDimImg.raycastTarget)
            {
                qDimImg.raycastTarget = true;
                isSceneDirty = true;
            }
            CanvasGroup qDimGroup = qDimPanel.gameObject.GetComponent<CanvasGroup>();
            if (qDimGroup == null)
            {
                qDimGroup = qDimPanel.gameObject.AddComponent<CanvasGroup>();
                isSceneDirty = true;
            }
            if (!qDimGroup.blocksRaycasts)
            {
                qDimGroup.blocksRaycasts = true;
                isSceneDirty = true;
            }

            // Content Panel for Quest Popup
            RectTransform qContentPanel = SetupPanel(questPopupPanel, "Content_Panel", ref isSceneDirty);
            SafeSetAnchor(qContentPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), ref isSceneDirty);
            SafeSetPivot(qContentPanel, new Vector2(0.5f, 0.5f), ref isSceneDirty);
            SafeSetSizeDelta(qContentPanel, new Vector2(500f, 500f), ref isSceneDirty);
            SafeSetAnchoredPosition(qContentPanel, Vector2.zero, ref isSceneDirty);
            Image qContentImg = qContentPanel.gameObject.GetComponent<Image>();
            if (qContentImg == null)
            {
                qContentImg = qContentPanel.gameObject.AddComponent<Image>();
                isSceneDirty = true;
            }
            Color contentBg = new Color(0.15f, 0.15f, 0.15f, 0.95f);
            if (qContentImg.color != contentBg)
            {
                qContentImg.color = contentBg;
                isSceneDirty = true;
            }

            Button qCloseBtn = SetupButton(qContentPanel, "Close_Button", "X", ref isSceneDirty);
            RectTransform qCloseRect = qCloseBtn.GetComponent<RectTransform>();
            SafeSetAnchor(qCloseRect, new Vector2(1f, 1f), new Vector2(1f, 1f), ref isSceneDirty);
            SafeSetPivot(qCloseRect, new Vector2(1f, 1f), ref isSceneDirty);
            SafeSetSizeDelta(qCloseRect, new Vector2(40f, 40f), ref isSceneDirty);
            SafeSetAnchoredPosition(qCloseRect, new Vector2(-10f, -10f), ref isSceneDirty);

            TextMeshProUGUI qTitle = SetupText(qContentPanel, "Quest_Title", "ACTIVE QUESTS", "Header_Text", ref isSceneDirty);
            if (!Mathf.Approximately(qTitle.fontSize, 24f)) { qTitle.fontSize = 24f; isSceneDirty = true; }
            if (qTitle.fontStyle != FontStyles.Bold) { qTitle.fontStyle = FontStyles.Bold; isSceneDirty = true; }
            if (qTitle.raycastTarget) { qTitle.raycastTarget = false; isSceneDirty = true; }
            SafeSetAnchoredPosition(qTitle.GetComponent<RectTransform>(), new Vector2(0f, 200f), ref isSceneDirty);

            RectTransform qItemsList = SetupPanel(qContentPanel, "Quest_Items_List", ref isSceneDirty);
            SafeSetAnchor(qItemsList, new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.8f), ref isSceneDirty);
            SafeSetSizeDelta(qItemsList, Vector2.zero, ref isSceneDirty);
            SafeSetAnchoredPosition(qItemsList, Vector2.zero, ref isSceneDirty);
            Image qListImg = qItemsList.gameObject.GetComponent<Image>();
            if (qListImg != null)
            {
                DestroyImmediate(qListImg);
                isSceneDirty = true;
            }
            VerticalLayoutGroup qItemsLayout = qItemsList.gameObject.GetComponent<VerticalLayoutGroup>();
            if (qItemsLayout == null)
            {
                qItemsLayout = qItemsList.gameObject.AddComponent<VerticalLayoutGroup>();
                isSceneDirty = true;
            }
            if (qItemsLayout.childAlignment != TextAnchor.UpperLeft) { qItemsLayout.childAlignment = TextAnchor.UpperLeft; isSceneDirty = true; }
            if (!Mathf.Approximately(qItemsLayout.spacing, 15f)) { qItemsLayout.spacing = 15f; isSceneDirty = true; }
            if (qItemsLayout.childForceExpandHeight) { qItemsLayout.childForceExpandHeight = false; isSceneDirty = true; }

            TextMeshProUGUI qTemplate = SetupText(qItemsList, "Quest_Item_Template", "- Loading Quest...", "", ref isSceneDirty);
            if (!Mathf.Approximately(qTemplate.fontSize, 20f)) { qTemplate.fontSize = 20f; isSceneDirty = true; }
            if (qTemplate.alignment != TextAlignmentOptions.Left) { qTemplate.alignment = TextAlignmentOptions.Left; isSceneDirty = true; }
            if (qTemplate.raycastTarget) { qTemplate.raycastTarget = false; isSceneDirty = true; }

            // Wire QuestPopup
            SerializedObject soQPopup = new SerializedObject(questPopup);
            bool qPopupDirty = false;
            SafeSetObjectReference(soQPopup.FindProperty("_contentPanel"), qContentPanel, ref qPopupDirty);
            SafeSetObjectReference(soQPopup.FindProperty("_backgroundDim"), qDimGroup, ref qPopupDirty);
            SafeSetObjectReference(soQPopup.FindProperty("_closeButton"), qCloseBtn, ref qPopupDirty);
            SafeSetObjectReference(soQPopup.FindProperty("_questItemTemplate"), qTemplate, ref qPopupDirty);
            if (qPopupDirty)
            {
                soQPopup.ApplyModifiedProperties();
                isSceneDirty = true;
            }

            // B. Setup Shop Popup
            RectTransform shopPopupPanel = SetupPanel(canvas.transform, "Shop_Popup", ref isSceneDirty);
            StretchToFill(shopPopupPanel, ref isSceneDirty);
            if (shopPopupPanel.gameObject.activeSelf)
            {
                shopPopupPanel.gameObject.SetActive(false);
                isSceneDirty = true;
            }

            ShopPopup shopPopup = shopPopupPanel.gameObject.GetComponent<ShopPopup>();
            if (shopPopup == null)
            {
                shopPopup = shopPopupPanel.gameObject.AddComponent<ShopPopup>();
                isSceneDirty = true;
            }

            // Dim Blocker for Shop Popup
            RectTransform sDimPanel = SetupPanel(shopPopupPanel, "Background_Dim", ref isSceneDirty);
            StretchToFill(sDimPanel, ref isSceneDirty);
            Image sDimImg = sDimPanel.gameObject.GetComponent<Image>();
            if (sDimImg == null)
            {
                sDimImg = sDimPanel.gameObject.AddComponent<Image>();
                isSceneDirty = true;
            }
            if (sDimImg.color != dimColor)
            {
                sDimImg.color = dimColor;
                isSceneDirty = true;
            }
            if (!sDimImg.raycastTarget)
            {
                sDimImg.raycastTarget = true;
                isSceneDirty = true;
            }
            CanvasGroup sDimGroup = sDimPanel.gameObject.GetComponent<CanvasGroup>();
            if (sDimGroup == null)
            {
                sDimGroup = sDimPanel.gameObject.AddComponent<CanvasGroup>();
                isSceneDirty = true;
            }
            if (!sDimGroup.blocksRaycasts)
            {
                sDimGroup.blocksRaycasts = true;
                isSceneDirty = true;
            }

            // Content Panel for Shop Popup
            RectTransform sContentPanel = SetupPanel(shopPopupPanel, "Content_Panel", ref isSceneDirty);
            SafeSetAnchor(sContentPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), ref isSceneDirty);
            SafeSetPivot(sContentPanel, new Vector2(0.5f, 0.5f), ref isSceneDirty);
            SafeSetSizeDelta(sContentPanel, new Vector2(850f, 600f), ref isSceneDirty);
            SafeSetAnchoredPosition(sContentPanel, Vector2.zero, ref isSceneDirty);
            Image sContentImg = sContentPanel.gameObject.GetComponent<Image>();
            if (sContentImg == null)
            {
                sContentImg = sContentPanel.gameObject.AddComponent<Image>();
                isSceneDirty = true;
            }
            if (sContentImg.color != contentBg)
            {
                sContentImg.color = contentBg;
                isSceneDirty = true;
            }

            Button sCloseBtn = SetupButton(sContentPanel, "Close_Button", "X", ref isSceneDirty);
            RectTransform sCloseRect = sCloseBtn.GetComponent<RectTransform>();
            SafeSetAnchor(sCloseRect, new Vector2(1f, 1f), new Vector2(1f, 1f), ref isSceneDirty);
            SafeSetPivot(sCloseRect, new Vector2(1f, 1f), ref isSceneDirty);
            SafeSetSizeDelta(sCloseRect, new Vector2(40f, 40f), ref isSceneDirty);
            SafeSetAnchoredPosition(sCloseRect, new Vector2(-10f, -10f), ref isSceneDirty);

            TextMeshProUGUI sTitle = SetupText(sContentPanel, "Shop_Title", "TIEM TAP HOA (SHOP)", "Header_Text", ref isSceneDirty);
            if (!Mathf.Approximately(sTitle.fontSize, 24f)) { sTitle.fontSize = 24f; isSceneDirty = true; }
            if (sTitle.fontStyle != FontStyles.Bold) { sTitle.fontStyle = FontStyles.Bold; isSceneDirty = true; }
            if (sTitle.raycastTarget) { sTitle.raycastTarget = false; isSceneDirty = true; }
            SafeSetAnchoredPosition(sTitle.GetComponent<RectTransform>(), new Vector2(0f, 250f), ref isSceneDirty);

            TextMeshProUGUI sCoinsText = SetupText(sContentPanel, "Player_Coins_Text", "Coins: 100", "", ref isSceneDirty);
            if (!Mathf.Approximately(sCoinsText.fontSize, 20f)) { sCoinsText.fontSize = 20f; isSceneDirty = true; }
            if (sCoinsText.fontStyle != FontStyles.Bold) { sCoinsText.fontStyle = FontStyles.Bold; isSceneDirty = true; }
            if (sCoinsText.raycastTarget) { sCoinsText.raycastTarget = false; isSceneDirty = true; }
            SafeSetAnchoredPosition(sCoinsText.GetComponent<RectTransform>(), new Vector2(0f, 210f), ref isSceneDirty);

            // Setup Tab Buttons
            Button seedsTabBtn = SetupButton(sContentPanel, "Tab_Seeds", "Seeds", ref isSceneDirty);
            RectTransform seedsTabBtnRect = seedsTabBtn.GetComponent<RectTransform>();
            SafeSetAnchoredPosition(seedsTabBtnRect, new Vector2(-150f, 160f), ref isSceneDirty);
            SafeSetSizeDelta(seedsTabBtnRect, new Vector2(120f, 35f), ref isSceneDirty);

            Button stickersTabBtn = SetupButton(sContentPanel, "Tab_Stickers", "Stickers", ref isSceneDirty);
            RectTransform stickersTabBtnRect = stickersTabBtn.GetComponent<RectTransform>();
            SafeSetAnchoredPosition(stickersTabBtnRect, new Vector2(0f, 160f), ref isSceneDirty);
            SafeSetSizeDelta(stickersTabBtnRect, new Vector2(120f, 35f), ref isSceneDirty);

            Button cropsTabBtn = SetupButton(sContentPanel, "Tab_Crops", "Crops", ref isSceneDirty);
            RectTransform cropsTabBtnRect = cropsTabBtn.GetComponent<RectTransform>();
            SafeSetAnchoredPosition(cropsTabBtnRect, new Vector2(150f, 160f), ref isSceneDirty);
            SafeSetSizeDelta(cropsTabBtnRect, new Vector2(120f, 35f), ref isSceneDirty);

            // Grids Container
            RectTransform sGridsContainer = SetupPanel(sContentPanel, "Grids_Container", ref isSceneDirty);
            SafeSetAnchor(sGridsContainer, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.65f), ref isSceneDirty);
            SafeSetSizeDelta(sGridsContainer, Vector2.zero, ref isSceneDirty);
            SafeSetAnchoredPosition(sGridsContainer, Vector2.zero, ref isSceneDirty);
            Image sGridContImg = sGridsContainer.gameObject.GetComponent<Image>();
            if (sGridContImg != null)
            {
                DestroyImmediate(sGridContImg);
                isSceneDirty = true;
            }
            HorizontalLayoutGroup sGridsLayout = sGridsContainer.gameObject.GetComponent<HorizontalLayoutGroup>();
            if (sGridsLayout != null)
            {
                DestroyImmediate(sGridsLayout);
                isSceneDirty = true;
            }

            // Group 1: Seeds
            RectTransform seedsGroup = SetupPanel(sGridsContainer, "Seeds_Group", ref isSceneDirty);
            StretchToFill(seedsGroup, ref isSceneDirty);
            CanvasGroup seedsCg = seedsGroup.gameObject.GetComponent<CanvasGroup>();
            if (seedsCg == null)
            {
                seedsCg = seedsGroup.gameObject.AddComponent<CanvasGroup>();
                isSceneDirty = true;
            }

            VerticalLayoutGroup seedsLayout = seedsGroup.gameObject.GetComponent<VerticalLayoutGroup>();
            if (seedsLayout == null)
            {
                seedsLayout = seedsGroup.gameObject.AddComponent<VerticalLayoutGroup>();
                isSceneDirty = true;
            }
            if (!Mathf.Approximately(seedsLayout.spacing, 10f)) { seedsLayout.spacing = 10f; isSceneDirty = true; }
            TextMeshProUGUI seedsTitleText = SetupText(seedsGroup, "Seeds_Title", "Seeds (Buy)", "", ref isSceneDirty);
            if (!Mathf.Approximately(seedsTitleText.fontSize, 18f)) { seedsTitleText.fontSize = 18f; isSceneDirty = true; }
            if (seedsTitleText.fontStyle != FontStyles.Bold) { seedsTitleText.fontStyle = FontStyles.Bold; isSceneDirty = true; }
            RectTransform seedsGrid = SetupPanel(seedsGroup, "Seeds_Grid", ref isSceneDirty);
            GridLayoutGroup seedsGridGroup = seedsGrid.gameObject.GetComponent<GridLayoutGroup>();
            if (seedsGridGroup == null)
            {
                seedsGridGroup = seedsGrid.gameObject.AddComponent<GridLayoutGroup>();
                isSceneDirty = true;
            }
            Vector2 cellSize80 = new Vector2(200f, 80f);
            Vector2 spacing5 = new Vector2(5f, 5f);
            if (seedsGridGroup.cellSize != cellSize80) { seedsGridGroup.cellSize = cellSize80; isSceneDirty = true; }
            if (seedsGridGroup.spacing != spacing5) { seedsGridGroup.spacing = spacing5; isSceneDirty = true; }

            // Group 2: Stickers
            RectTransform stickersGroup = SetupPanel(sGridsContainer, "Stickers_Group", ref isSceneDirty);
            StretchToFill(stickersGroup, ref isSceneDirty);
            CanvasGroup stickersCg = stickersGroup.gameObject.GetComponent<CanvasGroup>();
            if (stickersCg == null)
            {
                stickersCg = stickersGroup.gameObject.AddComponent<CanvasGroup>();
                isSceneDirty = true;
            }

            VerticalLayoutGroup stickersLayout = stickersGroup.gameObject.GetComponent<VerticalLayoutGroup>();
            if (stickersLayout == null)
            {
                stickersLayout = stickersGroup.gameObject.AddComponent<VerticalLayoutGroup>();
                isSceneDirty = true;
            }
            if (!Mathf.Approximately(stickersLayout.spacing, 10f)) { stickersLayout.spacing = 10f; isSceneDirty = true; }
            TextMeshProUGUI stickersTitleText = SetupText(stickersGroup, "Stickers_Title", "Stickers (Buy)", "", ref isSceneDirty);
            if (!Mathf.Approximately(stickersTitleText.fontSize, 18f)) { stickersTitleText.fontSize = 18f; isSceneDirty = true; }
            if (stickersTitleText.fontStyle != FontStyles.Bold) { stickersTitleText.fontStyle = FontStyles.Bold; isSceneDirty = true; }
            RectTransform stickersGrid = SetupPanel(stickersGroup, "Stickers_Grid", ref isSceneDirty);
            GridLayoutGroup stickersGridGroup = stickersGrid.gameObject.GetComponent<GridLayoutGroup>();
            if (stickersGridGroup == null)
            {
                stickersGridGroup = stickersGrid.gameObject.AddComponent<GridLayoutGroup>();
                isSceneDirty = true;
            }
            if (stickersGridGroup.cellSize != cellSize80) { stickersGridGroup.cellSize = cellSize80; isSceneDirty = true; }
            if (stickersGridGroup.spacing != spacing5) { stickersGridGroup.spacing = spacing5; isSceneDirty = true; }

            // Group 3: Crops
            RectTransform cropsGroup = SetupPanel(sGridsContainer, "Crops_Group", ref isSceneDirty);
            StretchToFill(cropsGroup, ref isSceneDirty);
            CanvasGroup cropsCg = cropsGroup.gameObject.GetComponent<CanvasGroup>();
            if (cropsCg == null)
            {
                cropsCg = cropsGroup.gameObject.AddComponent<CanvasGroup>();
                isSceneDirty = true;
            }

            VerticalLayoutGroup cropsLayout = cropsGroup.gameObject.GetComponent<VerticalLayoutGroup>();
            if (cropsLayout == null)
            {
                cropsLayout = cropsGroup.gameObject.AddComponent<VerticalLayoutGroup>();
                isSceneDirty = true;
            }
            if (!Mathf.Approximately(cropsLayout.spacing, 10f)) { cropsLayout.spacing = 10f; isSceneDirty = true; }
            TextMeshProUGUI cropsTitleText = SetupText(cropsGroup, "Crops_Title", "Crops (Sell)", "", ref isSceneDirty);
            if (!Mathf.Approximately(cropsTitleText.fontSize, 18f)) { cropsTitleText.fontSize = 18f; isSceneDirty = true; }
            if (cropsTitleText.fontStyle != FontStyles.Bold) { cropsTitleText.fontStyle = FontStyles.Bold; isSceneDirty = true; }
            RectTransform cropsGrid = SetupPanel(cropsGroup, "Crops_Grid", ref isSceneDirty);
            GridLayoutGroup cropsGridGroup = cropsGrid.gameObject.GetComponent<GridLayoutGroup>();
            if (cropsGridGroup == null)
            {
                cropsGridGroup = cropsGrid.gameObject.AddComponent<GridLayoutGroup>();
                isSceneDirty = true;
            }
            if (cropsGridGroup.cellSize != cellSize80) { cropsGridGroup.cellSize = cellSize80; isSceneDirty = true; }
            if (cropsGridGroup.spacing != spacing5) { cropsGridGroup.spacing = spacing5; isSceneDirty = true; }

            // Shop Item Template under Prefabs_Holder
            RectTransform shopItemTemplate = SetupPanel(prefabsHolder, "Shop_Item_Template", ref isSceneDirty);
            SafeSetSizeDelta(shopItemTemplate, cellSize80, ref isSceneDirty);
            if (shopItemTemplate.gameObject.activeSelf)
            {
                shopItemTemplate.gameObject.SetActive(false);
                isSceneDirty = true;
            }
            ShopItemWidget widgetComponent = shopItemTemplate.gameObject.GetComponent<ShopItemWidget>();
            if (widgetComponent == null)
            {
                widgetComponent = shopItemTemplate.gameObject.AddComponent<ShopItemWidget>();
                isSceneDirty = true;
            }

            Image itemIcon = SetupImage(shopItemTemplate, "Item_Icon", ref isSceneDirty);
            SafeSetSizeDelta(itemIcon.GetComponent<RectTransform>(), new Vector2(50f, 50f), ref isSceneDirty);
            TextMeshProUGUI itemName = SetupText(shopItemTemplate, "Item_Name", "Product Name", "", ref isSceneDirty);
            if (!Mathf.Approximately(itemName.fontSize, 14f)) { itemName.fontSize = 14f; isSceneDirty = true; }
            TextMeshProUGUI itemPrice = SetupText(shopItemTemplate, "Item_Price", "50 Coins", "", ref isSceneDirty);
            if (!Mathf.Approximately(itemPrice.fontSize, 12f)) { itemPrice.fontSize = 12f; isSceneDirty = true; }
            Button itemActionBtn = SetupButton(shopItemTemplate, "Action_Button", "Buy", ref isSceneDirty);
            SafeSetSizeDelta(itemActionBtn.GetComponent<RectTransform>(), new Vector2(60f, 30f), ref isSceneDirty);
            TextMeshProUGUI itemActionTxt = itemActionBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (!Mathf.Approximately(itemActionTxt.fontSize, 12f)) { itemActionTxt.fontSize = 12f; isSceneDirty = true; }

            TextMeshProUGUI itemDisabledReason = SetupText(shopItemTemplate, "Disabled_Reason", "", "", ref isSceneDirty);
            if (!Mathf.Approximately(itemDisabledReason.fontSize, 10f)) { itemDisabledReason.fontSize = 10f; isSceneDirty = true; }
            Color reasonColor = new Color(0.9f, 0.3f, 0.3f, 1f);
            if (itemDisabledReason.color != reasonColor) { itemDisabledReason.color = reasonColor; isSceneDirty = true; }
            if (itemDisabledReason.alignment != TextAlignmentOptions.Center) { itemDisabledReason.alignment = TextAlignmentOptions.Center; isSceneDirty = true; }

            // Wire ShopItemWidget
            SerializedObject soItem = new SerializedObject(widgetComponent);
            bool itemDirty = false;
            SafeSetObjectReference(soItem.FindProperty("_itemIcon"), itemIcon, ref itemDirty);
            SafeSetObjectReference(soItem.FindProperty("_itemNameText"), itemName, ref itemDirty);
            SafeSetObjectReference(soItem.FindProperty("_priceText"), itemPrice, ref itemDirty);
            SafeSetObjectReference(soItem.FindProperty("_actionButton"), itemActionBtn, ref itemDirty);
            SafeSetObjectReference(soItem.FindProperty("_actionButtonText"), itemActionTxt, ref itemDirty);
            SafeSetObjectReference(soItem.FindProperty("_disabledReasonText"), itemDisabledReason, ref itemDirty);
            if (itemDirty)
            {
                soItem.ApplyModifiedProperties();
                isSceneDirty = true;
            }

            // Wire ShopPopup
            SerializedObject soSPopup = new SerializedObject(shopPopup);
            bool sPopupDirty = false;
            SafeSetObjectReference(soSPopup.FindProperty("_contentPanel"), sContentPanel, ref sPopupDirty);
            SafeSetObjectReference(soSPopup.FindProperty("_backgroundDim"), sDimGroup, ref sPopupDirty);
            SafeSetObjectReference(soSPopup.FindProperty("_closeButton"), sCloseBtn, ref sPopupDirty);
            SafeSetObjectReference(soSPopup.FindProperty("_playerCoinsText"), sCoinsText, ref sPopupDirty);
            SafeSetObjectReference(soSPopup.FindProperty("_itemPrefabTemplate"), widgetComponent, ref sPopupDirty);
            SafeSetObjectReference(soSPopup.FindProperty("_seedsContainer"), seedsGrid, ref sPopupDirty);
            SafeSetObjectReference(soSPopup.FindProperty("_stickersContainer"), stickersGrid, ref sPopupDirty);
            SafeSetObjectReference(soSPopup.FindProperty("_cropsContainer"), cropsGrid, ref sPopupDirty);
            SafeSetObjectReference(soSPopup.FindProperty("_seedsTabButton"), seedsTabBtn, ref sPopupDirty);
            SafeSetObjectReference(soSPopup.FindProperty("_stickersTabButton"), stickersTabBtn, ref sPopupDirty);
            SafeSetObjectReference(soSPopup.FindProperty("_cropsTabButton"), cropsTabBtn, ref sPopupDirty);
            SafeSetObjectReference(soSPopup.FindProperty("_seedsGroup"), seedsGroup, ref sPopupDirty);
            SafeSetObjectReference(soSPopup.FindProperty("_stickersGroup"), stickersGroup, ref sPopupDirty);
            SafeSetObjectReference(soSPopup.FindProperty("_cropsGroup"), cropsGroup, ref sPopupDirty);
            if (sPopupDirty)
            {
                soSPopup.ApplyModifiedProperties();
                isSceneDirty = true;
            }

            // B.5. Setup Diary Input Popup
            RectTransform diaryPopupPanel = SetupPanel(canvas.transform, "Diary_Input_Popup", ref isSceneDirty);
            StretchToFill(diaryPopupPanel, ref isSceneDirty);
            if (diaryPopupPanel.gameObject.activeSelf)
            {
                diaryPopupPanel.gameObject.SetActive(false);
                isSceneDirty = true;
            }

            CozyDiaryInputPopup diaryInputPopup = diaryPopupPanel.gameObject.GetComponent<CozyDiaryInputPopup>();
            if (diaryInputPopup == null)
            {
                diaryInputPopup = diaryPopupPanel.gameObject.AddComponent<CozyDiaryInputPopup>();
                isSceneDirty = true;
            }

            RectTransform diaryDimPanel = SetupPanel(diaryPopupPanel, "Background_Dim", ref isSceneDirty);
            StretchToFill(diaryDimPanel, ref isSceneDirty);
            CanvasGroup diaryDimGroup = diaryDimPanel.gameObject.GetComponent<CanvasGroup>();
            if (diaryDimGroup == null)
            {
                diaryDimGroup = diaryDimPanel.gameObject.AddComponent<CanvasGroup>();
                isSceneDirty = true;
            }
            if (!Mathf.Approximately(diaryDimGroup.alpha, 0f))
            {
                diaryDimGroup.alpha = 0f;
                isSceneDirty = true;
            }
            Image diaryDimImg = diaryDimPanel.gameObject.GetComponent<Image>();
            if (diaryDimImg == null)
            {
                diaryDimImg = diaryDimPanel.gameObject.AddComponent<Image>();
                isSceneDirty = true;
            }
            Color diaryDimColor = new Color(0f, 0f, 0f, 0.55f);
            if (diaryDimImg.color != diaryDimColor)
            {
                diaryDimImg.color = diaryDimColor;
                isSceneDirty = true;
            }

            RectTransform diaryContentPanel = SetupPanel(diaryPopupPanel, "Content_Panel", ref isSceneDirty);
            SafeSetAnchor(diaryContentPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), ref isSceneDirty);
            SafeSetPivot(diaryContentPanel, new Vector2(0.5f, 0.5f), ref isSceneDirty);
            SafeSetSizeDelta(diaryContentPanel, new Vector2(420f, 260f), ref isSceneDirty);
            SafeSetAnchoredPosition(diaryContentPanel, Vector2.zero, ref isSceneDirty);
            Image diaryContentImg = diaryContentPanel.gameObject.GetComponent<Image>();
            if (diaryContentImg == null)
            {
                diaryContentImg = diaryContentPanel.gameObject.AddComponent<Image>();
                isSceneDirty = true;
            }
            Color diaryContentColor = new Color(0.98f, 0.86f, 0.35f, 0.98f);
            if (diaryContentImg.color != diaryContentColor)
            {
                diaryContentImg.color = diaryContentColor;
                isSceneDirty = true;
            }

            TextMeshProUGUI diaryTitle = SetupText(diaryContentPanel, "Title_Text", "DIARY NOTE", "Header_Text", ref isSceneDirty);
            RectTransform diaryTitleRect = diaryTitle.GetComponent<RectTransform>();
            SafeSetAnchor(diaryTitleRect, new Vector2(0.1f, 0.78f), new Vector2(0.9f, 0.95f), ref isSceneDirty);
            SafeSetSizeDelta(diaryTitleRect, Vector2.zero, ref isSceneDirty);
            SafeSetAnchoredPosition(diaryTitleRect, Vector2.zero, ref isSceneDirty);
            if (!Mathf.Approximately(diaryTitle.fontSize, 22f)) { diaryTitle.fontSize = 22f; isSceneDirty = true; }

            RectTransform diaryInputRect = SetupPanel(diaryContentPanel, "Input_Field", ref isSceneDirty);
            SafeSetAnchor(diaryInputRect, new Vector2(0.1f, 0.36f), new Vector2(0.9f, 0.72f), ref isSceneDirty);
            SafeSetSizeDelta(diaryInputRect, Vector2.zero, ref isSceneDirty);
            SafeSetAnchoredPosition(diaryInputRect, Vector2.zero, ref isSceneDirty);
            Image diaryInputImg = diaryInputRect.gameObject.GetComponent<Image>();
            if (diaryInputImg == null)
            {
                diaryInputImg = diaryInputRect.gameObject.AddComponent<Image>();
                isSceneDirty = true;
            }
            Color diaryInputColor = new Color(1f, 0.96f, 0.72f, 1f);
            if (diaryInputImg.color != diaryInputColor)
            {
                diaryInputImg.color = diaryInputColor;
                isSceneDirty = true;
            }
            TMP_InputField diaryInput = diaryInputRect.gameObject.GetComponent<TMP_InputField>();
            if (diaryInput == null)
            {
                diaryInput = diaryInputRect.gameObject.AddComponent<TMP_InputField>();
                isSceneDirty = true;
            }
            if (diaryInput.characterLimit != 60)
            {
                diaryInput.characterLimit = 60;
                isSceneDirty = true;
            }

            TextMeshProUGUI diaryInputText = SetupText(diaryInputRect, "Text", "", "", ref isSceneDirty);
            RectTransform diaryInputTextRect = diaryInputText.GetComponent<RectTransform>();
            SafeSetAnchor(diaryInputTextRect, Vector2.zero, Vector2.one, ref isSceneDirty);
            SafeSetSizeDelta(diaryInputTextRect, new Vector2(-20f, -16f), ref isSceneDirty);
            SafeSetAnchoredPosition(diaryInputTextRect, Vector2.zero, ref isSceneDirty);
            if (diaryInputText.alignment != TextAlignmentOptions.TopLeft) { diaryInputText.alignment = TextAlignmentOptions.TopLeft; isSceneDirty = true; }
            if (!Mathf.Approximately(diaryInputText.fontSize, 18f)) { diaryInputText.fontSize = 18f; isSceneDirty = true; }
            if (diaryInputText.color != Color.black) { diaryInputText.color = Color.black; isSceneDirty = true; }

            TextMeshProUGUI diaryPlaceholder = SetupText(diaryInputRect, "Placeholder", "Write a memory...", "", ref isSceneDirty);
            RectTransform diaryPlaceholderRect = diaryPlaceholder.GetComponent<RectTransform>();
            SafeSetAnchor(diaryPlaceholderRect, Vector2.zero, Vector2.one, ref isSceneDirty);
            SafeSetSizeDelta(diaryPlaceholderRect, new Vector2(-20f, -16f), ref isSceneDirty);
            SafeSetAnchoredPosition(diaryPlaceholderRect, Vector2.zero, ref isSceneDirty);
            if (diaryPlaceholder.alignment != TextAlignmentOptions.TopLeft) { diaryPlaceholder.alignment = TextAlignmentOptions.TopLeft; isSceneDirty = true; }
            if (!Mathf.Approximately(diaryPlaceholder.fontSize, 18f)) { diaryPlaceholder.fontSize = 18f; isSceneDirty = true; }
            Color placeholderColor = new Color(0f, 0f, 0f, 0.45f);
            if (diaryPlaceholder.color != placeholderColor) { diaryPlaceholder.color = placeholderColor; isSceneDirty = true; }

            if (diaryInput.textComponent != diaryInputText)
            {
                diaryInput.textComponent = diaryInputText;
                isSceneDirty = true;
            }
            if (diaryInput.placeholder != diaryPlaceholder)
            {
                diaryInput.placeholder = diaryPlaceholder;
                isSceneDirty = true;
            }

            Button diaryConfirmBtn = SetupButton(diaryContentPanel, "Confirm_Button", "Add", ref isSceneDirty);
            RectTransform diaryConfirmRect = diaryConfirmBtn.GetComponent<RectTransform>();
            SafeSetAnchor(diaryConfirmRect, new Vector2(0.58f, 0.08f), new Vector2(0.88f, 0.25f), ref isSceneDirty);
            SafeSetSizeDelta(diaryConfirmRect, Vector2.zero, ref isSceneDirty);
            SafeSetAnchoredPosition(diaryConfirmRect, Vector2.zero, ref isSceneDirty);
            TextMeshProUGUI diaryConfirmText = diaryConfirmBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (!Mathf.Approximately(diaryConfirmText.fontSize, 18f)) { diaryConfirmText.fontSize = 18f; isSceneDirty = true; }

            Button diaryCancelBtn = SetupButton(diaryContentPanel, "Cancel_Button", "Cancel", ref isSceneDirty);
            RectTransform diaryCancelRect = diaryCancelBtn.GetComponent<RectTransform>();
            SafeSetAnchor(diaryCancelRect, new Vector2(0.12f, 0.08f), new Vector2(0.42f, 0.25f), ref isSceneDirty);
            SafeSetSizeDelta(diaryCancelRect, Vector2.zero, ref isSceneDirty);
            SafeSetAnchoredPosition(diaryCancelRect, Vector2.zero, ref isSceneDirty);
            TextMeshProUGUI diaryCancelText = diaryCancelBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (!Mathf.Approximately(diaryCancelText.fontSize, 18f)) { diaryCancelText.fontSize = 18f; isSceneDirty = true; }

            SerializedObject soDiaryPopup = new SerializedObject(diaryInputPopup);
            bool diaryPopupDirty = false;
            SafeSetObjectReference(soDiaryPopup.FindProperty("_contentPanel"), diaryContentPanel, ref diaryPopupDirty);
            SafeSetObjectReference(soDiaryPopup.FindProperty("_backgroundDim"), diaryDimGroup, ref diaryPopupDirty);
            SafeSetObjectReference(soDiaryPopup.FindProperty("_closeButton"), diaryCancelBtn, ref diaryPopupDirty);
            SafeSetObjectReference(soDiaryPopup.FindProperty("_inputField"), diaryInput, ref diaryPopupDirty);
            SafeSetObjectReference(soDiaryPopup.FindProperty("_confirmButton"), diaryConfirmBtn, ref diaryPopupDirty);
            SafeSetObjectReference(soDiaryPopup.FindProperty("_cancelButton"), diaryCancelBtn, ref diaryPopupDirty);
            if (diaryPopupDirty)
            {
                soDiaryPopup.ApplyModifiedProperties();
                isSceneDirty = true;
            }

            // C. Setup Dialogue Popup
            RectTransform dialoguePopupPanel = SetupPanel(canvas.transform, "Dialogue_Popup", ref isSceneDirty);
            StretchToFill(dialoguePopupPanel, ref isSceneDirty);

            // Parent MUST be active for Construct to run, but we can set its activeSelf to true.
            if (!dialoguePopupPanel.gameObject.activeSelf)
            {
                dialoguePopupPanel.gameObject.SetActive(true);
                isSceneDirty = true;
            }

            CozyDialoguePopup dialoguePopup = dialoguePopupPanel.gameObject.GetComponent<CozyDialoguePopup>();
            if (dialoguePopup == null)
            {
                dialoguePopup = dialoguePopupPanel.gameObject.AddComponent<CozyDialoguePopup>();
                isSceneDirty = true;
            }

            // Dialogue popup Content Panel (inner panel that actually gets toggled)
            RectTransform dContentPanel = SetupPanel(dialoguePopupPanel, "Content_Panel", ref isSceneDirty);
            SafeSetAnchor(dContentPanel, new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.15f), ref isSceneDirty);
            SafeSetPivot(dContentPanel, new Vector2(0.5f, 0.5f), ref isSceneDirty);
            SafeSetSizeDelta(dContentPanel, new Vector2(800f, 180f), ref isSceneDirty);
            SafeSetAnchoredPosition(dContentPanel, Vector2.zero, ref isSceneDirty);
            Image dContentImg = dContentPanel.gameObject.GetComponent<Image>();
            if (dContentImg == null)
            {
                dContentImg = dContentPanel.gameObject.AddComponent<Image>();
                isSceneDirty = true;
            }
            if (dContentImg.color != contentBg)
            {
                dContentImg.color = contentBg;
                isSceneDirty = true;
            }
            // Ensure content panel is inactive by default (the script will activate it)
            if (dContentPanel.gameObject.activeSelf)
            {
                dContentPanel.gameObject.SetActive(false);
                isSceneDirty = true;
            }

            // Portrait inside Content Panel
            Image dPortrait = SetupImage(dContentPanel, "Portrait", ref isSceneDirty);
            RectTransform dPortraitRect = dPortrait.GetComponent<RectTransform>();
            SafeSetAnchor(dPortraitRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), ref isSceneDirty);
            SafeSetPivot(dPortraitRect, new Vector2(0f, 0.5f), ref isSceneDirty);
            SafeSetSizeDelta(dPortraitRect, new Vector2(140f, 140f), ref isSceneDirty);
            SafeSetAnchoredPosition(dPortraitRect, new Vector2(20f, 0f), ref isSceneDirty);

            // Name Text inside Content Panel
            TextMeshProUGUI dNameText = SetupText(dContentPanel, "Name_Text", "NPC Name", "Header_Text", ref isSceneDirty);
            RectTransform dNameRect = dNameText.GetComponent<RectTransform>();
            SafeSetAnchor(dNameRect, new Vector2(0f, 1f), new Vector2(1f, 1f), ref isSceneDirty);
            SafeSetPivot(dNameRect, new Vector2(0.5f, 1f), ref isSceneDirty);
            SafeSetSizeDelta(dNameRect, new Vector2(-220f, 40f), ref isSceneDirty);
            SafeSetAnchoredPosition(dNameRect, new Vector2(90f, -15f), ref isSceneDirty);
            if (dNameText.alignment != TextAlignmentOptions.Left)
            {
                dNameText.alignment = TextAlignmentOptions.Left;
                isSceneDirty = true;
            }
            if (dNameText.fontStyle != FontStyles.Bold)
            {
                dNameText.fontStyle = FontStyles.Bold;
                isSceneDirty = true;
            }
            if (!Mathf.Approximately(dNameText.fontSize, 20f))
            {
                dNameText.fontSize = 20f;
                isSceneDirty = true;
                UnityEditor.EditorUtility.SetDirty(dNameText);
            }

            // Dialogue Text inside Content Panel
            TextMeshProUGUI dDialogueText = SetupText(dContentPanel, "Dialogue_Text", "... Dialogue text running ...", "", ref isSceneDirty);
            RectTransform dDialogueRect = dDialogueText.GetComponent<RectTransform>();
            SafeSetAnchor(dDialogueRect, new Vector2(0f, 0f), new Vector2(1f, 1f), ref isSceneDirty);
            SafeSetPivot(dDialogueRect, new Vector2(0.5f, 0.5f), ref isSceneDirty);
            // Height matches bottom area under Name_Text. We offset top by -60, left/right margins
            SafeSetSizeDelta(dDialogueRect, new Vector2(-220f, -90f), ref isSceneDirty);
            SafeSetAnchoredPosition(dDialogueRect, new Vector2(90f, -30f), ref isSceneDirty);
            if (dDialogueText.alignment != TextAlignmentOptions.TopLeft)
            {
                dDialogueText.alignment = TextAlignmentOptions.TopLeft;
                isSceneDirty = true;
            }
            if (!Mathf.Approximately(dDialogueText.fontSize, 18f))
            {
                dDialogueText.fontSize = 18f;
                isSceneDirty = true;
                UnityEditor.EditorUtility.SetDirty(dDialogueText);
            }

            // Next / Skip Button inside Content Panel
            Button dNextBtn = SetupButton(dContentPanel, "Next_Button", "Next", ref isSceneDirty);
            RectTransform dNextRect = dNextBtn.GetComponent<RectTransform>();
            SafeSetAnchor(dNextRect, new Vector2(1f, 0f), new Vector2(1f, 0f), ref isSceneDirty);
            SafeSetPivot(dNextRect, new Vector2(1f, 0f), ref isSceneDirty);
            SafeSetSizeDelta(dNextRect, new Vector2(100f, 40f), ref isSceneDirty);
            SafeSetAnchoredPosition(dNextRect, new Vector2(-20f, 15f), ref isSceneDirty);
            TextMeshProUGUI dNextTxt = dNextBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (!Mathf.Approximately(dNextTxt.fontSize, 16f)) { dNextTxt.fontSize = 16f; isSceneDirty = true; }

            // Wire CozyDialoguePopup
            SerializedObject soDPopup = new SerializedObject(dialoguePopup);
            bool dPopupDirty = false;
            SafeSetObjectReference(soDPopup.FindProperty("_contentPanel"), dContentPanel, ref dPopupDirty);
            SafeSetObjectReference(soDPopup.FindProperty("_portrait"), dPortrait, ref dPopupDirty);
            SafeSetObjectReference(soDPopup.FindProperty("_nameText"), dNameText, ref dPopupDirty);
            SafeSetObjectReference(soDPopup.FindProperty("_dialogueText"), dDialogueText, ref dPopupDirty);
            SafeSetObjectReference(soDPopup.FindProperty("_nextButton"), dNextBtn, ref dPopupDirty);

            SerializedProperty dialoguesProp = soDPopup.FindProperty("_questDialogues");
            if (dialoguesProp != null)
            {
                dialoguesProp.ClearArray();
                int index = 0;

                // Add mapping for Quest 2
                dialoguesProp.InsertArrayElementAtIndex(index);
                SerializedProperty element2 = dialoguesProp.GetArrayElementAtIndex(index);
                element2.FindPropertyRelative("QuestId").intValue = 2;
                element2.FindPropertyRelative("NpcName").stringValue = "Bà Ngoại";
                element2.FindPropertyRelative("DialogueText").stringValue = "Con yêu, cây mía ngọt này chính là hương vị mùa hè ở quê mình đấy. Ngày xưa mỗi lần trời nắng nóng, bà lại ép nước mía cho mẹ con uống.";
                element2.FindPropertyRelative("Portrait").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/CozyLifeSim/Textures/Heritage/NPC_Portrait_Grandma.png");
                index++;

                // Add mapping for Quest 3
                dialoguesProp.InsertArrayElementAtIndex(index);
                SerializedProperty element3 = dialoguesProp.GetArrayElementAtIndex(index);
                element3.FindPropertyRelative("QuestId").intValue = 3;
                element3.FindPropertyRelative("NpcName").stringValue = "Bà Ngoại";
                element3.FindPropertyRelative("DialogueText").stringValue = "Hạt gạo vàng từ bông lúa nước này làm nên bánh chưng, bánh dày thơm dẻo mỗi dịp Tết. Giữ lấy hạt gạo là giữ lấy hồn quê hương, con nhé.";
                element3.FindPropertyRelative("Portrait").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/CozyLifeSim/Textures/Heritage/NPC_Portrait_Grandma.png");
                index++;

                // Add mapping for Quest 4
                dialoguesProp.InsertArrayElementAtIndex(index);
                SerializedProperty element4 = dialoguesProp.GetArrayElementAtIndex(index);
                element4.FindPropertyRelative("QuestId").intValue = 4;
                element4.FindPropertyRelative("NpcName").stringValue = "Bà Ngoại";
                element4.FindPropertyRelative("DialogueText").stringValue = "Chú mèo tam thể này ngoan lắm. Động vật ở quê mình luôn hiền lành và ấm áp như thế, hãy luôn yêu thương chúng nhé con.";
                element4.FindPropertyRelative("Portrait").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/CozyLifeSim/Textures/Heritage/NPC_Portrait_Grandma.png");
                index++;

                dPopupDirty = true;
            }

            if (dPopupDirty)
            {
                soDPopup.ApplyModifiedProperties();
                isSceneDirty = true;
            }

            // Wire CozyDialoguePopup to GameLifetimeScope
            SerializedObject soScopeUpdateDialogue = new SerializedObject(lifetimeScope);
            bool scopeUpdateDialogueDirty = false;
            SafeSetObjectReference(soScopeUpdateDialogue.FindProperty("_dialoguePopup"), dialoguePopup, ref scopeUpdateDialogueDirty);
            if (scopeUpdateDialogueDirty)
            {
                soScopeUpdateDialogue.ApplyModifiedProperties();
                isSceneDirty = true;
            }

            // D. Setup Popup_Root under canvas.transform
            RectTransform popupRoot = SetupPanel(canvas.transform, "Popup_Root", ref isSceneDirty);
            StretchToFill(popupRoot, ref isSceneDirty);

            ConfigureStartupLoadingOverlay(popupRoot, ref isSceneDirty);

            // E. Setup Feedback Toast under Popup_Root
            RectTransform feedbackToastPanel = SetupPanel(popupRoot, "FeedbackToast", ref isSceneDirty);
            StretchToFill(feedbackToastPanel, ref isSceneDirty);

            if (!feedbackToastPanel.gameObject.activeSelf)
            {
                feedbackToastPanel.gameObject.SetActive(true);
                isSceneDirty = true;
            }

            CozyFeedbackToast feedbackToast = feedbackToastPanel.gameObject.GetComponent<CozyFeedbackToast>();
            if (feedbackToast == null)
            {
                feedbackToast = feedbackToastPanel.gameObject.AddComponent<CozyFeedbackToast>();
                isSceneDirty = true;
            }

            RectTransform ftContentPanel = SetupPanel(feedbackToastPanel, "Content_Panel", ref isSceneDirty);
            SafeSetAnchor(ftContentPanel, new Vector2(0.5f, 0.18f), new Vector2(0.5f, 0.18f), ref isSceneDirty);
            SafeSetPivot(ftContentPanel, new Vector2(0.5f, 0.5f), ref isSceneDirty);
            SafeSetSizeDelta(ftContentPanel, new Vector2(400f, 60f), ref isSceneDirty);
            SafeSetAnchoredPosition(ftContentPanel, Vector2.zero, ref isSceneDirty);

            Image ftBgImage = ftContentPanel.gameObject.GetComponent<Image>();
            if (ftBgImage == null)
            {
                ftBgImage = ftContentPanel.gameObject.AddComponent<Image>();
                isSceneDirty = true;
            }
            Color toastBgColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            if (ftBgImage.color != toastBgColor)
            {
                ftBgImage.color = toastBgColor;
                isSceneDirty = true;
            }

            CanvasGroup ftCanvasGroup = ftContentPanel.gameObject.GetComponent<CanvasGroup>();
            if (ftCanvasGroup == null)
            {
                ftCanvasGroup = ftContentPanel.gameObject.AddComponent<CanvasGroup>();
                isSceneDirty = true;
            }
            if (ftCanvasGroup.blocksRaycasts)
            {
                ftCanvasGroup.blocksRaycasts = false;
                isSceneDirty = true;
            }
            if (ftCanvasGroup.interactable)
            {
                ftCanvasGroup.interactable = false;
                isSceneDirty = true;
            }

            TextMeshProUGUI ftText = SetupText(ftContentPanel, "Message_Text", "Toast Message", "", ref isSceneDirty);
            RectTransform ftTextRect = ftText.GetComponent<RectTransform>();
            SafeSetAnchor(ftTextRect, Vector2.zero, Vector2.one, ref isSceneDirty);
            SafeSetSizeDelta(ftTextRect, new Vector2(-20f, -10f), ref isSceneDirty);
            SafeSetAnchoredPosition(ftTextRect, Vector2.zero, ref isSceneDirty);
            if (ftText.alignment != TextAlignmentOptions.Center)
            {
                ftText.alignment = TextAlignmentOptions.Center;
                isSceneDirty = true;
            }
            if (!Mathf.Approximately(ftText.fontSize, 16f))
            {
                ftText.fontSize = 16f;
                isSceneDirty = true;
            }
            if (ftText.color != Color.white)
            {
                ftText.color = Color.white;
                isSceneDirty = true;
            }

            if (ftContentPanel.gameObject.activeSelf)
            {
                ftContentPanel.gameObject.SetActive(false);
                isSceneDirty = true;
            }

            // Wire CozyFeedbackToast
            SerializedObject soToast = new SerializedObject(feedbackToast);
            bool toastDirty = false;
            SafeSetObjectReference(soToast.FindProperty("_canvasGroup"), ftCanvasGroup, ref toastDirty);
            SafeSetObjectReference(soToast.FindProperty("_contentPanel"), ftContentPanel, ref toastDirty);
            SafeSetObjectReference(soToast.FindProperty("_messageText"), ftText, ref toastDirty);
            if (toastDirty)
            {
                soToast.ApplyModifiedProperties();
                isSceneDirty = true;
            }

            // Wire CozyFeedbackToast to GameLifetimeScope
            SerializedObject soScopeUpdateToast = new SerializedObject(lifetimeScope);
            bool scopeUpdateToastDirty = false;
            SafeSetObjectReference(soScopeUpdateToast.FindProperty("_feedbackToast"), feedbackToast, ref scopeUpdateToastDirty);
            if (scopeUpdateToastDirty)
            {
                soScopeUpdateToast.ApplyModifiedProperties();
                isSceneDirty = true;
            }


            // Wire CozySidebar
            CozySidebar sidebar = sidebarPanel.gameObject.GetComponent<CozySidebar>();
            if (sidebar == null)
            {
                sidebar = sidebarPanel.gameObject.AddComponent<CozySidebar>();
                isSceneDirty = true;
            }
            SerializedObject soSidebar = new SerializedObject(sidebar);
            bool sidebarDirty = false;
            SafeSetObjectReference(soSidebar.FindProperty("_questPopup"), questPopup, ref sidebarDirty);
            SafeSetObjectReference(soSidebar.FindProperty("_shopPopup"), shopPopup, ref sidebarDirty);
            SafeSetObjectReference(soSidebar.FindProperty("_questButton"), questBtn, ref sidebarDirty);
            SafeSetObjectReference(soSidebar.FindProperty("_shopButton"), shopBtn, ref sidebarDirty);
            if (sidebarDirty)
            {
                soSidebar.ApplyModifiedProperties();
                isSceneDirty = true;
            }

            // Setup in-world Interactive Objects (Quest Board & Shop Stall)
            GameObject questBoardGo = GameObject.Find("Quest_Board");
            if (questBoardGo == null)
            {
                questBoardGo = new GameObject("Quest_Board");
                isSceneDirty = true;
            }
            Vector3 targetQBoardPos = new Vector3(-6f, 0f, 0f);
            if (Vector3.Distance(questBoardGo.transform.position, targetQBoardPos) > 0.001f)
            {
                questBoardGo.transform.position = targetQBoardPos;
                isSceneDirty = true;
            }
            ConfigureWorldClickVisual(questBoardGo, heartSprite != null ? heartSprite : defaultSprite, new Color(1f, 0.82f, 0.2f, 1f), new Vector3(1.2f, 1.2f, 1f), 1, ref isSceneDirty);
            BoxCollider2D qCollider = questBoardGo.GetComponent<BoxCollider2D>();
            if (qCollider == null)
            {
                qCollider = questBoardGo.AddComponent<BoxCollider2D>();
                isSceneDirty = true;
            }
            Vector2 expectedQCollSize = new Vector2(2f, 2.5f);
            if (qCollider.size != expectedQCollSize)
            {
                qCollider.size = expectedQCollSize;
                isSceneDirty = true;
            }
            CozyInteractiveObject qInteractive = questBoardGo.GetComponent<CozyInteractiveObject>();
            if (qInteractive == null)
            {
                qInteractive = questBoardGo.AddComponent<CozyInteractiveObject>();
                isSceneDirty = true;
            }
            SerializedObject soQInteractive = new SerializedObject(qInteractive);
            bool qInterDirty = false;
            SafeSetObjectReference(soQInteractive.FindProperty("_targetPopup"), questPopup, ref qInterDirty);
            if (qInterDirty)
            {
                soQInteractive.ApplyModifiedProperties();
                isSceneDirty = true;
            }

            GameObject shopStallGo = GameObject.Find("Shop_Stall");
            if (shopStallGo == null)
            {
                shopStallGo = new GameObject("Shop_Stall");
                isSceneDirty = true;
            }
            Vector3 targetSStallPos = new Vector3(6f, 0f, 0f);
            if (Vector3.Distance(shopStallGo.transform.position, targetSStallPos) > 0.001f)
            {
                shopStallGo.transform.position = targetSStallPos;
                isSceneDirty = true;
            }
            ConfigureWorldClickVisual(shopStallGo, seedSprite != null ? seedSprite : defaultSprite, new Color(0.45f, 1f, 0.58f, 1f), new Vector3(1.3f, 1.3f, 1f), 1, ref isSceneDirty);
            BoxCollider2D sCollider = shopStallGo.GetComponent<BoxCollider2D>();
            if (sCollider == null)
            {
                sCollider = shopStallGo.AddComponent<BoxCollider2D>();
                isSceneDirty = true;
            }
            Vector2 expectedSCollSize = new Vector2(2.5f, 2.5f);
            if (sCollider.size != expectedSCollSize)
            {
                sCollider.size = expectedSCollSize;
                isSceneDirty = true;
            }
            CozyInteractiveObject sInteractive = shopStallGo.GetComponent<CozyInteractiveObject>();
            if (sInteractive == null)
            {
                sInteractive = shopStallGo.AddComponent<CozyInteractiveObject>();
                isSceneDirty = true;
            }
            SerializedObject soSInteractive = new SerializedObject(sInteractive);
            bool sInterDirty = false;
            SafeSetObjectReference(soSInteractive.FindProperty("_targetPopup"), shopPopup, ref sInterDirty);
            if (sInterDirty)
            {
                soSInteractive.ApplyModifiedProperties();
                isSceneDirty = true;
            }

            // Setup NPC Grandma
            GameObject npcGrandmaGo = GameObject.Find("NPC_BaNgoai");
            if (npcGrandmaGo == null)
            {
                npcGrandmaGo = new GameObject("NPC_BaNgoai");
                isSceneDirty = true;
            }
            Vector3 targetGrandmaPos = new Vector3(0f, -2.5f, 0f);
            if (Vector3.Distance(npcGrandmaGo.transform.position, targetGrandmaPos) > 0.001f)
            {
                npcGrandmaGo.transform.position = targetGrandmaPos;
                isSceneDirty = true;
            }

            Sprite grandmaWorldSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/CozyLifeSim/Textures/Heritage/NPC_Portrait_Grandma.png");
            if (grandmaWorldSprite == null)
            {
                grandmaWorldSprite = chickenSprite != null ? chickenSprite : defaultSprite;
            }

            ConfigureWorldClickVisual(npcGrandmaGo, grandmaWorldSprite, Color.white, new Vector3(1f, 1f, 1f), 1, ref isSceneDirty);

            BoxCollider2D grandmaCollider = npcGrandmaGo.GetComponent<BoxCollider2D>();
            if (grandmaCollider == null)
            {
                grandmaCollider = npcGrandmaGo.AddComponent<BoxCollider2D>();
                isSceneDirty = true;
            }
            Vector2 expectedGrandmaCollSize = new Vector2(2f, 2f);
            if (grandmaCollider.size != expectedGrandmaCollSize)
            {
                grandmaCollider.size = expectedGrandmaCollSize;
                isSceneDirty = true;
            }

            CozyNPCWidget grandmaWidget = npcGrandmaGo.GetComponent<CozyNPCWidget>();
            if (grandmaWidget == null)
            {
                grandmaWidget = npcGrandmaGo.AddComponent<CozyNPCWidget>();
                isSceneDirty = true;
            }

            SerializedObject soGrandma = new SerializedObject(grandmaWidget);
            bool grandmaDirty = false;

            SerializedProperty npcDataProp = soGrandma.FindProperty("_npcData");
            if (npcDataProp != null)
            {
                SerializedProperty nameProp = npcDataProp.FindPropertyRelative("NpcName");
                if (nameProp.stringValue != "Bà Ngoại")
                {
                    nameProp.stringValue = "Bà Ngoại";
                    grandmaDirty = true;
                }

                SerializedProperty portraitProp = npcDataProp.FindPropertyRelative("Portrait");
                if (portraitProp.objectReferenceValue != grandmaWorldSprite)
                {
                    portraitProp.objectReferenceValue = grandmaWorldSprite;
                    grandmaDirty = true;
                }

                SerializedProperty dialoguesListProp = npcDataProp.FindPropertyRelative("Dialogues");
                if (dialoguesListProp != null)
                {
                    dialoguesListProp.ClearArray();

                    int dIdx = 0;
                    dialoguesListProp.InsertArrayElementAtIndex(dIdx);
                    dialoguesListProp.GetArrayElementAtIndex(dIdx).FindPropertyRelative("Line").stringValue = "Quê mình đẹp lắm con ơi, lúa chín vàng đồng, ngọt lịm hương mía nồng nàn.";
                    dIdx++;

                    dialoguesListProp.InsertArrayElementAtIndex(dIdx);
                    dialoguesListProp.GetArrayElementAtIndex(dIdx).FindPropertyRelative("Line").stringValue = "Hãy chăm chỉ tưới nước cho mía ngọt nhé, bông lúa nước ngoài kia cũng đang lớn dần kìa.";
                    dIdx++;

                    dialoguesListProp.InsertArrayElementAtIndex(dIdx);
                    dialoguesListProp.GetArrayElementAtIndex(dIdx).FindPropertyRelative("Line").stringValue = "Cảm nhận từng làn gió ấm áp của làng quê, mọi thứ thật êm đềm phải không con?";
                    dIdx++;

                    grandmaDirty = true;
                }
            }

            if (grandmaDirty)
            {
                soGrandma.ApplyModifiedProperties();
                isSceneDirty = true;
            }

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

            Image visualImg = SetupImage(stickerTemplate, "Visual_Image", ref isSceneDirty);
            if (defaultSprite != null && visualImg.sprite != defaultSprite)
            {
                visualImg.sprite = defaultSprite;
                isSceneDirty = true;
            }

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
            Color diaryNoteColor = new Color(1f, 0.92f, 0.35f, 1f);
            if (diaryNoteImg.color != diaryNoteColor)
            {
                diaryNoteImg.color = diaryNoteColor;
                isSceneDirty = true;
            }

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

            // Configure CozyLandscapeLayoutController
            ConfigureLandscapeLayoutController(
                uiRoot,
                headerPanel,
                gameplayArea,
                farmPlot,
                animalPen,
                stickerBookPanel,
                inventoryTray,
                sidebarPanel,
                popupRoot,
                ref isSceneDirty);

            // Log and Save completion
            if (isSceneDirty)
            {
                EditorUtility.SetDirty(canvas.gameObject);
                if (lifetimeScopeGo != null) EditorUtility.SetDirty(lifetimeScopeGo);
                if (!Application.isPlaying)
                {
                    var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
                    bool saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
                    Debug.Log($"<color=green>[CozySim]</color> Active Scene saved to disk: {activeScene.path} (Success: {saved})");
                }
                Debug.Log("<color=green>[CozySim]</color> Test Scene Hierarchy and Wiring generated & saved due to modifications!");
                ShowNotification(new GUIContent("Test Scene generated!"));
            }
            else
            {
                Debug.Log("<color=green>[CozySim]</color> Idempotent Check: Scene is already completely clean and match-perfect. No changes serialized.");
            }
        }

        private RectTransform SetupPanel(Transform parent, string name, ref bool isDirty)
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

            bool createdNew = false;
            if (go == null)
            {
                go = new GameObject(name);
                createdNew = true;
                if (parent != null)
                {
                    go.transform.SetParent(parent, false);
                }
                isDirty = true;
            }
            else if (parent != null && go.transform.parent != parent)
            {
                go.transform.SetParent(parent, false);
                isDirty = true;
                createdNew = true;
            }

            if (createdNew && !Application.isPlaying)
            {
                Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            }

            RectTransform rect = go.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = go.AddComponent<RectTransform>();
                isDirty = true;
            }

            // Ensure Image component is present for background panels to raycast/drag drop
            if (name.Contains("Plot") || name.Contains("Pen") || name.Contains("Panel") || name.Contains("Tray") || name.Contains("Page_"))
            {
                Image img = go.GetComponent<Image>();
                if (img == null)
                {
                    img = go.AddComponent<Image>();
                    img.color = new Color(0f, 0f, 0f, 0.15f); // Semi transparent backing
                    isDirty = true;
                }
                else
                {
                    Color targetColor = new Color(0f, 0f, 0f, 0.15f);
                    if (name == "Sidebar_Panel") targetColor = new Color(0f, 0f, 0f, 0.25f);
                    else if (name.Contains("Content_Panel")) targetColor = new Color(0.15f, 0.15f, 0.15f, 0.95f);

                    if (img.color != targetColor)
                    {
                        img.color = targetColor;
                        isDirty = true;
                    }
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

        private void ConfigureWorldClickVisual(GameObject target, Sprite sprite, Color fallbackColor, Vector3 scale, int sortingOrder, ref bool isDirty)
        {
            if (target == null) return;

            SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = target.AddComponent<SpriteRenderer>();
                isDirty = true;
            }

            if (renderer.sprite != sprite)
            {
                renderer.sprite = sprite;
                isDirty = true;
            }

            Color targetColor = sprite == null ? fallbackColor : Color.white;
            if (renderer.color != targetColor)
            {
                renderer.color = targetColor;
                isDirty = true;
            }

            if (renderer.sortingOrder != sortingOrder)
            {
                renderer.sortingOrder = sortingOrder;
                isDirty = true;
            }

            if (Vector3.Distance(target.transform.localScale, scale) > 0.001f)
            {
                target.transform.localScale = scale;
                isDirty = true;
            }
        }

        private TextMeshProUGUI SetupText(Transform parent, string name, string defaultText, string styleKey, ref bool isDirty)
        {
            RectTransform panel = SetupPanel(parent, name, ref isDirty);
            TextMeshProUGUI tmp = panel.gameObject.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
            {
                tmp = panel.gameObject.AddComponent<TextMeshProUGUI>();
                isDirty = true;
            }
            if (tmp.fontSize <= 0f || Mathf.Approximately(tmp.fontSize, -99f))
            {
                tmp.fontSize = 16f;
                isDirty = true;
                UnityEditor.EditorUtility.SetDirty(tmp);
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
                isDirty = true;
            }

            if (tmp.text != defaultText)
            {
                tmp.text = defaultText;
                isDirty = true;
            }

            TextAlignmentOptions targetAlignment = TextAlignmentOptions.Center;
            if (name.Contains("Count")) targetAlignment = TextAlignmentOptions.BottomRight;
            else if (name.Contains("Template")) targetAlignment = TextAlignmentOptions.Left;

            if (tmp.alignment != targetAlignment)
            {
                tmp.alignment = targetAlignment;
                isDirty = true;
            }

            if (!string.IsNullOrEmpty(styleKey))
            {
                UIStyleElement element = panel.gameObject.GetComponent<UIStyleElement>();
                if (element == null)
                {
                    element = panel.gameObject.AddComponent<UIStyleElement>();
                    isDirty = true;
                }

                SerializedObject soElement = new SerializedObject(element);
                bool elementDirty = false;
                SerializedProperty propKey = soElement.FindProperty("_styleKey");
                if (propKey != null && propKey.stringValue != styleKey)
                {
                    propKey.stringValue = styleKey;
                    elementDirty = true;
                }
                if (elementDirty)
                {
                    soElement.ApplyModifiedProperties();
                    isDirty = true;
                }
            }

            return tmp;
        }

        private Image SetupImage(Transform parent, string name, ref bool isDirty)
        {
            RectTransform panel = SetupPanel(parent, name, ref isDirty);
            Image img = panel.gameObject.GetComponent<Image>();
            if (img == null)
            {
                img = panel.gameObject.AddComponent<Image>();
                isDirty = true;
            }
            if (img.color != Color.white)
            {
                img.color = Color.white;
                isDirty = true;
            }
            return img;
        }

        private Button SetupButton(Transform parent, string name, string labelText, ref bool isDirty)
        {
            RectTransform panel = SetupPanel(parent, name, ref isDirty);
            Button btn = panel.gameObject.GetComponent<Button>();
            if (btn == null)
            {
                btn = panel.gameObject.AddComponent<Button>();
                isDirty = true;
            }

            CozyButtonJuice juice = btn.gameObject.GetComponent<CozyButtonJuice>();
            if (juice == null)
            {
                juice = btn.gameObject.AddComponent<CozyButtonJuice>();
                isDirty = true;
            }

            // Ensure Image component exists on Button for transition target graphic and raycast target
            Image btnImg = panel.gameObject.GetComponent<Image>();
            if (btnImg == null)
            {
                btnImg = panel.gameObject.AddComponent<Image>();
                btnImg.color = new Color(1f, 1f, 1f, 0.8f); // Default light white background
                isDirty = true;
            }

            // Wire target graphic for proper transitions and click bounds
            if (btn.targetGraphic != btnImg)
            {
                btn.targetGraphic = btnImg;
                isDirty = true;
            }

            // Add text label inside button
            TextMeshProUGUI label = panel.gameObject.GetComponentInChildren<TextMeshProUGUI>();
            if (label == null)
            {
                GameObject labelGo = new GameObject("Label");
                labelGo.transform.SetParent(panel, false);
                label = labelGo.AddComponent<TextMeshProUGUI>();
                RectTransform labelRect = label.GetComponent<RectTransform>();
                StretchToFill(labelRect, ref isDirty);
                isDirty = true;
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
                isDirty = true;
            }

            if (label.text != labelText)
            {
                label.text = labelText;
                isDirty = true;
            }

            if (label.alignment != TextAlignmentOptions.Center)
            {
                label.alignment = TextAlignmentOptions.Center;
                isDirty = true;
            }

            if (!Mathf.Approximately(label.fontSize, 24f))
            {
                label.fontSize = 24f;
                isDirty = true;
            }

            if (label.color != Color.black)
            {
                label.color = Color.black;
                isDirty = true;
            }

            return btn;
        }

        [MenuItem("Tools/CozySim/Refresh Asset Database")]
        public static void RefreshAssetDatabase()
        {
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log("<color=green>[CozySim]</color> AssetDatabase.Refresh() completed on the main thread!");
        }

        private void StretchToFill(RectTransform rect, ref bool isDirty)
        {
            SafeSetAnchor(rect, Vector2.zero, Vector2.one, ref isDirty);
            SafeSetSizeDelta(rect, Vector2.zero, ref isDirty);
            SafeSetAnchoredPosition(rect, Vector2.zero, ref isDirty);
        }

        private static void SafeSetAnchor(RectTransform rect, Vector2 min, Vector2 max, ref bool isDirty)
        {
            if (rect.anchorMin != min)
            {
                rect.anchorMin = min;
                isDirty = true;
            }
            if (rect.anchorMax != max)
            {
                rect.anchorMax = max;
                isDirty = true;
            }
        }

        private static void SafeSetSizeDelta(RectTransform rect, Vector2 sizeDelta, ref bool isDirty)
        {
            if (rect.sizeDelta != sizeDelta)
            {
                rect.sizeDelta = sizeDelta;
                isDirty = true;
            }
        }

        private static void SafeSetAnchoredPosition(RectTransform rect, Vector2 anchoredPosition, ref bool isDirty)
        {
            if (rect.anchoredPosition != anchoredPosition)
            {
                rect.anchoredPosition = anchoredPosition;
                isDirty = true;
            }
        }

        private static void SafeSetPivot(RectTransform rect, Vector2 pivot, ref bool isDirty)
        {
            if (rect.pivot != pivot)
            {
                rect.pivot = pivot;
                isDirty = true;
            }
        }

        private static void SafeSetObjectReference(SerializedProperty prop, UnityEngine.Object target, ref bool isDirty)
        {
            if (prop != null && prop.objectReferenceValue != target)
            {
                prop.objectReferenceValue = target;
                isDirty = true;
            }
        }

        private static void SafeSetInt(SerializedProperty prop, int value, ref bool isDirty)
        {
            if (prop != null && prop.intValue != value)
            {
                prop.intValue = value;
                isDirty = true;
            }
        }

        private static void SafeSetFloat(SerializedProperty prop, float value, ref bool isDirty)
        {
            if (prop != null && !Mathf.Approximately(prop.floatValue, value))
            {
                prop.floatValue = value;
                isDirty = true;
            }
        }

        private static void SafeSetString(SerializedProperty prop, string value, ref bool isDirty)
        {
            if (prop != null && prop.stringValue != value)
            {
                prop.stringValue = value;
                isDirty = true;
            }
        }

        private const string MainScenePath = "Assets/CozyLifeSim/Scenes/Main.unity";

        private static void ConfigureBuildAndPlayerSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            var existingIndex = scenes.FindIndex(scene => scene.path == MainScenePath);

            if (existingIndex >= 0)
            {
                var scene = scenes[existingIndex];
                if (existingIndex != 0 || !scene.enabled)
                {
                    scenes.RemoveAt(existingIndex);
                    scenes.Insert(0, new EditorBuildSettingsScene(MainScenePath, true));
                    EditorBuildSettings.scenes = scenes.ToArray();
                }
            }
            else
            {
                scenes.Insert(0, new EditorBuildSettingsScene(MainScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
        }

        private void ConfigureStartupLoadingOverlay(Transform popupRoot, ref bool isSceneDirty)
        {
            RectTransform overlayRect = SetupPanel(popupRoot, "Startup_Loading_Overlay", ref isSceneDirty);
            StretchToFill(overlayRect, ref isSceneDirty);
            overlayRect.transform.SetAsLastSibling();

            var group = overlayRect.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = overlayRect.gameObject.AddComponent<CanvasGroup>();
                isSceneDirty = true;
            }
            if (!Mathf.Approximately(group.alpha, 1f))
            {
                group.alpha = 1f;
                isSceneDirty = true;
            }
            if (!group.interactable)
            {
                group.interactable = true;
                isSceneDirty = true;
            }
            if (!group.blocksRaycasts)
            {
                group.blocksRaycasts = true;
                isSceneDirty = true;
            }

            Image backdrop = SetupImage(overlayRect, "Loading_Backdrop", ref isSceneDirty);
            StretchToFill(backdrop.GetComponent<RectTransform>(), ref isSceneDirty);
            var backdropColor = new Color(0.08f, 0.08f, 0.10f, 0.92f);
            if (backdrop.color != backdropColor)
            {
                backdrop.color = backdropColor;
                isSceneDirty = true;
            }

            TextMeshProUGUI message = SetupText(overlayRect, "Loading_Message", "Dang vao nong trai...", "Header_Text", ref isSceneDirty);
            RectTransform messageRect = message.GetComponent<RectTransform>();
            SafeSetAnchor(messageRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), ref isSceneDirty);
            SafeSetSizeDelta(messageRect, new Vector2(640f, 96f), ref isSceneDirty);
            SafeSetAnchoredPosition(messageRect, Vector2.zero, ref isSceneDirty);
            if (!Mathf.Approximately(message.fontSize, 32f))
            {
                message.fontSize = 32f;
                isSceneDirty = true;
            }
            if (message.color != Color.white)
            {
                message.color = Color.white;
                isSceneDirty = true;
            }

            var component = overlayRect.GetComponent<CozyStartupLoadingOverlay>();
            if (component == null)
            {
                component = overlayRect.gameObject.AddComponent<CozyStartupLoadingOverlay>();
                isSceneDirty = true;
            }

            SerializedObject soOverlay = new SerializedObject(component);
            bool overlayDirty = false;
            SafeSetObjectReference(soOverlay.FindProperty("canvasGroup"), group, ref overlayDirty);
            SafeSetObjectReference(soOverlay.FindProperty("messageText"), message, ref overlayDirty);
            SafeSetFloat(soOverlay.FindProperty("minVisibleSeconds"), 0.35f, ref overlayDirty);
            SafeSetFloat(soOverlay.FindProperty("fadeOutSeconds"), 0.2f, ref overlayDirty);
            if (overlayDirty)
            {
                soOverlay.ApplyModifiedProperties();
                isSceneDirty = true;
            }
        }

        private void ConfigureLandscapeLayoutController(
            RectTransform uiRoot,
            RectTransform headerPanel,
            RectTransform gameplayArea,
            RectTransform farmPlot,
            RectTransform animalPen,
            RectTransform stickerBookPanel,
            RectTransform inventoryTray,
            RectTransform sidebarPanel,
            RectTransform popupRoot,
            ref bool isSceneDirty)
        {
            RectTransform controllerRect = SetupPanel(uiRoot, "Landscape_Layout_Controller", ref isSceneDirty);
            StretchToFill(controllerRect, ref isSceneDirty);

            var controller = controllerRect.GetComponent<CozyLandscapeLayoutController>();
            if (controller == null)
            {
                controller = controllerRect.gameObject.AddComponent<CozyLandscapeLayoutController>();
                isSceneDirty = true;
            }

            SerializedObject soLayout = new SerializedObject(controller);
            bool layoutDirty = false;
            SafeSetObjectReference(soLayout.FindProperty("header"), headerPanel, ref layoutDirty);
            SafeSetObjectReference(soLayout.FindProperty("gameplayArea"), gameplayArea, ref layoutDirty);
            SafeSetObjectReference(soLayout.FindProperty("farmPlot"), farmPlot, ref layoutDirty);
            SafeSetObjectReference(soLayout.FindProperty("animalPen"), animalPen, ref layoutDirty);
            SafeSetObjectReference(soLayout.FindProperty("stickerBookPanel"), stickerBookPanel, ref layoutDirty);
            SafeSetObjectReference(soLayout.FindProperty("inventoryTray"), inventoryTray, ref layoutDirty);
            SafeSetObjectReference(soLayout.FindProperty("sidebar"), sidebarPanel, ref layoutDirty);
            SafeSetObjectReference(soLayout.FindProperty("popupRoot"), popupRoot, ref layoutDirty);
            if (layoutDirty)
            {
                soLayout.ApplyModifiedProperties();
                isSceneDirty = true;
            }
        }
    }
}
