using System;
using CozyLifeSim.Core;

namespace CozyLifeSim.UI.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly ISaveService _saveService;
        private SaveData ActiveSave => _saveService.ActiveSave;

        public int Coins => ActiveSave.Coins;
        public int Seeds => ActiveSave.Seeds;
        public int Crops => ActiveSave.Crops;

        public event Action<int> OnCoinsChanged;
        public event Action<int> OnSeedsChanged;
        public event Action<int> OnCropsChanged;
        public event Action OnInventoryReloaded;

        public InventoryService(ISaveService saveService)
        {
            _saveService = saveService;
        }

        public void ReloadFromSave()
        {
            OnCoinsChanged?.Invoke(ActiveSave.Coins);
            OnSeedsChanged?.Invoke(ActiveSave.Seeds);
            OnCropsChanged?.Invoke(ActiveSave.Crops);
            OnInventoryReloaded?.Invoke();
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            ActiveSave.Coins += amount;
            OnCoinsChanged?.Invoke(ActiveSave.Coins);
            _saveService.Save();
        }

        public bool ConsumeCoins(int amount)
        {
            if (amount <= 0 || ActiveSave.Coins < amount) return false;
            ActiveSave.Coins -= amount;
            OnCoinsChanged?.Invoke(ActiveSave.Coins);
            _saveService.Save();
            return true;
        }

        public void AddSeeds(int amount)
        {
            if (amount <= 0) return;
            ActiveSave.Seeds += amount;
            OnSeedsChanged?.Invoke(ActiveSave.Seeds);
            _saveService.Save();
        }

        public bool ConsumeSeeds(int amount)
        {
            if (amount <= 0 || ActiveSave.Seeds < amount) return false;
            ActiveSave.Seeds -= amount;
            OnSeedsChanged?.Invoke(ActiveSave.Seeds);
            _saveService.Save();
            return true;
        }

        public void AddCrops(int amount)
        {
            if (amount <= 0) return;
            ActiveSave.Crops += amount;
            OnCropsChanged?.Invoke(ActiveSave.Crops);
            _saveService.Save();
        }

        public bool ConsumeCrops(int amount)
        {
            if (amount <= 0 || ActiveSave.Crops < amount) return false;
            ActiveSave.Crops -= amount;
            OnCropsChanged?.Invoke(ActiveSave.Crops);
            _saveService.Save();
            return true;
        }
    }
}
