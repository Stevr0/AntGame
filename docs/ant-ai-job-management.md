# Ant AI and Job Management

## Approved job model

Future autonomous work uses a per-ant matrix of priorities `1–4` plus **Disabled**. The highest eligible priority wins before the closest reachable job by path cost. Skills change efficiency; they do not override player-assigned priority.

Ants may also receive temporary explicit orders. Critical survival may interrupt ordinary work at a safe point. Valid partial progress persists, and exclusive reservations prevent two ants from consuming or owning the same work target. Drafted survival intervention remains unresolved.

## Current implementation

Milestones 3.3A and 3.3B are accepted, and 3.3C extends that foundation with personal inventory and explicit material transfers:

- Every ant record owns independent priorities for Excavation, Surface Preparation, Construction, Hauling, Foraging, Production, Combat, and Brood Care.
- `ColonyJobRegistry` owns synthetic job identity, target, status, reservation, progress, and failure state outside GameObjects.
- Highest enabled priority is evaluated before actual A* route cost. Equal costs use stable deterministic ties.
- Evaluations inspect at most 32 candidates per priority, are staggered, and back off for 2/4/8 seconds on failure.
- `SyntheticColonyJobOrder` remains as a regression fixture. Real designations use `ExcavationJobOrder` through the same registry, selector, motor, adjacency, priority and interruption seams.
- Excavate mode accepts eligible foreground Solids by world coordinate, supports bounded area drags, and blocks ordinary selection/orders while active.
- Soil and Stone completion uses a stable job-keyed transaction. Output enters the excavator's inventory when possible, with any remainder becoming a recoverable drop. Output failure preserves the Solid; successful removal preserves Background and refreshes one visual/collision/navigation locality.
- Work takes a provisional four simulation hours, uses Rested and relevant-skill speed, and awards proportional XP only for committed progress.
- Player orders and critical survival pause work and release ownership while preserving progress. Ordinary survival waits for active work.
- Explicit player pickup reserves one loose drop quantity and travels through the same shared navigation/adjacency seam. This is an order, not an autonomous Hauling job.
- The development scene exposes deterministic job, excavation and loose-drop fixtures plus priority, inventory, cancellation, reset, and diagnostic controls.

Milestones 3.3A and 3.3B are accepted. Milestone 3.3C is **Implemented — Pending Manual Acceptance** with 26/26 focused Edit, 5/5 focused Play, 432/432 full Edit and 42/42 full Play passing.

## Future decision flow

```mermaid
flowchart TD
  A[Ant available] --> S{Critical survival?}
  S -- Yes --> I[Interrupt safely and reserve survival interaction]
  S -- No --> O{Explicit order?}
  O -- Yes --> E[Validate and execute order]
  O -- No --> P[Find highest enabled work priority]
  P --> R[Filter eligible reachable unreserved jobs]
  R --> C[Choose lowest path cost]
  C --> X[Reserve, travel, work, and commit]
  I --> X
  E --> X
  X --> A
```

## Required architecture boundaries

- Job choice must not live in UI code.
- Navigation calculates routes; it does not decide why an ant travels.
- Work progress and physical resources remain authoritative outside visuals.
- Cancellation and interruption must release reservations exactly once.
- Construction preserves builder self-supply.
- Hundreds-of-ants goals require controlled update rates and shared data, but do not justify DOTS/ECS before profiling.

## Not implemented

Excavation, personal inventory and explicit loose-drop pickup are now real. Autonomous hauling, Material Storage, Surface Preparation, construction, gathering, production, combat, brood care, forced-work orders, Draft Mode, group tactics, local avoidance, persistence, and production-scale indexing require later milestones.
