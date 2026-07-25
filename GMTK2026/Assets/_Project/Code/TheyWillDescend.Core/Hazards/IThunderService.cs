using UnityEngine;

namespace TheyWillDescend.Core.Hazards
{
    /// <summary>
    /// Strike effects: thunder SFX + temporary building disable. Workers stay assigned. VFX in DisasterManager.
    /// </summary>
    public interface IThunderService
    {
        void PlayThunderSfx();

        void ApplyStrike(GameObject houseRoot, float disableDuration);
    }
}
