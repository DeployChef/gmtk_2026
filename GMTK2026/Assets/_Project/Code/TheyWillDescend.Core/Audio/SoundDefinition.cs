using UnityEngine;
using UnityEngine.Audio;

namespace TheyWillDescend.Core.Audio
{
    [System.Serializable]
    public class SoundDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private AudioChannel channel = AudioChannel.GameplaySfx;
        [SerializeField] private AudioClip[] clips;
        [SerializeField] private AudioMixerGroup mixerGroup;
        [SerializeField] private int priority = 50;
        [SerializeField] private bool allowOverlap = true;
        [SerializeField] private float cooldown;
        [SerializeField] private bool loop;
        [SerializeField] private bool enableFade;
        [SerializeField] private float fadeDuration = 1f;
        [SerializeField] private float fadeOutDuration;
        [SerializeField] private bool stopOnPause;
        [SerializeField] private float basePitch = 1f;
        [SerializeField] private float pitchRandomRange;
        [SerializeField] private float bpmStart;
        [SerializeField] private float bpmEnd;
        [SerializeField] private float bpmDuration;

        public string Id => id;
        public AudioChannel Channel => channel;
        public AudioClip[] Clips => clips;
        public AudioMixerGroup MixerGroup => mixerGroup;
        public int Priority => priority;
        public bool AllowOverlap => allowOverlap;
        public float Cooldown => cooldown;
        public bool Loop => loop;
        public bool EnableFade => enableFade;
        public float FadeDuration => fadeDuration;
        public float FadeOutDuration => fadeOutDuration;
        public bool StopOnPause => stopOnPause;
        public float BasePitch => basePitch;
        public float PitchRandomRange => pitchRandomRange;
        public float BpmStart => bpmStart;
        public float BpmEnd => bpmEnd;
        public float BpmDuration => bpmDuration;
    }
}
