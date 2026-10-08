# Ant Colony Development Roadmap

## Roadmap status

This roadmap reflects the repository as inspected on 8 October 2026. Milestone 1 is in progress. Directional input, floor detection, and isolated left/right wall movement are implemented, but physical traversal still requires Play Mode acceptance.

`GameDesignDocument.md` version 0.2 is the primary design reference. Its P1 individual-traversal gate requires one ordinary player ant to use directional input across floors, walls, ceilings, corners and a physically valid one-tile opening. This roadmap breaks that gate into small implementation steps; it does not replace the GDD.

## Milestone 1 — Player Ant Movement Prototype

**Status:** In progress. The authored prototype, dedicated directional-input component, bounded floor/wall probes, attachment state, and isolated vertical wall movement are implemented. Manual Play Mode verification is pending. Ceilings, connected corners, and later P1 capabilities remain unimplemented.

**Goal:** Prove that one directly controlled ordinary ant can move through a small, authored 2D underground tunnel and that a simple camera can follow it. Becoming a queen is later progression and is outside this milestone.

### Scope

- One ordinary player ant in one small test scene.
- Directional WASD movement intent.
- Horizontal floor movement.
- Vertical wall climbing.
- Upside-down ceiling movement.
- Smooth transitions between connected floor, wall, and ceiling surfaces.
- Natural rotation so the ant aligns with the current surface.
- Configurable Auto Step-Over across a physically valid one-tile opening.
- Collision and whole-footprint clearance checks against terrain and blocking test obstacles.
- A simple 2D follow camera.
- Placeholder visuals are acceptable while movement is being proved.

### Explicitly out of scope

- Excavation and terrain modification.
- Food, water, rest, health, reproduction, or other survival systems.
- Multiple ants, control handover, autonomous tasks, route planning, traffic management, or deadlock recovery.
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

3. **Separate directional intent from reusable movement**
   - Interpret WASD as an intended world direction for the selected ant.
   - Keep direct-input interpretation separate from the physical surface motor so future autonomous navigation can submit movement intent without receiving different collision rules.
   - Do not implement selection, autonomous behaviour or pathfinding during this step.

   **Implementation status:** Implemented in code and prefab; Unity compilation passed. Manual Play Mode input verification remains pending.

4. **Add surface detection and alignment**
   - Detect nearby climbable surfaces with bounded 2D physics queries.
   - Derive the ant's outward direction from the chosen surface normal.
   - Rotate toward that orientation at a configurable rate rather than snapping instantly.
   - Validate enough free space for the ant's whole collider, not only its centre point.

   **Implementation status:** Floor and side-wall casts, contact point/normal state, attachment, gravity handling, and smooth motor-controlled alignment are implemented. Physics collisions still provide the current whole-collider barrier; dedicated route-clearance validation remains pending. Structural Editor validation passed, while Play Mode acceptance is pending.

5. **Support walls, ceilings and directional junction choice**
   - Move along the tangent of the selected surface instead of assuming world-horizontal movement.
   - Verify movement on isolated floor, wall and ceiling sections before testing corners.
   - At a junction, use the player's intended world direction to choose among reachable connected surfaces.
   - Ensure the ant does not drift away, tunnel through terrain or acquire an inaccessible surface.

   **Implementation status:** The isolated vertical-wall portion is implemented for both sides. Upward intent acquires a nearby wall; W/S then climb and descend, and no input maintains attachment. Ceiling movement and general directional junction choice are not implemented. Physical behaviour is not yet Play Mode verified.

6. **Support connected-surface transitions**
   - Add short forward and around-corner checks so the ant can acquire the next connected surface.
   - Tune attachment, query distance, rotation speed and collider size for smooth inside and outside corners.
   - Verify clockwise and counter-clockwise transitions across floor-to-wall, wall-to-ceiling, ceiling-to-wall and wall-to-floor joins.

7. **Add Auto Step-Over**
   - Add a configurable maximum crossing distance rather than hard-coding a tile count into movement logic.
   - Cross only when the opposite surface is reachable and the ant's entire swept footprint is clear.
   - Verify that a valid one-tile opening can be crossed continuously and that wider openings or crossings blocked by a test obstacle are rejected.
   - Do not add jumping, flying, teleportation or unrestricted falling.

8. **Add the camera follow**
   - Add one small camera-follow script with configurable smoothing and optional position offset.
   - Keep the camera upright; the ant's surface rotation must not rotate the view.
   - Verify that the ant stays visible through the complete tunnel route.

9. **Validate and document the milestone**
   - Run the complete route repeatedly in Play Mode in both directions.
   - Check the Console for errors and warnings.
   - Record Inspector values and any remaining limitations in `CurrentGameState.md`.
   - Mark the milestone complete only after all completion criteria have been observed in Unity.

### Completion criteria

- The player ant can traverse a continuous authored route containing floors, vertical walls, and ceilings in both directions.
- W, A, S and D express world-direction intent, and the player can deliberately choose a reachable surface at a junction.
- Surface transitions do not require teleporting, scene-specific trigger scripts, or manual Inspector changes during play.
- The ant remains aligned and attached without obvious jitter, tunnelling, or unintended detachment.
- A valid one-tile opening is crossed through configured Auto Step-Over, while wider or obstructed openings remain uncrossable without a connected surface route.
- Terrain and blocking obstacles are never bypassed, and clearance checks account for the ant's collider footprint.
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

- `AntSurfaceMotor.cs` — accepts a normalised `Vector2` movement intent, detects nearby floors and side walls, owns attachment/gravity, and moves or aligns the Rigidbody2D along the selected supported surface.
- `PlayerMovementInput.cs` — reads the existing `Move` action, normalises the result, and supplies it to `AntSurfaceMotor`. It contains no physics or autonomous navigation logic.
- `CameraFollow2D.cs` — follows a target position with simple smoothing while keeping the camera upright.

The existing root-level Unity template assets can remain where they are until the authored Ant Colony scene replaces the template scene. No Bootstrap exists in the current repository; Milestone 1 should use authored scene and prefab references and should not add a global manager or new bootstrap system.

## Later GDD stages

These stages are recorded from GDD version 0.2 for visibility. They are not approved implementation work while Milestone 1 remains incomplete.

| GDD stage | Deliverable | Status |
| --- | --- | --- |
| P2 — Tile excavation integration | Designate and remove accessible tiles, then update affected surfaces and connectivity at runtime. | Not started |
| P3 — Soil logistics and basic autonomous navigation | Let one autonomous ant follow a surface-aware route between work and deposit destinations, recover from invalid routes, and deliver exactly one soil unit per tile. | Not started |
| P4 — Multi-ant occupancy and control | Support multiple selectable ants, physical collision, single-width blocking, clearance-based passing, control handover and bounded traffic recovery. | Not started |
| P5 — Survival and growth | Add food, water, rest, the queen brood cycle, and skill-by-doing at small scale. | Not started |
| P6 — Finished slice | Add onboarding, a clear objective, win/lose feedback, save/load, and a usability pass. | Not started |

No later stage should begin until the current acceptance gate is verified and the developer approves the next stage.
