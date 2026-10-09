# Ant Colony Current Game State

## Current milestone

**Milestone 1 — Player Ant Movement Prototype**

**Status:** In progress. The user has confirmed floor movement, vertical wall climbing, and resolution of the previously reported corner-transition deadlock in Unity Play Mode. Ceiling/corner route acceptance is still incomplete. Bounded Auto Step-Over is now implemented for one-tile openings and awaits its first Play Mode acceptance pass.

Milestone 1 is explicitly a prototype and regression baseline, not the final player control design. Milestone 2.1B now adds a data-only two-layer world model alongside that prototype. Autonomous navigation, tile presentation, terrain migration, colony management and individual task assignment have not been implemented.

## Repository and architecture

- The Unity 2D URP project uses Unity `6000.3.8f1` and the new Input System.
- `Assets/AntColony/Scenes/AntMovementPrototype.unity` is the first build scene.
- The prototype controls one ordinary player ant. Becoming a queen is later progression.
- Input and movement retain the established `PlayerMovementInput -> AntSurfaceMotor -> Rigidbody2D` flow.
- `AntSurfaceMotor.SetMovementIntent(Vector2)` remains a useful prototype boundary while the future movement model is developed and validated.
- The current authored collider course is not evidence of the approved Background Tile/Solid Tile data model.
- No Bootstrap or global manager has been added. The world-data authority is an ordinary, scene-independent C# object; AI navigation, excavation, multiplayer, and multi-ant systems remain absent.

### Milestone 2.1B world-data foundation

- `ChunkedWorldData` explicitly owns resident chunks in a sparse dictionary. Creating or reading world data does not create GameObjects, components, colliders, or neighbouring chunks.
- Each `WorldChunk` is 32×32 cells and immediately fills all 1,024 Background Tile entries. A parallel compact array stores zero or one optional Solid Tile ID per cell.
- Central coordinate utilities use floor division for negative positions, keep local coordinates in the 0–31 range, and preserve the prototype convention that integer coordinates are tile centres in world space.
- Missing chunks return `ChunkUnavailable`; they never masquerade as exposed cells. Duplicate creation and unknown material IDs are rejected without overwriting data.
- `Background_Default` (`background.default`) and `Solid_Soil` (`solid.soil`) are initial authored ScriptableObject definitions. Their optional visual Tile references are currently empty because rendering is outside this milestone.
- A world seed can be stored for future deterministic generation, but no generation, unloading, streaming, persistence, or scene-level host exists.
- The existing `DirtBlock` objects have not been imported. The prototype scene, prefab, movement code, Rigidbody2D, colliders, gravity, and input remain unchanged.

## Approved future design, not implemented beyond the data foundation

- Normal play manages ants through selection, inspection, designations, colony priorities and specific task assignments; it does not directly steer them.
- Every coordinate in a generated chunk contains one permanent Background Tile and may additionally contain one foreground Solid Tile; ungenerated chunks remain unavailable.
- Excavation removes an excavatable Solid Tile, leaves its Background Tile unchanged, updates collision and reevaluates affected navigation.
- Background Tiles do not inherently block movement or provide navigation support.
- Traversable Exposed Spaces require Qualifying Geometry in a cardinal direction, adequate whole-body clearance and applicable obstacle clearance. Reachable Space additionally requires a valid connection to the ant's current navigation region.
- Eight-direction route connections are allowed, but diagonal corner-cutting and unsupported-air movement are not.
- Large chamber centres without cardinal support remain unsupported; perimeter routes may be valid.
- Multiple ants may share a tile and use hybrid separation, local avoidance and bounded recovery rather than universal one-ant-per-tile occupancy.
- Friendly, neutral and hostile colonies share the same terrain navigation model.
- The proposed architecture separates world tile data, terrain/excavation changes, colony task decisions, shared navigation data, per-ant route following, continuous movement, local avoidance and authoritative collision.
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

- Focused Edit Mode tests were added for all required coordinate examples, negative boundaries, cell/world round trips, explicit and duplicate chunk creation, 1,024-cell Background coverage, optional Solid mutation, invalid IDs, read/mutation isolation, four-direction expansion, and the absence of gameplay GameObjects.
- Static source and repository checks can be performed outside Unity. The focused Unity suite has not yet run because this project is already open in another Unity instance and the desktop automation bridge cannot access that window; compilation and test results therefore remain pending rather than passed.

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
- The follow camera remains unimplemented.
- The Milestone 2.1B data model is not connected to a scene, Tilemap, collider, generator, persistence layer, navigation classifier, or the legacy DirtBlocks.
- Autonomous navigation, multi-ant occupancy, and colony simulation remain later milestones.

## Recommended next implementation step

Run the existing movement validation still pending for Milestone 1, and import/compile the new `AntColony.World` assembly in the open Editor. Then run `ChunkedWorldDataTests` before marking Milestone 2.1B complete. The next authorised world slice should begin with navigation classification and visualisation over authoritative resident chunk data; do not connect or replace the legacy prototype terrain implicitly.
