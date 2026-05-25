using UnityEngine;
using TMPro;
using VContainer;
using VContainer.Unity;
using CozyLifeSim.UI.Style;

namespace CozyLifeSim.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class UIStyleElement : MonoBehaviour
    {
        [SerializeField] private string _styleKey;
        private IStyleService _styleService;
        private TextMeshProUGUI _textComponent;
        private float _baseFontSize;
        private bool _isInitialized;
        private bool _isInjected;

        private void Awake()
        {
            _textComponent = GetComponent<TextMeshProUGUI>();
            if (_textComponent != null)
            {
                _baseFontSize = _textComponent.fontSize;
                _isInitialized = true;
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

        [Inject]
        public void Construct(IStyleService styleService)
        {
            if (_isInjected) return;
            _isInjected = true;

            _styleService = styleService;
            _styleService.OnStyleChanged += ApplyStyle;
            ApplyStyle();
        }

        private void ApplyStyle()
        {
            if (!_isInitialized || _styleService == null || _styleService.CurrentConfig == null || _textComponent == null) return;
            
            var textStyle = _styleService.CurrentConfig.GetTextStyle(_styleKey);
            if (textStyle != null)
            {
                _textComponent.font = textStyle.FontAsset;
                _textComponent.color = textStyle.Color;
                // Fix cumulative growth bug: always add offset to cached base font size
                _textComponent.fontSize = _baseFontSize + textStyle.FontSizeOffset;
            }
        }

        private void OnDestroy()
        {
            if (_styleService != null)
            {
                _styleService.OnStyleChanged -= ApplyStyle;
            }
        }
    }
}
