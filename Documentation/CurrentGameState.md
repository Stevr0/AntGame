# Ant Colony Current Game State

## Current milestone

**Milestone 1 — Player Ant Movement Prototype**

**Status:** In progress. The user has confirmed floor movement, vertical wall climbing, and resolution of the previously reported corner-transition deadlock in Unity Play Mode. Ceiling/corner route acceptance is still incomplete. Bounded Auto Step-Over is now implemented for one-tile openings and awaits its first Play Mode acceptance pass.

Milestone 1 is explicitly a prototype and regression baseline, not the final player control design. Milestones 2.1B through 2.2B are complete and verified alongside that prototype; Milestone 2.2B passed 155/155 automated tests and all 9 manual clearance checks. Milestone 2.2C Background movement capability and gravity eligibility is implemented and automated, but still needs its separate graphics-enabled manual Scene-view acceptance. A bounded integrated sample world has now been implemented as a development checkpoint before 2.2D; its Play Mode acceptance is still pending. Autonomous navigation, Surface Preparation gameplay, skills, colony management and individual task assignment have not been implemented.

## Repository and architecture

- The Unity 2D URP project uses Unity `6000.3.8f1` and the new Input System.
- `Assets/AntColony/Scenes/AntMovementPrototype.unity` is the first build scene.
- `Assets/AntColony/Scenes/AntGameDevelopment.unity` is the integrated development scene. The movement and navigation prototype scenes remain available temporarily as regression fixtures.
- The prototype controls one ordinary player ant. Becoming a queen is later progression.
- Input and movement retain the established `PlayerMovementInput -> AntSurfaceMotor -> Rigidbody2D` flow.
- `AntSurfaceMotor.SetMovementIntent(Vector2)` remains a useful prototype boundary while the future movement model is developed and validated.
- The current authored collider course is not evidence of the approved Background Tile/Solid Tile data model.
- The integrated scene has a small authored `World Bootstrap` coordinator, not a global manager or second world-data implementation. The world-data authority remains an ordinary, scene-independent C# object; AI navigation, excavation gameplay, multiplayer, and multi-ant systems remain absent.

### Milestone 2.1B world-data foundation

- `ChunkedWorldData` explicitly owns resident chunks in a sparse dictionary. Creating or reading world data does not create GameObjects, components, colliders, or neighbouring chunks.
- Each `WorldChunk` is 32×32 cells and immediately fills all 1,024 Background Tile entries. A parallel compact array stores zero or one optional Solid Tile ID per cell.
- Central coordinate utilities use floor division for negative positions, keep local coordinates in the 0–31 range, and preserve the prototype convention that integer coordinates are tile centres in world space.
- Missing chunks return `ChunkUnavailable`; they never masquerade as exposed cells. Duplicate creation and unknown material IDs are rejected without overwriting data.
- `Background_Default` (`background.default`) and `Solid_Soil` (`solid.soil`) are initial authored ScriptableObject definitions. Milestone 2.1E assigns each a simple development-only visual Tile while retaining the material definition as the source of that reference.
- The 2.1B foundation stores a world seed without implicit generation. The later integrated checkpoint now consumes that seed through one explicit bounded generator; unloading, streaming and persistence still do not exist, and ordinary queries still never generate terrain.
- The existing `DirtBlock` objects have not been imported. The prototype scene, prefab, movement code, Rigidbody2D, colliders, gravity, and input remain unchanged.
- The focused Milestone 2.1B Unity Edit Mode suite passed 25/25, and the user confirmed that existing ant movement still works in Play Mode. Milestone 2.1B is complete.

### Milestone 2.1C navigation classification

- `WorldNavigationClassifier` is a plain C# service that reads `ChunkedWorldData` directly. It does not use scene colliders, physics queries, Tilemaps, movement components, or a scene host.
- Occupancy results distinguish `Unavailable`, `Covered`, and `Exposed`. Only a Solid Tile whose registered material has `BlocksMovement` enabled makes its cell Covered.
- Exposed cells separately report `Supported`, `Unsupported`, or `Unknown` support. This does not claim Traversable or Reachable status because body clearance, obstacles, movement-edge checks, and connectivity are not implemented.
- Only north, south, east, and west neighbours can support a cell. The neighbouring Solid Material must have `SupportsNavigation` enabled; Background Tiles and diagonal solids never qualify.
- Classification crosses positive and negative chunk boundaries. Missing target chunks are Unavailable. A missing neighbour produces Unknown only when no resident cardinal neighbour already provides confirmed support.
- Results are uncached and therefore reflect successful Solid Tile mutations immediately. Change notifications are deferred until a later cached visualisation or incremental navigation consumer genuinely requires them.
- The user-confirmed Milestone 2.1C acceptance suite passed 50/50 Edit Mode tests. The subsequent full Milestone 2.1D run also retained all classifier coverage, and Milestone 2.1C is complete.

### Milestone 2.1D navigation visualisation

