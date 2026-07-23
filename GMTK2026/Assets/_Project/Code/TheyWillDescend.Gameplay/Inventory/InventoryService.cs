using System.Collections.Generic;
using TheyWillDescend.Core.Bus;
using TheyWillDescend.Core.Bus.Events;
using TheyWillDescend.Core.Economy;
using TheyWillDescend.Core.Inventory;

namespace TheyWillDescend.Gameplay.Inventory
{
    /// <summary>
    /// Source of truth for available cards. UI listens to bus events.
    /// </summary>
    public sealed class InventoryService : IInventory
    {
        private readonly IGameEventBus _bus;
        private readonly Dictionary<string, int> _counts = new();
        private readonly Dictionary<string, ResourceDefinition> _definitions = new();

        public InventoryService(IGameEventBus bus)
        {
            _bus = bus;
        }

        public int GetCount(ResourceDefinition definition)
        {
            if (definition == null)
                return 0;
            return GetCount(definition.Id);
        }

        public int GetCount(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId))
                return 0;
            return _counts.TryGetValue(resourceId, out var count) ? count : 0;
        }

        public ResourceDefinition GetDefinition(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId))
                return null;
            return _definitions.TryGetValue(resourceId, out var def) ? def : null;
        }

        public bool TryAdd(ResourceDefinition definition, int amount = 1)
        {
            if (definition == null || amount <= 0)
                return false;

            _definitions[definition.Id] = definition;

            var addedAny = false;
            for (var i = 0; i < amount; i++)
            {
                if (TryAddOne(definition))
                    addedAny = true;
            }

            return addedAny;
        }

        public bool TryRemove(ResourceDefinition definition, int amount = 1)
        {
            if (definition == null)
                return false;
            return TryRemove(definition.Id, amount);
        }

        public bool TryRemove(string resourceId, int amount = 1)
        {
            if (string.IsNullOrEmpty(resourceId) || amount <= 0)
                return false;

            if (!_counts.TryGetValue(resourceId, out var count) || count < amount)
                return false;

            count -= amount;
            if (count <= 0)
            {
                _counts.Remove(resourceId);
                count = 0;
            }
            else
            {
                _counts[resourceId] = count;
            }

            _definitions.TryGetValue(resourceId, out var definition);
            PublishChanged(resourceId, definition, count);
            return true;
        }

        public void Clear()
        {
            _counts.Clear();
            // Keep definitions so returns / UI can still resolve SO after clear+reseed.
            _bus.Publish(new InventoryClearedEvent());
        }

        private bool TryAddOne(ResourceDefinition definition)
        {
            var id = definition.Id;
            var count = GetCount(id);

            if (definition.HasTrayCapacityLimit && count >= definition.TrayCapacity)
                return false;

            count++;
            _counts[id] = count;
            PublishChanged(id, definition, count);
            return true;
        }

        private void PublishChanged(string resourceId, ResourceDefinition definition, int count)
        {
            var capacity = definition != null && definition.HasTrayCapacityLimit
                ? definition.TrayCapacity
                : -1;
            _bus.Publish(new InventoryChangedEvent(resourceId, definition, count, capacity));
        }
    }
}
