using TheyWillDescend.Core.Audio;
using TheyWillDescend.Core.Inventory;
using TheyWillDescend.UI.Cards;
using UnityEngine;

namespace TheyWillDescend.Main.GameAppStates
{
    /// <summary>
    /// Enter once after Game scope is ready: seed inventory + start BGM. No FSM — just Enter().
    /// </summary>
    public sealed class GameStartState
    {
        private const int StartingVillagerCount = 1;

        private readonly IInventory _inventory;
        private readonly InventoryTraysView _trays;
        private readonly IAudioManager _audio;

        public GameStartState(IInventory inventory, InventoryTraysView trays, IAudioManager audio)
        {
            _inventory = inventory;
            _trays = trays;
            _audio = audio;
        }

        public void Enter()
        {
            _inventory.Clear();

            var villager = _trays != null ? _trays.FindVillagerResource() : null;
            if (villager == null)
            {
                Debug.LogError(
                    "[GameStartState] No villager CardTrayView.Resource. " +
                    "Assign Resource_Villager on the villager tray.");
            }
            else
            {
                for (var i = 0; i < StartingVillagerCount; i++)
                    _inventory.TryAdd(villager);
            }

            _audio?.Play(AudioCatalog.Ids.MusicMain);
            Debug.Log("[GameStartState] Enter — starting inventory ready.");
        }
    }
}
