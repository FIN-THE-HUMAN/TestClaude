using System;
using System.Collections.Generic;

namespace Game.Core.States
{
    /// <summary>
    /// Type-keyed FSM. States are registered once at bootstrap and addressed
    /// by their concrete type — this avoids enum drift as new states get added
    /// and gives compile-time safety for ChangeState&lt;T&gt;().
    /// </summary>
    public sealed class GameStateMachine
    {
        private readonly Dictionary<Type, IGameState> _states = new();
        public IGameState Current { get; private set; }
        public Type CurrentType { get; private set; }

        public event Action<Type, Type> StateChanged; // (from, to)

        public void Register<T>(T state) where T : class, IGameState
        {
            _states[typeof(T)] = state;
        }

        public void ChangeState<T>() where T : class, IGameState
        {
            if (!_states.TryGetValue(typeof(T), out var next))
                throw new InvalidOperationException($"State {typeof(T).Name} not registered.");

            if (next == Current) return; // idempotent re-entry guard

            var fromType = CurrentType;
            Current?.OnExit();
            Current = next;
            CurrentType = typeof(T);
            Current.OnEnter();
            StateChanged?.Invoke(fromType, typeof(T));
        }

        public bool IsInState<T>() where T : class, IGameState => CurrentType == typeof(T);
    }
}
