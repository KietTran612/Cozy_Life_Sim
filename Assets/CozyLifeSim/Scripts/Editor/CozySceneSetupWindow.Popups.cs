using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CozyLifeSim.UI;

namespace CozyLifeSim.Editor
{
    public partial class CozySceneSetupWindow
    {
        private void ConfigureQuestPopup(
            RectTransform uiRoot,
            Color dimColor,
            ref bool isSceneDirty,
            out QuestPopup questPopup)
        {
            // A. Setup Quest Popup
            RectTransform questPopupPanel = SetupPanel(uiRoot, "Quest_Popup", ref isSceneDirty);
            StretchToFill(questPopupPanel, ref isSceneDirty);
            if (questPopupPanel.gameObject.activeSelf)
            {
                questPopupPanel.gameObject.SetActive(false);
                isSceneDirty = true;
            }

            questPopup = questPopupPanel.gameObject.GetComponent<QuestPopup>();
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
            var targetQContentSprite = LoadSprite("UI_Panel_Frame_Wood");
            if (qContentImg.sprite != targetQContentSprite)
            {
                qContentImg.sprite = targetQContentSprite;
                isSceneDirty = true;
            }
            ConfigureSlicedImage(qContentImg, ref isSceneDirty);

            Button qCloseBtn = SetupButton(qContentPanel, "Close_Button", "X", ref isSceneDirty);
            RectTransform qCloseRect = qCloseBtn.GetComponent<RectTransform>();
            SafeSetAnchor(qCloseRect, new Vector2(1f, 1f), new Vector2(1f, 1f), ref isSceneDirty);
            SafeSetPivot(qCloseRect, new Vector2(1f, 1f), ref isSceneDirty);
            SafeSetSizeDelta(qCloseRect, new Vector2(40f, 40f), ref isSceneDirty);
            SafeSetAnchoredPosition(qCloseRect, new Vector2(-10f, -10f), ref isSceneDirty);
            ConfigureCloseButton(qCloseBtn, LoadSprite("UI_Button_Close_X"), ref isSceneDirty);

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

            // Cleanup obsolete Text component on Quest_Item_Template if it exists
            Transform oldTemplate = qItemsList.Find("Quest_Item_Template");
            if (oldTemplate != null)
            {
                TextMeshProUGUI oldTextComp = oldTemplate.GetComponent<TextMeshProUGUI>();
                if (oldTextComp != null)
                {
                    DestroyImmediate(oldTextComp);
                    isSceneDirty = true;
                }
            }
            CozyQuestItemWidget questItemWidgetComponent = SetupQuestItemWidgetTemplate(qItemsList, "Quest_Item_Template", ref isSceneDirty);

            // Wire QuestPopup
            SerializedObject soQPopup = new SerializedObject(questPopup);
            bool qPopupDirty = false;
            SafeSetObjectReference(soQPopup.FindProperty("_contentPanel"), qContentPanel, ref qPopupDirty);
            SafeSetObjectReference(soQPopup.FindProperty("_backgroundDim"), qDimGroup, ref qPopupDirty);
            SafeSetObjectReference(soQPopup.FindProperty("_closeButton"), qCloseBtn, ref qPopupDirty);
            SafeSetObjectReference(soQPopup.FindProperty("_questItemTemplate"), questItemWidgetComponent, ref qPopupDirty);
            SafeSetObjectReference(soQPopup.FindProperty("_itemBgSprite"), LoadSprite("UI_Quest_Item_Bg"), ref qPopupDirty);
            SafeSetObjectReference(soQPopup.FindProperty("_questWaterIcon"), LoadSprite("UI_Icon_QuestType_Water"), ref qPopupDirty);
            SafeSetObjectReference(soQPopup.FindProperty("_questHarvestIcon"), LoadSprite("UI_Icon_QuestType_Harvest"), ref qPopupDirty);
            SafeSetObjectReference(soQPopup.FindProperty("_questPetIcon"), LoadSprite("UI_Icon_QuestType_Pet"), ref qPopupDirty);
            SafeSetObjectReference(soQPopup.FindProperty("_questCompletedStamp"), LoadSprite("UI_Quest_Stamp_Completed"), ref qPopupDirty);
            if (qPopupDirty)
            {
                soQPopup.ApplyModifiedProperties();
                isSceneDirty = true;
            }
        }

