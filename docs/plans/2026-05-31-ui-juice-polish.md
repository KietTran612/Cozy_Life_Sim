# UI, Shop Tabs & Juice Polish Implementation Plan

> **For Antigravity:** REQUIRED WORKFLOW: Use `.agent/workflows/execute-plan.md` to execute this plan in single-flow mode.

**Goal:** Trien khai phan tab Shop muot ma voi DOTween, tray sticker cuon ngang hien thi so luong (countable tray), cu cu chi double-click de thu hoi sticker ve kho (Return/Remove UX) voi RAM transaction an toan va hieu ung bay xu (Juicy coin fly) cực kỳ sinh dong.

**Architecture:** Cap nhat StickerBookPresenter chu dung cac transaction an toan cho viec dan va thu hoi sticker. Bo sung Text hien thi so luong vao CozySticker, bat su kien double-click qua IPointerClickHandler. Them he thong tab chuyen doi muot ma bang CanvasGroup/DOTween va script phat xu bay tuyen tinh trong ShopPopup.

**Tech Stack:** C#, Unity 6000.3.11f1, VContainer, DOTween, TextMeshPro, C# Unit Tests.

---

### Task 1: Presenter Atomic Placements & Refund Transactions

**Files:**
- Modify: [StickerBookPresenter.cs](file:///c:/1.SOURCE/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Presenters/StickerBookPresenter.cs)
- Test: [CozyLifeSimValidation.cs](file:///c:/1.SOURCE/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimValidation.cs)

**Step 1: Write the failing test for Presenter placement and refund**
Mở file `CozyLifeSimValidation.cs` và thêm khối kiểm tra Test 11.16 trực tiếp vào phương thức `RunTests()` (sau Test 11.15) để kiểm tra các phương thức `TryPlaceSticker` và `TryReturnSticker` với cơ chế rollback RAM:
*(Sử dụng khối mã Test 11.16 đầy đủ được mô tả chi tiết tại Task 6 Step 1. Không dùng thuộc tính `[Test]` vì `CozyLifeSimValidation.cs` chạy kiểm thử thủ công qua static menu method)*

**Step 2: Run test to verify it fails**
Chạy logic tests thông qua menu editor `Tools/CozySim/Run Logic Verification Tests` và xác nhận biên dịch lỗi vì các phương thức này chưa tồn tại trong presenter hoặc test block biên dịch lỗi.

**Step 3: Update StickerBookPresenter.cs with transactions**
Sửa đổi constructor để inject thêm `IInventoryService` và `ISaveService`, đồng thời triển khai `TryPlaceSticker` và `TryReturnSticker`:
```csharp
using System;
using System.Collections.Generic;
using VContainer;
using CozyLifeSim.Core;

namespace CozyLifeSim.UI.Presenters
{
    public class StickerBookPresenter : IDisposable
    {
        private readonly IMemoryService _memory;
        private readonly IInventoryService _inventory;
        private readonly ISaveService _saveService;

        [Inject]
        public StickerBookPresenter(IMemoryService memory, IInventoryService inventory, ISaveService saveService)
        {
            _memory = memory;
            _inventory = inventory;
            _saveService = saveService;
        }

        public IReadOnlyList<StickerPlacedData> GetPlacedStickers()
        {
            return _memory.PlacedStickers;
        }

        public string TryPlaceSticker(int stickerId, int pageIndex, float x, float y, float scale, float rot)
        {
            if (_inventory.ConsumeStickerNonSaving(stickerId))
            {
                var data = new StickerPlacedData(stickerId, pageIndex, x, y, scale, rot);
                data = _memory.AddPlacedStickerNonSaving(data);

                try
                {
                    _saveService.Save();
                    return data.PlacementId;
                }
                catch (Exception)
                {
                    // Rollback
                    _inventory.AddStickerCountNonSaving(stickerId, 1);
                    _memory.TryRemovePlacedStickerNonSaving(data.PlacementId, out _);
                }
            }
            return null;
        }

        public bool TryReturnSticker(string placementId)
        {
            if (string.IsNullOrEmpty(placementId)) return false;

            if (_memory.TryRemovePlacedStickerNonSaving(placementId, out var removedData))
            {
                _inventory.AddStickerCountNonSaving(removedData.StickerId, 1);

                try
                {
                    _saveService.Save();
                    return true;
                }
                catch (Exception)
                {
                    // Rollback
                    _inventory.ConsumeStickerNonSaving(removedData.StickerId);
                    _memory.AddPlacedStickerNonSaving(removedData);
                }
            }
            return false;
        }

        [Obsolete("Dung TryPlaceSticker thay the de dam bao transaction")]
        public void SaveStickerPosition(int stickerId, int pageIndex, float x, float y, float scale, float rot)
        {
            var data = new StickerPlacedData(stickerId, pageIndex, x, y, scale, rot);
            _memory.PlaceSticker(data);
        }

        [Obsolete("Dung TryReturnSticker de dam bao transaction")]
        public void RemoveSticker(int stickerId, int pageIndex)
        {
            _memory.RemoveSticker(stickerId, pageIndex);
        }

        public void Dispose() { }
    }
}
```

**Step 4: Run test to verify it passes**
Chạy lại logic tests thông qua menu editor và xác nhận Test 11.16 PASS.

**Step 5: Commit**
*(Chỉ stage và commit các thay đổi nếu được người dùng yêu cầu trực tiếp)*

---

### Task 2: CozySticker Count Badge & Double-Click Return Input

**Files:**
- Modify: [CozySticker.cs](file:///c:/1.SOURCE/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/CozySticker.cs)

**Step 1: Write mock tests or verify compilation**
Khai báo các sự kiện static lifecycle và các trường private lưu trạng thái số lượng badge hiển thị ban đầu:
- `public static event System.Action<CozySticker> OnStickerPlacedOnPage;`
- `public static event System.Action OnStickerReturnedToInventory;`
- `[SerializeField] private TMPro.TextMeshProUGUI _countText;`
- `private string _placementId;`
- `private int _ownedCount = 1;`
- `private bool _showCountBadge = false;`
- Property: `public string PlacementId { get; set; }`

**Step 2: Update Setup signature & IPointerClickHandler implementation**
Triển khai `IPointerClickHandler` để nhận dạng double click, lưu trữ trạng thái hiển thị số lượng của khay ban đầu và helper `UpdateCountText` khôi phục badge:
```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using VContainer;
using VContainer.Unity;
using CozyLifeSim.UI.Presenters;

namespace CozyLifeSim.UI
{
    public class CozySticker : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public static event System.Action<CozySticker> OnStickerPlacedOnPage;
        public static event System.Action OnStickerReturnedToInventory;

        [SerializeField] private int _stickerId;
        [SerializeField] private RectTransform _shadowOffset;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _visualImage;
        [SerializeField] private TMPro.TextMeshProUGUI _countText;

        private Vector3 _startPosition;
        private Transform _originalParent;
        private Canvas _canvas;
        private RectTransform _rectTransform;
        private Tween _scaleTween;
        private Tween _shadowTween;
        private bool _isDragging;
        private Vector3 _startScale;
        private Quaternion _startRotation;
        private StickerBookPresenter _presenter;
        private int _pageIndex;
        private string _placementId;

        private int _ownedCount = 1;
        private bool _showCountBadge = false;

        public int StickerId => _stickerId;
        public string PlacementId 
        { 
            get => _placementId; 
            set => _placementId = value; 
        }

        public void Setup(int stickerId, Sprite mainSprite, Sprite shadowSprite, int count = 1, bool showCount = false)
        {
            EnsureInitialized();
            _stickerId = stickerId;
            _ownedCount = count;
            _showCountBadge = showCount;

            Image targetImg = _visualImage;
            if (targetImg == null)
            {
                Transform visualChild = transform.Find("Visual_Image");
                targetImg = visualChild != null ? visualChild.GetComponent<Image>() : GetComponent<Image>();
            }

            if (targetImg != null)
            {
                targetImg.sprite = mainSprite;
            }

            if (_shadowOffset != null)
            {
                var shadowImg = _shadowOffset.GetComponent<Image>();
                if (shadowImg != null)
                {
                    shadowImg.sprite = shadowSprite != null ? shadowSprite : mainSprite;
                    shadowImg.color = new Color(0f, 0f, 0f, 0.3f);
                }
            }

            UpdateCountText();
        }

        private void UpdateCountText()
        {
            if (_countText != null)
            {
                _countText.gameObject.SetActive(_showCountBadge && _ownedCount > 1);
                _countText.text = $"x{_ownedCount}";
            }
        }
```

Cập nhật `FinalizePlacement` để gọi `TryPlaceSticker` (với sự kiện `OnStickerPlacedOnPage`), `OnPointerClick` để thu hồi sticker (với sự kiện `OnStickerReturnedToInventory`), và khôi phục badge số lượng trong `ResetToTray`:
```csharp
        public void OnPointerClick(PointerEventData eventData)
        {
            EnsureInitialized();
            if (eventData.button != PointerEventData.InputButton.Left) return;

            if (eventData.clickCount == 2)
            {
                TryReturnToInventory();
            }
        }

        public void TryReturnToInventory()
        {
            if (_presenter == null || string.IsNullOrEmpty(_placementId)) return;

            if (_presenter.TryReturnSticker(_placementId))
            {
                // Bắn event thông báo thu hồi thành công trước khi đối tượng bị tiêu hủy
                OnStickerReturnedToInventory?.Invoke();

                // Juice thu hoi: xuyen bien mat bang DOTween roi destroy object
                transform.DOScale(0f, 0.25f).SetEase(Ease.InBack).OnComplete(() => {
                    Destroy(gameObject);
                });
            }
        }

        public void FinalizePlacement(Transform pageParent, Vector2 pageAnchoredPosition, int pageIndex, bool saveToDisk = true)
        {
            EnsureInitialized();
            EnsurePresenterInjected();
            _pageIndex = pageIndex;
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.alpha = 1.0f;
            }

            // Capture the original tray parent and position before moving the object
            Transform oldTrayParent = _originalParent;
            Vector3 oldTrayPos = _startPosition;

            transform.SetParent(pageParent, true);
            _rectTransform.anchoredPosition = pageAnchoredPosition;

            _scaleTween = transform.DOScale(1.0f, 0.2f).SetEase(Ease.OutQuad);
            if (_shadowOffset != null)
            {
                _shadowTween = TweenAnchorPos(_shadowOffset, new Vector2(-3f, -4f), 0.2f).SetEase(Ease.OutQuad);
            }

            if (_countText != null)
            {
                _countText.gameObject.SetActive(false); // An count badge khi da dan len sach
            }

            if (saveToDisk && _presenter != null)
            {
                string pId = _presenter.TryPlaceSticker(_stickerId, _pageIndex, pageAnchoredPosition.x, pageAnchoredPosition.y, 1.0f, transform.localRotation.eulerAngles.z);
                if (string.IsNullOrEmpty(pId))
                {
                    // Dán lỗi (hết count hoặc save crash) -> snap back về khay (dùng các biến local cũ trước khi di chuyển)
                    ResetToTray(oldTrayParent, oldTrayPos);
                    return;
                }
                _placementId = pId;

                // Bắn event thông báo đã dán thành công lên trang sách
                OnStickerPlacedOnPage?.Invoke(this);
            }

            // Cập nhật các thuộc tính fallback chỉ KHI dán/lưu thành công hoặc dán tự do
            _originalParent = pageParent;
            _startPosition = pageAnchoredPosition;
        }

        public void ResetToTray(Transform trayParent, Vector2 trayPosition)
        {
            EnsureInitialized();
            
            _scaleTween?.Kill();
            _shadowTween?.Kill();

            transform.SetParent(trayParent, false);
            _rectTransform.anchoredPosition = trayPosition;
            _originalParent = trayParent;
            _startPosition = trayPosition;
            _pageIndex = 0;
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.alpha = 1.0f;
            }
            transform.localScale = _startScale;
            transform.localRotation = _startRotation;
            if (_shadowOffset != null)
            {
                _shadowOffset.anchoredPosition = Vector2.zero;
            }

            // Khôi phục chính xác trạng thái hiển thị số lượng badge ban đầu khi snap back về tray
            UpdateCountText();
        }
```

Xác nhận compilation thành công thông qua trình kiểm tra của Unity Editor.

**Step 4: Commit**
*(Chỉ stage và commit các thay đổi nếu được người dùng yêu cầu trực tiếp)*

---

### Task 3: StickerBook Count-Displaying, Placement Lifecycle & Safe Tray Refresh

**Files:**
- Modify: [StickerBook.cs](file:///c:/1.SOURCE/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/StickerBook.cs)

**Step 1: Subscribe to safe lifecycle events and update spawning count injection**
Sửa đổi `StickerBook.cs` để đăng ký sự kiện dán/thu hồi an toàn nhằm tránh phá hủy placed stickers khi khay refresh, đồng thời truyền owned count khi sinh khay:
- Đăng ký sự kiện trong `Start()`:
```csharp
            // Lắng nghe sự kiện từ CozySticker để quản lý vòng đời sticker an toàn và cập nhật tray count
            CozySticker.OnStickerPlacedOnPage += HandleStickerPlacedOnPage;
            CozySticker.OnStickerReturnedToInventory += SpawnDynamicStickers;
```
- Hủy đăng ký sự kiện trong `OnDestroy()`:
```csharp
            CozySticker.OnStickerPlacedOnPage -= HandleStickerPlacedOnPage;
            CozySticker.OnStickerReturnedToInventory -= SpawnDynamicStickers;
```
- Triển khai phương thức xử lý dán sticker lên trang sách:
```csharp
        private void HandleStickerPlacedOnPage(CozySticker sticker)
        {
            if (sticker != null && _spawnedInventoryStickers.Contains(sticker))
            {
                // Gỡ sticker khỏi list spawned của khay trước khi thực hiện refresh tray
                _spawnedInventoryStickers.Remove(sticker);
            }
            // Refresh lại khay để cập nhật count badge còn lại của loại sticker đó
            SpawnDynamicStickers();
        }
```
- Cập nhật phương thức `SpawnDynamicStickers()` để đếm số lượng và gán PlacementId khi khôi phục:
```csharp
        private void SpawnDynamicStickers()
        {
            if (_stickerDatabase == null || _inventoryTrayRoot == null || _stickerPrefabTemplate == null) return;
            if (_stickerDatabase.Stickers == null) return;

            foreach (var spawned in _spawnedInventoryStickers)
            {
                if (spawned != null)
                {
                    Destroy(spawned.gameObject);
                }
            }
            _spawnedInventoryStickers.Clear();

            // Hide the original editor setup template
            _stickerPrefabTemplate.gameObject.SetActive(false);

            foreach (var template in _stickerDatabase.Stickers)
            {
                if (template == null || template.Sprite == null) continue;

                int ownedCount = _inventoryService != null ? _inventoryService.GetStickerCount(template.StickerId) : 0;
                if (ownedCount <= 0)
                {
                    continue; // An sticker khoi khay neu so luong = 0
                }

                var sticker = Instantiate(_stickerPrefabTemplate, _inventoryTrayRoot);
                sticker.gameObject.SetActive(true);

                sticker.Setup(template.StickerId, template.Sprite, template.ShadowSprite, ownedCount, true);
                _spawnedInventoryStickers.Add(sticker);
            }
        }
```
- Trong `RestoreStickers()`:
```csharp
                // Config graphics dynamically
                spawned.Setup(template.StickerId, template.Sprite, template.ShadowSprite, 1, false);
                spawned.PlacementId = item.PlacementId; // Giữ lại PlacementId duy nhất
```

**Step 2: Verify compilation**
Chạy logic tests để xác nhận biên dịch sạch sẽ.

**Step 3: Commit**
*(Chỉ stage và commit các thay đổi nếu được người dùng yêu cầu trực tiếp)*

---

### Task 4: ShopPopup Category Tabs & Coin Fly Visual Feedback

**Files:**
- Modify: [ShopPopup.cs](file:///c:/1.SOURCE/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/ShopPopup.cs)

**Step 1: Write implementation for Shop Tabs & DOTween coin fly in ShopPopup.cs**
*Đầu tiên, thêm các `using UnityEngine.UI;` và `using DG.Tweening;` vào đầu file `ShopPopup.cs` (nếu chưa có). Đồng thời, thêm private field `private IProgressionService _progressionService;` và cập nhật hàm `Construct` để inject thêm `IProgressionService progressionService` và gán `_progressionService = progressionService;` vào class.*
Thêm tab buttons, panels, và logic chuyển đổi CanvasGroup muot ma kết hợp hiệu ứng bay xu:
- Thêm trường `[SerializeField]`:
```csharp
        [Header("Tabs Configuration")]
        [SerializeField] private Button _seedsTabButton;
        [SerializeField] private Button _stickersTabButton;
        [SerializeField] private Button _cropsTabButton;
        [SerializeField] private RectTransform _seedsGroup;
        [SerializeField] private RectTransform _stickersGroup;
        [SerializeField] private RectTransform _cropsGroup;
```
- Triển khai chuyển tab trong `Start()` và các hàm liên quan:
```csharp
        private enum ShopTab { Seeds, Stickers, Crops }
        private ShopTab _currentTab = ShopTab.Seeds;

        protected override void Start()
        {
            base.Start();

            if (_itemPrefabTemplate != null)
            {
                _itemPrefabTemplate.gameObject.SetActive(false);
            }

            if (Application.isPlaying)
            {
                var scope = LifetimeScope.Find<GameLifetimeScope>();
                if (scope != null && scope.Container != null)
                {
                    scope.Container.Inject(this);
                }
            }

            // Tab listeners
            if (_seedsTabButton != null) _seedsTabButton.onClick.AddListener(SetSeedsTab);
            if (_stickersTabButton != null) _stickersTabButton.onClick.AddListener(SetStickersTab);
            if (_cropsTabButton != null) _cropsTabButton.onClick.AddListener(SetCropsTab);

            SetTab(ShopTab.Seeds, false); // Default tab
            RefreshShop();
        }

        private void SetSeedsTab() => SetTab(ShopTab.Seeds);
        private void SetStickersTab() => SetTab(ShopTab.Stickers);
        private void SetCropsTab() => SetTab(ShopTab.Crops);

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_seedsTabButton != null) _seedsTabButton.onClick.RemoveListener(SetSeedsTab);
            if (_stickersTabButton != null) _stickersTabButton.onClick.RemoveListener(SetStickersTab);
            if (_cropsTabButton != null) _cropsTabButton.onClick.RemoveListener(SetCropsTab);

            // Target-based DOTween cleanup to prevent memory leaks or console warnings
            if (_seedsTabButton != null) _seedsTabButton.transform.DOKill();
            if (_stickersTabButton != null) _stickersTabButton.transform.DOKill();
            if (_cropsTabButton != null) _cropsTabButton.transform.DOKill();
            
            // Clean up group canvas tween fades (cg target)
            if (_seedsGroup != null) { var cg = _seedsGroup.GetComponent<CanvasGroup>(); if (cg != null) cg.DOKill(); }
            if (_stickersGroup != null) { var cg = _stickersGroup.GetComponent<CanvasGroup>(); if (cg != null) cg.DOKill(); }
            if (_cropsGroup != null) { var cg = _cropsGroup.GetComponent<CanvasGroup>(); if (cg != null) cg.DOKill(); }

            DOTween.Kill(this);

            if (_isSubscribed && _inventoryService != null)
            {
                _inventoryService.OnCoinsChanged -= OnCoinsChanged;
                _inventoryService.OnSeedsChanged -= OnSeedsChanged;
                _inventoryService.OnCropsChanged -= OnCropsChanged;
                _inventoryService.OnInventoryReloaded -= OnInventoryReloaded;
                _isSubscribed = false;
            }

            if (_presenter != null)
            {
                _presenter.OnShopTransactionSuccess -= RefreshShop;
            }
        }

        private void SetTab(ShopTab tab, bool animate = true)
        {
            _currentTab = tab;

            HighlightTabButton(_seedsTabButton, tab == ShopTab.Seeds, animate);
            HighlightTabButton(_stickersTabButton, tab == ShopTab.Stickers, animate);
            HighlightTabButton(_cropsTabButton, tab == ShopTab.Crops, animate);

            TransitionGroup(_seedsGroup, tab == ShopTab.Seeds, animate);
            TransitionGroup(_stickersGroup, tab == ShopTab.Stickers, animate);
            TransitionGroup(_cropsGroup, tab == ShopTab.Crops, animate);
        }

        private void HighlightTabButton(Button btn, bool active, bool animate)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img == null) return;

            Color targetColor = active ? new Color(0.35f, 0.35f, 0.35f, 1.0f) : new Color(0.2f, 0.2f, 0.2f, 1.0f);
            float targetScale = active ? 1.05f : 0.95f;

            if (animate && Application.isPlaying)
            {
                img.DOColor(targetColor, 0.2f).SetTarget(this);
                btn.transform.DOScale(targetScale, 0.2f).SetTarget(this);
            }
            else
            {
                img.color = targetColor;
                btn.transform.localScale = new Vector3(targetScale, targetScale, 1.0f);
            }
        }

        private void TransitionGroup(RectTransform group, bool active, bool animate)
        {
            if (group == null) return;
            var cg = group.GetComponent<CanvasGroup>();
            if (cg == null) cg = group.gameObject.AddComponent<CanvasGroup>();

            float targetAlpha = active ? 1f : 0f;
            bool targetBlocks = active;

            if (animate && Application.isPlaying)
            {
                cg.DOFade(targetAlpha, 0.2f).SetTarget(this).OnStart(() => {
                    if (active) group.gameObject.SetActive(true);
                }).OnComplete(() => {
                    group.gameObject.SetActive(active);
                    cg.blocksRaycasts = targetBlocks;
                });
            }
            else
            {
                group.gameObject.SetActive(active);
                cg.alpha = targetAlpha;
                cg.blocksRaycasts = targetBlocks;
            }
        }

        public void PlayCoinFlyAnimation(Vector3 startWorldPos, Vector3 endWorldPos)
        {
            if (!Application.isPlaying) return;
            int coinCount = 6;
            
            for (int i = 0; i < coinCount; i++)
            {
                GameObject coinGo = new GameObject("CoinFly");
                coinGo.transform.SetParent(_contentPanel, false);
                coinGo.transform.position = startWorldPos;

                var text = coinGo.AddComponent<TextMeshProUGUI>();
                text.text = "\u25cf"; // Dùng Unicode escape sequence để tránh mojibake
                text.color = new Color(1f, 0.85f, 0f, 1f); // Gold yellow
                text.fontSize = 24f;
                text.alignment = TextAlignmentOptions.Center;

                Vector3 spread = new Vector3(Random.Range(-20f, 20f), Random.Range(-20f, 20f), 0f);
                coinGo.transform.position += spread / 100f;

                float delay = i * 0.04f;
                float duration = Random.Range(0.5f, 0.7f);

                Sequence seq = DOTween.Sequence().SetTarget(this);
                seq.AppendInterval(delay);
                
                Vector3 burstDir = (endWorldPos - startWorldPos).normalized;
                Vector3 perpendicular = new Vector3(-burstDir.y, burstDir.x, 0f);
                Vector3 popTarget = startWorldPos + (perpendicular * Random.Range(-40f, 40f) + burstDir * Random.Range(-20f, 20f)) / 100f;
                
                seq.Append(coinGo.transform.DOMove(popTarget, 0.15f).SetEase(Ease.OutQuad));
                seq.Append(coinGo.transform.DOMove(endWorldPos, duration).SetEase(Ease.InQuad));
                seq.Join(coinGo.transform.DOScale(0.5f, duration).SetEase(Ease.InQuad));
                
                seq.OnKill(() => {
                    if (coinGo != null) Destroy(coinGo);
                });
                
                seq.OnComplete(() => {
                    if (_playerCoinsText != null && endWorldPos == _playerCoinsText.transform.position)
                    {
                        _playerCoinsText.transform.DOPunchScale(new Vector3(0.08f, 0.08f, 0.08f), 0.12f, 10, 1f);
                    }
                });
            }
        }
```

- Cập nhật trong `RefreshShop()` để đóng các closures kích hoạt hiệu ứng xu bay khi TryBuy/TrySell thành công.
*Đặc biệt: Cho phép mua nhiều bản sao sticker thay vì lock "Unlocked". Trong `RefreshShop()`, thay thế kiểm tra `alreadyUnlocked` bằng hiển thị Owned Count của sticker nhưng vẫn cho phép mua thêm nếu đủ level và coin:*
```csharp
                    int ownedCount = _inventoryService != null ? _inventoryService.GetStickerCount(sticker.StickerId) : 0;
                    bool canBuy = _inventoryService != null && _inventoryService.Coins >= sticker.BuyPrice;
                    
                    int playerLevel = _progressionService != null ? _progressionService.PlayerLevel : 999;
                    if (playerLevel < sticker.RequiredLevel)
                    {
                        canBuy = false;
                    }
                    
                    string btnLabel = ownedCount > 0 ? $"Buy (x{ownedCount})" : "Buy";

                    widget.Setup(
                        sticker.Name,
                        sticker.Sprite,
                        sticker.BuyPrice,
                        btnLabel,
                        canBuy,
                        () => {
                            if (_presenter != null && _presenter.TryBuySticker(stickerId))
                            {
                                PlayCoinFlyAnimation(widget.transform.position, _playerCoinsText != null ? _playerCoinsText.transform.position : Vector3.zero);
                            }
                        }
                    );
```
Tương tự cho `TryBuySeed` (xu bay từ ví `_playerCoinsText != null ? _playerCoinsText.transform.position : Vector3.zero` tới widget) và `TrySellCrop` (xu bay từ widget tới ví `_playerCoinsText != null ? _playerCoinsText.transform.position : Vector3.zero`).

**Step 2: Verify compilation**
Đảm bảo dự án biên dịch sạch sẽ.

**Step 3: Commit**
*(Chỉ stage và commit các thay đổi nếu được người dùng yêu cầu trực tiếp)*

---

### Task 5: CozySceneSetupWindow Automation Upgrades

**Files:**
- Modify: [CozySceneSetupWindow.cs](file:///c:/1.SOURCE/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs)

**Step 1: Upgrade scene setup generation for Shop tabs & Sticker count text**
Sửa đổi `CozySceneSetupWindow.cs` để tự động khởi tạo tab buttons và stacking layout:
- Ở đầu `Setup Shop Popup` (line 595 onwards):
  - Hủy `HorizontalLayoutGroup` trên `Grids_Container` để các categories panel xếp đè lên nhau.
  - Neo `seedsGroup`, `stickersGroup`, và `cropsGroup` lấp đầy grid container (`StretchToFill`).
  - Thêm một `CanvasGroup` vào mỗi group.
  - Sinh 3 tab buttons `Tab_Seeds`, `Tab_Stickers`, `Tab_Crops` đặt tại vị trí đẹp trong `Content_Panel` (ở `y = 150f`).
- Wire các tab buttons và panels mới sinh vào `shopPopup` serialized properties:
```csharp
            soSPopup.FindProperty("_seedsTabButton").objectReferenceValue = seedsTabBtn;
            soSPopup.FindProperty("_stickersTabButton").objectReferenceValue = stickersTabBtn;
            soSPopup.FindProperty("_cropsTabButton").objectReferenceValue = cropsTabBtn;
            soSPopup.FindProperty("_seedsGroup").objectReferenceValue = seedsGroup;
            soSPopup.FindProperty("_stickersGroup").objectReferenceValue = stickersGroup;
            soSPopup.FindProperty("_cropsGroup").objectReferenceValue = cropsGroup;
```
- Sinh `Count_Text` bên trong generic `Sticker_Template` và wire vào `CozySticker`:
```csharp
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
```

**Step 2: Run scene setup silent validation**
Chạy lại CozyLifeSimValidation và xác nhận scene tự động sinh sạch sẽ, không có lỗi biên dịch.

**Step 3: Commit**
*(Chỉ stage và commit các thay đổi nếu được người dùng yêu cầu trực tiếp)*

---

### Task 6: Logic Verification & Integration Testing

**Files:**
- Modify: [CozyLifeSimValidation.cs](file:///c:/1.SOURCE/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimValidation.cs)

**Step 1: Implement Test 11.16 directly within the RunTests() method in CozyLifeSimValidation.cs**
*(Đặt trực tiếp vào bên trong phương thức `RunTests()`, sau test 11.15)*
```csharp
                // Test 11.16: StickerBookPresenter Atomic Placement & Refund
                SaveService testPresSave = new SaveService();
                testPresSave.ActiveSave.Coins = 100;
                testPresSave.ActiveSave.StickerOwned.Clear();
                testPresSave.ActiveSave.StickerOwned.Add(new StickerInventory(3, 2)); // 2 copies of ID 3
                testPresSave.Save();

                InventoryService testPresInv = new InventoryService(testPresSave);
                MemoryService testPresMem = new MemoryService(testPresSave);
                StickerBookPresenter testPresenter = new StickerBookPresenter(testPresMem, testPresInv, testPresSave);

                // 1. Success Path placement
                string pId = testPresenter.TryPlaceSticker(3, 1, 10f, 10f, 1f, 0f);
                if (string.IsNullOrEmpty(pId)) throw new System.Exception("Placement should succeed");
                if (testPresInv.GetStickerCount(3) != 1) throw new System.Exception("Count should decrement to 1");

                // 2. Simulated Save Failure during Placement (try-finally guarded)
                try
                {
                    testPresSave.ForceSaveFailure = true;
                    string failedPId = testPresenter.TryPlaceSticker(3, 1, 10f, 10f, 1f, 0f);
                    if (!string.IsNullOrEmpty(failedPId)) throw new System.Exception("Placement should fail under simulated IO save failure");
                    if (testPresInv.GetStickerCount(3) != 1) throw new System.Exception("Sticker count should remain 1 on failed placement rollback");
                }
                finally
                {
                    testPresSave.ForceSaveFailure = false; // Always restore
                }

                // 3. Success Refund path
                if (!testPresenter.TryReturnSticker(pId)) throw new System.Exception("Return sticker should succeed");
                if (testPresInv.GetStickerCount(3) != 2) throw new System.Exception("Count should refund to 2");

                // 4. Simulated Save Failure during Refund (try-finally guarded)
                // Place it successfully again first
                pId = testPresenter.TryPlaceSticker(3, 1, 10f, 10f, 1f, 0f);
                if (string.IsNullOrEmpty(pId)) throw new System.Exception("Repeated placement should succeed");
                
                try
                {
                    testPresSave.ForceSaveFailure = true;
                    bool failedReturn = testPresenter.TryReturnSticker(pId);
                    if (failedReturn) throw new System.Exception("Return sticker should fail under simulated IO save failure");
                    if (testPresInv.GetStickerCount(3) != 1) throw new System.Exception("Count should remain 1 on failed return rollback");

                    // Manual loop verification since PlacedStickers is IReadOnlyList and doesn't support .Exists
                    bool exists = false;
                    foreach (var s in testPresMem.PlacedStickers)
                    {
                        if (s.PlacementId == pId) exists = true;
                    }
                    if (!exists) throw new System.Exception("Placed sticker must still exist on failed return rollback");
                }
                finally
                {
                    testPresSave.ForceSaveFailure = false; // Always restore
                }

                passCount++;
                CozyValidationLog.Pass("CozySim Logic", "StickerBookPresenter atomic placement and return with simulated failure rollback verified");
```

**Step 2: Run the full validation pipeline**
Xác nhận toàn bộ các bước kiểm thử của dự án bằng cách thực hiện tuần tự trong Unity Editor:
1. Chạy static logic tests bằng cách chọn: **Tools** -> **CozySim** -> **Run Logic Verification Tests** (Xác nhận **19/19 tests PASS** - lưu ý đây là kết quả sau khi Task 24.5 đã hoàn thành với 18 tests thành công).
2. Chạy scene generation silent bằng cách chọn: **Tools** -> **CozySim** -> **Setup Test Scene Silent** (Xác nhận sinh thành công scene `Main.unity`).
3. Vào **Play Mode** trong Unity Editor, sau đó chọn: **Tools** -> **CozySim** -> **Run MCP Gameplay Loop Validation** (Xác nhận 17/17 runtime tests PASS và persistence phục hồi chuẩn xác).

**Step 2.5: Manual/Runtime Integration Testing for UI Lifecycle & Count UX**
Để đảm bảo vòng đời sticker, badge đếm số lượng, và luồng trả lại hoạt động hoàn hảo trên giao diện (Play Mode), thực hiện các bước kiểm tra thủ công sau trong Unity Editor:
1. **Mua sticker số lượng lớn**: Mở ShopPopup, chuyển sang tab Stickers, mua 2 sticker Bunny Pink (ID 1). Xác nhận khay StickerBook hiển thị sticker Bunny Pink với badge số lượng `x2` rõ nét.
2. **Kéo dán sticker**: Kéo 1 chiếc Bunny Pink từ khay dán lên trang sách (ví dụ Page 1).
   - *Xác nhận*: Sticker vừa dán lên trang sách không còn hiển thị badge số lượng. Khay StickerBook tự động refresh và hiển thị Bunny Pink còn lại với badge số lượng `x1`.
3. **Mô phỏng Shop Refresh & Reload**: Mở Shop và thực hiện mua bán bất kỳ (hoặc bấm Reload Inventory).
   - *Xác nhận*: Sticker Bunny Pink đã dán trên trang sách vẫn tồn tại nguyên vẹn ở vị trí cũ (không bị phá hủy/Destroy do refresh khay). Khay StickerBook tải lại mượt mà với badge `x1`.
4. **Double-click thu hồi sticker**: Double-click vào sticker Bunny Pink đang nằm trên trang sách.
   - *Xác nhận*: Sticker trên trang biến mất hoàn toàn (sau hiệu ứng co nhỏ Ease.InBack). Khay StickerBook tự động refresh và khôi phục badge của Bunny Pink trở về `x2`.
5. **Kiểm thử Snap Back và khôi phục badge khi lưu thất bại**:
   - Gây lỗi giả lập lưu đĩa (chẳng hạn bật cờ `ForceSaveFailure = true` trên Presenter) khi đang dán sticker.
   - *Xác nhận*: Kéo sticker ra trang sách và thả. Do lưu đĩa thất bại, sticker tự động snap back về khay và badge hiển thị số lượng được khôi phục đầy đủ (không bị ẩn mất badge).

**Step 3: Commit**
*(Chỉ stage và commit các thay đổi nếu được người dùng yêu cầu trực tiếp)*

---

## Execution Handoff

**Plan complete and saved to `docs/plans/2026-05-31-ui-juice-polish.md`.**
**Next step: run `.agent/workflows/execute-plan.md` to execute this plan task-by-task in single-flow mode.**
