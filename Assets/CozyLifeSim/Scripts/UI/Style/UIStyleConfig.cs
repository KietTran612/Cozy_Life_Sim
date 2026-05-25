using System.Collections.Generic;
using UnityEngine;

namespace CozyLifeSim.UI.Style
{
    [CreateAssetMenu(fileName = "CozyUIStyleConfig", menuName = "CozySim/UI Style Config")]
    public class UIStyleConfig : ScriptableObject
    {
        [Header("Typography Palette")]
        public List<TextStyle> TextStyles = new List<TextStyle>();

        [Header("Structural Theme Prefabs")]
        public GameObject PrimaryButtonPrefab;
        public GameObject PanelBackgroundPrefab;

        public TextStyle GetTextStyle(string key)
        {
            return TextStyles.Find(x => x != null && x.StyleKey == key);
        }
    }
}
