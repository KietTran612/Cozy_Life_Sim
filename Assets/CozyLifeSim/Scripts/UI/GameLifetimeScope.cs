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
        [SerializeField] private CozyLifeSim.UI.Settings.CropDatabase _cropDatabase;
        [SerializeField] private CozyLifeSim.UI.Settings.AnimalDatabase _animalDatabase;
        [SerializeField] private CozyLifeSim.UI.Settings.StickerDatabase _stickerDatabase;

        protected override void Configure(IContainerBuilder builder)
        {
            // Register Style Service as Singleton in Presentation boundary
            builder.Register<IStyleService>(container => new StyleService(_defaultStyleConfig), Lifetime.Singleton);

            // Null-safe singleton register: always resolves to _cropDatabase (even if null) without exception
            builder.Register<CozyLifeSim.UI.Settings.CropDatabase>(resolver => _cropDatabase, Lifetime.Singleton);

            // Null-safe singleton register: always resolves to _animalDatabase (even if null) without exception
            builder.Register<CozyLifeSim.UI.Settings.AnimalDatabase>(resolver => _animalDatabase, Lifetime.Singleton);

            // Null-safe singleton register: always resolves to _stickerDatabase (even if null) without exception
            builder.Register<CozyLifeSim.UI.Settings.StickerDatabase>(resolver => _stickerDatabase, Lifetime.Singleton);

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
