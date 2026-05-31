using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using VContainer; // Required DI import
using VContainer.Unity; // Required LifetimeScope import
using CozyLifeSim.UI.Presenters; // Required Presenter import

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

        [Inject]
        public void Construct(StickerBookPresenter presenter)
        {
            _presenter = presenter;
        }

        private bool _isInitialized;

        private void EnsureInitialized()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            _rectTransform = GetComponent<RectTransform>();
            _startPosition = _rectTransform != null ? (Vector3)_rectTransform.anchoredPosition : Vector3.zero;
            _originalParent = transform.parent;
            _canvas = GetComponentInParent<Canvas>();
            _startScale = transform.localScale;
            _startRotation = transform.localRotation;
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Start()
        {
            EnsurePresenterInjected();
        }

        private void EnsurePresenterInjected()
        {
            if (_presenter != null || !Application.isPlaying) return;

            var scope = LifetimeScope.Find<GameLifetimeScope>();
            if (scope != null && scope.Container != null)
            {
                scope.Container.Inject(this);
            }
        }

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

        public void OnBeginDrag(PointerEventData eventData)
        {
            EnsureInitialized();
            if (_canvas == null) return;
            _isDragging = true;

            _scaleTween?.Kill();
            _shadowTween?.Kill();

            // Upscale sticker on lift
            _scaleTween = transform.DOScale(1.1f, 0.15f).SetEase(Ease.OutBack);

            // Translate shadow downwards to mock physical depth/lift
            if (_shadowOffset != null)
            {
                _shadowTween = TweenAnchorPos(_shadowOffset, new Vector2(-10f, -15f), 0.15f).SetEase(Ease.OutQuad);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.alpha = 0.8f;
            }

            // Move to top hierarchy of canvas to prevent clipping
            transform.SetParent(_canvas.transform, true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            EnsureInitialized();
            if (!_isDragging || _canvas == null) return;
            
            // Move using canvas scale factor
            _rectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            EnsureInitialized();
            if (!_isDragging) return;
            _isDragging = false;

            _scaleTween?.Kill();
            _shadowTween?.Kill();

            // Check if dropped over a valid drop target (e.g. StickerBookPage)
            var droppedOn = eventData.pointerEnter;
            bool isValidDrop = false;

            if (droppedOn != null)
            {
                var page = droppedOn.GetComponentInParent<StickerBookPage>();
                if (page != null)
                {
                    isValidDrop = page.TryPlaceSticker(this, eventData.position, eventData.pressEventCamera);
                }
            }

            if (!isValidDrop)
            {
                // Smoothly snap back to inventory tray if dropped invalidly
                if (_canvasGroup != null)
                {
                    _canvasGroup.blocksRaycasts = true;
                    _canvasGroup.alpha = 1.0f;
                }

                transform.SetParent(_originalParent, true);
                TweenAnchorPos(_rectTransform, _startPosition, 0.3f).SetEase(Ease.OutQuad);
                _scaleTween = transform.DOScale(1.0f, 0.3f).SetEase(Ease.OutQuad);

                if (_shadowOffset != null)
                {
                    _shadowTween = TweenAnchorPos(_shadowOffset, Vector2.zero, 0.3f).SetEase(Ease.OutQuad);
                }
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

        private static Tween TweenAnchorPos(RectTransform rect, Vector2 endValue, float duration)
        {
            return DOTween.To(
                () => rect.anchoredPosition,
                pos => rect.anchoredPosition = pos,
                endValue,
                duration);
        }

        private void OnDestroy()
        {
            _scaleTween?.Kill();
            _shadowTween?.Kill();
        }
    }
}
