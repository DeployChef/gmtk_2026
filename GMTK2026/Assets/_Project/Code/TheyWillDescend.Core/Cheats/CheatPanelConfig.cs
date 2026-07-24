using TheyWillDescend.Core.Cheats;
using TheyWillDescend.Core.Economy;
using UnityEngine;

namespace TheyWillDescend.Core.Cheats
{
    /// <summary>
    /// Debug / cheat settings for the Cheat Panel editor window. Not wired into GameLifetimeScope.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CheatPanelConfig",
        menuName = "They Will Descend/Cheat Panel Config")]
    public sealed class CheatPanelConfig : ScriptableObject
    {
        [Header("Grant cards")]
        [Tooltip("After Cheat Panel Jump: also fill catalog (replaces that phase's Starting Cards).")]
        [SerializeField] private bool grantAllCardsOnJump;
        [Tooltip("Resource types to grant.")]
        [SerializeField] private ResourceDefinition[] allCardsCatalog = System.Array.Empty<ResourceDefinition>();
        [Tooltip("How many of each. 0 = fill to tray capacity (or Unlimited Grant Count).")]
        [SerializeField] private int grantAllCardsCount;
        [Tooltip("When count is 0 and the resource has unlimited tray capacity (e.g. Villager).")]
        [SerializeField] private int unlimitedGrantCount = 20;

        public bool GrantAllCardsOnJump => grantAllCardsOnJump;
        public ResourceDefinition[] AllCardsCatalog => allCardsCatalog ?? System.Array.Empty<ResourceDefinition>();
        public int GrantAllCardsCount => Mathf.Max(0, grantAllCardsCount);
        public int UnlimitedGrantCount => Mathf.Max(0, unlimitedGrantCount);
    }
}
