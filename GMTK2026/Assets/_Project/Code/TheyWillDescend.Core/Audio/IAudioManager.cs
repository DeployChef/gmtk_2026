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
        System.Collections.Generic.IEnumerable<AudioClip> EnumerateClips();
        void WarmupClip(AudioClip clip);

        void SetMusicVolume(float volume);
        void SetSfxVolume(float volume);
        float GetMusicVolume();
        float GetSfxVolume();
    }
}
