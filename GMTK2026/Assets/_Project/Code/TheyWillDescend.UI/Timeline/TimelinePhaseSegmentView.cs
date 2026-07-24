using TheyWillDescend.Core.Timeline;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheyWillDescend.UI.Timeline
{
    /// <summary>
    /// One phase strip in the TopBar row. Stretch layout so N segments look like one slider.
    /// </summary>
    public sealed class TimelinePhaseSegmentView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Image fill;
        [SerializeField] private TMP_Text label;

        public void Setup(PhaseDefinition phase, int index)
        {
            if (phase == null)
            {
                if (label != null)
                    label.text = (index + 1).ToString();
                return;
            }

            if (background != null)
                background.color = phase.Color;

            if (fill != null)
            {
                var c = phase.Color;
                fill.color = new Color(
                    Mathf.Clamp01(c.r * 0.65f + 0.2f),
                    Mathf.Clamp01(c.g * 0.65f + 0.2f),
                    Mathf.Clamp01(c.b * 0.65f + 0.2f),
                    1f);
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            }

            if (label != null)
                label.text = string.IsNullOrEmpty(phase.Title) ? (index + 1).ToString() : phase.Title;
        }

        public void SetFill(float normalized)
        {
            if (fill == null)
                return;

            fill.fillAmount = Mathf.Clamp01(normalized);
        }
    }
}
