using System.Collections.Generic;

namespace Game.Chain
{
    /// <summary>
    /// Closes gaps between segments and merges them.
    ///
    /// Movement model:
    /// - Front segment (index 0) advances at <see cref="ChainConfig.ForwardSpeed"/>.
    /// - Every trailing segment advances at ForwardSpeed + CollapseCatchUp while
    ///   a positive gap exists to the segment in front. Once the gap drops
    ///   below MergeEpsilon, the trailing segment is snapped flush and the
    ///   two segments merge into one. The merge handler then asks the match
    ///   resolver to check the seam — which is where chain reactions come from.
    ///
    /// Spacing inside a merged segment is taken care of by
    /// <see cref="ChainSegment.Resync"/>, called immediately after the merge.
    ///
    /// This method does not raise events — the controller owns event dispatch
    /// so cascades are reported with a single combo depth, not interleaved.
    /// </summary>
    public sealed class ChainCollapseResolver
    {
        // Returned to the controller so it can drive recursive match checks.
        public readonly List<MergeInfo> ProducedMerges = new(4);

        public readonly struct MergeInfo
        {
            public readonly ChainSegment Merged;
            public readonly int          SeamIndex; // index of the first ball that came from the trailing segment
            public MergeInfo(ChainSegment merged, int seamIndex) { Merged = merged; SeamIndex = seamIndex; }
        }

        /// <summary>
        /// Advance segment positions by dt and merge any segments that collide.
        /// Called from the controller's update loop while the chain is alive.
        /// </summary>
        public void Tick(ChainModel model, float dt)
        {
            ProducedMerges.Clear();
            var cfg = model.Config;
            var segs = model.Segments;
            if (segs.Count == 0) return;

            // 1. Move the front segment.
            segs[0].HeadDistance += cfg.ForwardSpeed * dt;

            // 2. Move trailing segments, accelerating any that have a gap.
            // When a merge fires we remove the trailing segment from the list
            // and decrement the cursor so the *next* iteration compares the
            // now-grown segment against whatever segment used to be two-behind.
            for (int i = 1; i < segs.Count; i++)
            {
                var ahead = segs[i - 1];
                var trail = segs[i];

                float aheadTail = ahead.TailDistance(cfg.BallDiameter);
                float trailHead = trail.HeadDistance;
                float gap       = aheadTail - trailHead - cfg.BallDiameter; // positive when separated

                float velocity  = cfg.ForwardSpeed + (gap > cfg.MergeEpsilon ? cfg.CollapseCatchUp : 0f);
                trail.HeadDistance += velocity * dt;

                // Re-evaluate gap after the move; clamp overshoot to the
                // exact contact distance to keep spacing invariant.
                aheadTail = ahead.TailDistance(cfg.BallDiameter);
                trailHead = trail.HeadDistance;
                gap       = aheadTail - trailHead - cfg.BallDiameter;
                if (gap <= cfg.MergeEpsilon)
                {
                    // Snap into contact: trailing head sits exactly BallDiameter behind aheadTail.
                    trail.HeadDistance = aheadTail - cfg.BallDiameter;
                    // Merge trail INTO ahead. SeamIndex = original size of ahead before append.
                    int seam = ahead.Balls.Count;
                    for (int b = 0; b < trail.Balls.Count; b++) ahead.Balls.Add(trail.Balls[b]);
                    trail.Balls.Clear();
                    ahead.Resync(cfg.BallDiameter);
                    ProducedMerges.Add(new MergeInfo(ahead, seam));

                    // Remove the emptied segment in-place so subsequent iterations
                    // see the correct neighbour topology. Decrement i so the loop
                    // re-examines the same index, which is now the next segment back.
                    segs.RemoveAt(i);
                    i--;
                }
            }

            // 4. Make sure every segment honours invariant (2).
            for (int i = 0; i < segs.Count; i++) segs[i].Resync(cfg.BallDiameter);
        }
    }
}
