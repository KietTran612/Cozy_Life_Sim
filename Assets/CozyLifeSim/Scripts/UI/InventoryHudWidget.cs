using UnityEngine;
using TMPro;
using VContainer;
using VContainer.Unity;
using CozyLifeSim.Core;

namespace CozyLifeSim.UI
{
    public class InventoryHudWidget : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _coinsText;
        [SerializeField] private TextMeshProUGUI _seedsText;
        [SerializeField] private TextMeshProUGUI _cropsText;

        private IInventoryService _inventoryService;
        private bool _isSubscribed;

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
            if (_coinsText != null) _coinsText.text = $"Coins: {value}";
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
        }
    }
}
