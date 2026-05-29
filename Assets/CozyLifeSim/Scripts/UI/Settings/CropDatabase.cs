using System.Collections.Generic;
using UnityEngine;

namespace CozyLifeSim.UI.Settings
{
    [CreateAssetMenu(fileName = "CropDatabase", menuName = "CozySim/Crop Database")]
    public class CropDatabase : ScriptableObject
    {
        public List<CropTemplate> Crops = new List<CropTemplate>();

        public CropTemplate GetCrop(int cropId)
        {
            if (Crops == null) return null;
            return Crops.Find(x => x != null && x.CropId == cropId);
        }

        public bool ValidateDatabase(out List<string> errors)
        {
            errors = new List<string>();
            HashSet<int> uniqueIds = new HashSet<int>();

            if (Crops == null)
            {
                errors.Add("Crop list is null.");
                return false;
            }

            for (int i = 0; i < Crops.Count; i++)
            {
                var c = Crops[i];
                if (c == null)
                {
                    errors.Add($"Crop at index {i} is null.");
                    continue;
                }

                if (uniqueIds.Contains(c.CropId))
                {
                    errors.Add($"Duplicate Crop ID found: {c.CropId} ('{c.Name}')");
                }
                else
                {
                    uniqueIds.Add(c.CropId);
                }

                if (string.IsNullOrWhiteSpace(c.Name))
                {
                    errors.Add($"Crop with ID {c.CropId} has an empty Name.");
                }

                if (c.StageDurationSeconds <= 0f)
                {
                    errors.Add($"Crop with ID {c.CropId} ('{c.Name}') has an invalid duration ({c.StageDurationSeconds}s). Duration must be greater than zero.");
                }

                if (c.SeedSprite == null || c.SproutSprite == null || c.MatureSprite == null || c.HarvestSprite == null)
                {
                    errors.Add($"Crop with ID {c.CropId} ('{c.Name}') is missing one or more Stage Sprites.");
                }
            }

            return errors.Count == 0;
        }
    }
}
