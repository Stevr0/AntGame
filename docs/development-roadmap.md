# Development Roadmap

This page is a concise navigation view of the authoritative `Documentation/DevelopmentRoadmap.md`. The authoritative file retains the full acceptance criteria and historical details.

## Immediate boundary

**Next action:** complete the exact 20-step milestone 3.2B manual Play Mode checklist in `Assets/AntColony/Scenes/AntGameDevelopment.unity` and wait for user acceptance.

Do not begin autonomous survival, colony jobs, gathering, hauling, construction, excavation gameplay, or another milestone without separate approval.

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
| 3.2B | Physical food, water, and rest interactions | Implemented — Pending Manual Acceptance |

## Ordered future direction

The numbered roadmap beyond the accepted foundation remains direction, not authorised implementation:

1. Hybrid task assignment and colony-wide priorities.
2. Multiple-ant movement, local avoidance, and congestion recovery.
3. Autonomous worker and survival loops.
4. Excavation, Surface Preparation, physical resources, and hauling.
5. Functional construction, rooms, doors, and quality.
6. Nursery, brood, reproduction, and colony growth.
7. Population/performance scaling and persistence.
8. Enemy colonies, equipment, combat, and defence.
9. Finished-slice onboarding, objectives, save/load, and polish.

Detailed scope and acceptance criteria must be approved when each milestone begins. See [Design Decision Log](design-decision-log.md) for unresolved dependencies.
