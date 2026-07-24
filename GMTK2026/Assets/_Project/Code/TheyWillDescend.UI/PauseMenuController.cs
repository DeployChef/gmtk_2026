using System;
using Cysharp.Threading.Tasks;
using TheyWillDescend.Core;
using TheyWillDescend.Core.Audio;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;

namespace TheyWillDescend.UI
{
    /// <summary>
    /// Pause menu: Esc to toggle, restart/resume buttons, music/sfx volume sliders.
    /// Place on Root scene — survives Game scene reloads.
    /// </summary>
    public sealed class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup pauseGroup;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        private IGameDirector _director;
        private IAudioManager _audio;
        private bool _paused;
        private bool _restarting;

        [Inject]
        public void Construct(IGameDirector director, IAudioManager audio)
        {
            _director = director;
            _audio = audio;
        }

        private void Awake()
        {
            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);

            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResumeClicked);

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.minValue = 0f;
                musicVolumeSlider.maxValue = 1f;
                musicVolumeSlider.value = _audio != null ? _audio.GetMusicVolume() : 0.8f;
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.minValue = 0f;
                sfxVolumeSlider.maxValue = 1f;
                sfxVolumeSlider.value = _audio != null ? _audio.GetSfxVolume() : 0.8f;
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            }

            SetVisible(false);
        }

        private void OnDestroy()
        {
            if (restartButton != null)
                restartButton.onClick.RemoveListener(OnRestartClicked);

            if (resumeButton != null)
                resumeButton.onClick.RemoveListener(OnResumeClicked);

            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
        }

        private void Update()
        {
            if (Keyboard.current != null
                && Keyboard.current.escapeKey.wasPressedThisFrame
                && !_restarting)
                TogglePause();
        }

        private void TogglePause()
        {
            if (_paused)
                Resume();
            else
                Pause();
        }

        private void Pause()
        {
            _paused = true;
            Time.timeScale = 0f;
            SetVisible(true);

            if (_audio != null)
                _audio.PauseMusic();
        }

        private void Resume()
        {
            _paused = false;
            Time.timeScale = 1f;
            SetVisible(false);

            if (_audio != null)
                _audio.ResumeMusic();
        }

        private void OnRestartClicked()
        {
            if (_restarting)
                return;

            _restarting = true;
            Resume();
            RestartAsync().Forget();
        }

        private async UniTaskVoid RestartAsync()
        {
            try
            {
                if (_director != null)
                    await _director.RestartAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[PauseMenuController] Restart failed: {e}");
            }
            finally
            {
                _restarting = false;
            }
        }

        private void OnResumeClicked()
        {
            Resume();
        }

        private void OnMusicVolumeChanged(float value)
        {
            if (_audio != null)
                _audio.SetMusicVolume(value);
        }

        private void OnSfxVolumeChanged(float value)
        {
            if (_audio != null)
                _audio.SetSfxVolume(value);
        }

        private void SetVisible(bool visible)
        {
            if (pauseGroup != null)
            {
                pauseGroup.alpha = visible ? 1f : 0f;
                pauseGroup.interactable = visible;
                pauseGroup.blocksRaycasts = visible;
            }
            else
            {
                gameObject.SetActive(visible);
            }
        }
    }
}
