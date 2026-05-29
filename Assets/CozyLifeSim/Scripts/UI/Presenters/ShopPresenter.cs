using System;
using VContainer;
using CozyLifeSim.Core;

namespace CozyLifeSim.UI.Presenters
{
    public class ShopPresenter : IDisposable
    {
        private readonly IShopService _shopService;

        public event Action OnShopTransactionSuccess
        {
            add => _shopService.OnShopTransactionSuccess += value;
            remove => _shopService.OnShopTransactionSuccess -= value;
        }

        [Inject]
        public ShopPresenter(IShopService shopService)
        {
            _shopService = shopService;
        }

        public bool TryBuySeed(int cropId)
        {
            return _shopService.TryBuySeed(cropId);
        }

        public bool TrySellCrop(int cropId)
        {
            return _shopService.TrySellCrop(cropId);
        }

        public bool TryBuySticker(int stickerId)
        {
            return _shopService.TryBuySticker(stickerId);
        }

        public bool IsStickerUnlocked(int stickerId)
        {
            return _shopService.IsStickerUnlocked(stickerId);
        }

        public void Dispose() { }
    }
}
