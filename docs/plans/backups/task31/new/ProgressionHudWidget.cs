using CozyLifeSim.Core;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace CozyLifeSim.UI
{
    public class ProgressionHudWidget : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private Image _xpProgressBar;

        private IProgressionService _progressionService;
        private Tween _fillTween;
        private Tween _punchTween;

        [Inject]
        public void Construct(IProgressionService progressionService)
        {
            _progressionService = progressionService;
        }

        private void Start()
        {
            if (_progressionService == null)
            {
                return;
            }

            _progressionService.OnLevelUp += OnLevelUp;
            _progressionService.OnXPChanged += OnXPChanged;

            UpdateUI(true);
        }

        private void OnDestroy()
        {
            if (_progressionService != null)
            {
                _progressionService.OnLevelUp -= OnLevelUp;
                _progressionService.OnXPChanged -= OnXPChanged;
            }

            _fillTween?.Kill();
            _punchTween?.Kill();
        }

        private void OnLevelUp(int newLevel)
        {
            if (_levelText != null)
            {
                _levelText.text = $"Level: {newLevel}";
                _punchTween?.Kill();
                _levelText.transform.localScale = Vector3.one;
                _punchTween = _levelText.transform.DOPunchScale(Vector3.one * 0.25f, 0.4f, 10, 1f).SetTarget(this);
            }

            UpdateXPFill(false);
        }

        private void OnXPChanged(int currentXP)
        {
            UpdateXPFill(false);
        }

        private void UpdateUI(bool immediate)
        {
            if (_progressionService == null)
            {
                return;
            }

            if (_levelText != null)
            {
                _levelText.text = $"Level: {_progressionService.PlayerLevel}";
            }

            UpdateXPFill(immediate);
        }

        private void UpdateXPFill(bool immediate)
        {
            if (_progressionService == null || _xpProgressBar == null)
            {
                return;
            }

            int threshold = Mathf.Max(1, _progressionService.GetXPThresholdForLevel(_progressionService.PlayerLevel));
            float targetFill = Mathf.Clamp01((float)_progressionService.PlayerXP / threshold);

            _fillTween?.Kill();
            if (immediate)
            {
                _xpProgressBar.fillAmount = targetFill;
            }
            else
            {
                _fillTween = _xpProgressBar.DOFillAmount(targetFill, 0.35f).SetEase(Ease.OutQuad).SetTarget(this);
            }
        }
    }
}
