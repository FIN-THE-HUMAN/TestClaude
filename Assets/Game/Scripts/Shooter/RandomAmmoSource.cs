using System.Collections.Generic;
using Game.Balls;
using UnityEngine;

namespace Game.Shooter
{
    /// <summary>
    /// Picks a uniformly random colour from the supplied allowed list.
    /// The list is the level's available palette — drawing colours not in
    /// the chain is wasteful (player can never match), so a future
    /// WeightedSource should look at <see cref="Game.Chain.ChainModel"/>.
    /// </summary>
    public sealed class RandomAmmoSource : IAmmoSource
    {
        private readonly IReadOnlyList<BallColor> _allowed;
        private readonly System.Random _rng;

        public RandomAmmoSource(IReadOnlyList<BallColor> allowed, int seed = 0)
        {
            _allowed = allowed;
            _rng = seed == 0 ? new System.Random() : new System.Random(seed);
        }

        public BallColor Draw()
        {
            if (_allowed == null || _allowed.Count == 0) return BallColor.Red;
            return _allowed[_rng.Next(0, _allowed.Count)];
        }
    }
}
