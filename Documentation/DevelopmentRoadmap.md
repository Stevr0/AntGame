# Ant Colony Development Roadmap

## Current accepted milestone and experimental next step — 12 October 2026

**Revised 2.4B Grid Navigation Proof of Concept: Complete.** A separate ordinary-ant experiment derives grippable and grounded anchors from authoritative world data, caches whole-body-safe eight-direction edges with diagonal restrictions, searches them with movement-time-weighted deterministic A*, and interpolates smoothly between anchors. Stable traversal excludes the experimental bodies from Physics2D; only Falling enables Rigidbody2D gravity, followed by controlled anchor recovery. Local terrain edits invalidate a bounded cache neighbourhood. Moving agents, local avoidance and enemies are not baked into static terrain navigation. The production 2.4A motor remains intact for comparison. The user confirmed manual Unity Play Mode acceptance after the automated results below.

Automated evidence: 11/11 focused grid EditMode, 238/238 full EditMode, 3/3 focused grid PlayMode and 9/9 full PlayMode passed. The 500-agent representative batch recorded 645.436 ms for 500 independent uncached A* searches, 24.277 ms to simulate all complete smooth routes, 1.086 ms for 100 Physics2D steps with stable bodies excluded, 1,875,968 retained managed bytes, 250,500 examined nodes and 500/500 correct arrivals. This validated the execution model but also made path-request budgeting and common-route reuse a prerequisite for a production hundreds-of-ants milestone. The user later completed manual Play Mode acceptance; this proof of concept still does not replace production movement.

**2.4A Natural Background Movement Realignment: COMPLETE.** User reports Roslyn compilation, focused 56/56 Edit Mode, full graphics-enabled 227/227 Edit Mode, 6/6 physics Play Mode and scene validators passed. User has confirmed the manual Unity Play Mode acceptance. Natural earth/stone grip works without preparation; prepared terrain increases movement speed by provisional 1.25×; sky remains non-grippable. Older milestones remain historical. This status is based on user-reported local work and validation; GitHub may need the associated Unity source/tests pushed.

**Current approved scope: 2.4C Grid Navigation Stabilisation and Multi-Ant Readiness.** Add central bounded incremental path-request scheduling, deterministic priority with starvation prevention, stale-result protection, bounded movement recovery, and development-only multi-ant stress controls. Preserve the accepted shared terrain map, 2.4A regression motor and 2.4B behaviour. **Do not add colony jobs, inventories, excavation gameplay, queen/brood, drafting, save/streaming or multiplayer.**

**2.4C implementation status: Implemented — Pending Manual Acceptance.** A shared main-thread scheduler now advances deterministic resumable A* with a configurable 128-node/update total budget and 8-node/request slice. Its weighted High/Normal/Low service cycle prevents starvation; duplicate suppression, replacement/cancellation, terrain revisions and request identity prevent stale delivery. The experimental motor now has explicit waiting, following, blocked, falling and recovery lifecycle states with two bounded automatic retries. The development scene can spawn 1/100/300/500 grid ants, exercise simultaneous or staggered requests, cancel one request, mutate terrain and display scheduler diagnostics. Friendly overlap is intentional; stable grid ants do not pay Physics2D contact cost, and the accepted 2.4A ant is retained.

Unity Bee response-file Roslyn compilation passed for the World, Development, EditMode-test and PlayMode-test assemblies. Focused scheduler EditMode tests passed 11/11. Focused PlayMode tests independently passed shared three-ant request ownership/cancellation, active-route mutation/replan, and falling interruption/recovery. A combined focused run passed four tests before the mutation test exceeded its original 20-second test-only bound; that test passed with a 35-second bound. Later combined/full reruns were blocked when Unity repeatedly lost its Licensing Client and crashed before emitting results, so no post-fix combined result is claimed. Deterministic 1/100/300/500 request batches completed with 1/100/300/500 successes and zero failures. At 500 requests, total batch time was 187.776 ms, accumulated scheduler work 171.911 ms, maximum scheduler update 3.197 ms, average/worst completion latency 176.614/219 updates, total expansions 25,500 over 219 updates, peak queue 500, retained managed-memory lower bound 1,118,208 bytes, and 100 disabled-body Physics2D steps cost 1.143 ms. These editor measurements validate bounded scheduling, not production frame rate. Full results and the exact 13-step manual checklist are recorded in `CurrentGameState.md`.


