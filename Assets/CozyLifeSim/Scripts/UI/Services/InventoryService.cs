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
            AddCoinsNonSaving(amount);
            _saveService.Save();
        }

        public bool ConsumeCoins(int amount)
        {
            if (ConsumeCoinsNonSaving(amount))
            {
                _saveService.Save();
                return true;
            }
            return false;
        }

        public void AddCoinsNonSaving(int amount)
        {
            if (amount <= 0) return;
            ActiveSave.Coins += amount;
            OnCoinsChanged?.Invoke(ActiveSave.Coins);
        }

        public bool ConsumeCoinsNonSaving(int amount)
        {
            if (amount <= 0 || ActiveSave.Coins < amount) return false;
            ActiveSave.Coins -= amount;
            OnCoinsChanged?.Invoke(ActiveSave.Coins);
            return true;
        }

        public void AddSeeds(int amount)
        {
            AddSeedsNonSaving(amount);
            _saveService.Save();
        }

        public bool ConsumeSeeds(int amount)
        {
            if (ConsumeSeedsNonSaving(amount))
            {
                _saveService.Save();
                return true;
            }
            return false;
        }

        public void AddSeedsNonSaving(int amount)
        {
            if (amount <= 0) return;
            ActiveSave.Seeds += amount;
            OnSeedsChanged?.Invoke(ActiveSave.Seeds);
        }

        public bool ConsumeSeedsNonSaving(int amount)
        {
            if (amount <= 0 || ActiveSave.Seeds < amount) return false;
            ActiveSave.Seeds -= amount;
            OnSeedsChanged?.Invoke(ActiveSave.Seeds);
            return true;
        }


        public void AddCrops(int amount)
        {
            if (amount <= 0) return;
            AddCropsNonSaving(amount);
            _saveService.Save();
        }

        public bool ConsumeCrops(int amount)
        {
            if (ConsumeCropsNonSaving(amount))
            {
                _saveService.Save();
                return true;
            }
            return false;
        }

        public void AddCropsNonSaving(int amount)
        {
            if (amount <= 0) return;
            ActiveSave.Crops += amount;
            OnCropsChanged?.Invoke(ActiveSave.Crops);
        }

        public bool ConsumeCropsNonSaving(int amount)
        {
            if (amount <= 0 || ActiveSave.Crops < amount) return false;
            ActiveSave.Crops -= amount;
            OnCropsChanged?.Invoke(ActiveSave.Crops);
            return true;
        }

        // Sticker Countable API
        public event Action<int, int> OnStickerCountChanged;

        public int GetStickerCount(int stickerId)
        {
            var item = ActiveSave.StickerOwned.Find(x => x.StickerId == stickerId);
            return item?.Count ?? 0;
        }

        public void AddStickerCount(int stickerId, int amount)
        {
            AddStickerCountNonSaving(stickerId, amount);
            _saveService.Save();
        }

        public bool ConsumeSticker(int stickerId)
        {
            if (ConsumeStickerNonSaving(stickerId))
            {
                _saveService.Save();
                return true;
            }
            return false;
        }

        public void AddStickerCountNonSaving(int stickerId, int amount)
        {
            if (amount <= 0) return;
            var list = ActiveSave.StickerOwned;
            var item = list.Find(x => x.StickerId == stickerId);
            if (item == null)
            {
                item = new StickerInventory(stickerId, amount);
                list.Add(item);
            }
            else
            {
                item.Count += amount;
            }
            OnStickerCountChanged?.Invoke(stickerId, item.Count);
        }

        public bool ConsumeStickerNonSaving(int stickerId)
        {
            var list = ActiveSave.StickerOwned;
            var item = list.Find(x => x.StickerId == stickerId);
            if (item == null || item.Count <= 0) return false;

            item.Count--;
            OnStickerCountChanged?.Invoke(stickerId, item.Count);
            return true;
        }
    }
}
