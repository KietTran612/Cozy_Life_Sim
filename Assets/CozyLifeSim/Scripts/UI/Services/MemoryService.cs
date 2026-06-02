using System.Collections.Generic;
using CozyLifeSim.Core;

namespace CozyLifeSim.UI.Services
{
    public class MemoryService : IMemoryService
    {
        private readonly ISaveService _saveService;
        private SaveData ActiveSave => _saveService.ActiveSave;

        public IReadOnlyList<StickerPlacedData> PlacedStickers => ActiveSave.PlacedStickers;
        public IReadOnlyList<DiaryNotePlacedData> PlacedDiaryNotes => ActiveSave.PlacedDiaryNotes;
        public IReadOnlyList<PageStyleData> PageStyles => ActiveSave.PageStyles;

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

        public DiaryNotePlacedData AddDiaryNoteNonSaving(string noteId, string text, float x, float y, int pageIndex)
        {
            var list = ActiveSave.PlacedDiaryNotes;
            bool hasDuplicate = false;
            if (!string.IsNullOrEmpty(noteId))
            {
                hasDuplicate = list.Exists(note => note.NoteId == noteId);
            }

            if (string.IsNullOrEmpty(noteId) || hasDuplicate)
            {
                noteId = System.Guid.NewGuid().ToString();
            }

            var data = new DiaryNotePlacedData(noteId, text, x, y, pageIndex);
            list.Add(data);
            return data;
        }

        public bool UpdateDiaryNotePositionNonSaving(string noteId, float x, float y, out DiaryNotePlacedData updatedData)
        {
            updatedData = default;
            if (string.IsNullOrEmpty(noteId)) return false;

            var list = ActiveSave.PlacedDiaryNotes;
            int index = list.FindIndex(note => note.NoteId == noteId);
            if (index < 0) return false;

            var noteData = list[index];
            noteData.PositionX = x;
            noteData.PositionY = y;
            list[index] = noteData;
            updatedData = noteData;
            return true;
        }

        public bool RemoveDiaryNoteNonSaving(string noteId, out DiaryNotePlacedData removedData)
        {
            removedData = default;
            if (string.IsNullOrEmpty(noteId)) return false;

            var list = ActiveSave.PlacedDiaryNotes;
            int index = list.FindIndex(note => note.NoteId == noteId);
            if (index < 0) return false;

            removedData = list[index];
            list.RemoveAt(index);
            return true;
        }

        public void SetPageStyleNonSaving(int pageIndex, int styleIndex)
        {
            var list = ActiveSave.PageStyles;
            int index = list.FindIndex(style => style.PageIndex == pageIndex);
            var data = new PageStyleData(pageIndex, styleIndex);
            if (index >= 0)
            {
                list[index] = data;
            }
            else
            {
                list.Add(data);
            }
        }

        public void RestoreMemoryStateNonSaving(List<DiaryNotePlacedData> notes, List<PageStyleData> styles)
        {
            ActiveSave.PlacedDiaryNotes = notes != null ? new List<DiaryNotePlacedData>(notes) : new List<DiaryNotePlacedData>();
            ActiveSave.PageStyles = styles != null ? new List<PageStyleData>(styles) : new List<PageStyleData>();
        }
    }
}
