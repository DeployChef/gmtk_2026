using VContainer;
using VContainer.Unity;

namespace TheyWillDescend.Main.DI
{
    /// <summary>
    /// Game DI scope on Game.unity. Disable Auto Run — <see cref="GameDirector"/> builds after additive load.
    /// Parent: RootLifetimeScope (set in code before Build).
    /// Composition root for Gameplay + scene UI registrations.
    /// </summary>
    public sealed class GameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // Register Gameplay and Game-scene UI here as systems appear.
        }
    }
}
