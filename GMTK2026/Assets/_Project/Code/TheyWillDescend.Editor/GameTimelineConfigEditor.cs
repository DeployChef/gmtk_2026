using TheyWillDescend.Core.Timeline;
using TheyWillDescend.Main.DI;
using UnityEditor;
using UnityEngine;
using VContainer;

namespace TheyWillDescend.Editor
{
    /// <summary>
    /// Inspector-only debug: jump to a phase while Play Mode is running (applies that phase's start loadout).
    /// </summary>
    [CustomEditor(typeof(GameTimelineConfig))]
    public sealed class GameTimelineConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var config = (GameTimelineConfig)target;
            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Debug — Phase Jump", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Enter Play Mode, then use these buttons. Jump applies the phase Starting Cards / Buildings loadout and resets the pyramid timer.",
                    MessageType.Info);
                GUI.enabled = false;
            }

            var phases = config.Phases;
            if (phases == null || phases.Length == 0)
            {
                EditorGUILayout.HelpBox("No phases configured.", MessageType.Warning);
                GUI.enabled = true;
                return;
            }

            for (var i = 0; i < phases.Length; i++)
            {
                var phase = phases[i];
                var title = phase != null ? phase.Title : $"Phase {i}";
                if (!GUILayout.Button($"Jump to [{i}] {title}"))
                    continue;

                if (!TryResolveTimeline(out var timeline))
                {
                    Debug.LogWarning(
                        "[GameTimelineConfig] No ITimelineService — is GameLifetimeScope built (Game scene loaded)?");
                    continue;
                }

                timeline.DebugJumpToPhase(i);
            }

            GUI.enabled = true;
        }

        private static bool TryResolveTimeline(out ITimelineService timeline)
        {
            timeline = null;
            var scopes = Object.FindObjectsByType<GameLifetimeScope>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            for (var i = 0; i < scopes.Length; i++)
            {
                var scope = scopes[i];
                if (scope == null || scope.Container == null)
                    continue;

                try
                {
                    timeline = scope.Container.Resolve<ITimelineService>();
                    if (timeline != null)
                        return true;
                }
                catch (VContainerException)
                {
                    // try next
                }
            }

            return false;
        }
    }
}
