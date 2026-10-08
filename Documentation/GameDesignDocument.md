# ANT COLONY

**Game Design Document  |  Version 0.1**

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
| Status | Concept agreed; balance, reproduction details and controls remain provisional. |

## 2. Experience pillars

- A colony built physically: tunnels and chambers exist because ants excavate and move real soil.
- Individual ants, not disposable units: skills improve through doing; health, hydration, food and energy influence productivity.
- Direct control plus delegation: the player can take over any ant; unattended ants resume their assigned jobs.
- Readable, satisfying simulation: worker behaviour is observable and the player understands why an ant is idle or struggling.
- Finishability first: implement and polish one complete play loop before adding broad simulation features.
## 3. Player fantasy and game loop

The player begins as an ordinary ant. They initially explore and work directly, then later gain the ability to become a queen through a progression event that is still to be designed. Colony founding, reproduction and the transition into colony management occur after that transformation.

Core loop: Mark accessible soil → an assigned ant travels to it → excavates it → collects a soil unit → carries it to a dump location → returns for another job. As the colony grows, workers also gather food and water, rest, and gain skills.

## 4. World, camera and movement

- Side-view world with a grassy surface and soil below. Underground terrain is represented by solid, destructible tiles.
- Ants adhere to exposed surfaces: floors, walls and ceilings. No separate climb button is required in the intended final controls.
- The ant rotates with the surface normal and traverses contiguous tunnel boundaries.
- Excavation changes the available movement surfaces; navigation must update when tiles are removed.
- Initial prototype uses a small authored map, not procedural generation.
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

## 10. UI and controls — provisional

| Interaction | Proposed behaviour |
| --- | --- |
| Select ant | Click an ant or select from colony roster. |
| Mark excavation | Activate dig tool; click/drag across soil tiles. |
| Assign job | Choose job on selected ant information panel. |
| Direct movement | Issue movement commands or use direct-control mode (input details TBD). |
| Inspect | View needs, skills, job and current AI activity. |
| Camera | Pan/zoom and centre on selected ant. |

Important: direct-control inputs and command-style inputs are not yet finalised. Avoid designing both full control schemes until tested.

## 11. Small-game scope and win condition

The initial full playable game should offer a single level with a small colony and an achievable victory. A proposed win condition is: maintain a colony of 20 ants, excavate at least three chambers, and accumulate 100 units of food. These targets are provisional and should be adjusted after playtesting.

Lose/pressure conditions are deliberately light in v1: a failed colony may occur if the queen dies or all ants perish, but balance should emphasise learning and rebuilding rather than frequent failure.

## 12. Roadmap and acceptance gates

| Stage | Deliverable / test |
| --- | --- |
| P1 — Surface traversal | One player ant follows floor, wall and ceiling in a pre-excavated test tunnel with no collision glitches. |
| P2 — Tile excavation | Mark accessible tiles; queen/worker navigates, removes and opens paths. |
| P3 — Soil logistics | For each excavated tile, one soil unit reaches a fixed deposit point; no loss/duplication. |
| P4 — Individual ants | Multiple selectable ants, basic roles and autonomous job completion. |
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
- Use shared interaction rules for human-directed and autonomous ant actions.
- Keep world state (tiles, designated jobs, carried loads, ant stats) independent of visual presentation.
- Build a simple reachable-surface representation before committing to a sophisticated pathfinder.
- Update navigation locally after terrain edits where practical; profile only once the core loop works.
- Track job claims to avoid ants competing for one tile. Plan explicit cancellation and recovery paths.
- Keep components small and comments educational. Avoid premature generic frameworks.
## 15. Open design questions

- Exactly how does direct ant control work alongside point-and-click designation?
- What event, requirements and costs allow the starting ordinary ant to become a queen?
- After transformation, does the queen dig and haul before workers hatch, and how is her energy replenished?
- How is brood nurtured, and what inputs are needed to hatch the first worker?
- Are ants individually named by the player or automatically numbered?
- Should stockpiled soil ever have mechanical value before placement is introduced?
- What are the minimum food/water supply systems needed for a satisfying completed game?
## 16. Scope-control rule

An idea is not added to the current milestone unless it directly enables its acceptance test or fixes a blocker. New ideas go into the backlog, and the project remains single-player until a finished small game exists.

Immediate next task: prototype one ordinary player ant traversing a pre-cut floor/wall/ceiling tunnel. Do not start queen transformation, reproduction, complex AI or nutrient simulation before that movement test passes.
