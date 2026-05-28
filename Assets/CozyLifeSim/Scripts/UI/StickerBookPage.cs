using UnityEngine;
using UnityEngine.UI;

namespace CozyLifeSim.UI
{
    [RequireComponent(typeof(Image))]
    public class StickerBookPage : MonoBehaviour
    {
        [SerializeField] private int _pageIndex;

        public int PageIndex => _pageIndex;

        public bool TryPlaceSticker(CozySticker sticker, Vector2 screenPosition, Camera eventCamera)
        {
            RectTransform pageRect = GetComponent<RectTransform>();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(pageRect, screenPosition, eventCamera, out Vector2 localPoint))
            {
                // Confirm drop is fully within page boundaries
                if (pageRect.rect.Contains(localPoint))
                {
                    sticker.FinalizePlacement(transform, localPoint, _pageIndex);
                    return true;
                }
            }
            return false;
        }
    }
}
