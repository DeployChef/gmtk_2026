using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TheyWillDescend.Core.Audio;
using UnityEngine;
using UnityEngine.Audio;

namespace TheyWillDescend.UI.Audio
{
    public sealed class AudioManager : MonoBehaviour, IAudioManager
    {
        private const string PrefsMusicVolume = "twd.audio.music_volume";
        private const string PrefsSfxVolume = "twd.audio.sfx_volume";
        private const string ExposedMusicParam = "MusicVolume";
        private const string ExposedSfxParam = "SfxVolume";
        private const float DefaultVolume = 0.8f;

        [SerializeField] private AudioCatalog config;
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private int sfxPoolSize = 8;
        [SerializeField] private int uiPoolSize = 3;
        [SerializeField] private bool enableFade = true;

        private AudioSource _musicSource;
        private readonly List<SfxVoice> _sfxVoices = new();
        private readonly List<SfxVoice> _uiVoices = new();
        private readonly Dictionary<string, float> _lastPlayTimes = new();
        private string _musicSoundId;
        private float _musicFadeDuration = 1f;
        private float _musicVolumeBeforePause = 1f;
        private bool _musicPaused;
        private CancellationTokenSource _musicFadeCts;

        public bool IsMusicPaused => _musicPaused;
        public bool HasMusicClip => _musicSource != null && _musicSource.clip != null;

        private void Awake()
        {
            _musicSource = CreateSource("Music", transform);
            _musicSource.loop = true;
            _musicSource.volume = 0f;

            for (var i = 0; i < sfxPoolSize; i++)
                _sfxVoices.Add(new SfxVoice(CreateSource($"Sfx_{i}", transform)));

            for (var i = 0; i < uiPoolSize; i++)
                _uiVoices.Add(new SfxVoice(CreateSource($"Ui_{i}", transform)));

            ApplyVolumeFromPrefs();
        }

        private void OnDestroy()
        {
            CancelMusicFade();
            foreach (var voice in _sfxVoices)
                voice.CancelFade();
            foreach (var voice in _uiVoices)
                voice.CancelFade();
        }

        public void Play(string soundId, float? pitch = null, float? pitchRandomRange = null)
        {
            if (config == null || !config.TryGet(soundId, out var definition))
                return;

            if (definition.Clips == null || definition.Clips.Length == 0)
                return;

            var playClock = GetPlayClock(definition.Channel);
            if (definition.Cooldown > 0f
                && _lastPlayTimes.TryGetValue(soundId, out var lastTime)
                && playClock - lastTime < definition.Cooldown)
                return;

            if (!definition.AllowOverlap && IsPlaying(soundId))
                return;

            PlayDefinition(definition, pitch, pitchRandomRange);
            _lastPlayTimes[soundId] = playClock;
        }

        public void Stop(string soundId)
        {
            if (_musicSoundId == soundId)
            {
                StopMusic();
                return;
            }

            StopInPool(_sfxVoices, soundId);
            StopInPool(_uiVoices, soundId);
        }

        public void StopMusic()
        {
            if (_musicSource == null || !HasMusicClip)
                return;

            CancelMusicFade();
            _musicPaused = false;

            if (_musicSource.isPlaying && enableFade)
            {
                _musicFadeCts = new CancellationTokenSource();
                FadeAndStopMusicAsync(_musicFadeDuration, _musicFadeCts.Token).Forget();
                _musicSoundId = null;
                return;
            }

            _musicSource.Stop();
            _musicSource.clip = null;
            _musicSource.volume = 0f;
            _musicSoundId = null;
        }

        public void PauseMusic()
        {
            if (_musicSource == null || _musicSource.clip == null || _musicPaused)
                return;

            if (!_musicSource.isPlaying && _musicSource.time <= 0f)
                return;

            StopSfxWithStopOnPause();
            _musicVolumeBeforePause = _musicSource.volume > 0f ? _musicSource.volume : 1f;
            _musicPaused = true;
            FadeOutAndPauseMusicAsync().Forget();
        }

        public void ResumeMusic()
        {
            if (_musicSource == null || _musicSource.clip == null || !_musicPaused)
                return;

            _musicPaused = false;
            _musicSource.UnPause();
            CancelMusicFade();
            _musicFadeCts = new CancellationTokenSource();
            var targetVolume = _musicVolumeBeforePause > 0f ? _musicVolumeBeforePause : 1f;
            FadeToAsync(_musicSource, targetVolume, _musicFadeDuration, _musicFadeCts.Token).Forget();
        }

        public void StopAll()
        {
            StopMusic();
            foreach (var voice in _sfxVoices)
                StopVoiceImmediate(voice);
            foreach (var voice in _uiVoices)
                StopVoiceImmediate(voice);
        }

