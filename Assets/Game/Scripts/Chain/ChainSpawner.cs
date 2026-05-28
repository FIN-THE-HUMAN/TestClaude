using System.Collections.Generic;
using Game.Balls;
using Game.Level;
using UnityEngine;

namespace Game.Chain
{
    /// <summary>
    /// Owns the queue of balls waiting to enter the chain at the path start.
    /// "Progressive" spawning means balls trickle in at the path origin and
    /// the chain grows; once spawning is finished and the chain is cleared,
    /// the level is won.
    ///
    /// Independent from chain movement: the spawner only injects new balls
    /// into the back of the rear-most segment (creating one if needed). The
    /// chain controller handles all motion.
    /// </summary>
    public sealed class ChainSpawner
    {
        private readonly Queue<BallColor> _queue = new(128);
        private readonly System.Random _rng = new();
        private BallColor[] _palette = System.Array.Empty<BallColor>();
        private int _paletteCount;
        private float _spawnAccumulator;
        private float _spawnInterval;
        private ChainSpawnMode _mode;
        private int _randomSpawnRemaining;

        public bool SpawningComplete =>
            _mode == ChainSpawnMode.RandomFromPalette
                ? _randomSpawnRemaining <= 0
                : _queue.Count == 0;

        public int QueueCount => _mode == ChainSpawnMode.FixedQueue ? _queue.Count : _randomSpawnRemaining;

        public void Configure(
            ChainSpawnMode mode,
            IList<BallColor> fixedQueue,
            IReadOnlyList<BallColor> palette,
            float spawnInterval,
            int randomSpawnCount)
        {
            _mode = mode;
            _queue.Clear();
            _spawnInterval = Mathf.Max(0.01f, spawnInterval);
            _spawnAccumulator = 0f;

            if (mode == ChainSpawnMode.FixedQueue)
            {
                for (int i = 0; i < fixedQueue.Count; i++) _queue.Enqueue(fixedQueue[i]);
                _randomSpawnRemaining = 0;
            }
            else
            {
                _randomSpawnRemaining = Mathf.Max(0, randomSpawnCount);
            }

            _paletteCount = Mathf.Min(palette.Count, 16);
            if (_palette.Length < _paletteCount)
                _palette = new BallColor[_paletteCount];
            for (int i = 0; i < _paletteCount; i++)
                _palette[i] = palette[i];
        }

        public void Enqueue(BallColor color) => _queue.Enqueue(color);

        /// <summary>
        /// Advance the spawn timer. While the timer matures and spawning is
        /// not finished, returns the next colour to spawn (call repeatedly per
        /// frame in case the interval is shorter than dt). The caller is
        /// responsible for materialising the ball and inserting it into
        /// the rear of the chain.
        /// </summary>
        public bool TickNext(float dt, out BallColor next)
        {
            next = BallColor.None;
            if (SpawningComplete) return false;

            _spawnAccumulator += dt;
            if (_spawnAccumulator < _spawnInterval) return false;
            _spawnAccumulator -= _spawnInterval;

            if (_mode == ChainSpawnMode.RandomFromPalette)
            {
                if (_paletteCount == 0) return false;
                next = _palette[_rng.Next(0, _paletteCount)];
                _randomSpawnRemaining--;
                return true;
            }

            if (_queue.Count == 0) return false;
            next = _queue.Dequeue();
            return true;
        }
    }
}
