using UnityEngine;

namespace TheyWillDescend.Core.Economy
{
    /// <summary>
    /// Canonical resource / card type. Used by recipes (input/output), trays, and card visuals.
    /// </summary>
    [CreateAssetMenu(fileName = "ResourceDefinition", menuName = "They Will Descend/Resource Definition")]
    public sealed class ResourceDefinition : ScriptableObject
    {
        [SerializeField] private string id = ResourceIds.Id1;
        [SerializeField] private string displayName = "Resource";
        [SerializeField] private ResourceKind kind = ResourceKind.Resource;
        [SerializeField] private Sprite icon;
        [Tooltip("Max cards in this type's tray. -1 = unlimited (villagers).")]
        [SerializeField] private int trayCapacity = 8;

        public string Id => id;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? id : displayName;
        public ResourceKind Kind => kind;
        public Sprite Icon => icon;

        /// <summary>False for villagers (and any type with trayCapacity &lt; 0).</summary>
        public bool HasTrayCapacityLimit => trayCapacity >= 0;

        public int TrayCapacity => trayCapacity;
    }
}
