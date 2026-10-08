# Ant Colony Current Game State

## Current milestone

**Milestone 1 — Player Ant Movement Prototype**

**Status:** In progress. Floor detection and isolated left/right wall climbing are implemented in code and authored content. Hands-on Play Mode acceptance is still pending.

## Repository assessment

- The Unity 2D URP project uses Unity `6000.3.8f1`.
- `Assets/AntColony/Scenes/AntMovementPrototype.unity` is the first build scene; the original template scene remains available.
- The project uses the new Input System and the existing `Player/Move` action.
- The current prototype controls one ordinary player ant. Becoming a queen is later progression.
- No Bootstrap or global manager exists. The milestone continues to use authored scene and prefab references.
- Netcode, excavation, autonomous navigation, and multi-ant systems are not implemented.

## Implemented systems

### Directional input

- `PlayerMovementInput` reads the existing `Player/Move` action as a `Vector2`, normalises diagonal input, and calls `AntSurfaceMotor.SetMovementIntent(Vector2)`.
- WASD and arrow-key composites supply up, down, left, and right intent. Opposing composite bindings cancel through the Input System.
- `AntSurfaceMotor` does not read keyboard or `PlayerInput` state. A future controller can submit the same movement intent after player control is disabled.

### Floor and wall motor

- `AntSurfaceMotor` performs three non-allocating `Collider2D.Cast` probes: down, left, and right.
- Each probe records the nearest contact point, surface normal, and distance. The nearest obstruction must be on the `ClimbableSurface` layer and its normal must match the expected floor or wall direction.
- Attachment has explicit `None`, `Floor`, `LeftWall`, and `RightWall` states. `IsAttached`, `IsGrounded`, `SurfaceType`, `SurfacePoint`, and `SurfaceNormal` are read-only public state.
- On a floor, A/D applies horizontal `Rigidbody2D` velocity. A wall is selected only when upward intent is pressed while that wall is within the configured probe distance; walking into it with A/D alone remains blocked by physics.
- On a wall, W applies upward velocity and S applies downward velocity. Releasing input sets traversal velocity to zero while gravity remains disabled for a valid attachment.
- The motor restores the Rigidbody2D's authored gravity when no valid supported surface is detected. It does not teleport the ant back to a lost surface and does not provide air steering.
- Attached orientation is derived from the surface normal and approached at a configurable rotation rate with `Rigidbody2D.MoveRotation`. The physical collider rotates with the ant.
- Small attachment correction is applied only across a detected positive gap. At contact distance zero, the motor leaves collision resolution to Physics2D instead of continually pushing into terrain.
- Optional selected-object gizmos show the three probe directions, attached/detached colour, contact point, normal, and applied movement direction. They are disabled by default.

### Authored prototype

- `PlayerAnt.prefab` contains the placeholder sprite, dynamic `Rigidbody2D`, horizontal `CapsuleCollider2D`, `PlayerInput`, `PlayerMovementInput`, and `AntSurfaceMotor`.
- Rigidbody rotation is no longer frozen because surface alignment is motor-controlled. Gravity scale 4, interpolation, and continuous collision detection remain configured.
- The motor uses movement speed 4, rotation speed 360 degrees per second, a 0.12-unit surface probe, normal tolerance 0.7, and the `ClimbableSurface` mask.
- `AntMovementPrototype.unity` contains the existing floor plus left and right six-unit-tall climbable walls. The orthographic camera is framed at size 5.5 to show both climbing sections.
- `Milestone1PrototypeSetup` can apply the wall-climbing migration without regenerating unrelated assets and can structurally validate the prefab and scene.

## Automated validation performed

- Unity compiled the runtime and Editor assemblies successfully with exit code 0.
- The wall-climbing migration completed successfully and serialized the prefab and scene.
- `ValidateWallClimbingPrototype` passed. It verified the player input and motor components, a rotatable Rigidbody2D, the climbable layer mask, the player scene instance, and colliders/layers for the floor and both walls.
- Repository whitespace validation passed with `git diff --check` after generated whitespace was cleaned up.
- No NUnit test assemblies currently exist in the project, so no automated movement test suite was available to run.

These checks prove compilation and authored setup only. They do not prove successful physical traversal or movement feel.

## Manual Play Mode checklist

1. Open `Assets/AntColony/Scenes/AntMovementPrototype.unity`, enter Play Mode, and keep the Console visible.
2. Use A/D and Left/Right on the floor; confirm normal movement, stopping, cancellation of opposing input, and immediate direction reversal still work.
3. Hold W/S away from a wall; confirm neither input creates jumping or unsupported vertical movement.
4. Walk into each wall using A/D alone; confirm the ant stops at the collider and does not automatically climb or pass through it.
5. While beside a wall, press W; confirm attachment changes to that wall, the ant climbs upward, and its feet rotate toward the wall by approximately 90 degrees.
6. While attached, press S; confirm the ant descends without sliding through the floor.
7. Release all input on each wall; confirm the ant remains attached and stationary.
8. Test diagonal input beside each wall; confirm input remains normalised and does not increase movement speed.
9. Move beyond a valid wall section if practical; confirm loss of the wall restores gravity rather than creating invisible support or teleporting the ant.
10. Confirm the ant never crosses the floor or wall colliders and the Console has no errors.
11. Optionally enable `Show Debug Gizmos` on `AntSurfaceMotor`, select the ant, and inspect probe, point, normal, direction, and attachment-state gizmos in the Scene view.

## Known limitations and remaining P1 work

- Physical floor and wall traversal has not yet been observed in Play Mode, so this slice is not marked complete.
- The current wall acquisition is deliberately narrow: upward intent selects a nearby side wall. There is no general junction solver.
- Ceiling detection, upside-down movement, floor-to-wall corner polish, wall-to-ceiling transitions, inside/outside corners, and bidirectional connected-surface traversal are not implemented.
- The rotating capsule at the floor/wall join is provisional and may need probe distance, collider, rotation-rate, or clearance tuning after Play Mode observation.
- Whole-route footprint clearance and obstruction testing are not yet implemented. Collision is currently enforced by the Rigidbody2D/collider and nearest-obstruction probe rule.
- Auto Step-Over, camera follow, a valid one-tile opening test, excavation, autonomous navigation, and multi-ant occupancy remain future work.

## Recommended next P1 slice

First complete the manual floor/wall checklist and tune only the current probe, rotation, and collider values if evidence requires it. Once isolated wall movement is accepted, add isolated ceiling detection and ceiling-tangent movement through the same motor. Keep connected corner transitions and general directional junction choice as a later, separate slice.
