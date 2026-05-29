using System.Collections.Generic;
using UnityEngine;

namespace CozyLifeSim.UI.Settings
{
    [CreateAssetMenu(fileName = "StickerDatabase", menuName = "CozySim/Sticker Database")]
    public class StickerDatabase : ScriptableObject
    {
        public List<StickerTemplate> Stickers = new List<StickerTemplate>();

        public StickerTemplate GetSticker(int stickerId)
        {
            if (Stickers == null) return null;
            return Stickers.Find(x => x != null && x.StickerId == stickerId);
        }

        public bool ValidateDatabase(out List<string> errors)
        {
            errors = new List<string>();
            HashSet<int> uniqueIds = new HashSet<int>();

            if (Stickers == null)
            {
                errors.Add("Sticker list is null.");
                return false;
            }

            for (int i = 0; i < Stickers.Count; i++)
            {
                var s = Stickers[i];
                if (s == null)
                {
                    errors.Add($"Sticker at index {i} is null.");
                    continue;
                }

                if (s.StickerId <= 0)
                {
                    errors.Add($"Sticker at index {i} has an invalid ID ({s.StickerId}). ID must be positive.");
                }

                if (uniqueIds.Contains(s.StickerId))
                {
                    errors.Add($"Duplicate Sticker ID found: {s.StickerId} ('{s.Name}')");
                }
                else
                {
                    uniqueIds.Add(s.StickerId);
                }

                if (string.IsNullOrWhiteSpace(s.Name))
                {
                    errors.Add($"Sticker with ID {s.StickerId} has an empty Name.");
                }

                if (s.Sprite == null)
                {
                    errors.Add($"Sticker with ID {s.StickerId} ('{s.Name}') is missing its main Sprite.");
                }
            }

            return errors.Count == 0;
        }
    }
}
