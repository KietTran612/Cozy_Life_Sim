using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;
using VContainer; // Required DI import
using VContainer.Unity; // Required LifetimeScope import
using CozyLifeSim.UI.Presenters; // Required Presenter import

namespace CozyLifeSim.UI
{
    public class StickerBook : MonoBehaviour
    {
        [SerializeField] private List<StickerBookPage> _pages;
        [SerializeField] private RectTransform _flipPageIndicator; // Target for ScaleX compression
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _prevButton;
        [SerializeField] private List<CozySticker> _stickerTemplates; // Prefab references mapping StickerId

        private int _currentPageIndex = 0;
        private bool _isTransitioning = false;
        private Sequence _flipSequence;
        private StickerBookPresenter _presenter;

        [Inject]
        public void Construct(StickerBookPresenter presenter)
        {
            _presenter = presenter;
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

            // Restore sticker layouts from database on load
            RestoreStickers();
        }

        private void RestoreStickers()
        {
            if (_presenter == null || _stickerTemplates == null) return;

            var placedList = _presenter.GetPlacedStickers();
            foreach (var item in placedList)
            {
                // Find matching page index
                StickerBookPage targetPage = _pages.Find(p => p.PageIndex == item.PageIndex);
                if (targetPage == null) continue;

                // Find template prefab
                CozySticker prefab = _stickerTemplates.Find(t => t.StickerId == item.StickerId);
                if (prefab == null) continue;

                // Spawn and position safely
                CozySticker spawned = Instantiate(prefab, targetPage.transform);
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
