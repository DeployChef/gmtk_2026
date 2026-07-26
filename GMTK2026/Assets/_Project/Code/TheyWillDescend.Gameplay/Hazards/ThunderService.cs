using TheyWillDescend.Core.Audio;
using TheyWillDescend.Core.Hazards;
using TheyWillDescend.Gameplay.Buildings;
using UnityEngine;

namespace TheyWillDescend.Gameplay.Hazards
{
    /// <summary>
    /// Plain DI: thunder SFX + temporary building disable (workers stay assigned, resume after fire).
    /// </summary>
    public sealed class ThunderService : IThunderService
    {
        private readonly IAudioManager _audio;

        public ThunderService(IAudioManager audio)
        {
            _audio = audio;
        }

        public void PlayThunderSfx()
        {
            _audio?.Play(AudioCatalog.Ids.Thunder);
        }

        public void ApplyStrike(GameObject houseRoot, float disableDuration)
        {
            PlayThunderSfx();

            if (houseRoot == null)
                return;

            var building = houseRoot.GetComponentInChildren<ProductionBuilding>();
            if (building == null)
                return;

            // Workers stay in the building; production resumes when disable ends.
            building.DisableTemporarily(disableDuration);
        }
    }
}
