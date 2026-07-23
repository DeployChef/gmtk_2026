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
        [SerializeField] private TMP_Text inputLabel;
        [SerializeField] private TMP_Text outputLabel;
        [SerializeField] private Slider progressSlider;

        private void Awake()
        {
            if (building == null)
                building = GetComponentInParent<ProductionBuilding>();

            if (addWorkerButton != null)
                addWorkerButton.onClick.AddListener(() => building.TryAddWorker());

            if (removeWorkerButton != null)
                removeWorkerButton.onClick.AddListener(() => building.TryRemoveWorker());
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
            if (progressSlider == null || building == null)
                return;

            var show = building.IsProducing;
            if (progressSlider.gameObject.activeSelf != show)
                progressSlider.gameObject.SetActive(show);

            if (show)
                progressSlider.value = 1f - building.NormalizedProgress;
        }

        private void Refresh()
        {
            if (building == null)
                return;

            var recipe = building.Recipe;

            if (workersLabel != null)
                workersLabel.text = $"{building.Workers}/{building.MaxWorkers}";

            if (inputLabel != null)
            {
                if (recipe != null && recipe.RequiresInput)
                    inputLabel.text = $"In: {recipe.InputResourceId}";
                else
                    inputLabel.text = "In: —";
            }

            if (outputLabel != null)
                outputLabel.text = recipe != null ? $"Out: {recipe.OutputResourceId}" : "Out: —";

            if (progressSlider != null)
            {
                var show = building.IsProducing;
                progressSlider.gameObject.SetActive(show);
                if (show)
                    progressSlider.value = 1f - building.NormalizedProgress;
            }

            if (addWorkerButton != null)
                addWorkerButton.interactable = building.Workers < building.MaxWorkers;

            if (removeWorkerButton != null)
                removeWorkerButton.interactable = building.Workers > building.MinWorkers;
        }
    }
}
