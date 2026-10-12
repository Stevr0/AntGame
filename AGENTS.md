# AGENTS.md — Ant Colony

These instructions apply to the entire repository. They are intended for Codex and other coding assistants contributing to this Unity project.

## Project and source of truth

- **Game:** Ant Colony — a small, finishable, single-player 2D side-view ant colony simulation.
- **Engine:** Unity 6; **language:** C#; **initial target:** Windows PC.
- Read `Documentation/GameDesignDocument.md` before planning gameplay changes. It describes the design intent, not necessarily implemented features.
- Read `Documentation/CurrentGameState.md` and `Documentation/DevelopmentRoadmap.md` when present. Inspect the actual repository to establish what is implemented.
- If documents disagree with working code or each other, report the discrepancy rather than silently guessing or rewriting the GDD.
- Do not treat a planned feature as complete without verification in Unity.

## Approved 12 October 2026 design overrides (authoritative)

Read the **Authoritative design realignment** section near the top of `Documentation/GameDesignDocument.md` before any implementation or planning. It supersedes conflicting older AGENTS.md passages and old roadmap/GDD history. Key corrections: a working founding queen starts the game; **normal player-facing direct ant control is required** alongside automation; natural exposed backgrounds are grippable without preparation; preparation boosts travel speed; functional background tiles (nursery, sleeping, food, water, production, burial) require resource-consuming Construction conversion jobs and supply 1:1 capacity; all openings of an enclosed room have doors; mixed-use is allowed, enclosed specialised rooms gain quality bonuses; first authorised brood is three larvae requiring repeated feeding; shared food/water stores, individual depletion, Rested decay and permanent ant deaths are approved. Never describe these as implemented until tested.

**Current authorisation is documentation-only.** Do not implement gameplay, edit Unity assets or change tests based on this design update without a separately approved milestone. The previously accepted 2.3B movement tests remain historical validation, not validation of the revised movement rules. Any contradictory older instruction below is historical and is overridden by this section.

## Game vision (not an implementation checklist)

- The player begins as an ordinary ant. Becoming a queen is a future progression event whose requirements remain unresolved.
- Each ant is selectable and inspectable, with distinct attributes, learned skills, health, hunger, hydration and energy.
- Ants autonomously perform work selected from colony priorities or specifically assigned to them while responding to urgent needs.
- Ants move continuously through supported tile-adjacent space in a 2D side-view environment; normal play does not directly steer them.
- The player marks soil Solid Tiles for excavation. Assigned ants find reachable designations, excavate the foreground material, carry away loose soil and deposit it at designated locations.
- Skills improve through doing relevant work. Later features may include soil placement and tunnel sealing.
- **Do not implement these future systems until their milestone is explicitly approved.**

## Current development boundary

**Initial milestone: Player Ant Movement Prototype.** Unless the user explicitly changes the approved milestone, work only toward:

1. A simple 2D test scene with one ordinary player ant and an already-open tunnel.
2. Movement on floors, walls and ceilings, including smooth transitions and surface-aligned rotation.
3. Directional WASD input, intentional surface choice at junctions, and physically valid Auto Step-Over across a suitable one-tile opening.
4. A basic follow camera.
5. A reproducible in-Editor test of the above.

**Not yet in scope:** digging, dynamic terrain editing, automatic pathfinding, worker AI, ant needs, skill progression, brood, reproduction, food/water logistics, multiplayer or networking.

The movement prototype is a regression baseline, not the final player-facing control model. Once Milestone 1 is fully accepted, the next planned milestone is World Tiles and Autonomous Navigation, beginning with the two-layer tile model and classification visualisation without altering movement. Do not begin that implementation without explicit approval.

For the *first* Codex session: inspect the repository, describe the minimal structure and propose the smallest implementation plan **before writing gameplay code**. Wait for approval to implement that plan. In later sessions, carry out the specifically approved task without needlessly re-requesting permission for every small edit.

## Scope control

- Make the smallest change that achieves the current task and has a clear acceptance test.
- Do not add unrelated mechanics, speculative infrastructure, generic frameworks, complex inheritance trees, unnecessary interfaces or new packages.
- Do not install Unity NGO or any networking solution; this game is single-player.
- Prefer built-in Unity functionality and straightforward, modular components.
- Avoid refactoring working unrelated systems. Preserve existing scenes, prefabs and serialized Inspector references.
- Explain any proposed architectural change, dependency or scope expansion and seek approval before proceeding.
- Treat the GDD's future ideas as a backlog, **not** a request to build them now.

## C# coding practices

