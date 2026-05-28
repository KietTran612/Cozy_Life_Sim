using System;
using System.Collections.Generic;
using CozyLifeSim.Core;

namespace CozyLifeSim.UI.Services
{
    public class QuestService : IQuestService
    {
        private readonly ISaveService _saveService;
        private readonly IInventoryService _inventoryService;
        private readonly CozyLifeSim.UI.Settings.QuestDatabase _questDatabase;
        private SaveData ActiveSave => _saveService.ActiveSave;

        private readonly List<QuestData> _quests = new List<QuestData>();
        public IReadOnlyList<QuestData> ActiveQuests => _quests;

        public event Action<QuestData> OnQuestProgressed;
        public event Action<QuestData> OnQuestCompleted;

        public QuestService(ISaveService saveService, IInventoryService inventoryService, CozyLifeSim.UI.Settings.QuestDatabase questDatabase)
        {
            _saveService = saveService;
            _inventoryService = inventoryService;
            _questDatabase = questDatabase;

            InitializeQuests();
        }

        private void InitializeQuests()
        {
            if (_questDatabase != null && _questDatabase.Quests != null && _questDatabase.Quests.Count > 0)
            {
                foreach (var template in _questDatabase.Quests)
                {
                    if (template != null)
                    {
                        _quests.Add(new QuestData(template.QuestId, template.Title, template.TargetCount, template.RewardCoins, template.Type));
                    }
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("[CozySim] QuestDatabase is null or empty. Falling back to default hardcoded quests.");
                _quests.Add(new QuestData(1, "Water 3 Crops", 3, 50, QuestType.WaterCrops));
                _quests.Add(new QuestData(2, "Harvest 2 Mature Crops", 2, 80, QuestType.HarvestCrops));
                _quests.Add(new QuestData(3, "Pet the Breathing Chicken 5 times", 5, 40, QuestType.PetAnimal));
            }

            // Normalize/Sanitize Save Data: remove active/completed progress records for obsolete/deleted quest IDs
            if (ActiveSave != null)
            {
                ActiveSave.ActiveQuestProgress.RemoveAll(x => !_quests.Exists(q => q.QuestId == x.QuestId));
                ActiveSave.CompletedQuestIds.RemoveAll(x => !_quests.Exists(q => q.QuestId == x));

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
                        if (progress.QuestId == quest.QuestId) // Check if match is found (default struct has QuestId = 0)
                        {
                            quest.CurrentCount = Math.Min(progress.CurrentCount, quest.TargetCount);
                        }
                    }
                }
            }
        }

        public void ProgressQuest(QuestType type, int amount)
        {
            // Linear Progression: Find the FIRST uncompleted active quest of this type
            QuestData quest = _quests.Find(x => x.Type == type && !x.IsCompleted);
            if (quest == null) return;

            int questId = quest.QuestId;
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
