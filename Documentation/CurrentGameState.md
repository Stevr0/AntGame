# Ant Colony Current Game State

## Current milestone

**Milestone 1 — Player Ant Movement Prototype**

**Status:** In progress. The user has confirmed floor movement and vertical wall climbing in Unity Play Mode. Live testing then found a corner-transition deadlock. Its state-machine causes are fixed in code and covered by an automated Editor regression harness, but the fix and the wider ceiling/corner route still require Play Mode acceptance.

## Repository and architecture

- The Unity 2D URP project uses Unity `6000.3.8f1` and the new Input System.
- `Assets/AntColony/Scenes/AntMovementPrototype.unity` is the first build scene.
- The prototype controls one ordinary player ant. Becoming a queen is later progression.
- Input and movement retain the established `PlayerMovementInput -> AntSurfaceMotor -> Rigidbody2D` flow.
- `AntSurfaceMotor.SetMovementIntent(Vector2)` remains the controller-neutral boundary for future autonomous control.
- No Bootstrap, global manager, AI navigation, excavation, multiplayer, or multi-ant system has been added.

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

### Attachment and debugging

- Attachment states now include `None`, `Floor`, `LeftWall`, `RightWall`, and `Ceiling`, plus an explicit active corner-transition state.
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
- the player ant starting left of the central block with room to approach and reverse.

`Milestone1PrototypeSetup` updates or regenerates these named objects without duplicating them. The Editor menu provides `Apply Surface Transition Course` and `Validate Surface Transition Course` commands; the older wall-climbing commands remain compatible wrappers.

## Automated validation performed

- Unity compiled the updated runtime and Editor assemblies successfully.
- The Editor migration serialized the ceiling course, central outside-corner block, new starting position, and new motor configuration.
- `ValidateSurfaceTransitionPrototype` passed, confirming prefab wiring, the climbable mask, rotatable Rigidbody2D, player scene instance, enclosed floor/wall/ceiling course, and outside-corner block.
- Repository whitespace validation passed with `git diff --check` after Unity-generated whitespace was cleaned up.
- A focused `AntColony.EditModeTests` Unity Test Framework assembly was added without moving the runtime scripts out of Unity's existing predefined assembly.
- The focused Edit Mode test passed 1/1. `AntSurfaceMotorRegressionValidation` exercised the actual motor transition method and covered release/pause, both intuitive reversal directions, blocked-first-step cancellation, later-step forward blocking, blocked-reverse safe cancellation, failure-reason recording, retry suppression, and suppression reset after input release.

These checks validate compilation and authored structure. The new ceiling and transition behavior has not been visually verified in Play Mode.

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

## Known limitations and remaining P1 work

- The corner-deadlock fix has passed automated Editor regression checks but has not yet been re-tested in live Play Mode. It must not be considered resolved until the original reproduction no longer locks movement.
- The newly implemented ceiling and wider corner behavior is not yet Play Mode accepted and may require tuning of probe distance, look-ahead distance, collider inset, or rotation speed.
- Corners are intentionally limited to nearby, approximately perpendicular surfaces. Irregular or curved terrain is not supported by this slice.
- Outside corners require deliberate diagonal intent because both approaching the edge and selecting the outgoing surface must be unambiguous.
- The transition path is validated against current static collision geometry; runtime excavation and dynamic traversal invalidation do not exist yet.
- Auto Step-Over, the physically valid one-tile opening test, and the follow camera remain unimplemented.
- Autonomous navigation, multi-ant occupancy, and colony simulation remain later milestones.

## Recommended next P1 slice

Run and tune the complete transition course in Play Mode first. Once floor, wall, ceiling, inside-corner, and outside-corner acceptance passes, implement Auto Step-Over as a separate bounded feature with swept whole-footprint clearance and an explicitly configured maximum crossing distance.
