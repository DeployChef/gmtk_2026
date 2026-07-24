using System.Collections.Generic;
using TheyWillDescend.Core.Audio;
using TheyWillDescend.Core.Buildings;
using TheyWillDescend.Core.Bus;
using TheyWillDescend.Core.Bus.Events;
using TheyWillDescend.Core.Economy;
using TheyWillDescend.Core.Inventory;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace TheyWillDescend.Gameplay.Buildings
{
    /// <summary>
    /// Slot: Locked → Buildable → Constructing → Built (production).
    /// </summary>
    public sealed class ProductionBuilding : MonoBehaviour
    {
        [SerializeField] private int buildingId = 1;
        [FormerlySerializedAs("recipe")]
        [SerializeField] private BuildingDefinition definition;
        [SerializeField] private BuildingSlotState initialState = BuildingSlotState.Built;
        [SerializeField] private int minWorkers;
        [SerializeField] private int maxWorkers = 3;
        [SerializeField] private int startingWorkers;
        [Tooltip("AudioCatalog id played when craft completes. Empty = silent.")]
        [SerializeField] private string produceSoundId = "";

        private IGameEventBus _bus;
        private IInventory _inventory;
        private IAudioManager _audio;
        private BuildingSlotState _slotState;
        private int _workers;
        private readonly Dictionary<string, int> _storedInputs = new();
        private readonly Dictionary<string, int> _storedBuildCosts = new();
        private float _progress;
        private bool _producing;
        private float _buildProgress;
        private float _disabledTimer;

        public int BuildingId => buildingId;
        public BuildingDefinition Definition => definition;
        /// <summary>Alias for older call sites / HUD.</summary>
        public BuildingDefinition Recipe => definition;
        public BuildingSlotState SlotState => _slotState;
        public bool IsBuilt => _slotState == BuildingSlotState.Built;
        public bool IsConstructing => _slotState == BuildingSlotState.Constructing;
        public bool IsBuildable => _slotState == BuildingSlotState.Buildable;
        public bool IsLocked => _slotState == BuildingSlotState.Locked;
        public int Workers => _workers;
        public int MinWorkers => minWorkers;
        public int MaxWorkers => maxWorkers;
        public int StoredInput =>
            definition != null && _storedInputs.TryGetValue(definition.InputResourceId, out var stored)
                ? stored
                : 0;
        public int InputRequired => definition != null && definition.InputResources.Length > 0
                                    && definition.InputAmounts.Length > 0
            ? Mathf.Max(0, definition.InputAmounts[0])
            : 0;

        public float NormalizedProgress
        {
            get
            {
                if (_slotState == BuildingSlotState.Constructing)
                {
                    var duration = definition != null ? definition.BuildDurationSeconds : 0f;
                    return duration <= 0.01f ? 1f : Mathf.Clamp01(_buildProgress / duration);
                }

                if (_slotState != BuildingSlotState.Built || definition == null)
                    return 0f;

                return Mathf.Clamp01(_progress / definition.ProductionDurationSeconds);
            }
        }

        public bool IsProducing =>
            _slotState == BuildingSlotState.Constructing
            || (_slotState == BuildingSlotState.Built && _producing);

        public bool IsDisabled => _disabledTimer > 0f;

        public bool CanProduce =>
            _slotState == BuildingSlotState.Built
            && definition != null
            && !IsDisabled
            && _workers >= definition.WorkersRequired
            && (!definition.RequiresInput || AllInputsFulfilled());

        public bool CanHireWorker =>
            _slotState == BuildingSlotState.Built
            && _workers < maxWorkers
            && _inventory != null
            && _inventory.GetCount(ResourceIds.Villager) > 0;

        public event System.Action StateChanged;

        [Inject]
        public void Construct(IGameEventBus bus, IInventory inventory, IAudioManager audio)
        {
            _bus = bus;
            _inventory = inventory;
            _audio = audio;
        }

        private void Awake()
        {
            _slotState = initialState;
            _workers = _slotState == BuildingSlotState.Built
                ? Mathf.Clamp(startingWorkers, minWorkers, maxWorkers)
                : 0;
        }

        private void Start()
        {
            PublishWorkers();
            StateChanged?.Invoke();
        }

        private void Update()
        {
            if (_slotState == BuildingSlotState.Constructing)
            {
                TickConstruction();
                return;
            }

            if (_slotState != BuildingSlotState.Built || definition == null)
                return;

            if (IsDisabled)
            {
                _disabledTimer -= Time.deltaTime;

                if (_producing || _progress > 0f)
                {
                    _producing = false;
                    _progress = 0f;
                    PublishProgress();
                }

                if (_disabledTimer <= 0f)
                {
                    _disabledTimer = 0f;
                    StateChanged?.Invoke();
                }

                return;
            }

            if (!CanProduce)
            {
                if (_producing || _progress > 0f)
                {
                    _producing = false;
                    _progress = 0f;
                    PublishProgress();
                    StateChanged?.Invoke();
                }

                return;
            }

            _producing = true;
            _progress += Time.deltaTime;
            PublishProgress();

            if (_progress < definition.ProductionDurationSeconds)
                return;

            CompleteProduction();
        }

        public void DisableTemporarily(float seconds)
        {
            if (_slotState != BuildingSlotState.Built)
                return;

            _disabledTimer = Mathf.Max(0.01f, seconds);
            _producing = false;
            _progress = 0f;
            PublishProgress();
            StateChanged?.Invoke();
        }

        /// <summary>
        /// Debug / phase-start loadout. Keeps GameObject active (ruins stay visible when locked).
        /// </summary>
        public void ApplyPhaseLoadout(bool active, int workers)
        {
            gameObject.SetActive(true);

            _progress = 0f;
            _producing = false;
            _buildProgress = 0f;
            _disabledTimer = 0f;
            _storedInputs.Clear();
            _storedBuildCosts.Clear();

            if (!active)
            {
                _workers = 0;
                SetSlotState(BuildingSlotState.Locked);
                PublishProgress();
                PublishWorkers();
                return;
            }

            _workers = Mathf.Clamp(workers, minWorkers, maxWorkers);
            SetSlotState(BuildingSlotState.Built);
            PublishProgress();
            PublishWorkers();
        }

        /// <summary>Locked → Buildable (or skip to Constructing/Built if no cost).</summary>
        public bool TryUnlock()
        {
            if (_slotState != BuildingSlotState.Locked)
                return false;

            _bus?.Publish(new BuildingUnlockedEvent(buildingId));

            if (definition == null || !definition.HasBuildCost)
            {
                if (definition == null || definition.BuildDurationSeconds <= 0.01f)
                    CompleteConstruction();
                else
                    BeginConstruction();
            }
            else
            {
                SetSlotState(BuildingSlotState.Buildable);
            }

            return true;
        }

        public int GetStoredAmount(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId))
                return 0;

            if (_slotState == BuildingSlotState.Buildable)
                return _storedBuildCosts.TryGetValue(resourceId, out var buildStored) ? buildStored : 0;

            return _storedInputs.TryGetValue(resourceId, out var stored) ? stored : 0;
        }

        public bool TryAddWorker()
        {
            if (_slotState != BuildingSlotState.Built || _workers >= maxWorkers)
                return false;

            if (_inventory == null || !_inventory.TryRemove(ResourceIds.Villager))
                return false;

            _workers++;
            PublishWorkers();
            StateChanged?.Invoke();
            return true;
        }

        public bool TryAcceptVillagerCard()
        {
            if (_slotState != BuildingSlotState.Built)
                return false;

            if (definition != null && definition.WorkersRequired <= 0)
                return false;

            if (_workers >= maxWorkers)
                return false;

            if (_inventory == null || !_inventory.TryRemove(ResourceIds.Villager))
                return false;

            _workers++;
            PublishWorkers();
            StateChanged?.Invoke();
            return true;
        }

        public bool TryRemoveWorker()
        {
            if (_slotState != BuildingSlotState.Built || _workers <= minWorkers || _inventory == null)
                return false;

            var villager = _inventory.GetDefinition(ResourceIds.Villager);
            if (villager == null)
            {
                Debug.LogWarning(
                    $"[ProductionBuilding:{buildingId}] Cannot return villager — ResourceDefinition unknown to inventory.");
                return false;
            }

            if (!_inventory.TryAdd(villager))
                return false;

            _workers--;
            PublishWorkers();
            StateChanged?.Invoke();
            return true;
        }

        public bool TryAcceptResource(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId))
                return false;

            if (_slotState == BuildingSlotState.Buildable)
                return TryAcceptBuildResource(resourceId);

            if (_slotState != BuildingSlotState.Built)
                return false;

            if (definition == null || !definition.RequiresInput)
                return false;

            var inputIndex = -1;
            var inputs = definition.InputResources;
            for (var i = 0; i < inputs.Length; i++)
            {
                if (inputs[i] != null && inputs[i].Id == resourceId)
                {
                    inputIndex = i;
                    break;
                }
            }

            if (inputIndex < 0)
                return false;

            var amounts = definition.InputAmounts;
            var required = inputIndex < amounts.Length ? amounts[inputIndex] : 1;
            if (required <= 0)
                return false;

            _storedInputs.TryGetValue(resourceId, out var stored);
            if (stored >= required)
                return false;

            if (_inventory == null || !_inventory.TryRemove(resourceId))
                return false;

            _storedInputs[resourceId] = stored + 1;
            PublishInput();
            StateChanged?.Invoke();
            return true;
        }

        private bool TryAcceptBuildResource(string resourceId)
        {
            if (definition == null)
                return false;

            var costs = definition.BuildCost;
            var costIndex = -1;
            for (var i = 0; i < costs.Length; i++)
            {
                var item = costs[i];
                if (item?.Resource == null || item.Count <= 0)
                    continue;
                if (item.ResourceId != resourceId)
                    continue;
                costIndex = i;
                break;
            }

            if (costIndex < 0)
                return false;

            var required = costs[costIndex].Count;
            _storedBuildCosts.TryGetValue(resourceId, out var stored);
            if (stored >= required)
                return false;

            if (_inventory == null || !_inventory.TryRemove(resourceId))
                return false;

            stored++;
            _storedBuildCosts[resourceId] = stored;
            _bus?.Publish(new BuildingBuildProgressEvent(buildingId, resourceId, stored, required));
            StateChanged?.Invoke();

            if (AllBuildCostsFulfilled())
                BeginConstruction();

            return true;
        }

        private bool AllBuildCostsFulfilled()
        {
            if (definition == null)
                return false;

            var costs = definition.BuildCost;
            for (var i = 0; i < costs.Length; i++)
            {
                var item = costs[i];
                if (item?.Resource == null || item.Count <= 0)
                    continue;

                if (!_storedBuildCosts.TryGetValue(item.ResourceId, out var stored) || stored < item.Count)
                    return false;
            }

            return true;
        }

        private bool AllInputsFulfilled()
        {
            var inputs = definition.InputResources;
            var amounts = definition.InputAmounts;
            for (var i = 0; i < inputs.Length; i++)
            {
                var card = inputs[i];
                if (card == null)
                    continue;

                var required = i < amounts.Length ? amounts[i] : 1;
                if (required <= 0)
                    continue;

                if (!_storedInputs.TryGetValue(card.Id, out var stored) || stored < required)
                    return false;
            }

            return true;
        }

        private void TickConstruction()
        {
            var duration = definition != null ? definition.BuildDurationSeconds : 0f;
            if (duration <= 0.01f)
            {
                CompleteConstruction();
                return;
            }

            _buildProgress += Time.deltaTime;
            PublishProgress();

            if (_buildProgress < duration)
                return;

            CompleteConstruction();
        }

        private void BeginConstruction()
        {
            _buildProgress = 0f;
            SetSlotState(BuildingSlotState.Constructing);
            _bus?.Publish(new BuildingConstructionStartedEvent(buildingId));
            PublishProgress();
        }

        private void CompleteConstruction()
        {
            _buildProgress = 0f;
            _storedBuildCosts.Clear();
            _workers = Mathf.Clamp(_workers, minWorkers, maxWorkers);
            SetSlotState(BuildingSlotState.Built);
            _bus?.Publish(new BuildingConstructedEvent(buildingId));
            PublishProgress();
            PublishWorkers();
        }

        private void SetSlotState(BuildingSlotState next)
        {
            _slotState = next;
            StateChanged?.Invoke();
        }

        private void CompleteProduction()
        {
            if (definition.RequiresInput)
            {
                var inputs = definition.InputResources;
                var amounts = definition.InputAmounts;
                for (var i = 0; i < inputs.Length; i++)
                {
                    var card = inputs[i];
                    if (card == null)
                        continue;

                    var required = i < amounts.Length ? amounts[i] : 1;
                    if (required <= 0)
                        continue;

                    if (_storedInputs.TryGetValue(card.Id, out var stored))
                        _storedInputs[card.Id] = stored - required;
                }
            }

            _progress = 0f;
            _producing = CanProduce;

            PublishInput();
            PublishProgress();
            _bus?.Publish(new ResourceProducedEvent(buildingId, definition.OutputResourceId));

            if (definition.OutputResource != null)
            {
                _inventory?.TryAdd(definition.OutputResource);
                if (!string.IsNullOrEmpty(produceSoundId))
                    _audio?.Play(produceSoundId);
            }
            else
                Debug.LogWarning($"[ProductionBuilding:{buildingId}] Recipe output ResourceDefinition is missing.");

            StateChanged?.Invoke();
        }

        private void PublishWorkers() =>
            _bus?.Publish(new BuildingWorkersChangedEvent(buildingId, _workers));

        private void PublishInput()
        {
            if (definition == null)
                return;

            _storedInputs.TryGetValue(definition.InputResourceId, out var stored);
            _bus?.Publish(new BuildingInputChangedEvent(
                buildingId,
                definition.InputResourceId,
                stored,
                definition.InputAmountRequired));
        }

        private void PublishProgress() =>
            _bus?.Publish(new BuildingProductionProgressEvent(buildingId, NormalizedProgress));
    }
}
