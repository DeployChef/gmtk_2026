using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TheyWillDescend.Core;
using TheyWillDescend.Core.Audio;
using TheyWillDescend.Core.Bus;
using TheyWillDescend.Core.Bus.Events;
using TheyWillDescend.Core.Dialogue;
using TheyWillDescend.Core.Session;
using TheyWillDescend.Core.Timeline;
using UnityEngine;
using VContainer;

namespace TheyWillDescend.UI.Session
{
    /// <summary>
    /// Shows Win/Lose canvases after optional win/lose cinematics.
    /// </summary>
    public sealed class ResultScreenController : MonoBehaviour
    {
        private const string MoveTrigger = "Move";
        private const string MoveUpState = "MoveUp";

        [SerializeField] private CanvasGroup winCanvas;
        [SerializeField] private CanvasGroup loseCanvas;
        [SerializeField] private UnityEngine.UI.Button winRestartButton;
        [SerializeField] private UnityEngine.UI.Button loseRestartButton;
        [SerializeField] private LoseSequenceDriver loseSequence;
        [SerializeField] private WinSequenceDriver winSequence;
        [Header("Win credits")]
        [SerializeField] private Animator winCreditsAnimator;
        [SerializeField] private CanvasGroup winEndButtons;
        [SerializeField] private float endButtonsFadeDuration = 0.8f;

        private IGameEventBus _bus;
        private IAudioManager _audio;
        private IDisposable _wonSub;
        private IDisposable _lostSub;
        private IDisposable _runStartedSub;
        private Tween _endButtonsTween;

        [Inject]
        public void Construct(
            IGameEventBus bus,
            IGameDirector director,
            IAudioManager audio,
            IGameplayTimePause timePause,
            IDialogueService dialogue,
            ITimelineService timeline)
        {
            _bus = bus;
            _audio = audio;

            if (loseSequence == null)
                loseSequence = GetComponent<LoseSequenceDriver>();
            if (winSequence == null)
                winSequence = GetComponent<WinSequenceDriver>();

            loseSequence?.BindPause(timePause);
            winSequence?.Bind(timePause, dialogue, timeline, audio);

            if (winRestartButton != null)
                winRestartButton.onClick.AddListener(() =>
                {
                    _audio?.StopMusic();
                    director.RestartAsync();
                });

            if (loseRestartButton != null)
                loseRestartButton.onClick.AddListener(() =>
                {
                    _audio?.Stop(AudioCatalog.Ids.Defeat);
                    director.RestartAsync();
                });

            _wonSub = _bus.Subscribe<GameWonEvent>(OnWon);
            _lostSub = _bus.Subscribe<GameLostEvent>(OnLost);
            _runStartedSub = _bus.Subscribe<RunStartedEvent>(_ =>
            {
                Hide(winCanvas);
                Hide(loseCanvas);
                HideEndButtonsImmediate();
            });
        }

        private void Awake()
        {
            Hide(winCanvas);
            Hide(loseCanvas);
            ResolveEndButtons();
            HideEndButtonsImmediate();

            if (winCreditsAnimator == null)
            {
                var credits = GameObject.Find("WinBackground")?.transform.Find("Text (TMP)");
                if (credits != null)
                    winCreditsAnimator = credits.GetComponent<Animator>();
            }
        }

        private void OnDestroy()
        {
            _endButtonsTween?.Kill();
            _wonSub?.Dispose();
            _lostSub?.Dispose();
            _runStartedSub?.Dispose();
        }

        private void OnWon(GameWonEvent e)
        {
            if (winSequence != null &&
                (e.Cause == GameResultCause.FinalOfferCompleted
                 || e.Cause == GameResultCause.AllPhasesCompleted
                 || e.Cause == GameResultCause.Cheat))
            {
                PlayWinSequenceThenShow().Forget();
                return;
            }

            _audio?.StopAmbient();
            _audio?.SetMusicPitch(1f);
            _audio?.Play(AudioCatalog.Ids.MusicFinal);
            ShowWinWithCredits().Forget();
        }

