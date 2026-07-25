using UnityEngine;

namespace TheyWillDescend.UI.Buildings
{
    /// <summary>
    /// Centers this RectTransform on a target RectTransform's width.
    /// Useful for buttons that live outside a layout group but need to track its center.
    /// </summary>
    public sealed class CenterOnTarget : MonoBehaviour
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private Vector3 offset;

        private RectTransform _rect;

        private void Awake()
        {
            _rect = transform as RectTransform;
        }

        private void LateUpdate()
        {
            if (target == null || _rect == null)
                return;

            // Position at horizontal center of target, same Y as this object
            var targetCenterX = target.position.x + target.rect.width * (0.5f - target.pivot.x) * target.lossyScale.x;
            _rect.position = new Vector3(targetCenterX + offset.x, _rect.position.y + offset.y, _rect.position.z + offset.z);
        }
    }
}
