using System;

namespace CozyLifeSim.UI.Style
{
    public interface IStyleService
    {
        UIStyleConfig CurrentConfig { get; }
        event Action OnStyleChanged;
        void ApplyTheme(UIStyleConfig newConfig);
    }
}
