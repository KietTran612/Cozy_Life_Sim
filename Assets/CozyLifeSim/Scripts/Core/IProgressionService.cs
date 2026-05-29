using System;

namespace CozyLifeSim.Core
{
    public interface IProgressionService
    {
        int PlayerLevel { get; }
        int PlayerXP { get; }
        void AddXP(int amount);
        event Action<int> OnLevelUp;
        event Action<int> OnXPChanged;
    }
}
