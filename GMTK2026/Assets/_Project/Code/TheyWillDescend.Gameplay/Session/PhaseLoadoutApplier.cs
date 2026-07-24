using System.Collections.Generic;
using TheyWillDescend.Core.Inventory;
using TheyWillDescend.Core.Timeline;
using TheyWillDescend.Gameplay.Buildings;
using UnityEngine;

namespace TheyWillDescend.Gameplay.Session
{
    public sealed class PhaseLoadoutApplier : IPhaseLoadoutApplier
    {
        private readonly IInventory _inventory;

        public PhaseLoadoutApplier(IInventory inventory)
        {
            _inventory = inventory;
        }

        public void Apply(PhaseDefinition phase)
        {
            if (phase == null)
                return;

            ApplyCards(phase.StartingCards);
            ApplyBuildings(phase.StartingBuildings);
        }

        private void ApplyCards(PhaseStartingCard[] cards)
        {
            _inventory.Clear();

            if (cards == null)
                return;

            for (var i = 0; i < cards.Length; i++)
            {
                var entry = cards[i];
                if (entry?.Resource == null || entry.Count <= 0)
                    continue;

                for (var n = 0; n < entry.Count; n++)
                {
                    if (!_inventory.TryAdd(entry.Resource))
                    {
                        Debug.LogWarning(
                            $"[PhaseLoadout] Could not add {entry.Resource.Id} (tray full?). Stopped this stack.");
                        break;
                    }
                }
            }
        }

        private void ApplyBuildings(PhaseStartingBuilding[] setups)
        {
            if (setups == null || setups.Length == 0)
                return;

            var byId = new Dictionary<int, PhaseStartingBuilding>();
            for (var i = 0; i < setups.Length; i++)
            {
                var setup = setups[i];
                if (setup == null)
                    continue;
                byId[setup.BuildingId] = setup;
            }

            var buildings = Object.FindObjectsByType<ProductionBuilding>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (var i = 0; i < buildings.Length; i++)
            {
                var building = buildings[i];
                if (byId.TryGetValue(building.BuildingId, out var setup))
                    building.ApplyPhaseLoadout(setup.Active, setup.Workers);
                else
                    building.ApplyPhaseLoadout(active: false, workers: 0);
            }
        }
    }
}
