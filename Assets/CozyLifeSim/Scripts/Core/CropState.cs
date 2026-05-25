using System.Runtime.InteropServices;

namespace CozyLifeSim.Core
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CropState
    {
        public int CropId;
        public int GrowthStage; // 0: Seed, 1: Sprout, 2: Mature, 3: Harvestable
        public float TimeRemainingSeconds;
        public bool IsWatered;

        public CropState(int cropId, int growthStage, float timeRemainingSeconds, bool isWatered)
        {
            CropId = cropId;
            GrowthStage = growthStage;
            TimeRemainingSeconds = timeRemainingSeconds;
            IsWatered = isWatered;
        }
    }
}
