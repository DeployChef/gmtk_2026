using Cysharp.Threading.Tasks;
using TheyWillDescend.Core;
using TheyWillDescend.Main.DI;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace TheyWillDescend.Main
{
    public sealed class GameDirector : IGameDirector
    {
        private readonly RootLifetimeScope _rootScope;
        private GameLifetimeScope _gameScope;

        public GameDirector(RootLifetimeScope rootScope)
        {
            _rootScope = rootScope;
        }

        public async UniTask InitializeGameAsync()
        {
            Debug.Log("[GameDirector] Cold start — loading Game scene…");
            await LoadGameSceneAsync();
            Debug.Log("[GameDirector] Game scope ready.");
        }

        public async UniTask StartNewRunAsync()
        {
            await RestartRunAsync();
        }

        public async UniTask RestartRunAsync()
        {
            Debug.Log("[GameDirector] Restart run — reload Game scene.");
            await UnloadGameSceneAsync();
            await LoadGameSceneAsync();
        }

        public void NotifyRunWon() =>
            Debug.Log("[GameDirector] Run won.");

        public void NotifyRunLost() =>
            Debug.Log("[GameDirector] Run lost.");

        private async UniTask LoadGameSceneAsync()
        {
            await UniTask.Yield();

            if (!TryFindLoadedGameScene(out _))
            {
                var op = SceneManager.LoadSceneAsync(GameScenes.Game, LoadSceneMode.Additive);
                if (op == null)
                {
                    Debug.LogError(
                        $"[GameDirector] Failed to load '{GameScenes.Game}'. Is it in Build Settings?");
                    return;
                }

                await op.ToUniTask();
            }

            if (!TryFindLoadedGameScene(out var gameScene))
            {
                Debug.LogError($"[GameDirector] Scene '{GameScenes.Game}' not loaded.");
                return;
            }

            SceneManager.SetActiveScene(gameScene);

            var found = LifetimeScope.Find<GameLifetimeScope>(gameScene);
            _gameScope = found as GameLifetimeScope;
            if (_gameScope == null)
            {
                Debug.LogError(
                    "[GameDirector] GameLifetimeScope not found on Game scene. Add it and disable Auto Run.");
                return;
            }

            if (_gameScope.Container != null)
            {
                Debug.LogWarning("[GameDirector] GameLifetimeScope already built — skipping Build().");
                return;
            }

            _gameScope.parentReference.Object = _rootScope;
            _gameScope.Build();
        }

        private async UniTask UnloadGameSceneAsync()
        {
            if (_gameScope != null)
            {
                _gameScope.DisposeCore();
                _gameScope = null;
            }

            if (!TryFindLoadedGameScene(out var gameScene))
                return;

            await SceneManager.UnloadSceneAsync(gameScene).ToUniTask();
        }

        private static bool TryFindLoadedGameScene(out Scene scene)
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var candidate = SceneManager.GetSceneAt(i);
                if (candidate.name == GameScenes.Game && candidate.isLoaded)
                {
                    scene = candidate;
                    return true;
                }
            }

            scene = default;
            return false;
        }
    }
}