**Construction/resource addendum (12 October 2026):** New approved GDD rules require item stacks, slot-only carrying, excavator auto-pickup with safe drop fallback, Material Storage functional tiles, physical resource reservations, and **builder self-supply** before door/background conversion. First architecture audit must evaluate resource ownership, multi-trip supply, cancellation recovery, shared stockpiles and resource conservation. Numerical recipes and stack sizes remain provisional. Do not start implementation without approval.

**Job/priority design addendum (12 October 2026):** Per-ant 1–4/Disabled matrices; priority before closest reachable job; direct control plus specific automatic orders; critical survival overrides ordinary work with safe-interruption handling; persistent valid partial progress; skills improve efficiency but never silently outrank settings. Read-only audit must cover job dispatch, path cost, exclusive reservations, handoff, saved state and construction builder self-supply. Detailed thresholds and job categories remain provisional. No implementation authorised.

**Control design amendment — 12 October 2026:** Approved normal-play controls are now RimWorld-style command-based: autonomous ants, temporary explicit ordered work (automatic return to autonomy), persistent tactical Draft Mode for single/multi-ant movement, defence and attack. Continuous WASD steering is developer-only prototype functionality, not an intended gameplay feature. Reinterpret former manual productivity bonus as modest nonstacking ordered-task work-speed bonus, percentage TBD. In the read-only audit, flag player/AI locomotion separation, order lifecycle, reservation handoffs, draft-state persistence, multi-ant route contention and UI implications. No Unity implementation is approved by this amendment.

## Design realignment notice — 12 October 2026

The authoritative design is the **Authoritative design realignment** section in `Documentation/GameDesignDocument.md`. Existing implementation milestones below may retain historical terminology. Milestone 2.3B was manually accepted under its previous rules; Milestone 2.4A implements the revised natural-background rules and has been manually accepted.

**Next milestone: documentation-led, read-only design/architecture audit** (no code changes). Inspect all Unity systems, compare each to the new design, classify Keep/Refactor/Replace/Defer, identify migration sequencing and tests affected, and propose a smallest safe vertical slice. Proposed future slices (not authorised): shared ant/player/AI control; natural-grip movement correction; functional tile conversion + physical material logistics; room/door detection; individual needs and Rested; founding queen nursery/brood/first workers; autonomous jobs and persistence. Do not mark an unimplemented design feature complete.

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

The existing root-level Unity template assets can remain where they are until the authored Ant Colony scene replaces the template scene. Milestone 1 itself has no Bootstrap. The later integrated development scene uses a small authored world coordinator while preserving the rule against a global manager or replacement authority.

## Milestone 2 — World Tiles and Autonomous Navigation

**Status:** In progress. Milestones through revised 2.4B retain their recorded automated/manual evidence. Milestone 2.4C is **Implemented — Pending Manual Acceptance**; a combined regression rerun remains blocked by Unity licensing as recorded above.

**Goal:** Establish the approved two-layer world representation, validate physical body clearance independently, derive Background-based walking/gripping/falling capability, build directional connections, then prove that one ant can move autonomously without direct movement input.

Milestone 2 must retain `AntSurfaceMotor` and `PlayerMovementInput` as prototype and developer-test tools until the replacement movement is validated. Existing corner-following and Auto Step-Over behaviour are not authoritative navigation rules.

### 2.1 — World Tile Model and Navigation Visualisation

- **2.1A — Architecture audit:** Completed. The existing authored `DirtBlock` collider course remains the movement prototype and is not authoritative tile data.
- **2.1B — Chunk data foundation:** Complete. A scene-independent sparse dictionary owns explicitly created 32×32 chunks. Each resident chunk contains 1,024 Background Tile IDs and zero or one Solid Tile ID per cell. Coordinate conversion supports positive and negative chunk positions, and missing chunks return an unavailable status without implicit generation.
- Stable ScriptableObject material definitions provide the initial `background.default` and `solid.soil` IDs. The 2.1B data object merely stores a seed and never generates on query; the later integrated checkpoint invokes its bounded generator explicitly.
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
- **Integrated sample-world checkpoint — Complete:** `AntGameDevelopment.unity` provides one authored Play Mode environment over a deterministic 4×4-chunk, 128×128-cell world. It reuses authoritative world data, Background/Solid materials, visual Tilemaps, collision-only Tilemap, clearance and movement-capability services. The user confirmed terrain readability, camera controls, inspection, foreground mutation, prepared-Background replacement and the corrected brown-to-green-to-brown rendering round trip in Play Mode. This acceptance does not mark the separate Milestone 2.2C Scene-view checklist complete.
- The older movement and navigation scenes remain available until a separate cleanup milestone can verify they are no longer needed.
- **2.2D — Directional navigation connections:** Not started. Combine full-body clearance with Background permissions, grounding, grip, falling, missing-world handling and applicable obstacles. Connections may be one-way through explicitly non-grippable space and must reject invalid diagonal corner cutting.
- **2.2E — General autonomous destination navigation:** Not started. Extend beyond the bounded 2.3A/2.3B direct segments by routing through valid directed connections, following waypoints continuously and reporting arbitrary invalid or unreachable destinations safely.

