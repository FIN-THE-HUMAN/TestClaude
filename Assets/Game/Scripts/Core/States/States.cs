using Game.Core.Events;
using UnityEngine;

namespace Game.Core.States
{
    // All concrete states live in one file because each implementation is tiny:
    // the FSM is intentionally thin and uses events for cross-system signalling.

    public sealed class BootstrapState : IGameState
    {
        public void OnEnter() { /* Bootstrap is handled by GameBootstrap before any state is entered. */ }
        public void OnExit()  { }
    }

    public sealed class PlayingState : IGameState
    {
        private readonly IEventBus _bus;
        public PlayingState(IEventBus bus) { _bus = bus; }
        public void OnEnter() { Time.timeScale = 1f; _bus.Publish(new GamePausedEvent(false)); }
        public void OnExit()  { }
    }

    public sealed class PausedState : IGameState
    {
        private readonly IEventBus _bus;
        public PausedState(IEventBus bus) { _bus = bus; }
        // Time scale is the simplest authoritative gate: physics, animations
        // and input-based intents all observe it. Systems that must keep
        // updating (UI tween) should use unscaled time explicitly.
        public void OnEnter() { Time.timeScale = 0f; _bus.Publish(new GamePausedEvent(true)); }
        public void OnExit()  { Time.timeScale = 1f; _bus.Publish(new GamePausedEvent(false)); }
    }

    public sealed class WinState : IGameState
    {
        private readonly IEventBus _bus;
        public WinState(IEventBus bus) { _bus = bus; }
        public void OnEnter() { Time.timeScale = 0f; _bus.Publish(new GameWonEvent()); }
        public void OnExit()  { Time.timeScale = 1f; }
    }

    public sealed class LoseState : IGameState
    {
        private readonly IEventBus _bus;
        public LoseState(IEventBus bus) { _bus = bus; }
        public void OnEnter() { Time.timeScale = 0f; _bus.Publish(new GameLostEvent()); }
        public void OnExit()  { Time.timeScale = 1f; }
    }
}
