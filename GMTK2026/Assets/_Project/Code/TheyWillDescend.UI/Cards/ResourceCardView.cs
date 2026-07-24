using System.Collections.Generic;
using TheyWillDescend.Core.Audio;
using TheyWillDescend.Core.Economy;
using TheyWillDescend.Gameplay.Buildings;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;

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
        private ResourceKind _kind = ResourceKind.Resource;
        private IAudioManager _audio;
        private readonly List<RaycastResult> _raycastHits = new();

        public string ResourceId => resourceId;
        public ResourceKind Kind => _kind;

        private bool IsVillager =>
            _kind == ResourceKind.Villager || resourceId == ResourceIds.Villager;

        [Inject]
        public void Construct(IAudioManager audio)
        {
            _audio = audio;
        }

        public void BindAudio(IAudioManager audio)
        {
            _audio = audio;
        }

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
            _kind = id == ResourceIds.Villager ? ResourceKind.Villager : ResourceKind.Resource;
            _consumed = false;
            RefreshLabel();
        }

        public void Setup(ResourceDefinition definition)
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
            _audio?.Play(AudioCatalog.Ids.CardPickup);
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

            var pyramid = ResolvePyramidUnderPointer(eventData);
            var building = pyramid == null ? ResolveBuildingUnderPointer(eventData) : null;
            canvasGroup.blocksRaycasts = true;

            var accepted = false;
            if (pyramid != null)
            {
                accepted = pyramid.TryOffer(resourceId);
            }
            else if (building != null)
            {
                if (IsVillager)
                {
                    if (!building.IsBuilt)
                    {
                        accepted = false;
                    }
                    else
                    {
                        var workersSatisfied = building.Definition != null
                            && building.Workers >= building.Definition.WorkersRequired;

                        accepted = workersSatisfied
                            ? building.TryAcceptResource(ResourceIds.Villager)
                              || building.TryAcceptVillagerCard()
                            : building.TryAcceptVillagerCard()
                              || building.TryAcceptResource(ResourceIds.Villager);
                    }
                }
                else
                {
                    accepted = building.TryAcceptResource(resourceId);
                }
            }

            if (accepted)
            {
                _consumed = true;
                _audio?.Play(AudioCatalog.Ids.CardDropOk);
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

        private PyramidOfferingPoint ResolvePyramidUnderPointer(PointerEventData eventData)
        {
            _raycastHits.Clear();
            if (EventSystem.current != null)
                EventSystem.current.RaycastAll(eventData, _raycastHits);

            for (var i = 0; i < _raycastHits.Count; i++)
            {
                var go = _raycastHits[i].gameObject;
                var pyramid = go.GetComponentInParent<PyramidOfferingPoint>();
                if (pyramid != null)
                    return pyramid;

                var zone = go.GetComponentInParent<PyramidCardDropZone>();
                if (zone != null && zone.Pyramid != null)
                    return zone.Pyramid;
            }

            var fallback = eventData.pointerEnter;
            if (fallback == null && eventData.pointerCurrentRaycast.gameObject != null)
                fallback = eventData.pointerCurrentRaycast.gameObject;

            if (fallback == null)
                return null;

            return fallback.GetComponentInParent<PyramidOfferingPoint>()
                   ?? fallback.GetComponentInParent<PyramidCardDropZone>()?.Pyramid;
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
