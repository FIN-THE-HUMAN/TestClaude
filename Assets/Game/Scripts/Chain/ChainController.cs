using System.Collections.Generic;
using Game.Balls;
using Game.Core.Events;
using Game.Core.Pooling;
using Game.Core.Services;
using Game.Core.States;
using Game.Level;
using Game.PathSystem;
using Game.Projectile;
using UnityEngine;

namespace Game.Chain
{
    /// <summary>
    /// The orchestrator that owns the runtime <see cref="ChainModel"/>,
    /// drives motion via the collapse resolver, exposes APIs for the
    /// projectile system to insert balls, and runs the event-driven
    /// "Insert → Resolve Matches → Remove Balls → Collapse Gaps → Recheck"
    /// pipeline.
    ///
    /// Why a MonoBehaviour at all?
    /// - The chain needs an Update tick for movement, and a place for
    ///   serialized references (path, pool, database, level definition).
    /// - All actual algorithms live in plain C# helpers so they remain
    ///   unit-testable without Unity Play Mode.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ChainController : MonoBehaviour
    {
        [Header("Scene references (assign in inspector)")]
        [SerializeField] private WaypointPath  _path;
        [SerializeField] private GameObjectPool _pool;
        [SerializeField] private BallDatabase  _ballDatabase;
        [SerializeField] private LevelDefinition _level;
        [SerializeField] private Transform _ballParent;

        private IEventBus       _bus;
        private GameStateMachine _fsm;

        private readonly ChainModel             _model    = new();
        private readonly ChainSpawner           _spawner  = new();
        private readonly ChainMatchResolver     _matcher  = new();
        private readonly ChainCollapseResolver  _collapse = new();
        private readonly Stack<ChainBall>       _ballPool = new(64); // cheap pool for ChainBall records

        // Combo tracking: increments every time a match resolves; resets when
        // the player fires (which logically starts a new cascade) and when
        // an idle frame passes with no merges. This is the canonical place to
        // own combo depth because the controller is the only system that
        // sees every link of the cascade.
        private int   _comboDepth;
        private float _comboIdleTimer;
        private const float ComboResetSeconds = 0.35f;

        public ChainModel Model => _model;
        public IPath      Path  => _path;

        // ---- Bootstrap ----------------------------------------------------

        private void Awake()
        {
            if (_path == null || _pool == null || _ballDatabase == null || _level == null)
                Debug.LogError("ChainController has missing serialized references.", this);
        }

        public void Initialise(IEventBus bus, GameStateMachine fsm)
        {
            _bus = bus;
            _fsm = fsm;
            _model.Config = _level.BuildChainConfig();

            // Seed initial balls (pre-placed at path start, behind the path origin).
            _spawner.Configure(_level.SpawnQueue, _level.SpawnInterval);
            for (int i = 0; i < _level.InitialBalls.Count; i++)
                SpawnAtTail(_level.InitialBalls[i]);
        }

        // ---- Tick ---------------------------------------------------------

        private void Update()
        {
            if (_fsm == null || !_fsm.IsInState<PlayingState>()) return;

            float dt = Time.deltaTime;

            // 1. Top up the chain from the spawn queue (progressive spawn).
            while (_spawner.TickNext(dt, out var color))
                SpawnAtTail(color);

            // 2. Advance segments and merge anyone that has closed a gap.
            _collapse.Tick(_model, dt);
            // Each merge can be the seed of a cascade — re-run the match
            // resolver at every produced seam. CollapseTick already cleared
            // its merge list before populating.
            bool merged = _collapse.ProducedMerges.Count > 0;
            for (int i = 0; i < _collapse.ProducedMerges.Count; i++)
            {
                var info = _collapse.ProducedMerges[i];
                var outcome = _matcher.ResolveAtBoundary(_model, info.Merged, info.SeamIndex);
                if (outcome.Matched)
                {
                    _comboDepth++;
                    HandleRemoval(outcome, _comboDepth);
                }
            }
            // Decay combo if no match fired this tick.
            if (!merged)
            {
                _comboIdleTimer += dt;
                if (_comboIdleTimer >= ComboResetSeconds) _comboDepth = 0;
            }
            else _comboIdleTimer = 0f;

            // 3. Repaint views from the authoritative distances.
            SyncViews();

            // 4. Publish movement state for HUD/audio listeners.
            _bus?.Publish(new ChainAdvancedEvent(_model.FrontHeadDistance, _path.TotalLength));

            // 5. Lose condition.
            if (_model.FrontHeadDistance >= _path.TotalLength && _model.TotalBallCount > 0)
                _fsm.ChangeState<LoseState>();

            // 6. Win condition.
            if (_spawner.QueueEmpty && _model.TotalBallCount == 0)
                _fsm.ChangeState<WinState>();
        }

        // ---- Public API used by ProjectileBall ----------------------------

        /// <summary>
        /// Resolve and execute an insertion triggered by a projectile collision.
        /// Returns true if a ball was inserted (always true in current rules).
        /// The match cascade runs inline so view sync sees the final state.
        /// </summary>
        public bool TryInsertFromProjectile(ChainBall hitBall, BallColor projectileColor, Vector3 projectileWorldPos)
        {
            // Locate target segment and ball index.
            ChainSegment seg = null;
            int idx = -1;
            for (int s = 0; s < _model.Segments.Count; s++)
            {
                var found = _model.Segments[s].Balls.IndexOf(hitBall);
                if (found >= 0) { seg = _model.Segments[s]; idx = found; break; }
            }
            if (seg == null) return false;

            // Decide side using the path tangent at the target's distance.
            _path.Sample(hitBall.DistanceAlongPath, out var targetWorld, out var tangent);
            bool inFront = ChainInsertionResolver.IsImpactInFrontOf(targetWorld, projectileWorldPos, tangent);

            // Build a chain ball record + visual. Register the view in the
            // live map BEFORE the match resolver runs — otherwise a same-frame
            // match removal of this ball would leak its view (it's not in the
            // model anymore and was never indexed).
            var newBall = AcquireBall();
            newBall.Color = projectileColor;
            newBall.View  = SpawnView(projectileColor, projectileWorldPos);
            _liveViews[newBall] = newBall.View;

            var result = ChainInsertionResolver.Insert(_model, newBall, seg, idx, inFront);
            _bus?.Publish(new BallInsertedEvent(projectileColor, result.SegmentIndex, result.BallIndex));

            // A fresh shot resets the combo cascade — anything that follows
            // is attributable to this projectile's chain reaction.
            _comboDepth = 0;
            _comboIdleTimer = 0f;

            var firstOutcome = _matcher.ResolveFromIndex(_model, seg, result.BallIndex);
            if (firstOutcome.Matched)
            {
                _comboDepth = 1;
                HandleRemoval(firstOutcome, _comboDepth);
                // After removal the segment list may have split; further
                // cascade propagation happens via the collapse → merge →
                // ResolveAtBoundary loop during the next Update tick.
            }

            SyncViews();
            return true;
        }

        // ---- Internals ----------------------------------------------------

        private void HandleRemoval(ChainMatchResolver.Outcome outcome, int comboDepth)
        {
            // Release ball records + views for the removed balls.
            // The match resolver already removed them from the segment list,
            // but it has no knowledge of the view pool — that is our job.
            // We use the centre distance for an approximate VFX origin.
            _path.Sample(outcome.CentreDistance, out var centre, out _);
            _bus?.Publish(new MatchResolvedEvent(outcome.Color, outcome.Removed, comboDepth, centre));

            // The resolver mutates segment lists in-place, but it cannot know
            // which ChainBall records correspond to which views. Instead, the
            // SyncViews pass below detects "orphaned" views by scanning the
            // model. Cleaner: we collect orphans on the fly.
            ReleaseOrphanedViews();
        }

        // Track every view we ever spawned so we can find orphans without a
        // search of the scene. Keyed by ChainBall identity.
        private readonly Dictionary<ChainBall, BallView> _liveViews = new(128);

        private BallView SpawnView(BallColor color, Vector3 atWorld)
        {
            var def = _ballDatabase.Get(color);
            var view = _pool.Get(def.ViewPrefab, atWorld, Quaternion.identity, _ballParent);
            view.Apply(def);
            return view;
        }

        // Called by SyncViews to maintain the back-reference used by projectile triggers.
        private static void BindBackReference(BallView view, ChainBall ball)
        {
            if (view == null) return;
            var refComp = view.GetComponent<ChainBallReference>();
            if (refComp == null) refComp = view.gameObject.AddComponent<ChainBallReference>();
            refComp.Ball = ball;
        }

        private void ReleaseOrphanedViews()
        {
            // Build a hashset of balls that are currently in the model.
            // Allocation is bounded by total chain length; called only on
            // match events, which are rare relative to Update.
            var alive = new HashSet<ChainBall>();
            for (int s = 0; s < _model.Segments.Count; s++)
            {
                var balls = _model.Segments[s].Balls;
                for (int i = 0; i < balls.Count; i++) alive.Add(balls[i]);
            }

            // Pool out anyone the model no longer holds.
            var toRemove = new List<ChainBall>();
            foreach (var kvp in _liveViews)
                if (!alive.Contains(kvp.Key)) toRemove.Add(kvp.Key);

            for (int i = 0; i < toRemove.Count; i++)
            {
                var ball = toRemove[i];
                if (_liveViews.TryGetValue(ball, out var v) && v != null)
                    _pool.Release(v.gameObject);
                _liveViews.Remove(ball);
                ReleaseBall(ball);
            }
        }

        private void SpawnAtTail(BallColor color)
        {
            // The new tail ball goes one BallDiameter behind the current
            // rear-most ball, or at distance 0 if the chain is empty.
            var cfg  = _model.Config;
            ChainSegment rear;
            float distance;
            if (_model.Segments.Count == 0)
            {
                rear = new ChainSegment();
                _model.Segments.Add(rear);
                distance       = 0f;
                rear.HeadDistance = 0f;
            }
            else
            {
                rear = _model.Segments[_model.Segments.Count - 1];
                distance = rear.TailDistance(cfg.BallDiameter) - cfg.BallDiameter;
                if (distance < 0f) distance = 0f; // clamp; collapse will tighten
            }

            var ball = AcquireBall();
            ball.Color = color;
            ball.DistanceAlongPath = distance;
            _path.Sample(distance, out var pos, out _);
            ball.View = SpawnView(color, pos);
            rear.Balls.Add(ball);
            _liveViews[ball] = ball.View;
        }

        private void SyncViews()
        {
            for (int s = 0; s < _model.Segments.Count; s++)
            {
                var seg = _model.Segments[s];
                for (int i = 0; i < seg.Balls.Count; i++)
                {
                    var ball = seg.Balls[i];
                    if (ball.View == null) continue;
                    _path.Sample(ball.DistanceAlongPath, out var pos, out var tan);
                    var rot = tan.sqrMagnitude > 0f ? Quaternion.LookRotation(tan, Vector3.up) : Quaternion.identity;
                    ball.View.SetWorldPose(pos, rot);

                    // Register newly-created views in the live map. (Tail-spawn
                    // and projectile-insert both add here; doing it once at
                    // sync keeps the call sites simple.)
                    _liveViews[ball] = ball.View;
                    BindBackReference(ball.View, ball);
                }
            }
        }

        // ---- ChainBall record reuse --------------------------------------
        // Avoids GC churn over a session of many matches and respawns.

        private ChainBall AcquireBall()
        {
            if (_ballPool.Count > 0) { var b = _ballPool.Pop(); b.Reset(); return b; }
            return new ChainBall();
        }

        private void ReleaseBall(ChainBall ball)
        {
            ball.Reset();
            _ballPool.Push(ball);
        }
    }
}
