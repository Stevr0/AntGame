# Ant Colony Development Roadmap

## Roadmap status

This roadmap reflects the repository as inspected on 9 October 2026. Milestone 1 is in progress. The user has confirmed floor and isolated wall movement plus resolution of the reported corner deadlock in Play Mode. Ceiling/corner route acceptance and the newly implemented Auto Step-Over slice still await complete Play Mode acceptance.

`GameDesignDocument.md` version 3.2 is the primary design reference. Milestone 1 records the existing manually driven movement prototype as a regression baseline. The approved future foundation is a two-layer world model—one permanently present but replaceable Background Material at every coordinate plus an optional foreground Solid Tile—combined with Background-driven movement, directional navigation and Hybrid Task Management.

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

**Status:** In progress. Milestones 2.1B through 2.2B are complete and verified. Milestone 2.2B passed 155/155 automated tests and all 9 manual clearance checks. Milestone 2.2C is implemented and passes automated and structural validation; graphics-enabled manual Scene-view acceptance remains pending.

**Goal:** Establish the approved two-layer world representation, validate physical body clearance independently, derive Background-based walking/gripping/falling capability, build directional connections, then prove that one ant can move autonomously without direct movement input.

Milestone 2 must retain `AntSurfaceMotor` and `PlayerMovementInput` as prototype and developer-test tools until the replacement movement is validated. Existing corner-following and Auto Step-Over behaviour are not authoritative navigation rules.

### 2.1 — World Tile Model and Navigation Visualisation

- **2.1A — Architecture audit:** Completed. The existing authored `DirtBlock` collider course remains the movement prototype and is not authoritative tile data.
- **2.1B — Chunk data foundation:** Complete. A scene-independent sparse dictionary owns explicitly created 32×32 chunks. Each resident chunk contains 1,024 Background Tile IDs and zero or one Solid Tile ID per cell. Coordinate conversion supports positive and negative chunk positions, and missing chunks return an unavailable status without implicit generation.
- Stable ScriptableObject material definitions now provide the initial `background.default` and `solid.soil` IDs. A stored world seed is reserved for later deterministic generation but causes no generation itself.
- The Milestone 2.1B Unity Edit Mode suite passed 25/25, and the existing ant movement remained functional in the user's Play Mode verification.
- **2.1C — Navigation classification:** Complete. Direct, uncached queries over authoritative world data distinguish Unavailable, Covered, and Exposed occupancy, while exposed cells report Supported, Unsupported, or Unknown cardinal support. Unknown prevents missing adjacent chunks from being mistaken for definitively unsupported terrain. The user-confirmed acceptance run passed 50/50 Edit Mode tests.
- **2.1D — Navigation visualisation:** Complete. The separate `AntNavigationPrototype` developer scene uses a deterministic eight-chunk fixture and one bounded batched mesh to display the actual classifier's Covered, Supported, Unsupported, Unknown, and Unavailable results. The automated suite passed 61/61 and the user confirmed all 11 manual visual acceptance checks plus unchanged original ant movement.
- **2.1E — World Tilemap integration:** Complete. Separate Background and foreground Solid Tilemaps project explicitly requested resident chunks from `ChunkedWorldData` through registered material definitions. Explicit cell, chunk, and bounded-region refreshes preserve Background visuals during Solid mutation, diagnose missing visuals safely, align signed cells through an explicit half-cell Grid offset, and add no collision components.
- The 2.1E Edit Mode suite passed 89/89, including 28 Tilemap projection cases, the expanded scene validator passed, and the user confirmed all 10 manual visual checks.
- The existing Supported classification remains a useful foreground-adjacency diagnostic, not the complete movement-capability rule; body clearance, Background permissions, grip, grounding, gravity, directional connectivity and Reachable Space are separate work.
- Preserve Background Tiles when a coordinate is covered by a Solid Tile.
- Visualise which cardinal Qualifying Geometry makes an Exposed Space navigation-eligible.
- Keep local traversability distinct from connectivity and Reachable Space.
- Confirm classification in the current prototype scene, including open sky, tunnels and unsupported chamber centres.
- Do not change existing movement behaviour.

**Acceptance:** Every inspected resident coordinate has one Background Tile and no more than one Solid Tile; visualisation consistently distinguishes Covered, Supported, Unsupported, Unknown, and Unavailable cells, rejects diagonal-only or Background-Tile-only support, and changes no ant movement. Supported does not imply clearance, connectivity, or reachability.

### 2.2 — Clearance, Movement Capability and Autonomous Navigation

