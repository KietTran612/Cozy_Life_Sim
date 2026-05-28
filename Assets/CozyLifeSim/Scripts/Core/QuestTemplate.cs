using System;

namespace CozyLifeSim.Core
{
    [Serializable]
    public class QuestTemplate
    {
        public int QuestId;
        public string Title;
        public int TargetCount;
        public int RewardCoins;
        public QuestType Type;

        // Required parameterless constructor for dynamic instantiation and editor staging
        public QuestTemplate() { }

        public QuestTemplate(int questId, string title, int targetCount, int rewardCoins, QuestType type)
        {
            QuestId = questId;
            Title = title;
            TargetCount = targetCount;
            RewardCoins = rewardCoins;
            Type = type;
        }
    }
}
