using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CozyLifeSim.UI;

namespace CozyLifeSim.Editor
{
    public partial class CozySceneSetupWindow
    {
        private void ConfigureNPCGrandma(Sprite chickenSprite, Sprite defaultSprite, ref bool isSceneDirty)
        {
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
        }

        private void ConfigureInWorldInteractiveObjects(
            Sprite heartSprite,
            Sprite seedSprite,
            Sprite defaultSprite,
            QuestPopup questPopup,
            ShopPopup shopPopup,
            ref bool isSceneDirty)
        {
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

        private void ConfigureHeaderPanel(
            RectTransform uiRoot,
            Sprite coinSprite,
            Sprite seedsSprite,
            Sprite cropsSprite,
            ref bool isSceneDirty,
            out RectTransform headerPanel)
        {
            // 4. Setup Header Panel
            headerPanel = SetupPanel(uiRoot, "Header_Panel", ref isSceneDirty);
            SafeSetAnchor(headerPanel, new Vector2(0f, 0.90f), new Vector2(1f, 1f), ref isSceneDirty);
            SafeSetSizeDelta(headerPanel, Vector2.zero, ref isSceneDirty);
            SafeSetAnchoredPosition(headerPanel, Vector2.zero, ref isSceneDirty);

            // Configure HorizontalLayoutGroup to cleanly align elements and prevent layout fight
            HorizontalLayoutGroup headerLayout = headerPanel.gameObject.GetComponent<HorizontalLayoutGroup>();
            if (headerLayout == null)
            {
                headerLayout = headerPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                isSceneDirty = true;
            }
            if (headerLayout.childAlignment != TextAnchor.MiddleLeft)
            {
                headerLayout.childAlignment = TextAnchor.MiddleLeft;
                isSceneDirty = true;
            }
            if (headerLayout.padding.left != 50)
            {
                headerLayout.padding.left = 50;
                isSceneDirty = true;
            }
            if (headerLayout.padding.right != 50)
            {
                headerLayout.padding.right = 50;
                isSceneDirty = true;
            }
            if (!Mathf.Approximately(headerLayout.spacing, 50f))
            {
                headerLayout.spacing = 50f;
                isSceneDirty = true;
            }
            if (headerLayout.childControlWidth != false)
            {
                headerLayout.childControlWidth = false;
                isSceneDirty = true;
            }
            if (headerLayout.childControlHeight != false)
            {
                headerLayout.childControlHeight = false;
                isSceneDirty = true;
            }
            if (headerLayout.childForceExpandWidth != false)
            {
                headerLayout.childForceExpandWidth = false;
                isSceneDirty = true;
            }
            if (headerLayout.childForceExpandHeight != false)
            {
                headerLayout.childForceExpandHeight = false;
                isSceneDirty = true;
            }

            // Left HUD elements: Player status text displays (Coins, Seeds, Crops)
            TextMeshProUGUI coinsText = SetupText(headerPanel, "Coins_Text", "100", "", ref isSceneDirty);
            RectTransform coinsRect = coinsText.GetComponent<RectTransform>();
            SafeSetAnchor(coinsRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), ref isSceneDirty);
            SafeSetPivot(coinsRect, new Vector2(0f, 0.5f), ref isSceneDirty);
            SafeSetSizeDelta(coinsRect, new Vector2(100f, 40f), ref isSceneDirty);
            SafeSetAnchoredPosition(coinsRect, new Vector2(50f, 0f), ref isSceneDirty);
            if (coinsText.alignment != TextAlignmentOptions.Left) { coinsText.alignment = TextAlignmentOptions.Left; isSceneDirty = true; }

            RectTransform coinIconRect = SetupPanel(coinsText.transform, "Coin_Icon", ref isSceneDirty);
            SafeSetAnchor(coinIconRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), ref isSceneDirty);
            SafeSetPivot(coinIconRect, new Vector2(1f, 0.5f), ref isSceneDirty);
            SafeSetSizeDelta(coinIconRect, new Vector2(30f, 30f), ref isSceneDirty);
            SafeSetAnchoredPosition(coinIconRect, new Vector2(-10f, 0f), ref isSceneDirty);
            Image coinIconImg = coinIconRect.gameObject.GetComponent<Image>();
            if (coinIconImg == null)
            {
                coinIconImg = coinIconRect.gameObject.AddComponent<Image>();
                isSceneDirty = true;
            }
            if (coinIconImg.sprite != coinSprite)
            {
                coinIconImg.sprite = coinSprite;
                isSceneDirty = true;
            }

