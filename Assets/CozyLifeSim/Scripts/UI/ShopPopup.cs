using UnityEngine;
using TMPro;
using System.Collections.Generic;
using VContainer;
using VContainer.Unity;
using CozyLifeSim.Core;
using CozyLifeSim.UI.Presenters;
using CozyLifeSim.UI.Settings;

namespace CozyLifeSim.UI
{
    public class ShopPopup : CozyPopup
    {
        [Header("Shop Specifics")]
        [SerializeField] private TextMeshProUGUI _playerCoinsText;
        [SerializeField] private ShopItemWidget _itemPrefabTemplate;

        [Header("Containers")]
        [SerializeField] private RectTransform _seedsContainer;
        [SerializeField] private RectTransform _stickersContainer;
        [SerializeField] private RectTransform _cropsContainer;

        private ShopPresenter _presenter;
        private IInventoryService _inventoryService;
        private CropDatabase _cropDatabase;
        private StickerDatabase _stickerDatabase;

        private readonly List<ShopItemWidget> _spawnedWidgets = new List<ShopItemWidget>();
        private bool _isSubscribed;

        [Inject]
        public void Construct(
            ShopPresenter presenter,
            IInventoryService inventoryService,
            CropDatabase cropDatabase,
            StickerDatabase stickerDatabase)
        {
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

            _presenter = presenter;
            _inventoryService = inventoryService;
            _cropDatabase = cropDatabase;
            _stickerDatabase = stickerDatabase;

            if (_inventoryService != null)
            {
                _inventoryService.OnCoinsChanged += OnCoinsChanged;
                _inventoryService.OnSeedsChanged += OnSeedsChanged;
                _inventoryService.OnCropsChanged += OnCropsChanged;
                _inventoryService.OnInventoryReloaded += OnInventoryReloaded;
                _isSubscribed = true;
            }

            if (_presenter != null)
            {
                _presenter.OnShopTransactionSuccess += RefreshShop;
            }
        }

        protected override void Start()
        {
            base.Start();

            if (_itemPrefabTemplate != null)
            {
                _itemPrefabTemplate.gameObject.SetActive(false); // Hide template initially
            }

            if (Application.isPlaying)
            {
                var scope = LifetimeScope.Find<GameLifetimeScope>();
                if (scope != null && scope.Container != null)
                {
                    scope.Container.Inject(this);
                }
            }

            RefreshShop();
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            RefreshShop();
        }

        private void OnCoinsChanged(int coins) => RefreshShop();
        private void OnSeedsChanged(int seeds) => RefreshShop();
        private void OnCropsChanged(int crops) => RefreshShop();
        private void OnInventoryReloaded() => RefreshShop();

        private void RefreshShop()
        {
            if (_playerCoinsText != null && _inventoryService != null)
            {
                _playerCoinsText.text = $"Coins: {_inventoryService.Coins}";
            }

            // Clear previously spawned widgets
            foreach (var widget in _spawnedWidgets)
            {
                if (widget != null)
                {
                    Destroy(widget.gameObject);
                }
            }
            _spawnedWidgets.Clear();

            if (_itemPrefabTemplate == null) return;

            // 1. Populate Seeds grid (Buy seeds)
            if (_cropDatabase != null && _cropDatabase.Crops != null && _seedsContainer != null)
            {
                foreach (var crop in _cropDatabase.Crops)
                {
                    if (crop == null) continue;

                    var widget = Instantiate(_itemPrefabTemplate, _seedsContainer);
                    widget.gameObject.SetActive(true);

                    bool canBuy = _inventoryService != null && _inventoryService.Coins >= crop.BuyPrice;
                    int cropId = crop.CropId;

                    widget.Setup(
                        $"{crop.Name} Seed",
                        crop.SeedSprite,
                        crop.BuyPrice,
                        "Buy",
                        canBuy,
                        () => _presenter?.TryBuySeed(cropId)
                    );
                    _spawnedWidgets.Add(widget);
                }
            }

            // 2. Populate Stickers grid (Buy stickers)
            if (_stickerDatabase != null && _stickerDatabase.Stickers != null && _stickersContainer != null)
            {
                foreach (var sticker in _stickerDatabase.Stickers)
                {
                    if (sticker == null) continue;

                    var widget = Instantiate(_itemPrefabTemplate, _stickersContainer);
                    widget.gameObject.SetActive(true);

                    bool alreadyUnlocked = _presenter != null && _presenter.IsStickerUnlocked(sticker.StickerId);

                    bool canBuy = !alreadyUnlocked && _inventoryService != null && _inventoryService.Coins >= sticker.BuyPrice;
                    string btnLabel = alreadyUnlocked ? "Unlocked" : "Buy";
                    int stickerId = sticker.StickerId;

                    widget.Setup(
                        sticker.Name,
                        sticker.Sprite,
                        sticker.BuyPrice,
                        btnLabel,
                        canBuy,
                        () => _presenter?.TryBuySticker(stickerId)
                    );
                    _spawnedWidgets.Add(widget);
                }
            }

            // 3. Populate Crops grid (Sell crops)
            if (_cropDatabase != null && _cropDatabase.Crops != null && _cropsContainer != null)
            {
                foreach (var crop in _cropDatabase.Crops)
                {
                    if (crop == null) continue;

                    var widget = Instantiate(_itemPrefabTemplate, _cropsContainer);
                    widget.gameObject.SetActive(true);

                    bool canSell = _inventoryService != null && _inventoryService.Crops > 0;
                    int cropId = crop.CropId;

                    widget.Setup(
                        crop.Name,
                        crop.HarvestSprite,
                        crop.SellPrice,
                        $"Sell ({(_inventoryService != null ? _inventoryService.Crops : 0)})",
                        canSell,
                        () => _presenter?.TrySellCrop(cropId)
                    );
                    _spawnedWidgets.Add(widget);
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

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
    }
}
