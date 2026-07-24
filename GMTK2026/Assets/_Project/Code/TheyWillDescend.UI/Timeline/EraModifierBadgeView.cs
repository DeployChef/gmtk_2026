using TheyWillDescend.Core.Timeline;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TheyWillDescend.UI.Timeline
{
    /// <summary>
    /// Resource-style icon on a timeline segment. Tint green/red by +/-; hover shows description.
    /// Wire on the modifier icon prefab (child of segment Modifiers row).
    /// </summary>
    public sealed class EraModifierBadgeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image icon;
        [SerializeField] private GameObject tooltipRoot;
        [SerializeField] private TMP_Text tooltipTitle;
        [SerializeField] private TMP_Text tooltipBody;
        [SerializeField] private Color buffColor = new(0.35f, 1f, 0.45f, 1f);
        [SerializeField] private Color debuffColor = new(1f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color neutralColor = Color.white;

        public void Bind(Image iconImage, GameObject tooltip, TMP_Text title, TMP_Text body)
        {
            icon = iconImage;
            tooltipRoot = tooltip;
            tooltipTitle = title;
            tooltipBody = body;
        }

        public void Setup(PhaseProductionModifier modifier)
        {
            HideTooltip();

            if (modifier == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            var sprite = modifier.ResolveIcon();
            if (icon != null)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
                icon.preserveAspect = true;
                icon.color = ResolveTint(modifier.SpeedPercent);
            }

            if (tooltipTitle != null)
                tooltipTitle.text = modifier.ResolveTitle();
            if (tooltipBody != null)
                tooltipBody.text = modifier.ResolveDescription();
        }

        public void OnPointerEnter(PointerEventData eventData) => ShowTooltip();

        public void OnPointerExit(PointerEventData eventData) => HideTooltip();

        private Color ResolveTint(float speedPercent)
        {
            if (speedPercent > 0.01f)
                return buffColor;
            if (speedPercent < -0.01f)
                return debuffColor;
            return neutralColor;
        }

        private void ShowTooltip()
        {
            if (tooltipRoot != null)
                tooltipRoot.SetActive(true);
        }

        private void HideTooltip()
        {
            if (tooltipRoot != null)
                tooltipRoot.SetActive(false);
        }

        private void OnDisable() => HideTooltip();
    }
}
