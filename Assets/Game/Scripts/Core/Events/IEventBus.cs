using System;

namespace Game.Core.Events
{
    /// <summary>
    /// Typed publish/subscribe event bus. Strongly typed events avoid
    /// stringly-typed messaging and remove boxing of struct payloads.
    /// One instance is registered with the ServiceLocator at bootstrap;
    /// gameplay systems subscribe in OnEnable and unsubscribe in OnDisable.
    /// </summary>
    public interface IEventBus
    {
        void Subscribe<T>(Action<T> handler) where T : struct;
        void Unsubscribe<T>(Action<T> handler) where T : struct;
        void Publish<T>(T payload) where T : struct;
    }
}
