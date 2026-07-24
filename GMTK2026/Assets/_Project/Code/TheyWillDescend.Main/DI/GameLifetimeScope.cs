using TheyWillDescend.Core.Cheats;
using TheyWillDescend.Core.Hazards;
using TheyWillDescend.Core.Inventory;
using TheyWillDescend.Core.Timeline;
using TheyWillDescend.Gameplay.Buildings;
using TheyWillDescend.Gameplay.Hazards;
using TheyWillDescend.Gameplay.Inventory;
using TheyWillDescend.Gameplay.Session;
using TheyWillDescend.Main.GameAppStates;
using TheyWillDescend.UI.Buildings;
using TheyWillDescend.UI.Cards;
using TheyWillDescend.UI.Timeline;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace TheyWillDescend.Main.DI
{
    /// <summary>
    /// Game DI scope on Game.unity. Disable Auto Run — <see cref="GameDirector"/> builds after additive load.
    /// Parent: RootLifetimeScope (set in code before Build).
    /// </summary>
    public sealed class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private GameTimelineConfig timelineConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            var config = timelineConfig;
            if (config == null)
            {
                Debug.LogError(
                    "[GameLifetimeScope] Assign GameTimelineConfig on GameLifetimeScope (Inspector). " +
                    "Using empty runtime stub until then.");
                config = ScriptableObject.CreateInstance<GameTimelineConfig>();
            }

            builder.RegisterInstance(config);
            builder.Register<PhaseLoadoutApplier>(Lifetime.Singleton).As<IPhaseLoadoutApplier>();
            builder.Register<PyramidTimerService>(Lifetime.Singleton).As<IPyramidTimerService>();
            builder.Register<TimelineService>(Lifetime.Singleton).As<ITimelineService>();
            builder.RegisterEntryPoint<TimelineSessionDriver>();

            builder.Register<InventoryService>(Lifetime.Singleton).As<IInventory>();
            builder.Register<GameStartState>(Lifetime.Singleton);
            builder.Register<ThunderService>(Lifetime.Singleton).As<IThunderService>();

            builder.RegisterComponentInHierarchy<InventoryTraysView>();
            builder.RegisterComponentInHierarchy<DisasterManager>().As<IDisasterManager>();
            builder.RegisterComponentInHierarchy<PyramidOfferingPoint>();
            builder.RegisterComponentInHierarchy<PyramidTimerWorldHud>();
            builder.RegisterComponentInHierarchy<PyramidOfferWorldHud>();
            builder.RegisterComponentInHierarchy<RandomStrikeButton>();
            builder.RegisterComponentInHierarchy<TimelineHudView>();

            builder.RegisterBuildCallback(resolver =>
            {
                foreach (var building in Object.FindObjectsByType<ProductionBuilding>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                    resolver.Inject(building);

                foreach (var hud in Object.FindObjectsByType<BuildingWorldHud>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                    resolver.Inject(hud);

                foreach (var constructionHud in Object.FindObjectsByType<BuildingConstructionHud>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                    resolver.Inject(constructionHud);

                foreach (var card in Object.FindObjectsByType<ResourceCardView>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                    resolver.Inject(card);
            });
        }
    }
}
