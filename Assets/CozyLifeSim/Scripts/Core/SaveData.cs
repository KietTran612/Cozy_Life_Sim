using System;
using System.Collections.Generic;

namespace CozyLifeSim.Core
{
    [Serializable]
    public struct QuestProgressData
    {
        public int QuestId;
        public int CurrentCount;

        public QuestProgressData(int questId, int currentCount)
        {
            QuestId = questId;
            CurrentCount = currentCount;
        }
    }

    [Serializable]
    public class StickerInventory
    {
        public int StickerId;
        public int Count;

        public StickerInventory(int stickerId, int count)
        {
            StickerId = stickerId;
            Count = count;
        }
    }

    [Serializable]
    public struct DiaryNotePlacedData
    {
        public string NoteId;
        public string Text;
        public float PositionX;
        public float PositionY;
        public int PageIndex;

        public DiaryNotePlacedData(string noteId, string text, float positionX, float positionY, int pageIndex)
        {
            NoteId = string.IsNullOrEmpty(noteId) ? Guid.NewGuid().ToString() : noteId;
            Text = text ?? string.Empty;
            PositionX = positionX;
            PositionY = positionY;
            PageIndex = pageIndex;
        }
    }

    [Serializable]
    public struct PageStyleData
    {
        public int PageIndex;
        public int StyleIndex;

        public PageStyleData(int pageIndex, int styleIndex)
        {
            PageIndex = pageIndex;
            StyleIndex = styleIndex;
        }
    }

    [Serializable]
    public class SaveData
    {
        public int Coins = 100;
        public int Seeds = 5;
        public int Crops = 0;
        public int PlayerLevel = 1;
        public int PlayerXP = 0;

        [System.Obsolete("Dung StickerOwned thay the")]
        public List<int> UnlockedStickerIds = new List<int> { 1, 2 };

        public List<StickerInventory> StickerOwned = new List<StickerInventory>();
        public bool HasMigratedStickerOwned = false;

        public List<StickerPlacedData> PlacedStickers = new List<StickerPlacedData>();
        public List<DiaryNotePlacedData> PlacedDiaryNotes = new List<DiaryNotePlacedData>();
        public List<PageStyleData> PageStyles = new List<PageStyleData>();
        public List<int> CompletedQuestIds = new List<int>();
        public List<QuestProgressData> ActiveQuestProgress = new List<QuestProgressData>();
    }
}
