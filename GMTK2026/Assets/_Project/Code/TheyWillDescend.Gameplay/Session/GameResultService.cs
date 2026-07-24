using System;
using TheyWillDescend.Core.Bus;
using TheyWillDescend.Core.Bus.Events;
using TheyWillDescend.Core.Session;
using TheyWillDescend.Core.Timeline;
using UnityEngine;
using VContainer.Unity;

namespace TheyWillDescend.Gameplay.Session
{
    /// <summary>
    /// Win: last phase completed with offer done. Lose: pyramid timer expired.
    /// Stub — events + logs only; UI later.
    /// </summary>
    public sealed class GameResultService : IGameResultService, IStartable, IDisposable
    {
        private readonly IGameEventBus _bus;
        private readonly ITimelineService _timeline;

        private IDisposable _runStartedSub;
        private IDisposable _phaseCompletedSub;
        private IDisposable _timerExpiredSub;

        private bool _hasResult;
        private bool _isVictory;

        public GameResultService(IGameEventBus bus, ITimelineService timeline)
        {
            _bus = bus;
            _timeline = timeline;
        }

        public bool HasResult => _hasResult;
        public bool IsVictory => _hasResult && _isVictory;

        public void Start()
        {
            _runStartedSub = _bus.Subscribe<RunStartedEvent>(_ => Clear());
            _phaseCompletedSub = _bus.Subscribe<PhaseCompletedEvent>(OnPhaseCompleted);
            _timerExpiredSub = _bus.Subscribe<PyramidTimerExpiredEvent>(_ =>
                DeclareLose(GameResultCause.PyramidTimerExpired));
        }

        public void DeclareWin(GameResultCause cause)
        {
            if (_hasResult)
                return;

            _hasResult = true;
            _isVictory = true;
            _timeline.StopRun();
            Debug.Log($"[GameResultService] WIN ({cause}) — stub.");
            _bus.Publish(new GameWonEvent(cause));
        }

        public void DeclareLose(GameResultCause cause)
        {
            if (_hasResult)
                return;

            _hasResult = true;
            _isVictory = false;
            _timeline.StopRun();
            Debug.Log($"[GameResultService] LOSE ({cause}) — stub.");
            _bus.Publish(new GameLostEvent(cause));
        }

        public void Clear()
        {
            _hasResult = false;
            _isVictory = false;
        }

        public void Dispose()
        {
            _runStartedSub?.Dispose();
            _phaseCompletedSub?.Dispose();
            _timerExpiredSub?.Dispose();
            _runStartedSub = null;
            _phaseCompletedSub = null;
            _timerExpiredSub = null;
        }

        private void OnPhaseCompleted(PhaseCompletedEvent e)
        {
            if (_hasResult)
                return;

            if (!e.OfferWasComplete)
                return;

            if (_timeline.PhaseCount <= 0 || e.PhaseIndex != _timeline.PhaseCount - 1)
                return;

            DeclareWin(GameResultCause.AllPhasesCompleted);
        }
    }
}
