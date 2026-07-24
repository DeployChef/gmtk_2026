using System.Collections.Generic;
using TheyWillDescend.Core.Cheats;
using TheyWillDescend.Core.Economy;
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

        public void ApplyRunStart(PhaseStartingCard[] cards, PhaseStartingBuilding[] buildings)
        {
            ApplyCards(cards);
            ApplyBuildings(buildings);
        }

        public void ApplyUnlocks(PhaseDefinition phase)
        {
            if (phase == null)
                return;

            UnlockIds(phase.UnlockBuildingIds);
        }

        public void ApplyUnlocksCumulative(GameTimelineConfig timeline, int throughPhaseIndexInclusive)
        {
            if (timeline == null || throughPhaseIndexInclusive < 0)
                return;

            var ids = new HashSet<int>();
            var max = Mathf.Min(throughPhaseIndexInclusive, timeline.PhaseCount - 1);
            for (var p = 0; p <= max; p++)
            {
                var phase = timeline.Phases[p];
                if (phase == null)
                    continue;

                var unlock = phase.UnlockBuildingIds;
                for (var i = 0; i < unlock.Length; i++)
                    ids.Add(unlock[i]);
            }

            if (ids.Count == 0)
                return;

            UnlockIdSet(ids);
        }

        public void ApplyCheatJump(CheatPanelConfig cheats, GameTimelineConfig timeline, int phaseIndex)
        {
            var loadout = cheats != null ? cheats.GetPhaseLoadout(phaseIndex) : null;
            var built = loadout != null ? loadout.BuiltBuildings : System.Array.Empty<CheatBuiltBuilding>();

            ResetBuildingsForCheatJump(built);
            ApplyUnlocksCumulative(timeline, phaseIndex);

            if (cheats != null && cheats.GrantAllCardsOnJump)
                GrantAllCardsFromCatalog(cheats);
            else if (loadout != null)
                ApplyCards(loadout.StartingCards);
            else
                _inventory.Clear();
        }

        public void GrantAllCardsFromCatalog(CheatPanelConfig cheats)
        {
            _inventory.Clear();

            if (cheats == null)
            {
                Debug.LogWarning("[PhaseLoadout] CheatPanelConfig is null — cannot grant cards.");
                return;
            }

            var catalog = cheats.AllCardsCatalog;
            if (catalog.Length == 0)
            {
                Debug.LogWarning(
                    "[PhaseLoadout] All Cards Catalog is empty — assign ResourceDefinitions on CheatPanelConfig.");
                return;
            }

            var fixedCount = cheats.GrantAllCardsCount;
            for (var i = 0; i < catalog.Length; i++)
            {
                var definition = catalog[i];
                if (definition == null)
                    continue;

                if (fixedCount > 0)
                {
                    GrantAmount(definition, fixedCount);
                    continue;
                }

                if (definition.HasTrayCapacityLimit)
                    GrantUntilFull(definition);
                else
                    GrantAmount(definition, cheats.UnlimitedGrantCount);
            }

            Debug.Log("[PhaseLoadout] Granted all cards from cheat catalog.");
        }

        private void ResetBuildingsForCheatJump(CheatBuiltBuilding[] built)
        {
            var byId = new Dictionary<int, CheatBuiltBuilding>();
            if (built != null)
            {
                for (var i = 0; i < built.Length; i++)
                {
                    var entry = built[i];
                    if (entry == null)
                        continue;
                    byId[entry.BuildingId] = entry;
                }
            }

            var buildings = Object.FindObjectsByType<ProductionBuilding>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (var i = 0; i < buildings.Length; i++)
            {
                var building = buildings[i];
                if (byId.TryGetValue(building.BuildingId, out var setup))
                    building.ApplyPhaseLoadout(active: true, setup.Workers);
                else
                    building.ApplyPhaseLoadout(active: false, workers: 0);
            }
        }

        private void UnlockIds(int[] ids)
        {
            if (ids == null || ids.Length == 0)
                return;

            UnlockIdSet(new HashSet<int>(ids));
        }

        private static void UnlockIdSet(HashSet<int> unlock)
        {
            var buildings = Object.FindObjectsByType<ProductionBuilding>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (var i = 0; i < buildings.Length; i++)
            {
                var building = buildings[i];
                if (!unlock.Contains(building.BuildingId))
                    continue;

                building.TryUnlock();
            }
        }

        private void GrantUntilFull(ResourceDefinition definition)
        {
            var capacity = Mathf.Max(0, definition.TrayCapacity);
            for (var n = 0; n < capacity; n++)
            {
                if (!_inventory.TryAdd(definition))
                    break;
            }
        }

        private void GrantAmount(ResourceDefinition definition, int amount)
        {
            for (var n = 0; n < amount; n++)
            {
                if (!_inventory.TryAdd(definition))
                {
                    Debug.LogWarning(
                        $"[PhaseLoadout] Could not add more {definition.Id} (tray full?). Stopped this stack.");
                    break;
                }
            }
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

                GrantAmount(entry.Resource, entry.Count);
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
