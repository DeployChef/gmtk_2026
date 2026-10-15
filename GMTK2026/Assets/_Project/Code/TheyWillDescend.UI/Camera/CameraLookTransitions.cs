using Unity.Cinemachine;
using UnityEngine;

namespace TheyWillDescend.UI.CameraFlow
{
    public enum CameraEdge
    {
        Bottom = 0,
        Top = 1,
        Left = 2,
        Right = 3
    }

    /// <summary>
    /// Works only while this camera is the Brain's active (highest-priority) camera.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CameraLookTransitions : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera top;
        [SerializeField] private CinemachineCamera bottom;
        [SerializeField] private CinemachineCamera left;
        [SerializeField] private CinemachineCamera right;
        [SerializeField] private bool startHere;

        private CinemachineCamera _self;
        private CinemachineBrain _brain;

        private void Awake()
        {
            _self = GetComponent<CinemachineCamera>();
            _brain = FindFirstObjectByType<CinemachineBrain>();
            if (_self != null)
                _self.StandbyUpdate = CinemachineVirtualCameraBase.StandbyUpdateMode.Always;

            // Gameplay pause sets timeScale=0. Brain blends use deltaTime, so the
            // active cam name changes but the picture stays on the old cam at 0% blend.
            if (_brain != null)
                _brain.IgnoreTimeScale = true;
        }

        private void Start()
        {
            if (startHere && _self != null)
            {
                Debug.Log($"[CameraFlow] StartHere → {_self.name} (timeScale={Time.timeScale}, brainIgnoreTS={_brain != null && _brain.IgnoreTimeScale})", this);
                Activate(_self);
            }
        }

        /// <summary>True only for the camera Brain currently drives (not blend leftovers).</summary>
        public bool IsActivePriority
        {
            get
            {
                if (_self == null)
                    return false;
                if (_brain != null && _brain.ActiveVirtualCamera != null)
                    return ReferenceEquals(_brain.ActiveVirtualCamera, (ICinemachineCamera)_self);
                return _self.IsLive && _self.Priority.Value >= 100;
            }
        }

        public void TrySwitchFromEdge(CameraEdge edge)
        {
            if (!IsActivePriority)
            {
                Debug.Log($"[CameraFlow] skip '{name}': not active priority", this);
                return;
            }

            var next = edge switch
            {
                CameraEdge.Top => top,
                CameraEdge.Bottom => bottom,
                CameraEdge.Left => left,
                CameraEdge.Right => right,
                _ => null
            };

            if (next == null)
            {
                Debug.Log($"[CameraFlow] skip '{name}': {edge} empty", this);
                return;
            }

            Debug.Log($"[CameraFlow] SWITCH {_self.name} → {next.name} ({edge})", this);
            Activate(next);
        }

        private static void Activate(CinemachineCamera live)
        {
            var all = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
            for (var i = 0; i < all.Length; i++)
            {
                var on = all[i] == live;
                all[i].Priority = new PrioritySettings
                {
                    Enabled = true,
                    Value = on ? 100 : 0
                };
                all[i].Prioritize();
            }

            Debug.Log($"[CameraFlow] Activate done → {live.name}. Main will move only if Brain blends advance (IgnoreTimeScale should be on).");
        }
    }
}
