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
            {
                var child = stackRoot.GetChild(i);
                DOTween.Kill(child);
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// FIFO stack: index 0 from top = oldest. New cards insert under and slide in from above.
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

            // Hierarchy: first sibling = bottom of deck, last sibling = top (oldest, clickable).
            while (stackRoot.childCount > visible)
            {
                var top = stackRoot.GetChild(stackRoot.childCount - 1);
                DOTween.Kill(top);
                Destroy(top.gameObject);
            }

            var added = new List<RectTransform>();
            while (stackRoot.childCount < visible)
            {
                var instance = Instantiate(cardPrefab, stackRoot);
                instance.name = $"Card_{resource.Id}";
                var card = instance.GetComponentInChildren<IResourceCard>(true);
                card?.Setup(resource);

                var rt = instance.transform as RectTransform;
                if (rt == null)
                    rt = instance.GetComponentInChildren<RectTransform>();

                // New card goes under the deck (behind top).
                instance.transform.SetAsFirstSibling();

                if (rt != null)
                    added.Add(rt);
            }

            Relayout(stackOffset, added, insertRisePixels, insertDuration);
        }

        private void Relayout(
            Vector2 stackOffset,
            List<RectTransform> animatedNewCards,
            float insertRisePixels,
            float insertDuration)
        {
            var n = stackRoot.childCount;
            // depth 0 = top = last sibling
            for (var depth = 0; depth < n; depth++)
            {
                var child = stackRoot.GetChild(n - 1 - depth) as RectTransform;
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
                        .SetTarget(child);
                }
                else
                {
                    child.anchoredPosition = target;
                }
            }
        }
    }
}
