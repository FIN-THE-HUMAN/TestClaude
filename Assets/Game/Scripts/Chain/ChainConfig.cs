using UnityEngine;

namespace Game.Chain
{
    /// <summary>
    /// Tuning knobs that change per level/build. Held as a small struct so
    /// gameplay tests can construct one without touching ScriptableObjects.
    /// Authored at runtime from a <see cref="Game.Level.LevelDefinition"/>.
    /// </summary>
    [System.Serializable]
    public struct ChainConfig
    {
        [Min(0.05f)] public float BallDiameter;       // world units between ball centres
        [Min(0f)]    public float ForwardSpeed;       // base forward velocity (m/s)
        [Min(0f)]    public float CollapseCatchUp;    // extra speed for trailing segments while a gap exists
        [Min(0.0001f)] public float MergeEpsilon;     // distance under which a gap counts as "closed"
        [Min(3)]     public int   MinMatch;           // minimum run length to remove (Zuma classic = 3)

        public static ChainConfig Default => new()
        {
            BallDiameter    = 0.5f,
            ForwardSpeed    = 1.2f,
            CollapseCatchUp = 6f,
            MergeEpsilon    = 0.001f,
            MinMatch        = 3,
        };
    }
}
