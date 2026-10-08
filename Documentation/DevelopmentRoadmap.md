# Ant Colony Development Roadmap

## Roadmap status

This roadmap reflects the repository as inspected on 8 October 2026. The project is at the planning stage for Milestone 1. No gameplay feature is complete or Unity-verified yet.

The requested `GameDesignDocument.md` is not present in the repository. Until it is added, the initial project brief is the provisional scope reference. This roadmap does not replace or modify the future GDD.

## Milestone 1 — Queen Movement Prototype

**Status:** Planning complete; implementation is awaiting approval.

**Goal:** Prove that one directly controlled queen can move through a small, authored 2D underground tunnel and that a simple camera can follow her.

### Scope

- One queen ant in one small test scene.
- Horizontal floor movement.
- Vertical wall climbing.
- Upside-down ceiling movement.
- Smooth transitions between connected floor, wall, and ceiling surfaces.
- Natural rotation so the queen aligns with the current surface.
- A simple 2D follow camera.
- Placeholder visuals are acceptable while movement is being proved.

### Explicitly out of scope

- Excavation and terrain modification.
- Food, water, rest, health, reproduction, or other survival systems.
- Worker ants, jobs, autonomous behaviour, or pathfinding.
- Skills, attributes, inventory, resources, dumping, or colony management.
- Multiplayer or networking.

### Proposed implementation sequence

Each step should be tested in the Unity Editor before the next step begins.

1. **Author the test scene and queen prefab**
   - Create a small tunnel from simple 2D collider shapes or a Tilemap with a `TilemapCollider2D`.
   - Create a placeholder queen with a `SpriteRenderer`, `Rigidbody2D`, and a simple 2D collider.
   - Put traversable tunnel geometry on a dedicated `ClimbableSurface` layer.
   - Confirm that the scene opens without errors and its collision geometry is visible and correct.

2. **Add horizontal floor movement**
   - Add a focused queen movement script that reads the existing Input System `Move` action.
   - Move the queen with `Rigidbody2D` in `FixedUpdate` so physics remains predictable.
   - Detect the nearby floor and keep the queen attached without adding jumping or free flight.
   - Verify starting, stopping, reversing direction, and remaining attached on a flat floor.

3. **Add surface detection and alignment**
   - Detect a nearby climbable surface with short 2D physics queries.
   - Derive the queen's outward direction from the surface normal.
   - Rotate toward that orientation at a configurable rate rather than snapping instantly.
   - Keep detection and movement in one small component initially; split them only if the code becomes difficult to understand or test.

4. **Support walls and ceilings**
   - Move along the tangent of the detected surface instead of assuming world-horizontal movement.
   - Verify movement on isolated floor, wall, and ceiling sections before testing corners.
   - Ensure the queen does not drift away from the surface or tunnel through it.

5. **Support connected-surface transitions**
   - Add short forward/around-corner checks so the queen can acquire the next connected surface.
   - Tune attachment force, query distance, rotation speed, and collider size for smooth inner and outer corners.
   - Verify clockwise and counter-clockwise transitions across floor-to-wall, wall-to-ceiling, ceiling-to-wall, and wall-to-floor joins.

6. **Add the camera follow**
   - Add one small camera-follow script with configurable smoothing and optional position offset.
   - Keep the camera upright; the queen's surface rotation must not rotate the view.
   - Verify that the queen stays visible through the complete tunnel route.

7. **Validate and document the milestone**
   - Run the complete route repeatedly in Play Mode in both directions.
   - Check the Console for errors and warnings.
   - Record Inspector values and any remaining limitations in `CurrentGameState.md`.
   - Mark the milestone complete only after all completion criteria have been observed in Unity.

### Completion criteria

- The queen can traverse a continuous authored route containing floors, vertical walls, and ceilings in both directions.
- Surface transitions do not require teleporting, scene-specific trigger scripts, or manual Inspector changes during play.
- The queen remains aligned and attached without obvious jitter, tunnelling, or unintended detachment.
- The camera follows without inheriting the queen's rotation or showing disruptive jitter.
- The Unity Console remains free of errors during the test.
- Required scene objects, components, layers, controls, and Inspector values are documented.

### Minimal proposed project structure

Create these folders only as their contents are needed:

```text
Assets/
  AntColony/
    Art/
      Placeholder/
    Prefabs/
      Ants/
    Scenes/
    Scripts/
      Ants/
      Camera/
    Tests/
      EditMode/
      PlayMode/
```

The initial script set should remain small:

- `QueenSurfaceMotor.cs` — reads movement input, finds the current surface, moves the queen along it, and aligns her with it. Surface sensing can become a separate component later only if that makes the implementation clearer.
- `CameraFollow2D.cs` — follows a target position with simple smoothing while keeping the camera upright.

The existing root-level Unity template assets can remain where they are until the authored Ant Colony scene replaces the template scene. No Bootstrap exists in the current repository; Milestone 1 should use authored scene and prefab references and should not add a global manager or new bootstrap system.

## Later milestones

Later colony features remain deferred until Milestone 1 is verified. Their order and acceptance criteria should be taken from the GDD once that document is available. No later system should be inferred from this roadmap or started without approval.

