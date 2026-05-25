using System.Collections.Generic;
using Game.Balls;
using Game.Chain;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// Authored description of a single level. Every per-level tunable lives
    /// here so designers can ship new content by creating a new asset, no
    /// code changes required. The runtime reads this once at bootstrap.
    ///
    /// Future "special balls" (bombs, multipliers) are intentionally left as
    /// a serialised list of references that can be empty for v1 — the field
    /// is here so adding the feature does not need a schema migration of
    /// every existing level asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Level/Level Definition", fileName = "Level_")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _displayName = "Level";

        [Header("Movement / Chain")]
        [SerializeField] private ChainConfig _chainConfig = ChainConfig.Default;

        [Header("Palette")]
        [Tooltip("Colours that may appear in the chain and in the shooter's ammo source.")]
        [SerializeField] private List<BallColor> _availableColors = new() { BallColor.Red, BallColor.Green, BallColor.Blue };

        [Header("Initial chain (pre-placed balls, ordered front→back)")]
        [SerializeField] private List<BallColor> _initialBalls = new();

        [Header("Progressive spawning")]
        [SerializeField] private List<BallColor> _spawnQueue = new();
        [Min(0.05f)] [SerializeField] private float _spawnInterval = 0.6f;

        [Header("Victory / Score")]
        [SerializeField] private int _scoreMultiplier = 1;

        [Header("Future hooks")]
        [Tooltip("Reserved for special-ball definitions. Safe to leave empty.")]
        [SerializeField] private List<ScriptableObject> _specialBalls = new();

        public string                     DisplayName     => _displayName;
        public IReadOnlyList<BallColor>   AvailableColors => _availableColors;
        public IReadOnlyList<BallColor>   InitialBalls    => _initialBalls;
        public IReadOnlyList<BallColor>   SpawnQueue      => _spawnQueue;
        public float                      SpawnInterval   => _spawnInterval;
        public int                        ScoreMultiplier => _scoreMultiplier;

        public ChainConfig BuildChainConfig() => _chainConfig;
    }
}
