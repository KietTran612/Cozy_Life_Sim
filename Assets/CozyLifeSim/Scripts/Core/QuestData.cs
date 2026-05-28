using System;

namespace CozyLifeSim.Core
{
    [Serializable]
    public class QuestData
    {
        public int QuestId;
        public string Title;
        public int TargetCount;
        public int CurrentCount;
        public int RewardCoins;
        public bool IsCompleted;

        public QuestData(int questId, string title, int targetCount, int rewardCoins)
        {
            QuestId = questId;
            Title = title;
            TargetCount = targetCount;
            RewardCoins = rewardCoins;
            CurrentCount = 0;
            IsCompleted = false;
        }
    }
}
