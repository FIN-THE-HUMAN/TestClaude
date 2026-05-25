using Game.Core.Pooling;
using UnityEngine;

namespace Game.Balls
{
    /// <summary>
    /// Pure visual representation of a ball. Has no knowledge of the chain,
    /// the path, or whether it is a projectile — it only paints itself and
    /// plays small juicy reactions. This means the same prefab is reusable
    /// for chain balls, projectiles, ammo previews, and tutorial graphics.
    ///
    /// Gameplay drives the view via <see cref="Apply"/> (data) and a few
    /// explicit hooks (<see cref="PlayPopIntent"/>, etc.). The view never
    /// looks up gameplay state on its own.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BallView : MonoBehaviour, IPoolable
    {
        [SerializeField] private MeshRenderer _renderer;
        [SerializeField] private Transform    _visualRoot;

        private MaterialPropertyBlock _mpb;
        private static readonly int s_colorId = Shader.PropertyToID("_BaseColor");

        public BallColor Color { get; private set; }

        private void Reset()
        {
            _renderer    = GetComponentInChildren<MeshRenderer>();
            _visualRoot  = transform;
        }

        public void Apply(BallDefinition definition)
        {
            Color = definition.Color;
            if (_renderer == null) return;
            _mpb ??= new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(s_colorId, definition.DisplayColor);
            _renderer.SetPropertyBlock(_mpb);
        }

        public void SetWorldPose(Vector3 position, Quaternion rotation)
        {
            // Direct transform set is intentional — the view does not animate
            // its own position. The chain controller is the source of truth.
            transform.SetPositionAndRotation(position, rotation);
        }

        public void PlayPopIntent()
        {
            // Hook for designers: a tween/VFX could play here. Kept as a stub
            // because we want the view to never block gameplay timing.
        }

        public void OnSpawned()
        {
            if (_visualRoot != null) _visualRoot.localScale = Vector3.one;
        }

        public void OnDespawned()
        {
            Color = BallColor.None;
        }
    }
}
