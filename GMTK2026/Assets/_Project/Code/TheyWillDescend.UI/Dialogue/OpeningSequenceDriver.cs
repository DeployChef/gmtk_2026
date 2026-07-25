using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TheyWillDescend.Core;
using TheyWillDescend.Core.Audio;
using TheyWillDescend.Core.Dialogue;
using TheyWillDescend.UI.Buildings;
using TheyWillDescend.UI.Timeline;
using UnityEngine;
using VContainer;

namespace TheyWillDescend.UI.Dialogue
{
    /// <summary>
    /// Opening: hold intro + grass drift → approach Play + grass exit → storm → pyramid → dialogue →
    /// pull to Play → bottom bar.
    /// </summary>
    public sealed class OpeningSequenceDriver : MonoBehaviour, IOpeningSequence
    {
        [Header("Debug / skip")]
        [Tooltip("If enabled, skips the whole intro and starts gameplay UI immediately.")]
        [SerializeField] private bool skipIntro;

        [Header("Content")]
        [SerializeField] private DialogueDefinition introDialogue;

        [Header("Scene refs")]
        [SerializeField] private IntroCameraDirector cameraDirector;
        [SerializeField] private PyramidTimerWorldHud pyramidTimerHud;
        [SerializeField] private BottomBarSlabReveal bottomBarReveal;
        [SerializeField] private IntroGrassAnimator introGrass;

        [Header("Storm SFX (no VFX)")]
        [SerializeField] private string thunderSoundId = AudioCatalog.Ids.Thunder;
        [SerializeField] private string fireSoundId = AudioCatalog.Ids.Fire;
        [SerializeField] private int stormBurstCount = 3;
        [SerializeField] private float stormStagger = 0.35f;
        [SerializeField] private float afterStormPause = 0.4f;

        [Header("Timing (unscaled seconds)")]
        [Tooltip("Seconds to stay on IntroStart while grass drifts, before the approach blend.")]
        [SerializeField] private float introHold = 2.4f;
        [Tooltip("Must be >= Custom Blend time IntroStart→Play.")]
        [SerializeField] private float approachWait = 10f;
        [SerializeField] private float pyramidSnapWait = 3f;
        [SerializeField] private float returnToPlayWait = 5f;
        [SerializeField] private float bottomBarDelay = 1f;

        private IDialogueService _dialogue;
        private IAudioManager _audio;
        private bool _playing;

        [Inject]
        public void Construct(IDialogueService dialogue, IAudioManager audio)
        {
            _dialogue = dialogue;
            _audio = audio;
        }

        public async UniTask PlayAsync(CancellationToken cancellationToken = default)
        {
            if (_playing)
                return;

            if (skipIntro)
            {
                ApplyGameplayReadyState();
                return;
            }

            _playing = true;
            SetBuildingHudsSuppressed(true);

            try
            {
                pyramidTimerHud?.Hide();
                introGrass?.StartDrift();

                // Force IntroStart first, hold, then blend to Play with grass exit in parallel.
                if (cameraDirector != null)
                {
                    cameraDirector.SnapToIntroStart();
                    await DelayUnscaled(introHold, cancellationToken);

                    var approach = cameraDirector.TransitionToPlayAsync(approachWait)
                        .AttachExternalCancellation(cancellationToken);
                    var grassExit = introGrass != null
                        ? introGrass.PlayExitAsync(approachWait, cancellationToken)
                        : UniTask.CompletedTask;

                    await UniTask.WhenAll(approach, grassExit);
                }
                else
                {
                    await DelayUnscaled(introHold + approachWait, cancellationToken);
                    if (introGrass != null)
                        await introGrass.PlayExitAsync(approachWait, cancellationToken);
                }

                await PlayStormSfxAsync(cancellationToken);
                await DelayUnscaled(afterStormPause, cancellationToken);

                pyramidTimerHud?.Show();

                if (cameraDirector != null)
                    await cameraDirector.TransitionToPyramidAsync(pyramidSnapWait)
                        .AttachExternalCancellation(cancellationToken);

                await PlayIntroDialogueAsync(cancellationToken);

                if (cameraDirector != null)
                    await cameraDirector.TransitionToPlayAsync(returnToPlayWait)
                        .AttachExternalCancellation(cancellationToken);

                await DelayUnscaled(bottomBarDelay, cancellationToken);

                if (bottomBarReveal != null)
                    await bottomBarReveal.RevealAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Sequence aborted (scene unload / destroy).
            }
            finally
            {
                introGrass?.SnapHidden();
                SetBuildingHudsSuppressed(false);
                _playing = false;
            }
        }

        private void ApplyGameplayReadyState()
        {
            cameraDirector?.SnapToPlay();
            introGrass?.SnapHidden();
            pyramidTimerHud?.Show();
            bottomBarReveal?.SnapRevealed();
            SetBuildingHudsSuppressed(false);
        }

        private async UniTask PlayStormSfxAsync(CancellationToken cancellationToken)
        {
            if (_audio == null)
                return;

            var bursts = Mathf.Max(1, stormBurstCount);
            for (var i = 0; i < bursts; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!string.IsNullOrEmpty(thunderSoundId))
                    _audio.Play(thunderSoundId, pitchRandomRange: 0.08f);

                if (!string.IsNullOrEmpty(fireSoundId))
                    _audio.Play(fireSoundId, pitchRandomRange: 0.06f);

                if (i < bursts - 1)
                    await DelayUnscaled(stormStagger, cancellationToken);
            }
        }

        private async UniTask PlayIntroDialogueAsync(CancellationToken cancellationToken)
        {
            if (introDialogue == null || _dialogue == null)
                return;

            var tcs = new UniTaskCompletionSource();
            using var reg = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

            if (!_dialogue.TryPlay(introDialogue, () => tcs.TrySetResult()))
            {
                tcs.TrySetResult();
                return;
            }

            await tcs.Task;
        }

        private static UniTask DelayUnscaled(float seconds, CancellationToken cancellationToken)
        {
            if (seconds <= 0f)
                return UniTask.CompletedTask;

            return UniTask.Delay(
                TimeSpan.FromSeconds(seconds),
                DelayType.UnscaledDeltaTime,
                cancellationToken: cancellationToken);
        }

        private static void SetBuildingHudsSuppressed(bool suppressed)
        {
            foreach (var hud in UnityEngine.Object.FindObjectsByType<BuildingWorldHud>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                hud.SetSuppressed(suppressed);

            foreach (var hud in UnityEngine.Object.FindObjectsByType<BuildingConstructionHud>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                hud.SetSuppressed(suppressed);
        }
    }
}
