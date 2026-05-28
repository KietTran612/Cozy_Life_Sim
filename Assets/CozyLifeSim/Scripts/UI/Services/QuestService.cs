using System;
using System.Collections.Generic;
using CozyLifeSim.Core;

namespace CozyLifeSim.UI.Services
{
    public class QuestService : IQuestService
    {
        private readonly ISaveService _saveService;
        private readonly IInventoryService _inventoryService;
        private SaveData ActiveSave => _saveService.ActiveSave;

        private readonly List<QuestData> _quests = new List<QuestData>();
        public IReadOnlyList<QuestData> ActiveQuests => _quests;

        public event Action<QuestData> OnQuestProgressed;
        public event Action<QuestData> OnQuestCompleted;

        public QuestService(ISaveService saveService, IInventoryService inventoryService)
        {
            _saveService = saveService;
            _inventoryService = inventoryService;

            InitializeQuests();
        }

        private void InitializeQuests()
        {
            _quests.Add(new QuestData(1, "Water 3 Crops", 3, 50));
            _quests.Add(new QuestData(2, "Harvest 2 Mature Crops", 2, 80));
            _quests.Add(new QuestData(3, "Pet the Breathing Chicken 5 times", 5, 40));

            // Load completion states and partial progress
            foreach (var quest in _quests)
            {
                if (ActiveSave.CompletedQuestIds.Contains(quest.QuestId))
                {
                    quest.CurrentCount = quest.TargetCount;
                    quest.IsCompleted = true;
                }
                else
                {
                    var progress = ActiveSave.ActiveQuestProgress.Find(x => x.QuestId == quest.QuestId);
                    quest.CurrentCount = Math.Min(progress.CurrentCount, quest.TargetCount);
                }
            }
        }

        public void ProgressQuest(int questId, int amount)
        {
            QuestData quest = _quests.Find(x => x.QuestId == questId);
            if (quest == null || quest.IsCompleted) return;

            quest.CurrentCount = Math.Min(quest.CurrentCount + amount, quest.TargetCount);
            
            // Persist partial progress
            ActiveSave.ActiveQuestProgress.RemoveAll(x => x.QuestId == questId);
            if (quest.CurrentCount < quest.TargetCount)
            {
                ActiveSave.ActiveQuestProgress.Add(new QuestProgressData(questId, quest.CurrentCount));
            }
            
            _saveService.Save();
            OnQuestProgressed?.Invoke(quest);

            if (quest.CurrentCount >= quest.TargetCount)
            {
                quest.IsCompleted = true;
                ActiveSave.CompletedQuestIds.Add(questId);
                ActiveSave.ActiveQuestProgress.RemoveAll(x => x.QuestId == questId);
                _saveService.Save();
                
                // Reward coins
                _inventoryService.AddCoins(quest.RewardCoins);
                OnQuestCompleted?.Invoke(quest);
            }
        }
    }
}
