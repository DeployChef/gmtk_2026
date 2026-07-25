using System;
using TheyWillDescend.Core.Audio;
using TheyWillDescend.Core.Bus;
using TheyWillDescend.Core.Bus.Events;
using VContainer.Unity;

namespace TheyWillDescend.Gameplay.Session
{
    /// <summary>
    /// Drives main music pitch from pyramid timer remaining seconds.
    /// Hardcoded 10s bands; max +150% (pitch 2.5) at the lowest band. Instant on band change.
    /// </summary>
    public sealed class PyramidTimerMusicDriver : IStartable, IDisposable
    {
        private readonly IAudioManager _audio;
        private readonly IGameEventBus _bus;
        private IDisposable _timerSub;

        public PyramidTimerMusicDriver(IAudioManager audio, IGameEventBus bus)
        {
            _audio = audio;
            _bus = bus;
        }

        public void Start()
        {
            _timerSub = _bus.Subscribe<PyramidTimerChangedEvent>(OnTimerChanged);
        }

        public void Dispose()
        {
            _timerSub?.Dispose();
            _timerSub = null;
        }

        private void OnTimerChanged(PyramidTimerChangedEvent evt)
        {
            if (_audio == null || !_audio.HasMusicClip || _audio.IsMusicPaused)
                return;

            _audio.SetMusicPitch(PitchForRemaining(evt.RemainingSeconds));
        }

        /// <summary>
        /// remaining high → calm (1.0); remaining low → urgent (2.5 = +150%).
        /// Bands of 10 seconds.
        /// </summary>
        internal static float PitchForRemaining(float remainingSeconds)
        {
            if (remainingSeconds >= 90f) return 1.00f;
            if (remainingSeconds >= 80f) return 1.15f;
            if (remainingSeconds >= 70f) return 1.30f;
            if (remainingSeconds >= 60f) return 1.45f;
            if (remainingSeconds >= 50f) return 1.60f;
            if (remainingSeconds >= 40f) return 1.75f;
            if (remainingSeconds >= 30f) return 1.90f;
            if (remainingSeconds >= 20f) return 2.05f;
            if (remainingSeconds >= 10f) return 2.20f;
            return 2.50f;
        }
    }
}
