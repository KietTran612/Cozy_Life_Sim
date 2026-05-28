using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace CozyLifeSim.UI
{
    public class CozySidebar : MonoBehaviour
    {
        [Header("Sliding Configuration")]
        [SerializeField] private RectTransform _slidingPanel;
        [SerializeField] private Button _toggleButton;
        [SerializeField] private TextMeshProUGUI _toggleButtonText;
        [SerializeField] private float _slideDuration = 0.3f;
        [SerializeField] private Ease _slideEase = Ease.OutQuad;

        [Header("Tab Configuration")]
        [SerializeField] private Button[] _tabButtons;
        [SerializeField] private RectTransform[] _tabContents;

        private bool _isOpen = true;
        private Vector2 _openPosition;
        private Vector2 _closedPosition;
        private Tween _slideTween;

        private void Start()
        {
            if (_slidingPanel == null)
            {
                _slidingPanel = GetComponent<RectTransform>();
            }

            // Record open position
            _openPosition = _slidingPanel.anchoredPosition;

            // Closed position is offset to the right by the width of the panel
            // minus the width of the tabs container (approx. 65px) so the tabs remain visible!
            float panelWidth = _slidingPanel.rect.width;
            float visibleWidthWhenClosed = 65f;
            _closedPosition = new Vector2(_openPosition.x + (panelWidth - visibleWidthWhenClosed), _openPosition.y);

            // Initialize tabs - Select Quest tab (index 0) by default
            SelectTab(0);

            if (_toggleButton != null)
            {
                _toggleButton.onClick.AddListener(ToggleSidebar);
                UpdateToggleButtonVisual();
            }

            for (int i = 0; i < _tabButtons.Length; i++)
            {
                int index = i;
                if (_tabButtons[i] != null)
                {
                    _tabButtons[i].onClick.AddListener(() => SelectTab(index));
                }
            }
        }

        public void ToggleSidebar()
        {
            _isOpen = !_isOpen;
            _slideTween?.Kill();

            Vector2 targetPos = _isOpen ? _openPosition : _closedPosition;
            _slideTween = _slidingPanel.DOAnchorPos(targetPos, _slideDuration)
                .SetEase(_slideEase)
                .SetUpdate(true);

            UpdateToggleButtonVisual();
        }

        private void UpdateToggleButtonVisual()
        {
            if (_toggleButtonText != null)
            {
                _toggleButtonText.text = _isOpen ? ">" : "<";
            }
        }

        public void SelectTab(int tabIndex)
        {
            if (_tabContents == null || _tabButtons == null) return;

            for (int i = 0; i < _tabContents.Length; i++)
            {
                if (_tabContents[i] == null) continue;

                if (i == tabIndex)
                {
                    _tabContents[i].gameObject.SetActive(true);
                    
                    // Juice: scale-up pop transition on activation
                    _tabContents[i].localScale = Vector3.one * 0.95f;
                    _tabContents[i].DOScale(1.0f, 0.2f)
                        .SetEase(Ease.OutBack)
                        .SetUpdate(true);

                    // Style selected tab button (bright white backing)
                    if (_tabButtons[i] != null)
                    {
                        _tabButtons[i].image.color = Color.white;
                    }
                }
                else
                {
                    _tabContents[i].gameObject.SetActive(false);
                    
                    // Style unselected tab button (semi-transparent gray)
                    if (_tabButtons[i] != null)
                    {
                        _tabButtons[i].image.color = new Color(0.7f, 0.7f, 0.7f, 0.6f);
                    }
                }
            }

            // Auto-open sidebar if it was closed when user clicked a tab
            if (!_isOpen)
            {
                ToggleSidebar();
            }
        }

        private void OnDestroy()
        {
            _slideTween?.Kill();
            if (_toggleButton != null)
            {
                _toggleButton.onClick.RemoveListener(ToggleSidebar);
            }
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                if (_tabButtons[i] != null)
                {
                    int index = i;
                    _tabButtons[i].onClick.RemoveAllListeners();
                }
            }
        }
    }
}
