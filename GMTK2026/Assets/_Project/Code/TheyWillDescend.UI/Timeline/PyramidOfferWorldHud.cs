using TheyWillDescend.Core.Timeline;
using TMPro;
using UnityEngine;
using VContainer;

namespace TheyWillDescend.UI.Timeline
{
    /// <summary>
    /// World-space text of the current pyramid offer (resource × remaining).
    /// </summary>
    public sealed class PyramidOfferWorldHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text offerLabel;

        private ITimelineService _timeline;

        [Inject]
        public void Construct(ITimelineService timeline)
        {
            _timeline = timeline;
            Refresh();
        }

        private void LateUpdate() => Refresh();

        private void Refresh()
        {
            if (offerLabel == null || _timeline == null)
                return;

            var phase = _timeline.CurrentPhase;
            if (phase == null)
            {
                offerLabel.text = string.Empty;
                return;
            }

            var reqs = phase.Requirements;
            if (reqs.Length == 0)
            {
                offerLabel.text = $"{phase.Title}\n(no offer)";
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.Append(phase.Title);
            for (var i = 0; i < reqs.Length; i++)
            {
                var item = reqs[i];
                var name = item.Resource != null ? item.Resource.DisplayName : item.ResourceId;
                var delivered = _timeline.GetOfferDelivered(i);
                var required = item.Count;
                sb.Append('\n').Append(name).Append(' ').Append(delivered).Append('/').Append(required);
                sb.Append(" (+").Append(item.SecondsReward.ToString("0.#")).Append("s)");
            }

            if (_timeline.IsCurrentOfferComplete)
                sb.Append("\nOK — wait phase end");

            offerLabel.text = sb.ToString();
        }
    }
}
