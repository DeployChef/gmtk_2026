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
        [SerializeField] private bool enableFade = true;
        [Tooltip("Smooth time for music pitch changes (pyramid timer bands).")]
        [SerializeField] private float musicPitchSmoothTime = 0.85f;

        private AudioSource _musicSource;
        private AudioSource _ambientSource;
        private readonly Dictionary<string, float> _lastPlayTimes = new();
        private readonly Dictionary<string, List<SfxVoice>> _soundVoicePools = new();
        private string _musicSoundId;
        private string _ambientSoundId;
        private float _musicFadeDuration = 1f;
        private float _musicFadeOutDuration = 1f;
        private float _ambientFadeDuration = 1f;
        private float _musicVolumeBeforePause = 1f;
        private bool _musicPaused;
        private CancellationTokenSource _musicFadeCts;
        private CancellationTokenSource _ambientFadeCts;
        private float _musicPitchTarget = 1f;
        private float _musicPitchVelocity;

        public bool IsMusicPaused => _musicPaused;
        public bool HasMusicClip => _musicSource != null && _musicSource.clip != null;
        public bool HasAmbientClip => _ambientSource != null && _ambientSource.clip != null;

        private void Awake()
        {
            _musicSource = CreateSource("Music", transform);
            _musicSource.loop = true;
            _musicSource.volume = 0f;

            _ambientSource = CreateSource("Ambient", transform);
            _ambientSource.loop = true;
            _ambientSource.volume = 0f;

            ApplyVolumeFromPrefs();
        }

        private void Update()
        {
            if (_musicSource == null || !HasMusicClip || _musicPaused)
                return;

            if (Mathf.Abs(_musicSource.pitch - _musicPitchTarget) < 0.0005f)
            {
                _musicSource.pitch = _musicPitchTarget;
                _musicPitchVelocity = 0f;
                return;
            }

            var smooth = Mathf.Max(0.01f, musicPitchSmoothTime);
            _musicSource.pitch = Mathf.SmoothDamp(
                _musicSource.pitch,
                _musicPitchTarget,
                ref _musicPitchVelocity,
                smooth,
                Mathf.Infinity,
                Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            CancelMusicFade();
            CancelAmbientFade();
            
            // Очищаем динамические пулы
            foreach (var pool in _soundVoicePools.Values)
            {
                foreach (var voice in pool)
                    voice.CancelFade();
            }
        }

        public void Play(string soundId, float? pitch = null, float? pitchRandomRange = null)
        {
            if (config == null || !config.TryGet(soundId, out var definition))
                return;

            if (definition.Clips == null || definition.Clips.Length == 0)
                return;

            var playClock = GetPlayClock(definition.Channel);

            // Кулдаун блокирует только если AllowOverlap = false
            if (definition.Cooldown > 0f
                && !definition.AllowOverlap
                && _lastPlayTimes.TryGetValue(soundId, out var lastTime)
                && playClock - lastTime < definition.Cooldown)
                return;

            if (!definition.AllowOverlap && IsPlaying(soundId))
                return;

            bool played = PlayDefinition(definition, pitch, pitchRandomRange);
            // Записываем время только если звук реально проигрался
            if (played)
                _lastPlayTimes[soundId] = playClock;
        }

        public void Stop(string soundId)
        {
            if (_musicSoundId == soundId)
            {
                StopMusic();
                return;
            }

            // Останавливаем все голоса этого звука
            if (_soundVoicePools.TryGetValue(soundId, out var voicePool))
            {
                foreach (var voice in voicePool)
                    StopVoiceImmediate(voice);
            }
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
                var outDur = _musicFadeOutDuration > 0f ? _musicFadeOutDuration : _musicFadeDuration;
                FadeAndStopMusicAsync(outDur, _musicFadeCts.Token).Forget();
                _musicSoundId = null;
                return;
            }

            _musicSource.Stop();
            _musicSource.clip = null;
            _musicSource.volume = 0f;
            _musicSource.pitch = 1f;
            _musicPitchTarget = 1f;
            _musicPitchVelocity = 0f;
            _musicSoundId = null;
        }

        public void PauseMusic()
        {
            if (_musicSource == null || _musicSource.clip == null || _musicPaused)
                return;

            if (!_musicSource.isPlaying && _musicSource.time <= 0f)
                return;

            // Останавливаем все голоса со stopOnPause
            foreach (var pool in _soundVoicePools.Values)
            {
                foreach (var voice in pool)
                {
                    if (voice.Source.isPlaying && voice.StopOnPause)
                        StopVoiceImmediate(voice);
                }
            }
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
            StopAmbient();
            
            // Останавливаем все динамические пулы
            foreach (var pool in _soundVoicePools.Values)
            {
                foreach (var voice in pool)
                    StopVoiceImmediate(voice);
            }
            _soundVoicePools.Clear();
        }

        public void PlayAmbient(string soundId, float? pitch = null, float? pitchRandomRange = null)
        {
            if (config == null || !config.TryGet(soundId, out var definition))
                return;

            if (definition.Clips == null || definition.Clips.Length == 0)
                return;

            CancelAmbientFade();

            _ambientSource.outputAudioMixerGroup = definition.MixerGroup;
            _ambientSource.loop = definition.Loop;
            _ambientSource.clip = definition.Clips[Random.Range(0, definition.Clips.Length)];
            _ambientSource.pitch = ResolvePitch(definition, pitch, pitchRandomRange);
            _ambientSoundId = soundId;
            _ambientFadeDuration = definition.EnableFade ? definition.FadeDuration : 0f;
            _ambientFadeCts = new CancellationTokenSource();

            if (enableFade && definition.EnableFade)
            {
                _ambientSource.volume = 0f;
                _ambientSource.Play();
                FadeToAsync(_ambientSource, 1f, definition.FadeDuration, _ambientFadeCts.Token).Forget();
            }
            else
            {
                _ambientSource.volume = 1f;
                _ambientSource.Play();
            }
        }

        public void StopAmbient()
        {
            if (_ambientSource == null || !HasAmbientClip)
                return;

            CancelAmbientFade();
            _ambientSource.Stop();
            _ambientSource.clip = null;
            _ambientSource.volume = 0f;
            _ambientSoundId = null;
        }

        private void CancelAmbientFade()
        {
            if (_ambientFadeCts == null)
                return;

            _ambientFadeCts.Cancel();
            _ambientFadeCts.Dispose();
            _ambientFadeCts = null;
        }

        public bool IsPlaying(string soundId)
        {
            if (_musicSoundId == soundId && _musicSource != null && (_musicSource.isPlaying || _musicPaused))
                return true;

            // Проверяем динамические пулы
            if (_soundVoicePools.TryGetValue(soundId, out var pool))
            {
                foreach (var voice in pool)
                {
                    if (voice.Source.isPlaying && voice.SoundId == soundId)
                        return true;
                }
            }

            return false;
        }

        public IEnumerable<AudioClip> EnumerateClips() =>
            config != null ? config.EnumerateClips() : System.Array.Empty<AudioClip>();

        public void WarmupClip(AudioClip clip) => config?.WarmupClip(clip);

        public void SetMusicPitch(float pitch)
        {
            if (_musicSource == null)
                return;

            _musicPitchTarget = Mathf.Clamp(pitch, 0.5f, 3f);
        }

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

        private bool PlayDefinition(SoundDefinition sound, float? pitch, float? pitchRandomRange)
        {
            if (sound.Channel == AudioChannel.Music)
            {
                PlayMusic(sound, pitch, pitchRandomRange);
                return true;
            }

            var voice = AcquireVoiceForSound(sound);
            if (voice == null)
                return false;
            PlayOnVoice(voice, sound, pitch, pitchRandomRange);
            return true;
        }

        private void PlayMusic(SoundDefinition sound, float? pitch, float? pitchRandomRange)
        {
            if (sound.Clips == null || sound.Clips.Length == 0)
                return;

            // Already on this track — keep playing.
            if (_musicSoundId == sound.Id && HasMusicClip && (_musicSource.isPlaying || _musicPaused))
            {
                if (_musicPaused)
                    ResumeMusic();
                return;
            }

            var crossfade = enableFade
                            && sound.EnableFade
                            && HasMusicClip
                            && _musicSoundId != sound.Id
                            && (_musicSource.isPlaying || _musicSource.volume > 0.01f);

            if (crossfade)
            {
                CrossfadeToMusicAsync(sound, pitch, pitchRandomRange).Forget();
                return;
            }

            StartMusicImmediate(sound, pitch, pitchRandomRange, fadeIn: enableFade && sound.EnableFade);
        }

        private async UniTaskVoid CrossfadeToMusicAsync(
            SoundDefinition sound,
            float? pitch,
            float? pitchRandomRange)
        {
            CancelMusicFade();
            _musicFadeCts = new CancellationTokenSource();
            var ct = _musicFadeCts.Token;
            _musicPaused = false;

            try
            {
                var outDur = _musicFadeOutDuration > 0f
                    ? _musicFadeOutDuration
                    : (_musicFadeDuration > 0f ? _musicFadeDuration : sound.FadeDuration);

                if (outDur > 0f && _musicSource != null && (_musicSource.isPlaying || _musicSource.volume > 0.01f))
                    await FadeToAsync(_musicSource, 0f, outDur, ct);

                if (_musicSource != null)
                    _musicSource.Stop();

                StartMusicImmediate(sound, pitch, pitchRandomRange, fadeIn: true);
            }
            catch (System.OperationCanceledException)
            {
            }
        }

        private void StartMusicImmediate(
            SoundDefinition sound,
            float? pitch,
            float? pitchRandomRange,
            bool fadeIn)
        {
            CancelMusicFade();

            var clip = sound.Clips[Random.Range(0, sound.Clips.Length)];
            _musicSource.outputAudioMixerGroup = sound.MixerGroup;
            _musicSource.loop = sound.Loop;
            _musicSource.clip = clip;
            var resolvedPitch = ResolvePitch(sound, pitch, pitchRandomRange);
            _musicSource.pitch = resolvedPitch;
            _musicPitchTarget = resolvedPitch;
            _musicPitchVelocity = 0f;
            _musicSoundId = sound.Id;
            _musicFadeDuration = sound.EnableFade ? sound.FadeDuration : 0f;
            _musicFadeOutDuration = sound.FadeOutDuration > 0f ? sound.FadeOutDuration : _musicFadeDuration;
            _musicFadeCts = new CancellationTokenSource();

            if (fadeIn && sound.EnableFade && sound.FadeDuration > 0f)
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
            _musicVolumeBeforePause = 1f;
        }

        private SfxVoice AcquireVoiceForSound(SoundDefinition sound)
        {
            // Проверяем/создаём пул для этого soundId
            if (!_soundVoicePools.TryGetValue(sound.Id, out var pool))
            {
                pool = new List<SfxVoice>(sound.MaxVoices);
                _soundVoicePools[sound.Id] = pool;

                // Создаём голоса для этого звука
                for (int i = 0; i < sound.MaxVoices; i++)
                {
                    var source = CreateSource($"{sound.Id}_{i}", transform);
                    source.playOnAwake = false;
                    source.spatialBlend = 0f;
                    pool.Add(new SfxVoice(source));
                }
            }

            // Ищем свободный голос в пуле этого звука
            foreach (var voice in pool)
            {
                if (!voice.Source.isPlaying)
                    return voice;
            }

            // Все голоса заняты — не воспроизводим (лимит maxVoices достигнут)
            int activeCount = 0;
            foreach (var v in pool)
            {
                if (v.Source.isPlaying) activeCount++;
            }
            Debug.LogWarning($"[AudioManager] Sound '{sound.Id}' max voices ({sound.MaxVoices}) reached ({activeCount} active). Ignoring play request.");
            return null;
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
                _musicPitchTarget = 1f;
                _musicPitchVelocity = 0f;
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
