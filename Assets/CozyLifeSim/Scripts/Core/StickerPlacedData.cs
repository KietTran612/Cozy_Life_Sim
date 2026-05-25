using System.Runtime.InteropServices;

namespace CozyLifeSim.Core
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StickerPlacedData
    {
        public int StickerId;
        public int PageIndex;
        public float PositionX;
        public float PositionY;
        public float Scale;
        public float Rotation;

        public StickerPlacedData(int stickerId, int pageIndex, float positionX, float positionY, float scale, float rotation)
        {
            StickerId = stickerId;
            PageIndex = pageIndex;
            PositionX = positionX;
            PositionY = positionY;
            Scale = scale;
            Rotation = rotation;
        }
    }
}
