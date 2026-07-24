namespace TheyWillDescend.Core.Timeline
{
    /// <summary>
    /// Applies phase start inventory/buildings. Used on run start (phase 0) and debug jump — not on normal phase advance.
    /// </summary>
    public interface IPhaseLoadoutApplier
    {
        void Apply(PhaseDefinition phase);
    }
}