**Acceptance:** 2.2B reports physical fit independently of grip; 2.2C reports movement/gravity state; 2.2D produces safe directional connections; and 2.2E moves one ant to valid destinations without teleportation, free flight or an invalid movement state.

### 2.3 — Integrated Movement Validation

- **2.3A — First autonomous ant, ground movement:** Complete. `AntGameDevelopment.unity` spawns exactly one visually distinct ordinary ant only after a bounded nearby pose passes whole-body clearance and Grounded eligibility. The original direct-corridor slice revalidated every immediate grounded step and stopped on new blockers or lost support without input, teleportation or route search. Structural validation, 221/221 Edit Mode tests, 3/3 physics-backed Play Mode tests and all 11 user-confirmed manual checks passed.
- **2.3B — Background grip and gravity movement:** Complete under its historical prepared-grip rules. The user reported its automated run and confirmed all 12 manual checks. Milestone 2.4A supersedes only the prepared-only access assumption while preserving the proven locomotion states, collision handling and reusable motor.
- 2.3A and 2.3B are bounded observable precursors, not the general 2.2D/2.2E navigation solution. Full directional connection generation, waypoints, routed wall/ceiling traversal and route search remain future work.

- Validate routes beside ground, vertical walls, ceilings, corners and overhangs.
- Validate natural and prepared climbing regions plus explicitly non-grippable Sky/void fixtures using Background capability, grounding, grip and falling rules.
- Prevent invalid voluntary movement and diagonal corner-cutting while permitting valid directional falling.
- Validate every path segment for the full ant-body clearance, not only tile centres.
- Confirm invalid navigation states stop, recover or report failure safely.

**Acceptance:** The validation course proves the same movement model across all listed geometry, permits intended directional falling, and rejects every invalid voluntary, obstructed or insufficient-clearance route.

### 2.4 — Dynamic Terrain and Navigation Updates

- **2.4A — Natural Background Movement Realignment:** **Complete.** Natural Earth and Stone provide horizontal, vertical and diagonal grip at normal speed. Prepared Background provides identical access plus a data-driven provisional `1.25x` speed multiplier; Sky remains non-grippable. The existing capability evaluator, bounded route validator, autonomous motor, Rigidbody2D collision and Tilemap projections are reused. Runtime foreground/Background mutations re-evaluate locomotion and invalidate affected active direct routes. Focused EditMode tests passed 56/56, the full graphics-enabled EditMode suite passed 227/227, PlayMode tests passed 6/6, and the user confirmed manual acceptance.
- **2.4B — Grid Navigation Proof of Concept:** **Complete.** The experiment is separate from production 2.4A and limited to one ordinary-ant profile. Shared cached anchors/connections, whole-body clearance, eight-direction corner rules, travel-time A*, smooth anchor following, local invalidation and physics-only Falling are implemented. Deterministic fixtures and automated suites passed as recorded above, and the user confirmed manual Play Mode acceptance. The benchmark supports low stable-movement/physics overhead but rejects an unbudgeted 500-search frame as production-ready.
- **2.4C — Grid Navigation Stabilisation and Multi-Ant Readiness:** **Implemented — Pending Manual Acceptance.** Shared incremental scheduling, fair generic priorities, request cancellation/replacement, terrain-revision safety, explicit route/fall recovery, bounded retries, shared-map stress groups and diagnostics are implemented. Focused new automation passes; repeat the combined suites when Unity licensing is available and complete the manual checklist in `CurrentGameState.md`. Friendly overlap is allowed and colony gameplay remains excluded.

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

No new milestone is complete without implementation and validation evidence. Milestone 2.4C remains pending until the combined suites can be rerun and the user confirms manual acceptance. Excavation jobs, Surface Preparation, resources, skills, carrying, persistence, streaming and local avoidance are not implemented.
