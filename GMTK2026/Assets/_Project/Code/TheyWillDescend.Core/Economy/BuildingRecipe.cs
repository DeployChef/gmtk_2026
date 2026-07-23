using UnityEngine;

namespace TheyWillDescend.Core.Economy
{
    [CreateAssetMenu(fileName = "BuildingRecipe", menuName = "They Will Descend/Building Recipe")]
    public sealed class BuildingRecipe : ScriptableObject
{
        [SerializeField] private string buildingName = "House";
        [Tooltip("Resources required for production (empty = passive).")]
        [SerializeField] private ResourceDefinition[] inputResources;
        [Tooltip("Amounts required per input Resource (must match inputResources length).")]
        [SerializeField] private int[] inputAmountsRequired;
        [SerializeField] private ResourceDefinition outputResource;
        [SerializeField] private float productionDurationSeconds = 3f;
        [SerializeField] private int workersRequired = 1;

        public string BuildingName => buildingName;
        public ResourceDefinition[] InputResources => inputResources ?? System.Array.Empty<ResourceDefinition>();
        public ResourceDefinition OutputResource => outputResource;
        public int[] InputAmounts => inputAmountsRequired ?? System.Array.Empty<int>();

        /// <summary>First input resource id (for backward compat with events).</summary>
        public string InputResourceId =>
            InputResources.Length > 0 && InputResources[0] != null ? InputResources[0].Id : "";

        public string OutputResourceId =>
            outputResource != null ? outputResource.Id : "";

        public int InputAmountRequired =>
            InputAmounts.Length > 0 ? Mathf.Max(0, InputAmounts[0]) : 0;

        public float ProductionDurationSeconds => Mathf.Max(0.01f, productionDurationSeconds);
        public int WorkersRequired => Mathf.Max(0, workersRequired);

        public bool RequiresInput => InputResources.Length > 0;
    }
}
