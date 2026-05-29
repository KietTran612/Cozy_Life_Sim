using System;
using CozyLifeSim.Core;
using VContainer;

namespace CozyLifeSim.UI.Services
{
    public class ProgressionService : IProgressionService
    {
        private readonly ISaveService _saveService;

        public int PlayerLevel => _saveService.ActiveSave.PlayerLevel;
        public int PlayerXP => _saveService.ActiveSave.PlayerXP;

        public event Action<int> OnLevelUp;
        public event Action<int> OnXPChanged;

        [Inject]
        public ProgressionService(ISaveService saveService)
        {
            _saveService = saveService;
        }

        public void AddXP(int amount)
        {
            if (amount <= 0) return;

            var save = _saveService.ActiveSave;
            save.PlayerXP += amount;
            OnXPChanged?.Invoke(save.PlayerXP);

            int xpRequired = GetXPThreshold(save.PlayerLevel);
            bool levelUp = false;

            while (save.PlayerXP >= xpRequired)
            {
                save.PlayerXP -= xpRequired;
                save.PlayerLevel++;
                levelUp = true;
                xpRequired = GetXPThreshold(save.PlayerLevel);
            }

            if (levelUp)
            {
                OnLevelUp?.Invoke(save.PlayerLevel);
                OnXPChanged?.Invoke(save.PlayerXP);
            }
            
            _saveService.Save();
        }

        private int GetXPThreshold(int level)
        {
            return level * 100;
        }
    }
}
