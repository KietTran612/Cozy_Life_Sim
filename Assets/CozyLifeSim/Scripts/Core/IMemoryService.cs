using System.Collections.Generic;

namespace CozyLifeSim.Core
{
    public interface IMemoryService
    {
        IReadOnlyList<StickerPlacedData> PlacedStickers { get; }
        void PlaceSticker(StickerPlacedData sticker);
        void RemoveSticker(int stickerId, int pageIndex);
        void RemovePlacedSticker(int pageIndex, StickerPlacedData data);

        // Non-Saving, returning struct, defensive validation & UUID append
        StickerPlacedData AddPlacedStickerNonSaving(StickerPlacedData data);
        bool TryRemovePlacedStickerNonSaving(string placementId, out StickerPlacedData removedData);
    }
}
