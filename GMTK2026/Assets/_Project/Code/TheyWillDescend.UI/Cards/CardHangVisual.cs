using UnityEngine;

namespace TheyWillDescend.UI.Cards
{
    /// <summary>
    /// Rubber-band hang for card art: logic root may snap/move instantly,
    /// while <see cref="visual"/> springs after it and swings on Z like a weight on a string.
    /// Put on the card root; assign the Visual child (or leave empty to auto-find "Visual").
    /// </summary>
    public sealed class CardHangVisual : MonoBehaviour
    {
        [SerializeField] private RectTransform visual;
        [Tooltip("How quickly the art catches the root (smaller = snappier).")]
        [SerializeField] private float followSmoothTime = 0.07f;
        [Tooltip("Max Z tilt in degrees.")]
        [SerializeField] private float maxSwingDegrees = 22f;
        [Tooltip("How hard horizontal lag twists the card.")]
        [SerializeField] private float swingFromLag = 55f;
        [Tooltip("Spring pulling Z rotation back to upright.")]
        [SerializeField] private float swingRestore = 40f;
        [Tooltip("Damps angular velocity.")]
        [SerializeField] private float swingDamping = 8f;

        private Vector3 _followPos;
        private Vector3 _followVel;
        private float _zAngle;
        private float _zVel;
        private bool _initialized;

        private void Awake()
        {
            if (visual == null)
            {
                var child = transform.Find("Visual") as RectTransform;
                visual = child != null ? child : transform as RectTransform;
            }
        }

        private void OnEnable()
        {
            SnapToRoot();
        }

        private void OnDisable()
        {
            SnapToRoot();
        }

        /// <summary>Hard-reset art onto the logic root (e.g. after pool reuse).</summary>
        public void SnapToRoot()
        {
            _followPos = transform.position;
            _followVel = Vector3.zero;
            _zAngle = 0f;
            _zVel = 0f;
            _initialized = true;
            ApplyVisual();
        }

        private void LateUpdate()
        {
            if (visual == null || visual == transform)
                return;

            if (!_initialized)
                SnapToRoot();

            var dt = Time.unscaledDeltaTime;
            if (dt <= 0f)
                return;

            var target = transform.position;
            var lag = target - _followPos;

            _followPos = Vector3.SmoothDamp(
                _followPos,
                target,
                ref _followVel,
                Mathf.Max(0.01f, followSmoothTime),
                Mathf.Infinity,
                dt);

            // Pendulum: lag to the right tips the card clockwise (negative Z in UI often looks natural flipped).
            var swingAccel = (-lag.x * swingFromLag) - (_zAngle * swingRestore);
            _zVel += swingAccel * dt;
            _zVel *= Mathf.Clamp01(1f - swingDamping * dt);
            _zAngle += _zVel * dt;
            _zAngle = Mathf.Clamp(_zAngle, -maxSwingDegrees, maxSwingDegrees);

            ApplyVisual();
        }

        private void ApplyVisual()
        {
            if (visual == null || visual == transform)
                return;

            visual.position = _followPos;
            visual.localRotation = Quaternion.Euler(0f, 0f, _zAngle);
        }
    }
}
