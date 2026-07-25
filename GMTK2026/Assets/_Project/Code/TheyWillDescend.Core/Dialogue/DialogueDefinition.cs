using System;
using UnityEngine;

namespace TheyWillDescend.Core.Dialogue
{
    [CreateAssetMenu(fileName = "DialogueDefinition", menuName = "They Will Descend/Dialogue Definition")]
    public sealed class DialogueDefinition : ScriptableObject
    {
        [SerializeField] private string id = "dialogue";
        [SerializeField] private DialogueLine[] lines = Array.Empty<DialogueLine>();

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public DialogueLine[] Lines => lines ?? Array.Empty<DialogueLine>();
    }
}
