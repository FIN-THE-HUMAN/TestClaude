using Game.Chain;
using UnityEngine;

namespace Game.Projectile
{
    /// <summary>
    /// Bridge component attached to each chain ball's *collider* GameObject.
    /// The trigger callback on <see cref="ProjectileBall"/> retrieves this
    /// to find the logical <see cref="ChainBall"/> record it just hit, then
    /// asks <see cref="ChainController"/> to insert into that location.
    ///
    /// Kept as a dedicated component (rather than dumping ChainBall onto the
    /// MonoBehaviour) because ChainBall is a plain C# class — adopting it as
    /// a MonoBehaviour would re-couple data to engine lifecycle.
    /// </summary>
    public sealed class ChainBallReference : MonoBehaviour
    {
        public ChainBall Ball;
    }
}
