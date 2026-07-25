using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TheyWillDescend.UI.Dialogue
{
    /// <summary>
    /// Bottom bar starts off-screen and rises to a resting Y with a heavy slab grind/shake.
    /// Put on <c>BottomBar</c> (or assign its RectTransform).
    /// </summary>
    public sealed class BottomBarSlabReveal : MonoBehaviour
    {
        [SerializeField] private RectTransform bar;
        [SerializeField] private float targetY = 171f;
        [SerializeField] private float hiddenY = -280f;
        [SerializeField] private float duration = 1.35f;
        [SerializeField] private float overshoot = 14f;
        [SerializeField] private float settleDuration = 0.32f;
        [SerializeField] private Vector2 shakeStrength = new(16f, 6f);
        [SerializeField] private float shakeFrequency = 22f;

        private bool _revealed;
        private float _baseX;

        private void Awake()
        {
            if (bar == null)
                bar = transform as RectTransform;

            if (bar != null)
                _baseX = bar.anchoredPosition.x;

            SnapHidden();
        }

        public void SnapHidden()
        {
            _revealed = false;
            if (bar == null)
                return;

            bar.anchoredPosition = new Vector2(_baseX, hiddenY);
        }

        public void SnapRevealed()
        {
            _revealed = true;
            if (bar == null)
                return;

            bar.anchoredPosition = new Vector2(_baseX, targetY);
        }

        private void OnDisable()
        {
            // Avoid baking hiddenY into the scene when leaving Play Mode.
            if (bar != null)
                bar.anchoredPosition = new Vector2(_baseX, targetY);
            _revealed = false;
        }

        public async UniTask RevealAsync(CancellationToken cancellationToken = default)
        {
            if (_revealed || bar == null)
                return;

            _revealed = true;
            _baseX = bar.anchoredPosition.x;
            bar.anchoredPosition = new Vector2(_baseX, hiddenY);

            var peakY = targetY + Mathf.Max(0f, overshoot);

            await RiseWithShakeAsync(hiddenY, peakY, Mathf.Max(0.05f, duration), heavy: true, cancellationToken);

            if (overshoot > 0.01f && settleDuration > 0.01f)
                await RiseWithShakeAsync(peakY, targetY, settleDuration, heavy: false, cancellationToken);

            bar.anchoredPosition = new Vector2(_baseX, targetY);
        }

        private async UniTask RiseWithShakeAsync(
            float fromY,
            float toY,
            float seconds,
            bool heavy,
            CancellationToken cancellationToken)
        {
            var elapsed = 0f;
            var seed = Random.value * 100f;

            while (elapsed < seconds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                elapsed += Time.unscaledDeltaTime;
                var u = Mathf.Clamp01(elapsed / seconds);

                // Heavy plate: slow scrape up, then a firmer finish. Settle uses a softer ease.
                var eased = heavy
                    ? SmoothStep(SmoothStep(u))
                    : 1f - (1f - u) * (1f - u);

                var y = Mathf.LerpUnclamped(fromY, toY, eased);

                // Rumble fades as the slab seats; stronger horizontal grind.
                var rumble = (heavy ? 1f - u * 0.55f : (1f - u) * 0.45f);
                var t = elapsed * shakeFrequency;
                var shakeX = (Mathf.PerlinNoise(seed, t) - 0.5f) * 2f * shakeStrength.x * rumble;
                var shakeY = (Mathf.PerlinNoise(t, seed) - 0.5f) * 2f * shakeStrength.y * rumble;

                // Occasional hard ticks — stone catching on the frame.
                if (heavy && u > 0.15f && u < 0.9f)
                {
                    var tick = Mathf.Sin(t * 1.7f);
                    if (tick > 0.92f)
                        shakeX += Mathf.Sign(shakeX + 0.001f) * shakeStrength.x * 0.35f * rumble;
                }

                bar.anchoredPosition = new Vector2(_baseX + shakeX, y + shakeY);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        private static float SmoothStep(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
