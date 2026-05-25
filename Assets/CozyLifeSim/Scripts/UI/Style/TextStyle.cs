using System;
using UnityEngine;
using TMPro;

namespace CozyLifeSim.UI.Style
{
    [Serializable]
    public class TextStyle
    {
        public string StyleKey;
        public TMP_FontAsset FontAsset;
        public Color Color;
        public float FontSizeOffset;
    }
}
