using UnityEngine;
using UnityEngine.UI;

namespace CozyLifeSim.UI.Style
{
    public static class CozyProceduralUI
    {
        public static bool ForceFlatUIDebug = false;

        public static bool ShouldUseFlatFallback(Sprite sprite, UIStyleConfig styleConfig)
        {
            if (ForceFlatUIDebug) return true;
            if (styleConfig != null && styleConfig.ForceFlatUI) return true;
            if (sprite == null) return true;
            return false;
        }

        public static void ApplyFlatFallback(Image targetImage, Color baseColor)
        {
            if (targetImage == null) return;

            targetImage.sprite = null;
            targetImage.color = baseColor;

            // Ensure dark aesthetic outline
            var outline = targetImage.GetComponent<Outline>();
            if (outline == null)
            {
                outline = targetImage.gameObject.AddComponent<Outline>();
            }
            outline.effectColor = new Color(0.12f, 0.12f, 0.12f, 1f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = true;

            // Ensure soft drop shadow
            Shadow shadow = null;
            var shadows = targetImage.GetComponents<Shadow>();
            foreach (var s in shadows)
            {
                if (s.GetType() == typeof(Shadow))
                {
                    shadow = s;
                    break;
                }
            }
            if (shadow == null)
            {
                shadow = targetImage.gameObject.AddComponent<Shadow>();
            }
            shadow.effectColor = new Color(0f, 0f, 0f, 0.25f);
            shadow.effectDistance = new Vector2(4f, -4f);
            shadow.useGraphicAlpha = true;
        }

        public static Color GetColorForSticker(int stickerId)
        {
            switch (stickerId)
            {
                case 1: return new Color(1f, 0.7f, 0.75f); // Bunny Pink (Pastel Pink)
                case 2: return new Color(0.72f, 0.53f, 0.39f); // Bear Brown
                case 3: return new Color(0.95f, 0.95f, 0.95f); // Chicken White (Warm White)
                case 4: return new Color(0.94f, 0.69f, 0.23f); // Xe Banh Mi (Crisp Golden)
                case 5: return new Color(0.6f, 0.88f, 0.47f); // Ly Nuoc Mia (Sugarcane Green)
                case 6: return new Color(0.87f, 0.8f, 0.65f); // Chiec Non La (Straw/Dried Leaf)
                case 7: return new Color(0.2f, 0.35f, 0.45f); // Chiec Xich Lo (Antique Teal)
                case 8: return new Color(0.9f, 0.2f, 0.2f); // Long Den (Bright Red)
                default: return Color.white;
            }
        }

        public static Color GetColorForCrop(int cropId)
        {
            switch (cropId)
            {
                case 1: return new Color(0.85f, 0.85f, 0.85f); // White Acorn
                case 2: return new Color(0.55f, 0.83f, 0.42f); // Cay Mia Ngot
                case 3: return new Color(0.95f, 0.82f, 0.24f); // Lua Nuoc (Golden Yellow)
                case 4: return new Color(0.94f, 0.5f, 0.67f); // Hoa Sen (Lotus Pink)
                default: return Color.white;
            }
        }

        public static Color GetColorForAnimal(int animalId)
        {
            switch (animalId)
            {
                case 1: return new Color(0.95f, 0.95f, 0.95f); // Chicken White
                case 2: return new Color(0.92f, 0.64f, 0.33f); // Meo Tam The (Calico Orange)
                case 3: return new Color(0.28f, 0.28f, 0.32f); // Trau Nuoc (Water Buffalo Gray)
                default: return Color.white;
            }
        }
    }
}
