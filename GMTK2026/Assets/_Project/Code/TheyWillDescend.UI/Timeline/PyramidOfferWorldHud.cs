using System;
using System.Collections.Generic;
using TheyWillDescend.Core.Bus;
using TheyWillDescend.Core.Bus.Events;
using TheyWillDescend.Core.Timeline;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace TheyWillDescend.UI.Timeline
{
    /// <summary>
    /// World-space offer HUD: icons + counts for current phase requirements (same pattern as building inputs).
    /// </summary>
    public sealed class PyramidOfferWorldHud : MonoBehaviour
    {
        [SerializeField] private Transform offerContainer;
        [SerializeField] private GameObject offerIconPrefab;

        private ITimelineService _timeline;
        private readonly List<PyramidOfferIconView> _slots = new();
        private int _builtPhaseIndex = -1;
        private int _builtReqCount = -1;
        private IDisposable _phaseStartedSub;

        [Inject]
        public void Construct(ITimelineService timeline, IGameEventBus bus)
        {
            _timeline = timeline;
            _phaseStartedSub?.Dispose();
            _phaseStartedSub = bus.Subscribe<PhaseStartedEvent>(_ => Rebuild());
            Rebuild();
            RefreshCounts();
        }

        private void OnDestroy() => _phaseStartedSub?.Dispose();

        private void LateUpdate()
        {
            if (_timeline == null)
                return;

            if (_timeline.CurrentPhaseIndex != _builtPhaseIndex
                || (_timeline.CurrentPhase?.Requirements.Length ?? 0) != _builtReqCount)
                Rebuild();

            RefreshCounts();
        }

        private void Rebuild()
        {
            if (offerContainer == null || _timeline == null)
                return;

            for (var i = offerContainer.childCount - 1; i >= 0; i--)
                Destroy(offerContainer.GetChild(i).gameObject);
            _slots.Clear();

            var phase = _timeline.CurrentPhase;
            _builtPhaseIndex = _timeline.CurrentPhaseIndex;
            _builtReqCount = phase?.Requirements.Length ?? 0;

            if (phase == null)
                return;

            var reqs = phase.Requirements;
            for (var i = 0; i < reqs.Length; i++)
            {
                var item = reqs[i];
                var go = offerIconPrefab != null
                    ? Instantiate(offerIconPrefab, offerContainer)
                    : CreateDefaultSlot(offerContainer);

                go.SetActive(true);

                var view = go.GetComponent<PyramidOfferIconView>();
                if (view == null)
                {
                    view = go.AddComponent<PyramidOfferIconView>();
                    view.Bind(
                        go.GetComponentInChildren<Image>(),
                        go.GetComponentInChildren<TMP_Text>());
                }

                view.Setup(item.Resource, 0, item.Count);
                _slots.Add(view);
            }
        }

        private void RefreshCounts()
        {
            if (_timeline == null)
                return;

            for (var i = 0; i < _slots.Count; i++)
            {
                _slots[i].SetCount(
                    _timeline.GetOfferDelivered(i),
                    _timeline.GetOfferRequired(i));
            }
        }

        private static GameObject CreateDefaultSlot(Transform parent)
        {
            var go = new GameObject("OfferIcon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(80, 80);

            var img = go.GetComponent<Image>();
            img.preserveAspect = true;

            var labelGo = new GameObject("Count", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = (RectTransform)labelGo.transform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0.35f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 22f;
            tmp.text = "0/0";

            var view = go.AddComponent<PyramidOfferIconView>();
            view.Bind(img, tmp);
            return go;
        }
    }
}
