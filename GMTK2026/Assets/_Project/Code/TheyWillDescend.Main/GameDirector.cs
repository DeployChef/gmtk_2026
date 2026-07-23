using Cysharp.Threading.Tasks;
using TheyWillDescend.Core;
using UnityEngine;

namespace TheyWillDescend.Main
{
    public sealed class GameDirector : IGameDirector
    {
        public UniTask InitializeGameAsync()
        {
            Debug.Log("[GameDirector] Cold start — Root scope ready.");
            return UniTask.CompletedTask;
        }

        public UniTask StartNewRunAsync()
        {
            Debug.Log("[GameDirector] StartNewRun — stub.");
            return UniTask.CompletedTask;
        }

        public UniTask RestartRunAsync()
        {
            Debug.Log("[GameDirector] RestartRun — stub.");
            return UniTask.CompletedTask;
        }

        public void NotifyRunWon() =>
            Debug.Log("[GameDirector] Run won.");

        public void NotifyRunLost() =>
            Debug.Log("[GameDirector] Run lost.");
    }
}
