using System;
using System.Collections.Generic;
using VContainer;
using CozyLifeSim.Core;

namespace CozyLifeSim.UI.Presenters
{
    public class StickerBookPresenter : IDisposable
    {
        private readonly IMemoryService _memory;
        private readonly IInventoryService _inventory;
        private readonly ISaveService _saveService;

        [Inject]
        public StickerBookPresenter(IMemoryService memory, IInventoryService inventory, ISaveService saveService)
        {
            _memory = memory;
            _inventory = inventory;
            _saveService = saveService;
        }

        public IReadOnlyList<StickerPlacedData> GetPlacedStickers()
        {
            return _memory.PlacedStickers;
        }

        public string TryPlaceSticker(int stickerId, int pageIndex, float x, float y, float scale, float rot)
        {
            if (_inventory.ConsumeStickerNonSaving(stickerId))
            {
                var data = new StickerPlacedData(stickerId, pageIndex, x, y, scale, rot);
                data = _memory.AddPlacedStickerNonSaving(data);

                try
                {
                    _saveService.Save();
                    return data.PlacementId;
                }
                catch (Exception)
                {
                    // Rollback RAM state if save fails
                    _inventory.AddStickerCountNonSaving(stickerId, 1);
                    _memory.TryRemovePlacedStickerNonSaving(data.PlacementId, out _);
                }
            }
            return null;
        }

        public bool TryReturnSticker(string placementId)
        {
            if (string.IsNullOrEmpty(placementId)) return false;

            if (_memory.TryRemovePlacedStickerNonSaving(placementId, out var removedData))
            {
                _inventory.AddStickerCountNonSaving(removedData.StickerId, 1);

                try
                {
                    _saveService.Save();
                    return true;
                }
                catch (Exception)
                {
                    // Rollback RAM state if save fails
                    _inventory.ConsumeStickerNonSaving(removedData.StickerId);
                    _memory.AddPlacedStickerNonSaving(removedData);
                }
            }
            return false;
        }

        [Obsolete("Dung TryPlaceSticker thay the de dam bao transaction")]
        public void SaveStickerPosition(int stickerId, int pageIndex, float x, float y, float scale, float rot)
        {
            var data = new StickerPlacedData(stickerId, pageIndex, x, y, scale, rot);
            _memory.PlaceSticker(data);
        }

        [Obsolete("Dung TryReturnSticker de dam bao transaction")]
        public void RemoveSticker(int stickerId, int pageIndex)
        {
            _memory.RemoveSticker(stickerId, pageIndex);
        }

        public void Dispose() { }
    }
}
