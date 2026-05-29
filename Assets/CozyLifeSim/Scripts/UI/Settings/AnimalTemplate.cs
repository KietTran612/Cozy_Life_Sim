using System;
using UnityEngine;

namespace CozyLifeSim.UI.Settings
{
    [Serializable]
    public class AnimalTemplate
    {
        public int AnimalId;
        public string Name;
        public Sprite Sprite;
        public float BreathScaleY = 1.03f;
        public float BreathDuration = 1.5f;
        public float PetJumpHeight = 25f;
        public float PetJumpDuration = 0.4f;
        public Sprite HeartFeedbackSprite;

        public AnimalTemplate() { }

        public AnimalTemplate(int animalId, string name, Sprite sprite, float breathScaleY, float breathDuration, float petJumpHeight, float petJumpDuration, Sprite heartFeedbackSprite)
        {
            AnimalId = animalId;
            Name = name;
            Sprite = sprite;
            BreathScaleY = breathScaleY;
            BreathDuration = breathDuration;
            PetJumpHeight = petJumpHeight;
            PetJumpDuration = petJumpDuration;
            HeartFeedbackSprite = heartFeedbackSprite;
        }
    }
}
