using Game.Balls;

namespace Game.Chain
{
    /// <summary>
    /// Runtime ball record inside the chain.
    ///
    /// IMPORTANT: distance is the single source of truth for position. The
    /// view's transform is recomputed every tick from <see cref="DistanceAlongPath"/>
    /// — never write the transform directly from gameplay code, or insertion
    /// math will drift relative to the view and produce visible "jumps".
    ///
    /// Class (not struct) because:
    /// - Match/insertion code mutates many balls per frame; structs would
    ///   force copy-back into the list on every change.
    /// - Each ball owns a long-lived <see cref="BallView"/> reference;
    ///   pairing them in a reference type keeps lookup local.
    /// Allocations are amortised by the pool: balls are pooled and reused.
    /// </summary>
    public sealed class ChainBall
    {
        public BallColor Color;
        public float     DistanceAlongPath;
        public BallView  View;

        // Used by the match resolver as a scratch flag. Public to avoid
        // wrapping in a parallel HashSet allocation; resolver clears it.
        public bool      ScratchMarked;

        public void Reset()
        {
            Color = BallColor.None;
            DistanceAlongPath = 0f;
            View = null;
            ScratchMarked = false;
        }
    }
}
