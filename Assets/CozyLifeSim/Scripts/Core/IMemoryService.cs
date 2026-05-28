using System.Collections.Generic;

namespace CozyLifeSim.Core
{
    public interface IMemoryService
    {
        IReadOnlyList<StickerPlacedData> PlacedStickers { get; }
        void PlaceSticker(StickerPlacedData sticker);
        void RemoveSticker(int stickerId, int pageIndex);
    }
}
