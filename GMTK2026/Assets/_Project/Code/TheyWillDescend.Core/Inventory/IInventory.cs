using TheyWillDescend.Core.Economy;

namespace TheyWillDescend.Core.Inventory
{
    public interface IInventory
    {
        int GetCount(ResourceDefinition definition);
        int GetCount(string resourceId);
        ResourceDefinition GetDefinition(string resourceId);
        bool TryAdd(ResourceDefinition definition, int amount = 1);
        bool TryRemove(ResourceDefinition definition, int amount = 1);
        bool TryRemove(string resourceId, int amount = 1);
        void Clear();
    }
}