        private void ConfigureShopPopup(
            RectTransform uiRoot,
            Color dimColor,
            ref bool isSceneDirty,
            out ShopPopup shopPopup)
        {
            // B. Setup Shop Popup
            RectTransform shopPopupPanel = SetupPanel(uiRoot, "Shop_Popup", ref isSceneDirty);
            StretchToFill(shopPopupPanel, ref isSceneDirty);
            if (shopPopupPanel.gameObject.activeSelf)
            {
                shopPopupPanel.gameObject.SetActive(false);
                isSceneDirty = true;
            }

            shopPopup = shopPopupPanel.gameObject.GetComponent<ShopPopup>();
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
            var targetSContentSprite = LoadSprite("UI_Panel_Frame_Wood");
            if (sContentImg.sprite != targetSContentSprite)
            {
                sContentImg.sprite = targetSContentSprite;
                isSceneDirty = true;
            }
            ConfigureSlicedImage(sContentImg, ref isSceneDirty);

            Button sCloseBtn = SetupButton(sContentPanel, "Close_Button", "X", ref isSceneDirty);
            RectTransform sCloseRect = sCloseBtn.GetComponent<RectTransform>();
            SafeSetAnchor(sCloseRect, new Vector2(1f, 1f), new Vector2(1f, 1f), ref isSceneDirty);
            SafeSetPivot(sCloseRect, new Vector2(1f, 1f), ref isSceneDirty);
            SafeSetSizeDelta(sCloseRect, new Vector2(40f, 40f), ref isSceneDirty);
            SafeSetAnchoredPosition(sCloseRect, new Vector2(-10f, -10f), ref isSceneDirty);
            ConfigureCloseButton(sCloseBtn, LoadSprite("UI_Button_Close_X"), ref isSceneDirty);

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
            RectTransform shopItemTemplate = SetupPanel(GetPrefabsHolderTransform(), "Shop_Item_Template", ref isSceneDirty);
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
            ConfigureSimpleImage(itemIcon, true, ref isSceneDirty);
            SafeSetSizeDelta(itemIcon.GetComponent<RectTransform>(), new Vector2(50f, 50f), ref isSceneDirty);
            TextMeshProUGUI itemName = SetupText(shopItemTemplate, "Item_Name", "Product Name", "", ref isSceneDirty);
            if (!Mathf.Approximately(itemName.fontSize, 14f)) { itemName.fontSize = 14f; isSceneDirty = true; }
            TextMeshProUGUI itemPrice = SetupText(shopItemTemplate, "Item_Price", "50 Coins", "", ref isSceneDirty);
            if (!Mathf.Approximately(itemPrice.fontSize, 12f)) { itemPrice.fontSize = 12f; isSceneDirty = true; }
            Button itemActionBtn = SetupButton(shopItemTemplate, "Action_Button", "Buy", ref isSceneDirty);
            SafeSetSizeDelta(itemActionBtn.GetComponent<RectTransform>(), new Vector2(60f, 30f), ref isSceneDirty);
            TextMeshProUGUI itemActionTxt = itemActionBtn.GetComponentInChildren<TextMeshProUGUI>(true);
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
        }

