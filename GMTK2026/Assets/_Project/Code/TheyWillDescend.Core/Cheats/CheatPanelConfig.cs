using System;
using TheyWillDescend.Core.Economy;
using TheyWillDescend.Core.Timeline;
using UnityEngine;

namespace TheyWillDescend.Core.Cheats
{
    [Serializable]
    public sealed class CheatBuiltBuilding
    {
        [Tooltip("Must match ProductionBuilding.BuildingId on the Game scene.")]
        [SerializeField] private int buildingId = 1;
        [SerializeField] private int workers;

        public int BuildingId => buildingId;
        public int Workers => Mathf.Max(0, workers);
    }

    /// <summary>
    /// Per-phase cheat jump loadout. Index in <see cref="CheatPanelConfig.PhaseLoadouts"/> = phase index.
    /// </summary>
    [Serializable]
    public sealed class CheatPhaseLoadout
    {
        [Tooltip("Optional label in Inspector.")]
        [SerializeField] private string label = "";
        [Tooltip("Cards granted on Jump when Grant All Cards On Jump is off.")]
        [SerializeField] private PhaseStartingCard[] startingCards = Array.Empty<PhaseStartingCard>();
        [Tooltip("These become Built (with workers). Every other building is Locked, then timeline unlocks 0..phase apply.")]
        [SerializeField] private CheatBuiltBuilding[] builtBuildings = Array.Empty<CheatBuiltBuilding>();

        public string Label => label;
        public PhaseStartingCard[] StartingCards => startingCards ?? Array.Empty<PhaseStartingCard>();
        public CheatBuiltBuilding[] BuiltBuildings => builtBuildings ?? Array.Empty<CheatBuiltBuilding>();
    }

    /// <summary>
    /// Debug / cheat settings for the Cheat Panel editor window. Not wired into GameLifetimeScope.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CheatPanelConfig",
        menuName = "They Will Descend/Cheat Panel Config")]
    public sealed class CheatPanelConfig : ScriptableObject
    {
        [Header("Grant cards")]
        [Tooltip("On Jump: fill All Cards Catalog instead of that phase's Starting Cards.")]
        [SerializeField] private bool grantAllCardsOnJump;
        [SerializeField] private ResourceDefinition[] allCardsCatalog = Array.Empty<ResourceDefinition>();
        [Tooltip("0 = fill to tray capacity (or Unlimited Grant Count).")]
        [SerializeField] private int grantAllCardsCount;
        [SerializeField] private int unlimitedGrantCount = 20;

        [Header("Phase jump loadouts (index = phase index)")]
        [Tooltip("Built vs Locked reset on Jump. After Built list, cumulative unlockBuildingIds from timeline (0..phase) become Buildable.")]
        [SerializeField] private CheatPhaseLoadout[] phaseLoadouts = Array.Empty<CheatPhaseLoadout>();

        public bool GrantAllCardsOnJump => grantAllCardsOnJump;
        public ResourceDefinition[] AllCardsCatalog => allCardsCatalog ?? Array.Empty<ResourceDefinition>();
        public int GrantAllCardsCount => Mathf.Max(0, grantAllCardsCount);
        public int UnlimitedGrantCount => Mathf.Max(0, unlimitedGrantCount);
        public CheatPhaseLoadout[] PhaseLoadouts => phaseLoadouts ?? Array.Empty<CheatPhaseLoadout>();

        public CheatPhaseLoadout GetPhaseLoadout(int phaseIndex)
        {
            var list = PhaseLoadouts;
            if (phaseIndex < 0 || phaseIndex >= list.Length)
                return null;
            return list[phaseIndex];
        }
    }
}
