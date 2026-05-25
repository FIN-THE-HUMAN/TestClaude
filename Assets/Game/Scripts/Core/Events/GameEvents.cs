using Game.Balls;
using UnityEngine;

namespace Game.Core.Events
{
    // All gameplay events are immutable structs to keep dispatch allocation-free.
    // Adding a new event is a one-line struct declaration here.

    public readonly struct ChainAdvancedEvent
    {
        public readonly float HeadDistance;
        public readonly float PathLength;
        public ChainAdvancedEvent(float head, float length) { HeadDistance = head; PathLength = length; }
    }

    public readonly struct BallInsertedEvent
    {
        public readonly BallColor Color;
        public readonly int SegmentIndex;
        public readonly int BallIndex;
        public BallInsertedEvent(BallColor color, int segIdx, int ballIdx)
        { Color = color; SegmentIndex = segIdx; BallIndex = ballIdx; }
    }

    public readonly struct MatchResolvedEvent
    {
        public readonly BallColor Color;
        public readonly int Count;
        public readonly int ComboDepth;
        public readonly Vector3 Center;
        public MatchResolvedEvent(BallColor color, int count, int depth, Vector3 center)
        { Color = color; Count = count; ComboDepth = depth; Center = center; }
    }

    public readonly struct ScoreChangedEvent
    {
        public readonly int Score;
        public readonly int Delta;
        public ScoreChangedEvent(int score, int delta) { Score = score; Delta = delta; }
    }

    public readonly struct AmmoChangedEvent
    {
        public readonly BallColor Current;
        public readonly BallColor Next;
        public AmmoChangedEvent(BallColor cur, BallColor nxt) { Current = cur; Next = nxt; }
    }

    public readonly struct ProjectileFiredEvent
    {
        public readonly BallColor Color;
        public readonly Vector3 Origin;
        public ProjectileFiredEvent(BallColor color, Vector3 origin) { Color = color; Origin = origin; }
    }

    public readonly struct GameWonEvent { }
    public readonly struct GameLostEvent { }
    public readonly struct GamePausedEvent { public readonly bool Paused; public GamePausedEvent(bool p) { Paused = p; } }
}
