using UnityEngine;

namespace Game.Chain
{
    /// <summary>
    /// Inserts a projectile ball into the chain at the correct location.
    ///
    /// Convention chosen: "shift back, hold front fixed".
    /// - Balls in front of (greater distance than) the insertion point keep their distance.
    /// - The new ball assumes a distance one BallDiameter behind the ball it sits in front of.
    /// - Every ball at or behind the insertion index shifts backward by one BallDiameter.
    /// Holding the front fixed prevents projectiles from shoving the chain forward,
    /// which would otherwise let the player cause losses on impact.
    ///
    /// Edge case: when the target index is 0 (impact on the very head), the new
    /// ball becomes the new head and the segment's HeadDistance advances. This
    /// is the only case where the front moves, and it is intuitive — the player
    /// stuck the ball on the nose of the chain.
    /// </summary>
    public static class ChainInsertionResolver
    {
        public readonly struct Result
        {
            public readonly int SegmentIndex;
            public readonly int BallIndex;
            public Result(int s, int b) { SegmentIndex = s; BallIndex = b; }
        }

        /// <summary>
        /// Decide which side of the target ball the projectile struck.
        /// Returns true if the projectile is on the FORWARD (higher-distance)
        /// side of the target — i.e. the new ball should be inserted in front
        /// of the target. <paramref name="pathTangentAtTarget"/> must be the
        /// path direction (unit vector) sampled at the target's distance.
        /// </summary>
        public static bool IsImpactInFrontOf(
            Vector3 targetWorld,
            Vector3 projectileWorld,
            Vector3 pathTangentAtTarget)
            => Vector3.Dot(projectileWorld - targetWorld, pathTangentAtTarget) > 0f;

        public static Result Insert(
            ChainModel model,
            ChainBall newBall,
            ChainSegment targetSegment,
            int targetIndex,
            bool insertInFrontOfTarget)
        {
            var cfg = model.Config;
            var segIndex = model.Segments.IndexOf(targetSegment);
            if (segIndex < 0) return new Result(-1, -1);

            // "Insert in front of target" → the new ball sits at a HIGHER
            // distance → earlier list index → same index as the target,
            // which becomes index+1 after the insert.
            int insertIndex = insertInFrontOfTarget ? targetIndex : targetIndex + 1;
            targetSegment.Balls.Insert(insertIndex, newBall);

            if (insertIndex == 0)
            {
                // New head: extend segment forward by one diameter.
                newBall.DistanceAlongPath = targetSegment.HeadDistance + cfg.BallDiameter;
                targetSegment.HeadDistance = newBall.DistanceAlongPath;
            }
            else
            {
                var inFront = targetSegment.Balls[insertIndex - 1];
                newBall.DistanceAlongPath = inFront.DistanceAlongPath - cfg.BallDiameter;
                for (int i = insertIndex + 1; i < targetSegment.Balls.Count; i++)
                    targetSegment.Balls[i].DistanceAlongPath =
                        targetSegment.Balls[i - 1].DistanceAlongPath - cfg.BallDiameter;
            }

            return new Result(segIndex, insertIndex);
        }
    }
}
