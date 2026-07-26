using System;
using TheyWillDescend.Core.Audio;
using TheyWillDescend.Core.Bus;
using TheyWillDescend.Core.Bus.Events;
using TheyWillDescend.Core.Session;
using TheyWillDescend.Core.Timeline;
using UnityEngine;
using VContainer.Unity;

namespace TheyWillDescend.Gameplay.Session
{
    /// <summary>
    /// Win: final-phase offer completed (last required card). Lose: pyramid timer expired.
    /// </summary>
    public sealed class GameResultService : IGameResultService, IStartable, IDisposable
    {
        private readonly IGameEventBus _bus;
        private readonly ITimelineService _timeline;
        private readonly IAudioManager _audio;

        private IDisposable _runStartedSub;
        private IDisposable _offeringSub;
        private IDisposable _phaseCompletedSub;
        private IDisposable _timerExpiredSub;

        private bool _hasResult;
        private bool _isVictory;

        public GameResultService(IGameEventBus bus, ITimelineService timeline, IAudioManager audio)
        {
            _bus = bus;
            _timeline = timeline;
            _audio = audio;
        }

        public bool HasResult => _hasResult;
        public bool IsVictory => _hasResult && _isVictory;

        public void Start()
        {
            _runStartedSub = _bus.Subscribe<RunStartedEvent>(_ => Clear());
            _offeringSub = _bus.Subscribe<OfferingSubmittedEvent>(OnOfferingSubmitted);
            _phaseCompletedSub = _bus.Subscribe<PhaseCompletedEvent>(OnPhaseCompleted);
            _timerExpiredSub = _bus.Subscribe<PyramidTimerExpiredEvent>(OnPyramidTimerExpired);
        }

        private void OnPyramidTimerExpired(PyramidTimerExpiredEvent _)
        {
            if (_hasResult)
                return;

            DeclareLose(GameResultCause.PyramidTimerExpired);
        }

        public void DeclareWin(GameResultCause cause)
        {
            if (_hasResult)
                return;

            _hasResult = true;
            _isVictory = true;
            _timeline.StopRun();
            // Victory sting is played by WinSequence / ResultScreen after the cinematic.
            Debug.Log($"[GameResultService] WIN ({cause}).");
            _bus.Publish(new GameWonEvent(cause));
        }

        public void DeclareLose(GameResultCause cause)
        {
            if (_hasResult)
                return;

            _hasResult = true;
            _isVictory = false;
            _timeline.StopRun();
            PlayResultSting(AudioCatalog.Ids.Defeat);
            Debug.Log($"[GameResultService] LOSE ({cause}).");
            _bus.Publish(new GameLostEvent(cause));
        }

        private void PlayResultSting(string soundId)
        {
            if (_audio == null)
                return;

            _audio.StopMusic();
            _audio.StopAmbient();
            _audio.Play(soundId);
        }

        public void Clear()
        {
            _hasResult = false;
            _isVictory = false;
        }

        public void Dispose()
        {
            _runStartedSub?.Dispose();
            _offeringSub?.Dispose();
            _phaseCompletedSub?.Dispose();
            _timerExpiredSub?.Dispose();
            _runStartedSub = null;
            _offeringSub = null;
            _phaseCompletedSub = null;
            _timerExpiredSub = null;
        }

        private void OnOfferingSubmitted(OfferingSubmittedEvent e)
        {
            if (_hasResult)
                return;

            if (_timeline.PhaseCount <= 0 || e.PhaseIndex != _timeline.PhaseCount - 1)
                return;

            if (!_timeline.IsCurrentOfferComplete)
                return;

            DeclareWin(GameResultCause.FinalOfferCompleted);
        }

        private void OnPhaseCompleted(PhaseCompletedEvent e)
        {
            // Fallback if the final phase somehow ends with a complete offer
            // without going through the last-card path (e.g. future cheats).
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
