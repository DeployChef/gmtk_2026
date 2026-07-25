using System.Collections.Generic;
using DG.Tweening;
using TheyWillDescend.Core.Audio;
using TheyWillDescend.Core.Economy;
using TMPro;
using UnityEngine;

namespace TheyWillDescend.UI.Cards
{
    /// <summary>
    /// One hand-placed tray. Stack is a queue: oldest on top, new cards go under.
    /// </summary>
    public sealed class CardTrayView : MonoBehaviour
    {
        [SerializeField] private ResourceDefinition resource;
        [SerializeField] private Transform stackRoot;
        [SerializeField] private TMP_Text counterLabel;

        private Vector2 _stackOffset = new(8f, -8f);
        private float _insertRisePixels = 30f;
        private float _insertDuration = 0.25f;

        public ResourceDefinition Resource => resource;
        public string ResourceId => resource != null ? resource.Id : string.Empty;
        public Transform StackRoot => stackRoot != null ? stackRoot : transform;

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
        /// Returns how many new card visuals were spawned.
        /// </summary>
        public int SyncStack(
            int count,
            GameObject cardPrefab,
            Vector2 stackOffset,
            int maxVisibleStack,
            float insertRisePixels,
            float insertDuration,
            IAudioManager audio = null)
        {
            if (stackRoot == null || cardPrefab == null || resource == null)
                return 0;

            _stackOffset = stackOffset;
            _insertRisePixels = insertRisePixels;
            _insertDuration = insertDuration;

            var visible = count <= 0 ? 0 : Mathf.Clamp(count, 0, Mathf.Max(1, maxVisibleStack));

            var cards = CollectStackCards();

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

                var view = instance.GetComponentInChildren<ResourceCardView>(true);
                view?.BindAudio(audio);

                instance.transform.SetAsFirstSibling();
                cards.Insert(0, instance.transform);

                if (instance.transform is RectTransform rt)
                    added.Add(rt);
            }

            Relayout(cards, added, animateNew: true);
            return added.Count;
        }

        /// <summary>
        /// Call after a card was reparented out of the stack for dragging.
        /// </summary>
        public void NotifyCardDetached()
        {
            Relayout(CollectStackCards(), animatedCards: null, animateNew: false);
        }

        /// <summary>
        /// Rejected drop: card goes under the whole stack (newest / first sibling), then restack.
        /// </summary>
        public void ReturnCardUnderStack(Transform card, float moveDuration)
        {
            if (card == null || stackRoot == null)
                return;

            DOTween.Kill(card);
            card.SetParent(stackRoot, worldPositionStays: true);
            card.SetAsFirstSibling();

            var cards = CollectStackCards();
            var animate = card as RectTransform;
            var animated = animate != null ? new List<RectTransform> { animate } : null;

            // Snap layout targets; optionally tween the returned card from its world pose.
            Relayout(cards, animated, animateNew: false, returningCard: animate, returnDuration: moveDuration);
        }

        private List<Transform> CollectStackCards()
        {
            var cards = new List<Transform>(stackRoot.childCount);
            for (var i = 0; i < stackRoot.childCount; i++)
                cards.Add(stackRoot.GetChild(i));
            return cards;
        }

        private static void DestroyCard(Transform card)
        {
            if (card == null)
                return;

            DOTween.Kill(card);
            card.SetParent(null, false);
            Object.Destroy(card.gameObject);
        }

        /// <param name="cardsBottomToTop">Index 0 = under (newest), last = top (oldest).</param>
        private void Relayout(
            List<Transform> cardsBottomToTop,
            List<RectTransform> animatedCards,
            bool animateNew,
            RectTransform returningCard = null,
            float returnDuration = 0f)
        {
            var n = cardsBottomToTop.Count;
            for (var depth = 0; depth < n; depth++)
            {
                var child = cardsBottomToTop[n - 1 - depth] as RectTransform;
                if (child == null)
                    continue;

                var target = _stackOffset * depth;
                DOTween.Kill(child);

                var isReturning = returningCard != null && child == returningCard && returnDuration > 0.01f;
                var isNew = animateNew && animatedCards != null && animatedCards.Contains(child);

                if (isReturning)
                {
                    DOTween
                        .To(() => child.anchoredPosition, v => child.anchoredPosition = v, target, returnDuration)
                        .SetEase(Ease.OutCubic)
                        .SetTarget(child)
                        .SetLink(child.gameObject);
                }
                else if (isNew && _insertDuration > 0f)
                {
                    child.anchoredPosition = target + Vector2.up * _insertRisePixels;
                    DOTween
                        .To(() => child.anchoredPosition, v => child.anchoredPosition = v, target, _insertDuration)
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
