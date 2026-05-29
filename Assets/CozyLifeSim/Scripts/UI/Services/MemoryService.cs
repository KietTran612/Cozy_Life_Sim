using System.Collections.Generic;
using CozyLifeSim.Core;

namespace CozyLifeSim.UI.Services
{
    public class MemoryService : IMemoryService
    {
        private readonly ISaveService _saveService;
        private SaveData ActiveSave => _saveService.ActiveSave;

        public IReadOnlyList<StickerPlacedData> PlacedStickers => ActiveSave.PlacedStickers;

        public MemoryService(ISaveService saveService)
        {
            _saveService = saveService;
        }

        public void PlaceSticker(StickerPlacedData sticker)
        {
            AddPlacedStickerNonSaving(sticker);
            _saveService.Save();
        }

        public void RemoveSticker(int stickerId, int pageIndex)
        {
            ActiveSave.PlacedStickers.RemoveAll(x => x.StickerId == stickerId && x.PageIndex == pageIndex);
            _saveService.Save();
        }

        public void RemovePlacedSticker(int pageIndex, StickerPlacedData data)
        {
            if (TryRemovePlacedStickerNonSaving(data.PlacementId, out _))
            {
                _saveService.Save();
            }
        }

        public StickerPlacedData AddPlacedStickerNonSaving(StickerPlacedData data)
        {
            var list = ActiveSave.PlacedStickers;

            // Defensive validation: Ngan ngua UUID rong hoac trung lap
            bool hasDuplicate = false;
            if (!string.IsNullOrEmpty(data.PlacementId))
            {
                hasDuplicate = list.Exists(x => x.PlacementId == data.PlacementId);
            }

            if (string.IsNullOrEmpty(data.PlacementId) || hasDuplicate)
            {
                data.PlacementId = System.Guid.NewGuid().ToString();
            }

            // Pure append (Khong overwrite phan tu trung stickerId/pageIndex de cho phep nhieu ban sao)
            list.Add(data);
            return data;
        }

        public bool TryRemovePlacedStickerNonSaving(string placementId, out StickerPlacedData removedData)
        {
            removedData = default;
            if (string.IsNullOrEmpty(placementId)) return false;

            var list = ActiveSave.PlacedStickers;
            int idx = list.FindIndex(x => x.PlacementId == placementId);
            if (idx >= 0)
            {
                removedData = list[idx];
                list.RemoveAt(idx);
                return true;
            }
            return false;
        }
    }
}
