using System;
using DG.Tweening;
using TheyWillDescend.Core.Bus;
using TheyWillDescend.Core.Bus.Events;
using TMPro;
using UnityEngine;
using VContainer;

namespace TheyWillDescend.UI.Timeline
{
    /// <summary>
    /// World-space pyramid countdown with sine-wave glyph motion and punch on tick.
    /// </summary>
    public sealed class PyramidTimerWorldHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text timerLabel;

        [Header("Sine wave")]
        [SerializeField] private float waveAmplitude = 6f;
        [SerializeField] private float waveFrequency = 2.2f;
        [SerializeField] private float waveCharPhase = 0.55f;
        [SerializeField] private float lowTimeWaveMul = 1.65f;

        [Header("Tick punch")]
        [SerializeField] private float punchScale = 0.28f;
        [SerializeField] private float punchDuration = 0.28f;
        [SerializeField] private int punchVibrato = 8;
        [SerializeField] private Color tickFlashColor = new(1f, 0.85f, 0.35f, 1f);
        [SerializeField] private Color urgentFlashColor = new(1f, 0.35f, 0.25f, 1f);
        [SerializeField] private float flashDuration = 0.22f;
        [SerializeField] private float urgentBelowSeconds = 30f;

        [Header("Victory")]
        [SerializeField] private float victoryScaleMul = 2f;
        [SerializeField] private float victoryScaleDuration = 0.55f;

        private IDisposable _sub;
        private IDisposable _expiredSub;
        private int _displayed = -1;
        private Color _baseColor = Color.white;
        private Vector3 _baseScale = Vector3.one;
        private Vector3 _victoryRestScale = Vector3.one;
        private Tween _punchTween;
        private Tween _colorTween;
        private Tween _scaleTween;
        private bool _baseCaptured;
        private bool _victoryOverride;

        [Inject]
        public void Construct(IGameEventBus bus)
        {
            _sub?.Dispose();
            _expiredSub?.Dispose();
            _sub = bus.Subscribe<PyramidTimerChangedEvent>(OnTimerChanged);
            _expiredSub = bus.Subscribe<PyramidTimerExpiredEvent>(_ =>
            {
                if (_victoryOverride)
                    return;
                SetDisplay(0, forceAnim: true);
            });
            CaptureBase();
        }

        private void Awake() => CaptureBase();

        private void OnDestroy()
        {
            _punchTween?.Kill();
            _colorTween?.Kill();
            _scaleTween?.Kill();
            _sub?.Dispose();
            _expiredSub?.Dispose();
        }

        private void LateUpdate()
        {
            AnimateGlyphWave();
        }

        private void CaptureBase()
        {
            if (timerLabel == null || _baseCaptured)
                return;

            _baseColor = timerLabel.color;
            _baseScale = timerLabel.transform.localScale;
            if (_baseScale.sqrMagnitude < 0.0001f)
                _baseScale = Vector3.one;
            _baseCaptured = true;
        }

        private void OnTimerChanged(PyramidTimerChangedEvent evt)
        {
            if (_victoryOverride)
                return;

            var total = Mathf.Max(0f, evt.RemainingSeconds);
            var display = total <= 0f ? 0 : Mathf.CeilToInt(total);
            SetDisplay(display, forceAnim: false);
        }

        /// <summary>Win cinematic drives the label (e.g. date ramp to 21.12.2012).</summary>
        public void SetVictoryText(string text, bool punch = false)
        {
            if (timerLabel == null)
                return;

            _victoryOverride = true;
            CaptureBase();
            timerLabel.text = text ?? string.Empty;
            timerLabel.ForceMeshUpdate();

            if (punch)
                PlayTickFeedback(Mathf.Max(0, _displayed));
        }

        /// <summary>Grows the red timer to victory scale (default 2x).</summary>
        public void BeginVictoryPresentation()
        {
            if (timerLabel == null)
                return;

            _victoryOverride = true;
            CaptureBase();
            _punchTween?.Kill();
            _scaleTween?.Kill();
            _victoryRestScale = _baseScale * Mathf.Max(1f, victoryScaleMul);
            timerLabel.transform.localScale = _baseScale;
            _scaleTween = timerLabel.transform
                .DOScale(_victoryRestScale, Mathf.Max(0.05f, victoryScaleDuration))
                .SetEase(Ease.OutBack)
                .SetUpdate(true)
                .SetTarget(timerLabel.transform);
        }

        public void ClearVictoryTextOverride()
        {
            _victoryOverride = false;
            _punchTween?.Kill();
            _scaleTween?.Kill();
            if (timerLabel != null)
                timerLabel.transform.localScale = _baseScale;
        }

        private void SetDisplay(int value, bool forceAnim)
        {
            if (timerLabel == null)
                return;

            CaptureBase();

            var changed = value != _displayed;
            _displayed = value;
            timerLabel.text = value.ToString();
            timerLabel.ForceMeshUpdate();

            if (changed || forceAnim)
                PlayTickFeedback(value);
        }

        private void PlayTickFeedback(int value)
        {
            var urgent = value > 0 && value <= urgentBelowSeconds;
            var flash = urgent ? urgentFlashColor : tickFlashColor;
            var scaleMul = urgent ? punchScale * 1.35f : punchScale;
            var restScale = _victoryOverride ? _victoryRestScale : _baseScale;

            _punchTween?.Kill();
            timerLabel.transform.localScale = restScale;
            _punchTween = timerLabel.transform
                .DOPunchScale(Vector3.one * scaleMul, punchDuration, punchVibrato, elasticity: 0.65f)
                .SetUpdate(true)
                .SetTarget(timerLabel.transform);

            _colorTween?.Kill();
            timerLabel.color = flash;
            _colorTween = DOTween
                .To(() => timerLabel.color, c => timerLabel.color = c, _baseColor, flashDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetTarget(timerLabel);
        }

        private void AnimateGlyphWave()
        {
            if (timerLabel == null || !timerLabel.gameObject.activeInHierarchy)
                return;

            timerLabel.ForceMeshUpdate();
            var textInfo = timerLabel.textInfo;
            if (textInfo == null || textInfo.characterCount == 0)
                return;

            var urgent = _displayed > 0 && _displayed <= urgentBelowSeconds;
            var amp = waveAmplitude * (urgent ? lowTimeWaveMul : 1f);
            var t = Time.unscaledTime * waveFrequency * Mathf.PI * 2f;

            for (var i = 0; i < textInfo.characterCount; i++)
            {
                var charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible)
                    continue;

                var matIndex = charInfo.materialReferenceIndex;
                var vertIndex = charInfo.vertexIndex;
                var verts = textInfo.meshInfo[matIndex].vertices;

                var wave = Mathf.Sin(t + i * waveCharPhase) * amp;
                var wobbleX = Mathf.Cos(t * 0.85f + i * waveCharPhase * 1.3f) * amp * 0.2f;
                var offset = new Vector3(wobbleX, wave, 0f);

                verts[vertIndex + 0] += offset;
                verts[vertIndex + 1] += offset;
                verts[vertIndex + 2] += offset;
                verts[vertIndex + 3] += offset;
            }

            for (var i = 0; i < textInfo.meshInfo.Length; i++)
            {
                var meshInfo = textInfo.meshInfo[i];
                if (meshInfo.mesh == null)
                    continue;
                meshInfo.mesh.vertices = meshInfo.vertices;
                timerLabel.UpdateGeometry(meshInfo.mesh, i);
            }
        }
    }
}
