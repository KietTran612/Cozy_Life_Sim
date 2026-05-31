using System;
using System.Collections.Generic;

namespace CozyLifeSim.Core
{
    public interface IQuestService
    {
        IReadOnlyList<QuestData> ActiveQuests { get; }
        event Action<QuestData> OnQuestProgressed;
        event Action<QuestData> OnQuestCompleted;
        event Action OnQuestsReloaded;

        void ProgressQuest(QuestType type, int amount);
        bool TryProgressQuest(QuestType type, int amount);
        void ReloadFromSave(bool logFallbackWarning = true);
    }
}
