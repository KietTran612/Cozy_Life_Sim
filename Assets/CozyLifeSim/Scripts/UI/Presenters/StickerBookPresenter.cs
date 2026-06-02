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

        public event Action<DiaryNotePlacedData> OnDiaryNoteAdded;
        public event Action<DiaryNotePlacedData> OnDiaryNotePositionUpdated;
        public event Action<string> OnDiaryNoteRemoved;
        public event Action<PageStyleData> OnPageStyleChanged;

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

        public IReadOnlyList<DiaryNotePlacedData> GetPlacedDiaryNotes()
        {
            return _memory.PlacedDiaryNotes;
        }

        public IReadOnlyList<PageStyleData> GetPageStyles()
        {
            return _memory.PageStyles;
        }

        public int GetPageStyle(int pageIndex)
        {
            foreach (var style in _memory.PageStyles)
            {
                if (style.PageIndex == pageIndex)
                {
                    return style.StyleIndex;
                }
            }
            return 0;
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

        public bool TrySetPageStyle(int pageIndex, int styleIndex, int maxStyleCount)
        {
            if (maxStyleCount <= 0) return false;

            int clampedStyle = Math.Max(0, Math.Min(styleIndex, maxStyleCount - 1));
            var notesSnapshot = new List<DiaryNotePlacedData>(_memory.PlacedDiaryNotes);
            var stylesSnapshot = new List<PageStyleData>(_memory.PageStyles);

            _memory.SetPageStyleNonSaving(pageIndex, clampedStyle);
            try
            {
                _saveService.Save();
                var styleData = new PageStyleData(pageIndex, clampedStyle);
                OnPageStyleChanged?.Invoke(styleData);
                return true;
            }
            catch (Exception)
            {
                _memory.RestoreMemoryStateNonSaving(notesSnapshot, stylesSnapshot);
                return false;
            }
        }

        public string TryAddDiaryNote(string text, float x, float y, int pageIndex)
        {
            string normalizedText = NormalizeNoteText(text);
            if (string.IsNullOrEmpty(normalizedText)) return null;

            var notesSnapshot = new List<DiaryNotePlacedData>(_memory.PlacedDiaryNotes);
            var stylesSnapshot = new List<PageStyleData>(_memory.PageStyles);
            var data = _memory.AddDiaryNoteNonSaving(null, normalizedText, x, y, pageIndex);

            try
            {
                _saveService.Save();
                OnDiaryNoteAdded?.Invoke(data);
                return data.NoteId;
            }
            catch (Exception)
            {
                _memory.RestoreMemoryStateNonSaving(notesSnapshot, stylesSnapshot);
                return null;
            }
        }

        public bool TryUpdateDiaryNotePosition(string noteId, float x, float y)
        {
            if (string.IsNullOrEmpty(noteId)) return false;

            var notesSnapshot = new List<DiaryNotePlacedData>(_memory.PlacedDiaryNotes);
            var stylesSnapshot = new List<PageStyleData>(_memory.PageStyles);
            if (!_memory.UpdateDiaryNotePositionNonSaving(noteId, x, y, out var updatedData))
            {
                return false;
            }

            try
            {
                _saveService.Save();
                OnDiaryNotePositionUpdated?.Invoke(updatedData);
                return true;
            }
            catch (Exception)
            {
                _memory.RestoreMemoryStateNonSaving(notesSnapshot, stylesSnapshot);
                return false;
            }
        }

        public bool TryRemoveDiaryNote(string noteId)
        {
            if (string.IsNullOrEmpty(noteId)) return false;

            var notesSnapshot = new List<DiaryNotePlacedData>(_memory.PlacedDiaryNotes);
            var stylesSnapshot = new List<PageStyleData>(_memory.PageStyles);
            if (!_memory.RemoveDiaryNoteNonSaving(noteId, out _))
            {
                return false;
            }

            try
            {
                _saveService.Save();
                OnDiaryNoteRemoved?.Invoke(noteId);
                return true;
            }
            catch (Exception)
            {
                _memory.RestoreMemoryStateNonSaving(notesSnapshot, stylesSnapshot);
                return false;
            }
        }

        private static string NormalizeNoteText(string text)
        {
            string value = text == null ? string.Empty : text.Trim();
            return value.Length <= 60 ? value : value.Substring(0, 60);
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
