using System.Threading;
using Cysharp.Threading.Tasks;

namespace TheyWillDescend.Core.Hazards
{
    public interface IDisasterManager
    {
        bool TryStrikeRandomHouse();

        /// <summary>
        /// VFX + thunder only on every Built house, staggered. No disable / worker kill.
        /// Completes after the last strike is triggered (not when VFX finishes).
        /// </summary>
        UniTask PlayCinematicStrikesAsync(float staggerSeconds, CancellationToken cancellationToken = default);
    }
}
