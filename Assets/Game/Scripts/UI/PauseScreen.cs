using Game.Core.Events;
using Game.Core.Services;
using Game.Core.States;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Pause overlay. Listens for <see cref="GamePausedEvent"/> to toggle
    /// visibility, and calls the state machine to enter/exit
    /// <see cref="PausedState"/> when the resume button is pressed.
    /// </summary>
    public sealed class PauseScreen : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        private IEventBus _bus;
        private GameStateMachine _fsm;

        public void Bind(IEventBus bus, GameStateMachine fsm)
        {
            _bus = bus;
            _fsm = fsm;
            _bus.Subscribe<GamePausedEvent>(OnPaused);
            if (_root != null) _root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_bus != null) _bus.Unsubscribe<GamePausedEvent>(OnPaused);
        }

        private void OnPaused(GamePausedEvent ev)
        {
            if (_root != null) _root.SetActive(ev.Paused);
        }

        // Wire to UI buttons:
        public void OnResumeClicked() => _fsm?.ChangeState<PlayingState>();
        public void OnQuitClicked()   => Application.Quit();
    }
}