- `Assets/AntColony/Scenes/AntNavigationPrototype.unity` is a separate developer-only scene. It does not alter `AntMovementPrototype.unity` or the existing movement, Rigidbody2D, input, and collider prototype.
- `NavigationTestWorldHost` authors a deterministic eight-chunk fixture through `ChunkedWorldData` APIs. It includes open floor, a wall and overhang, a narrow tunnel, a large unsupported chamber centre, negative coordinates, chunk boundaries, diagonal-only geometry, and an absent neighbouring chunk.
- One bounded batched mesh visualises 3,564 queried cells without per-cell GameObjects or colliders: Covered is brown, Exposed and Supported is green, Unsupported is red, Unknown is amber, and Unavailable is grey.
- The renderer reads the real `WorldNavigationClassifier`. Optional chunk-boundary lines, overlay visibility, Scene-view cell selection, world/chunk/local inspection, and an explicit Solid Tile mutation-and-refresh control are developer diagnostics only.
- Supported still means local cardinal terrain support only. The display does not claim body clearance, obstacle clearance, connectivity, reachability, or pathfinding.
- The complete automated suite passed 61/61 and the user confirmed all 11 graphics-enabled manual visual checks. Milestone 2.1D is complete.

### Milestone 2.1E world Tilemap integration

- `AntNavigationPrototype.unity` now contains one explicitly half-cell-offset `World Grid`, a `Background Tilemap`, and a `Foreground Solid Tilemap`. Cell `(0,0)` is therefore centred at world `(0,0)` while signed Tilemap indices remain identical to authoritative world-cell coordinates.
- `WorldTilemapRenderer` is a one-way projection from `ChunkedWorldData` through the material definitions to the two Tilemaps. It never reads Tilemap contents as simulation state and exposes explicit cell, chunk, and bounded selected-region refresh methods.
- Resident cells receive their registered Background visual. Optional Solid visuals are written independently above them, including for non-blocking Solid Materials; removing a Solid clears only the foreground presentation.
- Missing chunks are cleared rather than generated. Unknown IDs and missing visual references return precise diagnostics, clear stale presentation, and leave world data and navigation classification unchanged.
- The initial material definitions reference two colour-tinted development Tiles that reuse the existing one-pixel placeholder soil Sprite. Both Tile assets explicitly disable collider generation.
- Background, foreground, and navigation-overlay visibility remain independent. The overlay still reads only `WorldNavigationClassifier` and renders above both Tilemaps.
- Milestone 2.1E added no collision components, per-tile GameObjects, pathfinding, excavation, or autonomous movement.
- Unity compilation, scene validation, and all 89/89 Edit Mode tests passed. The user subsequently confirmed all 10 graphics-enabled manual visual checks, so Milestone 2.1E is complete.

### Milestone 2.2A authoritative terrain collision

- `AntNavigationPrototype.unity` contains a third, collision-only `Foreground Collision Tilemap` beneath the same half-cell-offset Grid. The Background and visual foreground Tilemaps remain collider-free.
- `WorldTerrainCollisionRenderer` is a one-way projection from `ChunkedWorldData`: it places an invisible Grid-collider Tile only when the registered Solid Material has `BlocksMovement` enabled. It never treats Tilemap or collider occupancy as authoritative world state.
- Visual foreground occupancy, physical collision and navigation support remain independent. The fixture includes a non-blocking supporting Solid and a blocking non-supporting Solid to demonstrate that `BlocksMovement` and `SupportsNavigation` are not aliases.
- One `TilemapCollider2D` feeds one manually generated `CompositeCollider2D` on a static `Rigidbody2D` using the existing `ClimbableSurface` layer. Adjacent cells merge without per-tile GameObjects or duplicate terrain hits.
- Explicit cell, chunk and bounded-region refreshes update only requested collision occupancy. `SynchronizeCollisionGeometry` processes pending Tilemap collider changes, regenerates composite geometry and synchronizes transforms before development physics queries.
- The custom inspector reports authoritative blocking state and projected collision occupancy for the selected debug cell, refreshes a cell or selected region, coordinates the existing controlled Solid mutation, and can toggle collision-outline Gizmos.
- Unity compilation, the expanded scene/material/collider validator, and all 117/117 Edit Mode tests passed. The user subsequently confirmed all 12 manual Unity collision checks, so Milestone 2.2A is complete.
- This collision projection is development-scene infrastructure only. It does not establish whole-body clearance, connectivity, reachability, pathfinding, autonomous movement, procedural generation or excavation gameplay.

### Milestone 2.2B ant body clearance validation

