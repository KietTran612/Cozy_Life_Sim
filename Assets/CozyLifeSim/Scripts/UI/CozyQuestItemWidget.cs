using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CozyLifeSim.Core;

namespace CozyLifeSim.UI
{
    public class CozyQuestItemWidget : MonoBehaviour
    {
        [SerializeField] private Image _bgImage;
        [SerializeField] private Image _typeIcon;
        [SerializeField] private Image _stampOverlay;
        [SerializeField] private TextMeshProUGUI _questText;

        private bool _raycastTargetEnabled = true;

        public void SetRaycastTargetEnabled(bool enabled)
        {
            _raycastTargetEnabled = enabled;
            UpdateRaycastTargets();
        }

        private void Start()
        {
            UpdateRaycastTargets();
        }

        public void UpdateRaycastTargets()
        {
            foreach (var graphic in GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = _raycastTargetEnabled;
            }
        }

        public void Setup(QuestData quest, Sprite bg, Sprite typeIcon, Sprite stamp)
        {
            if (quest == null) return;

            if (_questText != null)
            {
                if (quest.IsCompleted)
                {
                    _questText.text = $"<s>{quest.Title} (Completed!)</s>";
                    _questText.color = Color.gray;
                }
                else
                {
                    _questText.text = $"{quest.Title} ({quest.CurrentCount}/{quest.TargetCount})";
                    _questText.color = Color.white;
                }
            }

            if (_bgImage != null)
            {
                _bgImage.sprite = bg;
                _bgImage.gameObject.SetActive(bg != null);
            }

            if (_typeIcon != null)
            {
                _typeIcon.sprite = typeIcon;
                _typeIcon.gameObject.SetActive(typeIcon != null);
            }

            if (_stampOverlay != null)
            {
                _stampOverlay.sprite = stamp;
                _stampOverlay.gameObject.SetActive(quest.IsCompleted && stamp != null);
            }

            UpdateRaycastTargets();
        }
    }
}

