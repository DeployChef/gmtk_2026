using TheyWillDescend.Core.Session;

namespace TheyWillDescend.Core.Bus.Events
{
    /// <summary>Published when a run starts or is debug-jumped — clears prior win/lose.</summary>
    public readonly struct RunStartedEvent
    {
    }

    public readonly struct GameWonEvent
    {
        public readonly GameResultCause Cause;

        public GameWonEvent(GameResultCause cause)
        {
            Cause = cause;
        }
    }

    public readonly struct GameLostEvent
    {
        public readonly GameResultCause Cause;

        public GameLostEvent(GameResultCause cause)
        {
            Cause = cause;
        }
    }
}