        private void ConfigureDiaryInputPopup(
            RectTransform uiRoot,
            Color dimColor,
            ref bool isSceneDirty,
            out RectTransform diaryPopupPanel,
            out CozyDiaryInputPopup diaryInputPopup)
        {
            // B.5. Setup Diary Input Popup
            diaryPopupPanel = SetupPanel(uiRoot, "Diary_Input_Popup", ref isSceneDirty);
            StretchToFill(diaryPopupPanel, ref isSceneDirty);
            if (diaryPopupPanel.gameObject.activeSelf)
            {
                diaryPopupPanel.gameObject.SetActive(false);
                isSceneDirty = true;
            }

            diaryInputPopup = diaryPopupPanel.gameObject.GetComponent<CozyDiaryInputPopup>();
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
            var targetDiaryContentSprite = LoadSprite("UI_Panel_Frame_Wood");
            if (diaryContentImg.sprite != targetDiaryContentSprite)
            {
                diaryContentImg.sprite = targetDiaryContentSprite;
                isSceneDirty = true;
            }
            ConfigureSlicedImage(diaryContentImg, ref isSceneDirty);

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
            TextMeshProUGUI diaryConfirmText = diaryConfirmBtn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (!Mathf.Approximately(diaryConfirmText.fontSize, 18f)) { diaryConfirmText.fontSize = 18f; isSceneDirty = true; }

            Button diaryCancelBtn = SetupButton(diaryContentPanel, "Cancel_Button", "Cancel", ref isSceneDirty);
            RectTransform diaryCancelRect = diaryCancelBtn.GetComponent<RectTransform>();
            SafeSetAnchor(diaryCancelRect, new Vector2(0.12f, 0.08f), new Vector2(0.42f, 0.25f), ref isSceneDirty);
            SafeSetSizeDelta(diaryCancelRect, Vector2.zero, ref isSceneDirty);
            SafeSetAnchoredPosition(diaryCancelRect, Vector2.zero, ref isSceneDirty);
            TextMeshProUGUI diaryCancelText = diaryCancelBtn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (!Mathf.Approximately(diaryCancelText.fontSize, 18f)) { diaryCancelText.fontSize = 18f; isSceneDirty = true; }
            ConfigureCloseButton(diaryCancelBtn, LoadSprite("UI_Button_Close_X"), ref isSceneDirty);

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
        }

