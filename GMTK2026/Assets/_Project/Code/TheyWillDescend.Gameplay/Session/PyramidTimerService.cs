using TheyWillDescend.Core.Bus;
using TheyWillDescend.Core.Bus.Events;
using TheyWillDescend.Core.Timeline;
using UnityEngine;

namespace TheyWillDescend.Gameplay.Session
{
    public sealed class PyramidTimerService : IPyramidTimerService
    {
        private readonly GameTimelineConfig _config;
        private readonly IGameEventBus _bus;

        private float _remaining;
        private bool _expiredPublished;

        public PyramidTimerService(GameTimelineConfig config, IGameEventBus bus)
        {
            _config = config;
            _bus = bus;
            _remaining = config != null ? config.BaselineSeconds : 99f;
        }

        public float RemainingSeconds => _remaining;
        public bool IsExpired => _remaining <= 0f;

        public void ResetToBaseline()
        {
            _expiredPublished = false;
            _remaining = _config != null ? _config.BaselineSeconds : 99f;
            PublishChanged();
        }

        public void AddSeconds(float delta)
        {
            if (Mathf.Approximately(delta, 0f))
                return;

            _remaining = Mathf.Max(0f, _remaining + delta);
            if (_remaining > 0f)
                _expiredPublished = false;

            PublishChanged();
            CheckExpired();
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f || IsExpired)
                return;

            _remaining = Mathf.Max(0f, _remaining - deltaTime);
            PublishChanged();
            CheckExpired();
        }

        private void CheckExpired()
        {
            if (!IsExpired || _expiredPublished)
                return;

            _expiredPublished = true;
            _bus.Publish(new PyramidTimerExpiredEvent());
        }

        private void PublishChanged()
        {
            var baseline = _config != null ? _config.BaselineSeconds : 99f;
            _bus.Publish(new PyramidTimerChangedEvent(_remaining, baseline));
        }
    }
}
