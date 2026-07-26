using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TheyWillDescend.Core;
using TheyWillDescend.Core.Audio;
using TheyWillDescend.Core.Dialogue;
using TheyWillDescend.Core.Timeline;
using TheyWillDescend.UI.Timeline;
using UnityEngine;
using UnityEngine.UI;

namespace TheyWillDescend.UI.Session
{
    /// <summary>
    /// Win cinematic: pyramid timer ramps to 21.12.2012 → victory dialogue → slow fade → win UI.
    /// </summary>
    public sealed class WinSequenceDriver : MonoBehaviour
    {
        private static readonly object PauseKey = new();
        private static readonly int[] LostNumbers = { 4, 8, 15, 16, 23, 42 };
        private const string TargetDateText = "21.12.2012"; // 10 chars
        // ASCII glyphs available in the timer font (ZegerSYSTEM).
        private static readonly char[] GlyphPool =
            "#@$%&*=+<>/\\^~[]{}|!?:;\"'XVWZHKNMqpdb".ToCharArray();

        [Header("Pyramid timer / calendar")]
        [SerializeField] private PyramidTimerWorldHud pyramidTimerHud;
        [SerializeField] private CalendarSpinView calendarSpin;
        [Tooltip("Pause between Lost numbers 4→23.")]
        [SerializeField] private float lostStepDelay = 0.4f;
        [Tooltip("Hold on 42 before acceleration kicks in.")]
        [SerializeField] private float lostHoldOn42 = 0.3f;
        [Tooltip("Rapid scramble after 42 before the 10-glyph decrypt.")]
        [SerializeField] private float accelerateDuration = 1.6f;
        [Tooltip("How long scrambled glyphs resolve into 21.12.2012.")]
        [SerializeField] private float decryptDuration = 3.2f;
        [SerializeField] private float victorySpinStart = 80f;
        [SerializeField] private float victorySpinEnd = 2200f;

        [Header("Dialogue")]
        [SerializeField] private DialogueDefinition victoryDialogue;

        [Header("Fade")]
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField] private float fadeOutDuration = 2.2f;

        private bool _playing;
        private CanvasGroup _runtimeFade;
        private IGameplayTimePause _timePause;
        private IDialogueService _dialogue;
        private IAudioManager _audio;

        public void Bind(
            IGameplayTimePause timePause,
            IDialogueService dialogue,
            ITimelineService timeline,
            IAudioManager audio)
        {
            _timePause = timePause;
            _dialogue = dialogue;
            _ = timeline;
            _audio = audio;
        }

        public async UniTask PlayAsync(CancellationToken cancellationToken = default)
        {
            if (_playing)
                return;

            _playing = true;
            ResolveRefs();
            _timePause?.Acquire(PauseKey);

            // Crossfade main OST → final scene track for the victory cinematic.
            BeginVictoryMusic();

            try
            {
                await RampPyramidDateAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                // Brief beat on 21.12.2012 before the dialogue box.
                await UniTask.Delay(
                    TimeSpan.FromSeconds(0.75f),
                    DelayType.UnscaledDeltaTime,
                    cancellationToken: cancellationToken);

                // Dialogue has its own pause stacking.
                _timePause?.Release(PauseKey);
                await PlayVictoryDialogueAsync(cancellationToken);
                _timePause?.Acquire(PauseKey);

                cancellationToken.ThrowIfCancellationRequested();
                await FadeToBlackAsync(cancellationToken);
            }
            finally
            {
                calendarSpin?.ClearVictorySpinOverride();
                pyramidTimerHud?.ClearVictoryTextOverride();
                _timePause?.Release(PauseKey);
                _playing = false;
            }
        }

        private void ResolveRefs()
        {
            if (pyramidTimerHud == null)
                pyramidTimerHud = FindFirstObjectByType<PyramidTimerWorldHud>();

            if (calendarSpin == null)
                calendarSpin = FindFirstObjectByType<CalendarSpinView>();
        }

        private async UniTask RampPyramidDateAsync(CancellationToken cancellationToken)
        {
            pyramidTimerHud?.BeginVictoryPresentation();
            calendarSpin?.SetVictorySpinOverride(victorySpinStart);

            // 1) Lost numbers — slow and clear.
            for (var i = 0; i < LostNumbers.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var number = LostNumbers[i];
                var isLast = i == LostNumbers.Length - 1;
                pyramidTimerHud?.SetVictoryText(number.ToString(), punch: true);

                var hold = isLast ? Mathf.Max(0.05f, lostHoldOn42) : Mathf.Max(0.05f, lostStepDelay);
                await UniTask.Delay(
                    TimeSpan.FromSeconds(hold),
                    DelayType.UnscaledDeltaTime,
                    cancellationToken: cancellationToken);
            }

            // 2) On 42 — accelerate into glyph chaos.
            await AccelerateIntoGlyphsAsync(cancellationToken);

            // 3) Ten glyphs decrypt left→right into 21.12.2012.
            await DecryptToDateAsync(cancellationToken);

            pyramidTimerHud?.SetVictoryText(TargetDateText, punch: true);
            calendarSpin?.SetVictorySpinOverride(victorySpinEnd);
        }

