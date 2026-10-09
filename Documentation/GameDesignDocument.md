# ANT COLONY

**Game Design Document  |  Version 0.4**

Working title • Unity 6 • Single-player • 2D side-view • PC-first prototype

Design intent: Build a small, finishable colony simulation in which individual ants matter. The player directs excavation and assigns work; ants physically perform tasks and develop through experience.

## 1. Game at a glance

| Item | Definition |
| --- | --- |
| Genre | 2D side-view colony simulation / management |
| Starting scenario | The player begins as an ordinary ant. Becoming a queen is a later progression event whose requirements are still to be designed. |
| Primary interaction | Select ants, designate jobs and mark soil Solid Tiles for excavation. |
| Defining feature | Each ant is a persistent, selectable individual with its own needs, stats, skills and autonomous behaviour. |
| Target | Small single-player game; initial colony target of up to approximately 20 ants. |
| Status | Core movement and navigation rules agreed; balance, queen transformation and later progression details remain provisional. |

## 2. Experience pillars

- A colony built physically: tunnels and chambers exist because ants excavate and move real soil.
- Individual ants, not disposable units: skills improve through doing; health, hydration, food and energy influence productivity.
- Colony management with individual visibility: the player directs work globally or assigns a specific task, while ants navigate and act autonomously.
- Readable, satisfying simulation: worker behaviour is observable and the player understands why an ant is idle or struggling.
- Finishability first: implement and polish one complete play loop before adding broad simulation features.

## 3. Player fantasy and game loop

The player begins with a single ordinary ant and gradually develops a functioning colony. Becoming a queen is a possible later progression event whose requirements are still to be designed; it is not part of the initial gameplay. The player manages the ant and later colony through selection, inspection, designations, priorities and task assignments rather than direct movement control.

Core loop: Mark an accessible soil Solid Tile → an assigned ant travels to it → excavates the foreground material → collects a soil unit → carries it to a dump location → returns for another job. As the colony grows, workers also gather food and water, rest, and gain skills.

## 4. World tiles, movement and navigation

The game uses a 2D side-view grid divided into sparse 32×32 chunks. A generated, resident chunk contains one permanent **Background Tile** at every 1×1 coordinate and may additionally contain one foreground **Solid Tile** at each coordinate. Chunks can be created at positive or negative coordinates, so the world can expand in all four directions without allocating one enormous rectangular array. Coordinates in chunks that have not been generated are unavailable; they are not silently treated as exposed terrain.

The data-only chunk model is implemented independently of the current authored `DirtBlock` course. It does not yet generate, stream, save, render, classify, or collide tiles, and it does not import the prototype terrain.

### 4.1 Two-layer world model

#### Background Tile layer

- Every coordinate in a generated chunk contains exactly one Background Tile.
- Background Tiles are permanent, cannot be excavated or collected, and remain beneath Solid Tiles.
- Removing a Solid Tile exposes the Background Tile without changing or destroying it.
- Background Tiles may hold visual material, biome, depth, temperature, moisture, environmental modifiers and decorative information.
- Those properties are future capabilities, not a requirement to implement a complete environmental simulation now.
- A Background Tile does not inherently block movement or qualify as navigation-supporting geometry.

#### Foreground Solid Tile layer

A coordinate contains zero or one foreground Solid Tile. Solid Tiles normally block movement and provide qualifying geometry for adjacent navigation. Exceptions must be explicit properties of the material or object configuration.

Possible Solid Tile materials include soil, sand, clay, stone, rock and later excavatable or non-excavatable materials. Material data may define hardness, durability, excavation requirements, whether it is excavatable, resource yield, movement blocking, navigation support and structural properties.

#### Standard terminology

| Term | Definition |
| --- | --- |
| **Background Tile** | The permanent tile occupying every world coordinate. |
| **Solid Tile** | An optional foreground tile occupying the same coordinate as its Background Tile. |
| **Covered Space** | A coordinate containing a movement-blocking foreground Solid Tile. |
| **Exposed Space** | A coordinate whose Background Tile is not covered by a foreground Solid Tile. |
| **Traversable Space** | Exposed Space satisfying navigation adjacency, whole-body clearance and applicable obstacle constraints. |
| **Unsupported Space** | Exposed Space that lacks the required supporting geometry. |
| **Reachable Space** | Traversable Space connected to the ant's current navigation region by a valid route. |
| **Qualifying Geometry** | Solid terrain or an explicitly configured object that provides valid adjacency for movement. |

