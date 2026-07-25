using System;
using TheyWillDescend.Core;
using TheyWillDescend.Core.Bus;
using TheyWillDescend.Core.Bus.Events;
using UnityEngine;
using VContainer;

namespace TheyWillDescend.UI.Session
{
    /// <summary>
    /// Shows Win/Lose canvases when GameWonEvent / GameLostEvent are published.
    /// </summary>
    public sealed class ResultScreenController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup winCanvas;
        [SerializeField] private CanvasGroup loseCanvas;
        [SerializeField] private UnityEngine.UI.Button winRestartButton;
        [SerializeField] private UnityEngine.UI.Button loseRestartButton;

        private IGameEventBus _bus;
        private IDisposable _wonSub;
        private IDisposable _lostSub;
        private IDisposable _runStartedSub;

        [Inject]
        public void Construct(IGameEventBus bus, IGameDirector director)
        {
            _bus = bus;

            if (winRestartButton != null)
                winRestartButton.onClick.AddListener(() => director.RestartAsync());

            if (loseRestartButton != null)
                loseRestartButton.onClick.AddListener(() => director.RestartAsync());

            _wonSub = _bus.Subscribe<GameWonEvent>(_ => Show(winCanvas));
            _lostSub = _bus.Subscribe<GameLostEvent>(_ => Show(loseCanvas));
            _runStartedSub = _bus.Subscribe<RunStartedEvent>(_ =>
            {
                Hide(winCanvas);
                Hide(loseCanvas);
            });
        }

        private void Awake()
        {
            Hide(winCanvas);
            Hide(loseCanvas);
        }

        private void OnDestroy()
        {
            _wonSub?.Dispose();
            _lostSub?.Dispose();
            _runStartedSub?.Dispose();
        }

        private static void Show(CanvasGroup cg)
        {
            if (cg == null)
                return;
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        private static void Hide(CanvasGroup cg)
        {
            if (cg == null)
                return;
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
    }
}
