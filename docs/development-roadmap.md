# Development Roadmap

This page is a concise navigation view of the authoritative `Documentation/DevelopmentRoadmap.md`. The authoritative file retains the full acceptance criteria and historical details.

## Immediate boundary

**Current action:** manually verify Milestone 3.4B's 30-step Unified Simulation Clock & Gameplay Mode checklist in `Assets/AntColony/Scenes/AntGameDevelopment.unity`.

Milestone 3.4B is **Implemented — Pending Manual Acceptance**. The scene starts paused at Day 1 08:00, normal Play bundles survival/work/hauling, and one authoritative clock drives pause plus 1×/2×/3×. Verification passed 11/11 focused Edit, 7/7 focused Play, 485/485 full graphics-enabled Edit and 58/58 full Play. The full player uGUI HUD remains 3.4C.

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
| 3.4A | Functional Background conversion and builder self-supply | Complete |
| 3.4B | Unified simulation clock and normal gameplay mode | Implemented — Pending Manual Acceptance |

## Ordered future direction

The numbered roadmap beyond the accepted foundation remains direction, not authorised implementation:

1. Complete 3.4B manual acceptance.
2. Implement 3.4C's authored player uGUI HUD only after separate approval.
2. Multiple-ant movement, local avoidance, and congestion recovery.
3. Autonomous worker and survival loops.
4. Excavation, Surface Preparation, physical resources, and hauling.
5. Functional construction, rooms, doors, and quality.
6. Nursery, brood, reproduction, and colony growth.
7. Population/performance scaling and persistence.
8. Enemy colonies, equipment, combat, and defence.
9. Finished-slice onboarding, objectives, save/load, and polish.

Detailed scope and acceptance criteria must be approved when each milestone begins. See [Design Decision Log](design-decision-log.md) for unresolved dependencies.
