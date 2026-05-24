using System.Collections.Generic;
using Game.Balls;
using UnityEngine;

namespace Game.Chain
{
    /// <summary>
    /// Owns the queue of balls waiting to enter the chain at the path start.
    /// "Progressive" spawning means balls trickle in at the path origin and
    /// the chain grows; once the queue is empty, no more balls appear and
    /// destroying the remaining chain wins the level.
    ///
    /// Independent from chain movement: the spawner only injects new balls
    /// into the back of the rear-most segment (creating one if needed). The
    /// chain controller handles all motion.
    /// </summary>
    public sealed class ChainSpawner
    {
        private readonly Queue<BallColor> _queue = new(128);
        private float _spawnAccumulator;
        private float _spawnInterval;

        public bool QueueEmpty => _queue.Count == 0;
        public int  QueueCount => _queue.Count;

        public void Configure(IList<BallColor> initialQueue, float spawnInterval)
        {
            _queue.Clear();
            for (int i = 0; i < initialQueue.Count; i++) _queue.Enqueue(initialQueue[i]);
            _spawnInterval = Mathf.Max(0.01f, spawnInterval);
            _spawnAccumulator = 0f;
        }

        public void Enqueue(BallColor color) => _queue.Enqueue(color);

        /// <summary>
        /// Advance the spawn timer. While the timer matures and balls are
        /// queued, returns the next colour to spawn (call repeatedly per
        /// frame in case the interval is shorter than dt). The caller is
        /// responsible for materialising the ball and inserting it into
        /// the rear of the chain.
        /// </summary>
        public bool TickNext(float dt, out BallColor next)
        {
            next = BallColor.None;
            if (_queue.Count == 0) return false;
            _spawnAccumulator += dt;
            if (_spawnAccumulator < _spawnInterval) return false;
            _spawnAccumulator -= _spawnInterval;
            next = _queue.Dequeue();
            return true;
        }
    }
}
