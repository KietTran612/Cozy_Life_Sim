using System.Collections.Generic;
using UnityEngine;
using CozyLifeSim.Core;

namespace CozyLifeSim.UI.Services
{
    public class SaveService : ISaveService
    {
        private const string SaveKey = "CozyLifeSim_SaveGame";
        public SaveData ActiveSave { get; private set; }

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

        private void NormalizeSaveData()
        {
            // Safeguard lists against null deserialization from older saves
            if (ActiveSave.PlacedStickers == null)
            {
                ActiveSave.PlacedStickers = new List<StickerPlacedData>();
            }
            if (ActiveSave.CompletedQuestIds == null)
            {
                ActiveSave.CompletedQuestIds = new List<int>();
            }
            if (ActiveSave.ActiveQuestProgress == null)
            {
                ActiveSave.ActiveQuestProgress = new List<QuestProgressData>();
            }
            if (ActiveSave.UnlockedStickerIds == null)
            {
                ActiveSave.UnlockedStickerIds = new List<int>();
            }
            if (!ActiveSave.UnlockedStickerIds.Contains(1)) ActiveSave.UnlockedStickerIds.Add(1);
            if (!ActiveSave.UnlockedStickerIds.Contains(2)) ActiveSave.UnlockedStickerIds.Add(2);
        }


        public void Save()
        {
            if (ActiveSave == null) return;
            string json = JsonUtility.ToJson(ActiveSave);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }
    }
}
