# ANT COLONY

**Game Design Document  |  Version 0.2**

Working title • Unity 6 • Single-player • 2D side-view • PC-first prototype

Design intent: Build a small, finishable colony simulation in which individual ants matter. The player directs excavation and assigns work; ants physically perform tasks and develop through experience.

## 1. Game at a glance

| Item | Definition |
| --- | --- |
| Genre | 2D side-view colony simulation / management |
| Starting scenario | The player begins as an ordinary ant. Becoming a queen is a later progression event whose requirements are still to be designed. |
| Primary interaction | Select ants, designate jobs and mark soil for excavation. |
| Defining feature | Each ant is a persistent, selectable individual with its own needs, stats, skills and autonomous behaviour. |
| Target | Small single-player game; initial colony target of up to approximately 20 ants. |
| Status | Core movement and navigation rules agreed; balance, queen transformation and later progression details remain provisional. |

## 2. Experience pillars

- A colony built physically: tunnels and chambers exist because ants excavate and move real soil.
- Individual ants, not disposable units: skills improve through doing; health, hydration, food and energy influence productivity.
- Direct control plus delegation: the player can take over any ant; unattended ants resume their assigned jobs.
- Readable, satisfying simulation: worker behaviour is observable and the player understands why an ant is idle or struggling.
- Finishability first: implement and polish one complete play loop before adding broad simulation features.

## 3. Player fantasy and game loop

The player begins as an ordinary ant. They initially explore and work directly, then later gain the ability to become a queen through a progression event that is still to be designed. Colony founding, reproduction and the transition into colony management occur after that transformation.

Core loop: Mark accessible soil → an assigned ant travels to it → excavates it → collects a soil unit → carries it to a dump location → returns for another job. As the colony grows, workers also gather food and water, rest, and gain skills.

## 4. World, movement and physical occupancy

The game uses a 2D side-view world with a grassy surface and solid, destructible soil tiles below. Excavated tiles create traversable empty space. The initial prototype uses a small authored map rather than procedural generation.

### 4.1 Control states

Every ant is one persistent physical entity that may operate in either of two control states:

- **Player controlled:** the selected ant receives directional input. Direct control safely interrupts autonomous movement and takes priority over task execution.
- **Autonomous:** an unselected ant eventually follows its assigned tasks and priorities without player movement input.

These are not separate ant types. Both states use the same surface attachment, clearance, collision and movement-execution rules. Only the selected ant receives direct input; every other ant remains present and collidable.

The player may select a different ant at any time. When an ant is selected, future route commitments or traffic reservations must be reconciled safely without teleporting or bypassing collision. When deselected, direct input stops and the ant may resume a still-valid assignment after recalculating navigation from its current position.

### 4.2 Tile scale and occupancy

- An ordinary ant has a nominal physical footprint of approximately one tile.
- This is a logical clearance and navigation constraint, not a requirement to snap the ant to tile centres. Movement remains smooth and continuous.
- Solid terrain and blocking entities prevent movement.
- Ants are physical entities and cannot pass through or overlap one another.
- Two ordinary ants cannot simultaneously occupy the same single-tile space.
- Clearance checks use the ant's whole footprint rather than only its centre point.
- The same occupancy and collision rules apply in both control states.
- Future ant types may have larger footprints and may require wider passages.

### 4.3 Surface traversal and directional control

Ants can attach to and move along any accessible exposed terrain surface, including floors, vertical walls, ceilings, inside corners, outside corners, shafts, tunnels and irregular excavation boundaries. An ant rotates naturally to align with its current surface and may transition only across physically connected, reachable surfaces with enough clearance for its footprint.

Direct control uses directional WASD input:

| Input | Intended world direction |
| --- | --- |
| W | Up |
| A | Left |
| S | Down |
| D | Right |

The player expresses a desired world direction; the traversal system determines which accessible surface can satisfy it. For example, D moves right along a floor, W can select and climb a reachable wall, S can descend one, and A/D move horizontally along a ceiling. At a junction, directional input lets the player deliberately choose a connected surface. Diagonal input may smooth transitions but must never bypass collision or reach an inaccessible surface.

Input interpretation, reusable surface movement and future route decisions are separate responsibilities. Autonomous ants must reuse the same physical movement rules rather than receiving a separate AI-only motor.

### 4.4 Openings and Auto Step-Over

**Auto Step-Over** is the provisional name for continuous traversal across a small unsupported opening.

