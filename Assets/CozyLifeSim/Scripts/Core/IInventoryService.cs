using System;

namespace CozyLifeSim.Core
{
    public interface IInventoryService
    {
        int Coins { get; }
        int Seeds { get; }
        int Crops { get; }

        event Action<int> OnCoinsChanged;
        event Action<int> OnSeedsChanged;
        event Action<int> OnCropsChanged;
        event Action OnInventoryReloaded;

        void AddCoins(int amount);
        bool ConsumeCoins(int amount);
        void AddSeeds(int amount);
        bool ConsumeSeeds(int amount);
        void AddCrops(int amount);
        bool ConsumeCrops(int amount);
        void ReloadFromSave();
    }
}
