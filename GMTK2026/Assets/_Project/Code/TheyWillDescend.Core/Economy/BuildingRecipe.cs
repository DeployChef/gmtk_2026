using UnityEngine;

namespace TheyWillDescend.Core.Economy
{
    [CreateAssetMenu(fileName = "BuildingRecipe", menuName = "They Will Descend/Building Recipe")]
    public sealed class BuildingRecipe : ScriptableObject
    {
        [SerializeField] private string buildingName = "House";
        [Tooltip("Empty = no input (passive production with workers only).")]
        [SerializeField] private string inputResourceId = "";
        [SerializeField] private string outputResourceId = ResourceIds.Id1;
        [Tooltip("0 = no input required.")]
        [SerializeField] private int inputAmountRequired;
        [SerializeField] private float productionDurationSeconds = 3f;
        [SerializeField] private int workersRequired = 1;

        public string BuildingName => buildingName;
        public string InputResourceId => inputResourceId;
        public string OutputResourceId => outputResourceId;
        public int InputAmountRequired => Mathf.Max(0, inputAmountRequired);
        public float ProductionDurationSeconds => Mathf.Max(0.01f, productionDurationSeconds);
        public int WorkersRequired => Mathf.Max(0, workersRequired);

        public bool RequiresInput =>
            !string.IsNullOrEmpty(inputResourceId) && InputAmountRequired > 0;
    }
}
