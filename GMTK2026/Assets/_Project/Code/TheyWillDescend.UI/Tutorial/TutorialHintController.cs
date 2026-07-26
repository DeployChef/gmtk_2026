using System;
using TheyWillDescend.Core.Bus;
using TheyWillDescend.Core.Bus.Events;
using UnityEngine;
using VContainer;

namespace TheyWillDescend.UI.Tutorial
{
    [Serializable]
    public sealed class TutorialHintStep
    {
        [Tooltip("Objects to turn ON for this step.")]
        public GameObject[] objects = Array.Empty<GameObject>();
    }

    /// <summary>
    /// Hardcoded jam tutorial:
    /// 1 start → 2 card picked → 3 dropped into any production building →
    /// 4 resource produced → 5 card picked again → 6 pyramid offering → all off.
    /// Assign highlight objects per step in Inspector. Call <see cref="Begin"/> after opening.
    /// </summary>
    public sealed class TutorialHintController : MonoBehaviour
    {
        private const int StepStart = 0;
        private const int StepCardPicked = 1;
        private const int StepCardDropped = 2;
        private const int StepCardReceived = 3;
        private const int StepCardPickedAgain = 4;
        private const int StepPyramidOffering = 5;

        [SerializeField] private TutorialHintStep[] steps = Array.Empty<TutorialHintStep>();
        [SerializeField] private bool disableAllOnAwake = true;
        [Tooltip("Fallback if GameStartState never calls Begin (Play on Game scene alone).")]
        [SerializeField] private bool beginOnStart;

        private IGameEventBus _bus;
        private IDisposable _cardDragSub;
        private IDisposable _cardDropSub;
        private IDisposable _producedSub;
        private IDisposable _offerSub;
        private int _index = -1;
        private bool _running;
        private bool _begun;

        [Inject]
        public void Construct(IGameEventBus bus)
        {
            _bus = bus;
        }

        private void Awake()
        {
            if (disableAllOnAwake)
                HideAll();
        }

        private void Start()
        {
            if (beginOnStart && !_begun)
                Begin();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        public int CurrentStep => _index;
        public bool IsRunning => _running;

        public void Begin()
        {
            if (steps == null || steps.Length == 0)
            {
                Debug.LogWarning("[TutorialHint] Begin skipped — Steps is empty.", this);
                return;
            }

            _begun = true;
            _running = true;
            Subscribe();
            SetStep(StepStart);
            Debug.Log($"[TutorialHint] Begin → step {StepStart + 1} / {steps.Length}", this);
        }

        public void Stop()
        {
            _running = false;
            _index = -1;
            Unsubscribe();
            HideAll();
            Debug.Log("[TutorialHint] Stopped.", this);
        }

        private void Subscribe()
        {
            if (_bus == null)
            {
                Debug.LogWarning("[TutorialHint] No event bus — steps will not auto-advance.", this);
                return;
            }

            Unsubscribe();
            _cardDragSub = _bus.Subscribe<CardDragStartedEvent>(_ => OnCardPicked());
            _cardDropSub = _bus.Subscribe<CardDroppedOnBuildingEvent>(_ => OnCardDroppedIntoBuilding());
            _producedSub = _bus.Subscribe<ResourceProducedEvent>(_ => OnCardReceived());
            _offerSub = _bus.Subscribe<OfferingSubmittedEvent>(_ => OnPyramidOffering());
        }

        private void Unsubscribe()
        {
            _cardDragSub?.Dispose();
            _cardDropSub?.Dispose();
            _producedSub?.Dispose();
            _offerSub?.Dispose();
            _cardDragSub = null;
            _cardDropSub = null;
            _producedSub = null;
            _offerSub = null;
        }

        private void OnCardPicked()
        {
            if (!_running)
                return;

            if (_index == StepStart)
                GoTo(StepCardPicked);
            else if (_index == StepCardReceived)
                GoTo(StepCardPickedAgain);
        }

        private void OnCardDroppedIntoBuilding()
        {
            if (_running && _index == StepCardPicked)
                GoTo(StepCardDropped);
        }

        private void OnCardReceived()
        {
            if (_running && _index == StepCardDropped)
                GoTo(StepCardReceived);
        }

        private void OnPyramidOffering()
        {
            if (!_running || _index != StepCardPickedAgain)
                return;

            GoTo(StepPyramidOffering);
            Stop();
        }

        private void GoTo(int index)
        {
            if (steps == null || index < 0 || index >= steps.Length)
            {
                Stop();
                return;
            }

            SetStep(index);
            Debug.Log($"[TutorialHint] → step {index + 1}", this);
        }

        private void SetStep(int index)
        {
            _begun = true;
            _running = true;
            _index = index;
            ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            // Empty step = turn everything off (no active highlights).
            if (!StepHasObjects(_index))
            {
                HideAll();
                return;
            }

            for (var s = 0; s < steps.Length; s++)
            {
                var on = s == _index;
                var objs = steps[s].objects;
                if (objs == null)
                    continue;

                for (var i = 0; i < objs.Length; i++)
                {
                    var go = objs[i];
                    if (go == null)
                    {
                        Debug.LogWarning($"[TutorialHint] Step {s + 1} objects[{i}] is null.", this);
                        continue;
                    }

                    go.SetActive(on);
                }
            }
        }

        private bool StepHasObjects(int index)
        {
            if (steps == null || index < 0 || index >= steps.Length)
                return false;

            var objs = steps[index].objects;
            if (objs == null || objs.Length == 0)
                return false;

            for (var i = 0; i < objs.Length; i++)
            {
                if (objs[i] != null)
                    return true;
            }

            return false;
        }

        private void HideAll()
        {
            if (steps == null)
                return;

            for (var s = 0; s < steps.Length; s++)
            {
                var objs = steps[s].objects;
                if (objs == null)
                    continue;

                for (var i = 0; i < objs.Length; i++)
                {
                    var go = objs[i];
                    if (go != null)
                        go.SetActive(false);
                }
            }
        }
    }
}
