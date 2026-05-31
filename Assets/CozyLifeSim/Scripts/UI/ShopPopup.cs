using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;
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

        [Header("Tabs Configuration")]
        [SerializeField] private Button _seedsTabButton;
        [SerializeField] private Button _stickersTabButton;
        [SerializeField] private Button _cropsTabButton;
        [SerializeField] private RectTransform _seedsGroup;
        [SerializeField] private RectTransform _stickersGroup;
        [SerializeField] private RectTransform _cropsGroup;

        private ShopPresenter _presenter;
        private IInventoryService _inventoryService;
        private IProgressionService _progressionService;
        private CropDatabase _cropDatabase;
        private StickerDatabase _stickerDatabase;

        private readonly List<ShopItemWidget> _spawnedWidgets = new List<ShopItemWidget>();
        private bool _isSubscribed;

        private enum ShopTab { Seeds, Stickers, Crops }
        private ShopTab _currentTab = ShopTab.Seeds;

        [Inject]
        public void Construct(
            ShopPresenter presenter,
            IInventoryService inventoryService,
            CropDatabase cropDatabase,
            StickerDatabase stickerDatabase,
            IProgressionService progressionService)
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
            _progressionService = progressionService;

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

            // Tab listeners
            if (_seedsTabButton != null) _seedsTabButton.onClick.AddListener(SetSeedsTab);
            if (_stickersTabButton != null) _stickersTabButton.onClick.AddListener(SetStickersTab);
            if (_cropsTabButton != null) _cropsTabButton.onClick.AddListener(SetCropsTab);

            SetTab(ShopTab.Seeds, false); // Default tab
            RefreshShop();
        }

        private void SetSeedsTab() => SetTab(ShopTab.Seeds);
        private void SetStickersTab() => SetTab(ShopTab.Stickers);
        private void SetCropsTab() => SetTab(ShopTab.Crops);

        protected override void OnOpen()
        {
            base.OnOpen();
            RefreshShop();
        }

        private void OnCoinsChanged(int coins) => RefreshShop();
        private void OnSeedsChanged(int seeds) => RefreshShop();
        private void OnCropsChanged(int crops) => RefreshShop();
        private void OnInventoryReloaded() => RefreshShop();

        private void SetTab(ShopTab tab, bool animate = true)
        {
            _currentTab = tab;

            HighlightTabButton(_seedsTabButton, tab == ShopTab.Seeds, animate);
            HighlightTabButton(_stickersTabButton, tab == ShopTab.Stickers, animate);
            HighlightTabButton(_cropsTabButton, tab == ShopTab.Crops, animate);

            TransitionGroup(_seedsGroup, tab == ShopTab.Seeds, animate);
            TransitionGroup(_stickersGroup, tab == ShopTab.Stickers, animate);
            TransitionGroup(_cropsGroup, tab == ShopTab.Crops, animate);
        }

        private void HighlightTabButton(Button btn, bool active, bool animate)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img == null) return;

            Color targetColor = active ? new Color(0.35f, 0.35f, 0.35f, 1.0f) : new Color(0.2f, 0.2f, 0.2f, 1.0f);
            float targetScale = active ? 1.05f : 0.95f;

            if (animate && Application.isPlaying)
            {
                img.DOColor(targetColor, 0.2f).SetTarget(this);
                btn.transform.DOScale(targetScale, 0.2f).SetTarget(this);
            }
            else
            {
                img.color = targetColor;
                btn.transform.localScale = new Vector3(targetScale, targetScale, 1.0f);
            }
        }

        private void TransitionGroup(RectTransform group, bool active, bool animate)
        {
            if (group == null) return;
            var cg = group.GetComponent<CanvasGroup>();
            if (cg == null) cg = group.gameObject.AddComponent<CanvasGroup>();

            float targetAlpha = active ? 1f : 0f;
            bool targetBlocks = active;

            if (animate && Application.isPlaying)
            {
                cg.DOFade(targetAlpha, 0.2f).SetTarget(this).OnStart(() => {
                    if (active) group.gameObject.SetActive(true);
                }).OnComplete(() => {
                    group.gameObject.SetActive(active);
                    cg.blocksRaycasts = targetBlocks;
                });
            }
            else
            {
                group.gameObject.SetActive(active);
                cg.alpha = targetAlpha;
                cg.blocksRaycasts = targetBlocks;
            }
        }

        public void PlayCoinFlyAnimation(Vector3 startWorldPos, Vector3 endWorldPos)
        {
            if (!Application.isPlaying) return;
            int coinCount = 6;

            for (int i = 0; i < coinCount; i++)
            {
                GameObject coinGo = new GameObject("CoinFly");
                coinGo.transform.SetParent(_contentPanel, false);
                coinGo.transform.position = startWorldPos;

                var text = coinGo.AddComponent<TextMeshProUGUI>();
                text.text = "\u25cf";
                text.color = new Color(1f, 0.85f, 0f, 1f); // Gold yellow
                text.fontSize = 24f;
                text.alignment = TextAlignmentOptions.Center;

                Vector3 spread = new Vector3(Random.Range(-20f, 20f), Random.Range(-20f, 20f), 0f);
                coinGo.transform.position += spread / 100f;

                float delay = i * 0.04f;
                float duration = Random.Range(0.5f, 0.7f);

                Sequence seq = DOTween.Sequence().SetTarget(this);
                seq.AppendInterval(delay);

                Vector3 burstDir = (endWorldPos - startWorldPos).normalized;
                Vector3 perpendicular = new Vector3(-burstDir.y, burstDir.x, 0f);
                Vector3 popTarget = startWorldPos + (perpendicular * Random.Range(-40f, 40f) + burstDir * Random.Range(-20f, 20f)) / 100f;

                seq.Append(coinGo.transform.DOMove(popTarget, 0.15f).SetEase(Ease.OutQuad));
                seq.Append(coinGo.transform.DOMove(endWorldPos, duration).SetEase(Ease.InQuad));
                seq.Join(coinGo.transform.DOScale(0.5f, duration).SetEase(Ease.InQuad));

                seq.OnKill(() => {
                    if (coinGo != null) Destroy(coinGo);
                });

                seq.OnComplete(() => {
                    if (_playerCoinsText != null && endWorldPos == _playerCoinsText.transform.position)
                    {
                        _playerCoinsText.transform
                            .DOPunchScale(new Vector3(0.08f, 0.08f, 0.08f), 0.12f, 10, 1f)
                            .SetTarget(this);
                    }
                });
            }
        }

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
                    int playerLevel = _progressionService != null ? _progressionService.PlayerLevel : 999;
                    if (playerLevel < crop.RequiredLevel)
                    {
                        canBuy = false;
                    }
                    int cropId = crop.CropId;

                    widget.Setup(
                        $"{crop.Name} Seed",
                        crop.SeedSprite,
                        crop.BuyPrice,
                        "Buy",
                        canBuy,
                        () => {
                            if (_presenter != null && _presenter.TryBuySeed(cropId))
                            {
                                PlayCoinFlyAnimation(_playerCoinsText != null ? _playerCoinsText.transform.position : Vector3.zero, widget.transform.position);
                            }
                        }
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

                    int ownedCount = _inventoryService != null ? _inventoryService.GetStickerCount(sticker.StickerId) : 0;
                    bool canBuy = _inventoryService != null && _inventoryService.Coins >= sticker.BuyPrice;
                    int playerLevel = _progressionService != null ? _progressionService.PlayerLevel : 999;
                    if (playerLevel < sticker.RequiredLevel)
                    {
                        canBuy = false;
                    }

                    string btnLabel = ownedCount > 0 ? $"Buy (x{ownedCount})" : "Buy";
                    int stickerId = sticker.StickerId;

                    widget.Setup(
                        sticker.Name,
                        sticker.Sprite,
                        sticker.BuyPrice,
                        btnLabel,
                        canBuy,
                        () => {
                            if (_presenter != null && _presenter.TryBuySticker(stickerId))
                            {
                                PlayCoinFlyAnimation(_playerCoinsText != null ? _playerCoinsText.transform.position : Vector3.zero, widget.transform.position);
                            }
                        }
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
                        () => {
                            if (_presenter != null && _presenter.TrySellCrop(cropId))
                            {
                                PlayCoinFlyAnimation(widget.transform.position, _playerCoinsText != null ? _playerCoinsText.transform.position : Vector3.zero);
                            }
                        }
                    );
                    _spawnedWidgets.Add(widget);
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_seedsTabButton != null) _seedsTabButton.onClick.RemoveListener(SetSeedsTab);
            if (_stickersTabButton != null) _stickersTabButton.onClick.RemoveListener(SetStickersTab);
            if (_cropsTabButton != null) _cropsTabButton.onClick.RemoveListener(SetCropsTab);

            if (_seedsTabButton != null) _seedsTabButton.transform.DOKill();
            if (_stickersTabButton != null) _stickersTabButton.transform.DOKill();
            if (_cropsTabButton != null) _cropsTabButton.transform.DOKill();

            if (_seedsGroup != null) { var cg = _seedsGroup.GetComponent<CanvasGroup>(); if (cg != null) cg.DOKill(); }
            if (_stickersGroup != null) { var cg = _stickersGroup.GetComponent<CanvasGroup>(); if (cg != null) cg.DOKill(); }
            if (_cropsGroup != null) { var cg = _cropsGroup.GetComponent<CanvasGroup>(); if (cg != null) cg.DOKill(); }

            DOTween.Kill(this);

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
