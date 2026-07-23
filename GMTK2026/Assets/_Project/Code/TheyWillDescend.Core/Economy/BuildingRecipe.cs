using UnityEngine;

namespace TheyWillDescend.Core.Economy
{
    [CreateAssetMenu(fileName = "BuildingRecipe", menuName = "They Will Descend/Building Recipe")]
    public sealed class BuildingRecipe : ScriptableObject
{
        [SerializeField] private string buildingName = "House";
        [Tooltip("Null = no input (passive production with workers only).")]
        [SerializeField] private CardDefinition inputCard;
        [SerializeField] private CardDefinition outputCard;
        [Tooltip("0 = no input required.")]
        [SerializeField] private int inputAmountRequired;
        [SerializeField] private float productionDurationSeconds = 3f;
        [SerializeField] private int workersRequired = 1;

        public string BuildingName => buildingName;
        public CardDefinition InputCard => inputCard;
        public CardDefinition OutputCard => outputCard;
        public string InputResourceId => inputCard != null ? inputCard.Id : "";
        public string OutputResourceId => outputCard != null ? outputCard.Id : "";
        public int InputAmountRequired => Mathf.Max(0, inputAmountRequired);
        public float ProductionDurationSeconds => Mathf.Max(0.01f, productionDurationSeconds);
        public int WorkersRequired => Mathf.Max(0, workersRequired);

        public bool RequiresInput =>
            inputCard != null && InputAmountRequired > 0;
    }
}