- Write readable, beginner-friendly C# using descriptive names and small, focused methods.
- Follow conventional C# naming: `PascalCase` for types/methods/properties, `camelCase` for locals/parameters, and `_camelCase` for private fields.
- Prefer `[SerializeField] private` for Inspector-exposed configuration; avoid making fields public only for Inspector access.
- Add detailed, useful comments explaining *why* non-obvious logic exists and how Unity-specific behavior works. Include XML summaries for public types and nontrivial public APIs. Do not obscure code with redundant line-by-line narration.
- Validate references and boundary conditions. Avoid allocations in hot per-frame loops where straightforward.
- Distinguish Unity lifecycle methods (`Awake`, `Start`, `Update`, `FixedUpdate`) and use them intentionally.
- Keep direct-input handling separate from movement mechanics when practical so autonomous ants can reuse movement later; do not build the autonomous system now.
- Never introduce editor-only dependencies into runtime assemblies.

## Unity assets and repository hygiene

- Preserve Unity `.meta` files and GUIDs. Do not rename or move assets casually.
- Do not commit generated folders such as `Library/`, `Temp/`, `Obj/`, `Logs/`, `Build/` or `Builds/`.
- Avoid manual edits to Unity scene/prefab YAML unless necessary and safe; clearly flag any setup that must be completed in the Unity Editor.
- When Editor work is needed, provide exact GameObject names, components, layers, Inspector values and references.
- Do not claim that Unity Editor play-mode checks ran unless they actually ran.
- Keep commits and change sets focused; describe all relevant files changed.

## Validation and acceptance

For each implementation task:

1. Define the observable success criteria before editing.
2. Make the smallest coherent changes.
3. Run available checks relevant to the repository (for example, a compile/test command when configured).
4. State what was actually checked versus what still needs to be tested manually in the Unity Editor.
5. Provide a brief step-by-step Editor test procedure and expected results.
6. Report known limitations and regressions. Never fabricate test results.

For player-ant movement, check directional input, floor traversal, climbing both sides of a tunnel, ceiling traversal, corners, reversal, Auto Step-Over and loss-of-surface behaviour. The ant should not spontaneously fall, teleport, overlap blockers or oscillate at corners under expected conditions.

## Movement and navigation conventions

- Normal gameplay uses autonomous movement; `PlayerMovementInput` remains a developer-only testing tool and gives no traffic priority.
- Every coordinate in a generated 32×32 chunk has one permanent Background Tile and may have zero or one foreground Solid Tile. Ungenerated chunks are unavailable, not exposed. Excavation removes only an excavatable Solid Tile; it never removes its Background Tile.
- Background Tiles do not inherently block movement or provide navigation support. Solid Tiles normally block movement and provide support unless their configuration explicitly says otherwise.
- Navigation classification must distinguish a missing target chunk from exposed terrain. An exposed cell with no confirmed cardinal support remains unknown when a required neighbouring chunk is unavailable; it is Unsupported only after all cardinal neighbours are available and evaluated.
- Use Covered Space, Exposed Space, Traversable Space, Unsupported Space, Reachable Space and Qualifying Geometry consistently as defined in the GDD; avoid describing excavated coordinates as empty tiles.
- Use one tile-adjacent terrain-navigation model for all colonies. Keep world tile data, excavation, task choice, shared navigation data, per-ant route following, movement execution, local avoidance and physics/collision separate where practical.
- An ordinary ant has a nominal footprint of approximately one tile, but movement remains smooth and continuous rather than grid-snapped.
- Potentially Traversable Space requires Qualifying Geometry in a cardinal direction and adequate whole-body clearance. Diagonal adjacency alone does not qualify; traversability and reachability remain distinct.
- Eight-direction route connections are allowed, but full path segments must remain supported and cannot cut diagonally through blocked corners.
- A surface gap cannot be crossed through unsupported space. Existing Auto Step-Over behaviour is prototype-only and is not an exception.
- Multiple ants may share a tile. Use local avoidance and hybrid separation; temporary friendly overlap is acceptable to prevent deadlock, but avoidance must never push an ant into terrain or unsupported space.
- Dynamic excavation must invalidate affected shared navigation data at runtime. Prefer local or event-driven updates over unconditional full-world recalculation.
- Design for potentially hundreds of ants using shared data, efficient nearby-ant lookup and controlled update rates. Do not add DOTS/ECS or another major dependency before profiling shows a need.

## Documentation maintenance

- `Documentation/GameDesignDocument.md`: authoritative design intent. Do not rewrite design decisions without the user's approval.
- `Documentation/DevelopmentRoadmap.md`: milestone order and explicit acceptance criteria. Create only if missing and needed for the approved task.
- `Documentation/CurrentGameState.md`: what is implemented, verified, pending Editor setup, broken and next. Update after meaningful implementation changes; distinguish **implemented** from **verified**.
- Keep documentation concise, accurate and in Markdown suitable for GitHub.
- If a referenced document is absent, say so and propose its creation; do not assume its contents.

## Response format for coding tasks

When delivering changes, summarize:

1. **What changed** — gameplay behavior and files affected.
2. **Why** — brief, plain-English explanation for a developer learning C#.
3. **Unity Editor setup** — exact steps, if any.
4. **Verification** — checks performed, their results and manual checks still required.
5. **Next smallest step** — within the approved milestone.

Keep explanations practical. Do not launch into a new milestone or implement extra features without authorization.
