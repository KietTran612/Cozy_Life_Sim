using System;
using System.Collections.Generic;
using CozyLifeSim.Core;

namespace CozyLifeSim.UI.Services
{
    public class QuestService : IQuestService
    {
        private readonly ISaveService _saveService;
        private readonly IInventoryService _inventoryService;
        private readonly IProgressionService _progressionService;
        private readonly CozyLifeSim.UI.Settings.QuestDatabase _questDatabase;
        private SaveData ActiveSave => _saveService.ActiveSave;

        private readonly List<QuestData> _quests = new List<QuestData>();
        public IReadOnlyList<QuestData> ActiveQuests => _quests;

        public event Action<QuestData> OnQuestProgressed;
        public event Action<QuestData> OnQuestCompleted;
        public event Action OnQuestsReloaded;

        public QuestService(
            ISaveService saveService, 
            IInventoryService inventoryService, 
            IProgressionService progressionService,
            CozyLifeSim.UI.Settings.QuestDatabase questDatabase, 
            bool logFallbackWarning = true)
        {
            _saveService = saveService;
            _inventoryService = inventoryService;
            _progressionService = progressionService;
            _questDatabase = questDatabase;

            InitializeQuests(logFallbackWarning);
        }

        // Backward compatibility constructor
        public QuestService(
            ISaveService saveService, 
            IInventoryService inventoryService, 
            CozyLifeSim.UI.Settings.QuestDatabase questDatabase, 
            bool logFallbackWarning = true)
            : this(saveService, inventoryService, null, questDatabase, logFallbackWarning)
        {
        }

        private void InitializeQuests(bool logFallbackWarning)
        {
            if (_questDatabase != null && _questDatabase.Quests != null && _questDatabase.Quests.Count > 0)
            {
                foreach (var template in _questDatabase.Quests)
                {
                    if (template != null)
                    {
                        _quests.Add(new QuestData(template.QuestId, template.Title, template.TargetCount, template.RewardCoins, template.Type, template.RewardXP));
                    }
                }
            }
            else
            {
                if (logFallbackWarning)
                {
                    UnityEngine.Debug.LogWarning("[CozySim] QuestDatabase is null or empty. Falling back to default hardcoded quests.");
                }
                _quests.Add(new QuestData(1, "Water 3 Crops", 3, 50, QuestType.WaterCrops, 50));
                _quests.Add(new QuestData(2, "Harvest 2 Mature Crops", 2, 80, QuestType.HarvestCrops, 100));
                _quests.Add(new QuestData(3, "Pet the Breathing Chicken 5 times", 5, 40, QuestType.PetAnimal, 40));
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

        public void ReloadFromSave(bool logFallbackWarning = true)
        {
            _quests.Clear();
            InitializeQuests(logFallbackWarning);
            OnQuestsReloaded?.Invoke();
        }

        public void ProgressQuest(QuestType type, int amount)
        {
            TryProgressQuest(type, amount);
        }

        public bool TryProgressQuest(QuestType type, int amount)
        {
            // Linear Progression: Find the FIRST uncompleted active quest of this type
            QuestData quest = _quests.Find(x => x.Type == type && !x.IsCompleted);
            if (quest == null || amount <= 0) return false;

            int questId = quest.QuestId;
            int oldQuestCount = quest.CurrentCount;
            bool oldQuestCompleted = quest.IsCompleted;
            int oldCoins = _inventoryService.Coins;
            int oldLevel = _progressionService != null ? _progressionService.PlayerLevel : ActiveSave.PlayerLevel;
            int oldXP = _progressionService != null ? _progressionService.PlayerXP : ActiveSave.PlayerXP;
            List<QuestProgressData> oldActiveProgress = new List<QuestProgressData>(ActiveSave.ActiveQuestProgress);
            List<int> oldCompletedQuestIds = new List<int>(ActiveSave.CompletedQuestIds);

            try
            {
                quest.CurrentCount = Math.Min(quest.CurrentCount + amount, quest.TargetCount);

                // Persist partial progress.
                ActiveSave.ActiveQuestProgress.RemoveAll(x => x.QuestId == questId);
                if (quest.CurrentCount < quest.TargetCount)
                {
                    ActiveSave.ActiveQuestProgress.Add(new QuestProgressData(questId, quest.CurrentCount));
                }

                bool completedNow = quest.CurrentCount >= quest.TargetCount;
                if (completedNow)
                {
                    quest.IsCompleted = true;
                    if (!ActiveSave.CompletedQuestIds.Contains(questId))
                    {
                        ActiveSave.CompletedQuestIds.Add(questId);
                    }
                    ActiveSave.ActiveQuestProgress.RemoveAll(x => x.QuestId == questId);

                    if (_progressionService != null)
                    {
                        _progressionService.AddXPNonSaving(quest.RewardXP);
                    }
                    else
                    {
                        AddXPToSaveNonSaving(quest.RewardXP);
                    }

                    _inventoryService.AddCoinsNonSaving(quest.RewardCoins);
                }

                _saveService.Save();

                OnQuestProgressed?.Invoke(quest);
                if (completedNow)
                {
                    OnQuestCompleted?.Invoke(quest);
                }

                return true;
            }
            catch (Exception ex)
            {
                quest.CurrentCount = oldQuestCount;
                quest.IsCompleted = oldQuestCompleted;

                ActiveSave.ActiveQuestProgress.Clear();
                ActiveSave.ActiveQuestProgress.AddRange(oldActiveProgress);
                ActiveSave.CompletedQuestIds.Clear();
                ActiveSave.CompletedQuestIds.AddRange(oldCompletedQuestIds);

                RestoreInventoryCoinsNonSaving(oldCoins);
                RestoreProgressionNonSaving(oldLevel, oldXP);

                UnityEngine.Debug.LogWarning($"[CozySim Quest] TryProgressQuest failed to save. Rolled back RAM. Exception: {ex.Message}");
                return false;
            }
        }

        private void RestoreInventoryCoinsNonSaving(int targetCoins)
        {
            int delta = _inventoryService.Coins - targetCoins;
            if (delta > 0)
            {
                _inventoryService.ConsumeCoinsNonSaving(delta);
            }
            else if (delta < 0)
            {
                _inventoryService.AddCoinsNonSaving(-delta);
            }
        }

        private void RestoreProgressionNonSaving(int level, int xp)
        {
            if (_progressionService != null)
            {
                _progressionService.SetProgressionNonSaving(level, xp);
                return;
            }

            ActiveSave.PlayerLevel = Math.Max(1, level);
            ActiveSave.PlayerXP = Math.Max(0, xp);
        }

        private void AddXPToSaveNonSaving(int amount)
        {
            if (amount <= 0) return;

            ActiveSave.PlayerXP += amount;
            int xpRequired = GetXPThreshold(ActiveSave.PlayerLevel);
            while (ActiveSave.PlayerXP >= xpRequired)
            {
                ActiveSave.PlayerXP -= xpRequired;
                ActiveSave.PlayerLevel++;
                xpRequired = GetXPThreshold(ActiveSave.PlayerLevel);
            }
        }

        private int GetXPThreshold(int level)
        {
            return level * 100;
        }
    }
}
