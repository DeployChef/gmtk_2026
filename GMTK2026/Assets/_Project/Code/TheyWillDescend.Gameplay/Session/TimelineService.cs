using TheyWillDescend.Core.Bus;
using TheyWillDescend.Core.Bus.Events;
using TheyWillDescend.Core.Economy;
using TheyWillDescend.Core.Inventory;
using TheyWillDescend.Core.Timeline;
using UnityEngine;

namespace TheyWillDescend.Gameplay.Session
{
    public sealed class TimelineService : ITimelineService
    {
        private readonly GameTimelineConfig _config;
        private readonly IGameEventBus _bus;
        private readonly IInventory _inventory;
        private readonly IPyramidTimerService _pyramidTimer;
        private readonly IPhaseLoadoutApplier _loadoutApplier;

        private int[] _delivered;
        private float _phaseElapsed;
        private float _totalElapsed;
        private float _lastPublishedYears = -1f;
        private bool _running;
        private bool _runFinished;

        public TimelineService(
            GameTimelineConfig config,
            IGameEventBus bus,
            IInventory inventory,
            IPyramidTimerService pyramidTimer,
            IPhaseLoadoutApplier loadoutApplier)
        {
            _config = config;
            _bus = bus;
            _inventory = inventory;
            _pyramidTimer = pyramidTimer;
            _loadoutApplier = loadoutApplier;
        }

        public bool IsRunning => _running && !_runFinished;
        public int PhaseCount => _config != null ? _config.PhaseCount : 0;
        public int CurrentPhaseIndex { get; private set; }
        public PhaseDefinition CurrentPhase => GetPhase(CurrentPhaseIndex);

        public PhaseDefinition GetPhase(int index)
        {
            if (_config == null || index < 0 || index >= _config.PhaseCount)
                return null;
            return _config.Phases[index];
        }

        public float CurrentPhaseElapsedSeconds => _phaseElapsed;

        public float CurrentPhaseNormalizedProgress
        {
            get
            {
                var phase = CurrentPhase;
                if (phase == null)
                    return 0f;
                return Mathf.Clamp01(_phaseElapsed / phase.DurationSeconds);
            }
        }

        public float TotalElapsedSeconds => _totalElapsed;
        public float YearsElapsed =>
            _config != null ? _totalElapsed * _config.YearsPerRealtimeSecond : 0f;

        public bool IsCurrentOfferComplete
        {
            get
            {
                var phase = CurrentPhase;
                if (phase == null)
                    return true;

                var reqs = phase.Requirements;
                EnsureDeliveredBuffer(reqs.Length);
                for (var i = 0; i < reqs.Length; i++)
                {
                    if (_delivered[i] < reqs[i].Count)
                        return false;
                }

                return true;
            }
        }

        public int GetOfferDelivered(int requirementIndex)
        {
            if (_delivered == null || requirementIndex < 0 || requirementIndex >= _delivered.Length)
                return 0;
            return _delivered[requirementIndex];
        }

        public int GetOfferRequired(int requirementIndex)
        {
            var phase = CurrentPhase;
            if (phase == null)
                return 0;
            var reqs = phase.Requirements;
            if (requirementIndex < 0 || requirementIndex >= reqs.Length)
                return 0;
            return reqs[requirementIndex].Count;
        }

        public void StartRun()
        {
            if (_config == null || _config.PhaseCount == 0)
            {
                Debug.LogError("[TimelineService] GameTimelineConfig missing or has no phases.");
                _running = false;
                return;
            }

            _running = true;
            _runFinished = false;
            _totalElapsed = 0f;
            _lastPublishedYears = -1f;
            _pyramidTimer.ResetToBaseline();
            _loadoutApplier?.ApplyRunStart(_config.RunStartCards, _config.RunStartBuildings);
            EnterPhase(0, applyUnlocks: true);
            PublishYears(force: true);
        }

