using System.Collections.Generic;
using UnityEngine;

namespace CozyLifeSim.UI.Settings
{
    [CreateAssetMenu(fileName = "AnimalDatabase", menuName = "CozySim/Animal Database")]
    public class AnimalDatabase : ScriptableObject
    {
        public List<AnimalTemplate> Animals = new List<AnimalTemplate>();

        public AnimalTemplate GetAnimal(int animalId)
        {
            if (Animals == null) return null;
            return Animals.Find(x => x != null && x.AnimalId == animalId);
        }

        public bool ValidateDatabase(out List<string> errors)
        {
            errors = new List<string>();
            HashSet<int> uniqueIds = new HashSet<int>();

            if (Animals == null)
            {
                errors.Add("Animal list is null.");
                return false;
            }

            for (int i = 0; i < Animals.Count; i++)
            {
                var a = Animals[i];
                if (a == null)
                {
                    errors.Add($"Animal at index {i} is null.");
                    continue;
                }

                if (a.AnimalId <= 0)
                {
                    errors.Add($"Animal at index {i}: ID must be positive.");
                }

                if (uniqueIds.Contains(a.AnimalId))
                {
                    errors.Add($"Duplicate Animal ID found: {a.AnimalId} ('{a.Name}')");
                }
                else
                {
                    uniqueIds.Add(a.AnimalId);
                }

                if (string.IsNullOrWhiteSpace(a.Name))
                {
                    errors.Add($"Animal with ID {a.AnimalId} has an empty Name.");
                }

                if (a.Sprite == null)
                {
                    errors.Add($"Animal with ID {a.AnimalId} ('{a.Name}') is missing its main Sprite.");
                }

                if (a.BreathScaleY <= 1.0f)
                {
                    errors.Add($"Animal with ID {a.AnimalId} ('{a.Name}') has an invalid breath scale ({a.BreathScaleY}). Must be greater than 1.0f.");
                }

                if (a.BreathDuration <= 0f)
                {
                    errors.Add($"Animal with ID {a.AnimalId} ('{a.Name}') has an invalid breath duration ({a.BreathDuration}s). Must be > 0.");
                }

                if (a.PetJumpHeight < 0f)
                {
                    errors.Add($"Animal with ID {a.AnimalId} ('{a.Name}') has a negative jump height ({a.PetJumpHeight}). Height must be non-negative.");
                }

                if (a.PetJumpDuration <= 0f)
                {
                    errors.Add($"Animal with ID {a.AnimalId} ('{a.Name}') has an invalid jump duration ({a.PetJumpDuration}s). Duration must be greater than zero.");
                }
            }

            return errors.Count == 0;
        }
    }
}
