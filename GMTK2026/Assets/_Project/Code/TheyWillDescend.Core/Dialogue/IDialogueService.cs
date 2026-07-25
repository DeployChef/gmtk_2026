using System;

namespace TheyWillDescend.Core.Dialogue
{
    public interface IDialogueService
    {
        bool IsPlaying { get; }

        /// <summary>
        /// Starts a dialogue sequence. Returns false if already playing or dialogue is empty.
        /// </summary>
        bool TryPlay(DialogueDefinition dialogue, Action onComplete = null);
    }
}
