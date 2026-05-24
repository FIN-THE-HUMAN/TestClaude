using System;
using System.Collections.Generic;

namespace Game.Core.Events
{
    /// <summary>
    /// Default <see cref="IEventBus"/>. Stores delegates in a per-type bucket.
    /// Handlers are copied to a local snapshot before invocation so handlers
    /// may safely Subscribe/Unsubscribe while a Publish is in flight.
    /// Generic payloads must be structs to keep allocations to zero in the
    /// common case (the delegate snapshot copy is the only allocation, and
    /// only on Publish when at least one subscriber exists).
    /// </summary>
    public sealed class EventBus : IEventBus
    {
        // Stored as Delegate (not Action<T>) so a single dictionary can host
        // every event type without per-type wrapper classes.
        private readonly Dictionary<Type, Delegate> _handlers = new(32);

        public void Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            if (_handlers.TryGetValue(typeof(T), out var existing))
                _handlers[typeof(T)] = Delegate.Combine(existing, handler);
            else
                _handlers[typeof(T)] = handler;
        }

        public void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) return;
            if (!_handlers.TryGetValue(typeof(T), out var existing)) return;

            var remaining = Delegate.Remove(existing, handler);
            if (remaining == null) _handlers.Remove(typeof(T));
            else _handlers[typeof(T)] = remaining;
        }

        public void Publish<T>(T payload) where T : struct
        {
            if (!_handlers.TryGetValue(typeof(T), out var existing)) return;

            // Snapshot to a strongly-typed delegate so handler exceptions
            // do not interrupt remaining subscribers, and mutation of the
            // handler list during dispatch does not invalidate iteration.
            var action = (Action<T>)existing;
            var invocations = action.GetInvocationList();
            for (int i = 0; i < invocations.Length; i++)
            {
                try { ((Action<T>)invocations[i]).Invoke(payload); }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }
        }
    }
}
