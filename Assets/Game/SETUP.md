# Scene Setup

You have two options.

## Option A — Automatic (recommended)

1. Open the project in Unity 6.2.
2. Wait for Unity to import scripts. If it prompts you to import **TextMeshPro Essentials**, click **Import**.
3. From the top menu, click **Tools → Game → Build Demo Scene**.
4. Wait a few seconds. A dialog will confirm: *"Built at Assets/Game/Scenes/Demo.unity"*.
5. The scene is now open and pressing **Play** runs the game.

The builder script (`Assets/Game/Scripts/Editor/SceneScaffolder.cs`) creates:

- `Assets/Game/Generated/Materials/` — five coloured ball materials.
- `Assets/Game/Generated/Definitions/` — five `BallDefinition` assets.
- `Assets/Game/Generated/BallDatabase.asset` — populated with the definitions.
- `Assets/Game/Generated/Level_Demo.asset` — chain config, initial balls, spawn queue.
- `Assets/Game/Prefabs/BallView.prefab` — a sphere with `BallView` + trigger sphere collider.
- `Assets/Game/Prefabs/Projectile.prefab` — a sphere with `Rigidbody` + `BallView` + `ProjectileBall`.
- `Assets/Game/Scenes/Demo.unity` — the playable scene below.

You can re-run **Build Demo Scene** at any time to regenerate from scratch. It overwrites; existing edits to those assets are lost.

## Option B — Manual

If you'd rather wire things by hand, follow this layout. Every entry below corresponds to an inspector field that needs to be assigned.

### 1. Create assets first

| Asset | How |
| --- | --- |
| Materials (Red/Green/Blue) | `Assets → Create → Material`. Tint to taste. |
| BallDefinitions | `Assets → Create → Game/Balls/Ball Definition`. One per colour. Fill **Color** enum, **Display Color**, and link **View Prefab** (created next). |
| BallDatabase | `Assets → Create → Game/Balls/Ball Database`. Drag each `BallDefinition` into the **Definitions** list. |
| LevelDefinition | `Assets → Create → Game/Level/Level Definition`. Fill **Chain Config** (try BallDiameter=0.5, ForwardSpeed=1, CollapseCatchUp=6, MergeEpsilon=0.001, MinMatch=3). Set **Available Colors** = Red, Green, Blue. Fill **Initial Balls** and **Spawn Queue** with whatever sequences you like; set **Spawn Interval** to 0.6. |

### 2. Build the BallView prefab

1. `GameObject → 3D Object → Sphere`. Rename to **BallView**. Scale to (0.5, 0.5, 0.5).
2. On the SphereCollider, tick **Is Trigger**.
3. Add component **BallView**. Inspector should auto-fill the renderer and visual root via `Reset()`; if not, drag the MeshRenderer into `_renderer` and the transform into `_visualRoot`.
4. Drag the GameObject into `Assets/Game/Prefabs/` to make it a prefab.
5. Open each `BallDefinition` asset and assign this prefab into **View Prefab**.

### 3. Build the Projectile prefab

1. `GameObject → 3D Object → Sphere`. Rename to **Projectile**. Scale ~(0.45, 0.45, 0.45).
2. On the SphereCollider, tick **Is Trigger**.
3. Add **Rigidbody**: Use Gravity = OFF, Collision Detection = Continuous, Interpolate = Interpolate.
4. Add **BallView** (same wiring as before).
5. Add **ProjectileBall**. Drag the BallView component into `_view`. Drag the Rigidbody into `_rigidbody`.
6. Drag into `Assets/Game/Prefabs/`.

### 4. Build the scene hierarchy

