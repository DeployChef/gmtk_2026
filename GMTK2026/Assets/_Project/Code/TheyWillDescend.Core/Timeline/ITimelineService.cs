namespace TheyWillDescend.Core.Timeline
{
    /// <summary>
    /// Party clock + phase offers. Dropping a valid offer card on the pyramid goes through <see cref="TryOffer"/>.
    /// </summary>
    public interface ITimelineService
    {
        bool IsRunning { get; }
        int PhaseCount { get; }
        int CurrentPhaseIndex { get; }
        PhaseDefinition CurrentPhase { get; }
        PhaseDefinition GetPhase(int index);
        float CurrentPhaseElapsedSeconds { get; }
        float CurrentPhaseNormalizedProgress { get; }
        float TotalElapsedSeconds { get; }
        float YearsElapsed { get; }
        bool IsCurrentOfferComplete { get; }
        int GetOfferDelivered(int requirementIndex);
        int GetOfferRequired(int requirementIndex);

        void StartRun();

        /// <summary>Stop phase/year progression (win/lose). Does not reset state.</summary>
        void StopRun();

        void Tick(float deltaTime);

        /// <summary>
        /// Attempt to submit a card to the current phase offer.
        /// Returns true if the card should be consumed. Wrong drops apply timer delta and return false.
        /// </summary>
        bool TryOffer(string resourceId);

        /// <summary>Debug/GD only: jump to phase and restart its segment.</summary>
        void DebugJumpToPhase(int phaseIndex);
    }
}
