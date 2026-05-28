using System;
using VContainer;
using CozyLifeSim.Core;

namespace CozyLifeSim.UI.Presenters
{
    public class AnimalPresenter : IDisposable
    {
        private readonly IInventoryService _inventory;
        private readonly IQuestService _quest;

        public event Action<int> OnPetRewardGiven;

        [Inject]
        public AnimalPresenter(IInventoryService inventory, IQuestService quest)
        {
            _inventory = inventory;
            _quest = quest;
        }

        public void PetAnimal()
        {
            _inventory.AddCoins(5);
            _quest.ProgressQuest(3, 1);
            OnPetRewardGiven?.Invoke(5);
        }

        public void Dispose() { }
    }
}
