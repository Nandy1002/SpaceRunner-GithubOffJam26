# Watch UI ¡ª teammate handoff

## Run the example

Open `Assets/_Scenes/WatchPrototype.unity` using the project's Unity **6000.6.8f1**, press Play, then **START RUN**.

- **R**: open / close the watch. **Esc**: cancel a dialog, return from a page, then close.
- **Hold Tab**: the existing gravity selector; release to select a direction. It is unavailable while the watch is open.
- **WASD**: movement; **Space**: jump; **Left Shift**: dash; **C**: slide. Sustained forward input activates the team's sprint behaviour.
- **RESUME** closes the watch. **END RUN** confirms before ending the timer and showing the main menu. **QUIT GAME** confirms before closing the app (or stopping Play Mode).
- Settings apply immediately and save when leaving the settings page or closing the watch.

The example uses the player prefab and locomotion from `nandy`, not a second movement controller. The test level is a saved octagonal tunnel with editable scene geometry and enemy prefab placement; see [SceneEditing.md](SceneEditing.md).

The scene also includes modular player health, a firing tower and a pursuing melee enemy. See [CombatPrototype.md](CombatPrototype.md) for parameters, prefab setup and artist handoff.

## Prefabs for designers and artists

All assets below live in `Assets/Prefabs` and can be opened in Prefab Mode without entering Play Mode.

| Prefab | Purpose / what to edit |
| --- | --- |
| `Watch/WatchSystem.prefab` | Scene integration. Assign the existing `FirstPersonController` to **Player**. Set **Watch Binding** (`<Keyboard>/r`) and **Pause Gameplay While Watch Open**. |
| `Watch/WatchRig.prefab` | Connects the arm, physical face, watch camera, presentation and menu. Adjust close-up angles / coverage / duration on **Watch Face Zoom**. |
| `UI/WatchMenu.prefab` | Actual UI layout. Backgrounds, dial outline and buttons are ordinary **Image** components; all labels and the timer are ordinary **Text** components. |
| `Watch/PlaceholderArm.prefab` | Replace the placeholder meshes with the artist's model. Preserve / rebind the watch face socket and presentation references. |
| `UI/ScreenUI.prefab` | Main menu, confirmation and gameplay HUD. All UI is English and editable in the prefab. |
| `UI/GravityMenu.prefab` | A basic layout wired to the team's existing `GravityChangerUI` and `GravityManager`; optional when the level already has its own gravity UI. |

### Replacing artwork

1. Import the supplied image as **Sprite (2D and UI)**.
2. Open `UI/WatchMenu.prefab`; select **Background**, **Dial Outline**, or a button.
3. Assign the sprite to the existing Image component's **Source Image**. Set tint to white for full-colour art; use **Sliced** if the supplied sprite has borders.
4. Edit positions / sizes using RectTransform. Edit label appearance through Text's Font, Font Size and Color. The prototype uses the built-in LegacyRuntime font; imported standard Unity Font assets can replace it.
5. Keep Button / Text components and their view bindings attached. Sprites, fonts, materials and layout can change without rewriting timer or pause code.
6. Settings and confirmation objects are saved inactive. Temporarily activate them in Prefab Mode to edit them, then restore their inactive state before saving.

`Assets/Graphics/Sprites/DialOutline.png` is a plain static placeholder sprite. There is no runtime ring mesh, seven-segment mesh, or UI hierarchy generator. Playing the game instantiates the saved prefabs; it does not overwrite artwork or layout.

`Space Runner > Create Missing Watch Prefabs` is a one-time editor authoring utility. It creates missing assets only; existing prefab and placeholder files are preserved. It is not needed for normal use or builds.

## Scene integration

