using UnityEngine;

namespace Game.PathSystem
{
    /// <summary>
    /// Abstraction over the path that the ball chain travels along.
    ///
    /// The contract is intentionally distance-parameterised rather than
    /// t-parameterised. Zuma-style gameplay needs *uniform spacing* between
    /// balls, which means arc-length parameterisation. A spline implementation
    /// (Catmull-Rom, Bezier) must internally reparameterise to arc length so
    /// callers always see a constant velocity for a constant Δdistance.
    ///
    /// All distances are clamped to [0, TotalLength] by implementations —
    /// out-of-range values are not errors, they just sample the endpoints.
    /// This greatly simplifies chain math at the boundaries.
    /// </summary>
    public interface IPath
    {
        float TotalLength { get; }

        /// <summary>World position at the given arc-length distance from the start.</summary>
        Vector3 EvaluatePosition(float distance);

        /// <summary>Unit forward tangent at the given arc-length distance.</summary>
        Vector3 EvaluateTangent(float distance);

        /// <summary>
        /// Combined sample, populated in one traversal. Implementations should
        /// share the segment lookup between position and tangent to avoid the
        /// duplicate binary search that two separate calls would incur.
        /// </summary>
        void Sample(float distance, out Vector3 position, out Vector3 tangent);
    }
}
