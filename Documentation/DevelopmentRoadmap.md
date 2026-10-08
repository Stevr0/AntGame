# Ant Colony Development Roadmap

## Roadmap status

This roadmap reflects the repository as inspected on 8 October 2026. Milestone 1 is in progress. The first horizontal-movement slice is implemented but is not complete until its Play Mode acceptance checks pass.

`GameDesignDocument.md` version 0.1 is the primary design reference. Its P1 surface-traversal gate matches this milestone: one ordinary player ant must traverse the floor, walls, and ceiling of a pre-excavated test tunnel without collision glitches. This roadmap breaks that gate into small implementation steps; it does not replace the GDD.

## Milestone 1 — Player Ant Movement Prototype

**Status:** In progress. The authored prototype scene, player-ant prefab, and horizontal floor movement are implemented. Manual Play Mode verification is pending before wall and ceiling work begins.

**Goal:** Prove that one directly controlled ordinary ant can move through a small, authored 2D underground tunnel and that a simple camera can follow it. Becoming a queen is later progression and is outside this milestone.

### Scope

- One ordinary player ant in one small test scene.
- Horizontal floor movement.
- Vertical wall climbing.
- Upside-down ceiling movement.
- Smooth transitions between connected floor, wall, and ceiling surfaces.
- Natural rotation so the ant aligns with the current surface.
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

1. **Author the test scene and player-ant prefab**
   - Create a small tunnel from simple 2D collider shapes or a Tilemap with a `TilemapCollider2D`.
   - Create a placeholder player ant with a `SpriteRenderer`, `Rigidbody2D`, and a simple 2D collider.
   - Put traversable tunnel geometry on a dedicated `ClimbableSurface` layer.
   - Confirm that the scene opens without errors and its collision geometry is visible and correct.

2. **Add horizontal floor movement**
   - Add a focused ant movement script that reads the existing Input System `Move` action.
   - Move the ant with `Rigidbody2D` in `FixedUpdate` so physics remains predictable.
   - Detect the nearby floor and keep the ant attached without adding jumping or free flight.
   - Verify starting, stopping, reversing direction, and remaining attached on a flat floor.

3. **Add surface detection and alignment**
   - Detect a nearby climbable surface with short 2D physics queries.
   - Derive the ant's outward direction from the surface normal.
   - Rotate toward that orientation at a configurable rate rather than snapping instantly.
   - Keep detection and movement in one small component initially; split them only if the code becomes difficult to understand or test.

4. **Support walls and ceilings**
   - Move along the tangent of the detected surface instead of assuming world-horizontal movement.
   - Verify movement on isolated floor, wall, and ceiling sections before testing corners.
   - Ensure the ant does not drift away from the surface or tunnel through it.

5. **Support connected-surface transitions**
   - Add short forward/around-corner checks so the ant can acquire the next connected surface.
   - Tune attachment force, query distance, rotation speed, and collider size for smooth inner and outer corners.
   - Verify clockwise and counter-clockwise transitions across floor-to-wall, wall-to-ceiling, ceiling-to-wall, and wall-to-floor joins.

6. **Add the camera follow**
   - Add one small camera-follow script with configurable smoothing and optional position offset.
   - Keep the camera upright; the ant's surface rotation must not rotate the view.
   - Verify that the ant stays visible through the complete tunnel route.

7. **Validate and document the milestone**
   - Run the complete route repeatedly in Play Mode in both directions.
   - Check the Console for errors and warnings.
   - Record Inspector values and any remaining limitations in `CurrentGameState.md`.
   - Mark the milestone complete only after all completion criteria have been observed in Unity.

### Completion criteria

- The player ant can traverse a continuous authored route containing floors, vertical walls, and ceilings in both directions.
- Surface transitions do not require teleporting, scene-specific trigger scripts, or manual Inspector changes during play.
- The ant remains aligned and attached without obvious jitter, tunnelling, or unintended detachment.
- The camera follows without inheriting the ant's rotation or showing disruptive jitter.
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

- `AntSurfaceMotor.cs` — reads movement input, finds the current surface, moves the ant along it, and aligns it with the surface. Surface sensing can become a separate component later only if that makes the implementation clearer.
- `CameraFollow2D.cs` — follows a target position with simple smoothing while keeping the camera upright.

The existing root-level Unity template assets can remain where they are until the authored Ant Colony scene replaces the template scene. No Bootstrap exists in the current repository; Milestone 1 should use authored scene and prefab references and should not add a global manager or new bootstrap system.

## Later GDD stages

These stages are recorded from GDD version 0.1 for visibility. They are not approved implementation work while Milestone 1 remains incomplete.

| GDD stage | Deliverable | Status |
| --- | --- | --- |
| P2 — Tile excavation | Designate accessible tiles, navigate to them, remove them, and open new paths. | Not started |
| P3 — Soil logistics | Deliver exactly one soil unit per excavated tile to a fixed deposit point. | Not started |
| P4 — Individual ants | Support multiple selectable ants, basic roles, and autonomous job completion. | Not started |
| P5 — Survival and growth | Add food, water, rest, the queen brood cycle, and skill-by-doing at small scale. | Not started |
| P6 — Finished slice | Add onboarding, a clear objective, win/lose feedback, save/load, and a usability pass. | Not started |

No later stage should begin until the current acceptance gate is verified and the developer approves the next stage.
