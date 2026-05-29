using System;
using UnityEngine;

namespace CozyLifeSim.UI.Settings
{
    [Serializable]
    public class CropTemplate
    {
        public int CropId;
        public string Name;
        public float StageDurationSeconds;
        public Sprite SeedSprite;
        public Sprite SproutSprite;
        public Sprite MatureSprite;
        public Sprite HarvestSprite;
        public int BuyPrice = 5;
        public int SellPrice = 15;


        public CropTemplate() { }

        public CropTemplate(int cropId, string name, float stageDurationSeconds, Sprite seedSprite, Sprite sproutSprite, Sprite matureSprite, Sprite harvestSprite)
        {
            CropId = cropId;
            Name = name;
            StageDurationSeconds = stageDurationSeconds;
            SeedSprite = seedSprite;
            SproutSprite = sproutSprite;
            MatureSprite = matureSprite;
            HarvestSprite = harvestSprite;
        }
    }
}
