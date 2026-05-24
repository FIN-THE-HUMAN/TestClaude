using Game.Chain;
using Game.PathSystem;
using UnityEngine;

namespace Game.Monster
{
    /// <summary>
    /// Visual-only follower that sits at the head of the chain to suggest
    /// "the frog/monster is pushing the balls" (Butterfly Escape style).
    ///
    /// CRITICAL: this script never writes to the chain model. The chain
    /// drives its own motion. The monster simply samples
    /// <see cref="ChainController.Model"/> + <see cref="IPath"/> each frame
    /// and snaps to a small offset ahead of the front ball. Decoupling like
    /// this is what lets us swap monster behaviours (sliding frog,
    /// shadow stalker, etc.) without ever risking gameplay regressions.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MonsterFollower : MonoBehaviour
    {
        [SerializeField] private ChainController _chain;
        [SerializeField] private float _leadDistance = 0.4f;
        [SerializeField] private Animator _animator;
        [SerializeField] private MonsterDefinition _definition;

        private void LateUpdate()
        {
            if (_chain == null || _chain.Path == null) return;
            var model = _chain.Model;
            if (model.Segments.Count == 0) return;

            float headDist = model.FrontHeadDistance + _leadDistance;
            _chain.Path.Sample(headDist, out var pos, out var tan);
            transform.SetPositionAndRotation(pos, tan.sqrMagnitude > 0f ? Quaternion.LookRotation(tan, Vector3.up) : transform.rotation);

            if (_animator != null && _definition != null && !string.IsNullOrEmpty(_definition.RunningBool))
                _animator.SetBool(_definition.RunningBool, true);
        }

        public void PlayEat()
        {
            if (_animator != null && _definition != null && !string.IsNullOrEmpty(_definition.EatTrigger))
                _animator.SetTrigger(_definition.EatTrigger);
        }
    }
}