- `AntClearanceValidator` is a scene-independent, read-only service that checks a complete rotated capsule directly against authoritative movement-blocking Solid cells. It does not read Tilemaps or collider geometry and therefore cannot return a stale result after a successful world-data mutation.
- `AntCollisionProfile` carries local capsule size, offset, long axis and optional clearance margin. The initial `OrdinaryAnt` profile matches the unchanged movement-scene ant: its prefab-local 1.2×0.6 capsule is scaled by the authored 0.8×0.5 instance scale to an effective horizontal 0.96×0.3-world-unit body with zero offset.
- Results distinguish Clear, Blocked by terrain, Unavailable world data and invalid query/profile. Clearance does not inspect foreground support, Background movement permissions, grip, grounding, gravity, movement state, connectivity or reachability.
- Capsule-versus-cell testing uses the exact central segment plus radius against candidate 1×1 axis-aligned cells. Rotation and collider offset are applied mathematically, only the footprint's bounded cell range is queried, exact tangency is permitted within a `0.00001`-unit tolerance, and an optional margin provides conservative separation.
- A valid capsule is Clear when every touched cell is available and no overlapping Solid Material blocks movement. A known blocking overlap takes precedence over missing data elsewhere in the footprint because it already proves the pose invalid; otherwise any missing touched cell returns Unavailable. Physically fitting open Sky and unprepared shafts are therefore Clear.
- `AntNavigationPrototype.unity` includes an `Ant Clearance Inspector` with an editable position, orientation and profile, a colour-coded exact capsule outline, a Scene-view position handle, a result label and explicit query button. It creates no gameplay ant, collider or Rigidbody2D.
- Unity compilation, focused 38/38 clearance tests, the expanded scene validator, the focused 2/2 movement regression and the full 155/155 Edit Mode suite passed. The user subsequently confirmed all 9 graphics-enabled manual clearance checks, so Milestone 2.2B is complete.

### Milestone 2.2C Background movement capability and gravity eligibility

- `BackgroundTileMaterialDefinition` now stores explicit horizontal, upward and downward voluntary movement permissions, intrinsic climbing grip, and whether gravity applies when unsupported.
- Authored development materials define Sky, Unprepared Earth and Unprepared Stone as grounded-horizontal-only, non-gripping and gravity-eligible. Prepared Grippable Background enables horizontal/up/down movement, supplies intrinsic grip and suppresses gravity while gripping.
- `AntMovementCapabilityEvaluator` is a read-only, scene-independent service. It composes the unchanged `AntClearanceValidator`, the centre Background Material and an authoritative ground-contact query into explicit `Grounded`, `Gripping` or `Falling` eligibility plus detailed permissions and failure diagnostics.
- Ground support uses the full rotated capsule's lowest extent and a `0.02`-world-unit contact tolerance against top faces of registered Solid Materials that both block movement and support navigation. Side walls, ceilings, non-blocking solids and Background Tiles do not become floor support.
- Grounded unprepared space permits horizontal walking only. Unsupported unprepared space is Falling with gravity eligibility but no voluntary downward movement. Prepared grip enables its configured voluntary directions and suppresses falling without changing physical clearance.
- The existing `SetBackgroundMaterial` mutation safely demonstrates Unprepared Earth to Prepared replacement without changing foreground occupancy, creating chunks or introducing Surface Preparation gameplay. `WorldTilemapRenderer.RefreshCell` explicitly refreshes its presentation.
- `AntNavigationPrototype.unity` includes one `Ant Movement Capability Inspector` at the unsupported chamber centre. It reports Background ID, clearance, ground, grip, direction permissions, gravity and suggested state; its explicit toggle button replaces the selected Background, refreshes the visual cell and requeries.
- Focused capability tests pass 37/37. The expanded structural scene validator, focused 2/2 legacy movement regression and complete 192/192 Edit Mode suite pass. Graphics-enabled manual Scene-view acceptance remains pending, so Milestone 2.2C is in progress rather than complete.

### Integrated development sample world checkpoint

