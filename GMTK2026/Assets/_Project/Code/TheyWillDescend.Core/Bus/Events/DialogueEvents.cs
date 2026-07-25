using TheyWillDescend.Core.Dialogue;

namespace TheyWillDescend.Core.Bus.Events
{
    public readonly struct DialogueEndedEvent
    {
        public readonly DialogueDefinition Dialogue;

        public DialogueEndedEvent(DialogueDefinition dialogue)
        {
            Dialogue = dialogue;
        }
    }
}
