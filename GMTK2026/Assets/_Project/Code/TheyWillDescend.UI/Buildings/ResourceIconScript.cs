using TheyWillDescend.Core.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheyWillDescend.UI
{
    /// <summary>
    /// Resource icon with count label: shows delivered/required for construction or input.
    /// </summary>
    public class ResourceIconScript : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text countLabel;
        [SerializeField] private Color incompleteColor = Color.white;
        [SerializeField] private Color completeColor = new Color(0.35f, 0.9f, 0.4f, 1f);

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

            if (icon != null)
                icon.color = required > 0 && delivered >= required ? completeColor : incompleteColor;
        }
    }
}
