using UnityEngine;
using VContainer;
using VContainer.Unity;
using CozyLifeSim.Core;
using CozyLifeSim.UI.Style;
using CozyLifeSim.UI.Services;
using CozyLifeSim.UI.Presenters;

namespace CozyLifeSim.UI
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private UIStyleConfig _defaultStyleConfig;
        [SerializeField] private CozyLifeSim.UI.Settings.QuestDatabase _questDatabase;

        protected override void Configure(IContainerBuilder builder)
        {
            // Register Style Service as Singleton in Presentation boundary
            builder.Register<IStyleService>(container => new StyleService(_defaultStyleConfig), Lifetime.Singleton);

            // Register Save and Inventory Services as singletons
            builder.Register<ISaveService, SaveService>(Lifetime.Singleton);
            builder.Register<IInventoryService, InventoryService>(Lifetime.Singleton);

            // Register Memory and Quest singletons
            builder.Register<IMemoryService, MemoryService>(Lifetime.Singleton);
            
            // Register Quest Service via 100% VContainer-safe nullable Lambda Factory
            builder.Register<IQuestService>(resolver => new CozyLifeSim.UI.Services.QuestService(
                resolver.Resolve<ISaveService>(),
                resolver.Resolve<IInventoryService>(),
                _questDatabase), Lifetime.Singleton);

            // Register Presenters
            builder.Register<FarmPresenter>(Lifetime.Singleton);
            builder.Register<AnimalPresenter>(Lifetime.Singleton);
            builder.Register<StickerBookPresenter>(Lifetime.Singleton);
        }
    }
}
