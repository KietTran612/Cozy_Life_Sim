using UnityEngine;
using VContainer;
using VContainer.Unity;
using CozyLifeSim.Core;
using CozyLifeSim.UI.Style;
using CozyLifeSim.UI.Services;

namespace CozyLifeSim.UI
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private UIStyleConfig _defaultStyleConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            // Register Style Service as Singleton in Presentation boundary
            builder.Register<IStyleService>(container => new StyleService(_defaultStyleConfig), Lifetime.Singleton);

            // Register Save and Inventory Services as singletons
            builder.Register<ISaveService, SaveService>(Lifetime.Singleton);
            builder.Register<IInventoryService, InventoryService>(Lifetime.Singleton);
        }
    }
}
