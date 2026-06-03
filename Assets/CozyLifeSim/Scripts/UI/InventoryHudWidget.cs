using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VContainer;
using VContainer.Unity;
using CozyLifeSim.Core;
using DG.Tweening;

namespace CozyLifeSim.UI
{
    public class InventoryHudWidget : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _coinsText;
        [SerializeField] private TextMeshProUGUI _seedsText;
        [SerializeField] private TextMeshProUGUI _cropsText;
        [SerializeField] private Image _autosaveIcon;
        [SerializeField] private Image _coinIcon;
        [SerializeField] private Image _seedsIcon;
        [SerializeField] private Image _cropsIcon;

        private IInventoryService _inventoryService;
        private bool _isSubscribed;
        private int _displayedCoins = -1;
        private Tween _coinTween;

        [Inject]
        public void Construct(IInventoryService inventoryService)
        {
            // Unsubscribe from previous service if currently subscribed
            if (_isSubscribed && _inventoryService != null)
            {
                _inventoryService.OnCoinsChanged -= UpdateCoins;
                _inventoryService.OnSeedsChanged -= UpdateSeeds;
                _inventoryService.OnCropsChanged -= UpdateCrops;
                _isSubscribed = false;
            }

            _inventoryService = inventoryService;

            if (_inventoryService != null)
            {
                // Subscribe to asset changes
                _inventoryService.OnCoinsChanged += UpdateCoins;
                _inventoryService.OnSeedsChanged += UpdateSeeds;
                _inventoryService.OnCropsChanged += UpdateCrops;
                _isSubscribed = true;

                // Initial display
                UpdateCoins(_inventoryService.Coins);
                UpdateSeeds(_inventoryService.Seeds);
                UpdateCrops(_inventoryService.Crops);
            }
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                var scope = LifetimeScope.Find<GameLifetimeScope>();
                if (scope != null && scope.Container != null)
                {
                    scope.Container.Inject(this);
                }
            }
        }

        private void UpdateCoins(int value)
        {
            if (_coinsText == null) return;
            if (_displayedCoins == -1 || !Application.isPlaying)
            {
                _displayedCoins = value;
                _coinsText.text = $"Coins: {value}";
                return;
            }

            _coinTween?.Kill();
            _coinTween = DOTween.To(() => _displayedCoins, x => {
                _displayedCoins = x;
                _coinsText.text = $"Coins: {x}";
            }, value, 0.75f).SetEase(Ease.OutQuad).SetTarget(this);
        }

        private void UpdateSeeds(int value)
        {
            if (_seedsText != null) _seedsText.text = $"Seeds: {value}";
        }

        private void UpdateCrops(int value)
        {
            if (_cropsText != null) _cropsText.text = $"Crops: {value}";
        }

        private void OnDestroy()
        {
            if (_isSubscribed && _inventoryService != null)
            {
                _inventoryService.OnCoinsChanged -= UpdateCoins;
                _inventoryService.OnSeedsChanged -= UpdateSeeds;
                _inventoryService.OnCropsChanged -= UpdateCrops;
                _isSubscribed = false;
            }
            _coinTween?.Kill();
        }
    }
}
