using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace CozyLifeSim.UI
{
    public class CozyPopup : MonoBehaviour
    {
        [SerializeField] protected RectTransform _contentPanel;
        [SerializeField] protected CanvasGroup _backgroundDim;
        [SerializeField] protected Button _closeButton;

        protected bool _isOpen;
        protected Tween _fadeTween;
        protected Tween _scaleTween;
        protected Tween _closeDelayTween;

        public bool IsOpen => _isOpen;

        protected virtual void Start()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Close);
            }
        }

        protected virtual void OnDestroy()
        {
            _fadeTween?.Kill();
            _scaleTween?.Kill();
            _closeDelayTween?.Kill();

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(Close);
            }
        }

        public virtual void Open()
        {
            _isOpen = true;
            gameObject.SetActive(true);

            // Reentrancy: Kill existing tweens
            _fadeTween?.Kill();
            _scaleTween?.Kill();
            _closeDelayTween?.Kill();

            // Set initial state
            if (_backgroundDim != null)
            {
                _backgroundDim.alpha = 0f;
                _backgroundDim.blocksRaycasts = true;
                _fadeTween = _backgroundDim.DOFade(1f, 0.2f).SetUpdate(true);
            }

            if (_contentPanel != null)
            {
                _contentPanel.localScale = new Vector3(0.8f, 0.8f, 1f);
                _scaleTween = _contentPanel.DOScale(1f, 0.3f)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true);
            }

            OnOpen();
        }

        public virtual void Close()
        {
            _isOpen = false;

            // Reentrancy: Kill existing tweens
            _fadeTween?.Kill();
            _scaleTween?.Kill();

            float duration = 0.2f;

            if (_backgroundDim != null)
            {
                _fadeTween = _backgroundDim.DOFade(0f, duration).SetUpdate(true);
            }

            if (_contentPanel != null)
            {
                _scaleTween = _contentPanel.DOScale(0.8f, duration)
                    .SetEase(Ease.InBack)
                    .SetUpdate(true);
            }

            // Deactivate gameObject after transitions complete
            _closeDelayTween = DOVirtual.DelayedCall(duration, () =>
            {
                if (!_isOpen)
                {
                    if (_backgroundDim != null)
                    {
                        _backgroundDim.blocksRaycasts = false;
                    }
                    gameObject.SetActive(false);
                }
            }).SetUpdate(true);

            OnClose();
        }

        protected virtual void OnOpen() { }
        protected virtual void OnClose() { }
    }
}
