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

        // Sticker Countable API
        int GetStickerCount(int stickerId);
        void AddStickerCount(int stickerId, int amount);
        bool ConsumeSticker(int stickerId);
        event Action<int, int> OnStickerCountChanged; // (stickerId, count)

        // Non-Saving APIs
        void AddStickerCountNonSaving(int stickerId, int amount);
        bool ConsumeStickerNonSaving(int stickerId);
        void AddCoinsNonSaving(int amount);
        bool ConsumeCoinsNonSaving(int amount);
    }
}
