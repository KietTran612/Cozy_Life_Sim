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
        public int RewardXP;
        public bool IsCompleted;
        public QuestType Type;

        public QuestData(int questId, string title, int targetCount, int rewardCoins, QuestType type, int rewardXP = 0)
        {
            QuestId = questId;
            Title = title;
            TargetCount = targetCount;
            RewardCoins = rewardCoins;
            Type = type;
            RewardXP = rewardXP;
            CurrentCount = 0;
            IsCompleted = false;
        }
    }
}
