using System;
using UnityEngine;

namespace TheyWillDescend.Core.Dialogue
{
    [CreateAssetMenu(fileName = "DialogueDefinition", menuName = "They Will Descend/Dialogue Definition")]
    public sealed class DialogueDefinition : ScriptableObject
    {
        [SerializeField] private string id = "dialogue";
        [Tooltip("Used when a line has no portrait of its own.")]
        [SerializeField] private Sprite defaultPortrait;
        [SerializeField] private DialogueLine[] lines = Array.Empty<DialogueLine>();

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public Sprite DefaultPortrait => defaultPortrait;
        public DialogueLine[] Lines => lines ?? Array.Empty<DialogueLine>();
    }
}
