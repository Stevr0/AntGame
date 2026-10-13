# Excavation and Terrain

## Two-layer world model

The world foundation is implemented. Each cell in a resident 32×32 chunk contains exactly one permanent Background Material and zero or one foreground Solid Material. Missing chunks are unavailable rather than silently generated during a query.

The two layers have different responsibilities:

| Layer | Purpose | Mutation rule |
| --- | --- | --- |
| Background | Persistent substrate, grip, movement speed, and future colony function | May be converted or prepared; never absent in a resident cell |
| Foreground Solid | Blocking/excavatable material and supporting geometry | May be removed by future excavation when eligible |

Visual Tilemaps and the collision-only Tilemap are projections of authoritative world data. The visual layers do not own gameplay state.

## Implemented terrain foundations

- Sparse signed-coordinate chunk data and explicit bounded generation.
- Covered, Exposed, Supported, Unsupported, Unknown, and Unavailable classification.
- Material definitions with independent movement-blocking and navigation-support flags.
- Background and foreground Tilemap rendering.
- A collision-only Tilemap with composite collider generation.
- Whole-body rotated-capsule clearance queries.
- Local refresh and navigation invalidation seams for controlled development mutations.
- Natural Earth/Stone grip, Prepared speed, and non-grippable Sky behavior.

These systems are development foundations. They do not yet provide player-facing excavation work.

## Approved excavation design

The player will designate eligible foreground Solid cells for excavation. An assigned ant must reach a valid work position, make incremental progress, and remove only the foreground material when work completes. The Background remains. Excavated material becomes a physical resource: the excavator picks it up when slot capacity permits and otherwise drops it safely in the world.

Valid partial progress must survive safe interruptions. Dynamic terrain changes must refresh collision, movement capability, and relevant route data without rebuilding the entire world unconditionally.

## Surface Preparation

Preparation is a future resource-backed Background conversion job. It does not unlock natural climbing: intact exposed Earth and Stone are already grippable. Prepared Background instead provides the provisional `1.25x` travel-speed benefit. Exact recipes, work time, visual progression, and final balance are unresolved.

## Not implemented

- Excavation designations and worker selection.
- Digging progress, completion, and physical soil creation.
- Hauling or stockpile delivery.
- Resource-backed preparation jobs.
- Procedural generation, durable chunk streaming, or save/load.
- Player-facing terrain tools outside deterministic development controls.

See [Ant Movement and Navigation](ant-movement-navigation.md) for mutation response and [Colony Construction and Rooms](colony-construction-rooms.md) for functional Background conversion.
