using System.Collections.Generic;

namespace CozyLifeSim.Core
{
    public interface IMemoryService
    {
        IReadOnlyList<StickerPlacedData> PlacedStickers { get; }
        IReadOnlyList<DiaryNotePlacedData> PlacedDiaryNotes { get; }
        IReadOnlyList<PageStyleData> PageStyles { get; }
        void PlaceSticker(StickerPlacedData sticker);
        void RemoveSticker(int stickerId, int pageIndex);
        void RemovePlacedSticker(int pageIndex, StickerPlacedData data);

        // Non-Saving, returning struct, defensive validation & UUID append
        StickerPlacedData AddPlacedStickerNonSaving(StickerPlacedData data);
        bool TryRemovePlacedStickerNonSaving(string placementId, out StickerPlacedData removedData);

        DiaryNotePlacedData AddDiaryNoteNonSaving(string noteId, string text, float x, float y, int pageIndex);
        bool UpdateDiaryNotePositionNonSaving(string noteId, float x, float y, out DiaryNotePlacedData updatedData);
        bool RemoveDiaryNoteNonSaving(string noteId, out DiaryNotePlacedData removedData);
        void SetPageStyleNonSaving(int pageIndex, int styleIndex);
        void RestoreMemoryStateNonSaving(List<DiaryNotePlacedData> notes, List<PageStyleData> styles);
    }
}
