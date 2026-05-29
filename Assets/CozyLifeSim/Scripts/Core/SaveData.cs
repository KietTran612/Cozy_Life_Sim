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
    public class SaveData
    {
        public int Coins = 100;
        public int Seeds = 5;
        public int Crops = 0;
        public List<int> UnlockedStickerIds = new List<int> { 1, 2 };
        public List<StickerPlacedData> PlacedStickers = new List<StickerPlacedData>();
        public List<int> CompletedQuestIds = new List<int>();
        public List<QuestProgressData> ActiveQuestProgress = new List<QuestProgressData>();
    }
}
