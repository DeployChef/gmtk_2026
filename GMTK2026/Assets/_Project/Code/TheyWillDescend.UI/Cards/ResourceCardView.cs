using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TheyWillDescend.Core.Audio;
using TheyWillDescend.Core.Bus;
using TheyWillDescend.Core.Bus.Events;
using TheyWillDescend.Core.Economy;
using TheyWillDescend.Gameplay.Buildings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
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
        IEndDragHandler,
        IPointerDownHandler,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [SerializeField] private string resourceId = ResourceIds.Id1;
        [SerializeField] private TMPro.TMP_Text titleLabel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private UnityEngine.UI.Image iconImage;
        [SerializeField] private Image outlineImage;
        [SerializeField] private Color outlineColor = Color.yellow;
        [SerializeField] private Color dragGlowColorA = new Color(1f, 0.9f, 0.3f, 1f);
        [SerializeField] private Color dragGlowColorB = new Color(1f, 0.4f, 0.1f, 1f);
        [SerializeField] private float dragGlowColorCycle = 0.6f;
        [SerializeField] private float outlineFadeInDuration = 0.15f;
        [SerializeField] private float outlineFadeOutDuration = 0.25f;
        [SerializeField] private float outlinePulseMinAlpha = 0.5f;
        [SerializeField] private float outlinePulseDuration = 0.8f;
        [SerializeField] private string dissolveProperty = "_DissolveAmount";
        [SerializeField] private float dissolveDuration = 0.5f;
        [SerializeField] private Vector3 shrinkScale = new Vector3(0.5f, 0.5f, 1f);
        [SerializeField] private float returnHomeDuration = 0.25f;
        [Tooltip("Card transparency when hovering over a building during drag (0=invisible, 1=opaque).")]
        [SerializeField] private float hoverBuildingAlpha = 0.5f;
        [Header("Click punch")]
        [SerializeField] private Vector3 clickPunch = new Vector3(0.2f, 0.2f, 0f);
        [SerializeField] private float clickPunchDuration = 0.28f;
        [SerializeField] private int clickPunchVibrato = 8;
        [SerializeField] [Range(0f, 1f)] private float clickPunchElasticity = 0.55f;

        private RectTransform _rect;
        private Transform _homeParent;
        private CardTrayView _homeTray;
        private Canvas _canvas;
        private bool _consumed;
        private bool _dragging;
        private ProductionBuilding _hoverBuilding;
        private ResourceKind _kind = ResourceKind.Resource;
        private IAudioManager _audio;
        private IGameEventBus _bus;
        private readonly List<RaycastResult> _raycastHits = new();
        private readonly List<Material> _dissolveInstances = new();
        private Tween _outlineTween;
        private Tween _pulseTween;
        private Tween _colorTween;
        private Tween _punchTween;
        private Vector3 _baseScale = Vector3.one;
        private float _outlineAlpha;

        public string ResourceId => resourceId;
        public ResourceKind Kind => _kind;

        private bool IsVillager =>
            _kind == ResourceKind.Villager || resourceId == ResourceIds.Villager;

        [Inject]
        public void Construct(IAudioManager audio, IGameEventBus bus)
        {
            _audio = audio;
            _bus = bus;
        }

        public void BindAudio(IAudioManager audio)
        {
            _audio = audio;
        }

        public void BindBus(IGameEventBus bus)
        {
            _bus = bus;
        }

        private void Awake()
        {
            _rect = transform as RectTransform;
            _canvas = GetComponentInParent<Canvas>();
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            _baseScale = transform.localScale;

            if (outlineImage != null)
            {
                var c = outlineColor;
                c.a = 0f;
                outlineImage.color = c;
            }

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

            _homeTray = GetComponentInParent<CardTrayView>();
            _homeParent = transform.parent;

            if (_canvas != null)
                transform.SetParent(_canvas.transform, true);

            _homeTray?.NotifyCardDetached();

            canvasGroup.blocksRaycasts = false;
            transform.SetAsLastSibling();
            _dragging = true;
            _audio?.Play(AudioCatalog.Ids.CardPickup);
            _bus?.Publish(new CardDragStartedEvent(resourceId));
            ShowOutline(true, true);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_consumed)
                return;

            PlayClickPunch();
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

            UpdateHoverVfx(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_consumed)
                return;

            _dragging = false;
            ClearHoverVfx();
            ResetScale();
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
                    else if (building.CanAcceptWorkerCard)
                    {
                        // Worker assignment only — never also treat as production input in the same drop.
                        accepted = building.TryAcceptVillagerCard();
                    }
                    else
                    {
                        // Workers full / not needed: allow Villager as production input (e.g. Altar).
                        accepted = building.TryAcceptResource(ResourceIds.Villager);
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
                canvasGroup.blocksRaycasts = false;
                ResetScale();
                _audio?.Play(AudioCatalog.Ids.CardDropOk);

                if (building != null)
                    _bus?.Publish(new CardDroppedOnBuildingEvent(building.BuildingId, resourceId));

                // Instantly hide outline before dissolve
                _outlineTween?.Kill();
                _pulseTween?.Kill();
                if (outlineImage != null)
                {
                    var c = outlineImage.color;
                    c.a = 0f;
                    outlineImage.color = c;
                }

                PlayDissolveAndDestroy();
                return;
            }

            _audio?.Play(AudioCatalog.Ids.CardDropReject);
            ReturnHome();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_consumed)
                return;

            if (!_dragging)
                _audio?.Play(AudioCatalog.Ids.CardHover);

            ShowOutline(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_consumed && !_dragging)
                ShowOutline(false);
        }

        private void ReturnHome()
        {
            ShowOutline(false);
            _dragging = false;
            ResetScale();
            canvasGroup.blocksRaycasts = true;

            if (_homeTray != null)
            {
                _homeTray.ReturnCardUnderStack(transform, returnHomeDuration);
                return;
            }

            if (_homeParent != null)
                transform.SetParent(_homeParent, true);
        }

        private void PlayClickPunch()
        {
            _punchTween?.Kill();
            transform.localScale = _baseScale;

            if (clickPunch.sqrMagnitude <= 0.0001f || clickPunchDuration <= 0.01f)
                return;

            _punchTween = DOTween
                .Punch(
                    () => transform.localScale,
                    v => transform.localScale = v,
                    clickPunch,
                    clickPunchDuration,
                    Mathf.Max(1, clickPunchVibrato),
                    clickPunchElasticity)
                .SetUpdate(true)
                .SetTarget(this)
                .OnKill(() =>
                {
                    if (this != null)
                        transform.localScale = _baseScale;
                });
        }

        private void ResetScale()
        {
            _punchTween?.Kill();
            _punchTween = null;
            transform.localScale = _baseScale;
        }

        private void UpdateHoverVfx(PointerEventData eventData)
        {
            var building = ResolveBuildingUnderPointer(eventData);
            if (building != null && !building.CanAcceptDrop(resourceId))
                building = null;

            if (building == _hoverBuilding)
                return;

            ClearHoverVfx();

            if (building != null)
            {
                _hoverBuilding = building;
                _hoverBuilding.ShowDropIndicator(true);
                _audio?.Play(AudioCatalog.Ids.CardOverBuilding);
                if (canvasGroup != null)
                    canvasGroup.alpha = hoverBuildingAlpha;
            }
        }

        private void ClearHoverVfx()
        {
            if (_hoverBuilding != null)
            {
                _hoverBuilding.ShowDropIndicator(false);
                _hoverBuilding = null;
                if (canvasGroup != null)
                    canvasGroup.alpha = 1f;
            }
        }

        private void ShowOutline(bool show, bool isDragging = false)
        {
            if (outlineImage == null)
                return;

            _outlineTween?.Kill();
            _pulseTween?.Kill();
            _colorTween?.Kill();

            if (show)
            {
                // Set initial color + alpha
                var baseColor = isDragging ? dragGlowColorA : outlineColor;
                _outlineAlpha = 0f;
                baseColor.a = _outlineAlpha;
                outlineImage.color = baseColor;

                // Fade in alpha
                _outlineTween = DOTween.To(
                    () => _outlineAlpha,
                    val =>
                    {
                        _outlineAlpha = val;
                        var c = outlineImage.color;
                        c.a = val;
                        outlineImage.color = c;
                    },
                    1f,
                    outlineFadeInDuration)
                    .SetEase(Ease.OutCubic)
                    .OnComplete(() =>
                    {
                        // Alpha pulsation 100% <-> 50%
                        _pulseTween = DOTween.To(
                            () => _outlineAlpha,
                            val =>
                            {
                                _outlineAlpha = val;
                                var c = outlineImage.color;
                                c.a = val;
                                outlineImage.color = c;
                            },
                            outlinePulseMinAlpha,
                            outlinePulseDuration * 0.5f)
                            .SetLoops(-1, LoopType.Yoyo)
                            .SetEase(Ease.InOutSine);

                        // Color cycling between A and B (only when dragging)
                        if (isDragging)
                        {
                            _colorTween = DOTween.To(
                                () => 0f,
                                t =>
                                {
                                    var c = Color.Lerp(dragGlowColorA, dragGlowColorB, t);
                                    c.a = _outlineAlpha;
                                    outlineImage.color = c;
                                },
                                1f,
                                dragGlowColorCycle)
                                .SetLoops(-1, LoopType.Yoyo)
                                .SetEase(Ease.InOutSine);
                        }
                    });
            }
            else
            {
                // Fade out
                _outlineTween = DOTween.To(
                    () => _outlineAlpha,
                    val =>
                    {
                        _outlineAlpha = val;
                        var c = outlineImage.color;
                        c.a = val;
                        outlineImage.color = c;
                    },
                    0f,
                    outlineFadeOutDuration)
                    .SetEase(Ease.OutQuad);
            }
        }

        private void PlayDissolveAndDestroy()
        {
            _audio?.Play(AudioCatalog.Ids.CardRemove);

            var images = GetComponentsInChildren<Image>();
            var hasDissolve = false;

            foreach (var img in images)
            {
                if (img.material == null || !img.material.HasProperty(dissolveProperty))
                    continue;

                var instance = new Material(img.material);
                instance.SetFloat(dissolveProperty, 0f);
                img.material = instance;
                _dissolveInstances.Add(instance);
                hasDissolve = true;
            }

            if (!hasDissolve)
            {
                Destroy(gameObject);
                return;
            }

            DissolveAsync().Forget();
        }

        private async UniTaskVoid DissolveAsync()
        {
            var startScale = transform.localScale;
            var elapsed = 0f;
            while (elapsed < dissolveDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / dissolveDuration);
                foreach (var mat in _dissolveInstances)
                    mat.SetFloat(dissolveProperty, t);
                transform.localScale = Vector3.Lerp(startScale, shrinkScale, t);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            Destroy(gameObject);
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

        private void OnDestroy()
        {
            _outlineTween?.Kill();
            _pulseTween?.Kill();
            _colorTween?.Kill();
        }

        private void RefreshLabel()
        {
            if (titleLabel != null)
                titleLabel.text = resourceId;
        }
    }
}
