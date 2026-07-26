using Cysharp.Threading.Tasks;
using TheyWillDescend.Core;
using TheyWillDescend.Core.Audio;
using TheyWillDescend.Core.Timeline;
using TheyWillDescend.Main.DI;
using TheyWillDescend.Main.GameAppStates;
using TheyWillDescend.UI.Session;
using TheyWillDescend.UI.Timeline;
using TheyWillDescend.UI.Tutorial;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace TheyWillDescend.Main
{
    /// <summary>
    /// App-level only: load/unload Game, Enter GameStartState. No inventory / win-lose logic.
    /// </summary>
    public sealed class GameDirector : IGameDirector
    {
        private readonly RootLifetimeScope _rootScope;
        private GameLifetimeScope _gameScope;

        public GameDirector(RootLifetimeScope rootScope)
        {
            _rootScope = rootScope;
        }

        public async UniTask StartAsync()
        {
            Debug.Log("[GameDirector] Start — loading Game scene…");
            await LoadGameSceneAsync();
            EnterStartState();
        }

        public async UniTask RestartAsync()
        {
            Debug.Log("[GameDirector] Restart — reload Game scene.");
            await UnloadGameSceneAsync();
            await LoadGameSceneAsync();
            EnterStartState();
        }

        public UniTask SoftRestartToFirstPhaseAsync()
        {
            if (_gameScope?.Container == null)
            {
                Debug.LogWarning("[GameDirector] SoftRestart: no Game scope — falling back to full Restart.");
                return RestartAsync();
            }

            var container = _gameScope.Container;
            var timeline = container.Resolve<ITimelineService>();
            var audio = container.Resolve<IAudioManager>();

            Debug.Log("[GameDirector] SoftRestart → phase 1 (StartRun, no scene reload).");

            audio?.StopMusic();
            audio?.StopAmbient();
            audio?.SetMusicPitch(1f);
            audio?.Play(AudioCatalog.Ids.MusicMain);
            audio?.PlayAmbient(AudioCatalog.Ids.AmbientMain);

            var results = Object.FindFirstObjectByType<ResultScreenController>();
            results?.ResetPresentation();

            foreach (var hud in Object.FindObjectsByType<PyramidTimerWorldHud>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                hud.ClearVictoryTextOverride();

            foreach (var spin in Object.FindObjectsByType<CalendarSpinView>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                spin.ClearVictorySpinOverride();

            var boom = GameObject.Find("boom");
            if (boom != null)
            {
                boom.SetActive(false);
                var animator = boom.GetComponent<Animator>();
                if (animator != null)
                    animator.Rebind();
            }

            timeline.StartRun();

            var tutorial = Object.FindFirstObjectByType<TutorialHintController>();
            if (tutorial != null)
            {
                tutorial.Stop();
                tutorial.Begin();
            }

            return UniTask.CompletedTask;
        }

        private void EnterStartState()
        {
            if (_gameScope?.Container == null)
                return;

            _gameScope.Container.Resolve<GameStartState>().Enter();
        }

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
