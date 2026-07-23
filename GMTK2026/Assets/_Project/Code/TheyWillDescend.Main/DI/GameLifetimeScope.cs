using TheyWillDescend.Core.Inventory;
using TheyWillDescend.Gameplay.Buildings;
using TheyWillDescend.Gameplay.Inventory;
using TheyWillDescend.Main.GameAppStates;
using TheyWillDescend.UI.Buildings;
using TheyWillDescend.UI.Cards;
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
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<InventoryService>(Lifetime.Singleton).As<IInventory>();
            builder.Register<GameStartState>(Lifetime.Singleton);
            builder.RegisterComponentInHierarchy<InventoryTraysView>();

            builder.RegisterBuildCallback(resolver =>
            {
                foreach (var building in Object.FindObjectsByType<ProductionBuilding>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                    resolver.Inject(building);

                foreach (var hud in Object.FindObjectsByType<BuildingWorldHud>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                    resolver.Inject(hud);

                foreach (var card in Object.FindObjectsByType<ResourceCardView>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                    resolver.Inject(card);
            });
        }
    }
}
