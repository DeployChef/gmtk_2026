using UnityEngine;

namespace TheyWillDescend.Core.Hazards
{
    /// <summary>
    /// Strike effects: thunder SFX + building disable. VFX stays in DisasterManager.
    /// </summary>
    public interface IThunderService
    {
        void ApplyStrike(GameObject houseRoot, float disableDuration);
    }
}