- `AntGameDevelopment.unity` combines the existing authoritative chunk data, material registries, visual Tilemaps, collision-only Tilemap, clearance validator and movement-capability evaluator in one Play Mode inspection environment. It does not contain the legacy manually controlled ant.
- `SampleWorldGenerator` explicitly creates a bounded 4×4 group of 32×32 chunks from `(-2,-2)` through `(1,1)`, for an initial 128×128-cell resident world. Queries outside that region remain unavailable and do not create chunks.
- Seed `2203` deterministically produces gently varied surface elevation, exposed Sky, Earth and Stone Background regions, Soil and Stone foreground terrain, two chambers, connected passages, one vertical shaft and a prepared grippable Background strip. All authoritative cells are populated before Tilemap or collision projection.
- The scene hierarchy contains `World Bootstrap`, one half-cell-offset `World Grid`, separate Background and foreground visual Tilemaps, one hidden foreground collision Tilemap, `Main Camera`, and `Development Inspection UI`. No per-tile GameObjects or duplicate terrain colliders are introduced.
- WASD or arrow keys pan, the mouse wheel zooms, middle-mouse drag pans and Home returns to the starting view. Clicking outside the panel selects a cell. The panel reports authoritative cell/chunk/material data, blocking state, ordinary-ant clearance, grip, grounded/falling eligibility and voluntary directions.
- `F` or the foreground button toggles the selected Solid Tile and explicitly refreshes both foreground presentation and collision. `P` or the Background button switches exposed underground space between prepared and its depth-appropriate unprepared material, preserving foreground occupancy and refreshing the inspection result.
- The first manual Play Mode pass confirmed world generation and middle-mouse dragging, but found that the material palette rendered too dark and wheel zoom appeared inactive. The visual cause was multiplication of every Tile tint by the old dark-brown placeholder sprite. Integrated-world Tiles now use a neutral white pixel plus the explicit Sky `#719BC1`, Soil `#986342`, Stone `#737E89`, Earth Background `#514439`, Stone Background `#434C56` and Prepared Background `#648B62` palette.
- Wheel zoom previously assumed a roughly 120-unit Windows mouse delta and scaled the raw value directly, making approximately 1-unit device deltas visually negligible. Scroll is now normalised into device-independent steps, changes the target by 4 orthographic units per step, smoothly approaches that target over `0.08` seconds and remains clamped to an orthographic size of 4–64. Home resets both the current and target size to 30; pan and middle-drag remain in the same controller.
- A subsequent manual check showed that authoritative Unprepared-to-Prepared mutation, grip and movement eligibility changed while the visible Background remained brown. A graphics-enabled authored-scene test at the reported cell `(-1,15)` ruled out coordinate mismatch and occlusion. `Tilemap.SetTile` stored the prepared Tile asset but preserved the cell's previous Earth `TileData` colour; neither `RefreshTile` nor the bounded `RefreshAllTiles` diagnostic replaced that stale colour. Single-cell projection now copies a standard Tile's authored colour into the cell, temporarily unlocking only `LockColor` and restoring the effective flags before refreshing that cell. The effective authored flag remains `LockTransform`.
- The development inspection panel now reports the world and Tilemap coordinates, material ID, Tile name, Tile asset colour, effective cell colour, Tilemap colour, sampled Sprite pixel, composed expected colour, asset and cell flags, renderer material/shader, sorting, and active/enabled state. A development-only button isolates the Background visual Tilemap by hiding the foreground visual layer, without changing authoritative data or collision.
- The graphics-backed regression renders the authored camera at `(-1,15)`: Earth is `RGBA(0.318, 0.271, 0.220)`, prepared is `RGBA(0.388, 0.545, 0.384)`, the isolated prepared pixel is identical, and toggling back restores Earth. Five sample-world integration tests, 9/9 focused camera tests, the coordinate refresh test and the rendered-pixel test pass as part of the complete 208/208 Edit Mode suite. Graphics-enabled Play Mode re-acceptance of the corrected palette, zoom and Background visual refresh remains required, so this checkpoint is implemented but not yet manually accepted.
- This checkpoint does not complete Milestone 2.2C, begin 2.2D, add autonomous movement, perform real excavation or Surface Preparation jobs, stream chunks, or save/load the world.

## Approved future design, not implemented beyond the data foundation

- Normal play manages ants through selection, inspection, designations, colony priorities and specific task assignments; it does not directly steer them.
- Every coordinate in a generated chunk contains one permanently present Background Tile whose material may be replaced, plus zero or one foreground Solid Tile; ungenerated chunks remain unavailable.
- Background Materials are the primary authority for voluntary directions, intrinsic grip and gravity behavior. Background existence alone grants no movement permission.
- Excavation removes an eligible foreground Solid, leaves its Background unchanged, updates affected systems and does not automatically grant grip.
- Surface Preparation is an approved future job that consumes resources and time from an accessible nearby work position, then replaces an exposed unprepared Background with a grippable material only on completion. Vertical routes are prepared progressively.
- Physical clearance, Background movement capability and directional navigation connections are separate. Falling through physically clear, non-grippable space may be valid even when upward voluntary movement is not.
- Prepared grip can permit clearance-valid two-dimensional movement without immediate Solid adjacency. Foreground `BlocksMovement` always remains authoritative, and `SupportsNavigation` stays independent.
- Navigation connections may be directional; diagonal corner-cutting through blocking geometry remains invalid.
- Every ant starts every ordinary skill at 0, can perform ordinary tasks without a specialist gate, and improves from successful work on a 0–100 scale. Roles remain preferences and priorities.
- Independently tracked skills above 50 slowly decay after inactivity and stop at exactly 50; skills at or below 50 do not decay through inactivity.
- The proposed carrying system remains future work: one initial carrying slot plus one slot per 20 Hauling skill, capped at five slots, with one item per slot.
- Multiple ants may share a tile and use hybrid separation, local avoidance and bounded recovery rather than universal one-ant-per-tile occupancy.
- Friendly, neutral and hostile colonies share the same terrain navigation model.
- The proposed architecture separates world tile data, terrain/excavation and preparation changes, physical clearance, Background movement capability, directional navigation, colony task decisions, per-ant route following, continuous movement, local avoidance and authoritative collision.
- The design target allows potentially hundreds of simulated ants, but no DOTS/ECS or other substantial dependency is approved before profiling demonstrates a need.

## Implemented and Play Mode confirmed

The user has confirmed the previous movement prototype works in Unity Play Mode:

- A/D and Left/Right horizontal floor movement.
- Stopping and direction reversal.
- Floor attachment.
- W upward climbing and S downward movement on left and right walls.
- Wall attachment without sliding when input is released.
- Surface-aligned floor and wall rotation.

