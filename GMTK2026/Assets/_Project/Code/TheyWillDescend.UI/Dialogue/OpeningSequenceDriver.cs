using System;
using TheyWillDescend.Core.Bus;
using TheyWillDescend.Core.Bus.Events;
using TheyWillDescend.Core.Dialogue;
using UnityEngine;
using VContainer;

namespace TheyWillDescend.UI.Dialogue
{
    /// <summary>
    /// Runs the opening sequence once per run start.
    /// For now: only the intro dialogue. Add more steps in <see cref="PlayAsync"/> later.
    /// </summary>
    public sealed class OpeningSequenceDriver : MonoBehaviour
    {
        [SerializeField] private DialogueDefinition introDialogue;

        private IDialogueService _dialogue;
        private IDisposable _runStartedSub;
        private bool _playedThisRun;

        [Inject]
        public void Construct(IDialogueService dialogue, IGameEventBus bus)
        {
            _dialogue = dialogue;

            _runStartedSub?.Dispose();
            _runStartedSub = bus.Subscribe<RunStartedEvent>(_ => OnRunStarted());
        }

        private void OnDestroy()
        {
            _runStartedSub?.Dispose();
        }

        private void OnRunStarted()
        {
            _playedThisRun = false;
            Play();
        }

        private void Play()
        {
            if (_playedThisRun)
                return;

            if (introDialogue == null)
            {
                Debug.LogWarning("[OpeningSequenceDriver] Assign Intro Dialogue in Inspector.");
                return;
            }

            if (_dialogue == null)
                return;

            // Sequence step 1 (only for now): intro dialogue.
            // Later: chain more steps in onComplete / UniTask.
            if (_dialogue.TryPlay(introDialogue))
                _playedThisRun = true;
        }
    }
}
