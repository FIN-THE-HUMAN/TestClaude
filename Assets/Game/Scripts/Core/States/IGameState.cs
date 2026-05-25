namespace Game.Core.States
{
    /// <summary>
    /// Lifecycle contract for a game state. The state machine never calls
    /// Update on states — gameplay systems drive themselves and consult
    /// the FSM via <see cref="GameStateMachine.IsInState{T}"/> when they
    /// need to gate behaviour (e.g. shooter input ignores clicks while paused).
    /// This keeps the FSM decoupled from MonoBehaviour update order.
    /// </summary>
    public interface IGameState
    {
        void OnEnter();
        void OnExit();
    }
}
