using System;
using UnityEngine;

namespace TheyWillDescend.Core.Economy
{
    [Serializable]
    public sealed class BuildCostItem
    {
        [SerializeField] private ResourceDefinition resource;
        [SerializeField] private int count = 1;

        public ResourceDefinition Resource => resource;
        public int Count => Mathf.Max(0, count);
        public string ResourceId => resource != null ? resource.Id : string.Empty;
    }

    /// <summary>
    /// Building definition: build offer cost + production recipe (GDD BuildingDefinition).
    /// </summary>
    [CreateAssetMenu(
        fileName = "BuildingDefinition",
        menuName = "They Will Descend/Building Definition")]
    public sealed class BuildingDefinition : ScriptableObject
    {
        [SerializeField] private string buildingName = "House";

        [Header("Construction")]
        [Tooltip("Resources to dump on the ruin before the build timer starts. Empty = unlock starts timer (or Built if duration ≈ 0).")]
        [SerializeField] private BuildCostItem[] buildCost = Array.Empty<BuildCostItem>();
        [SerializeField] private float buildDurationSeconds = 3f;

        [Header("Production")]
        [Tooltip("Resources required for production (empty = passive).")]
        [SerializeField] private ResourceDefinition[] inputResources;
        [Tooltip("Amounts required per input Resource (must match inputResources length).")]
        [SerializeField] private int[] inputAmountsRequired;
        [SerializeField] private ResourceDefinition outputResource;
        [SerializeField] private float productionDurationSeconds = 3f;
        [SerializeField] private int workersRequired = 1;

        public string BuildingName => buildingName;

        public BuildCostItem[] BuildCost => buildCost ?? Array.Empty<BuildCostItem>();
        public float BuildDurationSeconds => Mathf.Max(0f, buildDurationSeconds);
        public bool HasBuildCost
        {
            get
            {
                var costs = BuildCost;
                for (var i = 0; i < costs.Length; i++)
                {
                    if (costs[i]?.Resource != null && costs[i].Count > 0)
                        return true;
                }

                return false;
            }
        }

        public ResourceDefinition[] InputResources => inputResources ?? Array.Empty<ResourceDefinition>();
        public ResourceDefinition OutputResource => outputResource;
        public int[] InputAmounts => inputAmountsRequired ?? Array.Empty<int>();

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
