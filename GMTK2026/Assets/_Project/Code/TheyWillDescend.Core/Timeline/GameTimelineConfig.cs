using System;
using TheyWillDescend.Core.Economy;
using UnityEngine;

namespace TheyWillDescend.Core.Timeline
{
    [Serializable]
    public sealed class PhaseOfferItem
    {
        [SerializeField] private ResourceDefinition resource;
        [SerializeField] private int count = 1;
        [Tooltip("Seconds added to the doomsday timer per accepted card of this type.")]
        [SerializeField] private float secondsReward = 10f;

        public ResourceDefinition Resource => resource;
        public int Count => Mathf.Max(0, count);
        public float SecondsReward => secondsReward;

        public string ResourceId => resource != null ? resource.Id : string.Empty;
    }

    [Serializable]
    public sealed class PhaseStartingCard
    {
        [SerializeField] private ResourceDefinition resource;
        [SerializeField] private int count = 1;

        public ResourceDefinition Resource => resource;
        public int Count => Mathf.Max(0, count);
    }

    [Serializable]
    public sealed class PhaseStartingBuilding
    {
        [Tooltip("Must match ProductionBuilding.BuildingId on the Game scene.")]
        [SerializeField] private int buildingId = 1;
        [SerializeField] private bool active = true;
        [SerializeField] private int workers;

        public int BuildingId => buildingId;
        public bool Active => active;
        public int Workers => Mathf.Max(0, workers);
    }

    [Serializable]
    public sealed class PhaseDefinition
    {
        [SerializeField] private string title = "Phase";
        [SerializeField] [TextArea] private string tooltip;
        [SerializeField] private float durationSeconds = 90f;
        [SerializeField] private Color color = Color.gray;
        [SerializeField] private PhaseOfferItem[] requirements = Array.Empty<PhaseOfferItem>();

        [Header("Construction unlock (every PhaseStarted, including normal advance)")]
        [Tooltip("BuildingIds that transition Locked → Buildable when this phase starts.")]
        [SerializeField] private int[] unlockBuildingIds = Array.Empty<int>();

        public string Title => string.IsNullOrEmpty(title) ? "Phase" : title;
        public string Tooltip => tooltip;
        public float DurationSeconds => Mathf.Max(0.1f, durationSeconds);
        public Color Color => color;
        public PhaseOfferItem[] Requirements => requirements ?? Array.Empty<PhaseOfferItem>();
        public int[] UnlockBuildingIds => unlockBuildingIds ?? Array.Empty<int>();

        public int TotalRequiredCards
        {
            get
            {
                var total = 0;
                var items = Requirements;
                for (var i = 0; i < items.Length; i++)
                    total += items[i].Count;
                return total;
            }
        }
    }

    [CreateAssetMenu(
        fileName = "GameTimelineConfig",
        menuName = "They Will Descend/Game Timeline Config")]
    public sealed class GameTimelineConfig : ScriptableObject
    {
        [SerializeField] private float baselineSeconds = 99f;
        [Tooltip("Applied to pyramid timer when a non-offer card is dropped on the pyramid (e.g. -1).")]
        [SerializeField] private float wrongOfferingTimerDelta = -1f;
        [SerializeField] private float yearsPerRealtimeSecond = 1f;

        [Header("Run start (StartRun only — not Cheat Panel jump)")]
        [SerializeField] private PhaseStartingCard[] runStartCards = Array.Empty<PhaseStartingCard>();
        [Tooltip("Empty = leave scene buildings as-is. Non-empty = listed Built, the rest Locked.")]
        [SerializeField] private PhaseStartingBuilding[] runStartBuildings = Array.Empty<PhaseStartingBuilding>();

        [SerializeField] private PhaseDefinition[] phases = Array.Empty<PhaseDefinition>();

        public float BaselineSeconds => Mathf.Max(0f, baselineSeconds);
        public float WrongOfferingTimerDelta => wrongOfferingTimerDelta;
        public float YearsPerRealtimeSecond => Mathf.Max(0f, yearsPerRealtimeSecond);
        public PhaseStartingCard[] RunStartCards => runStartCards ?? Array.Empty<PhaseStartingCard>();
        public PhaseStartingBuilding[] RunStartBuildings => runStartBuildings ?? Array.Empty<PhaseStartingBuilding>();
        public PhaseDefinition[] Phases => phases ?? Array.Empty<PhaseDefinition>();
        public int PhaseCount => Phases.Length;
    }
}
