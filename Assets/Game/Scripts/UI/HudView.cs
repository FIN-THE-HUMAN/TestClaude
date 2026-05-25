using Game.Balls;
using Game.Core.Events;
using Game.Core.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// HUD bindings. Strictly passive — subscribes to events and writes to
    /// UI fields; never reads gameplay state directly. Detaches in OnDisable
    /// so reloading the scene does not double-subscribe.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        [Header("Score / Combo")]
        [SerializeField] private TMP_Text _scoreLabel;
        [SerializeField] private TMP_Text _comboLabel;

        [Header("Ammo previews")]
        [SerializeField] private Image _currentSwatch;
        [SerializeField] private Image _nextSwatch;
        [SerializeField] private BallDatabase _database;

        private IEventBus _bus;
        private int _combo;
        private int _comboDecayFrames;

        private void OnEnable()
        {
            // ServiceLocator.Current may be null during scene boot — defer subscription.
            if (ServiceLocator.Current != null && ServiceLocator.Current.TryResolve<IEventBus>(out _bus))
                Subscribe();
        }

        private void OnDisable()
        {
            if (_bus != null) Unsubscribe();
        }

        public void Bind(IEventBus bus)
        {
            if (_bus != null) Unsubscribe();
            _bus = bus;
            Subscribe();
        }

        private void Subscribe()
        {
            _bus.Subscribe<ScoreChangedEvent>(OnScore);
            _bus.Subscribe<MatchResolvedEvent>(OnMatch);
            _bus.Subscribe<AmmoChangedEvent>(OnAmmo);
        }

        private void Unsubscribe()
        {
            _bus.Unsubscribe<ScoreChangedEvent>(OnScore);
            _bus.Unsubscribe<MatchResolvedEvent>(OnMatch);
            _bus.Unsubscribe<AmmoChangedEvent>(OnAmmo);
        }

        private void Update()
        {
            // Trivial combo decay so the displayed combo number fades off-screen.
            // Avoids a separate timer system for one label.
            if (_comboDecayFrames > 0)
            {
                _comboDecayFrames--;
                if (_comboDecayFrames == 0 && _comboLabel != null) _comboLabel.text = string.Empty;
            }
        }

        private void OnScore(ScoreChangedEvent ev)
        {
            if (_scoreLabel != null) _scoreLabel.text = ev.Score.ToString();
        }

        private void OnMatch(MatchResolvedEvent ev)
        {
            _combo = ev.ComboDepth;
            _comboDecayFrames = 90;
            if (_comboLabel != null) _comboLabel.text = _combo > 1 ? $"Combo x{_combo}" : string.Empty;
        }

        private void OnAmmo(AmmoChangedEvent ev)
        {
            if (_database == null) return;
            if (_currentSwatch != null)
            {
                var def = _database.Get(ev.Current);
                if (def != null) _currentSwatch.color = def.DisplayColor;
            }
            if (_nextSwatch != null)
            {
                var def = _database.Get(ev.Next);
                if (def != null) _nextSwatch.color = def.DisplayColor;
            }
        }
    }
}
