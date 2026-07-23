using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TheyWillDescend.Core.Bus;
using TheyWillDescend.Core.Bus.Events;
using System.Collections.Generic;
using TheyWillDescend.Core.Economy;
using TheyWillDescend.Gameplay.Buildings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace TheyWillDescend.UI.Buildings
{
    /// <summary>
    /// World-space HUD bound to a <see cref="ProductionBuilding"/>.
    /// </summary>
    public sealed class BuildingWorldHud : MonoBehaviour
    {
        [SerializeField] private ProductionBuilding building;
        [SerializeField] private Button addWorkerButton;
        [SerializeField] private Button removeWorkerButton;
        [SerializeField] private TMP_Text workersLabel;
        [SerializeField] private Transform inputContainer;
        [SerializeField] private Image outputIcon;
        [SerializeField] private GameObject inputIconPrefab;
        [SerializeField] private Slider progressSlider;
        [Header("Produce feedback")]
        [SerializeField] private TMP_Text producedPopup;
        [SerializeField] private float popupDuration = 0.65f;
        [SerializeField] private float popupRisePixels = 36f;

        private IDisposable _producedSub;
        private CancellationTokenSource _popupCts;
        private Vector2 _popupRestAnchoredPos;
        private bool _popupRestCaptured;

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
            if (building == null)
                return;

            if (progressSlider != null)
            {
                var show = building.IsProducing;
                if (progressSlider.gameObject.activeSelf != show)
                    progressSlider.gameObject.SetActive(show);

                if (show)
                    progressSlider.value = 1f - building.NormalizedProgress;
            }

            RefreshWorkerButtons();
        }

        private void OnResourceProduced(ResourceProducedEvent e)
        {
            if (building == null || e.BuildingId != building.BuildingId)
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

            var recipe = building.Recipe;

            if (workersLabel != null)
                workersLabel.text = $"{building.Workers}/{building.MaxWorkers}";

            if (inputContainer != null)
            {
                // Очистить старые иконки
                for (var i = inputContainer.childCount - 1; i >= 0; i--)
                    Destroy(inputContainer.GetChild(i).gameObject);

                // Спавнить иконки для каждого входа
                if (recipe != null && recipe.RequiresInput)
                {
                    var inputs = recipe.InputResources;
                    for (var i = 0; i < inputs.Length; i++)
                    {
                        var resource = inputs[i];
                        if (resource == null || resource.Icon == null)
                            continue;

                        var iconGo = inputIconPrefab != null
                            ? Instantiate(inputIconPrefab, inputContainer)
                            : CreateDefaultIcon(inputContainer);

                        var img = iconGo.GetComponent<Image>();
                        if (img != null)
                            img.sprite = resource.Icon;
                    }
                }
            }
            
            if (outputIcon != null)
            {
                var outputDef = recipe != null ? recipe.OutputResource : null;
                if (outputDef != null && outputDef.Icon != null)
                {
                    outputIcon.sprite = outputDef.Icon;
                    outputIcon.enabled = true;
                }
                else
                {
                    outputIcon.enabled = false;
                }
            }

            if (progressSlider != null)
            {
                var show = building.IsProducing;
                progressSlider.gameObject.SetActive(show);
                if (show)
                    progressSlider.value = 1f - building.NormalizedProgress;
            }

            RefreshWorkerButtons();
        }

        private void RefreshWorkerButtons()
        {
            if (addWorkerButton != null)
                addWorkerButton.interactable = building.CanHireWorker;

            if (removeWorkerButton != null)
                removeWorkerButton.interactable = building.Workers > building.MinWorkers;
        }

        private static GameObject CreateDefaultIcon(Transform parent)
        {
            var go = new GameObject("InputIcon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(80, 80);
            var img = go.GetComponent<Image>();
            img.preserveAspect = true;
            return go;
        }
    }
}
