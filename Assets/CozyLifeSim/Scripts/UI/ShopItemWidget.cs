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

        public void Setup(string itemName, Sprite icon, int price, string buttonLabel, bool isInteractable, System.Action onAction)
        {
            if (_itemNameText != null) _itemNameText.text = itemName;
            if (_itemIcon != null)
            {
                _itemIcon.sprite = icon;
                _itemIcon.gameObject.SetActive(icon != null);
            }
            if (_priceText != null) _priceText.text = $"{price} Coins";
            if (_actionButtonText != null) _actionButtonText.text = buttonLabel;
            if (_actionButton != null) _actionButton.interactable = isInteractable;

            _onActionCallback = onAction;
        }

        private void OnButtonClicked()
        {
            _onActionCallback?.Invoke();
        }
    }
}
