using System;
using DG.Tweening;
using TheyWillDescend.Core.Bus;
using TheyWillDescend.Core.Bus.Events;
using UnityEngine;
using VContainer;

namespace TheyWillDescend.UI.Timeline
{
    /// <summary>
    /// Calendar disc: on each era start, bursts up then eases down to a steady cruise spin
    /// in the same direction (never reverses).
    /// </summary>
    public sealed class CalendarSpinView : MonoBehaviour
    {
        [SerializeField] private RectTransform target;
        [Tooltip("Steady spin after the era burst settles (deg/sec). Always > 0.")]
        [SerializeField] private float cruiseSpeed = 40f;
        [Tooltip("Peak angular speed at the start of an era burst (deg/sec).")]
        [SerializeField] private float burstSpeed = 420f;
        [Tooltip("How long to ramp from current speed up to burst.")]
        [SerializeField] private float spinUpDuration = 0.2f;
        [Tooltip("How long the burst decelerates down to cruise.")]
        [SerializeField] private float settleDuration = 2.5f;
        [SerializeField] private Ease spinUpEase = Ease.OutQuad;
        [SerializeField] private Ease settleEase = Ease.OutCubic;
        [Tooltip("Positive = clockwise (negative Z for UI).")]
        [SerializeField] private bool clockwise = true;

        private IDisposable _phaseStartedSub;
        private Sequence _speedSequence;
        private float _angularSpeed;

        [Inject]
        public void Construct(IGameEventBus bus)
        {
            _phaseStartedSub?.Dispose();
            _phaseStartedSub = bus.Subscribe<PhaseStartedEvent>(_ => OnEraStarted());
            OnEraStarted();
        }

        private void Awake()
        {
            if (target == null)
                target = transform as RectTransform;

            // Kill any leftover DORotate tweens fighting this spin.
            if (target != null)
                DOTween.Kill(target, complete: false);
        }

        private void Update()
        {
            if (target == null)
                return;

            // Speed is always non-negative; direction comes only from clockwise.
            var speed = Mathf.Max(0f, _angularSpeed);
            if (speed <= 0.0001f)
                return;

            var delta = speed * Time.unscaledDeltaTime;
            if (clockwise)
                delta = -delta;

            target.Rotate(0f, 0f, delta, Space.Self);
        }

        private void OnDestroy()
        {
            _speedSequence?.Kill();
            _phaseStartedSub?.Dispose();
        }

        private void OnEraStarted()
        {
            if (target != null)
                DOTween.Kill(target, complete: false);

            _speedSequence?.Kill();

            var peak = Mathf.Max(0.01f, Mathf.Max(burstSpeed, cruiseSpeed));
            var cruise = Mathf.Max(0.01f, cruiseSpeed);
            // Never dip to 0 mid-burst — keep spinning the same way.
            _angularSpeed = Mathf.Max(_angularSpeed, cruise);

            _speedSequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            _speedSequence.Append(
                DOTween.To(() => _angularSpeed, v => _angularSpeed = v, peak, Mathf.Max(0.01f, spinUpDuration))
                    .SetEase(spinUpEase));
            _speedSequence.Append(
                DOTween.To(() => _angularSpeed, v => _angularSpeed = v, cruise, Mathf.Max(0.01f, settleDuration))
                    .SetEase(settleEase));
        }
    }
}
