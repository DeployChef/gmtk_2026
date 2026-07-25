using UnityEngine;

namespace TheyWillDescend.Core.Hazards
{
    /// <summary>
    /// Strike effects: thunder SFX + building disable + kill one worker. VFX stays in DisasterManager.
    /// </summary>
    public interface IThunderService
    {
        void PlayThunderSfx();

        void ApplyStrike(GameObject houseRoot, float disableDuration);
    }
}
