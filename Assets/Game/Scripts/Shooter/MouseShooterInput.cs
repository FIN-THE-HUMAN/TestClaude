using UnityEngine;

namespace Game.Shooter
{
    /// <summary>
    /// Concrete <see cref="IShooterInput"/> backed by mouse position.
    /// Projects the cursor onto a horizontal plane at the shooter's Y so
    /// it works under an orthographic top-down camera as required by the brief.
    ///
    /// Allocations: none per frame. <see cref="Plane.Raycast"/> is value-typed.
    /// </summary>
    public sealed class MouseShooterInput : MonoBehaviour, IShooterInput
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private float  _trackY = 0f;

        private Plane _trackPlane;

        public float HorizontalAxisWorld { get; private set; }
        public bool  FirePressed { get; private set; }
        public bool  SwapPressed { get; private set; }

        private void Awake()
        {
            if (_camera == null) _camera = Camera.main;
            _trackPlane = new Plane(Vector3.up, new Vector3(0f, _trackY, 0f));
        }

        private void Update()
        {
            if (_camera == null) return;
            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (_trackPlane.Raycast(ray, out var enter))
                HorizontalAxisWorld = ray.GetPoint(enter).x;

            FirePressed = Input.GetMouseButtonDown(0);
            SwapPressed = Input.GetMouseButtonDown(1);
        }
    }
}
