using System;

namespace CozyLifeSim.Core
{
    public interface IShopService
    {
        event Action OnShopTransactionSuccess;
        bool TryBuySeed(int cropId);
        bool TrySellCrop(int cropId);
        bool TryBuySticker(int stickerId);
        bool IsStickerUnlocked(int stickerId);
    }
}
