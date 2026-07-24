using TheyWillDescend.Core.Cheats;
using TheyWillDescend.Core.Timeline;
using TheyWillDescend.Main.DI;
using UnityEditor;
using UnityEngine;
using VContainer;

namespace TheyWillDescend.Editor
{
    /// <summary>
    /// Play Mode cheat panel: grant cards + phase jump. Uses <see cref="CheatPanelConfig"/> from the window field (not GameLifetimeScope).
    /// </summary>
    public sealed class CheatPanelWindow : EditorWindow
    {
        private CheatPanelConfig _cheats;
        private GameTimelineConfig _timeline;
        private Vector2 _scroll;

        [MenuItem("They Will Descend/Cheat Panel")]
        public static void Open()
        {
            var window = GetWindow<CheatPanelWindow>("Cheat Panel");
            window.minSize = new Vector2(280, 320);
            window.Show();
        }

        private void OnEnable()
        {
            if (_cheats == null)
            {
                var guids = AssetDatabase.FindAssets("t:CheatPanelConfig");
                if (guids.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _cheats = AssetDatabase.LoadAssetAtPath<CheatPanelConfig>(path);
                }
            }

            if (_timeline == null)
            {
                var guids = AssetDatabase.FindAssets("t:GameTimelineConfig");
                if (guids.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _timeline = AssetDatabase.LoadAssetAtPath<GameTimelineConfig>(path);
                }
            }
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("Configs", EditorStyles.boldLabel);
            _cheats = (CheatPanelConfig)EditorGUILayout.ObjectField(
                "Cheat Panel Config", _cheats, typeof(CheatPanelConfig), false);
            _timeline = (GameTimelineConfig)EditorGUILayout.ObjectField(
                "Timeline Config", _timeline, typeof(GameTimelineConfig), false);

            if (_cheats != null && GUILayout.Button("Ping Cheat Config"))
            {
                EditorGUIUtility.PingObject(_cheats);
                Selection.activeObject = _cheats;
            }

            EditorGUILayout.Space(10);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Enter Play Mode (Game scene loaded) to use cheats.\n" +
                    "No need to assign CheatPanelConfig on GameLifetimeScope — only here.",
                    MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.LabelField("Cards", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(_cheats == null))
            {
                if (GUILayout.Button("Grant All Cards (catalog)", GUILayout.Height(28)))
                    TryGrantAllCards();
            }

            if (_cheats == null)
                EditorGUILayout.HelpBox("Assign Cheat Panel Config (catalog lives there).", MessageType.Warning);
            else if (_cheats.GrantAllCardsOnJump)
                EditorGUILayout.HelpBox("Grant All Cards On Jump is ON — Jump will refill catalog after loadout.", MessageType.None);

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Phase Jump", EditorStyles.boldLabel);

            var phases = _timeline != null ? _timeline.Phases : null;
            if (phases == null || phases.Length == 0)
            {
                EditorGUILayout.HelpBox("Assign Timeline Config with phases.", MessageType.Warning);
            }
            else
            {
                for (var i = 0; i < phases.Length; i++)
                {
                    var phase = phases[i];
                    var title = phase != null ? phase.Title : $"Phase {i}";
                    if (!GUILayout.Button($"Jump to [{i}] {title}"))
                        continue;

                    if (!TryResolve(out ITimelineService timeline))
                    {
                        Debug.LogWarning("[CheatPanel] No ITimelineService — is Game scene loaded?");
                        continue;
                    }

                    timeline.DebugJumpToPhase(i);

                    if (_cheats != null && _cheats.GrantAllCardsOnJump)
                        TryGrantAllCards();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void TryGrantAllCards()
        {
            if (_cheats == null)
            {
                Debug.LogWarning("[CheatPanel] Assign Cheat Panel Config first.");
                return;
            }

            if (!TryResolve(out IPhaseLoadoutApplier loadout))
            {
                Debug.LogWarning("[CheatPanel] No IPhaseLoadoutApplier — is Game scene loaded?");
                return;
            }

            loadout.GrantAllCardsFromCatalog(_cheats);
        }

        private static bool TryResolve<T>(out T service) where T : class
        {
            service = null;
            var scopes = Object.FindObjectsByType<GameLifetimeScope>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            for (var i = 0; i < scopes.Length; i++)
            {
                var scope = scopes[i];
                if (scope == null || scope.Container == null)
                    continue;

                try
                {
                    service = scope.Container.Resolve<T>();
                    if (service != null)
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
