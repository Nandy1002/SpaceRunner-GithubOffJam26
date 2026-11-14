# Tutorial environment art

The tutorial now uses a clean orbital-station style across **all seven sections**: light grey powder-coated interiors, graphite bulkheads, recessed white lights, inspection panels, glazed observation bays and distant station structures. The approved arrival-hall sample is retained. Blue remains exclusive to the three existing wall-run markers.

The artwork is saved in the Unity scene and normal assets. No runtime level generator is required.

## Full-route scene editing

Open `Assets/_Scenes/TutorialGreybox.unity`, then expand **TutorialGeometry / [numbered section] / StationArt**. All sections now have normal saved prefab instances that can be selected, moved, scaled and replaced using Unity's usual tools. Move the numbered section parent when moving a complete lesson so its art, collision and signs stay together.

| Section | Art treatment |
| --- | --- |
| 01 Arrival hall | Approved detailed sample, curved hull bays, bulkheads, observation window and rear hatch. |
| 02 Jump courtyard | Exterior decks, layered platform skirts, thin caution inlays and service hatches. Original 6 / 8 / 10 m gaps retained. |
| 03 Wall-run gallery | Separate station corridor bays, neutral wall framing, rest-area glazing and recessed lights. The two running walls remain uninterrupted. |
| 04 Gravity court | Finite station hull wall with neutral direction marks and plain walking face. |
| 05 Gravity chamber | Detailed ceiling and wall faces, recessed lights and neutral ceiling guidance; the 16 m ceiling and 8 m upper landing are unchanged. |
| 06 Crossfire yard | Open test deck, dressed equipment-cover faces and tower pedestal collars, with original sightlines. |
| 07 Exit hall | Matching corridor bays, observation glazing and a decorative sealed exit hatch behind the existing destination frame. |

All five recovery decks, return walkways and ramps use the same deck treatment. Distant habitat barrels, docking hubs, radiator wings and connecting trusses form the visual background. They are scenery outside the playable route, not additional traversable platforms.

## Reusable station kit

The kit now contains **25 prefabs** in `Assets/Prefabs/Environment/Station`. Their reusable mesh assets are in `Assets/Graphics/Meshes/Environment/Station` and shared materials in `Assets/Graphics/Materials/Environment/Station`. The initial 13 detailed modules remain available:

| Module | Model detail |
| --- | --- |
| Hull Bay 8.5m | Continuous wall face, shaped skirting and a 12-segment curved ceiling transition. |
| Plain Hull Surface 8m | Scalable continuous surfaces surrounding the observation opening. |
| Open Bulkhead 28m | Thick clipped-corner frame, recessed sealing channel, metallic inner rim and access indicators. The clear passage remains over 25 m wide. |
| Observation Frame 20m | Layered frame, gasket, inner trim and slim mullions. |
| Observation Glazing 20m | Separate transparent pane with restrained reflections. |
| Recessed Strip 4m | Chamfered housing, inset recess, luminous diffuser and end-cap fasteners. |
| Flush Service Cabinet 2m | Beveled doors, separate handles, six vent slats, indicator and fasteners. |
| Deck Service Hatch 2m | Clipped corners, inset cover and lifting recesses. |
| Deck Lane Inlay | Thin painted lane edge, deliberately not blue. |
| Sealed Service Hatch 6m | Rear-wall hatch with layered frame, split door leaves and status indicator; decorative, not interactive. |
| Exterior Habitat 18m | Faceted pressure hull, shaped ends, structural bands and framed ports. |
| Exterior Truss 6m | Open lattice with diagonal braces. |
| Exterior Catwalk 6m | Deck, posts and two levels of rail. Exterior scenery, not part of the tutorial route. |

The full-route pass adds 12 modules:

| Module | Purpose |
| --- | --- |
| Deck Skirt 8m | Layered panel, recessed trim and end fittings on platform edges and ramp sides. |
| Flush Hull Perimeter 8x12m | Shaped edge frame around a broad, continuous wall face. |
| Flush Hull Rib 12m | Thin inset structural division on ordinary enclosure walls. |
| Flush Ceiling Rib 8m | Shallow ceiling detail that preserves the gravity-walking surface. |
| Deck Caution Inlay 6m | Muted ochre landing-edge markings, not emissive blue. |
| Neutral Direction Inlay | Flat chevron for gravity and recovery guidance. |
| Tower Pedestal Collar 4m | Rim and four small status indicators around an existing pedestal. |
| Equipment Cover Face 6m | Beveled panels, recessed lower vent and trim fitted to existing cover. |
| Glazed Hull Bay 12m | Separate opaque hull/frame and transparent pane; original wall collider retained. |
| Exterior Radiator Wing 12m | Framed, layered radiator fins. |
| Exterior Docking Hub 12m | Faceted hull, pressure bands and docking collar. |
| Bulkhead End Facing 1m | Scalable dark casing for exposed wall and roof ends. |

The 10 station materials are shared and can be edited independently. `Station Powder Coat` reuses the three original paint texture maps, with a new texture scale appropriate to the authored mesh UVs. The old material assets remain available. The left wall has continuous backing with no deliberate 3 cm panel gaps; broad structural ribs now define its bays.