        public bool IsPlaying(string soundId)
        {
            if (_musicSoundId == soundId && _musicSource != null && (_musicSource.isPlaying || _musicPaused))
                return true;

            return IsPlayingInPool(_sfxVoices, soundId) || IsPlayingInPool(_uiVoices, soundId);
        }

        public IEnumerable<AudioClip> EnumerateClips() =>
            config != null ? config.EnumerateClips() : System.Array.Empty<AudioClip>();

        public void WarmupClip(AudioClip clip) => config?.WarmupClip(clip);

        public void SetMusicVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(PrefsMusicVolume, volume);
            PlayerPrefs.Save();
            ApplyMusicVolume(volume);
        }

        public void SetSfxVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(PrefsSfxVolume, volume);
            PlayerPrefs.Save();
            ApplySfxVolume(volume);
        }

        public float GetMusicVolume() => PlayerPrefs.GetFloat(PrefsMusicVolume, DefaultVolume);
        public float GetSfxVolume() => PlayerPrefs.GetFloat(PrefsSfxVolume, DefaultVolume);

        private void ApplyVolumeFromPrefs()
        {
            ApplyMusicVolume(GetMusicVolume());
            ApplySfxVolume(GetSfxVolume());
        }

        private void ApplyMusicVolume(float volume)
        {
            if (mixer == null)
                return;
            mixer.SetFloat(ExposedMusicParam, VolumeToDb(volume));
        }

        private void ApplySfxVolume(float volume)
        {
            if (mixer == null)
                return;
            mixer.SetFloat(ExposedSfxParam, VolumeToDb(volume));
        }

        private static float VolumeToDb(float linear) =>
            linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;

        private static float GetPlayClock(AudioChannel channel) =>
            channel == AudioChannel.UiSfx ? Time.unscaledTime : Time.time;

        private void PlayDefinition(SoundDefinition sound, float? pitch, float? pitchRandomRange)
        {
            if (sound.Channel == AudioChannel.Music)
            {
                PlayMusic(sound, pitch, pitchRandomRange);
                return;
            }

            var pool = sound.Channel == AudioChannel.UiSfx ? _uiVoices : _sfxVoices;
            var voice = AcquireVoice(pool, sound);
            if (voice == null)
                return;

            PlayOnVoice(voice, sound, pitch, pitchRandomRange);
        }

        private void PlayMusic(SoundDefinition sound, float? pitch, float? pitchRandomRange)
        {
            CancelMusicFade();

            var clip = sound.Clips[Random.Range(0, sound.Clips.Length)];
            _musicSource.outputAudioMixerGroup = sound.MixerGroup;
            _musicSource.loop = sound.Loop;
            _musicSource.clip = clip;
            _musicSource.pitch = ResolvePitch(sound, pitch, pitchRandomRange);
            _musicSoundId = sound.Id;
            _musicFadeDuration = sound.EnableFade ? sound.FadeDuration : 0f;
            _musicFadeCts = new CancellationTokenSource();

            if (enableFade && sound.EnableFade)
            {
                _musicSource.volume = 0f;
                _musicSource.Play();
                FadeToAsync(_musicSource, 1f, sound.FadeDuration, _musicFadeCts.Token).Forget();
            }
            else
            {
                _musicSource.volume = 1f;
                _musicSource.Play();
            }

            _musicPaused = false;
            _musicVolumeBeforePause = _musicSource.volume;
        }

        private SfxVoice AcquireVoice(List<SfxVoice> pool, SoundDefinition sound)
        {
            var voice = FindFreeVoice(pool, sound);
            if (voice != null)
                return voice;

            if (TryStopLowestPrioritySfx(sound.Priority))
                return FindFreeVoice(pool, sound);

            return null;
        }

        private bool TryStopLowestPrioritySfx(int incomingPriority)
        {
            SfxVoice lowest = null;
            var lowestPriority = int.MinValue;

            foreach (var voice in _sfxVoices)
            {
                if (!voice.Source.isPlaying)
                    continue;

                if (voice.Priority > lowestPriority)
                {
                    lowestPriority = voice.Priority;
                    lowest = voice;
                }
            }

            if (lowest == null || incomingPriority >= lowestPriority)
                return false;

            StopVoiceImmediate(lowest);
            return true;
        }

        private static SfxVoice FindFreeVoice(List<SfxVoice> pool, SoundDefinition sound)
        {
            foreach (var voice in pool)
            {
                if (!voice.Source.isPlaying)
                    return voice;

                if (voice.SoundId == sound.Id && !sound.AllowOverlap)
                    return null;
            }

            return sound.AllowOverlap ? pool[0] : null;
        }

        private void PlayOnVoice(SfxVoice voice, SoundDefinition sound, float? pitch, float? pitchRandomRange)
        {
            voice.CancelFade();

            var clip = sound.Clips[Random.Range(0, sound.Clips.Length)];
            voice.Source.outputAudioMixerGroup = sound.MixerGroup;
            voice.Source.loop = sound.Loop;
            voice.Source.clip = clip;
            voice.Source.pitch = ResolvePitch(sound, pitch, pitchRandomRange);
            voice.SoundId = sound.Id;
            voice.Priority = sound.Priority;
            voice.StopOnPause = sound.StopOnPause;

            if (enableFade && sound.EnableFade && sound.FadeDuration > 0f)
            {
                voice.Source.volume = 0f;
                voice.Source.Play();
                voice.FadeCts = new CancellationTokenSource();
                FadeToAsync(voice.Source, 1f, sound.FadeDuration, voice.FadeCts.Token).Forget();
            }
            else
            {
                voice.Source.volume = 1f;
                voice.Source.Play();
            }
        }

        private static float ResolvePitch(SoundDefinition sound, float? pitch, float? pitchRandomRange)
        {
            var basePitch = pitch ?? sound.BasePitch;
            var range = pitchRandomRange ?? sound.PitchRandomRange;
            return range <= 0f ? basePitch : basePitch + Random.Range(-range, range);
        }

        private void StopInPool(List<SfxVoice> pool, string soundId)
        {
            foreach (var voice in pool)
            {
                if (voice.Source.isPlaying && voice.SoundId == soundId)
                    StopVoiceImmediate(voice);
            }
        }

        private static bool IsPlayingInPool(List<SfxVoice> pool, string soundId)
        {
            foreach (var voice in pool)
            {
                if (voice.Source.isPlaying && voice.SoundId == soundId)
                    return true;
            }

            return false;
        }

        private void StopVoiceImmediate(SfxVoice voice)
        {
            voice.CancelFade();
            voice.Source.volume = 0f;
            voice.Source.pitch = 1f;
            voice.Source.Stop();
            voice.Source.clip = null;
            voice.SoundId = null;
            voice.Priority = 0;
        }

        private void StopSfxWithStopOnPause()
        {
            foreach (var voice in _sfxVoices)
            {
                if (voice.Source.isPlaying && voice.StopOnPause)
                    StopVoiceImmediate(voice);
            }

            foreach (var voice in _uiVoices)
            {
                if (voice.Source.isPlaying && voice.StopOnPause)
                    StopVoiceImmediate(voice);
            }
        }

        private void CancelMusicFade()
        {
            if (_musicFadeCts == null)
                return;

            _musicFadeCts.Cancel();
            _musicFadeCts.Dispose();
            _musicFadeCts = null;
        }

        private async UniTaskVoid FadeOutAndPauseMusicAsync()
        {
            CancelMusicFade();
            _musicFadeCts = new CancellationTokenSource();
            var ct = _musicFadeCts.Token;
            _musicVolumeBeforePause = _musicSource.volume > 0f ? _musicSource.volume : 1f;

            try
            {
                if (enableFade && _musicFadeDuration > 0f)
                    await FadeToAsync(_musicSource, 0f, _musicFadeDuration, ct);

                if (_musicSource == null || _musicSource.clip == null)
                    return;

                if (_musicSource.isPlaying)
                    _musicSource.Pause();

                _musicSource.volume = 0f;
                _musicPaused = true;
            }
            catch (System.OperationCanceledException)
            {
            }
        }

        private async UniTaskVoid FadeAndStopMusicAsync(float duration, CancellationToken ct)
        {
            try
            {
                await FadeToAsync(_musicSource, 0f, duration, ct);
                _musicSource.Stop();
                _musicSource.clip = null;
                _musicSource.pitch = 1f;
                _musicPaused = false;
            }
            catch (System.OperationCanceledException)
            {
            }
        }

        private static async UniTask FadeToAsync(
            AudioSource source,
            float target,
            float duration,
            CancellationToken ct)
        {
            if (duration <= 0f)
            {
                source.volume = target;
                return;
            }

            var start = source.volume;
            var t = 0f;
            while (t < duration)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
                t += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(start, target, t / duration);
            }

            source.volume = target;
        }

        private static AudioSource CreateSource(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        private sealed class SfxVoice
        {
            public SfxVoice(AudioSource source) => Source = source;

            public AudioSource Source { get; }
            public string SoundId { get; set; }
            public int Priority { get; set; }
            public CancellationTokenSource FadeCts { get; set; }
            public bool StopOnPause { get; set; }

            public void CancelFade()
            {
                if (FadeCts == null)
                    return;

                FadeCts.Cancel();
                FadeCts.Dispose();
                FadeCts = null;
            }
        }
    }
}
