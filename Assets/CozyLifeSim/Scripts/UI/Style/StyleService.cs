using System;

namespace CozyLifeSim.UI.Style
{
    public class StyleService : IStyleService
    {
        public UIStyleConfig CurrentConfig { get; private set; }
        public event Action OnStyleChanged;

        public StyleService(UIStyleConfig initialConfig)
        {
            CurrentConfig = initialConfig;
        }

        public void ApplyTheme(UIStyleConfig newConfig)
        {
            if (newConfig == null || CurrentConfig == newConfig) return;
            CurrentConfig = newConfig;
            OnStyleChanged?.Invoke();
        }
    }
}
