using System;
using CozyLifeSim.Core;
using CozyLifeSim.UI.Settings;
using UnityEngine;

namespace CozyLifeSim.UI.Services
{
    public class ShopService : IShopService
    {
        private readonly ISaveService _saveService;
        private readonly IInventoryService _inventoryService;
        private readonly CropDatabase _cropDatabase;
        private readonly StickerDatabase _stickerDatabase;

        public event Action OnShopTransactionSuccess;

        public ShopService(
            ISaveService saveService,
            IInventoryService inventoryService,
            CropDatabase cropDatabase,
            StickerDatabase stickerDatabase)
        {
            _saveService = saveService;
            _inventoryService = inventoryService;
            _cropDatabase = cropDatabase;
            _stickerDatabase = stickerDatabase;
        }

        public bool TryBuySeed(int cropId)
        {
            if (_cropDatabase == null)
            {
                Debug.Log("[CozySim Shop] Cannot buy seed: CropDatabase is missing.");
                return false;
            }

            var crop = _cropDatabase.GetCrop(cropId);
            if (crop == null)
            {
                Debug.Log($"[CozySim Shop] Cannot buy seed: Crop template for ID {cropId} not found.");
                return false;
            }

            int price = crop.BuyPrice;
            if (_inventoryService.Coins < price)
            {
                Debug.Log($"[CozySim Shop] Insufficient coins to buy seed for crop ID {cropId}. Cost: {price}, owned: {_inventoryService.Coins}");
                return false;
            }

            // Perform transaction: deduct coins, add aggregate seeds
            if (_inventoryService.ConsumeCoins(price))
            {
                _inventoryService.AddSeeds(1);
                _saveService.Save();
                OnShopTransactionSuccess?.Invoke();
                return true;
            }

            return false;
        }

        public bool TrySellCrop(int cropId)
        {
            if (_cropDatabase == null)
            {
                Debug.Log("[CozySim Shop] Cannot sell crop: CropDatabase is missing.");
                return false;
            }

            var crop = _cropDatabase.GetCrop(cropId);
            if (crop == null)
            {
                Debug.Log($"[CozySim Shop] Cannot sell crop: Crop template for ID {cropId} not found.");
                return false;
            }

            if (_inventoryService.Crops < 1)
            {
                Debug.Log($"[CozySim Shop] Insufficient crops in inventory to sell crop ID {cropId}.");
                return false;
            }

            if (crop.SellPrice <= 0)
            {
                Debug.Log($"[CozySim Shop] Cannot sell crop: Crop ID {cropId} has invalid sell price {crop.SellPrice}.");
                return false;
            }

            // Perform transaction: deduct aggregate crops, add coins
            if (_inventoryService.ConsumeCrops(1))
            {
                _inventoryService.AddCoins(crop.SellPrice);
                _saveService.Save();
                OnShopTransactionSuccess?.Invoke();
                return true;
            }

            return false;
        }

        public bool TryBuySticker(int stickerId)
        {
            if (_stickerDatabase == null)
            {
                Debug.Log("[CozySim Shop] Cannot buy sticker: StickerDatabase is missing.");
                return false;
            }

            var sticker = _stickerDatabase.GetSticker(stickerId);
            if (sticker == null)
            {
                Debug.Log($"[CozySim Shop] Cannot buy sticker: Sticker template for ID {stickerId} not found.");
                return false;
            }

            int price = sticker.BuyPrice;
            if (price <= 0)
            {
                Debug.Log($"[CozySim Shop] Cannot buy sticker: Sticker ID {stickerId} has invalid buy price {price}.");
                return false;
            }

            if (_inventoryService.Coins < price)
            {
                Debug.Log($"[CozySim Shop] Insufficient coins to buy sticker ID {stickerId}. Cost: {price}, owned: {_inventoryService.Coins}");
                return false;
            }

            if (!_inventoryService.ConsumeCoinsNonSaving(price))
            {
                return false;
            }

            _inventoryService.AddStickerCountNonSaving(stickerId, 1);
            try
            {
                _saveService.Save();
                OnShopTransactionSuccess?.Invoke();
                return true;
            }
            catch
            {
                _inventoryService.AddCoinsNonSaving(price);
                _inventoryService.ConsumeStickerNonSaving(stickerId);
                throw;
            }
        }

        public bool IsStickerUnlocked(int stickerId)
        {
            return _inventoryService.GetStickerCount(stickerId) > 0;
        }
    }
}
