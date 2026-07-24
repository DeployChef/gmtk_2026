using TheyWillDescend.Core.Timeline;
using UnityEngine;
using VContainer;

namespace TheyWillDescend.Gameplay.Buildings
{
    /// <summary>
    /// World placeholder for the Great Pyramid. Accept path used by UI drop zone / card drag.
    /// </summary>
    public sealed class PyramidOfferingPoint : MonoBehaviour
    {
        private ITimelineService _timeline;

        [Inject]
        public void Construct(ITimelineService timeline)
        {
            _timeline = timeline;
        }

        public bool TryOffer(string resourceId)
        {
            if (_timeline == null)
            {
                Debug.LogWarning("[PyramidOfferingPoint] ITimelineService not injected.");
                return false;
            }

            return _timeline.TryOffer(resourceId);
        }
    }
}
