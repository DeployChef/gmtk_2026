using System;

namespace TheyWillDescend.Core.Bus
{
    public interface IGameEventBus
    {
        void Publish<T>(T message) where T : struct;
        IDisposable Subscribe<T>(Action<T> handler) where T : struct;
    }
}
