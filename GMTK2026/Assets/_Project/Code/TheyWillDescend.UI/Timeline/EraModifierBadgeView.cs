using System.Collections.Generic;
using TheyWillDescend.Core.Timeline;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TheyWillDescend.UI.Timeline
{
    /// <summary>
    /// Same pattern as building inputs / pyramid offer: container + icon prefab.
    /// Visible only when the era has started AND the phase has modifiers.
    /// Uses CanvasGroup (does not disable this GameObject) so Setup always works.
    /// </summary>
    public sealed class EraModifierBadgeView : MonoBehaviour
    {
        [SerializeField] private Transform iconsContainer;
        [SerializeField] private GameObject iconPrefab;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Color buffIconColor = new(0.35f, 1f, 0.45f, 1f);
        [SerializeField] private Color debuffIconColor = new(1f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color neutralIconColor = Color.white;
        [SerializeField] private string outlineChildName = "Outline";

        [Header("Tooltip (optional)")]
        [SerializeField] private GameObject tooltipRoot;
        [SerializeField] private TMP_Text tooltipTitle;
        [SerializeField] private TMP_Text tooltipBody;

        private readonly List<GameObject> _icons = new();
        private bool _hasModifiers;
        private bool _eraStarted;

        public bool HasModifiers => _hasModifiers;

        private void Awake()
        {
            if (iconsContainer == null)
                iconsContainer = transform;

            EnsureCanvasGroup();
            _hasModifiers = false;
            _eraStarted = false;
            ApplyVisibility();
        }

        /// <summary>
        /// Rebuild icons from phase data. Does not show the badge — call <see cref="SetEraStarted"/>.
        /// </summary>
        public void Setup(PhaseDefinition phase)
        {
            EnsureCanvasGroup();
            ClearIcons();
            HideTooltip();

            if (phase == null || iconsContainer == null)
            {
                _hasModifiers = false;
                ApplyVisibility();
                return;
            }

            var mods = phase.ProductionModifiers;
            for (var i = 0; i < mods.Length; i++)
            {
                var mod = mods[i];
                if (mod == null)
                    continue;

                var go = iconPrefab != null
                    ? Instantiate(iconPrefab, iconsContainer)
                    : CreateDefaultIcon(iconsContainer);

                go.SetActive(true);

                var img = go.GetComponentInChildren<Image>();
                var sprite = mod.ResolveIcon();
                if (img == null || sprite == null)
                {
                    Destroy(go);
                    continue;
                }

                img.sprite = sprite;
                img.enabled = true;
                img.preserveAspect = true;
                img.color = Color.white;
                img.raycastTarget = true;

                // Tint the outline frame (buff=green, debuff=red)
                var outlineTr = img.transform.Find(outlineChildName);
                if (outlineTr != null)
                {
                    var outlineImg = outlineTr.GetComponent<Image>();
                    if (outlineImg != null)
                        outlineImg.color = ResolveTint(mod.SpeedPercent);
                }

                var hover = go.GetComponent<ModifierIconHover>();
                if (hover == null)
                    hover = go.AddComponent<ModifierIconHover>();
                hover.Bind(this, mod.ResolveTitle(), mod.ResolveDescription());

                _icons.Add(go);
            }

            _hasModifiers = _icons.Count > 0;
            ApplyVisibility();
        }

        /// <summary>
        /// True when this era has been reached (current or past). Badge shows only if also has modifiers.
        /// </summary>
        public void SetEraStarted(bool eraStarted)
        {
            _eraStarted = eraStarted;
            ApplyVisibility();
        }

        public void SetRevealed(bool revealed) => SetEraStarted(revealed);

        private void EnsureCanvasGroup()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private void ApplyVisibility()
        {
            EnsureCanvasGroup();

            var show = _eraStarted && _hasModifiers;
            canvasGroup.alpha = show ? 1f : 0f;
            canvasGroup.interactable = show;
            canvasGroup.blocksRaycasts = show;

            if (!show)
            {
                HideTooltip();
                return;
            }

            if (iconsContainer is RectTransform rt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }

        internal void ShowTooltip(string title, string body)
        {
            if (tooltipRoot == null || !_eraStarted || !_hasModifiers)
                return;

            if (tooltipTitle != null)
                tooltipTitle.text = title;
            if (tooltipBody != null)
                tooltipBody.text = body;
            tooltipRoot.SetActive(true);
        }

        internal void HideTooltip()
        {
            if (tooltipRoot != null)
                tooltipRoot.SetActive(false);
        }

        private Color ResolveTint(float speedPercent)
        {
            if (speedPercent > 0.01f)
                return buffIconColor;
            if (speedPercent < -0.01f)
                return debuffIconColor;
            return neutralIconColor;
        }

        private void ClearIcons()
        {
            for (var i = 0; i < _icons.Count; i++)
            {
                if (_icons[i] != null)
                    Destroy(_icons[i]);
            }

            _icons.Clear();
            _hasModifiers = false;
        }

        private void OnDisable() => HideTooltip();

        private static GameObject CreateDefaultIcon(Transform parent)
        {
            var go = new GameObject("ModifierIcon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(28f, 28f);
            var img = go.GetComponent<Image>();
            img.preserveAspect = true;
            return go;
        }

        private sealed class ModifierIconHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            private EraModifierBadgeView _owner;
            private string _title;
            private string _body;

            public void Bind(EraModifierBadgeView owner, string title, string body)
            {
                _owner = owner;
                _title = title;
                _body = body;
            }

            public void OnPointerEnter(PointerEventData eventData) =>
                _owner?.ShowTooltip(_title, _body);

            public void OnPointerExit(PointerEventData eventData) =>
                _owner?.HideTooltip();
        }
    }
}
