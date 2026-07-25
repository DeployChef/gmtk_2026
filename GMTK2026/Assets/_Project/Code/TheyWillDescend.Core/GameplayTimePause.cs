using System.Collections.Generic;
using UnityEngine;

namespace TheyWillDescend.Core
{
    public sealed class GameplayTimePause : IGameplayTimePause
    {
        private readonly HashSet<object> _keys = new();

        public bool IsPaused => _keys.Count > 0;

        public void Acquire(object key)
        {
            if (key == null || !_keys.Add(key))
                return;

            Time.timeScale = 0f;
        }

        public void Release(object key)
        {
            if (key == null || !_keys.Remove(key))
                return;

            if (_keys.Count == 0)
                Time.timeScale = 1f;
        }
    }
}
