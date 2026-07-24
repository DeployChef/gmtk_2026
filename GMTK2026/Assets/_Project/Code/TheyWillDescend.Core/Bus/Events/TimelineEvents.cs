using TheyWillDescend.Core.Economy;

namespace TheyWillDescend.Core.Bus.Events
{
    public readonly struct PhaseStartedEvent
    {
        public readonly int PhaseIndex;
        public readonly string Title;
        public readonly float DurationSeconds;

        public PhaseStartedEvent(int phaseIndex, string title, float durationSeconds)
        {
            PhaseIndex = phaseIndex;
            Title = title;
            DurationSeconds = durationSeconds;
        }
    }

    public readonly struct PhaseCompletedEvent
    {
        public readonly int PhaseIndex;
        public readonly bool OfferWasComplete;

        public PhaseCompletedEvent(int phaseIndex, bool offerWasComplete)
        {
            PhaseIndex = phaseIndex;
            OfferWasComplete = offerWasComplete;
        }
    }

    public readonly struct PhaseFailedEvent
    {
        public readonly int PhaseIndex;

        public PhaseFailedEvent(int phaseIndex)
        {
            PhaseIndex = phaseIndex;
        }
    }

    public readonly struct TimelineYearsChangedEvent
    {
        public readonly float YearsElapsed;

        public TimelineYearsChangedEvent(float yearsElapsed)
        {
            YearsElapsed = yearsElapsed;
        }
    }

    public readonly struct OfferingSubmittedEvent
    {
        public readonly string ResourceId;
        public readonly ResourceDefinition Definition;
        public readonly float SecondsAdded;
        public readonly int PhaseIndex;

        public OfferingSubmittedEvent(
            string resourceId,
            ResourceDefinition definition,
            float secondsAdded,
            int phaseIndex)
        {
            ResourceId = resourceId;
            Definition = definition;
            SecondsAdded = secondsAdded;
            PhaseIndex = phaseIndex;
        }
    }

    public readonly struct OfferingRejectedEvent
    {
        public readonly string ResourceId;
        public readonly float TimerDelta;
        public readonly int PhaseIndex;

        public OfferingRejectedEvent(string resourceId, float timerDelta, int phaseIndex)
        {
            ResourceId = resourceId;
            TimerDelta = timerDelta;
            PhaseIndex = phaseIndex;
        }
    }

    public readonly struct PyramidTimerChangedEvent
    {
        public readonly float RemainingSeconds;
        public readonly float BaselineSeconds;

        public PyramidTimerChangedEvent(float remainingSeconds, float baselineSeconds)
        {
            RemainingSeconds = remainingSeconds;
            BaselineSeconds = baselineSeconds;
        }
    }

    public readonly struct PyramidTimerExpiredEvent
    {
    }
}
