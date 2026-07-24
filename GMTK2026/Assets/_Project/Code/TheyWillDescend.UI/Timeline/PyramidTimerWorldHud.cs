using System;
using TheyWillDescend.Core.Bus;
using TheyWillDescend.Core.Bus.Events;
using TMPro;
using UnityEngine;
using VContainer;

namespace TheyWillDescend.UI.Timeline
{
    /// <summary>
    /// World-space countdown above the pyramid (integer seconds, e.g. 99).
    /// </summary>
    public sealed class PyramidTimerWorldHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text timerLabel;

        private IDisposable _sub;
        private IDisposable _expiredSub;

        [Inject]
        public void Construct(IGameEventBus bus)
        {
            _sub?.Dispose();
            _expiredSub?.Dispose();
            _sub = bus.Subscribe<PyramidTimerChangedEvent>(OnTimerChanged);
            _expiredSub = bus.Subscribe<PyramidTimerExpiredEvent>(_ =>
            {
                if (timerLabel != null)
                    timerLabel.text = "0";
            });
        }

        private void OnDestroy()
        {
            _sub?.Dispose();
            _expiredSub?.Dispose();
        }

        private void OnTimerChanged(PyramidTimerChangedEvent evt)
        {
            if (timerLabel == null)
                return;

            var total = Mathf.Max(0f, evt.RemainingSeconds);
            var display = total <= 0f ? 0 : Mathf.CeilToInt(total);
            timerLabel.text = display.ToString();
        }
    }
}
