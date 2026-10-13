# Ant Movement and Navigation

Movement and navigation are the most extensively implemented and tested foundations in the project. They deliberately separate world truth, movement capability, route planning, and motor execution.

## Authoritative rules

- Every resident coordinate has one Background Tile and at most one foreground Solid Tile.
- A blocking Solid prevents occupancy; excavation will remove only eligible foreground material.
- Exposed natural Earth and Stone Backgrounds are grippable at normal speed.
- Prepared Background grants the same access with a provisional `1.25x` movement-speed multiplier.
- Sky is non-grippable; without ground or grip an ant can fall.
- Full-body clearance is separate from grip, grounding, traversability, and reachability.
- Valid diagonal connections cannot cut blocked corners.
- Stable movement follows shared navigation; Rigidbody2D gravity is reserved for Falling.

## Production flow

```mermaid
flowchart LR
  W[ChunkedWorldData] --> C[Clearance and capability]
  W --> M[Shared GridNavigationMap]
  C --> M
  M --> P[GridPathfinder and scheduler]
  P --> S[GridNavigationWorldService]
  S --> G[GridNavigationMotor]
  G --> V[Ant visual and status]
```

`GridNavigationWorldService` is the per-world owner of the shared map and bounded scheduler. `GridNavigationMotor` exposes the narrow destination, cancellation, and status boundary used by selection and survival interactions. The command layer does not implement a competing movement system.

## Implemented milestones

| Area | Status | Evidence summary |
| --- | --- | --- |
| Chunked two-layer world | Complete | Signed chunks and missing-world behavior tested |
| Classification and visualisation | Complete | Automated and manual visual acceptance recorded |
| Collision projection and body clearance | Complete | Physics-backed and manual checks accepted |
| Natural grip, prepared speed, gravity | Complete | 56/56 focused Edit Mode, 227/227 full Edit Mode, 6/6 Play Mode plus manual acceptance |
| Grid-navigation proof of concept | Complete | A*, smooth following, falling/recovery, and performance evidence accepted |
| Shared scheduling and multi-ant readiness | Complete | Bounded fair scheduling and 1/100/300/500 request batches validated |
| Production grid integration | Implemented — Pending Manual Acceptance | Static compilation and later suites passed; its dedicated manual checklist remains open |
| Selection and independent move orders | Complete | 6/6 focused Edit Mode, 4/4 focused Play Mode, 258/258 full Edit Mode, 18/18 full Play Mode plus manual acceptance |

## Known limitations

- Friendly overlap is currently intentional; local avoidance and congestion behavior are future work.
- The current grid is a deterministic authored development world, not procedural streaming gameplay.
- General multiple-ant traffic, reservations, and deadlock handling are not complete gameplay systems.
- The older `AntSurfaceMotor` and `PlayerMovementInput` course remains for regression testing. It is not the normal control model.
- The original movement prototype still has pending ceiling, complete-corner-route, Auto Step-Over, and camera-follow completion work; that does not invalidate the accepted grid-navigation milestones.

## Future acceptance

Dynamic excavation and functional conversion must locally invalidate affected navigation data. Future agents must use the same route and motor rules; they cannot teleport, fly through unsupported space, or bypass clearance because a task system requested it.

<div class="screenshot-slot">
Future diagram or screenshot: grid anchors, prepared-speed region, falling transition, and an invalidated route after terrain mutation.
</div>
