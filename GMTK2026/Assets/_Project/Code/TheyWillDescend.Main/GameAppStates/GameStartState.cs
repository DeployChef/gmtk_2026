using Cysharp.Threading.Tasks;
using TheyWillDescend.Core;
using TheyWillDescend.Core.Audio;
using TheyWillDescend.Core.Timeline;
using UnityEngine;

namespace TheyWillDescend.Main.GameAppStates
{
    /// <summary>
    /// Enter once after Game scope is ready: BGM, StartRun (loadout), opening cinematic, then gameplay ticks.
    /// </summary>
    public sealed class GameStartState
    {
        private static readonly object PauseKey = new();

        private readonly IAudioManager _audio;
        private readonly ITimelineService _timeline;
        private readonly IOpeningSequence _opening;
        private readonly IGameplayTimePause _timePause;

        public GameStartState(
            IAudioManager audio,
            ITimelineService timeline,
            IOpeningSequence opening,
            IGameplayTimePause timePause)
        {
            _audio = audio;
            _timeline = timeline;
            _opening = opening;
            _timePause = timePause;
        }

        public void Enter()
        {
            EnterAsync().Forget();
        }

        private async UniTaskVoid EnterAsync()
        {
            _audio?.Play(AudioCatalog.Ids.MusicMain);
            _audio?.PlayAmbient(AudioCatalog.Ids.AmbientMain);

            // Freeze gameplay before StartRun so phase/timer don't tick during the cinematic.
            _timePause?.Acquire(PauseKey);
            try
            {
                _timeline?.StartRun();
                Debug.Log("[GameStartState] StartRun — opening sequence (gameplay paused).");

                if (_opening != null)
                    await _opening.PlayAsync();

                Debug.Log("[GameStartState] Opening done — gameplay time running.");
            }
            finally
            {
                _timePause?.Release(PauseKey);
            }
        }
    }
}
