using TheyWillDescend.Core.Cheats;

namespace TheyWillDescend.Core.Timeline
{
    /// <summary>
    /// Phase loadout (StartRun / debug jump) and construction unlocks (every PhaseStarted).
    /// </summary>
    public interface IPhaseLoadoutApplier
    {
        void Apply(PhaseDefinition phase);
        void ApplyUnlocks(PhaseDefinition phase);

        /// <summary>Debug cheat: clear inventory and fill catalog from cheat config.</summary>
        void GrantAllCardsFromCatalog(CheatPanelConfig cheats);
    }
}
