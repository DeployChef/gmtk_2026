using System;
using System.Collections.Generic;
using TheyWillDescend.Core.Bus;
using TheyWillDescend.Core.Bus.Events;
using TheyWillDescend.Core.Timeline;
using TMPro;
using UnityEngine;
using VContainer;

namespace TheyWillDescend.UI.Timeline
{
    /// <summary>
    /// TopBar timeline: adjacent phase segments that read as one long slider + years label.
    /// </summary>
    public sealed class TimelineHudView : MonoBehaviour
    {
        [SerializeField] private RectTransform segmentsRoot;
        [SerializeField] private TimelinePhaseSegmentView segmentPrefab;
        [SerializeField] private TMP_Text yearsLabel;
        [SerializeField] private string yearsFormat = "{0:0} yr";

        private ITimelineService _timeline;
        private IDisposable _yearsSub;
        private IDisposable _phaseStartedSub;
        private readonly List<TimelinePhaseSegmentView> _segments = new();
        private int _builtForPhaseCount = -1;

        [Inject]
        public void Construct(ITimelineService timeline, IGameEventBus bus)
        {
            _timeline = timeline;

            _yearsSub?.Dispose();
            _phaseStartedSub?.Dispose();
            _yearsSub = bus.Subscribe<TimelineYearsChangedEvent>(OnYears);
            _phaseStartedSub = bus.Subscribe<PhaseStartedEvent>(_ => RebuildIfNeeded());

            RebuildIfNeeded();
            RefreshProgress();
        }

        private void LateUpdate() => RefreshProgress();

        private void OnDestroy()
        {
            _yearsSub?.Dispose();
            _phaseStartedSub?.Dispose();
        }

        private void OnYears(TimelineYearsChangedEvent evt)
        {
            if (yearsLabel == null)
                return;
            yearsLabel.text = string.Format(yearsFormat, evt.YearsElapsed);
        }

        private void RebuildIfNeeded()
        {
            if (_timeline == null || segmentsRoot == null || segmentPrefab == null)
                return;

            var count = _timeline.PhaseCount;
            if (_builtForPhaseCount == count && _segments.Count == count)
                return;

            for (var i = segmentsRoot.childCount - 1; i >= 0; i--)
                Destroy(segmentsRoot.GetChild(i).gameObject);
            _segments.Clear();

            for (var i = 0; i < count; i++)
            {
                var segment = Instantiate(segmentPrefab, segmentsRoot);
                segment.gameObject.SetActive(true);
                segment.Setup(_timeline.GetPhase(i), i);
                _segments.Add(segment);
            }

            _builtForPhaseCount = count;
        }

        private void RefreshProgress()
        {
            if (_timeline == null || _segments.Count == 0)
                return;

            var current = _timeline.CurrentPhaseIndex;
            for (var i = 0; i < _segments.Count; i++)
            {
                float fill;
                if (i < current)
                    fill = 1f;
                else if (i > current)
                    fill = 0f;
                else
                    fill = _timeline.CurrentPhaseNormalizedProgress;

                _segments[i].SetFill(fill);
            }
        }
    }
}
