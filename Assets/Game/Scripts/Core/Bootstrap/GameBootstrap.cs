using Game.Balls;
using Game.Chain;
using Game.Core.Events;
using Game.Core.Pooling;
using Game.Core.Services;
using Game.Core.States;
using Game.Level;
using Game.Shooter;
using Game.UI;
using UnityEngine;

namespace Game.Core.Bootstrap
{
    /// <summary>
    /// The single entry point that wires every system together.
    ///
    /// Composition root pattern: every dependency is resolved here, then
    /// passed to subsystems via Initialise() or Bind() calls. Gameplay
    /// scripts therefore never call Find/Resources/SingletonInstance — the
    /// references either come from the inspector or from this Initialise pass.
    ///
    /// Lifecycle:
    /// 1. Awake — build the event bus + state machine + score service, register
    ///    them in <see cref="ServiceLocator"/> so non-MonoBehaviour code can
    ///    reach them.
    /// 2. Start — initialise gameplay systems, then enter <see cref="PlayingState"/>.
    /// 3. Update — listen for Escape to toggle pause; everything else is
    ///    event-driven.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("Scene references")]
        [SerializeField] private GameObjectPool _pool;
        [SerializeField] private BallDatabase _ballDatabase;
        [SerializeField] private LevelDefinition _level;
        [SerializeField] private ChainController _chain;
        [SerializeField] private Shooter.Shooter _shooter;
        [SerializeField] private HudView _hud;
        [SerializeField] private PauseScreen _pauseScreen;
        [SerializeField] private WinScreen _winScreen;
        [SerializeField] private LoseScreen _loseScreen;

        [Header("Pool warmup")]
        [SerializeField] private GameObject[] _prewarmPrefabs;
        [SerializeField] private int[] _prewarmCounts;

        private EventBus _bus;
        private GameStateMachine _fsm;
        private ServiceLocator _services;
        private ScoreService _score;

        private void Awake()
        {
            _bus = new EventBus();
            _fsm = new GameStateMachine();

            // States — concrete instances, registered by type.
            _fsm.Register(new BootstrapState());
            _fsm.Register(new PlayingState(_bus));
            _fsm.Register(new PausedState(_bus));
            _fsm.Register(new WinState(_bus));
            _fsm.Register(new LoseState(_bus));

            _services = new ServiceLocator();
            _services.Register<IEventBus>(_bus);
            _services.Register(_fsm);
            _services.Register<IObjectPool>(_pool);
            _services.Register(_ballDatabase);
            ServiceLocator.SetCurrent(_services);

            _score = new ScoreService(_bus, _ballDatabase);

            // Pool warmup so the first wave of projectiles + balls is allocation-free.
            for (int i = 0; i < _prewarmPrefabs.Length && i < _prewarmCounts.Length; i++)
                if (_prewarmPrefabs[i] != null) _pool.Prewarm(_prewarmPrefabs[i], _prewarmCounts[i]);
        }

        private void Start()
        {
            _chain?.Initialise(_bus, _fsm);
            _shooter?.Initialise(_bus, _fsm);
            _hud?.Bind(_bus);
            _pauseScreen?.Bind(_bus, _fsm);
            _winScreen?.Bind(_bus);
            _loseScreen?.Bind(_bus);

            _fsm.ChangeState<PlayingState>();
        }

        private void Update()
        {
            // Pause toggle: lives at bootstrap level because pause is a
            // global concern that should not be owned by any gameplay system.
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_fsm.IsInState<PlayingState>()) _fsm.ChangeState<PausedState>();
                else if (_fsm.IsInState<PausedState>()) _fsm.ChangeState<PlayingState>();
            }
        }

        private void OnDestroy()
        {
            _score?.Dispose();
            ServiceLocator.Clear();
        }
    }
}