- A horizontally moving ant may automatically cross an approximately one-tile opening when a valid opposite surface is physically reachable.
- The crossing must validate the ant's entire footprint, collision path, opposite landing surface and dynamic occupancy.
- Another ant or obstacle occupying the required space blocks the manoeuvre.
- The exact maximum distance is configurable and must be tested against footprint, reach and clearance instead of relying only on a tile count.
- Suitable animation is deferred.

An opening wider than the permitted Auto Step-Over distance cannot be crossed through jumping, flying, teleportation or unrestricted falling. The ant must follow an accessible connected surface into, through and out of the opening. A player-controlled ant supplies the necessary direction; an autonomous ant must plan a valid route. If no connected route exists, the destination is unreachable.

### 4.5 Tunnel width and passing

- A single-width tunnel fits one ordinary ant across its width. Ants travel in single file, maintain separation and cannot pass opposing traffic through each other.
- A double-width tunnel may allow two ordinary ants to pass on different sides or lanes, but only when the real collider geometry and ant footprints provide sufficient clearance.
- Wider tunnels and chambers allow multiple valid paths around other ants and must not be reduced to one artificial lane unnecessarily.
- A blocked ant must eventually wait, reverse, yield or reroute using valid movement rather than repeatedly pushing through the blocker.

Narrow tunnels are excavation-efficient but create meaningful colony traffic bottlenecks.

## 5. Excavation and soil logistics

Player marks one or more soil tiles for removal using a dig-designation tool. Excavation workers select reachable designated tiles adjacent to open space; inaccessible tiles remain queued until connected by prior digging.

For the first implementation, each excavated tile yields one loose-soil unit. The ant that dug it collects that unit and delivers it to a fixed deposit location at the surface before taking another order. Soil piles are visible but initially non-blocking.

| Rule | MVP behaviour |
| --- | --- |
| Designation | Click or drag to mark soil tiles; visually distinguish pending, in-progress and complete. |
| Access | Workers can only dig tiles with a reachable exposed face. |
| Job ownership | A tile is reserved by at most one worker at a time. |
| Excavation | Work progresses over time, potentially modified by excavation skill later. |
| Hauling | Each removed tile generates one soil unit; one delivery per job in the first prototype. |
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
| Selection | Click ant or use roster; control selected ant and inspect its information. |

Autonomous priority: immediate danger → critical personal needs → assigned job → idle behaviour. Ants should automatically eat, drink and rest when supplies are available. Job interruption and resumption must be predictable.

## 7. Queen transformation, brood and colony progression

The player starts as an ordinary ant rather than a queen. Becoming a queen happens later through a progression event whose trigger, costs and presentation are open design questions. After transforming, the queen establishes the colony and later lays eggs. Brood matures through simplified stages until workers become selectable individuals. Once workers exist, the queen primarily focuses on reproduction and brood care; direct player selection remains possible.

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
| Mark excavation | Activate dig tool; click/drag across soil tiles. |
| Assign job | Choose job on selected ant information panel. |
| Direct movement | WASD expresses an intended world direction for the currently selected ant. |
| Inspect | View needs, skills, job and current AI activity. |
| Camera | Pan/zoom and centre on selected ant. |

Direct WASD movement is the agreed control scheme for the current prototype. Point-and-click movement commands are not required and should not be developed in parallel unless later playtesting establishes a need.

## 11. Small-game scope and win condition

The initial full playable game should offer a single level with a small colony and an achievable victory. A proposed win condition is: maintain a colony of 20 ants, excavate at least three chambers, and accumulate 100 units of food. These targets are provisional and should be adjusted after playtesting.

Lose/pressure conditions are deliberately light in v1: a failed colony may occur if the queen dies or all ants perish, but balance should emphasise learning and rebuilding rather than frequent failure.

## 12. Roadmap and acceptance gates

| Stage | Deliverable / test |
| --- | --- |
| P1 — Individual surface traversal | One player ant uses directional input to traverse floors, walls, ceilings, corners and a valid one-tile opening in a pre-excavated test tunnel. |
| P2 — Tile excavation integration | Mark accessible tiles, remove them and update affected movement surfaces and connectivity at runtime. |
| P3 — Soil logistics and basic autonomous navigation | One autonomous ant reaches valid work and deposit destinations, delivers one soil unit per tile and recovers from invalid routes without loss or duplication. |
| P4 — Multi-ant occupancy and control | Multiple selectable ants collide safely, hand over between control states and handle narrow-passage traffic without indefinite deadlock. |
| P5 — Survival & growth | Food, water, rest, queen brood cycle and skill-by-doing functioning at small scale. |
| P6 — Finished slice | Onboarding, clear objective, win/lose feedback, save/load and usability pass. |

## 13. Out of scope until after the first finished game

