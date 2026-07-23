using UnityEngine;

namespace TheyWillDescend.Core.Economy
{
    [CreateAssetMenu(fileName = "BuildingRecipe", menuName = "They Will Descend/Building Recipe")]
    public sealed class BuildingRecipe : ScriptableObject
{
        [SerializeField] private string buildingName = "House";
        [Tooltip("Resources required for production (empty = passive).")]
        [SerializeField] private CardDefinition[] inputCards;
        [Tooltip("Amounts required per input card (must match inputCards length).")]
        [SerializeField] private int[] inputAmountsRequired;
        [SerializeField] private CardDefinition outputCard;
        [SerializeField] private float productionDurationSeconds = 3f;
        [SerializeField] private int workersRequired = 1;

        public string BuildingName => buildingName;
        public CardDefinition[] InputCards => inputCards ?? System.Array.Empty<CardDefinition>();
        public CardDefinition OutputCard => outputCard;
        public int[] InputAmounts => inputAmountsRequired ?? System.Array.Empty<int>();

        /// <summary>First input resource id (for backward compat with events).</summary>
        public string InputResourceId =>
            InputCards.Length > 0 && InputCards[0] != null ? InputCards[0].Id : "";

        public string OutputResourceId =>
            outputCard != null ? outputCard.Id : "";

        public int InputAmountRequired =>
            InputAmounts.Length > 0 ? Mathf.Max(0, InputAmounts[0]) : 0;

        public float ProductionDurationSeconds => Mathf.Max(0.01f, productionDurationSeconds);
        public int WorkersRequired => Mathf.Max(0, workersRequired);

        public bool RequiresInput => InputCards.Length > 0;
    }
}
