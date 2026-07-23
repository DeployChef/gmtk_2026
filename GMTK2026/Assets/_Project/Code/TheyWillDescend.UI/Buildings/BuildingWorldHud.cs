using System.Collections.Generic;
using TheyWillDescend.Core.Economy;
using TheyWillDescend.Gameplay.Buildings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

            // Rail card count can change without building.StateChanged (other buildings).
            RefreshWorkerButtons();
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
                    var inputs = recipe.InputCards;
                    for (var i = 0; i < inputs.Length; i++)
                    {
                        var card = inputs[i];
                        if (card == null || card.Icon == null)
                            continue;

                        var iconGo = inputIconPrefab != null
                            ? Instantiate(inputIconPrefab, inputContainer)
                            : CreateDefaultIcon(inputContainer);

                        var img = iconGo.GetComponent<Image>();
                        if (img != null)
                            img.sprite = card.Icon;
                    }
                }
            }
            
            if (outputIcon != null)
            {
                var outputDef = recipe != null ? recipe.OutputCard : null;
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
