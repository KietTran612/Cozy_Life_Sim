using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CozyLifeSim.UI
{
    public class ShopItemWidget : MonoBehaviour
    {
        [SerializeField] private Image _itemIcon;
        [SerializeField] private TextMeshProUGUI _itemNameText;
        [SerializeField] private TextMeshProUGUI _priceText;
        [SerializeField] private Button _actionButton;
        [SerializeField] private TextMeshProUGUI _actionButtonText;
        [SerializeField] private TextMeshProUGUI _disabledReasonText;

        private System.Action _onActionCallback;

        private void Start()
        {
            if (_actionButton != null)
            {
                _actionButton.onClick.AddListener(OnButtonClicked);
            }
        }

        private void OnDestroy()
        {
            if (_actionButton != null)
            {
                _actionButton.onClick.RemoveListener(OnButtonClicked);
            }
        }

        public void Setup(
            string itemName,
            Sprite icon,
            int price,
            string buttonLabel,
            bool isInteractable,
            string disabledReason,
            System.Action onAction)
        {
            if (_itemNameText != null) _itemNameText.text = itemName;
            if (_itemIcon != null)
            {
                _itemIcon.sprite = icon;
                _itemIcon.gameObject.SetActive(icon != null);
                ConfigureSimpleArtImage(_itemIcon);
            }
            if (_priceText != null) _priceText.text = $"{price} Coins";
            if (_actionButtonText != null) _actionButtonText.text = buttonLabel;
            if (_actionButton != null) _actionButton.interactable = isInteractable;

            if (_disabledReasonText != null)
            {
                bool showReason = !isInteractable && !string.IsNullOrWhiteSpace(disabledReason);
                _disabledReasonText.gameObject.SetActive(showReason);
                if (showReason)
                {
                    _disabledReasonText.text = disabledReason.Trim();
                }
            }

            _onActionCallback = onAction;
        }

        private void OnButtonClicked()
        {
            _onActionCallback?.Invoke();
        }

        private static void ConfigureSimpleArtImage(Image image)
        {
            if (image == null) return;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
        }
    }
}
