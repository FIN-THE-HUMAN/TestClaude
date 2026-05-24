using System;
using System.Collections.Generic;

namespace Game.Core.Services
{
    /// <summary>
    /// Minimal service locator used as an injection seam for systems that
    /// cannot be wired via SerializeField (event bus, pool, state machine).
    /// Prefer SerializeField references for MonoBehaviour-to-MonoBehaviour
    /// links; reach for the locator only when the dependency is created at
    /// runtime by <c>GameBootstrap</c>.
    ///
    /// The locator is intentionally *not* a singleton. <see cref="GameBootstrap"/>
    /// owns an instance and disposes/recreates it across scene reloads, which
    /// keeps test scenes free of leftover state.
    /// </summary>
    public sealed class ServiceLocator
    {
        public static ServiceLocator Current { get; private set; }

        private readonly Dictionary<Type, object> _services = new();

        public static void SetCurrent(ServiceLocator locator) => Current = locator;
        public static void Clear() => Current = null;

        public void Register<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            _services[typeof(T)] = service;
        }

        public T Resolve<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service))
                return (T)service;
            throw new InvalidOperationException($"Service of type {typeof(T).Name} not registered.");
        }

        public bool TryResolve<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var raw))
            {
                service = (T)raw;
                return true;
            }
            service = null;
            return false;
        }
    }
}
