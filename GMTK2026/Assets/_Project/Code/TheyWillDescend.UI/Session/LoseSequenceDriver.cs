using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace TheyWillDescend.UI.Session
{
    /// <summary>
    /// Lose cinematic for pyramid timer expire: boom End trigger → wait → hard blackout + shake → lose UI.
    /// </summary>
    public sealed class LoseSequenceDriver : MonoBehaviour
    {
        private static readonly object PauseKey = new();

        private const string EndTrigger = "End";

        [Header("Boom / meteor")]
        [SerializeField] private Animator boomAnimator;
        [Tooltip("How long to wait after End trigger before blackout (meteor flight).")]
        [SerializeField] private float meteorDuration = 3f;

        [Header("Blackout + shake")]
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField] private float shakeDuration = 0.55f;
        [SerializeField] private Vector2 shakeStrength = new(24f, 16f);
        [SerializeField] private float shakeFrequency = 28f;

        private bool _playing;
        private CanvasGroup _runtimeFade;
        private RectTransform _fadeRect;
        private Vector2 _fadeRestAnchored;
        private TheyWillDescend.Core.IGameplayTimePause _timePause;

        public void BindPause(TheyWillDescend.Core.IGameplayTimePause timePause)
        {
            _timePause = timePause;
        }

        public async UniTask PlayAsync(CancellationToken cancellationToken = default)
        {
            if (_playing)
                return;

            _playing = true;

            if (boomAnimator == null)
            {
                var boomGo = GameObject.Find("boom");
                if (boomGo != null)
                    boomAnimator = boomGo.GetComponent<Animator>();
            }

            // Keep gameplay frozen, but meteor uses unscaled animator/time.
            _timePause?.Acquire(PauseKey);

            try
            {
                if (boomAnimator != null)
                {
                    boomAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
                    boomAnimator.gameObject.SetActive(true);
                    BoostBoomVisibility(boomAnimator.gameObject);
                    boomAnimator.ResetTrigger(EndTrigger);
                    boomAnimator.Play("Idle", 0, 0f);
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                    boomAnimator.SetTrigger(EndTrigger);
                    Debug.Log("[LoseSequence] End trigger → waiting meteor flight.");
                }
                else
                {
                    Debug.LogWarning("[LoseSequence] boom Animator missing — skip meteor, blackout only.");
                }

                var wait = Mathf.Max(0.05f, meteorDuration);
                await UniTask.Delay(
                    TimeSpan.FromSeconds(wait),
                    DelayType.UnscaledDeltaTime,
                    cancellationToken: cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                HardBlackout();
                await ShakeFadeAsync(cancellationToken);
            }
            finally
            {
                if (_fadeRect != null)
                    _fadeRect.anchoredPosition = _fadeRestAnchored;

                _timePause?.Release(PauseKey);
                _playing = false;
            }
        }

        private static void BoostBoomVisibility(GameObject boom)
        {
            foreach (var sr in boom.GetComponentsInChildren<SpriteRenderer>(true))
                sr.sortingOrder = Mathf.Max(sr.sortingOrder, 50);
        }

        private void HardBlackout()
        {
            var fade = ResolveFade();
            if (fade == null)
                return;

            fade.gameObject.SetActive(true);
            fade.alpha = 1f;
            fade.blocksRaycasts = true;
            fade.interactable = false;

            _fadeRect = fade.transform as RectTransform;
            if (_fadeRect != null)
                _fadeRestAnchored = _fadeRect.anchoredPosition;
        }

        private async UniTask ShakeFadeAsync(CancellationToken cancellationToken)
        {
            if (_fadeRect == null)
                return;

            var elapsed = 0f;
            var seed = UnityEngine.Random.value * 100f;
            var duration = Mathf.Max(0.05f, shakeDuration);

            while (elapsed < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();
                elapsed += Time.unscaledDeltaTime;
                var u = Mathf.Clamp01(elapsed / duration);
                var rumble = 1f - u;
                var t = elapsed * shakeFrequency;
                var x = (Mathf.PerlinNoise(seed, t) - 0.5f) * 2f * shakeStrength.x * rumble;
                var y = (Mathf.PerlinNoise(t, seed + 3.1f) - 0.5f) * 2f * shakeStrength.y * rumble;
                _fadeRect.anchoredPosition = _fadeRestAnchored + new Vector2(x, y);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            _fadeRect.anchoredPosition = _fadeRestAnchored;
        }

        private CanvasGroup ResolveFade()
        {
            if (fadeOverlay != null)
                return fadeOverlay;

            if (_runtimeFade != null)
                return _runtimeFade;

            var go = new GameObject("LoseBlackout", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(Image));
            go.transform.SetParent(transform, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Below LoseCanvas (sortingOrder 5) so the lose UI appears on top after the cut.
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
    }
}
