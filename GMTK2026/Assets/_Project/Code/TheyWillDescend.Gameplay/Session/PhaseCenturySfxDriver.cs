using System;
using TheyWillDescend.Core.Audio;
using TheyWillDescend.Core.Bus;
using TheyWillDescend.Core.Bus.Events;
using VContainer.Unity;

namespace TheyWillDescend.Gameplay.Session
{
    /// <summary>
    /// Plays Century SFX whenever a new timeline phase starts, except phase 0.
    /// </summary>
    public sealed class PhaseCenturySfxDriver : IStartable, IDisposable
    {
        private readonly IAudioManager _audio;
        private readonly IGameEventBus _bus;
        private IDisposable _phaseStartedSub;

        public PhaseCenturySfxDriver(IAudioManager audio, IGameEventBus bus)
        {
            _audio = audio;
            _bus = bus;
        }

        public void Start()
        {
            _phaseStartedSub = _bus.Subscribe<PhaseStartedEvent>(OnPhaseStarted);
        }

        public void Dispose()
        {
            _phaseStartedSub?.Dispose();
            _phaseStartedSub = null;
        }

        private void OnPhaseStarted(PhaseStartedEvent evt)
        {
            if (evt.PhaseIndex <= 0 || _audio == null)
                return;

            _audio.Play(AudioCatalog.Ids.Century);
        }
    }
}
