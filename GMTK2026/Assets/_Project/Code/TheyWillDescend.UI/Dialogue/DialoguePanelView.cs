using System;
using TheyWillDescend.Core;
using TheyWillDescend.Core.Audio;
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
    /// Slides up from below on start, slides down on end (unscaled time — works while paused).
    /// </summary>
    public sealed class DialoguePanelView : MonoBehaviour, IDialogueService
    {
        private static readonly object PauseKey = new();

        private enum SlideMode
        {
            None,
            In,
            Out
        }

        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private RectTransform slideRoot;
        [SerializeField] private Image portraitImage;
        [Tooltip("Fallback when a dialogue line has no portrait (same as Intro: DialogMan).")]
        [SerializeField] private Sprite defaultPortrait;
        [SerializeField] private TMP_Text bodyText;
        [Tooltip("Click target for skip / advance (usually the whole dialogue box).")]
        [SerializeField] private Button advanceButton;
        [SerializeField] private float charsPerSecond = 40f;
        [SerializeField] private string typeSoundId = AudioCatalog.Ids.Dialog;
        [SerializeField] [Range(0f, 0.5f)] private float typePitchRandom = 0.2f;
        [Header("Slide")]
        [SerializeField] private float slideDuration = 0.35f;
        [Tooltip("How far below the resting position the panel starts/ends (canvas units).")]
        [SerializeField] private float slideDistance = 1200f;
        [SerializeField] private AnimationCurve slideInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve slideOutCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private IGameEventBus _bus;
        private IGameplayTimePause _timePause;
        private IAudioManager _audio;
        private DialogueDefinition _current;
        private Action _onComplete;
        private string _lineText = string.Empty;
        private int _lineIndex;
        private int _visibleChars;
        private float _charAccumulator;
        private bool _lineComplete;
        private bool _contentActive;
        private bool _closing;
        private Vector2 _shownPos;
        private SlideMode _slideMode;
        private float _slideElapsed;
        private Vector2 _slideFrom;
        private Vector2 _slideTo;
        private DialogueDefinition _pendingEndDialogue;
        private Action _pendingEndCallback;

        public bool IsPlaying { get; private set; }

        [Inject]
        public void Construct(IGameEventBus bus, IGameplayTimePause timePause, IAudioManager audio)
        {
            _bus = bus;
            _timePause = timePause;
            _audio = audio;
        }

        private void Awake()
        {
            if (slideRoot == null)
                slideRoot = transform as RectTransform;

            if (slideRoot != null)
                _shownPos = slideRoot.anchoredPosition;

            if (advanceButton != null)
                advanceButton.onClick.AddListener(OnAdvancePressed);

            SnapHidden();
            SetVisible(false);
            if (bodyText != null)
                bodyText.text = string.Empty;
        }

        private void OnDisable()
        {
            // Avoid baking the off-screen pose into the scene when leaving Play Mode.
            if (!Application.isPlaying && slideRoot != null)
                slideRoot.anchoredPosition = _shownPos;
        }

        private void OnDestroy()
        {
            if (advanceButton != null)
                advanceButton.onClick.RemoveListener(OnAdvancePressed);

            if (IsPlaying)
                ForceStopInternal(invokeCallback: false, publishEnded: false);
        }

        private void Update()
        {
            TickSlide();

            if (!IsPlaying || !_contentActive || _closing)
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
            _closing = false;
            _contentActive = false;
            IsPlaying = true;

            _timePause?.Acquire(PauseKey);
            ShowLine(0);
            BeginSlideIn();
            return true;
        }

        private void OnAdvancePressed()
        {
            if (!IsPlaying || !_contentActive || _closing)
                return;

            if (!_lineComplete)
            {
                RevealFullLine();
                return;
            }

            var next = _lineIndex + 1;
            if (next >= _current.Lines.Length)
            {
                BeginClose();
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

            if (portraitImage != null)
            {
                var sprite = line.Portrait != null
                    ? line.Portrait
                    : _current != null && _current.DefaultPortrait != null
                        ? _current.DefaultPortrait
                        : defaultPortrait;

                if (sprite != null)
                {
                    portraitImage.sprite = sprite;
                    portraitImage.enabled = true;
                }
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
            var previous = _visibleChars;
            _visibleChars = Mathf.Min(_lineText.Length, _visibleChars + add);
            PlayTypeSounds(previous, _visibleChars);

            if (bodyText != null)
                bodyText.text = _lineText.Substring(0, _visibleChars);

            if (_visibleChars >= _lineText.Length)
                _lineComplete = true;
        }

        private void PlayTypeSounds(int fromExclusive, int toInclusive)
        {
            if (_audio == null || string.IsNullOrEmpty(typeSoundId) || string.IsNullOrEmpty(_lineText))
                return;

            for (var i = fromExclusive; i < toInclusive; i++)
            {
                if (char.IsWhiteSpace(_lineText[i]))
                    continue;

                _audio.Play(typeSoundId, pitchRandomRange: typePitchRandom);
            }
        }

        private void RevealFullLine()
        {
            _visibleChars = _lineText.Length;
            _charAccumulator = 0f;
            _lineComplete = true;
            if (bodyText != null)
                bodyText.text = _lineText;
        }

        private void BeginSlideIn()
        {
            if (slideRoot == null)
            {
                SetVisible(true);
                _contentActive = true;
                return;
            }

            CacheShownPos();
            SnapHidden();
            SetVisible(true);
            StartSlide(GetHiddenPos(), _shownPos, SlideMode.In);
        }

        private void BeginClose()
        {
            if (_closing)
                return;

            _closing = true;
            _contentActive = false;
            _pendingEndDialogue = _current;
            _pendingEndCallback = _onComplete;

            if (slideRoot == null)
            {
                FinishClose(_pendingEndDialogue, _pendingEndCallback);
                return;
            }

            CacheShownPos();
            StartSlide(slideRoot.anchoredPosition, GetHiddenPos(), SlideMode.Out);
        }

        private void StartSlide(Vector2 from, Vector2 to, SlideMode mode)
        {
            _slideMode = mode;
            _slideElapsed = 0f;
            _slideFrom = from;
            _slideTo = to;
            if (slideRoot != null)
                slideRoot.anchoredPosition = from;
        }

        private void TickSlide()
        {
            if (_slideMode == SlideMode.None || slideRoot == null)
                return;

            var duration = Mathf.Max(0.01f, slideDuration);
            _slideElapsed += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(_slideElapsed / duration);
            var curve = _slideMode == SlideMode.In ? slideInCurve : slideOutCurve;
            var eased = curve != null && curve.length > 0 ? Mathf.Clamp01(curve.Evaluate(t)) : t;
            slideRoot.anchoredPosition = Vector2.LerpUnclamped(_slideFrom, _slideTo, eased);

            if (t < 1f)
                return;

            slideRoot.anchoredPosition = _slideTo;
            var finished = _slideMode;
            _slideMode = SlideMode.None;

            if (finished == SlideMode.In)
            {
                if (IsPlaying && !_closing)
                    _contentActive = true;
                return;
            }

            FinishClose(_pendingEndDialogue, _pendingEndCallback);
        }

        private void FinishClose(DialogueDefinition dialogue, Action callback)
        {
            _pendingEndDialogue = null;
            _pendingEndCallback = null;
            ForceStopInternal(invokeCallback: false, publishEnded: false);
            callback?.Invoke();
            if (dialogue != null)
                _bus?.Publish(new DialogueEndedEvent(dialogue));
        }

        private void ForceStopInternal(bool invokeCallback, bool publishEnded)
        {
            _slideMode = SlideMode.None;

            var dialogue = _current;
            var callback = _onComplete;

            IsPlaying = false;
            _closing = false;
            _contentActive = false;
            _current = null;
            _onComplete = null;
            _pendingEndDialogue = null;
            _pendingEndCallback = null;
            _lineText = string.Empty;
            _lineIndex = 0;
            _visibleChars = 0;
            _charAccumulator = 0f;
            _lineComplete = false;

            if (bodyText != null)
                bodyText.text = string.Empty;

            SnapHidden();
            SetVisible(false);
            _timePause?.Release(PauseKey);

            if (invokeCallback)
                callback?.Invoke();
            if (publishEnded && dialogue != null)
                _bus?.Publish(new DialogueEndedEvent(dialogue));
        }

        private void CacheShownPos()
        {
            if (slideRoot == null)
                return;

            // Keep the designed resting Y; refresh X in case layout moved.
            _shownPos = new Vector2(slideRoot.anchoredPosition.x, _shownPos.y);
        }

        private Vector2 GetHiddenPos()
        {
            var distance = Mathf.Max(200f, slideDistance);
            return new Vector2(_shownPos.x, _shownPos.y - distance);
        }

        private void SnapHidden()
        {
            if (slideRoot == null)
                return;

            slideRoot.anchoredPosition = GetHiddenPos();
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
