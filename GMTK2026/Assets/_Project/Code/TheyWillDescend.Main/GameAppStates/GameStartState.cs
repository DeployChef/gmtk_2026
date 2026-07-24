using TheyWillDescend.Core.Audio;
using TheyWillDescend.Core.Timeline;
using UnityEngine;

namespace TheyWillDescend.Main.GameAppStates
{
    /// <summary>
    /// Enter once after Game scope is ready: start timeline (phase 0 loadout) + BGM.
    /// Starting cards/buildings come from <see cref="GameTimelineConfig"/> phase 0 — not hardcoded here.
    /// </summary>
    public sealed class GameStartState
    {
        private readonly IAudioManager _audio;
        private readonly ITimelineService _timeline;

        public GameStartState(IAudioManager audio, ITimelineService timeline)
        {
            _audio = audio;
            _timeline = timeline;
        }

        public void Enter()
        {
            _timeline?.StartRun();
            _audio?.Play(AudioCatalog.Ids.MusicMain);
            Debug.Log("[GameStartState] Enter — timeline StartRun (phase 0 loadout).");
        }
    }
}
