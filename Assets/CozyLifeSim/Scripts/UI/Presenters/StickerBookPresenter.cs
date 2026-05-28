using System;
using System.Collections.Generic;
using VContainer;
using CozyLifeSim.Core;

namespace CozyLifeSim.UI.Presenters
{
    public class StickerBookPresenter : IDisposable
    {
        private readonly IMemoryService _memory;

        [Inject]
        public StickerBookPresenter(IMemoryService memory)
        {
            _memory = memory;
        }

        public IReadOnlyList<StickerPlacedData> GetPlacedStickers()
        {
            return _memory.PlacedStickers;
        }

        public void SaveStickerPosition(int stickerId, int pageIndex, float x, float y, float scale, float rot)
        {
            var data = new StickerPlacedData(stickerId, pageIndex, x, y, scale, rot);
            _memory.PlaceSticker(data);
        }

        public void RemoveSticker(int stickerId, int pageIndex)
        {
            _memory.RemoveSticker(stickerId, pageIndex);
        }

        public void Dispose() { }
    }
}