## Newly implemented, pending Play Mode acceptance

### Ceiling traversal

- A fourth non-allocating collider cast detects a nearby climbable ceiling.
- Ceiling attachment uses the same motor, attachment state, gravity handling, and collision system as floors and walls.
- A/D is projected onto the ceiling tangent, and the ant rotates so its feet face the ceiling.
- Releasing input produces zero traversal velocity while a valid ceiling remains attached.

### Directional surface choice

- The current surface remains preferred while stationary or while intent still matches its tangent.
- A connected perpendicular surface is selected only when its movement direction clearly matches world-space input.
- Floor-to-wall selection therefore requires upward intent rather than automatically converting A/D into a climb.
- At a wall-to-ceiling junction, W continues toward the obstruction but does not choose left or right. A or D selects the corresponding valid ceiling direction.
- Ceiling-to-wall and wall-to-floor choices use the same intent projection rather than surface-specific input code.
- Selection thresholds and bias are configurable on `AntSurfaceMotor`.

### Inside and outside corners

- Direct down/left/right/up casts provide candidate surfaces at concave inside corners.
- A short look-ahead from the current contact detects the perpendicular face around a convex outside edge. Zero-distance results are rejected so a ray beginning inside ordinary terrain cannot create a false corner.
- Candidate surface lines must intersect within the configurable connected-corner distance, and separate source/target colliders must physically touch or fall within a small seam tolerance. This prevents attachment across unsupported gaps.
- A successful transition follows a smooth quadratic corner path through `Rigidbody2D.MovePosition` and `MoveRotation`; collision is never disabled and the Transform is not moved directly.
- The complete path is sampled before transition using a slightly inset copy of the ant collider. Every movement step also performs a collider sweep and a pose-overlap check.
- If clearance fails, the transition is rejected and the ant remains on the current surface. Releasing input pauses an active transition; opposite intent can reverse it.
- Position and orientation advance at the slower of configured movement and rotation rates to avoid snapping or uncontrolled acceleration.

### Corner-transition deadlock fix

- Live Play Mode testing confirmed that an ant could enter a corner transition and become unable to move or reverse.
- The transition step previously recorded a failed sweep/overlap and returned while leaving the motor permanently active. A progress-zero reversal was also validated as a collision step before it could exit to the source, and only the source-opposite direction counted as reverse input.
- A blocked first step now cancels immediately to the source. A later blocked forward step is latched instead of retried every physics frame, preserving reversal along the validated path.
- Either the opposite source direction or the opposite destination direction reverses a transition, so escaping does not require an unintuitive key combination at the midpoint.
- If an already-travelled reverse step becomes blocked by changed geometry, the motor cancels onto a currently detected source/target surface or explicitly detaches at its last valid physical pose.
- A rejected transition pair is not retried with unchanged held input. Releasing or changing input clears the suppression.
- Source and destination colliders are ignored only by the redundant movement sweep because they are intentional support contacts; inset pose-overlap validation remains active to prevent penetration.

The user subsequently confirmed that the original deadlock no longer reproduces in Play Mode. Broader ceiling and corner-route acceptance is still separate pending work.

### Auto Step-Over

- Continued tangent input near a support edge starts an explicit controlled crossing only when a same-type, same-normal landing surface is detected across an opening no wider than the ant's configured capability.
- The player prefab defines one prototype tile as one world unit and limits this ant to one tile. Openings wider than that limit and openings without a destination are rejected.
- Floor, ceiling, and vertical-wall openings use the same surface-relative detector; there is no floor-only jump path.
- A strong perpendicular input wins over automatic crossing, allowing later explicit opening entry or junction behavior without competing state changes.
- The ant has no ordinary `SurfaceType` while crossing. Gravity and ordinary attachment movement are replaced by a prevalidated straight `Rigidbody2D.MovePosition` path between real supports; collision is never disabled and no Transform teleport is used.
- The whole inset capsule is checked at 24 poses before entry. A collider cast and pose overlap are repeated for every movement step so a blocking obstacle cannot be bypassed.
- Releasing input pauses progress. Opposite tangent input reverses toward the source. A new block at the first step cancels to the source; a later block preserves the already validated reverse route, with safe surface recovery or explicit detachment if changed geometry blocks both directions.
- Rejected geometry is not queried again every physics tick while the same direction remains held. Input release/change or `NotifyTraversalGeometryChanged()` permits a fresh query.
- Read-only diagnostics expose active state, progress, measured gap start/end and distance, configured maximum distance, and a specific rejection reason. Optional gizmos show the measured opening and controlled path.

### Attachment and debugging

