using System;
using UnityEngine;

namespace TheyWillDescend.Core.Dialogue
{
    /// <summary>
    /// Maps timeline phase index → short era intro dialogue.
    /// Index 0 is usually empty (covered by the opening sequence).
    /// </summary>
    [CreateAssetMenu(fileName = "PhaseDialogueCatalog", menuName = "They Will Descend/Phase Dialogue Catalog")]
    public sealed class PhaseDialogueCatalog : ScriptableObject
    {
        [Tooltip("Index = phase index. Null entries are skipped.")]
        [SerializeField] private DialogueDefinition[] byPhaseIndex = Array.Empty<DialogueDefinition>();

        public bool TryGet(int phaseIndex, out DialogueDefinition dialogue)
        {
            dialogue = null;
            if (phaseIndex < 0 || byPhaseIndex == null || phaseIndex >= byPhaseIndex.Length)
                return false;

            dialogue = byPhaseIndex[phaseIndex];
            return dialogue != null && dialogue.Lines.Length > 0;
        }
    }
}
