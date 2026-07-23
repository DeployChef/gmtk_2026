using System;
using System.Collections.Generic;

namespace TheyWillDescend.Core.Bus
{
    public sealed class GameEventBus : IGameEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new();

        public void Publish<T>(T message) where T : struct
        {
            if (!_handlers.TryGetValue(typeof(T), out var list))
                return;

            var snapshot = list.ToArray();
            foreach (var handler in snapshot)
                ((Action<T>)handler)(message);
        }

        public IDisposable Subscribe<T>(Action<T> handler) where T : struct
        {
            if (!_handlers.TryGetValue(typeof(T), out var list))
            {
                list = new List<Delegate>();
                _handlers[typeof(T)] = list;
            }

            list.Add(handler);
            return new Subscription(() => list.Remove(handler));
        }

        private sealed class Subscription : IDisposable
        {
            private readonly Action _onDispose;

            public Subscription(Action onDispose) => _onDispose = onDispose;

            public void Dispose() => _onDispose();
        }
    }
}