```
GameRoot
├── Pool           (GameObjectPool component)
├── BallParent     (empty Transform — holds ball view instances at runtime)
├── Path           (WaypointPath component)
│   ├── WP_00      (empty children placed where you want the chain to travel)
│   ├── WP_01
│   ├── ...
│   └── WP_n
├── Chain          (ChainController component)
├── Shooter
│   ├── Input      (MouseShooterInput component; assign Camera = Main Camera)
│   ├── Muzzle     (empty child at +Z offset; this is where projectiles spawn)
│   ├── CurrentPreview  (instance of BallView prefab)
│   └── NextPreview     (instance of BallView prefab, smaller)
└── Bootstrap      (GameBootstrap component)

Main Camera      (Orthographic, top-down — Position (0,10,0), Rotation (90,0,0), Size 6)
Directional Light

UI (Canvas, Screen Space Overlay)
├── HUD                (HudView component)
│   ├── Score          (TextMeshProUGUI)
│   ├── Combo          (TextMeshProUGUI)
│   ├── CurrentSwatch  (Image)
│   └── NextSwatch     (Image)
├── PauseScreen        (Image background covering screen; PauseScreen component on it; child label)
├── WinScreen          (same shape, WinScreen component)
└── LoseScreen         (same shape, LoseScreen component)

EventSystem      (auto-created when you add the canvas)
```

### 5. Wire references

Open each component and assign its serialized fields:

**ChainController**
- `_path` → Path GameObject
- `_pool` → Pool GameObject
- `_ballDatabase` → BallDatabase asset
- `_level` → LevelDefinition asset
- `_ballParent` → BallParent transform

**Shooter**
- `_input` → MouseShooterInput on the Input child
- `_muzzle` → Muzzle child
- `_currentPreview` → BallView component on CurrentPreview
- `_nextPreview` → BallView component on NextPreview
- `_pool` → Pool GameObject
- `_database` → BallDatabase asset
- `_level` → LevelDefinition asset
- `_projectilePrefab` → ProjectileBall component on the Projectile prefab
- `_chain` → ChainController on the Chain GameObject

**HudView**
- `_scoreLabel` → Score TMP_Text
- `_comboLabel` → Combo TMP_Text
- `_currentSwatch` → CurrentSwatch Image
- `_nextSwatch` → NextSwatch Image
- `_database` → BallDatabase asset

**PauseScreen / WinScreen / LoseScreen**
- `_root` → the panel GameObject itself (so it can be enabled/disabled by the event).

**GameBootstrap**
- `_pool` → Pool
- `_ballDatabase` → BallDatabase asset
- `_level` → LevelDefinition asset
- `_chain` → ChainController
- `_shooter` → Shooter
- `_hud`, `_pauseScreen`, `_winScreen`, `_loseScreen` → their respective components
- `_prewarmPrefabs` (size 2): [0] BallView prefab, [1] Projectile prefab
- `_prewarmCounts` (size 2): [0] 32, [1] 8

## Controls

| Input | Action |
| --- | --- |
| Mouse X | Move the shooter along the bottom of the screen |
| Left Mouse | Fire the current ball |
| Right Mouse | Swap current ↔ next ball |
| Esc | Pause / Resume |

## Smoke test

After pressing Play you should see:

1. A spiral path of waypoints with balls flowing from one end toward the other.
2. The shooter at the bottom following the mouse.
3. Left-click fires a ball — it should hit the chain and insert at the impact point.
4. Three of the same colour adjacent to each other vanish; the chain behind the gap surges forward to close it.
5. The HUD score increases. A combo label appears for cascades.
6. If the head of the chain reaches the path end you lose; if the chain is empty and the spawn queue is drained you win.

## Common issues

- **"BallView is null" or balls are invisible** — the BallDefinition's `_viewPrefab` is unset, or the BallDatabase doesn't list that definition.
- **Projectile passes through chain** — Rigidbody is not set to Continuous collision detection, or the projectile's SphereCollider is not a trigger, or the chain ball prefab is missing the `BallView` component / trigger collider.
- **Nothing happens on click** — `_input` reference on Shooter is unset; or the camera reference inside MouseShooterInput is unset; or the scene has no GameBootstrap GameObject so the state machine never entered `Playing`.
- **TMP error on play** — Window → TextMeshPro → Import TMP Essential Resources.
