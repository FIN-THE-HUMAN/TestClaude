using Game.Balls;
using Game.Core.Events;

namespace Game.Level
{
    /// <summary>
    /// Accumulates the player's score from match events. Listens to
    /// <see cref="MatchResolvedEvent"/> via the event bus — gameplay code
    /// never calls Score.Add() directly. This means a future scoring rule
    /// (combo multipliers, time bonuses, special-ball multipliers) lives
    /// entirely inside this service.
    /// </summary>
    public sealed class ScoreService
    {
        private readonly IEventBus _bus;
        private readonly BallDatabase _database;

        public int Current { get; private set; }
        public int BestCombo { get; private set; }

        public ScoreService(IEventBus bus, BallDatabase database)
        {
            _bus = bus;
            _database = database;
            _bus.Subscribe<MatchResolvedEvent>(OnMatch);
        }

        public void Dispose() => _bus.Unsubscribe<MatchResolvedEvent>(OnMatch);

        private void OnMatch(MatchResolvedEvent ev)
        {
            var def    = _database.Get(ev.Color);
            var perBall = def != null ? def.ScorePerBall : 10;
            // Quadratic combo bonus: deeper cascades reward disproportionately.
            int delta = perBall * ev.Count * ev.ComboDepth;
            Current += delta;
            if (ev.ComboDepth > BestCombo) BestCombo = ev.ComboDepth;
            _bus.Publish(new ScoreChangedEvent(Current, delta));
        }
    }
}
