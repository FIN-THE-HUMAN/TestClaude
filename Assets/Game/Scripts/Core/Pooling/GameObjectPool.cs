using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Pooling
{
    /// <summary>
    /// Prefab-keyed Unity GameObject pool.
    ///
    /// Design notes:
    /// - Pools are bucketed by the *prefab* InstanceID, not by component type:
    ///   different prefabs sharing the same component (e.g. red vs. blue ball
    ///   sharing BallView) must not be drawn from the same bucket.
    /// - A reverse map (instance → prefab) lets <see cref="Release"/> find the
    ///   correct bucket without callers having to remember origin.
    /// - We deactivate before re-parenting on release to avoid OnEnable firing
    ///   while still attached to the world.
    /// - This pool does *not* try to handle destroyed instances; consumers are
    ///   expected to call Release deterministically. Scene unloads should call
    ///   <see cref="Clear"/>.
    /// </summary>
    public sealed class GameObjectPool : MonoBehaviour, IObjectPool
    {
        private readonly Dictionary<int, Stack<GameObject>> _buckets = new();
        private readonly Dictionary<int, GameObject> _instanceToPrefab = new();
        [SerializeField] private Transform _inactiveRoot;

        private void Awake()
        {
            if (_inactiveRoot == null)
            {
                var go = new GameObject("PooledInactive");
                go.transform.SetParent(transform, false);
                go.SetActive(false); // hidden parent keeps Hierarchy tidy
                _inactiveRoot = go.transform;
            }
        }

        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            var key = prefab.GetInstanceID();
            if (!_buckets.TryGetValue(key, out var stack))
            {
                stack = new Stack<GameObject>(8);
                _buckets[key] = stack;
            }

            GameObject instance;
            if (stack.Count > 0)
            {
                instance = stack.Pop();
                var t = instance.transform;
                t.SetParent(parent, false);
                t.SetPositionAndRotation(position, rotation);
                instance.SetActive(true);
            }
            else
            {
                instance = Object.Instantiate(prefab, position, rotation, parent);
                _instanceToPrefab[instance.GetInstanceID()] = prefab;
            }

            NotifySpawned(instance);
            return instance;
        }

        public T Get<T>(T prefabComponent, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
        {
            var instance = Get(prefabComponent.gameObject, position, rotation, parent);
            return instance.GetComponent<T>();
        }

        public void Release(GameObject instance)
        {
            if (instance == null) return;

            NotifyDespawned(instance);

            if (!_instanceToPrefab.TryGetValue(instance.GetInstanceID(), out var prefab))
            {
                // Foreign instance — destroy rather than orphan it in the pool.
                Object.Destroy(instance);
                return;
            }

            instance.SetActive(false);
            instance.transform.SetParent(_inactiveRoot, false);

            var key = prefab.GetInstanceID();
            if (!_buckets.TryGetValue(key, out var stack))
            {
                stack = new Stack<GameObject>(8);
                _buckets[key] = stack;
            }
            stack.Push(instance);
        }

        public void Prewarm(GameObject prefab, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var instance = Object.Instantiate(prefab, _inactiveRoot);
                _instanceToPrefab[instance.GetInstanceID()] = prefab;
                instance.SetActive(false);
                var key = prefab.GetInstanceID();
                if (!_buckets.TryGetValue(key, out var stack))
                {
                    stack = new Stack<GameObject>(count);
                    _buckets[key] = stack;
                }
                stack.Push(instance);
            }
        }

        public void Clear()
        {
            foreach (var kvp in _buckets)
                foreach (var go in kvp.Value)
                    if (go != null) Object.Destroy(go);
            _buckets.Clear();
            _instanceToPrefab.Clear();
        }

        private static readonly List<IPoolable> s_poolableBuffer = new(8);

        private static void NotifySpawned(GameObject instance)
        {
            instance.GetComponentsInChildren(true, s_poolableBuffer);
            for (int i = 0; i < s_poolableBuffer.Count; i++) s_poolableBuffer[i].OnSpawned();
        }
        private static void NotifyDespawned(GameObject instance)
        {
            instance.GetComponentsInChildren(true, s_poolableBuffer);
            for (int i = 0; i < s_poolableBuffer.Count; i++) s_poolableBuffer[i].OnDespawned();
        }
    }
}
