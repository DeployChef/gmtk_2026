using System;
using TheyWillDescend.Core;
using TheyWillDescend.Core.Bus;
using TheyWillDescend.Core.Bus.Events;
using TheyWillDescend.Core.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;

namespace TheyWillDescend.UI.Dialogue
{
    /// <summary>
    /// Classic dialogue box: portrait + typewriter text. Advance via click or Space.
    /// While playing: gameplay time is paused; new Play calls are ignored.
    /// </summary>
    public sealed class DialoguePanelView : MonoBehaviour, IDialogueService
    {
        private static readonly object PauseKey = new();

        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text bodyText;
        [Tooltip("Click target for skip / advance (usually the whole dialogue box).")]
        [SerializeField] private Button advanceButton;
        [SerializeField] private float charsPerSecond = 40f;

        private IGameEventBus _bus;
        private IGameplayTimePause _timePause;
        private DialogueDefinition _current;
        private Action _onComplete;
        private string _lineText = string.Empty;
        private int _lineIndex;
        private int _visibleChars;
        private float _charAccumulator;
        private bool _lineComplete;

        public bool IsPlaying { get; private set; }

        [Inject]
        public void Construct(IGameEventBus bus, IGameplayTimePause timePause)
        {
            _bus = bus;
            _timePause = timePause;
        }

        private void Awake()
        {
            if (advanceButton != null)
                advanceButton.onClick.AddListener(OnAdvancePressed);

            SetVisible(false);
            if (bodyText != null)
                bodyText.text = string.Empty;
        }

        private void OnDestroy()
        {
            if (advanceButton != null)
                advanceButton.onClick.RemoveListener(OnAdvancePressed);

            if (IsPlaying)
                ForceStopInternal();
        }

        private void Update()
        {
            if (!IsPlaying)
                return;

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                OnAdvancePressed();

            if (_lineComplete)
                return;

            TickTypewriter();
        }

        public bool TryPlay(DialogueDefinition dialogue, Action onComplete = null)
        {
            if (IsPlaying)
                return false;

            if (dialogue == null || dialogue.Lines.Length == 0)
                return false;

            _current = dialogue;
            _onComplete = onComplete;
            _lineIndex = 0;
            IsPlaying = true;

            _timePause?.Acquire(PauseKey);
            SetVisible(true);
            ShowLine(0);
            return true;
        }

        private void OnAdvancePressed()
        {
            if (!IsPlaying)
                return;

            if (!_lineComplete)
            {
                RevealFullLine();
                return;
            }

            var next = _lineIndex + 1;
            if (next >= _current.Lines.Length)
            {
                CompleteDialogue();
                return;
            }

            _lineIndex = next;
            ShowLine(next);
        }

        private void ShowLine(int index)
        {
            var line = _current.Lines[index];
            _lineText = line.Text ?? string.Empty;
            _visibleChars = 0;
            _charAccumulator = 0f;
            _lineComplete = string.IsNullOrEmpty(_lineText);

            if (line.Portrait != null && portraitImage != null)
            {
                portraitImage.sprite = line.Portrait;
                portraitImage.enabled = true;
            }

            if (bodyText != null)
                bodyText.text = _lineComplete ? _lineText : string.Empty;
        }

        private void TickTypewriter()
        {
            var speed = Mathf.Max(1f, charsPerSecond);
            _charAccumulator += Time.unscaledDeltaTime * speed;

            var add = (int)_charAccumulator;
            if (add <= 0)
                return;

            _charAccumulator -= add;
            _visibleChars = Mathf.Min(_lineText.Length, _visibleChars + add);

            if (bodyText != null)
                bodyText.text = _lineText.Substring(0, _visibleChars);

            if (_visibleChars >= _lineText.Length)
                _lineComplete = true;
        }

        private void RevealFullLine()
        {
            _visibleChars = _lineText.Length;
            _charAccumulator = 0f;
            _lineComplete = true;
            if (bodyText != null)
                bodyText.text = _lineText;
        }

        private void CompleteDialogue()
        {
            var dialogue = _current;
            var callback = _onComplete;
            ForceStopInternal();

            callback?.Invoke();
            if (dialogue != null)
                _bus?.Publish(new DialogueEndedEvent(dialogue));
        }

        private void ForceStopInternal()
        {
            IsPlaying = false;
            _current = null;
            _onComplete = null;
            _lineText = string.Empty;
            _lineIndex = 0;
            _visibleChars = 0;
            _charAccumulator = 0f;
            _lineComplete = false;

            if (bodyText != null)
                bodyText.text = string.Empty;

            SetVisible(false);
            _timePause?.Release(PauseKey);
        }

        private void SetVisible(bool visible)
        {
            if (panelGroup != null)
            {
                panelGroup.alpha = visible ? 1f : 0f;
                panelGroup.interactable = visible;
                panelGroup.blocksRaycasts = visible;
            }
            else
            {
                gameObject.SetActive(visible);
            }
        }
    }
}
