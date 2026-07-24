using TheyWillDescend.Gameplay.Buildings;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TheyWillDescend.UI.Cards
{
    /// <summary>
    /// Drop zone on pyramid World Space UI. Put on a raycast Image over/near the pyramid.
    /// </summary>
    public sealed class PyramidCardDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private PyramidOfferingPoint pyramid;

        public PyramidOfferingPoint Pyramid => pyramid;

        public void OnDrop(PointerEventData eventData)
        {
            // Accept handled in ResourceCardView.OnEndDrag via raycast hit.
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
        }

        public void OnPointerExit(PointerEventData eventData)
        {
        }

        private void OnValidate()
        {
            if (pyramid == null)
                pyramid = GetComponentInParent<PyramidOfferingPoint>();
        }
    }
}
