# Ant Colony Current Game State

## Current milestone

**Milestone 1 — Player Ant Movement Prototype**
**Status:** In progress. The horizontal floor-movement slice is implemented; manual Play Mode verification is pending.

## Repository assessment

- A Unity project exists at the repository root and uses Unity `6000.3.8f1`.
- The project was created from a Unity 2D URP template.
- `Assets/AntColony/Scenes/AntMovementPrototype.unity` is the first build scene, and the original `Assets/Scenes/SampleScene.unity` remains available as the second scene.
- The new Input System is enabled and the template input actions asset contains a `Player/Move` action.
- Unity's 2D, Tilemap, Tilemap Extras, Sprite Shape, 2D Animation, URP, and Test Framework packages are already available.
- Netcode for GameObjects is not installed. The built-in Multiplayer Center entry is present, but no networking gameplay package or networking code is present.
- The first movement script, player-ant prefab, placeholder visuals, and flat-floor prototype scene are present. Tilemaps, a complete tunnel, and automated tests are not present yet.
- No Bootstrap or global game manager exists. Future work should preserve an authored-content-first approach and should not introduce one unless a later requirement genuinely needs it.

## Documentation status

- `Documentation/GameDesignDocument.md` version 0.2 is the primary design reference.
- The GDD confirms P1 individual surface traversal with one ordinary player ant as the immediate task. Becoming a queen is later progression.
- Directional WASD input, whole-footprint clearance, physical ant occupancy, Auto Step-Over, tunnel-width rules, shared player/autonomous traversal, dynamic excavation updates and future traffic recovery are now established design rules.
- `Documentation/DevelopmentRoadmap.md` now records the approved Milestone 1 scope and proposed implementation order.
- Documented rules are not treated as implemented or verified gameplay.

## Implemented systems

- `AntSurfaceMotor` reads the existing `Player/Move` Input System action and applies grounded horizontal velocity through `Rigidbody2D` in `FixedUpdate`.
- A short floor probe limits steering to the authored `ClimbableSurface` layer. Jumping and free flight are intentionally absent.
- `PlayerAnt.prefab` contains the placeholder sprite, `Rigidbody2D`, horizontal `CapsuleCollider2D`, `PlayerInput`, and movement component with its layer reference assigned.
- `AntMovementPrototype.unity` contains a camera, one wide prototype floor, and one prefab instance of the ordinary player ant.
- The prototype scene is the first enabled scene in Build Settings; the original template scene remains available.
- `Milestone1PrototypeSetup` is an Editor-only, repeatable setup command. It authors project assets and is not a runtime Bootstrap or manager.

## Verified working features

The open Unity Editor successfully compiled the new scripts and executed the setup command to create the prefab and scene assets. Serialized-file inspection confirmed the expected component references, physics layer mask, and Build Settings entry.

Repository inspection confirmed that:

- the Unity project version and package manifests are present;
- the sample scene is included in Build Settings;
- the project uses the new Input System;
- the expected 2D physics and rendering packages are declared; and
- generated Unity folders are excluded by `.gitignore`.

Horizontal input, grounded movement, stopping, and direction reversal have not yet been observed in Play Mode. This implementation slice is therefore not marked complete.

## Designed but not implemented

- Full WASD world-direction interpretation. The current motor reads the `Move` action but uses only its horizontal component.
- Separation of direct-input intent from reusable physical movement execution.
- Surface attachment and rotation on walls, ceilings, inside corners and outside corners.
- Directional choice between reachable surfaces at junctions.
- Configurable Auto Step-Over with swept-footprint, landing-surface and obstruction checks.
- Tile-scale logical occupancy based on each ant's physical footprint.
- Multiple ants, selection handover, ant-to-ant collision validation, following distance, passing, yielding and deadlock recovery.
- Static surface connectivity, dynamic occupancy, route planning, task navigation and blocked-route recovery.
- Incremental navigation updates after runtime excavation.
- Scalability measures such as shared traversal data, event-driven invalidation and distributed simulation work.

These are agreed design requirements. Their specific algorithms and component boundaries remain deferred until their roadmap stage is approved and can be tested incrementally.

## Known issues and risks

- Horizontal floor movement still requires hands-on Play Mode verification with A/D and Left/Right input.
- Wall, ceiling, corner-transition, surface-alignment, and camera-follow behaviour are not implemented yet.
- W/S surface selection and directional junction choice are not implemented; the existing floor-only motor intentionally ignores vertical input.
- Auto Step-Over distance, clearance query shape and transition timing require prototype testing before their final values are chosen.
- Surface transitions around corners are the main technical risk for Milestone 1. They should be developed after flat floor, wall, and ceiling movement work independently.
- The routing representation, yielding priority, reservation needs and deadlock-recovery limits remain unresolved future implementation decisions.

## Authored Unity Editor setup

The first implementation slice created the following setup:

1. `Assets/AntColony/Scenes/AntMovementPrototype.unity` is the prototype scene.
2. `ClimbableSurface` is user layer 8.
3. `Prototype Floor` uses a visible placeholder sprite and `BoxCollider2D` on the climbable layer.
4. `PlayerAnt.prefab` contains:
   - a placeholder `SpriteRenderer`;
   - a dynamic `Rigidbody2D` with gravity scale 4, interpolation enabled, continuous collision detection, and frozen rotation;
   - a horizontal `CapsuleCollider2D` sized to 1.2 by 0.6 units;
   - `PlayerInput` using `InputSystem_Actions` and the `Player` action map; and
   - `AntSurfaceMotor` with speed 4, a 0.12-unit floor probe, and the `ClimbableSurface` mask.
5. The Main Camera is orthographic with size 5 and remains stationary for this floor-only slice.

Before continuing, open `AntMovementPrototype`, enter Play Mode, and verify that A/D and Left/Right move, stop, and reverse the ant without Console errors.

## Next approved development step

Manually verify the horizontal floor-movement slice in Play Mode. If it behaves correctly, the next small implementation slice should separate WASD world-direction intent from `AntSurfaceMotor` while preserving the current floor behaviour. Surface detection and alignment can then consume that shared intent without introducing autonomous AI or pathfinding.
