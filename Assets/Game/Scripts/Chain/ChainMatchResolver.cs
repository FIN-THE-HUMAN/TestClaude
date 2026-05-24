using System.Collections.Generic;
using Game.Balls;

namespace Game.Chain
{
    /// <summary>
    /// Detects and removes adjacent runs of same-colour balls.
    ///
    /// Two flavours are exposed:
    ///
    /// 1. <see cref="ResolveFromIndex"/> — used right after an insertion. Scans
    ///    OUTWARD from the inserted ball in both directions. This is the cheap,
    ///    targeted check that gets invoked dozens of times per chain reaction.
    ///
    /// 2. <see cref="ResolveAtBoundary"/> — used after a segment merge. Scans
    ///    outward from the seam between the two newly-joined segments. Same
    ///    underlying algorithm, different anchor.
    ///
    /// Both produce a <see cref="Outcome"/> reporting whether anything was
    /// removed, the colour removed, the count, and the original arc-length
    /// centre of the removed run (used by VFX/score).
    ///
    /// The resolver only mutates the segment it is told to operate on. Splitting
    /// the segment in two — when a run is in the middle, not at an end — is
    /// the resolver's responsibility, because that's a chain-structure change
    /// that depends on local context.
    /// </summary>
    public sealed class ChainMatchResolver
    {
        public struct Outcome
        {
            public bool       Matched;
            public BallColor  Color;
            public int        Removed;
            public float      CentreDistance;
            public ChainSegment LeftSegment;   // segment in front of the gap (may equal the input segment)
            public ChainSegment RightSegment;  // segment behind the gap, or null if removal touched the tail
        }

        public Outcome ResolveFromIndex(ChainModel model, ChainSegment segment, int indexHint)
        {
            var balls = segment.Balls;
            if (indexHint < 0 || indexHint >= balls.Count) return default;

            var color = balls[indexHint].Color;
            // Expand left (toward front) while colour matches.
            int left = indexHint;
            while (left - 1 >= 0 && balls[left - 1].Color == color) left--;
            // Expand right (toward back) while colour matches.
            int right = indexHint;
            while (right + 1 < balls.Count && balls[right + 1].Color == color) right++;

            int runLength = right - left + 1;
            if (runLength < model.Config.MinMatch) return default;

            return RemoveRange(model, segment, left, right, color);
        }

        public Outcome ResolveAtBoundary(ChainModel model, ChainSegment merged, int leftCount)
        {
            // After a merge, the seam is between leftCount-1 and leftCount.
            // Pick the colour at the seam and expand outward.
            if (leftCount <= 0 || leftCount >= merged.Balls.Count) return default;
            if (merged.Balls[leftCount - 1].Color != merged.Balls[leftCount].Color) return default;

            // Anchor on either side — colours agree, so it does not matter.
            return ResolveFromIndex(model, merged, leftCount);
        }

        // --- Removal core --------------------------------------------------

        private static Outcome RemoveRange(ChainModel model, ChainSegment segment, int left, int right, BallColor color)
        {
            var balls = segment.Balls;
            int n = balls.Count;
            int removed = right - left + 1;

            // Compute centre distance for VFX/score — averaging is fine because
            // distances within a segment are uniformly spaced.
            float centre = (balls[left].DistanceAlongPath + balls[right].DistanceAlongPath) * 0.5f;

            var outcome = new Outcome
            {
                Matched = true,
                Color = color,
                Removed = removed,
                CentreDistance = centre,
            };

            // Case A: range is at the front of the segment (touches index 0).
            //         Drop those entries and advance the segment's HeadDistance
            //         to the new front ball's distance.
            if (left == 0 && right < n - 1)
            {
                balls.RemoveRange(0, removed);
                segment.HeadDistance = balls[0].DistanceAlongPath;
                outcome.LeftSegment  = segment;
                outcome.RightSegment = null;
                return outcome;
            }
            // Case B: range is at the tail. Drop them — head unchanged.
            if (right == n - 1 && left > 0)
            {
                balls.RemoveRange(left, removed);
                outcome.LeftSegment  = segment;
                outcome.RightSegment = null;
                return outcome;
            }
            // Case C: range covers the whole segment — remove it entirely.
            if (left == 0 && right == n - 1)
            {
                balls.Clear();
                outcome.LeftSegment  = null;
                outcome.RightSegment = null;
                model.CompactEmptySegments();
                return outcome;
            }
            // Case D: range is interior — SPLIT into two segments.
            //         Left segment keeps the front portion and its HeadDistance.
            //         Right segment takes the back portion and adopts the
            //         distance of its first remaining ball as its HeadDistance.
            var rightSeg = new ChainSegment();
            for (int i = right + 1; i < n; i++) rightSeg.Balls.Add(balls[i]);
            rightSeg.HeadDistance = rightSeg.Balls[0].DistanceAlongPath;

            balls.RemoveRange(left, n - left);

            // Insert the new right segment right after this one in the model.
            var segIndex = model.Segments.IndexOf(segment);
            model.Segments.Insert(segIndex + 1, rightSeg);

            outcome.LeftSegment  = segment;
            outcome.RightSegment = rightSeg;
            return outcome;
        }
    }
}
