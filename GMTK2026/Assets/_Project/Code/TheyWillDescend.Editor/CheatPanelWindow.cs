using TheyWillDescend.Core.Cheats;
using TheyWillDescend.Core.Session;
using TheyWillDescend.Core.Timeline;
using TheyWillDescend.Main.DI;
using UnityEditor;
using UnityEngine;
using VContainer;

namespace TheyWillDescend.Editor
{
    /// <summary>
    /// Play Mode cheat panel: grant cards + phase jump with building reset from <see cref="CheatPanelConfig"/>.
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
            window.minSize = new Vector2(280, 360);
            window.Show();
        }

        private void OnEnable()
        {
            if (_cheats == null)
            {
                var guids = AssetDatabase.FindAssets("t:CheatPanelConfig");
                if (guids.Length > 0)
                    _cheats = AssetDatabase.LoadAssetAtPath<CheatPanelConfig>(
                        AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            if (_timeline == null)
            {
                var guids = AssetDatabase.FindAssets("t:GameTimelineConfig");
                if (guids.Length > 0)
                    _timeline = AssetDatabase.LoadAssetAtPath<GameTimelineConfig>(
                        AssetDatabase.GUIDToAssetPath(guids[0]));
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

            if (_cheats != null && GUILayout.Button("Ping / Edit Cheat Config"))
            {
                EditorGUIUtility.PingObject(_cheats);
                Selection.activeObject = _cheats;
            }

            EditorGUILayout.Space(10);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Play Mode required.\n" +
                    "Jump resets buildings from CheatPanelConfig phase loadouts (Built list → Built, rest Locked, then cumulative unlocks).\n" +
                    "Edit Built Buildings / cards on CheatPanelConfig — not on the timeline.",
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
                EditorGUILayout.HelpBox("Assign Cheat Panel Config.", MessageType.Warning);
            else if (_cheats.GrantAllCardsOnJump)
                EditorGUILayout.HelpBox("Grant All On Jump ON — Jump fills catalog instead of phase Starting Cards.", MessageType.None);

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Phase Jump", EditorStyles.boldLabel);

            var phases = _timeline != null ? _timeline.Phases : null;
            if (phases == null || phases.Length == 0)
            {
                EditorGUILayout.HelpBox("Assign Timeline Config with phases.", MessageType.Warning);
            }
            else
            {
                if (_cheats != null && _cheats.PhaseLoadouts.Length < phases.Length)
                {
                    EditorGUILayout.HelpBox(
                        $"Phase Loadouts ({_cheats.PhaseLoadouts.Length}) < phases ({phases.Length}). " +
                        "Missing indices = all Locked + cumulative unlocks only.",
                        MessageType.Warning);
                }

                for (var i = 0; i < phases.Length; i++)
                {
                    var phase = phases[i];
                    var title = phase != null ? phase.Title : $"Phase {i}";
                    var loadout = _cheats != null ? _cheats.GetPhaseLoadout(i) : null;
                    var builtCount = loadout != null ? loadout.BuiltBuildings.Length : 0;
                    var label = loadout != null && !string.IsNullOrEmpty(loadout.Label)
                        ? loadout.Label
                        : title;

                    if (!GUILayout.Button($"Jump [{i}] {label}  (built×{builtCount})"))
                        continue;

                    TryJumpToPhase(i);
                }
            }

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Win / Lose", EditorStyles.boldLabel);
            if (GUILayout.Button("Force Win", GUILayout.Height(28)))
                TryForceResult(win: true);
            if (GUILayout.Button("Force Lose", GUILayout.Height(28)))
                TryForceResult(win: false);

            EditorGUILayout.EndScrollView();
        }

        private void TryForceResult(bool win)
        {
            if (!TryResolve(out IGameResultService results))
            {
                Debug.LogWarning("[CheatPanel] No IGameResultService — is Game scene loaded?");
                return;
            }

            if (win)
                results.DeclareWin(GameResultCause.Cheat);
            else
                results.DeclareLose(GameResultCause.Cheat);
        }

        private void TryJumpToPhase(int phaseIndex)
        {
            if (!TryResolve(out ITimelineService timeline))
            {
                Debug.LogWarning("[CheatPanel] No ITimelineService — is Game scene loaded?");
                return;
            }

            if (!TryResolve(out IPhaseLoadoutApplier loadout))
            {
                Debug.LogWarning("[CheatPanel] No IPhaseLoadoutApplier — is Game scene loaded?");
                return;
            }

            timeline.DebugJumpToPhase(phaseIndex);
            loadout.ApplyCheatJump(_cheats, _timeline, phaseIndex);
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
