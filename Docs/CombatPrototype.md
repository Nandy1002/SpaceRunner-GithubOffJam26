# Health and enemy modules

Open `Assets/_Scenes/WatchPrototype.unity`, press Play, then **START RUN**. The long octagonal tunnel has red towers and a purple melee enemy. The lower-left HUD shows player health. Geometry and enemy placement are saved as editable scene objects. See [SceneEditing.md](SceneEditing.md) to move the player, walls or enemies.

## Current rules

| Feature | Default behaviour |
| --- | --- |
| Player health | 100 HP; the HUD updates immediately after damage. |
| Tower | Stationary, 10-unit spherical attack range, one shot every 1.5 seconds. A ready tower fires when the player enters its range. |
| Bullet | 25 damage, 12 units/second. Aims at the player's collider centre at launch, then travels in a straight line without tracking. |
| Leaving range | Every surviving bullet from that tower starts its own 10-second countdown when the player leaves. The tower remains. Re-entering does not reset an existing countdown. |
| Impact | A bullet is destroyed on its first solid collision, including walls and enemies. Only hitting the player deals damage. Bullets ignore their firing tower and other bullets. |
| Melee | Follows the player at 1.8 units/second using ground navigation. Contact kills the player regardless of remaining HP. Its contact volume cannot attack through a wall. |
| Death | Ends the timer, closes the watch, removes the encounter and shows **YOU DIED** on the main menu. **START RUN** restores full health and respawns the enemies. |
| Watch pause enabled | Freezes enemy movement/firing, bullets and their expiry countdowns; player damage is blocked. |
| Watch pause disabled | Combat and damage continue while the watch is open, alongside world time and player controls. |

The 10 seconds are **gameplay seconds**, excluding pauses. A collision or the end of a run can remove a bullet sooner. A missed bullet does not begin this countdown while the player remains in range.

## Prefabs and Inspector settings

Combat prefabs are in `Assets/Prefabs/Combat`; the health HUD is in `Assets/Prefabs/UI/HealthHUD.prefab`.

| Prefab | Editable settings |
| --- | --- |
| `CombatSystem.prefab` | Player Maximum Health; Health HUD; optional Ground Navigation Region; Spawns list for explicit prefab/spawn-point pairs; optional Scene Enemy Layout for visible scene prefab placement. |
| `TriggerTower.prefab` | Attack Range, Shot Interval, Aim Pivot, Muzzle and Projectile Prefab. |
| `TowerProjectile.prefab` | Damage, Speed, Seconds After Range Exit, Impact Layers; SphereCollider radius controls collision size. Keep the root at scale 1 and the projectile on Ignore Raycast (layer 2). |
| `MeleeEnemy.prefab` | Move Speed, Repath Interval, Target Projection Distance, Attack Origin; NavMeshAgent radius/height; Contact Kill Volume capsule for contact reach. |
| `HealthHUD.prefab` | Text, Slider fill/background sprites, colours, font and RectTransform layout. The HUD hides when using the watch or gravity selector. |

To add more enemies, duplicate a spawn Transform inside a **variant** of CombatSystem and add an entry to Spawns. Each run instantiates those entries; ending the run cleans up enemies and projectiles together. Each tower checks its own range independently.

In the octagonal example, use **Enemy Layout - Move or Duplicate These** instead: move or duplicate those scene prefab instances before Play. CombatSystem reads the layout at initialization, hides the templates, and creates active copies for each run. The example's explicit Spawns list is empty, so there are no duplicate spawn entries to maintain.

`Space Runner > Create Missing Combat Prefabs` creates missing prototype assets. It preserves existing prefabs and materials. It is an editor setup utility, not a runtime graphics generator. Scene placement is edited independently.

## Artist handoff

