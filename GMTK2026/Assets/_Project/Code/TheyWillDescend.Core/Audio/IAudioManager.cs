using UnityEngine;

namespace TheyWillDescend.Core.Audio
{
    public interface IAudioManager
    {
        void Play(string soundId, float? pitch = null, float? pitchRandomRange = null);
        void Stop(string soundId);
        void StopMusic();
        void PauseMusic();
        void ResumeMusic();
        void StopAll();
        bool IsPlaying(string soundId);
        bool IsMusicPaused { get; }
        bool HasMusicClip { get; }
        bool HasAmbientClip { get; }
        void PlayAmbient(string soundId, float? pitch = null, float? pitchRandomRange = null);
        void StopAmbient();
        System.Collections.Generic.IEnumerable<AudioClip> EnumerateClips();
        void WarmupClip(AudioClip clip);

        /// <summary>Instant music playback rate (1 = normal, 1.5 = +50%).</summary>
        void SetMusicPitch(float pitch);

        void SetMusicVolume(float volume);
        void SetSfxVolume(float volume);
        float GetMusicVolume();
        float GetSfxVolume();
    }
}
