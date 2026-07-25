using System.Collections.Generic;
using TheyWillDescend.Core.Audio;
using TheyWillDescend.Core.Buildings;
using TheyWillDescend.Core.Bus;
using TheyWillDescend.Core.Bus.Events;
using TheyWillDescend.Core.Economy;
using TheyWillDescend.Core.Inventory;
using TheyWillDescend.Core.Timeline;
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
        [Tooltip("VFX prefab spawned behind the building when a card hovers over it.")]
        [SerializeField] private GameObject dropVfxPrefab;
        [Tooltip("Spawn point for the drop VFX. Defaults to building position if unset.")]
        [SerializeField] private Transform vfxSpawnPoint;

        private IGameEventBus _bus;
        private IInventory _inventory;
        private IAudioManager _audio;
        private ITimelineService _timeline;
        private BuildingSlotState _slotState;
        private int _workers;
        private readonly Dictionary<string, int> _storedInputs = new();
        private readonly Dictionary<string, int> _storedBuildCosts = new();
        private float _progress;
        private bool _producing;
        private float _buildProgress;
        private float _disabledTimer;
        private int _villagersProduced;
        private GameObject _activeDropVfx;

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
        /// <summary>Villagers produced by this building this run (start inventory villager is not counted).</summary>
        public int VillagersProduced => _villagersProduced;
        public bool UsesHireOffers => definition != null && definition.HasHireOffers;
        public BuildCostItem[] CurrentHireOfferCost =>
            definition != null
                ? definition.GetHireOfferCost(_villagersProduced)
                : System.Array.Empty<BuildCostItem>();
        public int StoredInput =>
            definition != null && _storedInputs.TryGetValue(definition.InputResourceId, out var stored)
                ? stored
                : 0;
        public int InputRequired =>
            definition != null && definition.TryGetProductionInput(0, out _, out var required)
                ? required
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
            && (UsesHireOffers
                ? AllHireCostsFulfilled()
                : (!definition.RequiresInput || AllInputsFulfilled()));

        public bool CanHireWorker =>
            _slotState == BuildingSlotState.Built
            && !UsesHireOffers
            && definition != null
            && definition.WorkersRequired > 0
            && _workers < maxWorkers
            && _inventory != null
            && _inventory.GetCount(ResourceIds.Villager) > 0;

        /// <summary>True when a villager card should assign as a worker (not as production input).</summary>
        public bool CanAcceptWorkerCard =>
            _slotState == BuildingSlotState.Built
            && !UsesHireOffers
            && definition != null
            && definition.WorkersRequired > 0
            && _workers < maxWorkers;

        public event System.Action StateChanged;

        [Inject]
        public void Construct(
            IGameEventBus bus,
            IInventory inventory,
            IAudioManager audio,
            ITimelineService timeline)
        {
            _bus = bus;
            _inventory = inventory;
            _audio = audio;
            _timeline = timeline;
        }

        private void Awake()
        {
            _slotState = initialState;
            // Do not inflate to minWorkers for free — that desyncs available/assigned totals.
            _workers = _slotState == BuildingSlotState.Built
                ? Mathf.Clamp(startingWorkers, 0, maxWorkers)
                : 0;
        }

        private void Start()
        {
            PublishWorkers();
            StateChanged?.Invoke();
        }

        /// <summary>
        /// Shows/hides the drop indicator VFX when a card hovers over this building.
        /// </summary>
        public void ShowDropIndicator(bool show)
        {
            if (!show)
            {
                if (_activeDropVfx != null)
                    Destroy(_activeDropVfx);
                _activeDropVfx = null;
                return;
            }

            if (dropVfxPrefab == null || _activeDropVfx != null)
                return;

            var spawnPos = vfxSpawnPoint != null ? vfxSpawnPoint.position : GetBottomCenter();
            _activeDropVfx = Instantiate(dropVfxPrefab, spawnPos, Quaternion.identity, transform);
        }

        private Vector3 GetBottomCenter()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                for (var i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);
                return new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            }

            var colliders = GetComponentsInChildren<Collider>();
            if (colliders.Length > 0)
            {
                var bounds = colliders[0].bounds;
                for (var i = 1; i < colliders.Length; i++)
                    bounds.Encapsulate(colliders[i].bounds);
                return new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            }

            return transform.position;
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
            _progress += Time.deltaTime * GetEraProductionSpeedMultiplier();
            PublishProgress();

            if (_progress < definition.ProductionDurationSeconds)
                return;

            CompleteProduction();
        }

        private float GetEraProductionSpeedMultiplier()
        {
            var phase = _timeline?.CurrentPhase;
            if (phase == null || definition == null)
                return 1f;

            return phase.GetProductionSpeedMultiplier(buildingId, definition.OutputResourceId);
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
            _villagersProduced = 0;
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
            if (UsesHireOffers)
                return false;

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

            if (UsesHireOffers)
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

        /// <summary>
        /// Removes one assigned worker permanently (no return to inventory). Used by lightning strikes.
        /// </summary>
        public bool TryKillWorker()
        {
            if (_slotState != BuildingSlotState.Built || _workers <= 0)
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

            if (definition == null)
                return false;

            if (UsesHireOffers)
                return TryAcceptHireResource(resourceId);

            if (!definition.RequiresInput)
                return false;

            if (!TryFindProductionInput(resourceId, out var required) || required <= 0)
                return false;

            // Buffer allowed: dump more than one craft's worth; extras stay for the next craft.
            if (_inventory == null || !_inventory.TryRemove(resourceId))
                return false;

            _storedInputs.TryGetValue(resourceId, out var stored);
            _storedInputs[resourceId] = stored + 1;
            PublishInput();
            StateChanged?.Invoke();
            return true;
        }

        private bool TryFindProductionInput(string resourceId, out int required)
        {
            required = 0;
            if (definition == null)
                return false;

            for (var i = 0; i < definition.ProductionInputSlotCount; i++)
            {
                if (!definition.TryGetProductionInput(i, out var resource, out var amount))
                    continue;
                if (resource.Id != resourceId)
                    continue;

                required = amount;
                return true;
            }

            return false;
        }

        private bool TryAcceptHireResource(string resourceId)
        {
            if (IsDisabled || _producing)
                return false;

            var costs = CurrentHireOfferCost;
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
            for (var i = 0; i < definition.ProductionInputSlotCount; i++)
            {
                if (!definition.TryGetProductionInput(i, out var card, out var required))
                    continue;

                if (!_storedInputs.TryGetValue(card.Id, out var stored) || stored < required)
                    return false;
            }

            return true;
        }

        private bool AllHireCostsFulfilled()
        {
            var costs = CurrentHireOfferCost;
            for (var i = 0; i < costs.Length; i++)
            {
                var item = costs[i];
                if (item?.Resource == null || item.Count <= 0)
                    continue;

                if (!_storedInputs.TryGetValue(item.ResourceId, out var stored) || stored < item.Count)
                    return false;
            }

            // Empty step = free hire (timer only).
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
            _audio?.Play(AudioCatalog.Ids.BuildStart);
            _bus?.Publish(new BuildingConstructionStartedEvent(buildingId));
            PublishProgress();
        }

        private void CompleteConstruction()
        {
            _buildProgress = 0f;
            _storedBuildCosts.Clear();
            _workers = 0;
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
            if (UsesHireOffers)
            {
                _storedInputs.Clear();
                _villagersProduced++;
            }
            else if (definition.RequiresInput)
            {
                for (var i = 0; i < definition.ProductionInputSlotCount; i++)
                {
                    if (!definition.TryGetProductionInput(i, out var card, out var required))
                        continue;

                    if (!_storedInputs.TryGetValue(card.Id, out var stored))
                        continue;

                    stored -= required;
                    if (stored <= 0)
                        _storedInputs.Remove(card.Id);
                    else
                        _storedInputs[card.Id] = stored;
                }
            }

            _progress = 0f;
            _producing = false;

            PublishInput();
            PublishProgress();
            _bus?.Publish(new ResourceProducedEvent(buildingId, definition.OutputResourceId));

            if (definition.OutputResource != null)
            {
                _inventory?.TryAdd(definition.OutputResource);
                PlayProduceSound();
            }
            else
                Debug.LogWarning($"[ProductionBuilding:{buildingId}] Recipe output ResourceDefinition is missing.");

            // Re-evaluate after consume / hire step advance (StateChanged after so HUD sees new offer).
            _producing = CanProduce;
            StateChanged?.Invoke();
        }

        private void PlayProduceSound()
        {
            var soundId = !string.IsNullOrEmpty(produceSoundId)
                ? produceSoundId
                : AudioCatalog.ResolveProduceSound(definition.OutputResourceId);

            if (!string.IsNullOrEmpty(soundId))
                _audio?.Play(soundId);
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
