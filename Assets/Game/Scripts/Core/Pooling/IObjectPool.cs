using UnityEngine;

namespace Game.Core.Pooling
{
    /// <summary>
    /// Abstraction over the concrete pool. Gameplay code depends on this
    /// interface, so the pool implementation can be swapped (e.g. for a
    /// MultiScenePool, or Unity 6 UnityEngine.Pool.ObjectPool wrapper)
    /// without rippling changes through callers.
    /// </summary>
    public interface IObjectPool
    {
        GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null);
        T Get<T>(T prefabComponent, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component;
        void Release(GameObject instance);
        void Prewarm(GameObject prefab, int count);
        void Clear();
    }
}
