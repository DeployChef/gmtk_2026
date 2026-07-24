using System;
using System.Collections.Generic;
using TheyWillDescend.Core.Audio;
using TheyWillDescend.Core.Bus;
using TheyWillDescend.Core.Bus.Events;
using TheyWillDescend.Core.Economy;
using TheyWillDescend.Core.Inventory;
using TheyWillDescend.Gameplay.Buildings;
using UnityEngine;
using VContainer;

namespace TheyWillDescend.UI.Cards
{
    /// <summary>
    /// Wires hand-placed <see cref="CardTrayView"/> slots to inventory bus events.
    /// Does not create trays at runtime.
    /// </summary>
    public sealed class InventoryTraysView : MonoBehaviour
    {
        [SerializeField] private CardTrayView[] trays;
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private Vector2 stackOffset = new(8f, -8f);
        [SerializeField] private int maxVisibleStack = 5;
        [SerializeField] private float insertRisePixels = 30f;
        [SerializeField] private float insertDuration = 0.25f;

        private IInventory _inventory;
        private IAudioManager _audio;
        private IDisposable _changedSub;
        private IDisposable _clearedSub;
        private IDisposable _workersSub;
        private readonly Dictionary<string, CardTrayView> _byId = new();
        private readonly Dictionary<int, int> _workersByBuilding = new();
        private bool _suppressGainSfx;

        public ResourceDefinition FindVillagerResource()
        {
            if (trays == null)
                return null;

            for (var i = 0; i < trays.Length; i++)
            {
                var tray = trays[i];
                if (tray?.Resource == null)
                    continue;

                if (tray.Resource.Kind == ResourceKind.Villager
                    || tray.Resource.Id == ResourceIds.Villager)
                    return tray.Resource;
            }

            return null;
        }

        [Inject]
        public void Construct(IGameEventBus bus, IInventory inventory, IAudioManager audio)
        {
            DisposeSubscriptions();
            _inventory = inventory;
            _audio = audio;
            RebuildLookup();
            _changedSub = bus.Subscribe<InventoryChangedEvent>(OnInventoryChanged);
            _clearedSub = bus.Subscribe<InventoryClearedEvent>(_ => ClearAllVisuals());
            _workersSub = bus.Subscribe<BuildingWorkersChangedEvent>(OnWorkersChanged);
            CaptureInitialWorkers();
            _suppressGainSfx = true;
            SyncAllFromInventory();
            _suppressGainSfx = false;
        }

        private void Awake() => RebuildLookup();

        private void OnDestroy() => DisposeSubscriptions();

        private void DisposeSubscriptions()
        {
            _changedSub?.Dispose();
            _clearedSub?.Dispose();
            _workersSub?.Dispose();
            _changedSub = null;
            _clearedSub = null;
            _workersSub = null;
        }

        private void RebuildLookup()
        {
            _byId.Clear();
            if (trays == null)
                return;

            for (var i = 0; i < trays.Length; i++)
            {
                var tray = trays[i];
                if (tray == null || tray.Resource == null)
                    continue;

                var id = tray.ResourceId;
                if (string.IsNullOrEmpty(id))
                    continue;

                if (_byId.ContainsKey(id))
                {
                    Debug.LogWarning($"[InventoryTrays] Duplicate tray for resource id '{id}'.", tray);
                    continue;
                }

                _byId[id] = tray;
            }
        }

        private void OnInventoryChanged(InventoryChangedEvent e)
        {
            if (!_byId.TryGetValue(e.ResourceId, out var tray))
                return;

            var spawned = tray.SyncStack(
                e.Count,
                cardPrefab,
                stackOffset,
                maxVisibleStack,
                insertRisePixels,
                insertDuration,
                _audio);
            RefreshCounter(tray, e.Count, e.Capacity);

            if (!_suppressGainSfx && spawned > 0)
                _audio?.Play(AudioCatalog.Ids.CardPickup);
        }

        private void OnWorkersChanged(BuildingWorkersChangedEvent e)
        {
            _workersByBuilding[e.BuildingId] = e.Workers;
            RefreshVillagerCounterOnly();
        }

        private void SyncAllFromInventory()
        {
            if (_inventory == null || trays == null)
                return;

            for (var i = 0; i < trays.Length; i++)
            {
                var tray = trays[i];
                if (tray == null || tray.Resource == null)
                    continue;

                var def = tray.Resource;
                var count = _inventory.GetCount(def);
                var capacity = def.HasTrayCapacityLimit ? def.TrayCapacity : -1;
                tray.SyncStack(
                    count,
                    cardPrefab,
                    stackOffset,
                    maxVisibleStack,
                    insertRisePixels,
                    insertDuration,
                    _audio);
                RefreshCounter(tray, count, capacity);
            }
        }

        private void ClearAllVisuals()
        {
            if (trays == null)
                return;

            for (var i = 0; i < trays.Length; i++)
                trays[i]?.ClearCards();

            RefreshVillagerCounterOnly();
        }

        private void RefreshCounter(CardTrayView tray, int count, int capacity)
        {
            if (tray == null || tray.Resource == null)
                return;

            if (tray.Resource.Kind == ResourceKind.Villager)
            {
                var total = count + AssignedVillagers;
                tray.SetCounterText($"{count}/{total}");
                return;
            }

            tray.SetCounterText(capacity >= 0 ? $"{count}/{capacity}" : count.ToString());
        }

        private void RefreshVillagerCounterOnly()
        {
            if (trays == null || _inventory == null)
                return;

            for (var i = 0; i < trays.Length; i++)
            {
                var tray = trays[i];
                if (tray?.Resource == null || tray.Resource.Kind != ResourceKind.Villager)
                    continue;

                var count = _inventory.GetCount(tray.Resource);
                var capacity = tray.Resource.HasTrayCapacityLimit ? tray.Resource.TrayCapacity : -1;
                RefreshCounter(tray, count, capacity);
            }
        }

        private void CaptureInitialWorkers()
        {
            _workersByBuilding.Clear();
            var buildings = FindObjectsByType<ProductionBuilding>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < buildings.Length; i++)
            {
                var b = buildings[i];
                if (b != null)
                    _workersByBuilding[b.BuildingId] = b.Workers;
            }
        }

        private int AssignedVillagers
        {
            get
            {
                var sum = 0;
                foreach (var pair in _workersByBuilding)
                    sum += pair.Value;
                return sum;
            }
        }
    }
}
