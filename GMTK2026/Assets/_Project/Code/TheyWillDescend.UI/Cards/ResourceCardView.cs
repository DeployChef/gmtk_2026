using TheyWillDescend.Core.Economy;
using TheyWillDescend.Gameplay.Buildings;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TheyWillDescend.UI.Cards
{
    /// <summary>
    /// Draggable resource card (for CardPrefab). Needs Image raycast enabled + CanvasGroup.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ResourceCardView : MonoBehaviour,
        IResourceCard,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [SerializeField] private string resourceId = ResourceIds.Id1;
        [SerializeField] private TMPro.TMP_Text titleLabel;
        [SerializeField] private CanvasGroup canvasGroup;

        private RectTransform _rect;
        private Transform _homeParent;
        private Vector3 _homePosition;
        private Canvas _canvas;
        private bool _consumed;

        public string ResourceId => resourceId;

        private void Awake()
        {
            _rect = transform as RectTransform;
            _canvas = GetComponentInParent<Canvas>();
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            RefreshLabel();
        }

        public void Setup(string id)
        {
            resourceId = id;
            _consumed = false;
            RefreshLabel();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_consumed)
                return;

            _homeParent = transform.parent;
            _homePosition = transform.position;

            if (_canvas != null)
                transform.SetParent(_canvas.transform, true);

            canvasGroup.blocksRaycasts = false;
            transform.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_consumed || _rect == null)
                return;

            if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                var cam = eventData.pressEventCamera != null
                    ? eventData.pressEventCamera
                    : _canvas.worldCamera;
                if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                        _rect.parent as RectTransform,
                        eventData.position,
                        cam,
                        out var world))
                    _rect.position = world;
            }
            else
            {
                _rect.position = eventData.position;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_consumed)
                return;

            var building = ResolveBuildingUnderPointer(eventData);
            canvasGroup.blocksRaycasts = true;

            if (building != null && building.TryAcceptResource(resourceId))
            {
                _consumed = true;
                Destroy(gameObject);
                return;
            }

            ReturnHome();
        }

        private void ReturnHome()
        {
            if (_homeParent != null)
                transform.SetParent(_homeParent, true);

            transform.position = _homePosition;
        }

        private static ProductionBuilding ResolveBuildingUnderPointer(PointerEventData eventData)
        {
            var go = eventData.pointerEnter;
            if (go == null && eventData.pointerCurrentRaycast.gameObject != null)
                go = eventData.pointerCurrentRaycast.gameObject;

            if (go == null)
                return null;

            var building = go.GetComponentInParent<ProductionBuilding>();
            if (building != null)
                return building;

            return go.GetComponentInParent<BuildingCardDropZone>()?.Building;
        }

        private void RefreshLabel()
        {
            if (titleLabel != null)
                titleLabel.text = resourceId;
        }
    }
}
