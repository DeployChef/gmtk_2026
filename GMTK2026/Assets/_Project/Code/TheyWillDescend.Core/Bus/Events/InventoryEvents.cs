using TheyWillDescend.Core.Economy;

namespace TheyWillDescend.Core.Bus.Events
{
    public readonly struct InventoryChangedEvent
    {
        public readonly string ResourceId;
        public readonly ResourceDefinition Definition;
        public readonly int Count;
        public readonly int Capacity;

        public InventoryChangedEvent(
            string resourceId,
            ResourceDefinition definition,
            int count,
            int capacity)
        {
            ResourceId = resourceId;
            Definition = definition;
            Count = count;
            Capacity = capacity;
        }
    }

    public readonly struct InventoryClearedEvent
    {
    }
}
