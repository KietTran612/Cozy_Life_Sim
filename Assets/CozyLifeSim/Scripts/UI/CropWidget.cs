using System;
using System.Threading;
using CozyLifeSim.Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer; // Required DI import
using VContainer.Unity; // Required LifetimeScope import
using CozyLifeSim.UI.Presenters; // Required Presenter import

namespace CozyLifeSim.UI
{
    public class CropWidget : MonoBehaviour
    {
        [SerializeField] private int _cropId;
        [SerializeField] private float _stageDurationSeconds = 5f;

        [Header("UI Visuals")]
        [SerializeField] private Image _cropVisual;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private Button _waterButton;
        [SerializeField] private RectTransform _wateringCan;
        [SerializeField] private Button _plantButton;
        [SerializeField] private Button _harvestButton;

        [Header("Stage Sprites")]
        [SerializeField] private Sprite _seedSprite;
        [SerializeField] private Sprite _sproutSprite;
        [SerializeField] private Sprite _matureSprite;
        [SerializeField] private Sprite _harvestSprite;

        private CropState _state;
        private CancellationTokenSource _cts;
        private Sequence _wateringSequence;
        private Tween _cropFeedbackTween;
        private bool _isWatering;
        private FarmPresenter _presenter;

        [Inject]
        public void Construct(FarmPresenter presenter)
        {
            _presenter = presenter;
        }

        private void Start()
        {
            // Auto Injection helper
            if (Application.isPlaying)
            {
                var scope = LifetimeScope.Find<GameLifetimeScope>();
                if (scope != null && scope.Container != null)
                {
                    scope.Container.Inject(this);
                }
            }

            float stageDuration = Mathf.Max(1f, _stageDurationSeconds);
            // Start in empty soil stage (-1)
            _state = new CropState(_cropId, -1, stageDuration, false);
            UpdateVisuals();

            if (_wateringCan != null)
            {
                _wateringCan.gameObject.SetActive(false);
            }

            if (_waterButton != null)
            {
                _waterButton.onClick.AddListener(Irrigate);
            }

            if (_plantButton != null)
            {
                _plantButton.onClick.AddListener(PlantSeed);
            }

            if (_harvestButton != null)
            {
                _harvestButton.onClick.AddListener(HarvestCrop);
            }

            _cts = new CancellationTokenSource();
            RunGrowthTimer(_cts.Token).Forget();
        }

        private async UniTaskVoid RunGrowthTimer(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await UniTask.Delay(1000, cancellationToken: token);

                    if (_state.GrowthStage == -1 || _state.GrowthStage >= 3 || !_state.IsWatered)
                    {
                        continue;
                    }

                    _state.TimeRemainingSeconds -= 1f;
                    if (_state.TimeRemainingSeconds <= 0f)
                    {
                        AdvanceStage();
                    }

                    UpdateVisuals();
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when the widget is destroyed.
            }
        }

        private void PlantSeed()
        {
            if (_state.GrowthStage != -1 || _presenter == null) return;

            if (_presenter.TryPlantCrop())
            {
                _state = new CropState(_cropId, 0, Mathf.Max(1f, _stageDurationSeconds), false);
                UpdateVisuals();
            }
        }

        private void HarvestCrop()
        {
            if (_state.GrowthStage < 3 || _presenter == null) return;

            _presenter.HarvestCrop();

            // Reset plot back to empty soil (-1)
            _state = new CropState(_cropId, -1, Mathf.Max(1f, _stageDurationSeconds), false);
            UpdateVisuals();
        }

        private void AdvanceStage()
        {
            int nextStage = Mathf.Min(_state.GrowthStage + 1, 3);
            _state = new CropState(_state.CropId, nextStage, Mathf.Max(1f, _stageDurationSeconds), false);

            if (_cropVisual != null)
            {
                _cropFeedbackTween?.Kill();
                _cropFeedbackTween = _cropVisual.transform
                    .DOPunchScale(new Vector3(0.3f, 0.3f, 0f), 0.5f, 10, 1f);
            }
        }

