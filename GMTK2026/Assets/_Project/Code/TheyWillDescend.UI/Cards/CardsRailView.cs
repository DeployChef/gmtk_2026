using TheyWillDescend.Core.Cards;
using TheyWillDescend.Core.Economy;
using UnityEngine;

namespace TheyWillDescend.UI.Cards
{
    /// <summary>
    /// Bottom-bar CardsRail: spawns / counts / clears cards. Put on CardsRail under GameHud.
    /// </summary>
    public sealed class CardsRailView : MonoBehaviour, ICardSpawner
    {
        [SerializeField] private Transform railRoot;
        [SerializeField] private GameObject cardPrefab;
        [Header("Starting hand")]
        [SerializeField] private CardDefinition startingVillagerCard;
        [SerializeField] private int startingVillagerCount = 1;

        private void Awake()
        {
            if (railRoot == null)
                railRoot = transform;
        }

        public void SpawnStartingHand()
        {
            if (startingVillagerCard == null)
            {
                Debug.LogWarning("[CardsRail] Starting villager CardDefinition is not set.");
                return;
            }

            for (var i = 0; i < startingVillagerCount; i++)
                Spawn(startingVillagerCard);
        }

        public IResourceCard Spawn(CardDefinition definition)
        {
            if (definition == null)
                return null;

            var card = CreateCardInstance();
            if (card == null)
                return null;

            card.Setup(definition);
            return card;
        }

        public IResourceCard Spawn(string cardId)
        {
            if (string.IsNullOrEmpty(cardId))
                return null;

            var card = CreateCardInstance();
            if (card == null)
                return null;

            card.Setup(cardId);
            return card;
        }

        public int CountById(string cardId)
        {
            if (string.IsNullOrEmpty(cardId) || railRoot == null)
                return 0;

            var count = 0;
            for (var i = 0; i < railRoot.childCount; i++)
            {
                var card = railRoot.GetChild(i).GetComponentInChildren<IResourceCard>(true);
                if (card != null && card.ResourceId == cardId)
                    count++;
            }

            return count;
        }

        public bool TryConsume(string cardId)
        {
            if (string.IsNullOrEmpty(cardId) || railRoot == null)
                return false;

            for (var i = 0; i < railRoot.childCount; i++)
            {
                var child = railRoot.GetChild(i);
                var card = child.GetComponentInChildren<IResourceCard>(true);
                if (card == null || card.ResourceId != cardId)
                    continue;

                Destroy(child.gameObject);
                return true;
            }

            return false;
        }

        public void ClearRail()
        {
            if (railRoot == null)
                return;

            for (var i = railRoot.childCount - 1; i >= 0; i--)
                Destroy(railRoot.GetChild(i).gameObject);
        }

        private IResourceCard CreateCardInstance()
        {
            if (cardPrefab == null)
            {
                Debug.LogError("[CardsRail] Card prefab is missing.");
                return null;
            }

            var instance = Instantiate(cardPrefab, railRoot);
            var card = instance.GetComponentInChildren<IResourceCard>(true);
            if (card == null)
            {
                Debug.LogError("[CardsRail] Card prefab has no IResourceCard.");
                Destroy(instance);
            }

            return card;
        }
    }
}
