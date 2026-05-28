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
            // Remove existing placement of same sticker on this page
            ActiveSave.PlacedStickers.RemoveAll(x => x.StickerId == sticker.StickerId && x.PageIndex == sticker.PageIndex);
            ActiveSave.PlacedStickers.Add(sticker);
            _saveService.Save();
        }

        public void RemoveSticker(int stickerId, int pageIndex)
        {
            ActiveSave.PlacedStickers.RemoveAll(x => x.StickerId == stickerId && x.PageIndex == pageIndex);
            _saveService.Save();
        }
    }
}
