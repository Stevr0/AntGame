# Ant Colony Development Roadmap

## Roadmap status

This roadmap reflects the repository as inspected on 9 October 2026. Milestone 1 is in progress. The user has confirmed floor and isolated wall movement plus resolution of the reported corner deadlock in Play Mode. Ceiling/corner route acceptance and the newly implemented Auto Step-Over slice still await complete Play Mode acceptance.

`GameDesignDocument.md` version 0.4 is the primary design reference. Milestone 1 records the existing manually driven movement prototype as a regression baseline. The approved future foundation is a two-layer world model—one permanent Background Tile at every coordinate plus an optional foreground Solid Tile—combined with autonomous tile-adjacent navigation and Hybrid Task Management.

## Milestone 1 — Player Ant Movement Prototype

**Status:** In progress. Directional input, floor movement, isolated wall movement, and the corner-deadlock fix are Play Mode confirmed. Ceiling movement, intentional junction selection, the complete corner route, and bounded Auto Step-Over are implemented but not yet fully Play Mode accepted. Camera follow remains unimplemented.

**Goal:** Preserve and finish validating the existing directly controlled movement prototype as a technical baseline. This milestone does not define the final player control scheme. Becoming a queen is later progression and is outside this milestone.

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

   **Implementation status:** Implemented in code and prefab. Unity compilation passed, and the user has confirmed the directional floor/wall prototype in Play Mode.

4. **Add surface detection and alignment**
   - Detect nearby climbable surfaces with bounded 2D physics queries.
   - Derive the ant's outward direction from the chosen surface normal.
   - Rotate toward that orientation at a configurable rate rather than snapping instantly.
   - Validate enough free space for the ant's whole collider, not only its centre point.

   **Implementation status:** Implemented. Floor, side-wall, and ceiling casts expose contact point/normal state, attachment, gravity handling, and motor-controlled alignment. Floor and wall behavior is Play Mode confirmed by the user; ceiling behavior is pending acceptance.

5. **Support walls, ceilings and directional junction choice**
   - Move along the tangent of the selected surface instead of assuming world-horizontal movement.
   - Verify movement on isolated floor, wall and ceiling sections before testing corners.
   - At a junction, use the player's intended world direction to choose among reachable connected surfaces.
   - Ensure the ant does not drift away, tunnel through terrain or acquire an inaccessible surface.

   **Implementation status:** Implemented in code. Floor and wall movement is Play Mode confirmed. Ceiling A/D movement and intent-projected junction choice are implemented, including the rule that W alone at a wall/ceiling join does not choose a horizontal direction. Ceiling and junction behavior remains pending Play Mode acceptance.

6. **Support connected-surface transitions**
   - Add short forward and around-corner checks so the ant can acquire the next connected surface.
   - Tune attachment, query distance, rotation speed and collider size for smooth inside and outside corners.
   - Verify clockwise and counter-clockwise transitions across floor-to-wall, wall-to-ceiling, ceiling-to-wall and wall-to-floor joins.

   **Implementation status:** Implemented in code and authored test content. Live testing exposed a transition-state deadlock; the state exit, reversal, failed-step recovery, and unchanged-input retry defects are fixed. The focused Unity Edit Mode regression passed 1/1, while manual reproduction testing and wider corner acceptance remain pending.

7. **Add Auto Step-Over**
   - Add a configurable maximum crossing distance rather than hard-coding a tile count into movement logic.
   - Cross only when the opposite surface is reachable and the ant's entire swept footprint is clear.
   - Verify that a valid one-tile opening can be crossed continuously and that wider openings or crossings blocked by a test obstacle are rejected.
   - Do not add jumping, flying, teleportation or unrestricted falling.

   **Implementation status:** Implemented in the motor, player prefab, authored scene, setup validator, and focused regression harness. The motor measures openings relative to floor/wall/ceiling tangents, accepts only a configured one-tile parallel landing, prevalidates and continuously checks the whole collider path, prioritises explicit perpendicular input, pauses on release, reverses, recovers safely, and suppresses unchanged rejected retries. Automated Unity execution is pending because the local Licensing Client lost IPC during both command-line attempts; manual Play Mode acceptance is also pending.

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

- `AntSurfaceMotor.cs` — accepts a normalised `Vector2` movement intent, detects floors/walls/ceilings, selects connected surfaces from world-direction intent, and owns attachment, collision-checked corner motion, gravity, and Rigidbody2D alignment.
- `PlayerMovementInput.cs` — reads the existing `Move` action, normalises the result, and supplies it to `AntSurfaceMotor`. It contains no physics or autonomous navigation logic.
- `CameraFollow2D.cs` — follows a target position with simple smoothing while keeping the camera upright.

