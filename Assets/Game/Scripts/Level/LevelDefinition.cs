using System.Collections.Generic;
using Game.Balls;
using Game.Chain;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// How new balls enter the chain after the level starts.
    /// </summary>
    public enum ChainSpawnMode
    {
        /// <summary>Spawn colours in the order listed in <see cref="LevelDefinition.SpawnQueue"/>.</summary>
        FixedQueue = 0,
        /// <summary>Pick uniformly at random from the resolved level palette.</summary>
        RandomFromPalette = 1,
    }

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
        [Tooltip("Colours allowed in this level (chain + shooter). Ignored when Use All Database Colors is on.")]
        [SerializeField] private List<BallColor> _availableColors = new() { BallColor.Red, BallColor.Green, BallColor.Blue };
        [Tooltip("Use every colour that has an entry in BallDatabase, instead of Available Colors.")]
        [SerializeField] private bool _useAllDatabaseColors;

        [Header("Initial chain (pre-placed balls, ordered front→back)")]
        [SerializeField] private List<BallColor> _initialBalls = new();

        [Header("Progressive spawning")]
        [SerializeField] private ChainSpawnMode _chainSpawnMode = ChainSpawnMode.RandomFromPalette;
        [Tooltip("Used when Chain Spawn Mode = Random From Palette. Ignored for Fixed Queue.")]
        [Min(0)] [SerializeField] private int _randomSpawnCount = 60;
        [Tooltip("Explicit spawn order. Used only when Chain Spawn Mode = Fixed Queue.")]
        [SerializeField] private List<BallColor> _spawnQueue = new();
        [Min(0.05f)] [SerializeField] private float _spawnInterval = 0.6f;

        [Header("Victory / Score")]
        [SerializeField] private int _scoreMultiplier = 1;

        [Header("Future hooks")]
        [Tooltip("Reserved for special-ball definitions. Safe to leave empty.")]
        [SerializeField] private List<ScriptableObject> _specialBalls = new();

        public string                     DisplayName           => _displayName;
        public IReadOnlyList<BallColor>   AvailableColors       => _availableColors;
        public bool                       UseAllDatabaseColors  => _useAllDatabaseColors;
        public ChainSpawnMode             ChainSpawnMode        => _chainSpawnMode;
        public int                        RandomSpawnCount      => _randomSpawnCount;
        public IReadOnlyList<BallColor>   InitialBalls          => _initialBalls;
        public IReadOnlyList<BallColor>   SpawnQueue            => _spawnQueue;
        public float                      SpawnInterval         => _spawnInterval;
        public int                        ScoreMultiplier       => _scoreMultiplier;

        public ChainConfig BuildChainConfig() => _chainConfig;

        /// <summary>
        /// Palette actually used at runtime: either all database colours or
        /// <see cref="AvailableColors"/> filtered to entries that exist in the database.
        /// </summary>
        public List<BallColor> ResolvePalette(BallDatabase database)
        {
            var buffer = new BallColor[16];
            int count = _useAllDatabaseColors
                ? database.FillAllDefined(buffer)
                : database.FillAvailable(_availableColors, buffer);

            var result = new List<BallColor>(count);
            for (int i = 0; i < count; i++) result.Add(buffer[i]);
            return result;
        }
    }
}
