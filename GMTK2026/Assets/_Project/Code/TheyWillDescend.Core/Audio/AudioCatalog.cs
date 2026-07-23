using System.Collections.Generic;
using UnityEngine;

namespace TheyWillDescend.Core.Audio
{
    [CreateAssetMenu(fileName = "AudioCatalog", menuName = "They Will Descend/Audio Catalog")]
    public class AudioCatalog : ScriptableObject
    {
        public static class Ids
        {
            public const string UiClick = "UiClick";
            public const string CardDrop = "CardDrop";
            public const string OfferingAccept = "OfferingAccept";
            public const string PyramidTimerWarn = "PyramidTimerWarn";
            public const string EventDisaster = "EventDisaster";
            public const string MusicSettlement = "MusicSettlement";
        }

        [SerializeField] private List<SoundDefinition> sounds = new();

        public IEnumerable<AudioClip> EnumerateClips()
        {
            foreach (var sound in sounds)
            {
                if (sound?.Clips == null)
                    continue;

                foreach (var clip in sound.Clips)
                {
                    if (clip != null)
                        yield return clip;
                }
            }
        }

        public void WarmupClip(AudioClip clip)
        {
            if (clip == null || clip.loadState != AudioDataLoadState.Unloaded)
                return;

            clip.LoadAudioData();
        }

        public bool TryGet(string id, out SoundDefinition definition)
        {
            foreach (var sound in sounds)
            {
                if (sound != null && sound.Id == id)
                {
                    definition = sound;
                    return true;
                }
            }

            definition = null;
            return false;
        }
    }
}
