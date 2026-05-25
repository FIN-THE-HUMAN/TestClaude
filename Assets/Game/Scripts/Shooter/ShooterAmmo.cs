using System;
using Game.Balls;
using Game.Core.Events;
using UnityEngine;

namespace Game.Shooter
{
    /// <summary>
    /// Manages the "current + next" ammunition pair the player sees.
    ///
    /// Design choices:
    /// - The next-ball selection is delegated to an <see cref="IAmmoSource"/>
    ///   so we can implement different strategies (uniform random, weighted
    ///   by colours still in chain, hand-authored sequence) without touching
    ///   the shooter.
    /// - On <see cref="Consume"/>, current becomes the prior next and a new
    ///   next is drawn. This means the player always sees the *next* shot
    ///   one step ahead — a cornerstone of Zuma-style strategy.
    /// </summary>
    public sealed class ShooterAmmo
    {
        public BallColor Current { get; private set; } = BallColor.None;
        public BallColor Next    { get; private set; } = BallColor.None;

        public event Action<BallColor, BallColor> Changed;

        private readonly IAmmoSource _source;
        private readonly IEventBus   _bus;

        public ShooterAmmo(IAmmoSource source, IEventBus bus)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _bus    = bus;
        }

        public void Prime()
        {
            Current = _source.Draw();
            Next    = _source.Draw();
            RaiseChanged();
        }

        public BallColor Consume()
        {
            var fired = Current;
            Current = Next;
            Next    = _source.Draw();
            RaiseChanged();
            return fired;
        }

        public void Swap()
        {
            (Current, Next) = (Next, Current);
            RaiseChanged();
        }

        private void RaiseChanged()
        {
            Changed?.Invoke(Current, Next);
            _bus?.Publish(new AmmoChangedEvent(Current, Next));
        }
    }
}