- Attachment states now include `None`, `Floor`, `LeftWall`, `RightWall`, and `Ceiling`, plus explicit active corner-transition and Auto Step-Over states.
- Gravity is disabled only while a real surface or prevalidated connected-corner path is active. Losing support restores the Rigidbody2D's authored gravity without teleportation.
- Fixed-size hit, candidate, and overlap arrays avoid routine movement allocations.
- Optional gizmos show all four direct probes, input intent, candidate points/normals, the selected attachment, movement direction, active corner path, and the latest rejected transition point.
- Read-only diagnostics expose transition source, destination, progress, accepted direction, and the last failure reason. Optional `Log Transition Events` output reports state changes and failures without per-frame logging.

## Authored prototype course

`AntMovementPrototype.unity` now contains:

- the existing climbable floor;
- left and right climbable walls;
- a full-width climbable ceiling, forming a continuous enclosed rectangular loop;
- a floor-connected central block whose upper edges expose outside corners; and
- the player ant starting left of the central block with room to approach and reverse;
- separate one-tile floor, ceiling, and vertical-wall opening fixtures;
- a two-tile floor opening that must be rejected; and
- a one-tile opening with a non-climbable obstacle at its landing path.

`Milestone1PrototypeSetup` updates or regenerates these named objects without duplicating them. The Editor menu now also provides `Apply Auto Step-Over Course` and `Validate Auto Step-Over Course`; the older surface-transition and wall-climbing commands remain compatible.

## Automated validation performed

- Unity compiled the updated runtime and Editor assemblies successfully.
- The Editor migration serialized the ceiling course, central outside-corner block, new starting position, and new motor configuration.
- `ValidateSurfaceTransitionPrototype` passed, confirming prefab wiring, the climbable mask, rotatable Rigidbody2D, player scene instance, enclosed floor/wall/ceiling course, and outside-corner block.
- Repository whitespace validation passed with `git diff --check` after Unity-generated whitespace was cleaned up.
- A focused `AntColony.EditModeTests` Unity Test Framework assembly was added without moving the runtime scripts out of Unity's existing predefined assembly.
- The focused Edit Mode test passed 1/1. `AntSurfaceMotorRegressionValidation` exercised the actual motor transition method and covered release/pause, both intuitive reversal directions, blocked-first-step cancellation, later-step forward blocking, blocked-reverse safe cancellation, failure-reason recording, retry suppression, and suppression reset after input release.

These checks validate compilation and authored structure. The new ceiling and transition behavior has not been visually verified in Play Mode.

For the Auto Step-Over slice:

- Unity loaded the updated runtime and Editor assemblies without C# compilation errors during two command-line launches.
- Static authored-scene checks confirmed all 11 new fixture roots exist exactly once, all 74 YAML document identifiers are unique, the prefab serializes a one-world-unit tile and one-tile crossing limit, and `git diff --check` passes.
- A second Unity Test Framework case and `ValidateAutoStepOverRegression` were added. They exercise real floor/ceiling/wall detection, release pause, reversal to source, over-wide rejection, blocked-path rejection, failure reasons, and retry-suppression release.
- The current machine's Unity Licensing Client repeatedly lost its IPC connection before the migration method or test runner could execute. Consequently, the new 2/2 Edit Mode result, Editor structural validator, and automated scene import remain **not run**, not passed.

For the Milestone 2.1B world-data slice:

- Focused Edit Mode tests cover all required coordinate examples, negative boundaries, cell/world round trips, explicit and duplicate chunk creation, 1,024-cell Background coverage, optional Solid mutation, invalid IDs, read/mutation isolation, four-direction expansion, and the absence of gameplay GameObjects.
- The focused Unity suite passed 25/25. The user also confirmed that the existing ant movement remained functional in Play Mode.

For the Milestone 2.1C navigation-classification slice:

- Focused Edit Mode coverage was added for occupancy, all four support directions, diagonal rejection, independent material flags, positive and negative chunk boundaries, all quadrants, unavailable-neighbour uncertainty, support precedence, live mutation results, isolation, and scene independence.
- The user-confirmed acceptance run passed 50/50 Edit Mode tests. The current full run passed 61/61, including all 23 `WorldNavigationClassifierTests`; Milestone 2.1C is complete.

For the Milestone 2.1D navigation-visualisation slice:

- Unity imported and compiled the new `AntColony.NavigationDebug` runtime assembly, its custom Editor tooling, the prototype scene, and the expanded Edit Mode tests.
- The structural validator passed the authoritative eight-chunk fixture, bounded 3,564-cell overlay, representative classification examples, and absence of 2D or 3D colliders.
- The complete Edit Mode suite passed 61/61: 25 world-data, 23 classifier, 11 visualisation, and 2 movement-regression cases.
- The user confirmed all 11 graphics-enabled manual acceptance checks, including colours, labels, boundaries, toggles, mutation refresh, and preservation of the original ant movement. Milestone 2.1D is complete.

For the Milestone 2.1E world-Tilemap slice:

