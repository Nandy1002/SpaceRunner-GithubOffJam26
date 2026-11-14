# Integration with the nandy gameplay branch

Base inspected/imported: `8943e7e25cde5b238273624c528cdc7775db47a8` (2026-11-09, `baseline gravity switcher done`).

This change imports the existing gameplay scripts, their metadata, the player prefab, input action asset, layer configuration, and the player's material / particle dependencies into the current working branch. It does **not** merge the entire nandy branch or copy its skybox collection and prototype level.

The following small hooks were added to the imported gameplay code:

- **InputManager**: opt-in gameplay-input and gravity-selection gates, external cursor ownership, and a one-frame look suppression around cursor changes. Defaults preserve standalone behaviour.
- **FirstPersonController**: respects those opt-in gates and adds `ResetForRun` so restarting a run clears motion / camera / slide state.
- **GravityManager**: accepts a player binding for the optional selector prefab. Its gravity-direction calculation is unchanged.
- **SpaceRunner.Player.asmdef**: places those scripts in a named assembly so the independent watch runtime can reference them. Existing MonoScript GUIDs are preserved.

DashAbility, WallRunAbility, PlayerState, IAbility, PlayerSpeedVFX, GravityChangerUI and the generated input wrapper remain the teammate's implementation. InputManager still owns locomotion input. The watch's small menu-only input reader listens for R and Esc.

The existing input asset contains an unused **Time** action bound to R. No gameplay script in this inspected revision consumes it. R currently controls the watch as requested. If the future time ability is implemented, coordinate its binding with WatchSystem's configurable Watch Binding rather than leaving two features on R.

For merging back into nandy later, retain the original script and prefab GUIDs and reconcile the three hook-bearing scripts against any newer teammate changes. Do not add the old demo FirstPersonMotor; the watch demo now uses the existing FirstPersonController and Player prefab.

Read `WatchPrototype.md` for artist, animator and level integration steps.

After the folder reorganization, `SpaceRunner.Player.asmdef` lives in `Assets/Scripts/Player`. Assembly reference files keep `Assets/Scripts/Input` and `Assets/Scripts/UI/Gravity` in that same assembly. The gravity UI script moved into the latter folder with its original metadata. See `ProjectStructure.md` for all current locations and the status of the teammate's original PrototypeScene.
