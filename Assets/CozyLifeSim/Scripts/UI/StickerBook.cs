using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;
using VContainer; // Required DI import
using VContainer.Unity; // Required LifetimeScope import
using CozyLifeSim.UI.Presenters; // Required Presenter import
using CozyLifeSim.UI.Settings; // Import Settings

namespace CozyLifeSim.UI
{
    public class StickerBook : MonoBehaviour
    {
        [SerializeField] private List<StickerBookPage> _pages;
        [SerializeField] private RectTransform _flipPageIndicator; // Target for ScaleX compression
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _prevButton;

        [Header("Dynamic Templates")]
        [SerializeField] private Transform _inventoryTrayRoot;
        [SerializeField] private CozySticker _stickerPrefabTemplate; // Single generic prefab reference

        private int _currentPageIndex = 0;
        private bool _isTransitioning = false;
        private Sequence _flipSequence;
        private StickerBookPresenter _presenter;
        private StickerDatabase _stickerDatabase;
        private readonly List<CozySticker> _spawnedInventoryStickers = new List<CozySticker>();

        [Inject]
        public void Construct(StickerBookPresenter presenter, StickerDatabase stickerDatabase = null)
        {
            _presenter = presenter;
            _stickerDatabase = stickerDatabase;
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

            if (_pages == null || _pages.Count == 0)
            {
                if (_nextButton != null) _nextButton.interactable = false;
                if (_prevButton != null) _prevButton.interactable = false;
                return;
            }

            // Set initial state
            for (int i = 0; i < _pages.Count; i++)
            {
                if (_pages[i] != null)
                {
                    _pages[i].gameObject.SetActive(i == _currentPageIndex);
                }
            }

            if (_nextButton != null) _nextButton.onClick.AddListener(NextPage);
            if (_prevButton != null) _prevButton.onClick.AddListener(PrevPage);
            
            UpdateNavigationButtons();

            // Spawn inventory tray dynamically
            SpawnDynamicStickers();

            // Restore sticker layouts from database on load
            RestoreStickers();
        }

        private void SpawnDynamicStickers()
        {
            if (_stickerDatabase == null || _inventoryTrayRoot == null || _stickerPrefabTemplate == null) return;
            if (_stickerDatabase.Stickers == null) return;

            foreach (var spawned in _spawnedInventoryStickers)
            {
                if (spawned != null)
                {
                    Destroy(spawned.gameObject);
                }
            }
            _spawnedInventoryStickers.Clear();

            // Hide the original editor setup template
            _stickerPrefabTemplate.gameObject.SetActive(false);

            foreach (var template in _stickerDatabase.Stickers)
            {
                if (template == null || template.Sprite == null) continue;

                var sticker = Instantiate(_stickerPrefabTemplate, _inventoryTrayRoot);
                sticker.gameObject.SetActive(true);

                // Setup sticker values dynamically
                sticker.Setup(template.StickerId, template.Sprite, template.ShadowSprite);
                _spawnedInventoryStickers.Add(sticker);
            }
        }

        private void RestoreStickers()
        {
            if (_presenter == null || _stickerDatabase == null || _stickerPrefabTemplate == null || _pages == null) return;
            if (_stickerDatabase.Stickers == null) return;

            var placedList = _presenter.GetPlacedStickers();
            foreach (var item in placedList)
            {
                // Find matching page index
                StickerBookPage targetPage = _pages.Find(p => p != null && p.PageIndex == item.PageIndex);
                if (targetPage == null) continue;

                // Find template config from database
                var template = _stickerDatabase.GetSticker(item.StickerId);
                if (template == null || template.Sprite == null) continue;

                // Spawn and position safely
                CozySticker spawned = Instantiate(_stickerPrefabTemplate, targetPage.transform);
                spawned.gameObject.SetActive(true);
                
                // Configure graphics dynamically from database
                spawned.Setup(template.StickerId, template.Sprite, template.ShadowSprite);

                RectTransform rect = spawned.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchoredPosition = new Vector2(item.PositionX, item.PositionY);
                    rect.localScale = new Vector3(item.Scale, item.Scale, 1.0f);
                    rect.localRotation = Quaternion.Euler(0f, 0f, item.Rotation);
                    
                    // Finalize positioning with saveToDisk set to FALSE to eliminate PlayerPrefs loops
                    spawned.FinalizePlacement(targetPage.transform, rect.anchoredPosition, item.PageIndex, false);
                }
            }
        }

        public void NextPage()
        {
            if (_pages == null || _pages.Count == 0) return;
            if (_currentPageIndex < _pages.Count - 1)
            {
                FlipPage(_currentPageIndex + 1);
            }
        }

        public void PrevPage()
        {
            if (_pages == null || _pages.Count == 0) return;
            if (_currentPageIndex > 0)
            {
                FlipPage(_currentPageIndex - 1);
            }
        }

        private void FlipPage(int targetIndex)
        {
            if (_isTransitioning || _pages == null || targetIndex < 0 || targetIndex >= _pages.Count) return;
            _isTransitioning = true;

            if (_pages[targetIndex] == null || _pages[_currentPageIndex] == null)
            {
                _isTransitioning = false;
                return;
            }

            if (_flipPageIndicator != null)
            {
                _flipPageIndicator.gameObject.SetActive(true);
                _flipPageIndicator.localScale = Vector3.one;

                _flipSequence?.Kill();
                _flipSequence = DOTween.Sequence();

                // Animate half-flip compression
                _flipSequence.Append(_flipPageIndicator.DOScaleX(0.0f, 0.25f).SetEase(Ease.InQuad));
                
                // Swap pages at midpoint
                _flipSequence.AppendCallback(() =>
                {
                    _pages[_currentPageIndex].gameObject.SetActive(false);
                    _currentPageIndex = targetIndex;
                    _pages[_currentPageIndex].gameObject.SetActive(true);

                    UpdateNavigationButtons();
                });

                // Animate half-flip expansion
                _flipSequence.Append(_flipPageIndicator.DOScaleX(1.0f, 0.25f).SetEase(Ease.OutQuad));
                
                // Complete page swap
                _flipSequence.OnComplete(() =>
                {
                    _flipPageIndicator.gameObject.SetActive(false);
                    _isTransitioning = false;
                });
            }
            else
            {
                // Fallback direct swap if visual asset not assigned
                _pages[_currentPageIndex].gameObject.SetActive(false);
                _currentPageIndex = targetIndex;
                _pages[_currentPageIndex].gameObject.SetActive(true);
                UpdateNavigationButtons();
                _isTransitioning = false;
            }
        }

        private void UpdateNavigationButtons()
        {
            if (_pages == null || _pages.Count == 0)
            {
                if (_nextButton != null) _nextButton.interactable = false;
                if (_prevButton != null) _prevButton.interactable = false;
                return;
            }
            if (_nextButton != null) _nextButton.interactable = _currentPageIndex < _pages.Count - 1;
            if (_prevButton != null) _prevButton.interactable = _currentPageIndex > 0;
        }

        private void OnDestroy()
        {
            _flipSequence?.Kill();
            if (_nextButton != null) _nextButton.onClick.RemoveListener(NextPage);
            if (_prevButton != null) _prevButton.onClick.RemoveListener(PrevPage);
        }
    }
}
