using System.Collections.Generic;
using UnityEngine;

namespace Game.Balls
{
    /// <summary>
    /// Lookup table mapping <see cref="BallColor"/> → <see cref="BallDefinition"/>.
    /// One asset for the whole game; injected wherever a system needs to materialise
    /// or query ball data. Builds a sparse internal array on enable so the hot path
    /// is an O(1) index, not a dictionary lookup.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Balls/Ball Database", fileName = "BallDatabase")]
    public sealed class BallDatabase : ScriptableObject
    {
        [SerializeField] private List<BallDefinition> _definitions = new();

        // Sparse array indexed by the underlying byte value of BallColor.
        // None = 0 stays null which is the desired "missing" sentinel.
        private BallDefinition[] _byColor;

        private void OnEnable() => Rebuild();
        private void OnValidate() => Rebuild();

        private void Rebuild()
        {
            // Enum.GetValues allocates — but this only runs on enable/validate.
            var max = 0;
            foreach (var name in System.Enum.GetValues(typeof(BallColor)))
                if ((byte)name > max) max = (byte)name;
            _byColor = new BallDefinition[max + 1];

            for (int i = 0; i < _definitions.Count; i++)
            {
                var def = _definitions[i];
                if (def == null) continue;
                _byColor[(byte)def.Color] = def;
            }
        }

        public BallDefinition Get(BallColor color)
        {
            if (_byColor == null) Rebuild();
            var idx = (byte)color;
            return idx < _byColor.Length ? _byColor[idx] : null;
        }

        /// <summary>
        /// Returns all colours that have a definition AND appear in the supplied
        /// allowed-colours mask. Used by the spawner to pick legal random colours.
        /// The result is allocated by the caller (Buffer) to avoid GC.
        /// </summary>
        public int FillAvailable(IReadOnlyList<BallColor> allowed, BallColor[] buffer)
        {
            int n = 0;
            for (int i = 0; i < allowed.Count && n < buffer.Length; i++)
                if (Get(allowed[i]) != null) buffer[n++] = allowed[i];
            return n;
        }

        /// <summary>Writes every defined colour into <paramref name="buffer"/>.</summary>
        public int FillAllDefined(BallColor[] buffer)
        {
            int n = 0;
            for (int i = 0; i < _definitions.Count && n < buffer.Length; i++)
            {
                var def = _definitions[i];
                if (def == null) continue;
                buffer[n++] = def.Color;
            }
            return n;
        }
    }
}
