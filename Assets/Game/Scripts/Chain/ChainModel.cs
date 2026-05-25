using System.Collections.Generic;

namespace Game.Chain
{
    /// <summary>
    /// Pure data model: the ordered list of segments that make up the chain.
    /// The model is movement-agnostic — controllers, resolvers and tests can
    /// inspect or mutate it without depending on MonoBehaviour or Unity time.
    ///
    /// Front of the chain = <see cref="Segments"/>[0] = closest to the path end (the danger zone).
    /// </summary>
    public sealed class ChainModel
    {
        public readonly List<ChainSegment> Segments = new(4);
        public ChainConfig Config;

        public int TotalBallCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Segments.Count; i++) n += Segments[i].Balls.Count;
                return n;
            }
        }

        public float FrontHeadDistance => Segments.Count > 0 ? Segments[0].HeadDistance : 0f;

        /// <summary>Resync every segment's ball distances to the invariant.</summary>
        public void ResyncAll()
        {
            for (int i = 0; i < Segments.Count; i++) Segments[i].Resync(Config.BallDiameter);
        }

        /// <summary>Removes empty segments. Called after any removal operation.</summary>
        public void CompactEmptySegments()
        {
            for (int i = Segments.Count - 1; i >= 0; i--)
                if (Segments[i].Balls.Count == 0)
                    Segments.RemoveAt(i);
        }
    }
}
