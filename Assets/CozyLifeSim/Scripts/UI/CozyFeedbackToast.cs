using DG.Tweening;
using TMPro;
using UnityEngine;

namespace CozyLifeSim.UI
{
    public class CozyFeedbackToast : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _contentPanel;
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private float _visibleSeconds = 1.4f;

        private Sequence _sequence;

        public void Show(string message)
        {
            string resolved = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();
            if (string.IsNullOrEmpty(resolved)) return;

            if (_messageText != null) _messageText.text = resolved;
            if (_contentPanel != null) _contentPanel.gameObject.SetActive(true);
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
                _canvasGroup.alpha = 0f;
            }

            _sequence?.Kill();
            _sequence = DOTween.Sequence().SetTarget(this);
            if (_contentPanel != null)
            {
                _contentPanel.localScale = new Vector3(0.96f, 0.96f, 1f);
                _sequence.Join(_contentPanel.DOScale(1f, 0.12f).SetEase(Ease.OutBack));
            }
            if (_canvasGroup != null)
            {
                _sequence.Join(_canvasGroup.DOFade(1f, 0.12f));
                _sequence.AppendInterval(Mathf.Max(0.1f, _visibleSeconds));
                _sequence.Append(_canvasGroup.DOFade(0f, 0.18f));
            }
            _sequence.OnComplete(() =>
            {
                if (_contentPanel != null) _contentPanel.gameObject.SetActive(false);
            });
        }

        private void Awake()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }
            if (_contentPanel != null) _contentPanel.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _sequence?.Kill();
            DOTween.Kill(this);
        }
    }
}
