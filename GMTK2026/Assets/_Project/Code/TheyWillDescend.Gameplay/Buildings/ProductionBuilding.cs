using TheyWillDescend.Core.Bus;
using TheyWillDescend.Core.Bus.Events;
using TheyWillDescend.Core.Cards;
using TheyWillDescend.Core.Economy;
using UnityEngine;
using VContainer;

namespace TheyWillDescend.Gameplay.Buildings
{
    /// <summary>
    /// Core production building: workers, input stock, timed craft, output spawn.
    /// </summary>
    public sealed class ProductionBuilding : MonoBehaviour
    {
        [SerializeField] private int buildingId = 1;
        [SerializeField] private BuildingRecipe recipe;
        [SerializeField] private int minWorkers;
        [SerializeField] private int maxWorkers = 3;
        [SerializeField] private int startingWorkers;
        [SerializeField] private Transform outputSpawnPoint;
        [SerializeField] private GameObject outputCardPrefab;
        [SerializeField] private Transform cardParent;

        private IGameEventBus _bus;
        private ICardSpawner _cardSpawner;
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
            && _cardSpawner != null
            && _cardSpawner.CountById(ResourceIds.Villager) > 0;

        public event System.Action StateChanged;

        [Inject]
        public void Construct(IGameEventBus bus, ICardSpawner cardSpawner)
        {
            _bus = bus;
            _cardSpawner = cardSpawner;
        }

        private void Awake()
        {
            _workers = Mathf.Clamp(startingWorkers, minWorkers, maxWorkers);
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

        /// <summary>HUD [+] — consumes one Villager card from CardsRail.</summary>
        public bool TryAddWorker()
        {
            if (_workers >= maxWorkers)
                return false;

            if (_cardSpawner == null || !_cardSpawner.TryConsume(ResourceIds.Villager))
                return false;

            _workers++;
            PublishWorkers();
            StateChanged?.Invoke();
            return true;
        }

        /// <summary>DnD Villager card onto building — card is destroyed by the view.</summary>
        public bool TryAcceptVillagerCard()
        {
            if (_workers >= maxWorkers)
                return false;

            _workers++;
            PublishWorkers();
            StateChanged?.Invoke();
            return true;
        }

        /// <summary>HUD [-] — returns one Villager card to CardsRail.</summary>
        public bool TryRemoveWorker()
        {
            if (_workers <= minWorkers)
                return false;

            _workers--;
            _cardSpawner?.Spawn(ResourceIds.Villager);
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
            SpawnOutputCard();
            StateChanged?.Invoke();
        }

        private void SpawnOutputCard()
        {
            if (_cardSpawner != null)
            {
                _cardSpawner.Spawn(recipe.OutputResourceId);
                return;
            }

            if (outputCardPrefab == null)
            {
                Debug.LogWarning($"[ProductionBuilding:{buildingId}] Output card prefab is missing.");
                return;
            }

            var parent = cardParent != null ? cardParent : transform;
            var spawnPos = outputSpawnPoint != null ? outputSpawnPoint.position : transform.position + Vector3.right;
            var instance = Instantiate(outputCardPrefab, spawnPos, Quaternion.identity, parent);

            var card = instance.GetComponentInChildren<IResourceCard>(true);
            if (card != null)
                card.Setup(recipe.OutputResourceId);
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
