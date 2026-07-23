using UnityEngine;

namespace TheyWillDescend.Core.Economy
{
    [CreateAssetMenu(fileName = "BuildingRecipe", menuName = "They Will Descend/Building Recipe")]
    public sealed class BuildingRecipe : ScriptableObject
    {
        [SerializeField] private string buildingName = "House";
        [Tooltip("Null = no input (passive production with workers only).")]
        [SerializeField] private ResourceDefinition inputResource;
        [SerializeField] private ResourceDefinition outputResource;
        [Tooltip("0 = no input required.")]
        [SerializeField] private int inputAmountRequired;
        [SerializeField] private float productionDurationSeconds = 3f;
        [SerializeField] private int workersRequired = 1;

        public string BuildingName => buildingName;
        public ResourceDefinition InputResource => inputResource;
        public ResourceDefinition OutputResource => outputResource;
        public string InputResourceId => inputResource != null ? inputResource.Id : string.Empty;
        public string OutputResourceId => outputResource != null ? outputResource.Id : string.Empty;
        public int InputAmountRequired => Mathf.Max(0, inputAmountRequired);
        public float ProductionDurationSeconds => Mathf.Max(0.01f, productionDurationSeconds);
        public int WorkersRequired => Mathf.Max(0, workersRequired);

        public bool RequiresInput =>
            inputResource != null && InputAmountRequired > 0;
    }
}
