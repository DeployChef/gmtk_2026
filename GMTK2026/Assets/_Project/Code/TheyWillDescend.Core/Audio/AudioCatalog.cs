using System.Collections.Generic;
using UnityEngine;

namespace TheyWillDescend.Core.Audio
{
    [CreateAssetMenu(fileName = "AudioCatalog", menuName = "They Will Descend/Audio Catalog")]
    public class AudioCatalog : ScriptableObject
    {
        public static class Ids
        {
            public const string MusicMain = "MusicMain";
            public const string MusicDark = "MusicDark";
            public const string AmbientMain = "AmbientMain";
            public const string AmbientDark = "AmbientDark";
            public const string CardPickup = "CardPickup";
            public const string CardDropOk = "CardDropOk";
            public const string CardDropReject = "CardDropReject";
            public const string CardHover = "CardHover";
            public const string CardRemove = "CardRemove";
            public const string ResourceGold = "ResoursGold";
            public const string ResourceCorn = "ResourceCorn";
            public const string BuildStart = "BuildStart";
            public const string Thunder = "Thunder";
            public const string Fire = "Fire";
            public const string Defeat = "Defeat";
            public const string Victory = "Victory";
        }

        /// <summary>
        /// Default produce SFX for a resource output id. Empty = no known sound yet.
        /// Override per-building via <c>produceSoundId</c> when needed.
        /// </summary>
        public static string ResolveProduceSound(string outputResourceId)
        {
            if (string.IsNullOrEmpty(outputResourceId))
                return "";

            return outputResourceId switch
            {
                "Gold" => Ids.ResourceGold,
                "Corn" => Ids.ResourceCorn,
                "Wheat" => Ids.ResourceCorn,
                _ => ""
            };
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
