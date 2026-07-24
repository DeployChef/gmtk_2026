using System;
using TheyWillDescend.Core.Economy;
using UnityEngine;

namespace TheyWillDescend.Core.Timeline
{
    public enum PhaseModifierTarget
    {
        AllOutputs = 0,
        Resource = 1,
        BuildingId = 2,
    }

    /// <summary>
    /// Era production speed tweak. −20 = 20% slower (EffectiveSec = Base / 0.8).
    /// </summary>
    [Serializable]
    public sealed class PhaseProductionModifier
    {
        [SerializeField] private PhaseModifierTarget target = PhaseModifierTarget.AllOutputs;
        [SerializeField] private ResourceDefinition resource;
        [SerializeField] private int buildingId;
        [Tooltip("Speed delta in percent. −20 = slower, +10 = faster.")]
        [SerializeField] private float speedPercent;
        [SerializeField] private string displayTitle;
        [SerializeField] [TextArea] private string description;
        [Tooltip("Optional override icon. If empty, uses Resource.Icon (also for AllOutputs display).")]
        [SerializeField] private Sprite icon;

        public PhaseModifierTarget Target => target;
        public ResourceDefinition Resource => resource;
        public int BuildingId => buildingId;
        public float SpeedPercent => speedPercent;
        public Sprite IconOverride => icon;

        /// <summary>1 + speedPercent/100, clamped.</summary>
        public float SpeedMultiplier => Mathf.Clamp(1f + speedPercent / 100f, 0.05f, 5f);

        public Sprite ResolveIcon()
        {
            if (icon != null)
                return icon;
            // Resource ref is also used as display icon for AllOutputs / BuildingId.
            if (resource != null)
                return resource.Icon;
            return null;
        }

        public string ResolveTitle()
        {
            if (!string.IsNullOrEmpty(displayTitle))
                return displayTitle;

            var sign = speedPercent > 0f ? "+" : string.Empty;
            var pct = $"{sign}{speedPercent:0.#}%";
            return target switch
            {
                PhaseModifierTarget.Resource when resource != null =>
                    $"{pct} {resource.DisplayName}",
                PhaseModifierTarget.BuildingId =>
                    $"{pct} building #{buildingId}",
                _ => $"{pct} all production",
            };
        }

        public string ResolveDescription()
        {
            if (!string.IsNullOrEmpty(description))
                return description;

            var slower = speedPercent < 0f;
            var abs = Mathf.Abs(speedPercent);
            var verb = slower ? "slower" : "faster";
            return target switch
            {
                PhaseModifierTarget.Resource when resource != null =>
                    $"{resource.DisplayName} production is {abs:0.#}% {verb} this era.",
                PhaseModifierTarget.BuildingId =>
                    $"Building #{buildingId} produces {abs:0.#}% {verb} this era.",
                _ =>
                    $"All production is {abs:0.#}% {verb} this era.",
            };
        }

        public bool AppliesTo(int targetBuildingId, string outputResourceId)
        {
            return target switch
            {
                PhaseModifierTarget.AllOutputs => true,
                PhaseModifierTarget.Resource =>
                    resource != null
                    && !string.IsNullOrEmpty(outputResourceId)
                    && string.Equals(resource.Id, outputResourceId, StringComparison.Ordinal),
                PhaseModifierTarget.BuildingId => buildingId == targetBuildingId,
                _ => false,
            };
        }
    }
}
