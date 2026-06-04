using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CozyLifeSim.UI;

namespace CozyLifeSim.Editor
{
    public partial class CozySceneSetupWindow
    {
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
            Image seedsTabImg = seedsTabBtn.GetComponent<Image>();
            if (seedsTabImg != null)
            {
                var tabBtnBg = LoadSprite("UI_Tab_Button_Bg");
                if (seedsTabImg.sprite != tabBtnBg) { seedsTabImg.sprite = tabBtnBg; isSceneDirty = true; }
                ConfigureSlicedImage(seedsTabImg, ref isSceneDirty);
            }

            Button stickersTabBtn = SetupButton(sContentPanel, "Tab_Stickers", "Stickers", ref isSceneDirty);
            RectTransform stickersTabBtnRect = stickersTabBtn.GetComponent<RectTransform>();
            SafeSetAnchoredPosition(stickersTabBtnRect, new Vector2(0f, 160f), ref isSceneDirty);
            SafeSetSizeDelta(stickersTabBtnRect, new Vector2(120f, 35f), ref isSceneDirty);
            Image stickersTabImg = stickersTabBtn.GetComponent<Image>();
            if (stickersTabImg != null)
            {
                var tabBtnBg = LoadSprite("UI_Tab_Button_Bg");
                if (stickersTabImg.sprite != tabBtnBg) { stickersTabImg.sprite = tabBtnBg; isSceneDirty = true; }
                ConfigureSlicedImage(stickersTabImg, ref isSceneDirty);
            }

            Button cropsTabBtn = SetupButton(sContentPanel, "Tab_Crops", "Crops", ref isSceneDirty);
            RectTransform cropsTabBtnRect = cropsTabBtn.GetComponent<RectTransform>();
            SafeSetAnchoredPosition(cropsTabBtnRect, new Vector2(150f, 160f), ref isSceneDirty);
            SafeSetSizeDelta(cropsTabBtnRect, new Vector2(120f, 35f), ref isSceneDirty);
            Image cropsTabImg = cropsTabBtn.GetComponent<Image>();
            if (cropsTabImg != null)
            {
                var tabBtnBg = LoadSprite("UI_Tab_Button_Bg");
                if (cropsTabImg.sprite != tabBtnBg) { cropsTabImg.sprite = tabBtnBg; isSceneDirty = true; }
                ConfigureSlicedImage(cropsTabImg, ref isSceneDirty);
            }

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
            RectTransform shopItemTemplate = SetupPanel(GetPrefabsHolderTransform(ref isSceneDirty), "Shop_Item_Template", ref isSceneDirty);
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
            var defaultItemIconSprite = LoadSprite("System_Icon_Seeds");
            if (itemIcon.sprite != defaultItemIconSprite)
            {
                itemIcon.sprite = defaultItemIconSprite;
                isSceneDirty = true;
            }
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

    }
}