The first station sample retained the original 85 colliders and added eight matching **Ground-layer static MeshColliders** for its four bulkheads and four curved hull bays. The full-route pass preserves all **93 colliders** unchanged and adds no further collision geometry. All 16 teaching labels, three blue wall-run markers and three tower components retain their transforms, layers and settings. Window replacements retain the original wall box collider as a glazing boundary. Decorative modules elsewhere remain shallow and aligned with existing collision surfaces; exterior scenery has no collision. Gameplay scripts and the player prefab were not changed.

The arrival sample uses four local fill lights, one with soft shadows. The full-route pass adds nine non-shadowing interior fill lights, for **13 local fill lights** total. It reuses the existing station sky and ambient settings. `Station Star Sky` uses the replaceable cubemap in `Assets/Graphics/Textures/Station`; this is a placeholder for future 2D art. Changing this sky affects the entire tutorial scene.

## Material trial A — retained resources

Three original, seamless image textures are stored in `Assets/Graphics/Textures/PowderCoat`: BaseColor, tangent-space Normal, and packed MetallicSmoothness. They were authored as project assets to approximate fine powder coating; they are not downloaded photographs of the reference product. The opaque paint coating uses metallic 0 and nominal smoothness 0.27. Fine grain is intended to be subtle and to disappear at normal viewing distances. The sample preserves all existing collision shapes and text positions.

The saved scene before this trial and offscreen previews are in `Logs/PowderCoatSample`. The close-up uses a temporary grazing light to inspect the grain; that inspection light is not saved in the level.

## Scene editing

Every numbered group under **TutorialGeometry** now contains **StationArt**. The old EnvironmentArt groups have been replaced; their reusable assets remain on disk for reference.

- Move or replace individual art instances normally. The modules are linked prefab instances.
- Edit a shared prefab or material to update all uses, or make an instance override for a specific location.
- Existing gameplay geometry retains its collision shapes. The station bulkheads and curved hull bays additionally own matching mesh colliders.
- Empty sign backing renderers left behind after the user's text moves are disabled. Their objects remain available; enable them again after aligning them with their labels if needed.
- Earlier EnvironmentArt modules have no colliders. Their panels sit close to the existing surfaces; keep replacement art aligned with those surfaces.
- Disabling a StationArt group hides that section's added visuals and lights. Original collision geometry remains present. Where a wall has become a window, its original renderer is intentionally disabled: to revert that wall, restore its renderer as well as disabling the window art. Disabling the arrival-hall StationArt group also disables its eight additional structural colliders.
- When moving an entire lesson, move its numbered section parent so art and gameplay geometry stay together.

## Earlier resources retained

| Folder | Contents |
| --- | --- |
| `Assets/Prefabs/Environment` | Deck Panel 4m, Wall Panel 4m, Ceiling Panel 4m, Deck Edge 4m and Flush Light 2m. |
| `Assets/Graphics/Meshes/Environment` | The shared mesh assets used by those five modules. |
| `Assets/Graphics/Materials/Environment` | Structural Graphite, Deck Alloy, Recess and Fastener, Warm Ceramic Panel, Safety Ochre and Warm White Light. |

Panel prefabs face local +Z. Standard panels are 4 m square; the edge panel is 4 m wide and 1 m high. Scene instances scale in their face plane to fit each surface. Each module uses a combined mesh to keep its small construction details together; the MeshFilter can be replaced with another mesh asset. Materials remain separate and editable.

The earlier first pass originally used 3,570 surface-module instances plus ceiling fixtures across the 520 m route. After replacing the arrival sample, the full-route pass replaced another 3,230 old modules with 1,086 linked station-art instances. The current TutorialGeometry hierarchy has 1,253 mesh instances including underlying gameplay geometry and the arrival hall. Shared meshes and materials are reused; these counts are not a measured frame-rate result.

## Validation and further work

The scene was authored and checked in an isolated Unity project without desktop/screen automation. The arrival sample passed a 38 m central capsule sweep, 38 floor probes and its glazing-boundary check. The full-route pass compared all 93 collider signatures, 16 label components, three wall-run markers and three tower components before/after; the approved arrival sample was also preserved. Reopening the scene passed mesh/material/script-reference checks, nine gaps and recovery-deck probes, all three wall-run surfaces, the ceiling surface, four open turret firing lanes and two sheltered sightlines.

The existing tutorial PlayMode suites passed **11/11** on the full station scene: all three jumps, both practice wall runs, gravity-wall travel, ceiling travel to the upper landing, start/restart with three towers, R watch pause/resume, and all five return ramps. Movement uses the existing controller and synthetic keyboard input. Gravity tests call the alignment API directly rather than exercising the complete Tab-selection UI. The validation copy's scene list included TutorialGreybox for these tests; the main project's build settings were left unchanged.

Previews in `Logs/StationArtPass` and `Logs/StationFullPass` are actual offscreen Unity renders. **Teaching labels and the player body are hidden only during these preview captures**, so the models are visible; their visibility in the saved scene is unchanged.

The complete tutorial now has the station template applied. The 2D artist can supply wall graphics, hatch numbers and a final space background next. No door-opening mechanics, vacuum simulation, tutorial gates or functional level transition were added; the final hatch is a visual destination. Target-device performance and a human visual/navigation playtest remain separate from the geometry and automated movement checks.

The pre-full-route scene backup, authoring audit, movement-test results and previews are stored under `Logs/StationFullPass` (Git-ignored). Earlier backups remain under `Logs/StationArtPass`, `Logs/TutorialArtPass` and `Logs/PowderCoatSample`.
