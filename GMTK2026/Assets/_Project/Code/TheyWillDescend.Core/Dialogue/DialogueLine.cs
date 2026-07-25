using System;
using UnityEngine;

namespace TheyWillDescend.Core.Dialogue
{
    [Serializable]
    public sealed class DialogueLine
    {
        [SerializeField] [TextArea(2, 6)] private string text;
        [Tooltip("Optional. Leave empty to keep the previous portrait.")]
        [SerializeField] private Sprite portrait;

        public string Text => text ?? string.Empty;
        public Sprite Portrait => portrait;
    }
}
