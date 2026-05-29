using System.Collections.Generic;
using UnityEngine;
using CozyLifeSim.Core;

namespace CozyLifeSim.UI.Services
{
    public class SaveService : ISaveService
    {
        private const string SaveKey = "CozyLifeSim_SaveGame";
        public SaveData ActiveSave { get; private set; }

#if UNITY_EDITOR
        public bool ForceSaveFailure { get; set; } = false;
#endif

        public SaveService()
        {
            Load();
        }

        public void Load()
        {
            if (PlayerPrefs.HasKey(SaveKey))
            {
                string json = PlayerPrefs.GetString(SaveKey);
                try
                {
                    ActiveSave = JsonUtility.FromJson<SaveData>(json);
                }
                catch (System.Exception) { }
            }

            if (ActiveSave == null)
            {
                ActiveSave = new SaveData();
            }

            NormalizeSaveData();
        }

        public void NormalizeSaveData()
        {
            if (ActiveSave == null) return;

            bool isDirty = false;

            // 1. Invariant Null-Safety (Luon luon chay vo dieu kien)
            if (ActiveSave.StickerOwned == null)
            {
                ActiveSave.StickerOwned = new List<StickerInventory>();
                isDirty = true;
            }
            if (ActiveSave.PlacedStickers == null)
            {
                ActiveSave.PlacedStickers = new List<StickerPlacedData>();
                isDirty = true;
            }
            if (ActiveSave.CompletedQuestIds == null)
            {
                ActiveSave.CompletedQuestIds = new List<int>();
                isDirty = true;
            }
            if (ActiveSave.ActiveQuestProgress == null)
            {
                ActiveSave.ActiveQuestProgress = new List<QuestProgressData>();
                isDirty = true;
            }

            // 2. Logic di dan (Migration & Refill) duoc bao ve boi Marker
            if (!ActiveSave.HasMigratedStickerOwned)
            {
                // Refill so luong mac dinh ID 1 va 2 neu kho trong
                if (ActiveSave.StickerOwned.Count == 0)
                {
                    ActiveSave.StickerOwned.Add(new StickerInventory(1, 99));
                    ActiveSave.StickerOwned.Add(new StickerInventory(2, 99));
                }

                // Merge locked sticker cu null-safe
                #pragma warning disable CS0618
                if (ActiveSave.UnlockedStickerIds != null)
                {
                    foreach (var oldId in ActiveSave.UnlockedStickerIds)
                    {
                        if (!ActiveSave.StickerOwned.Exists(x => x.StickerId == oldId))
                        {
                            ActiveSave.StickerOwned.Add(new StickerInventory(oldId, 1));
                        }
                    }
                    ActiveSave.UnlockedStickerIds.Clear(); // Don dep vung nho cu
                }
                #pragma warning restore CS0618

                ActiveSave.HasMigratedStickerOwned = true;
                isDirty = true;
            }

            // 3. Backfill PlacementId cho PlacedStickers cu (Null-Safe, Struct-Safe & Duplicate-Safe)
            var seenIds = new HashSet<string>();
            for (int i = 0; i < ActiveSave.PlacedStickers.Count; i++)
            {
                var placed = ActiveSave.PlacedStickers[i];
                if (string.IsNullOrEmpty(placed.PlacementId) || seenIds.Contains(placed.PlacementId))
                {
                    placed.PlacementId = System.Guid.NewGuid().ToString();
                    ActiveSave.PlacedStickers[i] = placed; // Ghi lai vao list vi struct
                    isDirty = true;
                }
                seenIds.Add(placed.PlacementId);
            }

            // 4. Luu file neu thuc su thay doi
            if (isDirty)
            {
                Save();
            }
        }

        public void Save()
        {
            if (ActiveSave == null) return;

#if UNITY_EDITOR
            if (ForceSaveFailure)
            {
                throw new System.Exception("Simulated Save Failure for Atomicity Testing");
            }
#endif

            string json = JsonUtility.ToJson(ActiveSave);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }
    }
}
