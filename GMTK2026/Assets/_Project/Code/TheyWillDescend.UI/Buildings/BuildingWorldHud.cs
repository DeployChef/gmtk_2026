using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TheyWillDescend.Core.Buildings;
using TheyWillDescend.Core.Bus;
using TheyWillDescend.Core.Bus.Events;
using TheyWillDescend.Core.Economy;
using TheyWillDescend.Gameplay.Buildings;
using TheyWillDescend.UI.Timeline;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace TheyWillDescend.UI.Buildings
{
    /// <summary>
    /// World-space production HUD. Visible only when the slot is <see cref="BuildingSlotState.Built"/>.
    /// Construction uses <see cref="BuildingConstructionHud"/>.
    /// </summary>
    public sealed class BuildingWorldHud : MonoBehaviour
    {
        [SerializeField] private ProductionBuilding building;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button addWorkerButton;
        [SerializeField] private Button removeWorkerButton;
        [SerializeField] private TMP_Text workersLabel;
        [SerializeField] private Transform inputContainer;
        [SerializeField] private Image outputIcon;
        [SerializeField] private GameObject inputIconPrefab;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Image progressFill;
        [Header("Produce feedback")]
        [SerializeField] private TMP_Text producedPopup;
        [SerializeField] private float popupDuration = 0.65f;
        [SerializeField] private float popupRisePixels = 36f;

        private IDisposable _producedSub;
        private CancellationTokenSource _popupCts;
        private Vector2 _popupRestAnchoredPos;
        private bool _popupRestCaptured;
        private readonly List<PyramidOfferIconView> _inputSlots = new();
        private bool _iconsBuilt;
        private int _shownHireStep = -1;
        private bool _showingHireOffer;

        [Inject]
        public void Construct(IGameEventBus bus)
        {
            _producedSub?.Dispose();
            _producedSub = bus.Subscribe<ResourceProducedEvent>(OnResourceProduced);
        }

        private void Awake()
        {
            if (building == null)
                building = GetComponentInParent<ProductionBuilding>();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (addWorkerButton != null)
                addWorkerButton.onClick.AddListener(() =>
                {
                    building.TryAddWorker();
                    Refresh();
                });

            if (removeWorkerButton != null)
                removeWorkerButton.onClick.AddListener(() =>
                {
                    building.TryRemoveWorker();
                    Refresh();
                });

            if (producedPopup != null)
            {
                CapturePopupRest();
                producedPopup.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (building != null)
                building.StateChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (building != null)
                building.StateChanged -= Refresh;
        }

        private void OnDestroy()
        {
            _producedSub?.Dispose();
            CancelPopup();
        }

        private void LateUpdate()
        {
            if (building == null || !building.IsBuilt)
                return;

            var showProgress = building.IsProducing;
            if (progressSlider != null)
            {
                if (progressSlider.gameObject.activeSelf != showProgress)
                    progressSlider.gameObject.SetActive(showProgress);

                if (showProgress)
                    progressSlider.value = 1f - building.NormalizedProgress;
            }

            if (progressFill != null)
            {
                if (progressFill.gameObject.activeSelf != showProgress)
                    progressFill.gameObject.SetActive(showProgress);

                if (showProgress)
                    progressFill.fillAmount = building.NormalizedProgress;
            }

            if (inputContainer != null && building.UsesHireOffers)
            {
                var showOffer = !showProgress;
                if (inputContainer.gameObject.activeSelf != showOffer)
                    inputContainer.gameObject.SetActive(showOffer);
            }

            RefreshInputCounts();
            RefreshWorkerButtons();
        }

        private void OnResourceProduced(ResourceProducedEvent e)
        {
            if (building == null || !building.IsBuilt || e.BuildingId != building.BuildingId)
                return;

            PlayProducedPopup().Forget();
        }

        private async UniTaskVoid PlayProducedPopup()
        {
            if (producedPopup == null)
                return;

            CancelPopup();
            _popupCts = new CancellationTokenSource();
            var ct = _popupCts.Token;

            CapturePopupRest();
            producedPopup.text = "+1";
            producedPopup.gameObject.SetActive(true);

            var color = producedPopup.color;
            color.a = 1f;
            producedPopup.color = color;

            var rect = producedPopup.rectTransform;
            rect.anchoredPosition = _popupRestAnchoredPos;

            var duration = Mathf.Max(0.05f, popupDuration);
            var elapsed = 0f;
            try
            {
                while (elapsed < duration)
                {
                    ct.ThrowIfCancellationRequested();
                    elapsed += Time.unscaledDeltaTime;
                    var t = Mathf.Clamp01(elapsed / duration);
                    rect.anchoredPosition = _popupRestAnchoredPos + Vector2.up * (popupRisePixels * t);
                    color.a = 1f - t;
                    producedPopup.color = color;
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            producedPopup.gameObject.SetActive(false);
            rect.anchoredPosition = _popupRestAnchoredPos;
            color.a = 1f;
            producedPopup.color = color;
        }

        private void CapturePopupRest()
        {
            if (producedPopup == null || _popupRestCaptured)
                return;

            _popupRestAnchoredPos = producedPopup.rectTransform.anchoredPosition;
            _popupRestCaptured = true;
        }

        private void CancelPopup()
        {
            if (_popupCts == null)
                return;

            _popupCts.Cancel();
            _popupCts.Dispose();
            _popupCts = null;
        }

        private void Refresh()
        {
            if (building == null)
                return;

            var visible = building.IsBuilt;
            SetHudVisible(visible);
            if (!visible)
                return;

            var definition = building.Definition;
            var showWorkers = definition != null
                              && !building.UsesHireOffers
                              && definition.WorkersRequired > 0;

            if (workersLabel != null)
            {
                workersLabel.gameObject.SetActive(showWorkers);
                if (showWorkers)
                    workersLabel.text = $"{building.Workers}/{building.MaxWorkers}";
            }

            if (addWorkerButton != null)
                addWorkerButton.gameObject.SetActive(showWorkers);
            if (removeWorkerButton != null)
                removeWorkerButton.gameObject.SetActive(showWorkers);

            if (outputIcon != null)
            {
                var outputDef = definition != null ? definition.OutputResource : null;
                if (outputDef != null && outputDef.Icon != null)
                {
                    outputIcon.sprite = outputDef.Icon;
                    outputIcon.enabled = true;
                    outputIcon.gameObject.SetActive(true);
                }
                else
                {
                    outputIcon.enabled = false;
                    outputIcon.gameObject.SetActive(false);
                }
            }

            EnsureInputIcons();

            if (inputContainer != null && building.UsesHireOffers)
                inputContainer.gameObject.SetActive(!building.IsProducing);

            // Force layout rebuild after visibility change / icon spawn
            // to prevent overlapping (HorizontalLayoutGroup + ContentSizeFitter).
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);

            if (progressSlider != null)
            {
                var show = building.IsProducing;
                progressSlider.gameObject.SetActive(show);
                if (show)
                    progressSlider.value = 1f - building.NormalizedProgress;
            }

            RefreshInputCounts();
            RefreshWorkerButtons();
        }

        private void EnsureInputIcons()
        {
            if (building.UsesHireOffers)
            {
                if (!_iconsBuilt || !_showingHireOffer || _shownHireStep != building.VillagersProduced)
                    RebuildInputIcons();
                return;
            }

            if (!_iconsBuilt || _showingHireOffer)
                RebuildInputIcons();
        }

        private void SetHudVisible(bool visible)
        {
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        private void RebuildInputIcons()
        {
            if (inputContainer == null)
                return;

            for (var i = inputContainer.childCount - 1; i >= 0; i--)
                Destroy(inputContainer.GetChild(i).gameObject);
            _inputSlots.Clear();

            if (building.UsesHireOffers)
            {
                var costs = building.CurrentHireOfferCost;
                for (var i = 0; i < costs.Length; i++)
                {
                    var item = costs[i];
                    if (item?.Resource == null || item.Count <= 0)
                        continue;
                    SpawnInputSlot(item.Resource, building.GetStoredAmount(item.ResourceId), item.Count);
                }

                _showingHireOffer = true;
                _shownHireStep = building.VillagersProduced;
                _iconsBuilt = true;
                return;
            }

            _showingHireOffer = false;
            _shownHireStep = -1;

            var definition = building.Definition;
            if (definition != null && definition.RequiresInput)
            {
                var inputs = definition.InputResources;
                var amounts = definition.InputAmounts;
                for (var i = 0; i < inputs.Length; i++)
                {
                    var resource = inputs[i];
                    if (resource == null)
                        continue;
                    var required = i < amounts.Length ? amounts[i] : 1;
                    if (required <= 0)
                        continue;
                    SpawnInputSlot(resource, building.GetStoredAmount(resource.Id), required);
                }
            }

            _iconsBuilt = true;
        }

        private void SpawnInputSlot(ResourceDefinition resource, int stored, int required)
        {
            var iconGo = inputIconPrefab != null
                ? Instantiate(inputIconPrefab, inputContainer)
                : CreateDefaultIcon(inputContainer);

            iconGo.SetActive(true);

            var view = iconGo.GetComponent<PyramidOfferIconView>();
            if (view == null)
            {
                view = iconGo.AddComponent<PyramidOfferIconView>();
                view.Bind(
                    iconGo.GetComponentInChildren<Image>(),
                    iconGo.GetComponentInChildren<TMP_Text>());
            }

            view.Setup(resource, stored, required);
            _inputSlots.Add(view);
        }

        private void RefreshInputCounts()
        {
            if (building == null || !building.IsBuilt || _inputSlots.Count == 0)
                return;

            if (building.UsesHireOffers)
            {
                var costs = building.CurrentHireOfferCost;
                var idx = 0;
                for (var i = 0; i < costs.Length && idx < _inputSlots.Count; i++)
                {
                    var item = costs[i];
                    if (item?.Resource == null || item.Count <= 0)
                        continue;
                    _inputSlots[idx].SetCount(building.GetStoredAmount(item.ResourceId), item.Count);
                    idx++;
                }

                return;
            }

            var definition = building.Definition;
            if (definition == null || !definition.RequiresInput)
                return;

            var inputs = definition.InputResources;
            var amounts = definition.InputAmounts;
            var slot = 0;
            for (var i = 0; i < inputs.Length && slot < _inputSlots.Count; i++)
            {
                var resource = inputs[i];
                if (resource == null)
                    continue;
                var required = i < amounts.Length ? amounts[i] : 1;
                if (required <= 0)
                    continue;
                _inputSlots[slot].SetCount(building.GetStoredAmount(resource.Id), required);
                slot++;
            }
        }

        private void RefreshWorkerButtons()
        {
            if (addWorkerButton != null)
                addWorkerButton.interactable = building != null && building.CanHireWorker;

            if (removeWorkerButton != null)
                removeWorkerButton.interactable =
                    building != null && building.Workers > building.MinWorkers;
        }

        private static GameObject CreateDefaultIcon(Transform parent)
        {
            var go = new GameObject("InputIcon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(80, 80);
            var img = go.GetComponent<Image>();
            img.preserveAspect = true;

            var labelGo = new GameObject("Count", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = (RectTransform)labelGo.transform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0.35f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 22f;
            tmp.text = "0/0";

            var view = go.AddComponent<PyramidOfferIconView>();
            view.Bind(img, tmp);
            return go;
        }
    }
}