        private async UniTask AccelerateIntoGlyphsAsync(CancellationToken cancellationToken)
        {
            var duration = Mathf.Max(0.2f, accelerateDuration);
            var elapsed = 0f;
            var flickerTimer = 0f;

            while (elapsed < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var dt = Time.unscaledDeltaTime;
                elapsed += dt;
                flickerTimer += dt;

                var u = Mathf.Clamp01(elapsed / duration);
                // Slow start → very fast glyph flicker.
                var eased = Mathf.Pow(u, 2.2f);
                calendarSpin?.SetVictorySpinOverride(Mathf.Lerp(victorySpinStart, victorySpinEnd, eased));

                // Flicker rate ramps up hard.
                var interval = Mathf.Lerp(0.14f, 0.025f, eased);
                if (flickerTimer >= interval)
                {
                    flickerTimer = 0f;
                    // Stretch from "42" toward a full 10-glyph block.
                    var len = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(2f, TargetDateText.Length, eased)), 2, TargetDateText.Length);
                    pyramidTimerHud?.SetVictoryText(RandomGlyphString(len), punch: false);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            // Snap to a full 10-symbol block.
            pyramidTimerHud?.SetVictoryText(RandomGlyphString(TargetDateText.Length), punch: true);
        }

        private async UniTask DecryptToDateAsync(CancellationToken cancellationToken)
        {
            var target = TargetDateText;
            var len = target.Length;
            var duration = Mathf.Max(0.5f, decryptDuration);
            var elapsed = 0f;
            var buffer = RandomGlyphString(len).ToCharArray();

            while (elapsed < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();
                elapsed += Time.unscaledDeltaTime;
                var u = Mathf.Clamp01(elapsed / duration);
                // Lock characters left → right (with a little ease so the end snaps clean).
                var lockProgress = Mathf.Pow(u, 0.75f);
                var lockedCount = Mathf.Clamp(Mathf.FloorToInt(lockProgress * len), 0, len);

                for (var i = 0; i < len; i++)
                {
                    if (i < lockedCount)
                        buffer[i] = target[i];
                    else
                        buffer[i] = RandomGlyph();
                }

                pyramidTimerHud?.SetVictoryText(new string(buffer), punch: false);
                calendarSpin?.SetVictorySpinOverride(victorySpinEnd);

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            pyramidTimerHud?.SetVictoryText(target, punch: false);
        }

        private static string RandomGlyphString(int length)
        {
            length = Mathf.Max(1, length);
            var chars = new char[length];
            for (var i = 0; i < length; i++)
                chars[i] = RandomGlyph();
            return new string(chars);
        }

        private static char RandomGlyph() =>
            GlyphPool[UnityEngine.Random.Range(0, GlyphPool.Length)];

        private async UniTask PlayVictoryDialogueAsync(CancellationToken cancellationToken)
        {
            if (victoryDialogue == null || _dialogue == null)
                return;

            var tcs = new UniTaskCompletionSource();
            using var reg = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

            if (!_dialogue.TryPlay(victoryDialogue, () => tcs.TrySetResult()))
            {
                tcs.TrySetResult();
                return;
            }

            await tcs.Task;
        }

        private async UniTask FadeToBlackAsync(CancellationToken cancellationToken)
        {
            var fade = ResolveFade();
            if (fade == null)
                return;

            fade.gameObject.SetActive(true);
            fade.blocksRaycasts = true;
            fade.interactable = false;
            fade.alpha = 0f;

            var duration = Mathf.Max(0.05f, fadeOutDuration);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();
                elapsed += Time.unscaledDeltaTime;
                fade.alpha = Mathf.Clamp01(elapsed / duration);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            fade.alpha = 1f;
        }

        private CanvasGroup ResolveFade()
        {
            if (fadeOverlay != null)
                return fadeOverlay;

            if (_runtimeFade != null)
                return _runtimeFade;

            var go = new GameObject("WinBlackout", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(Image));
            go.transform.SetParent(transform, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 4;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var image = go.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = true;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _runtimeFade = go.GetComponent<CanvasGroup>();
            _runtimeFade.alpha = 0f;
            return _runtimeFade;
        }

        public void PlayVictorySting()
        {
            if (_audio == null)
                return;

            // Final OST should already be playing from BeginVictoryMusic; keep it.
            _audio.StopAmbient();
            if (!_audio.IsPlaying(AudioCatalog.Ids.MusicFinal))
                _audio.Play(AudioCatalog.Ids.MusicFinal);
        }

        private void BeginVictoryMusic()
        {
            if (_audio == null)
                return;

            _audio.StopAmbient();
            _audio.SetMusicPitch(1f);
            _audio.Play(AudioCatalog.Ids.MusicFinal);
        }
    }
}
