using UnityEngine;
using UnityEngine.UI;

namespace CozyLifeSim.UI
{
    public sealed class CozyLandscapeLayoutController : MonoBehaviour
    {
        [SerializeField] private RectTransform header;
        [SerializeField] private RectTransform gameplayArea;
        [SerializeField] private RectTransform farmPlot;
        [SerializeField] private RectTransform animalPen;
        [SerializeField] private RectTransform stickerBookPanel;
        [SerializeField] private RectTransform inventoryTray;
        [SerializeField] private RectTransform sidebar;
        [SerializeField] private RectTransform popupRoot;

        private void Start()
        {
            Apply();
        }

        private void OnRectTransformDimensionsChange()
        {
            Apply();
        }

        public void Apply()
        {
            ApplyStretch(header, new Vector2(0f, 0.9f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            ApplyStretch(gameplayArea, new Vector2(0f, 0.25f), new Vector2(1f, 0.9f), new Vector2(36f, 16f), new Vector2(-36f, -16f));
            ApplyStretch(inventoryTray, new Vector2(0f, 0f), new Vector2(1f, 0.23f), new Vector2(36f, 16f), new Vector2(-36f, -16f));
            ApplyStretch(sidebar, new Vector2(0.92f, 0.45f), new Vector2(0.985f, 0.9f), Vector2.zero, Vector2.zero);
            ApplyStretch(popupRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            ApplyPreferredSize(farmPlot, 350f, 500f);
            ApplyPreferredSize(animalPen, 350f, 500f);
            ApplyPreferredSize(stickerBookPanel, 500f, 500f);
        }

        private static void ApplyStretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void ApplyPreferredSize(RectTransform rect, float preferredWidth, float preferredHeight)
        {
            if (rect == null)
            {
                return;
            }

            var layout = rect.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = preferredWidth;
            layout.preferredHeight = preferredHeight;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;
        }
    }
}