The existing root-level Unity template assets can remain where they are until the authored Ant Colony scene replaces the template scene. No Bootstrap exists in the current repository; Milestone 1 should use authored scene and prefab references and should not add a global manager or new bootstrap system.

## Milestone 2 — World Tiles and Autonomous Navigation

**Status:** Approved design; not started.

**Goal:** Establish the approved two-layer world representation, then prove that one ant can autonomously route and move through Exposed Space that is cardinally adjacent to Qualifying Geometry without entering Unsupported Space or requiring direct movement input.

Milestone 2 must retain `AntSurfaceMotor` and `PlayerMovementInput` as prototype and developer-test tools until the replacement movement is validated. Existing corner-following and Auto Step-Over behaviour are not authoritative navigation rules.

### 2.1 — World Tile Model and Navigation Visualisation

- Inspect and document the existing world representation before choosing an implementation.
- Establish the smallest representation for one permanent Background Tile per coordinate and zero or one foreground Solid Tile.
- Preserve Background Tiles when a coordinate is covered by a Solid Tile.
- Classify Covered, Exposed, Unsupported and Traversable Space.
- Visualise which cardinal Qualifying Geometry makes an Exposed Space navigation-eligible.
- Keep local traversability distinct from connectivity and Reachable Space.
- Confirm classification in the current prototype scene, including open sky, tunnels and unsupported chamber centres.
- Do not change existing movement behaviour.

**Acceptance:** Every inspected coordinate has one Background Tile and no more than one Solid Tile; visualisation consistently distinguishes Covered, Exposed, Unsupported and Traversable Space, rejects diagonal-only or Background-Tile-only support, and changes no ant movement.

### 2.2 — Autonomous Destination Navigation

- Provide a developer tool to assign one destination to one ant.
- Calculate a route through connected, clearance-valid navigation space.
- Follow route waypoints with continuous 2D movement and no direct movement input.
- Report invalid and unreachable destinations clearly and fail safely.

**Acceptance:** One ant reaches valid destinations on connected routes, while invalid or disconnected destinations produce clear feedback without teleportation, free flight or an invalid movement state.

### 2.3 — Movement and Clearance Validation

- Validate routes beside ground, vertical walls, ceilings, corners and overhangs.
- Validate excavated tunnels and large chambers using strict cardinal adjacency.
- Prevent unsupported-air traversal and diagonal corner-cutting.
- Validate every path segment for the full ant-body clearance, not only tile centres.
- Confirm invalid navigation states stop, recover or report failure safely.

**Acceptance:** The validation course proves the same movement model across all listed geometry and rejects every deliberately unsupported, obstructed or insufficient-clearance route.

### 2.4 — Dynamic Terrain and Navigation Updates

- Remove excavatable foreground Solid Tiles in a controlled test while keeping their Background Tiles unchanged.
- Update terrain collision after removal.
- Reclassify affected navigation regions after excavation or another terrain change.
- Update or invalidate routes whose traversability, connectivity or clearance changed.
- Prefer local, incremental updates over rebuilding the entire map.

**Acceptance:** Controlled excavation removes only the Solid Tile, retains the Background Tile, updates collision and affected navigation data, and causes an impacted ant to replan or report failure without using stale route segments.

## Subsequent milestones

These milestones are ordered direction, not approved implementation work. Their detailed acceptance criteria should be written only when each milestone is authorised.

| Milestone | Intended outcome | Status |
| --- | --- | --- |
| 3 — Ant selection and inspection | Select an individual and read its attributes, skills, health, needs, assigned task and current activity. | Not started |
| 4 — Hybrid task assignment | Support colony-wide designations and priorities plus specific task assignment to an eligible individual. | Not started |
| 5 — Multiple-ant movement | Allow shared-tile presence, local avoidance, temporary friendly overlap, congestion-aware routing and bounded recovery without invalid displacement. | Not started |
| 6 — Autonomous worker loop | Select and perform eligible work while responding predictably to urgent needs and task invalidation. | Not started |
| 7 — Excavation and soil transport | Excavate reachable foreground Solid Tiles, retain Background Tiles, carry configured removed-material resources and update navigation incrementally. | Not started |
| 8 — Population and performance | Scale toward potentially hundreds of independently simulated ants using shared navigation data, efficient nearby-ant lookup and controlled update frequencies. | Not started |
| 9 — Enemy colonies and hostile interactions | Reuse the same terrain navigation for other colonies and add high-level engage, retreat, avoid, defend and assistance responses. | Not started |
| 10 — Survival, growth and finished slice | Add needs, skill progression, later queen/brood progression, onboarding, objectives, save/load and usability in bounded slices. | Not started |

No new milestone is complete without implementation and validation evidence. Finish the remaining Milestone 1 acceptance work before implementation of Milestone 2.1 unless the developer explicitly changes that order. The two-layer tile architecture and autonomous navigation remain approved design, not implemented functionality.
