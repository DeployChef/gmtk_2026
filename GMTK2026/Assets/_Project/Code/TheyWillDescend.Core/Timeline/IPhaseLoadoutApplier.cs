using TheyWillDescend.Core.Cheats;

namespace TheyWillDescend.Core.Timeline
{
    /// <summary>
    /// Phase loadout (StartRun / cheat jump) and construction unlocks.
    /// </summary>
    public interface IPhaseLoadoutApplier
    {
        void ApplyRunStart(PhaseStartingCard[] cards, PhaseStartingBuilding[] buildings);
        void ApplyUnlocks(PhaseDefinition phase);
        void ApplyUnlocksCumulative(GameTimelineConfig timeline, int throughPhaseIndexInclusive);

        /// <summary>
        /// Cheat jump: reset all buildings (Built list vs Locked), then cumulative unlocks → Buildable.
        /// Cards: grant catalog if GrantAllCardsOnJump, else phase loadout starting cards.
        /// </summary>
        void ApplyCheatJump(CheatPanelConfig cheats, GameTimelineConfig timeline, int phaseIndex);

        void GrantAllCardsFromCatalog(CheatPanelConfig cheats);
    }
}
