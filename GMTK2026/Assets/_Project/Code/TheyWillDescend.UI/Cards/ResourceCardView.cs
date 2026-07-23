using System.Collections.Generic;
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
        [SerializeField] private UnityEngine.UI.Image iconImage;

        private RectTransform _rect;
        private Transform _homeParent;
        private Vector3 _homePosition;
        private Canvas _canvas;
        private bool _consumed;
        private CardKind _kind = CardKind.Resource;
        private readonly List<RaycastResult> _raycastHits = new();

        public string ResourceId => resourceId;
        public CardKind Kind => _kind;

        private bool IsVillager =>
            _kind == CardKind.Villager || resourceId == ResourceIds.Villager;

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
            _kind = id == ResourceIds.Villager ? CardKind.Villager : CardKind.Resource;
            _consumed = false;
            RefreshLabel();
        }

        public void Setup(CardDefinition definition)
        {
            if (definition == null)
                return;

            resourceId = definition.Id;
            _kind = definition.Kind;
            _consumed = false;

            if (titleLabel != null)
                titleLabel.text = definition.DisplayName;

            if (iconImage != null && definition.Icon != null)
                iconImage.sprite = definition.Icon;
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

            var accepted = false;
            if (building != null)
            {
                accepted = IsVillager
                    ? building.TryAcceptVillagerCard()
                    : building.TryAcceptResource(resourceId);
            }

            if (accepted)
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

        private ProductionBuilding ResolveBuildingUnderPointer(PointerEventData eventData)
        {
            _raycastHits.Clear();
            if (EventSystem.current != null)
                EventSystem.current.RaycastAll(eventData, _raycastHits);

            for (var i = 0; i < _raycastHits.Count; i++)
            {
                var go = _raycastHits[i].gameObject;
                var building = go.GetComponentInParent<ProductionBuilding>();
                if (building != null)
                    return building;

                var zone = go.GetComponentInParent<BuildingCardDropZone>();
                if (zone != null && zone.Building != null)
                    return zone.Building;
            }

            var fallback = eventData.pointerEnter;
            if (fallback == null && eventData.pointerCurrentRaycast.gameObject != null)
                fallback = eventData.pointerCurrentRaycast.gameObject;

            if (fallback == null)
                return null;

            return fallback.GetComponentInParent<ProductionBuilding>()
                   ?? fallback.GetComponentInParent<BuildingCardDropZone>()?.Building;
        }

        private void RefreshLabel()
        {
            if (titleLabel != null)
                titleLabel.text = resourceId;
        }
    }
}
