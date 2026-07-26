using System;
using Cysharp.Threading.Tasks;
using TheyWillDescend.Core;
using TheyWillDescend.Core.Audio;
using TheyWillDescend.Core.Bus;
using TheyWillDescend.Core.Bus.Events;
using TheyWillDescend.Core.Session;
using UnityEngine;
using VContainer;

namespace TheyWillDescend.UI.Session
{
    /// <summary>
    /// Shows Win/Lose canvases when GameWonEvent / GameLostEvent are published.
    /// Timer-expire lose plays <see cref="LoseSequenceDriver"/> before the lose canvas.
    /// </summary>
    public sealed class ResultScreenController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup winCanvas;
        [SerializeField] private CanvasGroup loseCanvas;
        [SerializeField] private UnityEngine.UI.Button winRestartButton;
        [SerializeField] private UnityEngine.UI.Button loseRestartButton;
        [SerializeField] private LoseSequenceDriver loseSequence;

        private IGameEventBus _bus;
        private IAudioManager _audio;
        private IDisposable _wonSub;
        private IDisposable _lostSub;
        private IDisposable _runStartedSub;

        [Inject]
        public void Construct(
            IGameEventBus bus,
            IGameDirector director,
            IAudioManager audio,
            IGameplayTimePause timePause)
        {
            _bus = bus;
            _audio = audio;

            if (loseSequence == null)
                loseSequence = GetComponent<LoseSequenceDriver>();

            loseSequence?.BindPause(timePause);

            if (winRestartButton != null)
                winRestartButton.onClick.AddListener(() =>
                {
                    _audio?.Stop(AudioCatalog.Ids.Victory);
                    director.RestartAsync();
                });

            if (loseRestartButton != null)
                loseRestartButton.onClick.AddListener(() =>
                {
                    _audio?.Stop(AudioCatalog.Ids.Defeat);
                    director.RestartAsync();
                });

            _wonSub = _bus.Subscribe<GameWonEvent>(_ => Show(winCanvas));
            _lostSub = _bus.Subscribe<GameLostEvent>(OnLost);
            _runStartedSub = _bus.Subscribe<RunStartedEvent>(_ =>
            {
                Hide(winCanvas);
                Hide(loseCanvas);
            });
        }

        private void Awake()
        {
            Hide(winCanvas);
            Hide(loseCanvas);
        }

        private void OnDestroy()
        {
            _wonSub?.Dispose();
            _lostSub?.Dispose();
            _runStartedSub?.Dispose();
        }

        private void OnLost(GameLostEvent e)
        {
            // Cheat + timer expire both play the meteor sequence when driver is present.
            if (loseSequence != null &&
                (e.Cause == GameResultCause.PyramidTimerExpired || e.Cause == GameResultCause.Cheat))
            {
                PlayLoseSequenceThenShow().Forget();
                return;
            }

            Show(loseCanvas);
        }

        private async UniTaskVoid PlayLoseSequenceThenShow()
        {
            try
            {
                await loseSequence.PlayAsync(destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Scene unload / destroy.
            }

            Show(loseCanvas);
        }

        private static void Show(CanvasGroup cg)
        {
            if (cg == null)
                return;

            var canvas = cg.GetComponent<Canvas>();
            if (canvas != null)
                canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 100);

            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        private static void Hide(CanvasGroup cg)
        {
            if (cg == null)
                return;
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
    }
}
