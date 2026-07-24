using System.Collections.Generic;
using TheyWillDescend.Core.Buildings;
using TheyWillDescend.Core.Economy;
using TheyWillDescend.Gameplay.Buildings;
using TheyWillDescend.UI.Timeline;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheyWillDescend.UI.Buildings
{
    /// <summary>
    /// World-space construction HUD: build-cost icons + timer. No workers / output.
    /// Visible in <see cref="BuildingSlotState.Buildable"/> and <see cref="BuildingSlotState.Constructing"/>.
    /// </summary>
    public sealed class BuildingConstructionHud : MonoBehaviour
    {
        [SerializeField] private ProductionBuilding building;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Transform costContainer;
        [SerializeField] private GameObject costIconPrefab;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Image progressFill;

        private readonly List<PyramidOfferIconView> _slots = new();
        private bool _iconsBuilt;

        private void Awake()
        {
            if (building == null)
                building = GetComponentInParent<ProductionBuilding>();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
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

        private void LateUpdate()
        {
            if (building == null || !IsConstructionVisible(building.SlotState))
                return;

            RefreshProgress();
            RefreshCounts();
        }

        private void Refresh()
        {
            if (building == null)
                return;

            var state = building.SlotState;
            var visible = IsConstructionVisible(state);
            SetHudVisible(visible);

            if (!visible)
                return;

            if (!_iconsBuilt)
                RebuildCostIcons();

            if (costContainer != null)
                costContainer.gameObject.SetActive(state == BuildingSlotState.Buildable);

            RefreshProgress();
            RefreshCounts();
        }

        private void RebuildCostIcons()
        {
            if (costContainer == null)
                return;

            for (var i = costContainer.childCount - 1; i >= 0; i--)
                Destroy(costContainer.GetChild(i).gameObject);
            _slots.Clear();

            var definition = building.Definition;
            if (definition == null)
            {
                _iconsBuilt = true;
                return;
            }

            var costs = definition.BuildCost;
            for (var i = 0; i < costs.Length; i++)
            {
                var item = costs[i];
                if (item?.Resource == null || item.Count <= 0)
                    continue;

                SpawnSlot(item.Resource, building.GetStoredAmount(item.ResourceId), item.Count);
            }

            _iconsBuilt = true;
        }

        private void SpawnSlot(ResourceDefinition resource, int stored, int required)
        {
            var go = costIconPrefab != null
                ? Instantiate(costIconPrefab, costContainer)
                : CreateDefaultIcon(costContainer);

            go.SetActive(true);

            var view = go.GetComponent<PyramidOfferIconView>();
            if (view == null)
            {
                view = go.AddComponent<PyramidOfferIconView>();
                view.Bind(
                    go.GetComponentInChildren<Image>(),
                    go.GetComponentInChildren<TMP_Text>());
            }

            view.Setup(resource, stored, required);
            _slots.Add(view);
        }

        private void RefreshCounts()
        {
            if (building == null || building.SlotState != BuildingSlotState.Buildable || _slots.Count == 0)
                return;

            var definition = building.Definition;
            if (definition == null)
                return;

            var costs = definition.BuildCost;
            var slot = 0;
            for (var i = 0; i < costs.Length && slot < _slots.Count; i++)
            {
                var item = costs[i];
                if (item?.Resource == null || item.Count <= 0)
                    continue;

                _slots[slot].SetCount(building.GetStoredAmount(item.ResourceId), item.Count);
                slot++;
            }
        }

        private void RefreshProgress()
        {
            var show = building != null && building.IsConstructing;
            var progress = show ? building.NormalizedProgress : 0f;

            if (progressSlider != null)
            {
                if (progressSlider.gameObject.activeSelf != show)
                    progressSlider.gameObject.SetActive(show);
                if (show)
                    progressSlider.value = 1f - progress;
            }

            if (progressFill != null)
            {
                if (progressFill.gameObject.activeSelf != show)
                    progressFill.gameObject.SetActive(show);
                if (show)
                    progressFill.fillAmount = progress;
            }
        }

        private void SetHudVisible(bool visible)
        {
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        private static bool IsConstructionVisible(BuildingSlotState state) =>
            state == BuildingSlotState.Buildable || state == BuildingSlotState.Constructing;

        private static GameObject CreateDefaultIcon(Transform parent)
        {
            var go = new GameObject("CostIcon", typeof(RectTransform), typeof(Image));
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
