using Game.Balls;
using Game.Chain;
using Game.Core.Events;
using Game.Core.Pooling;
using Game.Core.States;
using Game.Level;
using Game.Projectile;
using UnityEngine;

namespace Game.Shooter
{
    /// <summary>
    /// The player-controlled launcher. Responsibilities:
    /// - Track the mouse horizontally and clamp to <see cref="_minX"/>/<see cref="_maxX"/>.
    /// - Aim is fixed: <see cref="_muzzle"/>.up = +Y. The shooter does not rotate.
    /// - Fire on left-click; swap current ↔ next on right-click.
    /// - Render the current + next ball previews via <see cref="_currentPreview"/>
    ///   and <see cref="_nextPreview"/> (BallView prefabs already in the scene).
    ///
    /// Cross-system coupling is kept minimal: the shooter only knows about
    /// the pool (to spawn projectiles), the ball database (to colour previews),
    /// and the state machine (to ignore input while paused/won/lost).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Shooter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MouseShooterInput _input;
        [SerializeField] private Transform _muzzle;
        [SerializeField] private BallView  _currentPreview;
        [SerializeField] private BallView  _nextPreview;
        [SerializeField] private GameObjectPool _pool;
        [SerializeField] private BallDatabase _database;
        [SerializeField] private LevelDefinition _level;
        [SerializeField] private ProjectileBall _projectilePrefab;
        [SerializeField] private ChainController _chain;

        [Header("Constraints")]
        [SerializeField] private float _minX = -4f;
        [SerializeField] private float _maxX =  4f;
        [SerializeField] private float _projectileSpeed = 16f;
        [SerializeField] private float _fireCooldown    = 0.18f;

        private IEventBus       _bus;
        private GameStateMachine _fsm;
        private ShooterAmmo     _ammo;
        private float           _cooldownTimer;

        public void Initialise(IEventBus bus, GameStateMachine fsm)
        {
            _bus = bus;
            _fsm = fsm;
            var source = new RandomAmmoSource(_level.AvailableColors);
            _ammo = new ShooterAmmo(source, bus);
            _ammo.Changed += RefreshPreviews;
            _ammo.Prime();
        }

        private void OnDestroy()
        {
            if (_ammo != null) _ammo.Changed -= RefreshPreviews;
        }

        private void Update()
        {
            if (_fsm == null || !_fsm.IsInState<PlayingState>()) return;
            if (_input == null) return;

            // Horizontal tracking — direct write to transform.position keeps
            // the shooter weightless and avoids physics interactions; the
            // muzzle children follow naturally.
            var pos = transform.position;
            pos.x   = Mathf.Clamp(_input.HorizontalAxisWorld, _minX, _maxX);
            transform.position = pos;

            _cooldownTimer -= Time.deltaTime;

            if (_input.SwapPressed) _ammo.Swap();
            if (_input.FirePressed && _cooldownTimer <= 0f) Fire();
        }

        private void Fire()
        {
            if (_projectilePrefab == null || _muzzle == null) return;
            var color = _ammo.Consume();
            _cooldownTimer = _fireCooldown;

            // Fire direction is the muzzle's local +Z (its forward). For the
            // top-down orthographic setup, place the shooter at -Z with the
            // muzzle un-rotated and projectiles travel along world +Z toward
            // the chain. To use a different convention, simply rotate the
            // muzzle child — its forward vector defines the firing direction.
            var velocity   = _muzzle.forward * _projectileSpeed;
            var spawnRot   = Quaternion.LookRotation(velocity.sqrMagnitude > 0f ? velocity : _muzzle.forward, Vector3.up);
            var projectile = _pool.Get(_projectilePrefab, _muzzle.position, spawnRot, null);
            projectile.Launch(color, _database.Get(color), velocity, _chain, _pool);
            _bus?.Publish(new ProjectileFiredEvent(color, _muzzle.position));
        }

        private void RefreshPreviews(BallColor current, BallColor next)
        {
            if (_currentPreview != null) _currentPreview.Apply(_database.Get(current));
            if (_nextPreview    != null) _nextPreview.Apply(_database.Get(next));
        }
    }
}