- Unity compiled `AntColony.WorldPresentation`, the expanded Editor tooling, and the updated tests without errors.
- The scene validator passed the one-root hierarchy, two material-driven Tilemaps, all 8,192 resident Background visuals, foreground ordering, signed coordinate alignment, remove/restore refresh sequence, unchanged classifier behavior, and absence of colliders or terrain Rigidbody2D components.
- The complete Edit Mode suite passed 89/89: the existing 61 regression cases plus 28 new Tilemap projection cases.
- The new cases cover both material layers, foreground removal with Background persistence, non-blocking and differently identified Solid visuals, missing references and unknown IDs, missing chunks, positive and negative boundaries, exact cell refresh scope, bulk negative-chunk refresh, repeated refresh object stability, classification isolation, and collision absence.
- The user confirmed all 10 graphics-enabled checks for visual alignment, layer ordering, visibility toggles, removal/restore behaviour, Background persistence, classifier refresh, and unchanged original ant movement. Milestone 2.1E is complete.

For the Milestone 2.2A terrain-collision slice:

- Unity compiled the collision projection, custom Editor tooling, generated assets, updated prototype scene and focused tests without C# errors.
- The scene validator passed the single collision-only Tilemap hierarchy, collider-free visual layers, material references and independent flag examples, `ClimbableSurface` assignment, static composite configuration, signed boundary alignment, mutation refresh, Background persistence, classifier isolation and duplicate-hit check.
- The complete Edit Mode suite passed 117/117: the existing 89 regression cases plus 28 new collision cases.
- The new cases exercise real Physics2D queries for blocking and non-blocking materials, all four blocking/support flag combinations, add/remove/replace mutations, merged neighbours, immediate explicit synchronization, requested-region isolation, unavailable chunks, repeated-refresh component stability, integer-centred cells and positive/negative X and Y chunk boundaries.
- The user confirmed all 12 graphics-enabled manual collision checks. Milestone 2.2A is complete.

For the Milestone 2.2B ant-clearance slice:

- Unity compiled the authoritative clearance API, Scene-view inspector and focused tests without C# errors.
- The corrected focused clearance suite passed 38/38, covering physically clear unsupported Sky, a support-free unprepared shaft, prepared/unprepared Background neutrality, complete-body overlap, four foreground material-flag combinations, blocked-over-unavailable precedence, horizontal/vertical/intermediate rotation, rotated offset, margin, narrow passages, concave corners, tangency tolerance, all signed chunk boundaries and quadrants, four-chunk footprints, missing target/footprint chunks, live mutations, and scene/physics independence.
- The scene validator passed the authored ordinary-ant 0.96×0.3 effective profile, Clear test pose, and absence of gameplay physics components on the inspector.
- The complete Edit Mode suite passed 155/155: all 117 pre-clearance regression cases plus 38 clearance cases. The existing movement regression fixture also passed separately at 2/2.
- Physical clearance now matches GDD v3.2 and is independent of Background grip/grounding eligibility. The user confirmed all 9 graphics-enabled manual clearance checks; Milestone 2.2B is complete.

For the Milestone 2.2C movement-capability slice:

- Unity compiled the Background properties, movement-capability evaluator, debug inspector, scene authoring and focused tests without C# errors or warnings.
- The focused capability suite passed 37/37, covering all four Background configurations, floor-only grounding, wall/ceiling/non-blocking rejection, grounded walking, prepared grip, unsupported falling, voluntary-versus-gravity descent, clearance separation, controlled Background replacement, signed chunk boundaries and all quadrants, unavailable data and scene/physics independence.
- The scene validator passed the four authored Background definitions, capability inspector, Falling-to-Gripping-to-Falling replacement round trip, explicit Tilemap refresh, unchanged classifier examples and absence of extra physics components.
- The complete Edit Mode suite passed 192/192, and the existing movement regression fixture passed separately at 2/2.
- Manual graphics-enabled Scene-view checks remain pending, so Milestone 2.2C is implemented and automated but not complete.

### Milestone 2.2C manual Scene-view checklist

1. Open `AntNavigationPrototype.unity`, select `Ant Movement Capability Inspector`, and confirm the default chamber-centre result is physically Clear, ungrounded, non-gripping and Falling.
2. Click `Toggle Prepared Background, Refresh, and Requery`; confirm the selected cell changes visual, reports the prepared material ID, enables horizontal/up/down movement, reports grip and changes to Gripping with gravity disabled.
3. Toggle again; confirm the original unprepared visual and Falling result return immediately.
4. Move the handle to a clearance-valid pose exactly `0.15` units above a floor top; confirm Grounded and horizontal-only voluntary movement.
5. Place the handle beside a wall and below a ceiling without a floor; confirm neither produces Grounded.
6. Place the capsule over blocking foreground; confirm `BlockedByTerrain` and no valid movement state.
7. Move the handle into a missing chunk or across a missing footprint boundary; confirm an explicit unavailable result rather than Falling or Gripping.
8. Confirm the existing navigation-classification overlay does not change when only Background preparation changes.
9. Confirm the Console remains clean and the original `AntMovementPrototype` movement still behaves unchanged.

## Manual Play Mode checklist

