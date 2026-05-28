using System;
using VContainer;
using CozyLifeSim.Core;

namespace CozyLifeSim.UI.Presenters
{
    public class FarmPresenter : IDisposable
    {
        private readonly IInventoryService _inventory;
        private readonly IQuestService _quest;

        public event Action<bool> OnPlantAttemptResult;
        public event Action OnCropHarvested;

        [Inject]
        public FarmPresenter(IInventoryService inventory, IQuestService quest)
        {
            _inventory = inventory;
            _quest = quest;
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
            _inventory.AddCrops(1);
            _inventory.AddCoins(10);
            _quest.ProgressQuest(QuestType.HarvestCrops, 1);
            OnCropHarvested?.Invoke();
        }

        public void Dispose() { }
    }
}
