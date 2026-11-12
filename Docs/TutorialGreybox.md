# Tutorial greybox — Draft 04

Open `Assets/_Scenes/TutorialGreybox.unity`, enter Play, then choose **START RUN**. The scene uses the existing Player, watch, health and tower systems. Its geometry is saved as ordinary scene objects; no level generator runs when you press Play.

## Layout

The route follows world **+Z**, from Z = -12 to 508 (520 m end to end). The route diagram's folded arrows describe lesson order, not turns in the actual scene. Most practice spaces are 24–32 m wide; individual jump platforms are 16–24 m wide. Recovery decks extend beyond the playable route.

Expand **TutorialGeometry**:

| Group | World Z | Layout |
| --- | --- | --- |
| `01_ArrivalHall` | -12–28 | Safe movement, R watch and Shift dash practice; wide floor and long run-up. |
| `02_JumpCourtyard` | 28–128 | Four platforms, with 6 / 8 / 10 m gaps and broad landings. |
| `03_WallRunGallery` | 128–220 | Two separate 24 m wall sections; an 18 m gap on each side of the middle rest platform. First left wall, then right wall. |
| `04_GravityCourt` | 220–274 | A 22 m floor opening with a finite side wall for gravity walking. Transfer back to the original floor. |
| `05_GravityChamber` | 274–338 | Entry wall, ceiling underside at Y = 16, and a separate upper landing at Y = 8. |
| `06_CrossfireYard` | 338–436 | Open 32 m wide courtyard at Y = 8, three staggered towers and a sheltered middle pocket. |
| `07_ExitHall` | 436–508 | A 6 m jump followed by an 18 m wall-run gap, then a wide final landing. |

The jump, wall-run and gravity sections have lower recovery decks 8 m below their entry floor, with ramps back to their starts. These are physical paths, not automatic respawns. Outer edges are open: falling beyond the recovery decks still requires **R → END RUN → START RUN**. The exit frame is a visual destination; it does not yet load another level or end the run.

## Editing

- Each numbered group has its origin at its entrance. Move the group to relocate the entire section; its individual floors, walls, roof sections, ramps and markings remain editable.
- Floor and gravity-walking surfaces use **Ground (6)**. The three dedicated wall-run panels use **Wall (7)**, matching the existing player's wall-run mask. Keep these layers when replacing meshes.
- **Blue stripes identify wall-run surfaces only**: the left and right practice walls in section 03, and the final wall in section 07. Floor, recovery, gravity-wall and ceiling route stripes have been removed; the exit frame is neutral grey. Wall-run stripes use **Ignore Raycast (2)** and have no colliders.
- English signs are ordinary **TextMesh** components under **Editable English text**. The user's saved sign edits are preserved.
- Materials are in `Assets/Graphics/Materials/Tutorial`. Replace them or the primitive MeshFilters with art assets while preserving intended collider shapes. Do not scale the player to resize the level.
- Roofs and enclosure walls in the wall-run and exit galleries are separate sections. Avoid joining them into a continuous outside route that bypasses the lessons.
- **Checkpoint**, **action check** and **level transition** objects explicitly marked **placement only** reserve positions. They have no checkpoint, trigger, ability-lock or exit-unlock behaviour yet. All existing abilities remain available.

## Towers

Edit prefab instances under **06_CrossfireYard / Enemy Layout - Move or Duplicate These** before Play. CombatSystem uses these as templates and creates fresh gameplay instances for every run.

| Instance | World position | Attack range |
| --- | --- | --- |
| Tower 01 — entry | (12, 9, 362) | 23.5 m |
| Tower 02 — crossfire | (-12, 9, 386) | 23.5 m |
| Tower 03 — exit | (12, 9, 410) | 23.5 m |

Ranges are scene-instance overrides. The shared tower prefab, bullet speed, shot interval and departure lifetime are unchanged. First and third tower centres are 48 m apart, greater than their combined attack radii of 47 m, so their attack spheres cannot overlap. Adjacent towers can engage together. Previously fired bullets can remain in flight after leaving a range; this is not a guarantee that only two towers' bullets are present.

The centre shelter near (0, 8, 393) uses separate cover pieces for towers 02 and 03. The towers have open firing lanes elsewhere. Cover tops remain exposed alternative routes. This lesson has no melee enemies; its CombatSystem therefore has no navigation-region dependency.

## Recovery ramps

The five return ramps rise 8 m over a 24 m horizontal distance. Their upper surface meets the landing flush at section-local Z = 8; their lower end meets the return walkway at Z = 32. Each upper landing has a 6 m turning area. Sections 03, 05 and 07 have a 6 m wide, 4.5 m high opening through the side wall, so the return path reaches the entry floor.

Section 02's ramp is at local X = -12. Sections 03 and 07 use X = -20; sections 04 and 05 use X = 20. The latter ramps sit outside the enclosure walls. Keep the upper landing from extending downhill across the slope: that creates a vertical lip that the Rigidbody controller cannot step over.

## Playtest route

1. Start a run; check WASD, Space, Shift, Tab and R using the current controls.
2. Cross all three courtyard gaps while steering in the air. Miss a jump and use the return ramp.
3. Jump beside the left wall while moving forward; jump away onto the rest platform. Repeat on the right wall.
4. Hold Tab and select gravity toward the practice wall. Traverse the missing floor, then select gravity toward the original floor.
5. Transfer from entry wall to ceiling, cross above the opening, move above the upper landing, then select gravity toward that landing. Gravity choices are relative to the current player orientation; **Down** is not a universal world-gravity reset.
6. Observe the first tower, enter adjacent-tower crossfire, use the middle shelter, then cross the exit lane. Check that slow bullets create readable pressure without trapping the player.
7. Finish the final movement combination. Use END RUN / START RUN to confirm reset behaviour.

## Validation

Validated in an isolated project copy with Unity **6000.6.8f1**, without screen control:

- Native scene import and reopen passed; all material and script references resolved.
- Checked nine openings and their recovery floors, dedicated wall-run layers, ceiling collider, four open firing lanes, two shelter sightlines and absence of a three-tower range overlap.
- **6/6 PlayMode checks passed**: startup/restart with three towers, R watch pause/resume, all three courtyard jumps, both wall runs and landings, gravity-wall crossing, and ceiling travel onto the upper landing.
- Movement checks used the existing controller and synthetic keyboard input. Gravity traversal checks called the controller's gravity-alignment API directly; they do not verify the full Tab-selection UI flow.

Logs and the previous tutorial scene backup are under the local, Git-ignored `Logs/TutorialDraft04` directory. Temporary authoring and validation scripts are kept outside the project's Assets; no new gameplay script is required by this map.

The subsequent ramp correction passed **5/5 additional PlayMode checks**, one per recovery area: walk from the lower deck onto the return lane, ascend without jump input, settle on the upper landing, and cross back onto the entry floor. Capsule-clearance checks also passed along all five slopes and connecting bridges. All 16 user-saved text components and their 40 text/ancestor transforms were preserved. The saved pre-fix scene and results are in `Logs/TutorialRampFix`.

Automated validation and visual playtesting are separate. Camera framing, sign readability, complete-route navigation and combat difficulty still need a human playtest before this becomes the final tutorial.