        private void Irrigate()
        {
            if (_isWatering || _state.IsWatered || _state.GrowthStage == -1 || _state.GrowthStage >= 3)
            {
                return;
            }

            _isWatering = true;
            _wateringSequence?.Kill();

            if (_wateringCan == null)
            {
                CompleteWatering();
                return;
            }

            _wateringCan.gameObject.SetActive(true);
            _wateringCan.localScale = Vector3.zero;
            _wateringCan.localRotation = Quaternion.identity;

            _wateringSequence = DOTween.Sequence();
            _wateringSequence.Append(_wateringCan.DOScale(1f, 0.25f).SetEase(Ease.OutBack));
            _wateringSequence.Append(_wateringCan.DORotate(new Vector3(0f, 0f, -45f), 0.3f).SetEase(Ease.OutQuad));
            _wateringSequence.Append(_wateringCan.DOShakeRotation(0.5f, new Vector3(0f, 0f, 15f), 10, 90f));
            _wateringSequence.AppendCallback(CompleteWatering);
            _wateringSequence.Append(_wateringCan.DORotate(Vector3.zero, 0.2f).SetEase(Ease.OutQuad));
            _wateringSequence.Join(_wateringCan.DOScale(0f, 0.2f).SetEase(Ease.InQuad));
            _wateringSequence.OnComplete(() => _wateringCan.gameObject.SetActive(false));
        }

        private void CompleteWatering()
        {
            _state.IsWatered = true;
            _isWatering = false;

            if (_cropVisual != null)
            {
                _cropFeedbackTween?.Kill();
                _cropFeedbackTween = _cropVisual.transform.DOShakePosition(0.4f, 8f, 15);
            }

            // Notify quest watered progress ONLY on successful complete watering completion
            if (_presenter != null)
            {
                _presenter.NotifyCropWatered();
            }

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (_cropVisual != null)
            {
                _cropVisual.gameObject.SetActive(_state.GrowthStage >= 0);
                if (_state.GrowthStage >= 0)
                {
                    _cropVisual.sprite = _state.GrowthStage switch
                    {
                        0 => _seedSprite,
                        1 => _sproutSprite,
                        2 => _matureSprite,
                        3 => _harvestSprite,
                        _ => _seedSprite
                    };
                }
            }

            if (_timerText != null)
            {
                if (_state.GrowthStage == -1)
                {
                    _timerText.text = "EMPTY SOIL";
                }
                else if (_state.GrowthStage >= 3)
                {
                    _timerText.text = "READY TO HARVEST!";
                }
                else if (!_state.IsWatered)
                {
                    _timerText.text = "NEED WATER!";
                }
                else
                {
                    _timerText.text = $"Growing: {Mathf.CeilToInt(_state.TimeRemainingSeconds)}s";
                }
            }

            if (_plantButton != null)
            {
                _plantButton.gameObject.SetActive(_state.GrowthStage == -1);
            }

            if (_harvestButton != null)
            {
                _harvestButton.gameObject.SetActive(_state.GrowthStage >= 0);
                _harvestButton.interactable = _state.GrowthStage >= 3;
            }

            if (_waterButton != null)
            {
                _waterButton.gameObject.SetActive(_state.GrowthStage >= 0);
                _waterButton.interactable = !_isWatering && !_state.IsWatered && _state.GrowthStage < 3;
            }
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _wateringSequence?.Kill();
            _cropFeedbackTween?.Kill();

            if (_waterButton != null)
            {
                _waterButton.onClick.RemoveListener(Irrigate);
            }

            if (_plantButton != null)
            {
                _plantButton.onClick.RemoveListener(PlantSeed);
            }

            if (_harvestButton != null)
            {
                _harvestButton.onClick.RemoveListener(HarvestCrop);
            }
        }
    }
}
