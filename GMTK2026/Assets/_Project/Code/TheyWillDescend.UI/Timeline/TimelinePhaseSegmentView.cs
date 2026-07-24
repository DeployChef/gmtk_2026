using System.Collections.Generic;
using TheyWillDescend.Core.Timeline;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheyWillDescend.UI.Timeline
{
    /// <summary>
    /// One phase strip in the TopBar row. Stretch layout so N segments look like one slider.
    /// Optional modifiers row (above fill): resource icons tinted green/red, revealed when era starts.
    /// </summary>
    public sealed class TimelinePhaseSegmentView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Image fill;
        [SerializeField] private TMP_Text label;

        [Header("Era modifiers (above progress)")]
        [Tooltip("Empty = no modifier UI. Parent for spawned/pooled icon instances.")]
        [SerializeField] private Transform modifiersRoot;
        [Tooltip("Prefab with EraModifierBadgeView (+ Image). Instantiated per modifier.")]
        [SerializeField] private EraModifierBadgeView modifierIconPrefab;

        private readonly List<EraModifierBadgeView> _modifierIcons = new();
        private bool _hasModifiers;

        public void Setup(PhaseDefinition phase, int index)
        {
            if (phase == null)
            {
                if (label != null)
                    label.text = (index + 1).ToString();
                ClearModifiers();
                SetModifiersRevealed(false);
                return;
            }

            if (background != null)
                background.color = phase.Color;

            if (fill != null)
            {
                var c = phase.Color;
                fill.color = new Color(
                    Mathf.Clamp01(c.r * 0.65f + 0.2f),
                    Mathf.Clamp01(c.g * 0.65f + 0.2f),
                    Mathf.Clamp01(c.b * 0.65f + 0.2f),
                    1f);
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            }

            if (label != null)
                label.text = string.IsNullOrEmpty(phase.Title) ? (index + 1).ToString() : phase.Title;

            BuildModifiers(phase);
            // Hidden until TimelineHudView reveals this era (and later ones stay hidden).
            SetModifiersRevealed(false);
        }

        public void SetFill(float normalized)
        {
            if (fill == null)
                return;

            fill.fillAmount = Mathf.Clamp01(normalized);
        }

        /// <summary>
        /// Show modifier icons only after this era has started. No-op / hide if phase has none.
        /// </summary>
        public void SetModifiersRevealed(bool revealed)
        {
            if (modifiersRoot == null)
                return;

            var show = revealed && _hasModifiers;
            if (modifiersRoot.gameObject.activeSelf != show)
                modifiersRoot.gameObject.SetActive(show);

            if (!show)
            {
                for (var i = 0; i < _modifierIcons.Count; i++)
                {
                    if (_modifierIcons[i] != null)
                        _modifierIcons[i].gameObject.SetActive(false);
                }

                return;
            }

            for (var i = 0; i < _modifierIcons.Count; i++)
            {
                if (_modifierIcons[i] != null)
                    _modifierIcons[i].gameObject.SetActive(true);
            }
        }

        private void BuildModifiers(PhaseDefinition phase)
        {
            ClearModifiers();

            if (modifiersRoot == null || modifierIconPrefab == null || phase == null)
            {
                _hasModifiers = false;
                return;
            }

            var mods = phase.ProductionModifiers;
            var count = 0;
            for (var i = 0; i < mods.Length; i++)
            {
                if (mods[i] != null)
                    count++;
            }

            _hasModifiers = count > 0;
            if (!_hasModifiers)
            {
                modifiersRoot.gameObject.SetActive(false);
                return;
            }

            for (var i = 0; i < mods.Length; i++)
            {
                var mod = mods[i];
                if (mod == null)
                    continue;

                var view = Instantiate(modifierIconPrefab, modifiersRoot);
                view.gameObject.SetActive(true);
                view.Setup(mod);
                _modifierIcons.Add(view);
            }
        }

        private void ClearModifiers()
        {
            for (var i = 0; i < _modifierIcons.Count; i++)
            {
                if (_modifierIcons[i] != null)
                    Destroy(_modifierIcons[i].gameObject);
            }

            _modifierIcons.Clear();
            _hasModifiers = false;
        }
    }
}
