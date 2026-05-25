using System.Collections.Generic;

namespace Game.Chain
{
    /// <summary>
    /// A contiguous run of balls inside the chain.
    ///
    /// Invariants enforced by the model (never violate them in new code):
    /// 1. <see cref="Balls"/> is ordered FRONT → BACK. Front has the highest
    ///    distance along the path, back has the lowest.
    /// 2. <see cref="Balls"/>[i].DistanceAlongPath = HeadDistance - i * BallDiameter,
    ///    where BallDiameter is taken from <see cref="ChainConfig"/>.
    /// 3. <see cref="HeadDistance"/> equals Balls[0].DistanceAlongPath.
    /// 4. Two adjacent segments are separated by a positive gap; merging is
    ///    the only legal way to close that gap, performed by <see cref="ChainCollapseResolver"/>.
    ///
    /// These invariants are what allow insertion math to be O(k) where k is
    /// the number of balls behind the insertion point — not O(n).
    /// </summary>
    public sealed class ChainSegment
    {
        public readonly List<ChainBall> Balls = new(32);
        public float HeadDistance;

        /// <summary>Distance of the rear-most ball.</summary>
        public float TailDistance(float ballDiameter)
            => Balls.Count == 0 ? HeadDistance : HeadDistance - (Balls.Count - 1) * ballDiameter;

        /// <summary>Resync ball distances to maintain invariant (2). Cheap: linear in size.</summary>
        public void Resync(float ballDiameter)
        {
            for (int i = 0; i < Balls.Count; i++)
                Balls[i].DistanceAlongPath = HeadDistance - i * ballDiameter;
        }
    }
}
