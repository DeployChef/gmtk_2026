using UnityEngine;
using UnityEngine.UI;

namespace TheyWillDescend.UI
{
    /// <summary>
    /// Simple button hook that finds PauseMenuController (on Root scene) and toggles pause.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class PauseButtonHook : MonoBehaviour
    {
        private Button _button;
        private PauseMenuController _controller;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            if (_controller == null)
                _controller = FindFirstObjectByType<PauseMenuController>();

            _controller?.TogglePause();
        }
    }
}
