using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

namespace CozyLifeSim.UI
{
    public class StickerBook : MonoBehaviour
    {
        [SerializeField] private List<StickerBookPage> _pages;
        [SerializeField] private RectTransform _flipPageIndicator; // Target for ScaleX compression
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _prevButton;

        private int _currentPageIndex = 0;
        private bool _isTransitioning = false;

        private void Start()
        {
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

                // Animate half-flip compression
                _flipPageIndicator.DOScaleX(0.0f, 0.25f)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() =>
                    {
                        // Swap pages at midpoint
                        _pages[_currentPageIndex].gameObject.SetActive(false);
                        _currentPageIndex = targetIndex;
                        _pages[_currentPageIndex].gameObject.SetActive(true);

                        UpdateNavigationButtons();

                        // Animate half-flip expansion
                        _flipPageIndicator.DOScaleX(1.0f, 0.25f)
                            .SetEase(Ease.OutQuad)
                            .OnComplete(() =>
                            {
                                _flipPageIndicator.gameObject.SetActive(false);
                                _isTransitioning = false;
                            });
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
            if (_nextButton != null) _nextButton.onClick.RemoveListener(NextPage);
            if (_prevButton != null) _prevButton.onClick.RemoveListener(PrevPage);
        }
    }
}
