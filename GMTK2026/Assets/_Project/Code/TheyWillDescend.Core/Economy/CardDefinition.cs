using UnityEngine;

namespace TheyWillDescend.Core.Economy
{
    [CreateAssetMenu(fileName = "CardDefinition", menuName = "They Will Descend/Card Definition")]
    public sealed class CardDefinition : ScriptableObject
    {
        [SerializeField] private string id = ResourceIds.Villager;
        [SerializeField] private string displayName = "Villager";
        [SerializeField] private CardKind kind = CardKind.Villager;
        [SerializeField] private Sprite icon;
        [TextArea(2, 5)]
        [SerializeField] private string description = "";

        public string Id => id;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? id : displayName;
        public CardKind Kind => kind;
        public Sprite Icon => icon;
        public string Description => description;
    }
}