1. Keep the level's existing player and its camera. Add one `Watch/WatchSystem.prefab` to the level.
2. Assign that player's **FirstPersonController** to **Player**. The system initializes at Start. A composition script can instead call `WatchSystem.Initialize(player)` explicitly.
3. Keep **Watch Rig Prefab** assigned. Set **Gravity Menu Prefab** to None if the level already contains a gravity selector.
4. Keep **Screen UI Prefab** for the provided main menu, or leave it unassigned when the game already provides its own menu. An external flow should call `WatchSystem.Session.StartRun()` when a run starts.
5. Call `Session.ReturnToMenu()` to end the run and reset the watch. This prototype's main menu is in the same scene; scene loading belongs to the game's flow system.
6. The system creates an EventSystem only if the scene does not already contain one. Do not add a second player input manager or movement controller.

The standalone InputManager defaults retain the teammate's behaviour. `NandyPlayerBridge` opts into input blocking and shared cursor ownership only while the watch system is present. It also resets the player and active abilities for a fresh run.

## Pause option

| Setting | World / timer | Movement / look | Pointer |
| --- | --- | --- | --- |
| On (default) | Paused | Blocked, including new jump / dash inputs | Visible and usable |
| Off | Continue | Continue | Visible and usable; mouse motion also turns the view |

The switch can be changed live through `WatchController.PauseGameplayWhileOpen`. Other modules can acquire `Session.Pause.Acquire()` and dispose their own handle later; closing the watch does not release another module's pause.

World pause starts immediately on opening and ends after the arm has lowered. The arm and close-up camera use unscaled time. R / Esc stay active during pause. Tab is reserved for gravity and cannot open both interfaces at once.

## Camera and animation

The UI Canvas stays attached to **FaceSocket** throughout the transition. The watch camera dollies from the gameplay viewpoint towards that surface, finishing with a slight tilt. It does not detach the UI and float it in front of a second watch. Arm geometry is hidden at the end of the close-up; the physical-face UI continues rendering.

The supplied placeholder raises in 0.20 s and focuses in 0.18 s. These are editable. The separate watch camera leaves the team's gameplay-camera movement / gravity alignment code intact. The rig follows the gameplay camera without inheriting the player's non-uniform scale.

For the animator:

1. Replace the arm meshes inside the rig (or create a prefab variant). Place **FaceSocket** on the surface; its local **-Z** points out of the face. Reparent the existing watch Canvas to this socket.
2. Add `AnimatorWatchPresentation` to the Animator object. Assign it to the rig's **Presentation** reference.
3. Use the Bool parameter **WatchOpen** and the initial state **WatchClosed** (names are configurable).
4. At the end of the raise animation, send the **Raised** animation event; at the end of the lower animation, send **Lowered**. Do not repeatedly send them from a hold loop. Support reversal from intermediate poses.
5. The adapter sets Animator update mode to **UnscaledTime**. Assign the final arm renderers to the focus component's **Arm Visuals** list so they are hidden at close range.
6. Keep first-person arm / watch UI objects on layer **30**; the rig camera renders only that layer. Reserve this layer in the team's project settings.

## Adding a feature page

Create a page prefab with its own content / behaviour. Add an instance beneath the menu's **Pages** transform and add a Button in the desired position. Append a **Pages** entry on WatchMenuView with a unique ID, English title, content root and open button. The menu handles showing, hiding and Back navigation. The new feature owns its own data and logic.

For runtime modules, instantiate the module's prefab into `WatchMenuView.PageHost` and call `RegisterPage(new WatchPageEntry { id, title, content, openButton })`. Registration does not create or redraw graphics.

## Validation and build

All **7 watch PlayMode tests passed**: R toggling during pause, pointer raycasts on the tilted physical face, hidden/restored arm geometry, live mode, pause ownership, run confirmation/reset, Tab gravity-menu isolation, and use after an upside-down gravity selection. The Windows test player also passed real pointer clicks, slider interaction, confirmation, camera-dolly and R/Tab checks; its 1280¡Á800 frames were inspected. Core EditMode tests cover pause and watch-state behaviour.

Windows build: **Space Runner > Build Windows Prototype**. Default output: `Builds/WatchPrototype/SpaceRunner.exe`.

The combat integration was subsequently compiled and tested in an isolated copy using the project's exact Unity **6000.6.8f1** version. All 7 watch PlayMode regressions still pass alongside the combat tests. See CombatPrototype.md for the combined validation.
