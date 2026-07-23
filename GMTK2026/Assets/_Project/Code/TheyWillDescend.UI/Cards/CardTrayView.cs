using System.Collections.Generic;
using DG.Tweening;
using TheyWillDescend.Core.Economy;
using TMPro;
using UnityEngine;

namespace TheyWillDescend.UI.Cards
{
    /// <summary>
    /// One hand-placed tray. Stack is a queue: first card stays on top, new cards go under.
    /// </summary>
    public sealed class CardTrayView : MonoBehaviour
    {
        [SerializeField] private ResourceDefinition resource;
        [SerializeField] private Transform stackRoot;
        [SerializeField] private TMP_Text counterLabel;

        public ResourceDefinition Resource => resource;
        public string ResourceId => resource != null ? resource.Id : string.Empty;

        private void Awake()
        {
            if (stackRoot == null)
                stackRoot = transform;
        }

        public void SetCounterText(string text)
        {
            if (counterLabel != null)
                counterLabel.text = text;
        }

        public void ClearCards()
        {
            if (stackRoot == null)
                return;

            for (var i = stackRoot.childCount - 1; i >= 0; i--)
                DestroyCard(stackRoot.GetChild(i));
        }

        /// <summary>
        /// FIFO stack: top = oldest (last sibling). New cards insert under and slide in from above.
        /// </summary>
        public void SyncStack(
            int count,
            GameObject cardPrefab,
            Vector2 stackOffset,
            int maxVisibleStack,
            float insertRisePixels,
            float insertDuration)
        {
            if (stackRoot == null || cardPrefab == null || resource == null)
                return;

            var visible = count <= 0 ? 0 : Mathf.Clamp(count, 0, Mathf.Max(1, maxVisibleStack));

            // Snapshot — never loop on childCount while Destroy is pending (leaks / freeze).
            var cards = new List<Transform>(stackRoot.childCount);
            for (var i = 0; i < stackRoot.childCount; i++)
                cards.Add(stackRoot.GetChild(i));

            // Hierarchy: last sibling = top. Trim from top first.
            while (cards.Count > visible)
            {
                var top = cards[cards.Count - 1];
                cards.RemoveAt(cards.Count - 1);
                DestroyCard(top);
            }

            var added = new List<RectTransform>();
            while (cards.Count < visible)
            {
                var instance = Instantiate(cardPrefab, stackRoot);
                instance.name = $"Card_{resource.Id}";
                var card = instance.GetComponentInChildren<IResourceCard>(true);
                card?.Setup(resource);

                // New card goes under the deck (behind top).
                instance.transform.SetAsFirstSibling();
                cards.Insert(0, instance.transform);

                if (instance.transform is RectTransform rt)
                    added.Add(rt);
            }

            Relayout(cards, stackOffset, added, insertRisePixels, insertDuration);
        }

        private static void DestroyCard(Transform card)
        {
            if (card == null)
                return;

            DOTween.Kill(card);
            // Unparent first so childCount drops this frame (Destroy is deferred).
            card.SetParent(null, false);
            Object.Destroy(card.gameObject);
        }

        private void Relayout(
            List<Transform> cardsBottomToTop,
            Vector2 stackOffset,
            List<RectTransform> animatedNewCards,
            float insertRisePixels,
            float insertDuration)
        {
            var n = cardsBottomToTop.Count;
            // depth 0 = top = last in list
            for (var depth = 0; depth < n; depth++)
            {
                var child = cardsBottomToTop[n - 1 - depth] as RectTransform;
                if (child == null)
                    continue;

                var target = stackOffset * depth;
                var shouldAnimate = animatedNewCards != null && animatedNewCards.Contains(child);

                DOTween.Kill(child);

                if (shouldAnimate && insertDuration > 0f)
                {
                    child.anchoredPosition = target + Vector2.up * insertRisePixels;
                    DOTween
                        .To(() => child.anchoredPosition, v => child.anchoredPosition = v, target, insertDuration)
                        .SetEase(Ease.OutCubic)
                        .SetTarget(child)
                        .SetLink(child.gameObject);
                }
                else
                {
                    child.anchoredPosition = target;
                }
            }
        }
    }
}
