# Project structure

Resources use the existing team categories. Gameplay modules are grouped below Scripts; prefabs and art have their own shared locations.

```text
Assets/
  Scripts/
    Core/                  Pause, timer, health and projectile lifetime state
    Input/                 Teammate input manager and generated input actions
    Player/                Teammate movement, dash, wall run and gravity
    Watch/                 Watch presentation and interaction
      Input/               R / Esc menu input
    Combat/                Health, tower, projectile, melee and encounter setup
    Session/               Run lifecycle, pause ownership and saved settings
    Integration/           Adapters connecting the session to teammate controls
    UI/                    Watch menu, settings, main menu and health HUD
      Gravity/             Teammate gravity selector
    Demo/                  Scene references and example composition
  Prefabs/
    Player.prefab          Teammate's original player asset
    Watch/                 Watch system, rig and placeholder arm
    UI/                    Watch menu, screen UI, gravity menu and health HUD
    Combat/                Combat system, tower, melee and projectile
  Graphics/
    Materials/             Shared materials and existing shader graph
    Sprites/               Replaceable UI images
    ParticleEffects/       Teammate speed particles
  _Scenes/
    WatchPrototype.unity   Editable octagonal watch + combat example
    SampleScene.unity      Existing template scene, also used by test teardown
  Editor/                  Prefab authoring and Windows build tools
  Tests/
    EditMode/              Pure state tests
    PlayMode/              Watch and combat integration tests
Docs/                      Team handoff and setup instructions
Builds/WatchPrototype/     Generated Windows player; ignored by Git
```

## Teammate's original prototype

The locally available `origin/nandy` revision `8943e7e` contains `Assets/_Scenes/PrototypeScene.unity`. It remains in that branch. The earlier integration imported the player, gameplay scripts and their required visual dependencies into the working branch, but did not import that scene or its skybox collection. The original prototype was not deleted or moved into WatchPrototype.

WatchPrototype is an additional example that reuses the teammate's player. Integrating the new modules into PrototypeScene is a separate level-integration task. This folder reorganization does not import or replace the teammate's scene.

## Assembly and reference preservation

Existing assets were moved with their `.meta` files, preserving script, prefab, material, sprite and scene GUIDs. Existing C# namespaces and assembly names remain the same.

- `Scripts/SpaceRunner.Runtime.asmdef` owns the new Unity gameplay and UI modules.
- `Scripts/Player/SpaceRunner.Player.asmdef` owns the teammate's player code. Assembly references in `Scripts/Input` and `Scripts/UI/Gravity` keep those existing scripts in the same player assembly.
- `Scripts/Core/SpaceRunner.Core.asmdef` keeps state logic independent of Unity.
- Editor and test assemblies have their own folders.

The teammate's movement implementation is unchanged by this reorganization. The previously added pause/reset/binding hooks remain documented in [NandyIntegration.md](NandyIntegration.md).

Use [WatchPrototype.md](WatchPrototype.md) for watch/animation setup and [CombatPrototype.md](CombatPrototype.md) for health/enemy setup. Prefab creation tools and the build command use these paths; they do not recreate the previous top-level feature folder.

## Reorganization validation

All 91 existing asset-file GUIDs were preserved. Existing scripts retain their assembly membership. Unity 6000.6.8f1 passed all 15 EditMode and 13 PlayMode tests after the move. Both prefab authoring commands resolved the new paths without changing existing prefab contents; all prefab and build-scene script references resolved. The Windows prototype was rebuilt successfully from the new structure.

The prototype now contains saved level geometry, a scene player and a visible enemy layout. See [SceneEditing.md](SceneEditing.md) to change their placement.