Exposed does not automatically mean traversable, and traversable does not automatically mean reachable. An Exposed Space may contain a blocking placed object. Background Tiles do not provide support merely because they exist. A resident generated coordinate always has a Background Tile; an ungenerated chunk is unavailable rather than exposed.

### 4.2 Player control and hybrid task management

Normal play uses **Hybrid Task Management**:

- Players establish colony-level job priorities and designations.
- Players may assign a particular eligible task to an individual ant.
- Ants choose eligible work according to role, skills, availability and colony priorities, then navigate and perform it autonomously.
- Selecting an ant is for inspection and task management, not ordinary steering.

The player can select an ant and inspect its identity, attributes, skills, health, needs, assigned task and current activity. The player can also designate excavation and construction areas, mark resources for gathering, manage colony development and defence, and override global priorities with a valid individual assignment.

Developer-only manual movement remains available for testing and regression work. It is not an intended player-facing control mode and grants no movement or traffic priority.

### 4.3 Free tile-adjacent movement

- An ordinary ant has a nominal physical footprint of approximately one tile.
- Ants move smoothly and continuously through permitted space rather than hopping between tile centres.
- Movement may be horizontal, vertical or diagonal. Navigation may connect eight neighbouring tiles, but diagonal corner-cutting through solid or blocked geometry is prohibited.
- Ordinary locomotion is not driven by gravity. Ants do not jump, fly or cross unsupported open airspace.
- Solid terrain and configured solid objects remain authoritative collision boundaries.
- Clearance checks use the ant's whole footprint rather than only its centre point.
- The entire path segment, not only its endpoints or tile centres, must remain within navigable space and provide adequate body clearance.
- Future ant types may have larger footprints and may require wider passages.

An Exposed Space is **potentially traversable** when it has Qualifying Geometry in at least one cardinal direction (north, south, east or west), provides enough physical clearance, and is not otherwise blocked. Diagonal adjacency alone never qualifies a space. **Reachability** is separate: a locally Traversable Space may still be disconnected from the ant's current navigation region.

Ground travel, climbing beside cliffs, movement beneath overhangs, corner negotiation and tunnel travel are configurations of this one tile-adjacent navigation model, not separate floor, wall or ceiling movement modes.

### 4.4 Tunnels, chambers and supporting geometry

- Excavating Solid Tiles creates Exposed Spaces. Tunnels are traversable wherever connected Exposed Spaces have qualifying cardinal adjacency and sufficient clearance.
- Tile occupancy is not exclusive: multiple ants may occupy the same traversable tile when their continuous positions and collision constraints permit it.
- In a large chamber, strict cardinal adjacency applies. Exposed Spaces beside the floor, walls or ceiling may be traversable; unsupported central spaces are not.
- Ants may travel around a chamber perimeter when those locations form a connected, clearance-valid route, but they cannot move freely through unsupported central airspace.
- A surface gap is not crossable when crossing requires entering unsupported space. The prototype's automatic step-over behaviour is not an exception to this rule.

Objects have separate collision, navigation-support, interaction and pass-through properties. Not every collider or interactable provides navigation adjacency.

| Object type | Initial navigation-support rule |
| --- | --- |
| Solid earth and rock | Qualifies |
| Structural walls and platforms | Qualifies |
| Ants | Do not qualify |
| Food items | Do not qualify initially |
| Water | Does not qualify |
| Large constructed obstacles | Explicitly configurable |

### 4.5 Multiple ants and hybrid separation

- Friendly ants attempt local avoidance when space permits.
- Temporary visual or physical overlap between friendly ants is acceptable when needed to prevent persistent deadlocks.
- Avoidance must never push an ant into solid or unsupported geometry.
- Crowding may reduce movement efficiency, and route selection may favour less congested alternatives.
- No ant may remain permanently blocked without bounded recovery or a reported navigation failure.
- Task urgency, carried loads, danger and fairness may influence yielding.
- Directly controlled ants receive no special priority because manual control is a developer-only tool.