1. Confirm the previously working floor movement, stopping, reversal, and both wall directions still behave identically.
2. Approach a wall using A/D only; confirm collision stops the ant without an automatic climb.
3. Press W near the wall; confirm floor-to-wall rotation and movement remain smooth and collision-safe.
4. Climb toward the ceiling using W only; confirm the ant stops without arbitrarily turning left or right.
5. At the wall/ceiling corner, press the direction leading into the room; confirm a wall-to-ceiling transition and approximately 180-degree ceiling alignment.
6. Use A/D along the ceiling; confirm normal speed, stopping, and reversal.
7. Press the appropriate W/S direction at each ceiling end; confirm ceiling-to-wall transitions in both directions.
8. Descend to the floor and deliberately select the valid A/D floor direction; confirm wall-to-floor transition.
9. Climb a side of `Outside Corner Block`, then use diagonal intent toward and around an upper edge; confirm a smooth outside-corner transition to its top.
10. Traverse the opposite outside corner from the block top onto its side, then reverse midway through a transition.
11. Try choosing the solid-facing or unavailable direction at inside and outside corners; confirm the transition is rejected without clipping, teleporting, or falling through terrain.
12. Release input midway through and after transitions; confirm the ant stops and remains supported.
13. Enable `Show Debug Gizmos` and inspect candidate normals, selected contact, orange transition path, and any red rejected point.
14. Confirm the Console contains no errors or warnings throughout repeated clockwise and counter-clockwise loops.
15. Reproduce the original deadlock path, then try release, the opposite source direction, and the opposite destination direction; confirm transition state always pauses, reverses, completes, or cancels instead of permanently consuming input.
16. On `One Tile Floor Approach`, hold the tangent direction and confirm the ant crosses continuously to `One Tile Floor Landing` without jumping, falling, or changing speed unexpectedly.
17. Repeat across `One Tile Ceiling Approach`/`Landing` and `One Tile Wall Lower`/`Upper` in both directions.
18. At a valid opening, press a strong perpendicular or diagonal input; confirm explicit directional intent takes priority and Auto Step-Over does not seize control.
19. Release input partway across a valid opening, then reverse; confirm the ant pauses and returns safely to its source support.
20. Approach the two-tile floor opening and the blocked landing; confirm both stop safely, do not oscillate or retry every frame, and become eligible for a new attempt after input release/change.
21. Enable `Show Debug Gizmos`; confirm the measured gap endpoints/path match the authored opening and rejection reasons are useful without per-frame Console spam.

## Known limitations and remaining P1 work

- The original corner-deadlock reproduction is user-confirmed resolved; the wider ceiling/corner route still needs full Play Mode acceptance.
- The newly implemented ceiling and wider corner behavior is not yet Play Mode accepted and may require tuning of probe distance, look-ahead distance, collider inset, or rotation speed.
- Corners are intentionally limited to nearby, approximately perpendicular surfaces. Irregular or curved terrain is not supported by this slice.
- Outside corners require deliberate diagonal intent because both approaching the edge and selecting the outgoing surface must be unambiguous.
- The transition path is validated against current static collision geometry; runtime excavation and dynamic traversal invalidation do not exist yet.
- Auto Step-Over and its authored fixtures are implemented but have not completed automated Unity execution or manual Play Mode acceptance on this machine.
- The current Step-Over path is intentionally straight between parallel supports. Irregular, curved, offset, or routed openings remain unsupported.
- The ant-follow camera remains unimplemented; the integrated sample scene has only a developer-controlled inspection camera.
- World data, material visuals and authoritative terrain collision are now connected in both the separate navigation fixture and the bounded integrated development scene. The integrated scene adds explicit sample generation, but persistence, pathfinding, autonomous movement, excavation gameplay and legacy DirtBlock conversion remain absent.
- The collision projection currently requires an explicit cell, chunk or bounded-region refresh after authoritative Solid mutation; no terrain-change event stream or chunk streaming lifecycle exists yet. Clearance itself reads world data directly and does not share that staleness risk.
- Static clearance does not validate Background permissions, grip, grounding, gravity, a swept movement segment, directional movement edge, route connectivity, Reachable Space, dynamic objects or ant-to-ant separation.
- Mathematical tangency is permitted within a `0.00001`-unit tolerance; future movement integration may use a small positive profile margin to remain conservative relative to Unity Physics2D contact handling.
- Background Material movement/grip/gravity properties are implemented as read-only capability data. Actual gravity forces, movement execution, Surface Preparation jobs, resource consumption, universal 0–100 skills and inactivity decay above 50 are not implemented.
- Milestone 2.2C needs graphics-enabled manual Scene-view acceptance before it can be marked complete.
- The integrated sample scene needs graphics-enabled Play Mode acceptance for visual readability, pan/zoom, cell selection, both mutation round trips and a clean Console.
- Autonomous navigation, multi-ant occupancy, and colony simulation remain later milestones.

## Recommended next implementation step

Complete the graphics-enabled `AntGameDevelopment.unity` Play Mode checklist, while keeping the separate Milestone 2.2C Scene-view acceptance status explicit. Do not begin 2.2D directional connections until the required acceptance work is confirmed; Surface Preparation gameplay, skills, pathfinding and autonomous movement remain later work.