- Multiplayer and Unity Netcode for GameObjects.
- Procedurally generated worlds or very large maps.
- Multiple ant species, elaborate caste systems, genetics and detailed pheromone modelling.
- Complex predators, combat campaigns or weather and seasons.
- Physical dirt barriers, sealing entrances and placing soil back into the terrain.
- Advanced storage logistics, colony diplomacy or a large technology tree.

## 14. Engineering principles

- Unity 6 + C#; single-player first. 2D Tilemap for destructible earth.
- Use shared surface movement, physical-clearance and collision rules for player-controlled and autonomous ants.
- Keep world state (tiles, designated jobs, carried loads, ant stats) independent of visual presentation.
- Build a simple reachable-surface representation before committing to a sophisticated pathfinder.
- Update navigation locally after terrain edits where practical; profile only once the core loop works.
- Track job claims to avoid ants competing for one tile. Plan explicit cancellation and recovery paths.
- Keep input interpretation, movement execution, static traversability, dynamic occupancy and task navigation conceptually separate, but do not require a heavyweight framework for each responsibility.
- Prefer event-driven navigation invalidation and bounded replanning over continuous full-route calculation.
- Keep components small and comments educational. Avoid premature generic frameworks.

## 15. Autonomous navigation and traffic design

Autonomous movement is designed but not implemented. It must support excavation work, soil delivery, food and water access, rest, chamber travel and navigation around other ants without changing the physical rules used for direct control.

The design separates four conceptual responsibilities:

- **Static traversability:** determine which surfaces exist, how they connect, whether an opening is crossable and whether a passage fits this ant's footprint.
- **Dynamic occupancy:** track which spaces or manoeuvres are occupied, being entered or exited, or temporarily blocked by ants and other moving entities.
- **Movement execution:** move one ant along a chosen route while maintaining surface attachment, collision and separation, and report when expected progress is blocked.
- **Task navigation:** request and follow a route to a task destination, notice invalidation, and resume, reconsider or release the task when necessary.

These responsibilities may begin as small cooperating components or data structures. They are not a requirement for four global managers or generic frameworks.

### 15.1 Traffic and deadlock rules

- Following traffic in a narrow tunnel maintains physical separation.
- Opposing traffic in a single-width passage cannot pass and must use a deterministic, testable yield decision when space is available.
- Waiting is valid temporarily, but excessive waiting must trigger bounded recovery such as reversing to a wider area, choosing an alternate route or replanning.
- Repeated collision attempts are not the primary navigation strategy.
- Replanning requests must be rate-limited or otherwise bounded to avoid uncontrolled repeated pathfinding.
- Recovery rules must prevent endless cycles where two ants repeatedly yield to one another.
- Passage reservations and local avoidance are possible future tools, not mandated systems. Implement them only if multi-ant tests show they are needed.

### 15.2 Dynamic excavation updates

Removing a soil tile can open space, connect tunnels, widen a passage and expose new floors, walls or ceilings. Runtime navigation therefore cannot rely on a once-baked static map. Terrain changes should invalidate only the affected traversal information where practical, and ants whose routes are affected must reconsider them safely.

### 15.3 Scalability expectations

- Avoid unnecessary per-frame physics queries for every ant.
- Reuse shared terrain and surface-connectivity data where practical.
- Keep high-level task decisions separate from low-level motion.
- Spread expensive work across simulation ticks when measurement shows it is useful.
- Keep traffic and recovery decisions deterministic enough to reproduce and debug.
- Do not add optimisation systems before profiling demonstrates a need.

## 16. Open design questions

- How should diagonal input and junction-selection tolerance be tuned without making direction choice ambiguous?
- What physical distance, clearance and timing define a valid Auto Step-Over for each ant footprint?
- What deterministic yielding priority best resolves opposing traffic without visible unfairness or oscillation?
- Which surface-aware routing representation is simplest once autonomous navigation work is approved?
- What event, requirements and costs allow the starting ordinary ant to become a queen?
- After transformation, does the queen dig and haul before workers hatch, and how is her energy replenished?
- How is brood nurtured, and what inputs are needed to hatch the first worker?
- Are ants individually named by the player or automatically numbered?
- Should stockpiled soil ever have mechanical value before placement is introduced?
- What are the minimum food/water supply systems needed for a satisfying completed game?

## 17. Scope-control rule

An idea is not added to the current milestone unless it directly enables its acceptance test or fixes a blocker. New ideas go into the backlog, and the project remains single-player until a finished small game exists.

Immediate next task: prototype one ordinary player ant traversing a pre-cut floor/wall/ceiling tunnel. Do not start queen transformation, reproduction, complex AI or nutrient simulation before that movement test passes.
