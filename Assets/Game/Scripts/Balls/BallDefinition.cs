using UnityEngine;

namespace Game.Balls
{
    /// <summary>
    /// Data-only description of a ball colour: its enum id, display colour,
    /// visual prefab, and any optional VFX/SFX overrides. Decoupling this
    /// from the gameplay code means designers add or rebalance colours
    /// without touching scripts.
    ///
    /// All <see cref="BallView"/> prefabs should share the same root
    /// component layout; only the materials/meshes vary by colour.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Balls/Ball Definition", fileName = "BallDefinition_")]
    public sealed class BallDefinition : ScriptableObject
    {
        [SerializeField] private BallColor _color = BallColor.Red;
        [SerializeField] private Color     _displayColor = UnityEngine.Color.red;
        [Tooltip("Visual prefab. Must contain a BallView component on the root.")]
        [SerializeField] private BallView  _viewPrefab;
        [Tooltip("Optional VFX prefab spawned on match removal. Falls back to a global default.")]
        [SerializeField] private GameObject _popVfxPrefab;
        [SerializeField] private int _scorePerBall = 10;

        public BallColor  Color         => _color;
        public Color      DisplayColor  => _displayColor;
        public BallView   ViewPrefab    => _viewPrefab;
        public GameObject PopVfxPrefab  => _popVfxPrefab;
        public int        ScorePerBall  => _scorePerBall;
    }
}
