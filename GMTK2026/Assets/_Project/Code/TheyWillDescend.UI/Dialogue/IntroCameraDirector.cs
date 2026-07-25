using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;

namespace TheyWillDescend.UI.Dialogue
{
    /// <summary>
    /// Switches intro Cinemachine cameras by priority. Blend durations are set on the Brain / Custom Blends.
    /// Wait times here should be &gt;= those blend durations.
    /// </summary>
    public sealed class IntroCameraDirector : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera vcamIntroStart;
        [SerializeField] private CinemachineCamera vcamPlay;
        [SerializeField] private CinemachineCamera vcamPyramid;
        [SerializeField] private int activePriority = 100;
        [SerializeField] private int inactivePriority;

        private void Awake()
        {
            EnsureOrthoLens(vcamIntroStart);
            EnsureOrthoLens(vcamPlay);
            EnsureOrthoLens(vcamPyramid);
            // Start far / wide before the sequence runs.
            Activate(vcamIntroStart);
        }

        public void SnapToIntroStart() => Activate(vcamIntroStart);

        public void SnapToPlay() => Activate(vcamPlay);

        public UniTask TransitionToPlayAsync(float waitSeconds) =>
            TransitionAsync(vcamPlay, waitSeconds);

        public UniTask TransitionToPyramidAsync(float waitSeconds) =>
            TransitionAsync(vcamPyramid, waitSeconds);

        public UniTask TransitionToIntroStartAsync(float waitSeconds) =>
            TransitionAsync(vcamIntroStart, waitSeconds);

        /// <summary>
        /// Hold on IntroStart, then blend to Play. Wait should match Custom Blend IntroStart→Play.
        /// </summary>
        public async UniTask ApproachPlayAsync(float holdOnIntroSeconds, float blendWaitSeconds)
        {
            EnsureOrthoLens(vcamIntroStart);
            EnsureOrthoLens(vcamPlay);
            Activate(vcamIntroStart);

            var hold = Mathf.Max(0f, holdOnIntroSeconds);
            if (hold > 0f)
                await UniTask.Delay(System.TimeSpan.FromSeconds(hold), DelayType.UnscaledDeltaTime);

            await TransitionToPlayAsync(blendWaitSeconds);
        }

        private async UniTask TransitionAsync(CinemachineCamera target, float waitSeconds)
        {
            if (target == null)
            {
                Debug.LogWarning("[IntroCameraDirector] Missing CinemachineCamera reference.");
                var fallback = Mathf.Max(0f, waitSeconds);
                if (fallback > 0f)
                    await UniTask.Delay(System.TimeSpan.FromSeconds(fallback), DelayType.UnscaledDeltaTime);
                return;
            }

            EnsureOrthoLens(target);
            Activate(target);
            var wait = Mathf.Max(0f, waitSeconds);
            if (wait > 0f)
                await UniTask.Delay(System.TimeSpan.FromSeconds(wait), DelayType.UnscaledDeltaTime);
        }

        private void Activate(CinemachineCamera live)
        {
            SetPriority(vcamIntroStart, live == vcamIntroStart);
            SetPriority(vcamPlay, live == vcamPlay);
            SetPriority(vcamPyramid, live == vcamPyramid);
        }

        private void SetPriority(CinemachineCamera cam, bool live)
        {
            if (cam == null)
                return;
            cam.Priority = live ? activePriority : inactivePriority;
        }

        /// <summary>
        /// Without Orthographic Mode Override, CM inherits Main Camera size (~1075) and
        /// IntroStart→Play looks identical — only a tiny position delta.
        /// </summary>
        private static void EnsureOrthoLens(CinemachineCamera cam)
        {
            if (cam == null)
                return;

            var lens = cam.Lens;
            if (lens.ModeOverride == LensSettings.OverrideModes.Orthographic)
                return;

            lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            cam.Lens = lens;
        }
    }
}
