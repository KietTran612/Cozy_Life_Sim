using System;
using System.Collections.Generic;

namespace CozyLifeSim.Core
{
    public interface IQuestService
    {
        IReadOnlyList<QuestData> ActiveQuests { get; }
        event Action<QuestData> OnQuestProgressed;
        event Action<QuestData> OnQuestCompleted;

        void ProgressQuest(int questId, int amount);
    }
}
