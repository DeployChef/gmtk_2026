using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TheyWillDescend.UI.CameraFlow
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class CameraTransitionZone : MonoBehaviour, IPointerEnterHandler
    {
        [SerializeField] private CameraEdge edge;

        private RectTransform _rect;
        private Canvas _canvas;
        private CinemachineBrain _brain;
        private bool _inside;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>();
            _brain = FindFirstObjectByType<CinemachineBrain>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Debug.Log($"[CameraFlow] Zone '{name}' PointerEnter {edge}", this);
            Fire();
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return;

            var inside = ContainsScreenPoint(mouse.position.ReadValue());
            if (inside && !_inside)
            {
                Debug.Log($"[CameraFlow] Zone '{name}' MouseEnter {edge}", this);
                Fire();
            }

            _inside = inside;
        }

        private void Fire()
        {
            // Only the Brain's active camera — not every IsLive during a blend.
            var active = _brain != null ? _brain.ActiveVirtualCamera as CinemachineCamera : null;
            if (active == null)
            {
                Debug.LogWarning($"[CameraFlow] Zone '{name}': no ActiveVirtualCamera", this);
                return;
            }

            var links = active.GetComponent<CameraLookTransitions>();
            if (links == null)
            {
                Debug.LogWarning($"[CameraFlow] Zone '{name}': '{active.name}' has no CameraLookTransitions", this);
                return;
            }

            if (!links.IsActivePriority)
            {
                Debug.Log($"[CameraFlow] Zone '{name}': '{active.name}' not active priority", this);
                return;
            }

            links.TrySwitchFromEdge(edge);
        }

        private bool ContainsScreenPoint(Vector2 screenPoint)
        {
            if (_rect == null)
                _rect = (RectTransform)transform;
            if (_canvas == null)
                _canvas = GetComponentInParent<Canvas>();

            Camera eventCam = null;
            if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                eventCam = _canvas.worldCamera != null ? _canvas.worldCamera : Camera.main;

            return RectTransformUtility.RectangleContainsScreenPoint(_rect, screenPoint, eventCam);
        }
    }
}
