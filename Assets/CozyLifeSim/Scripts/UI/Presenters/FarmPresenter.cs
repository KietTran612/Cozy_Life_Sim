using System;
using VContainer;
using CozyLifeSim.Core;
using UnityEngine;

namespace CozyLifeSim.UI.Presenters
{
    public class FarmPresenter : IDisposable
    {
        private readonly IInventoryService _inventory;
        private readonly IQuestService _quest;
        private readonly ISaveService _saveService;

        public event Action<bool> OnPlantAttemptResult;
        public event Action OnCropHarvested;

        [Inject]
        public FarmPresenter(IInventoryService inventory, IQuestService quest, ISaveService saveService)
        {
            _inventory = inventory;
            _quest = quest;
            _saveService = saveService;
        }

        public FarmPresenter(IInventoryService inventory, IQuestService quest)
            : this(inventory, quest, null)
        {
        }

        public bool TryPlantCrop()
        {
            bool success = _inventory.ConsumeSeeds(1);
            OnPlantAttemptResult?.Invoke(success);
            return success;
        }

        public void NotifyCropWatered()
        {
            _quest.ProgressQuest(QuestType.WaterCrops, 1);
        }

        public void HarvestCrop()
        {
            if (_saveService == null)
            {
                _inventory.AddCrops(1);
                _inventory.AddCoins(10);
                _quest.TryProgressQuest(QuestType.HarvestCrops, 1);
                OnCropHarvested?.Invoke();
                return;
            }

            _inventory.AddCropsNonSaving(1);
            _inventory.AddCoinsNonSaving(10);
            try
            {
                _saveService.Save();
            }
            catch (Exception ex)
            {
                _inventory.ConsumeCropsNonSaving(1);
                _inventory.ConsumeCoinsNonSaving(10);
                Debug.LogWarning($"[CozySim Farm] Harvest reward failed to save. Rolled back RAM. Exception: {ex.Message}");
                return;
            }

            _quest.TryProgressQuest(QuestType.HarvestCrops, 1);
            OnCropHarvested?.Invoke();
        }

        public void Dispose() { }
    }
}
