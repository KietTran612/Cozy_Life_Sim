using UnityEngine;
using UnityEngine.EventSystems;

namespace CozyLifeSim.UI
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class CozyInteractiveObject : MonoBehaviour
    {
        [SerializeField] private CozyPopup _targetPopup;

        private void OnMouseDown()
        {
            // Block click if hovering over any UI element (like popups or the dim blocker overlay)
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (_targetPopup != null)
            {
                _targetPopup.Open();
            }
        }
    }
}
