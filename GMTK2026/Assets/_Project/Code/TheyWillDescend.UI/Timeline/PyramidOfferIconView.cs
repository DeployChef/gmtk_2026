using TheyWillDescend.Core.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheyWillDescend.UI.Timeline
{
    /// <summary>
    /// One offer requirement slot: resource icon + delivered/required (like building input icons + count).
    /// </summary>
    public sealed class PyramidOfferIconView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text countLabel;

        public void Bind(Image iconImage, TMP_Text label)
        {
            icon = iconImage;
            countLabel = label;
        }

        public void Setup(ResourceDefinition resource, int delivered, int required)
        {
            if (icon != null)
            {
                if (resource != null && resource.Icon != null)
                {
                    icon.sprite = resource.Icon;
                    icon.enabled = true;
                }
                else
                {
                    icon.enabled = false;
                }
            }

            SetCount(delivered, required);
        }

        public void SetCount(int delivered, int required)
        {
            if (countLabel != null)
                countLabel.text = $"{delivered}/{required}";
        }
    }
}
