using UnityEngine;
using UnityEngine.UI;

namespace CozyLifeSim.UI
{
    public class CozySidebar : MonoBehaviour
    {
        [Header("Popups")]
        [SerializeField] private CozyPopup _questPopup;
        [SerializeField] private CozyPopup _shopPopup;

        [Header("Buttons")]
        [SerializeField] private Button _questButton;
        [SerializeField] private Button _shopButton;

        private void Start()
        {
            if (_questButton != null && _questPopup != null)
            {
                _questButton.onClick.AddListener(_questPopup.Open);
            }

            if (_shopButton != null && _shopPopup != null)
            {
                _shopButton.onClick.AddListener(_shopPopup.Open);
            }
        }

        private void OnDestroy()
        {
            if (_questButton != null && _questPopup != null)
            {
                _questButton.onClick.RemoveListener(_questPopup.Open);
            }

            if (_shopButton != null && _shopPopup != null)
            {
                _shopButton.onClick.RemoveListener(_shopPopup.Open);
            }
        }
    }
}
