using System;

namespace CozyLifeSim.Core
{
    public interface IProgressionService
    {
        int PlayerLevel { get; }
        int PlayerXP { get; }
        void AddXP(int amount);
        void AddXPNonSaving(int amount);
        void SetProgressionNonSaving(int level, int xp);
        event Action<int> OnLevelUp;
        event Action<int> OnXPChanged;
    }
}
