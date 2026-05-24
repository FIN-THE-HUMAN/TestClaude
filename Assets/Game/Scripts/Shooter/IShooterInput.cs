using UnityEngine;

namespace Game.Shooter
{
    /// <summary>
    /// Abstraction over the input device. The default implementation reads
    /// the legacy Input API in Unity 6.2; switching to the new Input System
    /// requires implementing this interface and assigning it on the shooter.
    /// This is what keeps the Shooter MonoBehaviour testable from edit-mode
    /// tests — feed a fake IShooterInput and assert behaviour.
    /// </summary>
    public interface IShooterInput
    {
        /// <summary>Cursor X in world units, projected onto the shooter's track plane.</summary>
        float HorizontalAxisWorld { get; }
        bool  FirePressed     { get; }
        bool  SwapPressed     { get; }
    }
}