Rigid ant-to-ant collision is not required to serve as the initial traffic-management solution. All autonomous ants use the same terrain movement rules regardless of colony membership.

### 4.6 Enemy colonies and hostile encounters

Colony relationships affect decisions, not basic terrain traversability. Friendly, neutral and hostile ants use the same tile-adjacent navigation model. On encountering a hostile ant, an ant may engage, retreat, avoid, defend or request assistance according to its aggression, role, assignment and colony orders. Combat-focused ants may deliberately defend chokepoints. Detailed combat behaviour is deferred.

### 4.7 Current prototype transition

The current `AntSurfaceMotor` provides manually tested surface movement, corner transitions and automatic step-over behaviour. It is a prototype baseline, not the final player movement design. Preserve it until tile-adjacent autonomous movement is implemented and validated. Preserve `PlayerMovementInput` for developer testing. Future work should replace or adapt these systems incrementally with regression coverage; existing contour following and automatic step-over do not override the approved navigation rules above.

## 5. Excavation and soil logistics

The player marks one or more excavatable Solid Tiles for removal using a dig-designation tool. Excavation workers select reachable designations beside Exposed Space; inaccessible Solid Tiles remain queued until prior digging creates a valid route.

For the first soil implementation, each excavated soil Solid Tile yields one loose-soil unit. Other materials may later define different yields. The ant that dug it collects that unit and delivers it to a fixed deposit location at the surface before taking another order. Soil piles are visible but initially non-blocking.

When excavation work completes:

1. Remove only the foreground Solid Tile.
2. Leave its permanent Background Tile unchanged and visible.
3. Produce a transportable resource when the removed material defines one.
4. Update terrain collision.
5. Reevaluate navigation data in the affected region.
6. Invalidate or recalculate affected ant routes when required.

Excavation never removes or destroys a Background Tile.

| Rule | MVP behaviour |
| --- | --- |
| Designation | Click or drag to mark soil Solid Tiles; visually distinguish pending, in-progress and complete. |
| Access | Workers can only dig Solid Tiles with a reachable exposed face. |
| Job ownership | A Solid Tile excavation job is reserved by at most one worker at a time. |
| Excavation | Work progresses over time, potentially modified by excavation skill later. |
| Hauling | Each removed soil Solid Tile generates one soil unit; one delivery per job in the first prototype. |
| Deposit | Use one known surface deposit point initially; player-defined zones later. |
| Failure recovery | If path or dig location becomes invalid, release reservation and retry another valid order. |

## 6. Ants: individuality, roles and needs

All ants are persistent individuals. A caste defines biological capabilities; attributes describe innate differences; learned skills change through repeated activity; and an assigned job describes the ant’s current responsibility.

| Category | Initial concept |
| --- | --- |
| Individual identity | Unique ID/name, caste, status, assigned job. |
| Innate attributes | Strength, movement speed and endurance (final list to be tested). |
| Skills | Excavation, foraging, carrying and combat (combat deferred). |
| Needs | Health, hunger/food, hydration/water and energy/rest. |
| Jobs | Excavator, forager and hauler initially; brood-care later. |
| Selection | Click an ant or use the roster to inspect it and manage its work. |

Autonomous priority: immediate danger → critical personal needs → assigned job → idle behaviour. Ants should automatically eat, drink and rest when supplies are available. Job interruption and resumption must be predictable.

## 7. Queen transformation, brood and colony progression

The player starts with an ordinary ant rather than a queen. Becoming a queen may happen later through a progression event whose trigger, costs and presentation are open design questions. After transforming, the queen may establish the colony and later lay eggs. Brood matures through simplified stages until workers become selectable individuals. Once workers exist, the queen primarily focuses on reproduction and brood care; individual inspection and task assignment remain available.

The exact egg-production timings, mating biology, nutrition costs and brood-care systems are open design decisions, not implementation requirements for the first prototype.

## 8. Skills and progression

Skills improve by performing relevant actions, not by spending conventional level-up points. Excavation increases through digging, foraging through collecting food, and carrying through transporting loads. Improvements should be noticeable but moderate to avoid extreme productivity gaps.

Example only: a skilled excavator might complete work ~20% faster than a novice. No progression curves or rates are locked yet.

## 9. Food, water and productivity

