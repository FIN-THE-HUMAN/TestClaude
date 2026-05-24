namespace Game.Core.Pooling
{
    /// <summary>
    /// Optional hook implemented by pooled components. <see cref="GameObjectPool"/>
    /// invokes <see cref="OnSpawned"/> after Get() and <see cref="OnDespawned"/>
    /// before the GameObject is deactivated — this is the right place for state
    /// reset (rigidbody velocity, listeners, particle replays) rather than the
    /// Awake/OnEnable pair, which fires inconsistently across pool lifecycles.
    /// </summary>
    public interface IPoolable
    {
        void OnSpawned();
        void OnDespawned();
    }
}
