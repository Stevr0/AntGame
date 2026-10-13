# Ant AI and Job Management

## Approved job model

Future autonomous work uses a per-ant matrix of priorities `1–4` plus **Disabled**. The highest eligible priority wins before the closest reachable job by path cost. Skills change efficiency; they do not override player-assigned priority.

Ants may also receive temporary explicit orders. Critical survival may interrupt ordinary work at a safe point. Valid partial progress persists, and exclusive reservations prevent two ants from consuming or owning the same work target. Drafted survival intervention remains unresolved.

## Current implementation

Milestone 3.3A implements the development-only foundation:

- Every ant record owns independent priorities for Excavation, Surface Preparation, Construction, Hauling, Foraging, Production, Combat, and Brood Care.
- `ColonyJobRegistry` owns synthetic job identity, target, status, reservation, progress, and failure state outside GameObjects.
- Highest enabled priority is evaluated before actual A* route cost. Equal costs use stable deterministic ties.
- Evaluations inspect at most 32 candidates per priority, are staggered, and back off for 2/4/8 seconds on failure.
- `SyntheticColonyJobOrder` uses `GridNavigationMotor` and an adjacent work position. It does not mutate terrain, resources, or inventories.
- Work takes a provisional four simulation hours, uses Rested and relevant-skill speed, and awards proportional XP only for committed progress.
- Player orders and critical survival pause work and release ownership while preserving progress. Ordinary survival waits for active work.
- The development scene exposes five deterministic fixtures plus priority, designation, cancellation, reset, and diagnostic controls.

Milestone 3.2C autonomous survival is accepted. Milestone 3.3A remains **Implemented — Pending Manual Acceptance**.

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

All work remains synthetic. Real hauling, excavation, Surface Preparation, construction, gathering, production, combat, brood care, inventories, forced-work orders, Draft Mode, group tactics, local avoidance, persistence, and production-scale indexing require later milestones.
