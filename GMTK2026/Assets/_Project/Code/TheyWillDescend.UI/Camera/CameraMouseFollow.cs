using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheyWillDescend.UI.CameraFlow
{
    /// <summary>
    /// Soft rotation chase toward the pointer. Put on each CinemachineCamera.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CameraMouseFollow : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera vcam;
        [SerializeField] private float yawDegrees = 6f;
        [SerializeField] private float pitchDegrees = 4f;
        [SerializeField] private float followSpeed = 5f;
        [SerializeField] private float deadZone = 0.02f;

        private Quaternion _restLocalRotation;
        private Vector2 _current;

        private void Awake()
        {
            if (vcam == null)
                vcam = GetComponent<CinemachineCamera>();
            _restLocalRotation = transform.localRotation;
        }

        private void OnDisable()
        {
            transform.localRotation = _restLocalRotation;
            _current = Vector2.zero;
        }

        private void LateUpdate()
        {
            if (vcam == null || !vcam.IsLive)
            {
                transform.localRotation = _restLocalRotation;
                _current = Vector2.zero;
                return;
            }

            var target = ReadPointerOffset(deadZone);
            var dt = Time.unscaledDeltaTime;
            _current = Vector2.Lerp(_current, target, 1f - Mathf.Exp(-followSpeed * dt));

            transform.localRotation =
                _restLocalRotation * Quaternion.Euler(-_current.y * pitchDegrees, _current.x * yawDegrees, 0f);
        }

        private static Vector2 ReadPointerOffset(float deadZone)
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return Vector2.zero;

            var cam = Camera.main;
            var screen = mouse.position.ReadValue();
            Vector2 offset;
            if (cam != null)
            {
                var vp = cam.ScreenToViewportPoint(screen);
                offset = new Vector2(vp.x * 2f - 1f, vp.y * 2f - 1f);
            }
            else
            {
                offset = new Vector2(
                    screen.x / Mathf.Max(1, Screen.width) * 2f - 1f,
                    screen.y / Mathf.Max(1, Screen.height) * 2f - 1f);
            }

            if (offset.sqrMagnitude < deadZone * deadZone)
                return Vector2.zero;
            return Vector2.ClampMagnitude(offset, 1f);
        }
    }
}
