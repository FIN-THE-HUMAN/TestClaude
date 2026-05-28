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
        // URP Lit uses _BaseColor; built-in Standard uses _Color.
        private static readonly int s_baseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int s_colorId     = Shader.PropertyToID("_Color");
        private static Material s_fallbackMaterial;
        private Vector3 _defaultVisualScale = Vector3.one;
        private float   _rollAngleDegrees;

        public BallColor Color { get; private set; }

        private void Awake()
        {
            if (_visualRoot == null) _visualRoot = transform;
            _defaultVisualScale = _visualRoot.localScale;
        }

        private void Reset()
        {
            _renderer    = GetComponentInChildren<MeshRenderer>();
            _visualRoot  = transform;
            _defaultVisualScale = _visualRoot.localScale;
        }

        public void Apply(BallDefinition definition)
        {
            if (definition == null) return;
            Color = definition.Color;
            if (_renderer == null) _renderer = GetComponentInChildren<MeshRenderer>();
            if (_renderer == null) return;

            if (definition.Material != null)
            {
                _renderer.SetPropertyBlock(null);
                _renderer.sharedMaterial = definition.Material;
                return;
            }

            ApplyColorViaPropertyBlock(definition.DisplayColor);
        }

        private void ApplyColorViaPropertyBlock(Color color)
        {
            EnsureFallbackMaterial();
            _mpb ??= new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(s_baseColorId, color);
            _mpb.SetColor(s_colorId, color);
            _renderer.SetPropertyBlock(_mpb);
        }

        private void EnsureFallbackMaterial()
        {
            if (_renderer.sharedMaterial != null) return;
            s_fallbackMaterial ??= CreateFallbackMaterial();
            _renderer.sharedMaterial = s_fallbackMaterial;
        }

        private static Material CreateFallbackMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader) { name = "BallView_Fallback" };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", UnityEngine.Color.white);
            if (mat.HasProperty("_Color"))     mat.SetColor("_Color", UnityEngine.Color.white);
            return mat;
        }

        public void SetWorldPose(Vector3 position, Quaternion rotation)
        {
            // Direct transform set is intentional — the view does not animate
            // its own position. The chain controller is the source of truth.
            transform.SetPositionAndRotation(position, rotation);
        }

        /// <summary>
        /// Chain-only pose: faces along the path tangent and accumulates roll
        /// around the local right axis so textures read as the ball is rolling.
        /// </summary>
        public void SetChainPose(Vector3 position, Vector3 tangent, float alongPathSpeed, float dt)
        {
            transform.position = position;
            if (tangent.sqrMagnitude < 1e-6f)
            {
                transform.rotation = Quaternion.identity;
                return;
            }

            var forward = tangent.normalized;
            var baseRot = Quaternion.LookRotation(forward, Vector3.up);

            // Unity sphere primitive: mesh radius 0.5 at unit scale.
            float worldRadius = transform.lossyScale.x * 0.5f;
            if (worldRadius > 1e-4f && Mathf.Abs(alongPathSpeed) > 1e-4f && dt > 0f)
            {
                float deltaDeg = alongPathSpeed * dt / worldRadius * Mathf.Rad2Deg;
                _rollAngleDegrees += deltaDeg;
            }

            transform.rotation = baseRot * Quaternion.AngleAxis(_rollAngleDegrees, Vector3.right);
        }

        public void ResetRoll() => _rollAngleDegrees = 0f;

        public void PlayPopIntent()
        {
            // Hook for designers: a tween/VFX could play here. Kept as a stub
            // because we want the view to never block gameplay timing.
        }

        public void OnSpawned()
        {
            // Restore prefab scale after pool reuse (was incorrectly forced to 1).
            if (_visualRoot != null) _visualRoot.localScale = _defaultVisualScale;
        }

        public void OnDespawned()
        {
            Color = BallColor.None;
            ResetRoll();
            if (_renderer != null) _renderer.SetPropertyBlock(null);
        }
    }
}
