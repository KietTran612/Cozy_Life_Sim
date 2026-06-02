using CozyLifeSim.Core;
using CozyLifeSim.UI.Presenters;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CozyLifeSim.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class CozyDiaryNote : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private CanvasGroup _canvasGroup;

        private RectTransform _rectTransform;
        private Canvas _canvas;
        private StickerBookPresenter _presenter;
        private string _noteId;
        private int _pageIndex;
        private Vector2 _lastSavedPosition;
        private Tween _snapBackTween;

        public string NoteId => _noteId;
        public int PageIndex => _pageIndex;

        private void Awake()
        {
            EnsureInitialized();
        }

        public void Setup(StickerBookPresenter presenter, DiaryNotePlacedData data)
        {
            EnsureInitialized();
            _presenter = presenter;
            _noteId = data.NoteId;
            _pageIndex = data.PageIndex;
            _lastSavedPosition = new Vector2(data.PositionX, data.PositionY);
            _rectTransform.anchoredPosition = _lastSavedPosition;
            if (_text != null)
            {
                _text.text = data.Text;
            }
        }

        public void ApplySavedPosition(Vector2 position)
        {
            EnsureInitialized();
            _lastSavedPosition = position;
            _rectTransform.anchoredPosition = position;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            EnsureInitialized();
            _snapBackTween?.Kill();
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0.85f;
                _canvasGroup.blocksRaycasts = false;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            EnsureInitialized();
            float scaleFactor = _canvas != null && _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f;
            _rectTransform.anchoredPosition += eventData.delta / scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            EnsureInitialized();
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
            }

            Vector2 targetPosition = _rectTransform.anchoredPosition;
            if (_presenter != null && _presenter.TryUpdateDiaryNotePosition(_noteId, targetPosition.x, targetPosition.y))
            {
                _lastSavedPosition = targetPosition;
                return;
            }

            SnapBack();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (eventData.clickCount == 2 && _presenter != null)
            {
                _presenter.TryRemoveDiaryNote(_noteId);
            }
        }

        private void SnapBack()
        {
            _snapBackTween?.Kill();
            _snapBackTween = DOTween.To(
                () => _rectTransform.anchoredPosition,
                value => _rectTransform.anchoredPosition = value,
                _lastSavedPosition,
                0.2f).SetEase(Ease.OutQuad);
        }

        private void EnsureInitialized()
        {
            if (_rectTransform != null) return;
            _rectTransform = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
        }

        private void OnDisable()
        {
            _snapBackTween?.Kill();
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
            }
        }

        private void OnDestroy()
        {
            _snapBackTween?.Kill();
        }
    }
}
