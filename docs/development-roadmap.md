# Development Roadmap

## Current implementation — Milestone 3.5A

**Implemented — Pending Manual Acceptance (14 October 2026).** Colony View designation previews, compatible right-click cancellation and the first allow-listed Soil/Stone custom-art projection are implemented. 3.4E congestion recovery, World View runtime, seamless autotiling and wider art replacement remain out of scope.

This page is a concise navigation view of the authoritative `Documentation/DevelopmentRoadmap.md`. The authoritative file retains the full acceptance criteria and historical details.

## Parallel art foundation

**Implemented — Pending Manual Acceptance.** The custom source sheets and separate 32-pixel Colony/World candidate libraries are committed with deterministic extraction and import settings. They are not gameplay-bound or certified seamless. Manual Sprite Editor, camera-scale and repeated-edge review precedes production Tile assets and palettes; World View gameplay remains deferred.

## Immediate boundary

**Current action:** manually verify Milestone 3.4D's 18-step Colony Management & Work Priorities checklist in `Assets/AntColony/Scenes/AntGameDevelopment.unity`.

Milestones 3.4B and 3.4C are manually accepted. Milestone 3.4D is **Implemented — Pending Manual Acceptance**. Its global Work matrix uses authoritative active ant records, exposes only real Excavation/Construction/Hauling executors, and edits the existing per-ant 1–4/Disabled values shared with developer diagnostics. Verification passed 7/7 focused Edit, 7/7 focused Play, 492/492 full Edit and 65/65 full Play.

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
| 3.4B | Unified simulation clock and normal gameplay mode | Complete |
| 3.4C | Colony View player HUD and developer UI separation | Complete |
| 3.4D | Colony management and work priorities UI | Implemented — Pending Manual Acceptance |

## Approved World View direction

World View is approved design but is not implemented or authorised for coding. It will add a top-down surface connected to the side-on Colony View through nest entrances. Persistent ants and the simulation clock are shared; switching views must not duplicate ants or stop the hidden region.

The proposed delivery order is:

1. approve two-view coordinate, residency, entrance, navigation and persistence contracts;
2. build one small bounded surface and one entrance transition;
3. add a narrow physical surface-foraging and return loop;
4. add bounded autonomous cross-view jobs;
5. expand surface chunks and environments incrementally;
6. add lightweight unloaded-chunk simulation; and
7. consider predators, rival colonies and wider ecology only through later approved scopes.

The authoritative prerequisites and phase boundaries are in `Documentation/DevelopmentRoadmap.md`.

## Ordered future direction

The numbered roadmap beyond the accepted foundation remains direction, not authorised implementation:

1. Complete 3.4D manual acceptance and correct only observed matrix defects.
2. Multiple-ant movement, local avoidance, and congestion recovery.
3. Extend functional construction into rooms, doors and quality through separate scope.
4. Add Nursery, brood, reproduction and colony growth through separate scope.
5. Address population/performance scaling and persistence.
6. Phase World View through its separately approved small-surface, cross-view and chunk-expansion milestones.
7. Add enemy colonies, equipment, combat and defence only when approved.
8. Complete finished-slice onboarding, objectives, save/load and polish.

Detailed scope and acceptance criteria must be approved when each milestone begins. See [Design Decision Log](design-decision-log.md) for unresolved dependencies.
