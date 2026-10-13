# Development Roadmap

This page is a concise navigation view of the authoritative `Documentation/DevelopmentRoadmap.md`. The authoritative file retains the full acceptance criteria and historical details.

## Immediate boundary

**Current action:** manually verify Milestone 3.4A's 30-step Background Conversion and Construction checklist in `Assets/AntColony/Scenes/AntGameDevelopment.unity`.

Milestone 3.4A is implemented with 14/14 focused Edit, 3/3 focused Play, 469/469 full Edit and 50/50 full Play passing. Builder self-supply, physical staging and Background conversion are in scope; doors, rooms, brood and production remain excluded. The UI pass and 3.3D are manually accepted.

## Current milestone state

| Milestone | Outcome | Status |
| --- | --- | --- |
| 1 | Continuous-input player-ant movement prototype | In progress; retained regression baseline |
| 2.1A–2.1E | Two-layer world, classification, visualisation, Tilemaps | Complete |
| 2.2A–2.2B | Authoritative collision and whole-body clearance | Complete |
| 2.2C | Background capability and gravity eligibility | Implemented and automated; dedicated manual Scene-view acceptance pending |
| 2.3A–2.3B | Bounded autonomous ground/grip/gravity movement | Complete under recorded acceptance |
| 2.4A | Natural Background grip and Prepared speed | Complete |
| 2.4B | Grid navigation proof of concept | Complete |
| 2.4C | Scheduler stabilisation and multi-ant readiness | Complete |
| 2.4D | Production grid navigation integration | Implemented — Pending Manual Acceptance |
| 3.1A | Selection and independent move orders | Complete |
| 3.1B | Persistent ant identity and attributes | Complete |
| 3.1C | Learn-by-doing skill foundation | Complete |
| 3.2A | Needs and survival foundation | Complete |
| 3.2B | Physical food, water, and rest interactions | Complete |
| 3.2C | Individual autonomous survival | Complete |
| 3.3A | Synthetic colony jobs and per-ant priorities | Complete |
| 3.3B | Real excavation and loose Soil/Stone output | Complete |
| 3.3C | Per-ant inventory and explicit physical material transfers | Complete |
| 3.3D | Physical Material Storage and autonomous Soil/Stone hauling | Complete |
| 3.4A | Functional Background conversion and builder self-supply | Implemented — Pending Manual Acceptance |

## Ordered future direction

The numbered roadmap beyond the accepted foundation remains direction, not authorised implementation:

1. Complete 3.4A manual acceptance before defining the next narrow slice.
2. Multiple-ant movement, local avoidance, and congestion recovery.
3. Autonomous worker and survival loops.
4. Excavation, Surface Preparation, physical resources, and hauling.
5. Functional construction, rooms, doors, and quality.
6. Nursery, brood, reproduction, and colony growth.
7. Population/performance scaling and persistence.
8. Enemy colonies, equipment, combat, and defence.
9. Finished-slice onboarding, objectives, save/load, and polish.

Detailed scope and acceptance criteria must be approved when each milestone begins. See [Design Decision Log](design-decision-log.md) for unresolved dependencies.
