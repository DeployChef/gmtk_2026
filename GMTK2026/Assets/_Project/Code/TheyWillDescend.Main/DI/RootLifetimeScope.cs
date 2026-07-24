using TheyWillDescend.Core;
using TheyWillDescend.Core.Audio;
using TheyWillDescend.Core.Bus;
using TheyWillDescend.UI;
using TheyWillDescend.UI.Audio;
using VContainer;
using VContainer.Unity;

namespace TheyWillDescend.Main.DI
{
    /// <summary>
    /// Root DI scope on Root.unity. Disable Auto Run in Inspector — <see cref="Startup"/> calls Build().
    /// </summary>
    public sealed class RootLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<GameDirector>(Lifetime.Singleton).As<IGameDirector>();
            builder.Register<IGameEventBus, GameEventBus>(Lifetime.Singleton);
            builder.RegisterComponentInHierarchy<AudioManager>().As<IAudioManager>();
            builder.RegisterComponentInHierarchy<PauseMenuController>();
        }
    }
}
