using Game.Balls;
using Game.Chain;
using Game.Core.Pooling;
using UnityEngine;

namespace Game.Projectile
{
    /// <summary>
    /// Flying shot. Distinct from <see cref="Game.Chain.ChainBall"/> by design:
    /// projectile logic and chain logic share zero state, so future projectile
    /// types (homing, bomb, area-splash) can extend this class without ever
    /// touching the chain code.
    ///
    /// Physics setup expected on the prefab:
    /// - Rigidbody: useGravity=false, isKinematic=false, interpolation=Interpolate,
    ///   collisionDetectionMode=Continuous (or ContinuousDynamic) to prevent
    ///   tunnelling through the chain at high muzzle speeds.
    /// - Collider: isTrigger=true. Collisions with chain balls are detected
    ///   via OnTriggerEnter; the chain colliders are also triggers, so the
    ///   physics layer matrix must allow trigger overlap between
    ///   "Projectile" and "ChainBall" layers, and we filter inside the callback.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public sealed class ProjectileBall : MonoBehaviour, IPoolable
    {
        [SerializeField] private BallView _view;
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private float _maxLifetime = 4f;

        private ChainController _chain;
        private IObjectPool     _pool;
        private BallColor       _color;
        private float           _lifetime;
        private bool            _consumed;

        private void Reset()
        {
            _view      = GetComponentInChildren<BallView>();
            _rigidbody = GetComponent<Rigidbody>();
        }

        public void Launch(BallColor color, BallDefinition definition, Vector3 velocity, ChainController chain, IObjectPool pool)
        {
            _color    = color;
            _chain    = chain;
            _pool     = pool;
            _lifetime = 0f;
            _consumed = false;

            if (_view != null && definition != null) _view.Apply(definition);
            _rigidbody.linearVelocity     = velocity;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        private void FixedUpdate()
        {
            _lifetime += Time.fixedDeltaTime;
            if (_lifetime >= _maxLifetime) Recycle();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_consumed) return;
            // Filter: we only care about colliders that carry a chain ball back-ref.
            var reference = other.GetComponentInParent<ChainBallReference>();
            if (reference == null || reference.Ball == null) return;

            _consumed = true; // guard against multiple triggers on the same frame
            // Capture world position now — once we recycle, this transform may be moved.
            var hitWorld = transform.position;

            // Hand off to the chain. The chain owns the entire insertion +
            // match cascade pipeline; we just deliver colour + impact data.
            _chain.TryInsertFromProjectile(reference.Ball, _color, hitWorld);
            Recycle();
        }

        private void Recycle()
        {
            if (_pool != null) _pool.Release(gameObject);
            else gameObject.SetActive(false);
        }

        public void OnSpawned()
        {
            _consumed = false;
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
        }

        public void OnDespawned()
        {
            _chain = null;
            _pool  = null;
        }
    }
}
