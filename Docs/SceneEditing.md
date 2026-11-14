# Editing the octagonal prototype

Open `Assets/_Scenes/WatchPrototype.unity` in Unity **6000.6.8f1**. The level is saved in the scene and is visible before Play. Stop Play mode before changing placement, then save the scene.

The initial tunnel is **160 units long**, with **28 units between opposite inner surfaces**, extending along world Z. Its cross section has eight sides. It contains four interior walls, four towers and one ground melee enemy. The tube is stationary.

## Where to edit

Expand **Watch Prototype** in the Hierarchy:

| Object | What to change |
| --- | --- |
| **Octagonal Tunnel / Eight Inner Surfaces** | Eight ordinary cube panels. Move/rotate/scale the floor, ceiling and six other surfaces using Transform. |
| **Octagonal Tunnel / Interior Walls - Move These** | Move, rotate, resize or duplicate the obstacle walls. Keep blocking walls on the Wall layer. |
| **Octagonal Tunnel / Entry End Cap**, **Far End Cap** | Normal mesh objects using the shared octagonal mesh asset. Move them when extending or shortening the tunnel. |
| **Octagonal Tunnel / Support Rings** | Decorative ordinary objects; freely adjust or remove them. |
| **Octagonal Tunnel / Lighting and Guide Strips** | Editable lights and guide-strip objects. |
| **Player** | The teammate's player prefab instance. Its saved Transform is the initial spawn position and the position restored for each new run. |
| **Enemy Layout - Move or Duplicate These** | Visible enemy prefab instances. Move/rotate them to set enemy placement, edit their Inspector parameters, or duplicate them to add more enemies. Disabled layout instances are omitted from the run. |
| **Watch System** | Watch prefab and player binding. |
| **Combat System** | Health HUD, Scene Enemy Layout reference and navigation bounds. |

The eight tube surfaces use **Ground** so they can serve as landing surfaces when player gravity changes. Interior obstacles use **Wall**. The melee enemy remains ground based; it does not follow the player onto the ceiling.

## Enemy layout and restarting

Before Play, the enemy layout contains actual prefab instances with visible meshes. When the session initializes, CombatSystem reads those instances as spawn templates and hides them. Each run creates active gameplay copies under **Combat System / Current Run Enemies and Projectiles**. Death or END RUN removes those copies; the next run recreates them at the layout positions. This keeps movement during a run from changing the authored placement.

Edit the **Enemy Layout** instances before Play rather than the temporary copies. Duplicating an enemy inside that layout is sufficient; no code or separate spawn-list entry is required. The melee prefab starts with its NavMeshAgent disabled; MeleeEnemy enables navigation on the gameplay copy after the level's navigation data exists.

## Navigation and geometry

GroundNavigationRegion builds navigation from the saved colliders at initialization. If you extend the level, update its **Center** and **Size** on Combat System to include the new geometry. Changes made to walls before entering Play are included in the next navigation build. Moving walls during Play does not automatically rebuild static navigation.

Materials are regular assets in `Assets/Graphics/Materials/Prototype`. The cap mesh is `Assets/Graphics/Meshes/Octagonal End Cap.asset`. Replace these resources or individual meshes normally.

`PrototypeSceneAuthoring` is a one-time editor utility used to author the initial scene. It is not included in the game player and does not run when pressing Play. Running **Space Runner > Author Octagonal Prototype (Once)** again preserves the existing tunnel. `WatchPrototypeBootstrap` only connects scene references; it no longer generates level geometry, materials, or a player at hardcoded coordinates.

## Validation

All 14 PlayMode tests pass in the authored tunnel, including placement-driven spawning and preservation of scene geometry across run restarts. Existing core state tests remain unchanged (15 passed). A Windows player was checked at 1280 x 800 for the first-person tunnel view, eight-sided interior and watch UI.
