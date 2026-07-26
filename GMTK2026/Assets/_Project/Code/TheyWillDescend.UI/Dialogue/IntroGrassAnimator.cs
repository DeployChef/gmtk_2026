using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TheyWillDescend.UI.Dialogue
{
    /// <summary>
    /// Intro grass: idle drift, then exit sideways. Runtime-only — no pose baking.
    /// Edit-mode placement is restored by Unity when you leave Play Mode (do not save during Play).
    /// </summary>
    public sealed class IntroGrassAnimator : MonoBehaviour
    {
        /// <summary>Fired when the grass exit animation begins.</summary>
        public event Action OnExitStarted;

        [SerializeField] private RectTransform left;
        [SerializeField] private RectTransform right;

        [Header("Idle drift")]
        [SerializeField] private float driftAmplitude = 18f;
        [SerializeField] private float driftSpeed = 0.275f;
        [SerializeField] private float driftRotation = 4f;

        [Header("Exit")]
        [SerializeField] private float exitDistance = 4200f;
        [SerializeField] private float exitRotationDegrees = 28f;

        private Vector2 _leftHome;
        private Vector2 _rightHome;
        private Quaternion _leftHomeRot;
        private Quaternion _rightHomeRot;
        private bool _homeReady;
        private bool _drifting;
        private bool _exiting;
        private float _seed;

        private void Awake()
        {
            _seed = UnityEngine.Random.value * 100f;
            RememberHome();
        }

        public void StartDrift()
        {
            RememberHome();
            _exiting = false;
            _drifting = true;
            Show(true);
        }

        public void SnapHidden()
        {
            _drifting = false;
            _exiting = false;
            ApplyHome();
            Show(false);
        }

        public void SnapRestVisible()
        {
            _drifting = false;
            _exiting = false;
            ApplyHome();
            Show(true);
        }

        public async UniTask PlayExitAsync(float durationSeconds, CancellationToken cancellationToken = default)
        {
            RememberHome();
            _drifting = false;
            _exiting = true;
            Show(true);
            OnExitStarted?.Invoke();

            var leftStartPos = left != null ? left.anchoredPosition : _leftHome;
            var rightStartPos = right != null ? right.anchoredPosition : _rightHome;
            var leftStartRot = left != null ? left.localRotation : _leftHomeRot;
            var rightStartRot = right != null ? right.localRotation : _rightHomeRot;

            var leftEndPos = _leftHome + Vector2.left * exitDistance;
            var rightEndPos = _rightHome + Vector2.right * exitDistance;
            var leftEndRot = _leftHomeRot * Quaternion.Euler(0f, 0f, exitRotationDegrees);
            var rightEndRot = _rightHomeRot * Quaternion.Euler(0f, 0f, -exitRotationDegrees);

            var duration = Mathf.Max(0.05f, durationSeconds);
            var elapsed = 0f;

            try
            {
                while (elapsed < duration)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    elapsed += Time.unscaledDeltaTime;
                    var u = Mathf.Clamp01(elapsed / duration);
                    var eased = SmoothStep(SmoothStep(u));

                    if (left != null)
                    {
                        left.anchoredPosition = Vector2.LerpUnclamped(leftStartPos, leftEndPos, eased);
                        left.localRotation = Quaternion.Slerp(leftStartRot, leftEndRot, eased);
                    }

                    if (right != null)
                    {
                        right.anchoredPosition = Vector2.LerpUnclamped(rightStartPos, rightEndPos, eased);
                        right.localRotation = Quaternion.Slerp(rightStartRot, rightEndRot, eased);
                    }

                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
            }
            finally
            {
                _exiting = false;
                SnapHidden();
            }
        }

        private void LateUpdate()
        {
            if (!_drifting || _exiting || !_homeReady)
                return;

            if (left != null)
            {
                left.anchoredPosition = _leftHome + DriftOffset(_seed, 1f);
                var z = (Mathf.PerlinNoise(_seed, Time.unscaledTime * driftSpeed) - 0.5f) * 2f * driftRotation;
                left.localRotation = _leftHomeRot * Quaternion.Euler(0f, 0f, z);
            }

            if (right != null)
            {
                right.anchoredPosition = _rightHome + DriftOffset(_seed + 11.3f, 1f);
                var z = (Mathf.PerlinNoise(Time.unscaledTime * driftSpeed, _seed + 4f) - 0.5f) * 2f * driftRotation;
                right.localRotation = _rightHomeRot * Quaternion.Euler(0f, 0f, z);
            }
        }

        private void RememberHome()
        {
            if (_homeReady)
                return;

            if (left != null)
            {
                _leftHome = left.anchoredPosition;
                _leftHomeRot = left.localRotation;
            }

            if (right != null)
            {
                _rightHome = right.anchoredPosition;
                _rightHomeRot = right.localRotation;
            }

            _homeReady = left != null || right != null;
        }

        private void ApplyHome()
        {
            if (!_homeReady)
                return;

            if (left != null)
            {
                left.anchoredPosition = _leftHome;
                left.localRotation = _leftHomeRot;
            }

            if (right != null)
            {
                right.anchoredPosition = _rightHome;
                right.localRotation = _rightHomeRot;
            }
        }

        private void Show(bool visible)
        {
            if (left != null)
                left.gameObject.SetActive(visible);
            if (right != null)
                right.gameObject.SetActive(visible);
        }

        private Vector2 DriftOffset(float seed, float ampMul)
        {
            var t = Time.unscaledTime * driftSpeed;
            var x = (Mathf.PerlinNoise(seed, t) - 0.5f) * 2f * driftAmplitude * ampMul;
            var y = (Mathf.PerlinNoise(t, seed + 7.7f) - 0.5f) * 2f * driftAmplitude * ampMul;
            return new Vector2(x, y);
        }

        private static float SmoothStep(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
