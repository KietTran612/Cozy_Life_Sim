using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

namespace CozyLifeSim.UI
{
    public class CozySticker : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private int _stickerId;
        [SerializeField] private RectTransform _shadowOffset;
        [SerializeField] private CanvasGroup _canvasGroup;

        private Vector3 _startPosition;
        private Transform _originalParent;
        private Canvas _canvas;
        private RectTransform _rectTransform;
        private Tween _scaleTween;
        private Tween _shadowTween;
        private bool _isDragging;

        public int StickerId => _stickerId;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _startPosition = _rectTransform.anchoredPosition;
            _originalParent = transform.parent;
            _canvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
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
            if (!_isDragging || _canvas == null) return;
            
            // Move using canvas scale factor
            _rectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
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

        public void FinalizePlacement(Transform pageParent, Vector2 pageAnchoredPosition)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.alpha = 1.0f;
            }

            transform.SetParent(pageParent, true);
            _rectTransform.anchoredPosition = pageAnchoredPosition;
            
            // Update fallback targets to prevent snapping back to original inventory tray on future invalid drops
            _originalParent = pageParent;
            _startPosition = pageAnchoredPosition;

            // Return to physical state
            _scaleTween = transform.DOScale(1.0f, 0.2f).SetEase(Ease.OutQuad);
            if (_shadowOffset != null)
            {
                _shadowTween = TweenAnchorPos(_shadowOffset, new Vector2(-3f, -4f), 0.2f).SetEase(Ease.OutQuad);
            }
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