- Replace children named **Visual**, **Visuals**, **Head**, or **Barrel** with meshes/prefabs. Preserve the gameplay components, colliders, Aim Pivot and Muzzle references. Match the collider dimensions to the new model.
- Tower rotation is applied to **Aim Pivot**. Place the muzzle just beyond the barrel, with local +Z pointing forward.
- Melee visuals face local +Z and stand on the root's ground plane. Keep the NavMeshAgent on the root; its movement drives the model. Animator locomotion can read agent velocity. Do not enable root motion without a movement adapter.
- A projectile has an independent visual child; swapping its mesh/material does not change its trajectory or damage code.
- Health HUD uses ordinary Unity UI components stored in a prefab. Replace sprites/fonts/layout directly. Runtime code only updates the label, bar value and visibility.

## Integrating another level

`CombatSystem` is initialized explicitly by the level composition code after the player, session and world geometry exist. The example does this in `WatchPrototypeBootstrap`:

```csharp
watchSystem.Initialize(playerController);
combatSystem.Initialize(
    watchSystem.Session,
    playerController.transform,
    playerController.GetComponent<Collider>());
// Start from your menu when ready:
watchSystem.Session.StartRun();
```

Use one CombatSystem per player/session. It adds or reuses HealthComponent and CombatTarget on the player. Damage is allowed only during a running, unpaused session. Other hazards can call `HealthComponent.TakeDamage(amount)` or `Kill()` without depending on the UI or enemy types. Subscribe to `Changed` for feedback and `Died` for death effects. An existing player-health component's maximum is configured by CombatSystem at initialization.

An authored level can use its existing baked NavMesh: clear **Navigation Region** on CombatSystem and remove the prototype GroundNavigationRegion component. Place melee spawn points on a valid NavMesh. The optional region collects physics colliders on Ground (6) and Wall (7), inside its configured world-aligned bounds, then builds once at initialization. Adjust these layers and bounds for a different level. Move/rotate spawn Transforms normally; the prototype navigation bounds remain aligned with world axes.

Navigation uses Unity's [NavMeshBuilder](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AI.NavMeshBuilder.BuildNavMeshData.html) and [NavMeshAgent](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AI.NavMeshAgent.html), without a new package dependency. This first melee enemy is **ground based**. Changing player gravity does not make it walk on walls or ceilings. It follows the nearest reachable ground position; it stops requesting a destination if the player is too far from navigation. Gaps require authored navigation links. It must physically reach the player to kill them.

## Code boundaries

| Module | Responsibility |
| --- | --- |
| Scripts/Core/HealthState | Damage clamping, one death notification per life, full reset. No Unity dependency. |
| Scripts/Core/DepartureLifetime | One-way countdown, driven by gameplay delta time. No Unity dependency. |
| Scripts/Combat/HealthComponent + CombatTarget | Health API and explicit target/collider ownership. |
| Scripts/Combat/TriggerTower + TowerProjectile | Range/firing cadence; fixed trajectory, swept collision and expiry. |
| Scripts/Combat/MeleeEnemy + GroundNavigationRegion | Navigation, slow pursuit and lethal contact. |
| Scripts/Combat/CombatSystem | Session integration, death outcome, run reset and encounter lifetime. |
| Scripts/UI/HealthHudView | Prefab UI binding only. |

Create another enemy by deriving from EnemyActor and implementing its behaviour using the injected Session, Target and Projectiles container. Use CanAct before simulation; avoid direct cursor, timer or Time.timeScale changes in enemies. Session pause ownership remains shared with the watch and other systems.

## Verification

Automated coverage includes health/reset/death rules, the ten-second countdown, tower range, non-homing trajectory, single-hit damage, thin-wall collision, pause/resume, ground pathfinding around the test pedestal, lethal contact and wall obstruction. The existing watch/input/gravity tests run alongside combat tests.

Validated on **Unity 6000.6.8f1** in an isolated project copy: **15 EditMode tests and 14 PlayMode tests passed**. A Windows player also passed real tower damage, watch pause and melee death checks; HUD and death-menu screenshots were inspected at 1280 x 800. Temporary player-check scripts are excluded from the delivered build.

Run the Windows prototype from `Builds/WatchPrototype/SpaceRunner.exe`, or rebuild with **Space Runner > Build Windows Prototype**. Keep the executable with its adjacent data folder and Unity DLLs when sharing the build.