- **2.2A — Authoritative terrain collision:** Complete. The dedicated navigation scene has a separate invisible collision Tilemap derived from authoritative Solid Material `BlocksMovement` flags. Background and visual foreground Tilemaps remain collider-free, while `SupportsNavigation` stays independent. One Tilemap collider, static Rigidbody2D and manually refreshed Composite collider merge adjacent blocking cells on `ClimbableSurface` without per-tile GameObjects.
- Explicit cell, chunk and bounded-region collision refreshes process pending Tilemap changes and synchronize Physics2D before validation. The deterministic fixture adds supporting/non-blocking and blocking/non-supporting examples alongside its existing terrain and signed chunk boundaries.
- Unity compilation and the expanded scene/material/collider validator passed. The Edit Mode suite passed 117/117, including 28 Physics2D collision cases, and the user confirmed all 12 manual collision checks.
- **2.2B — Physical ant-body clearance:** Complete. The full rotated-capsule query reads authoritative `BlocksMovement` data only and is independent of support, Background preparation, grip, grounding, gravity and reachability. Open Sky and a smooth support-free shaft may be physically Clear. Compilation, 38/38 focused tests, structural scene validation, 2/2 movement regression tests and the full 155/155 Edit Mode suite passed; the user confirmed all 9 manual clearance checks.
- **2.2C — Background movement capability and gravity eligibility:** In progress, pending manual acceptance. Background Materials explicitly define voluntary directions, completed grip and unsupported gravity. A read-only evaluator reports Grounded, Gripping or Falling while keeping physical clearance separate. Grounding uses a small whole-capsule bottom-contact test against authoritative blocking/supporting Solid top faces; Background replacement updates capability immediately without changing foreground occupancy. The 37/37 focused suite, expanded scene validator, 2/2 movement regressions and full 192/192 Edit Mode suite pass.
- **2.2D — Directional navigation connections:** Not started. Combine full-body clearance with Background permissions, grounding, grip, falling, missing-world handling and applicable obstacles. Connections may be one-way—for example, falling down an unprepared shaft without a return climb—and must reject invalid diagonal corner cutting.
- **2.2E — Autonomous destination movement:** Not started. Provide a developer destination tool, route through valid directed connections, follow waypoints continuously without direct input, and report invalid or unreachable destinations safely.

**Acceptance:** 2.2B reports physical fit independently of grip; 2.2C reports movement/gravity state; 2.2D produces safe directional connections; and 2.2E moves one ant to valid destinations without teleportation, free flight or an invalid movement state.

### 2.3 — Integrated Movement Validation

- Validate routes beside ground, vertical walls, ceilings, corners and overhangs.
- Validate excavated tunnels, prepared climbing regions, unprepared shafts and large chambers using Background capability, grounding, grip and falling rules.
- Prevent invalid voluntary movement and diagonal corner-cutting while permitting valid directional falling.
- Validate every path segment for the full ant-body clearance, not only tile centres.
- Confirm invalid navigation states stop, recover or report failure safely.

**Acceptance:** The validation course proves the same movement model across all listed geometry, permits intended directional falling, and rejects every invalid voluntary, obstructed or insufficient-clearance route.

### 2.4 — Dynamic Terrain and Navigation Updates

- Remove excavatable foreground Solid Tiles in a controlled test while keeping their Background Tiles unchanged.
- Update terrain collision after removal.
- Replace a designated exposed Background Material only after a preparation job completes.
- Reevaluate clearance, movement capability and directional navigation after excavation, preparation or another terrain change.
- Update or invalidate routes whose traversability, connectivity or clearance changed.
- Prefer local, incremental updates over rebuilding the entire map.

**Acceptance:** Controlled excavation removes only the Solid Tile and retains the Background; completed preparation replaces only the Background Material. Both operations update their affected collision/capability/navigation data and cause an impacted ant to replan or report failure without stale route segments.

## Subsequent milestones

These milestones are ordered direction, not approved implementation work. Their detailed acceptance criteria should be written only when each milestone is authorised.

| Milestone | Intended outcome | Status |
| --- | --- | --- |
| 3 — Ant selection and inspection | Select an individual and read its attributes, skills, health, needs, assigned task and current activity. | Not started |
| 4 — Hybrid task assignment | Support colony-wide designations and priorities plus specific task assignment to an eligible individual. | Not started |
| 5 — Multiple-ant movement | Allow shared-tile presence, local avoidance, temporary friendly overlap, congestion-aware routing and bounded recovery without invalid displacement. | Not started |
| 6 — Autonomous worker loop | Select and perform eligible work while responding predictably to urgent needs and task invalidation. | Not started |
| 7 — Excavation, Surface Preparation and resources | Excavate reachable foreground Solids, retain their Backgrounds, prepare exposed Background Materials progressively from accessible positions, and reserve/haul/consume configured resources. | Not started |
| 8 — Population and performance | Scale toward potentially hundreds of independently simulated ants using shared navigation data, efficient nearby-ant lookup and controlled update frequencies. | Not started |
| 9 — Enemy colonies and hostile interactions | Reuse the same terrain navigation for other colonies and add high-level engage, retreat, avoid, defend and assistance responses. | Not started |
| 10 — Skills, survival, growth and finished slice | Add universal 0–100 skill progression with inactivity decay above 50, needs, later queen/brood progression, onboarding, objectives, save/load and usability in bounded slices. | Not started |

The proposed carrying model remains future work: one initial slot plus one per 20 Hauling skill, capped at five, with one item per slot.

No new milestone is complete without implementation and validation evidence. Milestone 2.1B passed 25/25 focused Unity Edit Mode tests and is complete. Milestone 2.1C passed the user-confirmed 50/50 acceptance run and is complete. Milestone 2.1D passed 61/61 plus all 11 manual visual checks and is complete. Milestone 2.1E passed 89/89 plus all 10 manual visual checks and is complete. Milestone 2.2A passed 117/117 plus all 12 manual collision checks and is complete. Milestone 2.2B passed 155/155 plus all 9 manual clearance checks and is complete. Milestone 2.2C passes 37/37 focused capability tests, the structural validator, 2/2 movement regressions and all 192/192 Edit Mode tests; it remains in progress because graphics-enabled manual acceptance is pending. Directional connections, autonomous movement, excavation jobs, Surface Preparation, resources, skills, carrying, persistence and streaming are not implemented.
