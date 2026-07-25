using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TheyWillDescend.UI.Dialogue
{
    /// <summary>
    /// IntroGrass Left/Right: idle chaotic drift, then exit sideways with rotation during camera approach.
    /// Exit continues from the current drifted pose — no snap back to rest.
    /// </summary>
    public sealed class IntroGrassAnimator : MonoBehaviour
    {
        [SerializeField] private RectTransform left;
        [SerializeField] private RectTransform right;

        [Header("Idle drift")]
        [SerializeField] private float driftAmplitude = 18f;
        [SerializeField] private float driftSpeed = 0.275f;
        [SerializeField] private float driftRotation = 4f;

        [Header("Exit")]
        [SerializeField] private float exitDistance = 4200f;
        [SerializeField] private float exitRotationDegrees = 28f;

        private Vector2 _leftRest;
        private Vector2 _rightRest;
        private Quaternion _leftRestRot;
        private Quaternion _rightRestRot;
        private bool _restCaptured;
        private bool _drifting;
        private bool _exiting;
        private float _seed;

        private void Awake()
        {
            CaptureRest();
            _seed = Random.value * 100f;
            _drifting = true;
        }

        public void StartDrift()
        {
            CaptureRest();
            _exiting = false;
            _drifting = true;
            if (left != null)
                left.gameObject.SetActive(true);
            if (right != null)
                right.gameObject.SetActive(true);
        }

        public void SnapHidden()
        {
            _drifting = false;
            _exiting = false;
            ApplyRest();
            if (left != null)
                left.gameObject.SetActive(false);
            if (right != null)
                right.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            _drifting = false;
            _exiting = false;
            ApplyRest();
        }

        public void SnapRestVisible()
        {
            CaptureRest();
            _drifting = false;
            _exiting = false;
            ApplyRest();
            if (left != null)
                left.gameObject.SetActive(true);
            if (right != null)
                right.gameObject.SetActive(true);
        }

        public async UniTask PlayExitAsync(float durationSeconds, CancellationToken cancellationToken = default)
        {
            CaptureRest();

            // Freeze drift and continue from the CURRENT pose (no return-to-rest snap).
            _drifting = false;
            _exiting = true;

            if (left != null)
                left.gameObject.SetActive(true);
            if (right != null)
                right.gameObject.SetActive(true);

            var leftStartPos = left != null ? left.anchoredPosition : _leftRest;
            var rightStartPos = right != null ? right.anchoredPosition : _rightRest;
            var leftStartRot = left != null ? left.localRotation : _leftRestRot;
            var rightStartRot = right != null ? right.localRotation : _rightRestRot;

            var leftEndPos = _leftRest + Vector2.left * exitDistance;
            var rightEndPos = _rightRest + Vector2.right * exitDistance;
            var leftEndRot = _leftRestRot * Quaternion.Euler(0f, 0f, exitRotationDegrees);
            var rightEndRot = _rightRestRot * Quaternion.Euler(0f, 0f, -exitRotationDegrees);

            var duration = Mathf.Max(0.05f, durationSeconds);
            var elapsed = 0f;

            while (elapsed < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();
                elapsed += Time.unscaledDeltaTime;
                var u = Mathf.Clamp01(elapsed / duration);
                // Smooth ease-in-out — continuous velocity from the drifted pose.
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

            _exiting = false;
            SnapHidden();
        }

        private void LateUpdate()
        {
            if (!_drifting || _exiting)
                return;

            CaptureRest();

            if (left != null)
            {
                left.anchoredPosition = _leftRest + DriftOffset(_seed, 1f);
                var z = (Mathf.PerlinNoise(_seed, Time.unscaledTime * driftSpeed) - 0.5f) * 2f * driftRotation;
                left.localRotation = _leftRestRot * Quaternion.Euler(0f, 0f, z);
            }

            if (right != null)
            {
                right.anchoredPosition = _rightRest + DriftOffset(_seed + 11.3f, 1f);
                var z = (Mathf.PerlinNoise(Time.unscaledTime * driftSpeed, _seed + 4f) - 0.5f) * 2f * driftRotation;
                right.localRotation = _rightRestRot * Quaternion.Euler(0f, 0f, z);
            }
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

        private void CaptureRest()
        {
            if (_restCaptured)
                return;

            if (left != null)
            {
                _leftRest = left.anchoredPosition;
                _leftRestRot = left.localRotation;
            }

            if (right != null)
            {
                _rightRest = right.anchoredPosition;
                _rightRestRot = right.localRotation;
            }

            _restCaptured = left != null || right != null;
        }

        private void ApplyRest()
        {
            if (!_restCaptured)
                return;

            if (left != null)
            {
                left.anchoredPosition = _leftRest;
                left.localRotation = _leftRestRot;
            }

            if (right != null)
            {
                right.anchoredPosition = _rightRest;
                right.localRotation = _rightRestRot;
            }
        }
    }
}
