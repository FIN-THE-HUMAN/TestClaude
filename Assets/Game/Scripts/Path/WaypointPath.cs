using System.Collections.Generic;
using UnityEngine;

namespace Game.PathSystem
{
    /// <summary>
    /// Polyline path defined by a list of control points (children of this transform).
    ///
    /// Why polyline first?
    /// - It is the simplest correct implementation: arc-length is exact, segment
    ///   lookup is O(log n), and tangent evaluation is the segment direction.
    /// - It establishes the public surface (<see cref="IPath"/>) so a future
    ///   Catmull-Rom or Bezier path can be swapped in by changing one prefab field.
    ///
    /// Cache strategy:
    /// - <see cref="_cumulativeLengths"/>[i] = total length from index 0 to point i.
    /// - <see cref="_segmentDirections"/>[i] = unit vector from i to i+1.
    /// - Rebuilt on Awake and on demand from the editor via <see cref="RebuildCache"/>.
    /// - We do NOT auto-rebuild on Update because the path is static at runtime;
    ///   moving control points during play would invalidate every ball distance.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WaypointPath : MonoBehaviour, IPath
    {
        [Tooltip("World-space waypoints. If empty, child transforms in order are used.")]
        [SerializeField] private List<Vector3> _waypoints = new();

        [Tooltip("If true, use child transforms (in sibling order) as waypoints. Otherwise use the serialized _waypoints list.")]
        [SerializeField] private bool _useChildTransforms = true;

        [Header("Gizmos")]
        [SerializeField] private Color _gizmoColor = new(0.2f, 0.8f, 1f, 1f);
        [SerializeField] private float _gizmoRadius = 0.15f;
        [SerializeField] private bool _drawTangents;

        private Vector3[] _points;
        private float[]   _cumulativeLengths; // length [i+1] - length [i] = segment i length
        private Vector3[] _segmentDirections;
        private float     _totalLength;
        private bool      _cacheValid;

        public float TotalLength { get { EnsureCache(); return _totalLength; } }
        public int   PointCount  { get { EnsureCache(); return _points?.Length ?? 0; } }

        private void Awake() => RebuildCache();

        public void RebuildCache()
        {
            _points = ResolvePoints();
            if (_points == null || _points.Length < 2)
            {
                _cumulativeLengths = System.Array.Empty<float>();
                _segmentDirections = System.Array.Empty<Vector3>();
                _totalLength = 0f;
                _cacheValid = true;
                return;
            }

            _cumulativeLengths = new float[_points.Length];
            _segmentDirections = new Vector3[_points.Length - 1];
            _cumulativeLengths[0] = 0f;
            for (int i = 0; i < _points.Length - 1; i++)
            {
                var delta  = _points[i + 1] - _points[i];
                var length = delta.magnitude;
                _segmentDirections[i] = length > 1e-6f ? delta / length : Vector3.forward;
                _cumulativeLengths[i + 1] = _cumulativeLengths[i] + length;
            }
            _totalLength = _cumulativeLengths[_points.Length - 1];
            _cacheValid = true;
        }

        private void EnsureCache() { if (!_cacheValid) RebuildCache(); }

        private Vector3[] ResolvePoints()
        {
            if (_useChildTransforms)
            {
                var t = transform;
                var n = t.childCount;
                if (n == 0) return null;
                var arr = new Vector3[n];
                for (int i = 0; i < n; i++) arr[i] = t.GetChild(i).position;
                return arr;
            }
            if (_waypoints == null || _waypoints.Count == 0) return null;
            var w = new Vector3[_waypoints.Count];
            for (int i = 0; i < _waypoints.Count; i++) w[i] = transform.TransformPoint(_waypoints[i]);
            return w;
        }

        public Vector3 EvaluatePosition(float distance)
        {
            Sample(distance, out var pos, out _);
            return pos;
        }

        public Vector3 EvaluateTangent(float distance)
        {
            Sample(distance, out _, out var tan);
            return tan;
        }

        public void Sample(float distance, out Vector3 position, out Vector3 tangent)
        {
            EnsureCache();
            if (_points == null || _points.Length < 2)
            {
                position = transform.position;
                tangent  = Vector3.forward;
                return;
            }

            // Clamp at endpoints — chain code relies on this not throwing.
            if (distance <= 0f)
            {
                position = _points[0];
                tangent  = _segmentDirections[0];
                return;
            }
            if (distance >= _totalLength)
            {
                position = _points[_points.Length - 1];
                tangent  = _segmentDirections[_segmentDirections.Length - 1];
                return;
            }

            // Binary search for the segment that contains `distance`.
            int seg = FindSegment(distance);
            var segStart = _cumulativeLengths[seg];
            var segLen   = _cumulativeLengths[seg + 1] - segStart;
            var t        = segLen > 1e-6f ? (distance - segStart) / segLen : 0f;
            position = Vector3.LerpUnclamped(_points[seg], _points[seg + 1], t);
            tangent  = _segmentDirections[seg];
        }

        private int FindSegment(float distance)
        {
            int lo = 0, hi = _cumulativeLengths.Length - 1;
            while (lo < hi - 1)
            {
                int mid = (lo + hi) >> 1;
                if (_cumulativeLengths[mid] <= distance) lo = mid;
                else hi = mid;
            }
            return lo;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var pts = ResolvePoints();
            if (pts == null || pts.Length < 2) return;
            Gizmos.color = _gizmoColor;
            for (int i = 0; i < pts.Length - 1; i++)
            {
                Gizmos.DrawLine(pts[i], pts[i + 1]);
                Gizmos.DrawSphere(pts[i], _gizmoRadius);
                if (_drawTangents)
                {
                    var mid = (pts[i] + pts[i + 1]) * 0.5f;
                    var dir = (pts[i + 1] - pts[i]).normalized;
                    Gizmos.DrawLine(mid, mid + dir * 0.5f);
                }
            }
            Gizmos.DrawSphere(pts[pts.Length - 1], _gizmoRadius);
        }

        private void OnValidate() => _cacheValid = false;
#endif
    }
}
