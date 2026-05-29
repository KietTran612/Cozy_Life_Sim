using System;
using UnityEngine;

namespace CozyLifeSim.UI.Settings
{
    [Serializable]
    public class StickerTemplate
    {
        public int StickerId;
        public string Name;
        public Sprite Sprite;
        public Sprite ShadowSprite;
        public int BuyPrice = 50;
        public int RequiredLevel = 1;


        public StickerTemplate() { }

        public StickerTemplate(int stickerId, string name, Sprite sprite, Sprite shadowSprite)
        {
            StickerId = stickerId;
            Name = name;
            Sprite = sprite;
            ShadowSprite = shadowSprite;
        }
    }
}
