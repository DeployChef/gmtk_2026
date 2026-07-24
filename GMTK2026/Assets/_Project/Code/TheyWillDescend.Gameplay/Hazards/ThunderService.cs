using TheyWillDescend.Core.Audio;
using TheyWillDescend.Core.Hazards;
using TheyWillDescend.Gameplay.Buildings;
using UnityEngine;

namespace TheyWillDescend.Gameplay.Hazards
{
    /// <summary>
    /// Plain DI: thunder SFX + temporary building disable.
    /// </summary>
    public sealed class ThunderService : IThunderService
    {
        private readonly IAudioManager _audio;

        public ThunderService(IAudioManager audio)
        {
            _audio = audio;
        }

        public void ApplyStrike(GameObject houseRoot, float disableDuration)
        {
            _audio?.Play(AudioCatalog.Ids.Thunder);

            if (houseRoot == null)
                return;

            var building = houseRoot.GetComponentInChildren<ProductionBuilding>();
            if (building != null)
                building.DisableTemporarily(disableDuration);
        }
    }
}
