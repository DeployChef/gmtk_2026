using System;
using TheyWillDescend.Core.Bus;
using TheyWillDescend.Core.Bus.Events;
using TheyWillDescend.Core.Timeline;
using TMPro;
using UnityEngine;
using VContainer;

namespace TheyWillDescend.UI.Timeline
{
    /// <summary>
    /// TopBar timeline: hand-placed phase segments (no runtime spawn) + years label.
    /// Assign segments in order: index 0 = phase 0, etc.
    /// </summary>
    public sealed class TimelineHudView : MonoBehaviour
    {
        [SerializeField] private TimelinePhaseSegmentView[] segments;
        [SerializeField] private TMP_Text yearsLabel;
        [SerializeField] private string yearsFormat = "{0:0} yr";
        [Tooltip("If true, applies phase colors/titles from GameTimelineConfig onto hand-placed segments.")]
        [SerializeField] private bool applyPhaseVisualsFromConfig = true;

        private ITimelineService _timeline;
        private IDisposable _yearsSub;
        private IDisposable _phaseStartedSub;
        private bool _visualsApplied;

        [Inject]
        public void Construct(ITimelineService timeline, IGameEventBus bus)
        {
            _timeline = timeline;

            _yearsSub?.Dispose();
            _phaseStartedSub?.Dispose();
            _yearsSub = bus.Subscribe<TimelineYearsChangedEvent>(OnYears);
            _phaseStartedSub = bus.Subscribe<PhaseStartedEvent>(_ => ApplyVisualsOnce());

            _visualsApplied = false;
            ApplyVisualsOnce();
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

        private void ApplyVisualsOnce()
        {
            if (!applyPhaseVisualsFromConfig || _visualsApplied || _timeline == null || segments == null)
                return;

            for (var i = 0; i < segments.Length; i++)
            {
                if (segments[i] == null)
                    continue;
                segments[i].Setup(_timeline.GetPhase(i), i);
            }

            _visualsApplied = true;
        }

        private void RefreshProgress()
        {
            if (_timeline == null || segments == null || segments.Length == 0)
                return;

            var current = _timeline.CurrentPhaseIndex;
            for (var i = 0; i < segments.Length; i++)
            {
                if (segments[i] == null)
                    continue;

                float fill;
                if (i < current)
                    fill = 1f;
                else if (i > current)
                    fill = 0f;
                else
                    fill = _timeline.CurrentPhaseNormalizedProgress;

                segments[i].SetFill(fill);
            }
        }
    }
}