        private void OnLost(GameLostEvent e)
        {
            if (loseSequence != null &&
                (e.Cause == GameResultCause.PyramidTimerExpired || e.Cause == GameResultCause.Cheat))
            {
                PlayLoseSequenceThenShow().Forget();
                return;
            }

            Show(loseCanvas);
        }

        private async UniTaskVoid PlayWinSequenceThenShow()
        {
            try
            {
                await winSequence.PlayAsync(destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
            }

            winSequence?.PlayVictorySting();
            await ShowWinWithCredits();
        }

        private async UniTaskVoid PlayLoseSequenceThenShow()
        {
            try
            {
                await loseSequence.PlayAsync(destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
            }

            Show(loseCanvas);
        }

        private async UniTask ShowWinWithCredits()
        {
            HideEndButtonsImmediate();
            Show(winCanvas);
            try
            {
                await PlayCreditsMoveAsync(destroyCancellationToken);
                await FadeInEndButtonsAsync(destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async UniTask PlayCreditsMoveAsync(System.Threading.CancellationToken cancellationToken)
        {
            if (winCreditsAnimator == null)
                return;

            winCreditsAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            winCreditsAnimator.gameObject.SetActive(true);
            winCreditsAnimator.ResetTrigger(MoveTrigger);
            winCreditsAnimator.Play("Text", 0, 0f);
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            winCreditsAnimator.SetTrigger(MoveTrigger);

            // Wait until MoveUp is actually playing.
            await UniTask.WaitUntil(
                () =>
                {
                    if (winCreditsAnimator == null)
                        return true;
                    var info = winCreditsAnimator.GetCurrentAnimatorStateInfo(0);
                    return info.IsName(MoveUpState);
                },
                cancellationToken: cancellationToken);

            var duration = ResolveMoveUpDuration(winCreditsAnimator);
            await UniTask.Delay(
                TimeSpan.FromSeconds(duration),
                DelayType.UnscaledDeltaTime,
                cancellationToken: cancellationToken);
        }

        private async UniTask FadeInEndButtonsAsync(System.Threading.CancellationToken cancellationToken)
        {
            ResolveEndButtons();
            if (winEndButtons == null)
                return;

            _endButtonsTween?.Kill();
            winEndButtons.gameObject.SetActive(true);
            winEndButtons.alpha = 0f;
            winEndButtons.interactable = false;
            winEndButtons.blocksRaycasts = false;

            var duration = Mathf.Max(0.05f, endButtonsFadeDuration);
            var tcs = new UniTaskCompletionSource();
            using var reg = cancellationToken.Register(() =>
            {
                _endButtonsTween?.Kill();
                tcs.TrySetCanceled(cancellationToken);
            });

            _endButtonsTween = DOTween
                .To(() => winEndButtons.alpha, a => winEndButtons.alpha = a, 1f, duration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .OnComplete(() => tcs.TrySetResult());

            await tcs.Task;

            winEndButtons.alpha = 1f;
            winEndButtons.interactable = true;
            winEndButtons.blocksRaycasts = true;
        }

        private void ResolveEndButtons()
        {
            if (winEndButtons != null)
                return;

            var endButtons = GameObject.Find("EndButtons");
            if (endButtons != null)
                winEndButtons = endButtons.GetComponent<CanvasGroup>()
                                 ?? endButtons.AddComponent<CanvasGroup>();
        }

        private void HideEndButtonsImmediate()
        {
            ResolveEndButtons();
            _endButtonsTween?.Kill();
            if (winEndButtons == null)
                return;

            winEndButtons.alpha = 0f;
            winEndButtons.interactable = false;
            winEndButtons.blocksRaycasts = false;
            winEndButtons.gameObject.SetActive(true);
        }

        private static float ResolveMoveUpDuration(Animator animator)
        {
            const float fallback = 20f;
            if (animator == null || animator.runtimeAnimatorController == null)
                return fallback;

            foreach (var clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip != null && clip.name == MoveUpState)
                    return Mathf.Max(0.1f, clip.length);
            }

            var info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName(MoveUpState) && info.length > 0.01f)
                return info.length;

            return fallback;
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
