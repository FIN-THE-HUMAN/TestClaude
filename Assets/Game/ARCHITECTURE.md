# Architecture Overview

A Zuma / Butterfly Escape style arcade puzzle game built for Unity 6.2. The code is organised so the *hard* parts of this genre — chain insertion, match cascades, gap collapsing, view/data synchronisation — are isolated, unit-testable, and replaceable.

## Layered structure

```
Assets/Game/Scripts
├── Core/         Engine-adjacent infrastructure (event bus, FSM, pooling, services)
├── Path/         IPath + concrete WaypointPath. Arc-length parameterised.
├── Balls/        Data-only ball definition, colour-keyed database, BallView (visuals)
├── Chain/        The heart: model, segments, insertion / match / collapse resolvers
├── Shooter/      Player input, ammo source, Shooter MonoBehaviour
├── Projectile/   Rigidbody-driven shot, trigger collision into ChainController
├── Level/        LevelDefinition SO, ScoreService
├── Monster/      Visual-only Butterfly-Escape style follower
├── UI/           HUD, Pause, Win, Lose screens
└── Editor/       Editor-only tools (WaypointPathEditor)
```

Each folder owns its namespace; the assembly definition (`Game.Runtime.asmdef`) compiles the whole runtime as a single unit while keeping editor code in `Game.Editor.asmdef`.

## Core principles

| Principle | How it is enforced |
| --- | --- |
| **Data ≠ logic ≠ view** | `BallDefinition` (data) → `ChainBall` (data) → `BallView` (visual). The chain controller is the only thing that pairs them. |
| **Distance-along-path, not transform translation** | Every chain ball stores `DistanceAlongPath`. `ChainController.SyncViews` re-derives the world position via `IPath.Sample` every tick. Inserting, removing, or merging never writes to a Transform directly. |
| **Event-driven cascades** | The match pipeline is `Insert → ResolveMatches → Remove → Collapse → ResolveAtBoundary`. No subsystem calls `CheckMatches()` in `Update`. Cascades are produced as side-effects of segment merges. |
| **SOLID / open-closed** | `IPath`, `IObjectPool`, `IAmmoSource`, `IShooterInput`, `IEventBus`. Each has a concrete v1 implementation and a clear extension seam. |
| **ScriptableObject-driven** | `BallDefinition`, `BallDatabase`, `LevelDefinition`, `MonsterDefinition`. Designers can ship balance changes and new levels without code edits. |
| **Serialised references, not Find()** | `GameBootstrap` is the composition root; every system has its dependencies injected via `Initialise(...)` / `Bind(...)` or via inspector fields. |
| **Pool everything** | Projectiles and ball views are pooled via `GameObjectPool` (which implements `IObjectPool` so it can be swapped). `ChainBall` records have a per-controller free list. |
| **State machine, not flags** | `GameStateMachine` registers `Bootstrap`, `Playing`, `Paused`, `Win`, `Lose`. Gameplay systems gate behaviour with `_fsm.IsInState<PlayingState>()` instead of `if (gameOver)` sprinkled around. |

## The chain pipeline in detail

### Invariants (`ChainSegment`)
1. `Balls` is ordered FRONT → BACK (front = higher distance along path).
2. `Balls[i].DistanceAlongPath == HeadDistance - i * BallDiameter`.
3. `HeadDistance == Balls[0].DistanceAlongPath`.
4. Adjacent segments are separated by a positive gap; merging is the only legal way to close it.

These invariants are the contract that makes insertion math O(k) instead of O(n²).

### Insertion (`ChainInsertionResolver`)
"Shift back, hold front fixed":
- Balls in front of the insertion point keep their distance.
- The new ball sits one BallDiameter behind the ball it is inserted in front of.
- All balls at or behind the insertion index shift backward by one BallDiameter.

This convention prevents projectiles from shoving the chain forward and causing accidental losses. The only exception is inserting at index 0 (impact on the very nose of the chain), where the new ball legitimately extends the front.

The "side" decision is a clean dot product: `sign(dot(projectile - target, pathTangentAtTarget))`. This works on curved sections because the tangent is sampled at the target's path distance.

### Match resolution (`ChainMatchResolver`)
Scans outward from an anchor index (post-insert) or seam index (post-merge). On a hit of length ≥ `MinMatch`, four sub-cases:
- range at the segment front → trim front, update `HeadDistance`
- range at the segment back → trim back
- entire segment matched → drop the segment
- interior range → SPLIT the segment into two; the new "right" segment is inserted right after the original.

Returns a structured `Outcome` so the controller can publish a single `MatchResolvedEvent` and the score / VFX / sound layers can react.

### Collapse (`ChainCollapseResolver`)
Every tick:
1. Front segment advances at `ForwardSpeed`.
2. Each trailing segment advances at `ForwardSpeed + CollapseCatchUp` while a positive gap exists.
3. When a gap closes (≤ `MergeEpsilon`), the trailing segment snaps flush and is appended to the segment ahead.
4. Every produced merge is reported with its seam index; the controller then asks the match resolver to check that seam — this is the source of chain reactions.
5. All segments are `Resync()`-ed to honour invariant (2).

The cascade therefore runs naturally over multiple ticks: a match removes balls → segments become separated → collapse drives them back together → seam match → new removal → repeat.

## Lifecycle / wiring

```
GameBootstrap.Awake
  ├── new EventBus
  ├── new GameStateMachine + register states
  ├── new ServiceLocator + register bus, fsm, pool, database
  ├── new ScoreService (subscribes to MatchResolvedEvent)
  └── pool.Prewarm(prefab, count) for projectiles + ball views

GameBootstrap.Start
  ├── chain.Initialise(bus, fsm)
  ├── shooter.Initialise(bus, fsm)
  ├── hud.Bind(bus), pauseScreen.Bind(bus, fsm), winScreen.Bind(bus), loseScreen.Bind(bus)
  └── fsm.ChangeState<PlayingState>()
```

## Extension points

| Want to add… | Edit |
| --- | --- |
| A new ball colour | Add to `BallColor` enum, create a `BallDefinition` asset, drop it into `BallDatabase` |
| A new path type (Bezier, Catmull-Rom) | Implement `IPath` |
| A weighted ammo source | Implement `IAmmoSource`; swap the line in `Shooter.Initialise` |
| Touch / gamepad input | Implement `IShooterInput`; replace `MouseShooterInput` in the inspector |
| Special balls (bombs, multipliers) | Extend the placeholder list on `LevelDefinition`, add a new resolver that listens to `BallInsertedEvent` |
| Different monster behaviour | New `MonsterDefinition` + new follower component; chain logic does not change |

## Why no static singletons?

`ServiceLocator.Current` is the closest the codebase comes to a singleton, and it is owned by `GameBootstrap` — it is set in `Awake` and cleared in `OnDestroy`, so reloading the scene gives a clean slate. Every gameplay system receives its dependencies through Initialise/Bind; the locator only exists for non-MonoBehaviour code that has no other way to reach the bus.
