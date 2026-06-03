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
    public partial class CozySceneSetupWindow : EditorWindow
    {
        private const string MainScenePath = "Assets/CozyLifeSim/Scenes/Main.unity";
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

        [MenuItem("Tools/CozySim/Refresh Asset Database")]
        public static void RefreshAssetDatabase()
        {
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log("<color=green>[CozySim]</color> AssetDatabase.Refresh() completed on the main thread!");
        }

        private Sprite LoadSprite(string name)
        {
            string path = $"Assets/CozyLifeSim/Textures/Heritage/{name}.png";
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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

            ConfigureHeritageTextures();
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

            // Clean up legacy direct canvas children if they exist to prevent duplicates
            string[] legacyCanvasChildren = { 
                "Quest_Popup", "Shop_Popup", "Diary_Input_Popup", "Dialogue_Popup", 
                "Sidebar_Panel", "Gameplay_Area", "Header_Panel", "Inventory_Tray" 
            };
            foreach (string childName in legacyCanvasChildren)
            {
                Transform legacyChild = canvas.transform.Find(childName);
                if (legacyChild != null)
                {
                    DestroyImmediate(legacyChild.gameObject);
                    isSceneDirty = true;
                }
            }

            // 3. Setup UI_Root
            RectTransform uiRoot = SetupPanel(canvas.transform, "UI_Root", ref isSceneDirty);
            StretchToFill(uiRoot, ref isSceneDirty);

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
            Sprite starSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Packages/CuteKawaiiGUIPack/Icons/Icons/Levels/Star-256.png");
            if (starSprite == null)
            {
                starSprite = LoadSprite("System_Icon_Level_Star");
            }

            // 4. Setup Header Panel (displays player status text displays)
            RectTransform headerPanel;
            ConfigureHeaderPanel(uiRoot, LoadSprite("System_Coin_Vietnamese"), LoadSprite("System_Icon_Seeds"), LoadSprite("System_Icon_Crops"), ref isSceneDirty, out headerPanel);

            // 5. Setup Progression HUD inside header panel
            ConfigureProgressionHud(headerPanel, starSprite, ref isSceneDirty);

            // 6. Setup Gameplay Area
            RectTransform gameplayArea = SetupPanel(uiRoot, "Gameplay_Area", ref isSceneDirty);
            SafeSetAnchor(gameplayArea, new Vector2(0f, 0.25f), new Vector2(0.92f, 0.90f), ref isSceneDirty);
            SafeSetSizeDelta(gameplayArea, Vector2.zero, ref isSceneDirty);
            SafeSetAnchoredPosition(gameplayArea, Vector2.zero, ref isSceneDirty);

            // Ensure layout elements on gameplayArea are set up
            HorizontalLayoutGroup gameplayLayout = gameplayArea.gameObject.GetComponent<HorizontalLayoutGroup>();
            if (gameplayLayout == null)
            {
                gameplayLayout = gameplayArea.gameObject.AddComponent<HorizontalLayoutGroup>();
                isSceneDirty = true;
            }
            if (gameplayLayout.childAlignment != TextAnchor.MiddleCenter) { gameplayLayout.childAlignment = TextAnchor.MiddleCenter; isSceneDirty = true; }
            if (!Mathf.Approximately(gameplayLayout.spacing, 25f)) { gameplayLayout.spacing = 25f; isSceneDirty = true; }
            if (gameplayLayout.childControlHeight) { gameplayLayout.childControlHeight = false; isSceneDirty = true; }
            if (gameplayLayout.childControlWidth) { gameplayLayout.childControlWidth = false; isSceneDirty = true; }
            if (gameplayLayout.childForceExpandHeight) { gameplayLayout.childForceExpandHeight = false; isSceneDirty = true; }
            if (gameplayLayout.childForceExpandWidth) { gameplayLayout.childForceExpandWidth = false; isSceneDirty = true; }

            // 7. Setup Farm Plot (Trong trot)
            ConfigureFarmPlot(gameplayArea, defaultSprite, seedSprite, sproutSprite, matureSprite, wateringCanSprite, ref isSceneDirty);

            // 8. Setup Animal Pen (Nuoi ga)
            RectTransform prefabsHolder = SetupPanel(canvas.transform, "Prefabs_Holder", ref isSceneDirty);
            ConfigureAnimalPen(gameplayArea, defaultSprite, chickenSprite, heartSprite, prefabsHolder, ref isSceneDirty);

            // 9. Setup Sticker Book
            StickerBookPage bookPage0;
            StickerBookPage bookPage1;
            StickerBook stickerBook;
            ConfigureStickerBook(gameplayArea, ref isSceneDirty, out bookPage0, out bookPage1, out stickerBook);

            // 10. Popups
            Color dimColor = new Color(0f, 0f, 0f, 0.4f);
            QuestPopup questPopup;
            ConfigureQuestPopup(uiRoot, dimColor, ref isSceneDirty, out questPopup);

            ShopPopup shopPopup;
            ConfigureShopPopup(uiRoot, dimColor, ref isSceneDirty, out shopPopup);

            RectTransform diaryInputPopupPanel;
            CozyDiaryInputPopup diaryInputPopup;
            ConfigureDiaryInputPopup(uiRoot, dimColor, ref isSceneDirty, out diaryInputPopupPanel, out diaryInputPopup);

            CozyDialoguePopup dialoguePopup;
            ConfigureDialoguePopup(uiRoot, lifetimeScope, ref isSceneDirty, out dialoguePopup);

            // 11. Setup Popup_Root, Startup Loading Overlay & Feedback Toast
            RectTransform popupRoot = SetupPanel(canvas.transform, "Popup_Root", ref isSceneDirty);
            StretchToFill(popupRoot, ref isSceneDirty);

            ConfigureStartupLoadingOverlay(popupRoot, ref isSceneDirty);
            ConfigureFeedbackToast(popupRoot, lifetimeScope, ref isSceneDirty);

            // 12. Setup Sidebar Panel (Navigation Dock)
            RectTransform sidebarPanel;
            ConfigureSidebarPanel(uiRoot, questPopup, shopPopup, ref isSceneDirty, out sidebarPanel);

            // 13. Setup in-world Interactive Objects (Quest Board & Shop Stall) and NPC Grandma
            ConfigureInWorldInteractiveObjects(heartSprite, seedSprite, defaultSprite, questPopup, shopPopup, ref isSceneDirty);
            ConfigureNPCGrandma(chickenSprite, defaultSprite, ref isSceneDirty);

            // 14. Setup Inventory Tray & auto-wire templates
            ConfigureInventoryTray(uiRoot, prefabsHolder, defaultSprite, bookPage0, bookPage1, stickerBook, diaryInputPopup, ref isSceneDirty);

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
    }
}
