using System;
using TheyWillDescend.Core.Bus;
using TheyWillDescend.Core.Bus.Events;
using TheyWillDescend.Core.Dialogue;
using VContainer.Unity;

namespace TheyWillDescend.UI.Dialogue
{
    /// <summary>
    /// Plays a short era dialogue whenever a timeline phase starts (skips empty catalog slots).
    /// </summary>
    public sealed class PhaseDialogueDriver : IStartable, IDisposable
    {
        private readonly PhaseDialogueCatalog _catalog;
        private readonly IDialogueService _dialogue;
        private readonly IGameEventBus _bus;
        private IDisposable _phaseStartedSub;

        public PhaseDialogueDriver(
            PhaseDialogueCatalog catalog,
            IDialogueService dialogue,
            IGameEventBus bus)
        {
            _catalog = catalog;
            _dialogue = dialogue;
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
            if (_catalog == null || _dialogue == null)
                return;

            if (!_catalog.TryGet(evt.PhaseIndex, out var definition))
                return;

            _dialogue.TryPlay(definition);
        }
    }
}
