using TheyWillDescend.Gameplay.Buildings;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TheyWillDescend.UI.Cards
{
    /// <summary>
    /// Drop zone on building World Space UI. Put on raycast Image over/near the house.
    /// </summary>
    public sealed class BuildingCardDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private ProductionBuilding building;

        public ProductionBuilding Building => building;

        public void OnDrop(PointerEventData eventData)
        {
            // Accept handled in ResourceCardView.OnEndDrag via raycast hit.
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Optional highlight later.
        }

        public void OnPointerExit(PointerEventData eventData)
        {
        }

        private void OnValidate()
        {
            if (building == null)
                building = GetComponentInParent<ProductionBuilding>();
        }
    }
}