            TextMeshProUGUI seedsText = SetupText(headerPanel, "Seeds_Text", "5", "", ref isSceneDirty);
            RectTransform seedsRect = seedsText.GetComponent<RectTransform>();
            SafeSetAnchor(seedsRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), ref isSceneDirty);
            SafeSetPivot(seedsRect, new Vector2(0f, 0.5f), ref isSceneDirty);
            SafeSetSizeDelta(seedsRect, new Vector2(100f, 40f), ref isSceneDirty);
            SafeSetAnchoredPosition(seedsRect, new Vector2(200f, 0f), ref isSceneDirty);
            if (seedsText.alignment != TextAlignmentOptions.Left) { seedsText.alignment = TextAlignmentOptions.Left; isSceneDirty = true; }

            RectTransform seedsIconRect = SetupPanel(seedsText.transform, "Seeds_Icon", ref isSceneDirty);
            SafeSetAnchor(seedsIconRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), ref isSceneDirty);
            SafeSetPivot(seedsIconRect, new Vector2(1f, 0.5f), ref isSceneDirty);
            SafeSetSizeDelta(seedsIconRect, new Vector2(30f, 30f), ref isSceneDirty);
            SafeSetAnchoredPosition(seedsIconRect, new Vector2(-10f, 0f), ref isSceneDirty);
            Image seedsIconImg = seedsIconRect.gameObject.GetComponent<Image>();
            if (seedsIconImg == null)
            {
                seedsIconImg = seedsIconRect.gameObject.AddComponent<Image>();
                isSceneDirty = true;
            }
            if (seedsIconImg.sprite != seedsSprite)
            {
                seedsIconImg.sprite = seedsSprite;
                isSceneDirty = true;
            }

            TextMeshProUGUI cropsText = SetupText(headerPanel, "Crops_Text", "0", "", ref isSceneDirty);
            RectTransform cropsRect = cropsText.GetComponent<RectTransform>();
            SafeSetAnchor(cropsRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), ref isSceneDirty);
            SafeSetPivot(cropsRect, new Vector2(0f, 0.5f), ref isSceneDirty);
            SafeSetSizeDelta(cropsRect, new Vector2(100f, 40f), ref isSceneDirty);
            SafeSetAnchoredPosition(cropsRect, new Vector2(350f, 0f), ref isSceneDirty);
            if (cropsText.alignment != TextAlignmentOptions.Left) { cropsText.alignment = TextAlignmentOptions.Left; isSceneDirty = true; }

            RectTransform cropsIconRect = SetupPanel(cropsText.transform, "Crops_Icon", ref isSceneDirty);
            SafeSetAnchor(cropsIconRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), ref isSceneDirty);
            SafeSetPivot(cropsIconRect, new Vector2(1f, 0.5f), ref isSceneDirty);
            SafeSetSizeDelta(cropsIconRect, new Vector2(30f, 30f), ref isSceneDirty);
            SafeSetAnchoredPosition(cropsIconRect, new Vector2(-10f, 0f), ref isSceneDirty);
            Image cropsIconImg = cropsIconRect.gameObject.GetComponent<Image>();
            if (cropsIconImg == null)
            {
                cropsIconImg = cropsIconRect.gameObject.AddComponent<Image>();
                isSceneDirty = true;
            }
            if (cropsIconImg.sprite != cropsSprite)
            {
                cropsIconImg.sprite = cropsSprite;
                isSceneDirty = true;
            }

            // Right HUD element: Autosave Icon
            RectTransform autosaveIconRect = SetupPanel(headerPanel, "Autosave_Icon", ref isSceneDirty);
            SafeSetAnchor(autosaveIconRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), ref isSceneDirty);
            SafeSetPivot(autosaveIconRect, new Vector2(0.5f, 0.5f), ref isSceneDirty);
            SafeSetSizeDelta(autosaveIconRect, new Vector2(30f, 30f), ref isSceneDirty);
            SafeSetAnchoredPosition(autosaveIconRect, new Vector2(-50f, 0f), ref isSceneDirty);
            LayoutElement autosaveLayout = autosaveIconRect.gameObject.GetComponent<LayoutElement>();
            if (autosaveLayout == null)
            {
                autosaveLayout = autosaveIconRect.gameObject.AddComponent<LayoutElement>();
                isSceneDirty = true;
            }
            if (!autosaveLayout.ignoreLayout)
            {
                autosaveLayout.ignoreLayout = true;
                isSceneDirty = true;
            }

            Image autosaveIconImg = autosaveIconRect.gameObject.GetComponent<Image>();
            if (autosaveIconImg == null)
            {
                autosaveIconImg = autosaveIconRect.gameObject.AddComponent<Image>();
                isSceneDirty = true;
            }
            var autosaveSprite = LoadSprite("UI_Icon_Autosave");
            if (autosaveIconImg.sprite != autosaveSprite)
            {
                autosaveIconImg.sprite = autosaveSprite;
                isSceneDirty = true;
            }
            if (autosaveIconImg.type != Image.Type.Simple)
            {
                autosaveIconImg.type = Image.Type.Simple;
                isSceneDirty = true;
            }
            if (autosaveIconImg.color != Color.white)
            {
                autosaveIconImg.color = Color.white;
                isSceneDirty = true;
            }
        }

        private void ConfigureSidebarPanel(
            RectTransform uiRoot,
            QuestPopup questPopup,
            ShopPopup shopPopup,
            ref bool isSceneDirty,
            out RectTransform sidebarPanel)
        {
            // 8.5. Setup Sidebar Panel (Navigation Dock)
            sidebarPanel = SetupPanel(uiRoot, "Sidebar_Panel", ref isSceneDirty);
            SafeSetAnchor(sidebarPanel, new Vector2(0.92f, 0.45f), new Vector2(0.98f, 0.90f), ref isSceneDirty);
            SafeSetSizeDelta(sidebarPanel, Vector2.zero, ref isSceneDirty);
            SafeSetAnchoredPosition(sidebarPanel, Vector2.zero, ref isSceneDirty);

            // Set up wood frame background for sidebar panel
            Image sidebarImg = sidebarPanel.gameObject.GetComponent<Image>();
            if (sidebarImg == null)
            {
                sidebarImg = sidebarPanel.gameObject.AddComponent<Image>();
                isSceneDirty = true;
            }
            var sidebarWoodSprite = LoadSprite("UI_Panel_Frame_Wood");
            if (sidebarImg.sprite != sidebarWoodSprite)
            {
                sidebarImg.sprite = sidebarWoodSprite;
                isSceneDirty = true;
            }
            if (sidebarImg.type != Image.Type.Sliced)
            {
                sidebarImg.type = Image.Type.Sliced;
                isSceneDirty = true;
            }
            if (sidebarImg.color != Color.white)
            {
                sidebarImg.color = Color.white;
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
            Image questBtnImg = questBtn.GetComponent<Image>();
            if (questBtnImg != null)
            {
                var tabBtnBg = LoadSprite("UI_Tab_Button_Bg");
                if (questBtnImg.sprite != tabBtnBg) { questBtnImg.sprite = tabBtnBg; isSceneDirty = true; }
                if (questBtnImg.type != Image.Type.Sliced) { questBtnImg.type = Image.Type.Sliced; isSceneDirty = true; }
                if (questBtnImg.color != Color.white) { questBtnImg.color = Color.white; isSceneDirty = true; }
            }
            SetupButtonIcon(questBtn, "Quest_Icon", LoadSprite("UI_Icon_Quest"), ref isSceneDirty);

            Button shopBtn = SetupButton(sidebarPanel, "Shop_Button", "S", ref isSceneDirty);
            SafeSetSizeDelta(shopBtn.GetComponent<RectTransform>(), new Vector2(0f, 55f), ref isSceneDirty);
            Image shopBtnImg = shopBtn.GetComponent<Image>();
            if (shopBtnImg != null)
            {
                var tabBtnBg = LoadSprite("UI_Tab_Button_Bg");
                if (shopBtnImg.sprite != tabBtnBg) { shopBtnImg.sprite = tabBtnBg; isSceneDirty = true; }
                if (shopBtnImg.type != Image.Type.Sliced) { shopBtnImg.type = Image.Type.Sliced; isSceneDirty = true; }
                if (shopBtnImg.color != Color.white) { shopBtnImg.color = Color.white; isSceneDirty = true; }
            }
            SetupButtonIcon(shopBtn, "Shop_Icon", LoadSprite("UI_Icon_Shop"), ref isSceneDirty);

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
        }

        private void ConfigureProgressionHud(RectTransform headerPanel, Sprite starSprite, ref bool isSceneDirty)
        {
            RectTransform progressionHud = SetupPanel(headerPanel, "Progression_HUD", ref isSceneDirty);
            SafeSetAnchor(progressionHud, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), ref isSceneDirty);
            SafeSetPivot(progressionHud, new Vector2(0.5f, 0.5f), ref isSceneDirty);
            SafeSetSizeDelta(progressionHud, new Vector2(300f, 50f), ref isSceneDirty);
            SafeSetAnchoredPosition(progressionHud, new Vector2(50f, 0f), ref isSceneDirty);

            // ProgressionHUD level status Text displaying level
            TextMeshProUGUI levelText = SetupText(progressionHud, "Level_Text", "LV. 1", "", ref isSceneDirty);
            RectTransform levelRect = levelText.GetComponent<RectTransform>();
            SafeSetAnchor(levelRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), ref isSceneDirty);
            SafeSetPivot(levelRect, new Vector2(0f, 0.5f), ref isSceneDirty);
            SafeSetSizeDelta(levelRect, new Vector2(80f, 30f), ref isSceneDirty);
            SafeSetAnchoredPosition(levelRect, new Vector2(40f, 0f), ref isSceneDirty);
            if (levelText.alignment != TextAlignmentOptions.Left) { levelText.alignment = TextAlignmentOptions.Left; isSceneDirty = true; }

            RectTransform levelStarRect = SetupPanel(levelText.transform, "Level_Star_Icon", ref isSceneDirty);
            SafeSetAnchor(levelStarRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), ref isSceneDirty);
            SafeSetPivot(levelStarRect, new Vector2(1f, 0.5f), ref isSceneDirty);
            SafeSetSizeDelta(levelStarRect, new Vector2(25f, 25f), ref isSceneDirty);
            SafeSetAnchoredPosition(levelStarRect, new Vector2(-5f, 0f), ref isSceneDirty);
            Image levelStarImg = levelStarRect.gameObject.GetComponent<Image>();
            if (levelStarImg == null)
            {
                levelStarImg = levelStarRect.gameObject.AddComponent<Image>();
                isSceneDirty = true;
            }
            if (levelStarImg.sprite != starSprite)
            {
                levelStarImg.sprite = starSprite;
                isSceneDirty = true;
            }

            // ProgressionHUD XP slider bar elements
            RectTransform xpBarContainer = SetupPanel(progressionHud, "XP_Bar_Container", ref isSceneDirty);
            SafeSetAnchor(xpBarContainer, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), ref isSceneDirty);
            SafeSetPivot(xpBarContainer, new Vector2(0f, 0.5f), ref isSceneDirty);
            SafeSetSizeDelta(xpBarContainer, new Vector2(-150f, 20f), ref isSceneDirty);
            SafeSetAnchoredPosition(xpBarContainer, new Vector2(140f, 0f), ref isSceneDirty);

            Image xpBg = xpBarContainer.gameObject.GetComponent<Image>();
            if (xpBg == null)
            {
                xpBg = xpBarContainer.gameObject.AddComponent<Image>();
                isSceneDirty = true;
            }
            Color xpBgColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            if (xpBg.color != xpBgColor) { xpBg.color = xpBgColor; isSceneDirty = true; }

            RectTransform fillArea = SetupPanel(xpBarContainer, "Fill_Area", ref isSceneDirty);
            StretchToFill(fillArea, ref isSceneDirty);
            Image fillAreaImg = fillArea.gameObject.GetComponent<Image>();
            if (fillAreaImg != null)
            {
                DestroyImmediate(fillAreaImg);
                isSceneDirty = true;
            }

            RectTransform xpFill = SetupPanel(fillArea, "Fill", ref isSceneDirty);
            SafeSetAnchor(xpFill, Vector2.zero, new Vector2(0.5f, 1f), ref isSceneDirty); // Starts half full in preview
            SafeSetSizeDelta(xpFill, Vector2.zero, ref isSceneDirty);
            SafeSetAnchoredPosition(xpFill, Vector2.zero, ref isSceneDirty);

            Image fillImg = xpFill.gameObject.GetComponent<Image>();
            if (fillImg == null)
            {
                fillImg = xpFill.gameObject.AddComponent<Image>();
                isSceneDirty = true;
            }
            Color xpFillColor = new Color(0.2f, 0.8f, 0.2f, 1f);
            if (fillImg.color != xpFillColor) { fillImg.color = xpFillColor; isSceneDirty = true; }

            Slider xpSlider = progressionHud.gameObject.GetComponent<Slider>();
            if (xpSlider == null)
            {
                xpSlider = progressionHud.gameObject.AddComponent<Slider>();
                isSceneDirty = true;
            }
            if (xpSlider.fillRect != xpFill) { xpSlider.fillRect = xpFill; isSceneDirty = true; }
            if (xpSlider.targetGraphic != fillImg) { xpSlider.targetGraphic = fillImg; isSceneDirty = true; }
            if (xpSlider.navigation.mode != Navigation.Mode.None)
            {
                Navigation nav = xpSlider.navigation;
                nav.mode = Navigation.Mode.None;
                xpSlider.navigation = nav;
                isSceneDirty = true;
            }

            ProgressionHudWidget progressionHudWidget = progressionHud.gameObject.GetComponent<ProgressionHudWidget>();
            if (progressionHudWidget == null)
            {
                progressionHudWidget = progressionHud.gameObject.AddComponent<ProgressionHudWidget>();
                isSceneDirty = true;
            }

            SerializedObject soProg = new SerializedObject(progressionHudWidget);
            bool progDirty = false;
            SafeSetObjectReference(soProg.FindProperty("_xpSlider"), xpSlider, ref progDirty);
            SafeSetObjectReference(soProg.FindProperty("_levelText"), levelText, ref progDirty);
            if (progDirty)
            {
                soProg.ApplyModifiedProperties();
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

        private void ConfigureFeedbackToast(RectTransform popupRoot, GameLifetimeScope lifetimeScope, ref bool isSceneDirty)
        {
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
        }
    }
}
