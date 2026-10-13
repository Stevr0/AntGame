# Ant AI and Job Management

## Approved job model

Future autonomous work uses a per-ant matrix of priorities `1–4` plus **Disabled**. The highest eligible priority wins before the closest reachable job by path cost. Skills change efficiency; they do not override player-assigned priority.

Ants may also receive temporary explicit orders. Critical survival may interrupt ordinary work at a safe point. Valid partial progress persists, and exclusive reservations prevent two ants from consuming or owning the same work target. Drafted survival intervention remains unresolved.

## Current implementation

The general job system is not implemented. Existing command behavior is deliberately narrower:

- Ants can be selected individually.
- Right-click issues Move, Eat, Drink, or Rest according to target context.
- Orders have explicit travel, interaction, completion, cancellation, and failure states.
- Movement uses the shared grid navigation service and motor.
- 3.2B uses stable-ant reservations for physical resources and sleeping positions.
- A valid order continues when the player changes selection.

These seams are useful foundations, but they do not establish colony-wide work discovery, autonomous survival, priority evaluation, or generic job ownership.

## Future decision flow

```mermaid
flowchart TD
  A[Ant available] --> S{Critical survival?}
  S -- Yes --> I[Interrupt safely and reserve survival job]
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

Priority matrices, job categories, colony designations, work scanning, critical-survival autonomy, safe interruption thresholds, generic reservations, hauling, excavation, construction, Draft Mode, group tactics, local avoidance, and persistence all require separately approved milestones.
