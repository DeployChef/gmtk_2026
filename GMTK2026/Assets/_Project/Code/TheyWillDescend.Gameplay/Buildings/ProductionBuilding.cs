using TheyWillDescend.Core.Bus;
using TheyWillDescend.Core.Bus.Events;
using TheyWillDescend.Core.Economy;
using TheyWillDescend.Core.Inventory;
using UnityEngine;
using VContainer;

namespace TheyWillDescend.Gameplay.Buildings
{
    /// <summary>
    /// Core production building: workers, input stock, timed craft, output to inventory.
    /// </summary>
    public sealed class ProductionBuilding : MonoBehaviour
    {
        [SerializeField] private int buildingId = 1;
        [SerializeField] private BuildingRecipe recipe;
        [SerializeField] private int minWorkers;
        [SerializeField] private int maxWorkers = 3;
        [SerializeField] private int startingWorkers;

        private IGameEventBus _bus;
        private IInventory _inventory;
        private int _workers;
        private int _storedInput;
        private float _progress;
        private bool _producing;

        public int BuildingId => buildingId;
        public BuildingRecipe Recipe => recipe;
        public int Workers => _workers;
        public int MinWorkers => minWorkers;
        public int MaxWorkers => maxWorkers;
        public int StoredInput => _storedInput;
        public int InputRequired => recipe != null ? recipe.InputAmountRequired : 0;
        public float NormalizedProgress =>
            recipe == null ? 0f : Mathf.Clamp01(_progress / recipe.ProductionDurationSeconds);
        public bool IsProducing => _producing;
        public bool CanProduce =>
            recipe != null
            && _workers >= recipe.WorkersRequired
            && (!recipe.RequiresInput || _storedInput >= recipe.InputAmountRequired);

        public bool CanHireWorker =>
            _workers < maxWorkers
            && _inventory != null
            && _inventory.GetCount(ResourceIds.Villager) > 0;

        public event System.Action StateChanged;

        [Inject]
        public void Construct(IGameEventBus bus, IInventory inventory)
        {
            _bus = bus;
            _inventory = inventory;
        }

        private void Awake()
        {
            _workers = Mathf.Clamp(startingWorkers, minWorkers, maxWorkers);
        }

        private void Start()
        {
            // Ensure HUD / trays know initial assigned workers after DI.
            PublishWorkers();
        }

        private void Update()
        {
            if (recipe == null)
                return;

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

            if (_progress < recipe.ProductionDurationSeconds)
                return;

            CompleteProduction();
        }

        public bool TryAddWorker()
        {
            if (_workers >= maxWorkers)
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
            if (_workers <= minWorkers || _inventory == null)
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
            if (recipe == null || !recipe.RequiresInput || string.IsNullOrEmpty(resourceId))
                return false;

            if (resourceId == ResourceIds.Villager)
                return false;

            if (resourceId != recipe.InputResourceId)
                return false;

            if (_storedInput >= recipe.InputAmountRequired)
                return false;

            if (_inventory == null || !_inventory.TryRemove(resourceId))
                return false;

            _storedInput++;
            PublishInput();
            StateChanged?.Invoke();
            return true;
        }

        private void CompleteProduction()
        {
            if (recipe.RequiresInput)
                _storedInput -= recipe.InputAmountRequired;

            _progress = 0f;
            _producing = CanProduce;

            PublishInput();
            PublishProgress();
            _bus?.Publish(new ResourceProducedEvent(buildingId, recipe.OutputResourceId));

            if (recipe.OutputResource != null)
                _inventory?.TryAdd(recipe.OutputResource);
            else
                Debug.LogWarning($"[ProductionBuilding:{buildingId}] Recipe output ResourceDefinition is missing.");

            StateChanged?.Invoke();
        }

        private void PublishWorkers() =>
            _bus?.Publish(new BuildingWorkersChangedEvent(buildingId, _workers));

        private void PublishInput()
        {
            if (recipe == null)
                return;

            _bus?.Publish(new BuildingInputChangedEvent(
                buildingId,
                recipe.InputResourceId,
                _storedInput,
                recipe.InputAmountRequired));
        }

        private void PublishProgress() =>
            _bus?.Publish(new BuildingProductionProgressEvent(buildingId, NormalizedProgress));
    }
}