        private void ConfigureDialoguePopup(
            RectTransform uiRoot,
            GameLifetimeScope lifetimeScope,
            ref bool isSceneDirty,
            out CozyDialoguePopup dialoguePopup)
        {
            // C. Setup Dialogue Popup
            RectTransform dialoguePopupPanel = SetupPanel(uiRoot, "Dialogue_Popup", ref isSceneDirty);
            StretchToFill(dialoguePopupPanel, ref isSceneDirty);

            // Parent MUST be active for Construct to run, but we can set its activeSelf to true.
            if (!dialoguePopupPanel.gameObject.activeSelf)
            {
                dialoguePopupPanel.gameObject.SetActive(true);
                isSceneDirty = true;
            }

            dialoguePopup = dialoguePopupPanel.gameObject.GetComponent<CozyDialoguePopup>();
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
            var targetDContentSprite = LoadSprite("UI_Dialogue_Bubble");
            if (dContentImg.sprite != targetDContentSprite)
            {
                dContentImg.sprite = targetDContentSprite;
                isSceneDirty = true;
            }
            ConfigureSlicedImage(dContentImg, ref isSceneDirty);
            // Ensure content panel is inactive by default (the script will activate it)
            if (dContentPanel.gameObject.activeSelf)
            {
                dContentPanel.gameObject.SetActive(false);
                isSceneDirty = true;
            }

            // Portrait inside Content Panel
            Image dPortrait = SetupImage(dContentPanel, "Portrait", ref isSceneDirty);
            ConfigureSimpleImage(dPortrait, true, ref isSceneDirty);
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
            TextMeshProUGUI dDialogueText = SetupText(dContentPanel, "Dialogue_Text", "... Dialogue text", "Body_Text", ref isSceneDirty);
            RectTransform dDialogueRect = dDialogueText.GetComponent<RectTransform>();
            SafeSetAnchor(dDialogueRect, new Vector2(0f, 0f), new Vector2(1f, 1f), ref isSceneDirty);
            SafeSetPivot(dDialogueRect, new Vector2(0.5f, 0.5f), ref isSceneDirty);
            SafeSetSizeDelta(dDialogueRect, new Vector2(-260f, -60f), ref isSceneDirty);
            SafeSetAnchoredPosition(dDialogueRect, new Vector2(70f, -15f), ref isSceneDirty);
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

            // Next Button inside Content Panel
            Button dNextBtn = SetupButton(dContentPanel, "Next_Button", "Tiep", ref isSceneDirty);
            RectTransform dNextRect = dNextBtn.GetComponent<RectTransform>();
            SafeSetAnchor(dNextRect, new Vector2(1f, 0f), new Vector2(1f, 0f), ref isSceneDirty);
            SafeSetPivot(dNextRect, new Vector2(1f, 0f), ref isSceneDirty);
            SafeSetSizeDelta(dNextRect, new Vector2(100f, 40f), ref isSceneDirty);
            SafeSetAnchoredPosition(dNextRect, new Vector2(-20f, 15f), ref isSceneDirty);
            TextMeshProUGUI dNextText = dNextBtn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (!Mathf.Approximately(dNextText.fontSize, 16f)) { dNextText.fontSize = 16f; isSceneDirty = true; }

            // Dialogue Indicator (typewriter/prompt feedback)
            RectTransform dIndicatorRect = SetupPanel(dContentPanel, "Dialogue_Indicator", ref isSceneDirty);
            SafeSetAnchor(dIndicatorRect, new Vector2(1f, 0f), new Vector2(1f, 0f), ref isSceneDirty);
            SafeSetPivot(dIndicatorRect, new Vector2(1f, 0f), ref isSceneDirty);
            SafeSetSizeDelta(dIndicatorRect, new Vector2(25f, 25f), ref isSceneDirty);
            SafeSetAnchoredPosition(dIndicatorRect, new Vector2(-135f, 22f), ref isSceneDirty);
            Image dIndicatorImg = dIndicatorRect.gameObject.GetComponent<Image>();
            if (dIndicatorImg == null)
            {
                dIndicatorImg = dIndicatorRect.gameObject.AddComponent<Image>();
                isSceneDirty = true;
            }
            var dIndicatorSprite = LoadSprite("UI_Dialogue_Indicator");
            if (dIndicatorImg.sprite != dIndicatorSprite)
            {
                dIndicatorImg.sprite = dIndicatorSprite;
                isSceneDirty = true;
            }
            ConfigureSimpleImage(dIndicatorImg, true, ref isSceneDirty);

            // Wire CozyDialoguePopup
            SerializedObject soDPopup = new SerializedObject(dialoguePopup);
            bool dPopupDirty = false;
            SafeSetObjectReference(soDPopup.FindProperty("_contentPanel"), dContentPanel, ref dPopupDirty);
            SafeSetObjectReference(soDPopup.FindProperty("_npcNameText"), dNameText, ref dPopupDirty);
            SafeSetObjectReference(soDPopup.FindProperty("_npcPortraitImage"), dPortrait, ref dPopupDirty);
            SafeSetObjectReference(soDPopup.FindProperty("_dialogueText"), dDialogueText, ref dPopupDirty);
            SafeSetObjectReference(soDPopup.FindProperty("_nextButton"), dNextBtn, ref dPopupDirty);
            SafeSetObjectReference(soDPopup.FindProperty("_dialogueIndicator"), dIndicatorImg, ref dPopupDirty);

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
        }

        // Helper to find Prefabs_Holder from current hierarchy
        private Transform GetPrefabsHolderTransform()
        {
            GameObject holder = GameObject.Find("Prefabs_Holder");
            if (holder == null)
            {
                Canvas canvas = GameObject.FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    holder = new GameObject("Prefabs_Holder");
                    holder.transform.SetParent(canvas.transform, false);
                    holder.AddComponent<RectTransform>();
                }
            }
            return holder != null ? holder.transform : null;
        }
    }
}