Food and water are survival and logistics resources. Hunger and dehydration reduce productivity before eventually harming health. Energy falls during work and recovers during rest. Avoid fast-draining meters that turn play into micromanagement.

Initial implementation principle: ant needs may be displayed early, but full food/water production loops follow the proven movement-and-excavation prototype.

## 10. UI and controls

| Interaction | Proposed behaviour |
| --- | --- |
| Select ant | Click an ant or select from colony roster. |
| Mark excavation | Activate dig tool; click/drag across excavatable Solid Tiles. |
| Assign work | Set colony priorities or assign a specific eligible task to the selected ant. |
| Direct movement | Not part of normal play; developer-only manual input is retained for testing. |
| Inspect | View attributes, skills, health, needs, assigned task and current activity. |
| Camera | Pan/zoom and centre on selected ant. |

Normal gameplay does not manually steer ants. Destination requests arise from autonomous needs and jobs, global designations and priorities, or a specific task assigned to an individual.

## 11. Small-game scope and win condition

The initial full playable game should offer a single level with a small colony and an achievable victory. A proposed win condition is: maintain a colony of 20 ants, excavate at least three chambers, and accumulate 100 units of food. These targets are provisional and should be adjusted after playtesting.

Lose/pressure conditions are deliberately light in v1: a failed colony may occur if the queen dies or all ants perish, but balance should emphasise learning and rebuilding rather than frequent failure.

## 12. Roadmap and acceptance gates

| Stage | Deliverable / test |
| --- | --- |
| Milestone 1 — Movement prototype baseline | Preserve and finish validating the current manually driven surface-movement prototype as regression evidence; it is not the final player control design. |
| Milestone 2 — World tiles and autonomous navigation | Build on the sparse 32×32 two-layer chunk data foundation, visualise navigation classification, route one ant to a developer-assigned destination, validate all supported geometry, then update terrain and navigation incrementally. |
| Selection and task management | Inspect individual ants and support both colony-wide priorities and individual task assignments. |
| Multi-ant movement | Add hybrid separation, congestion-aware routing and bounded recovery. |
| Autonomous work | Perform excavation, soil transport, gathering and needs-driven behaviour. |
| Population and conflict | Scale toward hundreds of simulated ants, then support enemy-colony movement and high-level hostile responses. |
| Finished slice | Add onboarding, a clear objective, win/lose feedback, save/load and a usability pass. |

## 13. Out of scope until after the first finished game

- Multiplayer and Unity Netcode for GameObjects.
- Procedurally generated worlds or very large maps.
- Multiple ant species, elaborate caste systems, genetics and detailed pheromone modelling.
- Complex predators, combat campaigns or weather and seasons.
- Physical dirt barriers, sealing entrances and placing soil back into the terrain.
- Advanced storage logistics, colony diplomacy or a large technology tree.

## 14. Engineering principles

- Unity 6 + C#; single-player first. Represent permanent Background Tiles separately from optional foreground Solid Tiles.
- Use one tile-adjacent terrain navigation model for all autonomous ants, with whole-body clearance and authoritative collision.
- Keep world state (tiles, designated jobs, carried loads, ant stats) independent of visual presentation.
- Keep local traversability distinct from connectivity and reachability.
- Update navigation locally after terrain edits where practical; profile only once the core loop works.
- Track job claims to avoid ants competing for one tile. Plan explicit cancellation and recovery paths.
- Keep task selection, route requests, shared tile navigation data, movement execution, local avoidance and physics conceptually separate, but do not require a heavyweight framework or specific class name for each responsibility.
- Prefer event-driven navigation invalidation and bounded replanning over continuous full-route calculation.
- Design shared navigation data, nearby-ant queries, controlled update frequencies and optional pathfinding batches for potentially hundreds of independently simulated ants.
- Do not introduce DOTS/ECS or another substantial architecture dependency unless profiling demonstrates a need.
- Keep components small and comments educational. Avoid premature generic frameworks.

## 15. Autonomous navigation and traffic design

Autonomous movement is approved but not implemented. It must support excavation work, soil delivery, food and water access, rest, chamber travel, construction and hostile encounters.

The design separates these conceptual responsibilities. They describe boundaries, not mandatory class names:

