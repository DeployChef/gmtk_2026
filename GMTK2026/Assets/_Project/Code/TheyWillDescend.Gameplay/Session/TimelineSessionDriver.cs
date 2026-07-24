using System;
using TheyWillDescend.Core.Bus;
using TheyWillDescend.Core.Bus.Events;
using TheyWillDescend.Core.Hazards;
using TheyWillDescend.Core.Session;
using TheyWillDescend.Core.Timeline;
using VContainer.Unity;

namespace TheyWillDescend.Gameplay.Session
{
    /// <summary>
    /// Ticks timeline + pyramid timer; routes phase fail → disaster strike.
    /// StartRun is triggered from <c>GameStartState</c> after inventory seed.
    /// </summary>
    public sealed class TimelineSessionDriver : ITickable, IStartable, IDisposable
    {
        private readonly ITimelineService _timeline;
        private readonly IPyramidTimerService _pyramidTimer;
        private readonly IGameResultService _gameResult;
        private readonly IGameEventBus _bus;
        private readonly IDisasterManager _disasters;
        private IDisposable _phaseFailedSub;

        public TimelineSessionDriver(
            ITimelineService timeline,
            IPyramidTimerService pyramidTimer,
            IGameResultService gameResult,
            IGameEventBus bus,
            IDisasterManager disasters)
        {
            _timeline = timeline;
            _pyramidTimer = pyramidTimer;
            _gameResult = gameResult;
            _bus = bus;
            _disasters = disasters;
        }

        public void Start()
        {
            _phaseFailedSub = _bus.Subscribe<PhaseFailedEvent>(_ =>
            {
                _disasters?.TryStrikeRandomHouse();
            });
        }

        public void Tick()
        {
            if (_gameResult.HasResult)
                return;

            var dt = UnityEngine.Time.deltaTime;
            _timeline.Tick(dt);
            _pyramidTimer.Tick(dt);
        }

        public void Dispose()
        {
            _phaseFailedSub?.Dispose();
            _phaseFailedSub = null;
        }
    }
}
