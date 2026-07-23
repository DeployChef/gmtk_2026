using TheyWillDescend.Core.Economy;

namespace TheyWillDescend.Core.Cards
{
    public interface ICardSpawner
    {
        IResourceCard Spawn(string cardId);
        IResourceCard Spawn(CardDefinition definition);
        bool TryConsume(string cardId);
        int CountById(string cardId);
        void ClearRail();
        void SpawnStartingHand();
    }
}