- **World Tile Data:** stores resident 32×32 chunks containing a permanent Background Tile layer and optional foreground Solid Tile layer plus their configured properties. Missing chunks remain explicitly unavailable.
- **Terrain/Excavation System:** removes completed excavatable Solid Tiles, produces configured resources, updates collision and announces affected regions.
- **Colony Task System:** decides what work exists and which ants may perform it, using designations, priorities, roles, skills, availability and explicit individual assignments.
- **Ant Navigation Agent:** selects destinations, requests paths, follows routes and reports invalid or unreachable results.
- **Shared Navigation Map:** derives Traversable and Reachable Space, connectivity, clearance and relevant obstacles from world tile and object data.
- **Ant Movement Controller:** moves one ant continuously toward route waypoints while keeping the whole body inside valid space.
- **Local Avoidance:** handles nearby ants, congestion, yielding and hybrid separation without creating invalid movement.
- **Physics/Collision:** prevents penetration of solid terrain and relevant objects.

These responsibilities may begin as small cooperating components or data structures. They do not require global managers, generic frameworks or DOTS/ECS.

### 15.1 Traffic and deadlock rules

- Friendly ants use local avoidance where space permits, but temporary overlap is acceptable to break deadlocks.
- Avoidance and yielding never move ants through terrain or unsupported space.
- Waiting is valid temporarily, but excessive waiting must trigger bounded recovery such as reversing to a wider area, choosing an alternate route or replanning.
- Repeated collision attempts are not the primary navigation strategy.
- Replanning requests must be rate-limited or otherwise bounded to avoid uncontrolled repeated pathfinding.
- Recovery rules must prevent endless cycles where two ants repeatedly yield to one another.
- Task urgency, carrying state, danger and fairness may contribute to deterministic yielding decisions.

### 15.2 Dynamic excavation updates

Removing a foreground Solid Tile exposes its unchanged Background Tile. This can open Traversable Space, connect tunnels, widen a passage and change which Exposed Spaces have qualifying cardinal adjacency. Terrain collision and affected navigation regions must update together, and ants whose routes are affected must reconsider them safely. Runtime navigation therefore cannot rely on a once-baked static map.

### 15.3 Scalability expectations

- Avoid unnecessary per-frame physics queries for every ant.
- Reuse shared navigation information rather than rebuilding equivalent route data per ant or scanning the full map for every request.
- Use an efficient nearby-ant lookup for local avoidance.
- Keep high-level task decisions separate from low-level motion.
- Use efficient path requests, spatial partitioning for nearby-ant queries, and reduced AI update frequencies where appropriate.
- Control update frequencies and spread or batch expensive work across simulation ticks where appropriate.
- Keep traffic and recovery decisions deterministic enough to reproduce and debug.
- Do not add optimisation systems before profiling demonstrates a need.

## 16. Open design questions

- Which concrete tile-map data structure and pathfinding algorithm best meet the strict cardinal-support, eight-direction connection and incremental-update rules at the target scale?
- How should the approved Background Tile and Solid Tile layers map onto the project's eventual Unity Tilemaps or other runtime data without duplicating authoritative state?
- What clearance sampling or swept-body test is accurate enough for corners and non-square geometry without becoming too costly?
- Which congestion cost and yielding policy best balances task urgency, carrying status, danger and fairness?
- Which qualifying constructed objects should provide navigation support in the first playable slice?
- What failure feedback should be shown to the player when a task destination is invalid or unreachable?
- What event, requirements and costs allow the starting ordinary ant to become a queen?
- After transformation, does the queen dig and haul before workers hatch, and how is her energy replenished?
- How is brood nurtured, and what inputs are needed to hatch the first worker?
- Are ants individually named by the player or automatically numbered?
- Should stockpiled soil ever have mechanical value before placement is introduced?
- What are the minimum food/water supply systems needed for a satisfying completed game?

## 17. Scope-control rule

An idea is not added to the current milestone unless it directly enables its acceptance test or fixes a blocker. New ideas go into the backlog, and the project remains single-player until a finished small game exists.

Immediate implementation sequence: complete any remaining Milestone 1 regression acceptance, then begin Milestone 2.1 by inspecting the current world representation and establishing the smallest two-layer tile model plus navigation visualisation without changing movement behaviour. Do not begin autonomous routing, excavation, queen transformation, reproduction or needs simulation as part of that slice.