        public void Tick(float deltaTime)
        {
            if (!_running || _runFinished || deltaTime <= 0f)
                return;

            _totalElapsed += deltaTime;
            _phaseElapsed += deltaTime;
            PublishYears(force: false);

            var phase = CurrentPhase;
            if (phase == null)
                return;

            if (_phaseElapsed < phase.DurationSeconds)
                return;

            EndCurrentPhaseAndAdvance();
        }

        public bool TryOffer(string resourceId)
        {
            if (!_running || _runFinished || string.IsNullOrEmpty(resourceId))
                return false;

            if (resourceId == ResourceIds.Villager)
            {
                Reject(resourceId);
                return false;
            }

            var phase = CurrentPhase;
            if (phase == null)
                return false;

            var reqs = phase.Requirements;
            EnsureDeliveredBuffer(reqs.Length);

            var matchIndex = -1;
            for (var i = 0; i < reqs.Length; i++)
            {
                var item = reqs[i];
                if (item.Resource == null || item.ResourceId != resourceId)
                    continue;
                if (_delivered[i] >= item.Count)
                    continue;
                matchIndex = i;
                break;
            }

            if (matchIndex < 0)
            {
                Reject(resourceId);
                return false;
            }

            if (!_inventory.TryRemove(resourceId))
                return false;

            _delivered[matchIndex]++;
            var reward = reqs[matchIndex].SecondsReward;
            _pyramidTimer.AddSeconds(reward);
            _bus.Publish(new OfferingSubmittedEvent(
                resourceId,
                reqs[matchIndex].Resource,
                reward,
                CurrentPhaseIndex));
            return true;
        }

        public void DebugJumpToPhase(int phaseIndex)
        {
            if (_config == null || _config.PhaseCount == 0)
                return;

            phaseIndex = Mathf.Clamp(phaseIndex, 0, _config.PhaseCount - 1);
            _running = true;
            _runFinished = false;
            _pyramidTimer.ResetToBaseline();
            EnterPhase(phaseIndex, applyUnlocks: false);
            Debug.Log($"[TimelineService] Debug jump → phase {phaseIndex} ({CurrentPhase?.Title}).");
        }

        private void Reject(string resourceId)
        {
            var delta = _config != null ? _config.WrongOfferingTimerDelta : -1f;
            _pyramidTimer.AddSeconds(delta);
            _bus.Publish(new OfferingRejectedEvent(resourceId, delta, CurrentPhaseIndex));
        }

        private void EndCurrentPhaseAndAdvance()
        {
            var index = CurrentPhaseIndex;
            var complete = IsCurrentOfferComplete;

            if (!complete)
                _bus.Publish(new PhaseFailedEvent(index));

            _bus.Publish(new PhaseCompletedEvent(index, complete));

            var next = index + 1;
            if (next >= PhaseCount)
            {
                _runFinished = true;
                Debug.Log("[TimelineService] All phases finished.");
                return;
            }

            EnterPhase(next, applyUnlocks: true);
        }

        private void EnterPhase(int index, bool applyUnlocks)
        {
            CurrentPhaseIndex = index;
            _phaseElapsed = 0f;
            var phase = CurrentPhase;
            var reqCount = phase != null ? phase.Requirements.Length : 0;
            _delivered = new int[reqCount];

            if (applyUnlocks && phase != null)
                _loadoutApplier?.ApplyUnlocks(phase);

            if (phase != null)
            {
                _bus.Publish(new PhaseStartedEvent(index, phase.Title, phase.DurationSeconds));
                Debug.Log($"[TimelineService] Phase {index} started: {phase.Title} ({phase.DurationSeconds:0.#}s).");
            }
        }

        private void EnsureDeliveredBuffer(int length)
        {
            if (_delivered != null && _delivered.Length == length)
                return;
            _delivered = new int[length];
        }

        private void PublishYears(bool force)
        {
            var years = YearsElapsed;
            if (!force && Mathf.Abs(years - _lastPublishedYears) < 0.05f)
                return;

            _lastPublishedYears = years;
            _bus.Publish(new TimelineYearsChangedEvent(years));
        }
    }
}
