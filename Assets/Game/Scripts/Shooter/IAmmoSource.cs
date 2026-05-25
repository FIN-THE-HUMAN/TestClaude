using Game.Balls;

namespace Game.Shooter
{
    /// <summary>
    /// Strategy for picking the next ball colour to load into the shooter.
    /// Concrete implementations: RandomAmmoSource (uniform random), and
    /// future "WeightedByChainColours" / "AuthoredSequence" sources.
    /// </summary>
    public interface IAmmoSource
    {
        BallColor Draw();
    }
}
