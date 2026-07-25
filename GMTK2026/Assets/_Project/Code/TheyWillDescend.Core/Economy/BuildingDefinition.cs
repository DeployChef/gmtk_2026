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
    /// One hire step: dump these resources on Home, then production timer yields a Villager.
    /// </summary>
    [Serializable]
    public sealed class HireOfferStep
    {
        [SerializeField] private BuildCostItem[] cost = Array.Empty<BuildCostItem>();

        public BuildCostItem[] Cost => cost ?? Array.Empty<BuildCostItem>();
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

        [Header("Hire (Home)")]
        [Tooltip(
            "Escalating offers for each next villager produced by this building. " +
            "Empty = use Production inputs / passive. Last step repeats.")]
        [SerializeField] private HireOfferStep[] hireOffers = Array.Empty<HireOfferStep>();

        [Header("Production")]
        [Tooltip("Resources required for production (empty = passive). Ignored when Hire Offers are set.")]
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

        public HireOfferStep[] HireOffers => hireOffers ?? Array.Empty<HireOfferStep>();
        public bool HasHireOffers => HireOffers.Length > 0;

        /// <summary>
        /// Cost for the next villager after <paramref name="villagersProduced"/> this run.
        /// Clamps to the last step (repeats forever).
        /// </summary>
        public BuildCostItem[] GetHireOfferCost(int villagersProduced)
        {
            var offers = HireOffers;
            if (offers.Length == 0)
                return Array.Empty<BuildCostItem>();

            var index = Mathf.Clamp(villagersProduced, 0, offers.Length - 1);
            var step = offers[index];
            return step != null ? step.Cost : Array.Empty<BuildCostItem>();
        }

        public bool HireOfferHasCost(int villagersProduced)
        {
            var costs = GetHireOfferCost(villagersProduced);
            for (var i = 0; i < costs.Length; i++)
            {
                if (costs[i]?.Resource != null && costs[i].Count > 0)
                    return true;
            }

            return false;
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

        public bool RequiresInput => !HasHireOffers && InputResources.Length > 0;
    }
}
