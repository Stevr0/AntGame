# Excavation and Terrain

## Colony View two-layer world model

The implemented terrain foundation belongs to the underground **Colony View**. Each cell in a resident 32×32 chunk contains exactly one permanent Background Material and zero or one foreground Solid Material. Missing chunks are unavailable rather than silently generated during a query.

The two layers have different responsibilities:

| Layer | Purpose | Mutation rule |
| --- | --- | --- |
| Background | Persistent substrate, grip, movement speed, and future colony function | May be converted or prepared; never absent in a resident cell |
| Foreground Solid | Blocking/excavatable material and supporting geometry | Removed only by a completed eligible excavation transaction |

Visual Tilemaps and the collision-only Tilemap are projections of authoritative world data. The visual layers do not own gameplay state.

These Background/Solid semantics must be preserved for Colony View when World View is added. They are not automatically the terrain schema for the future top-down surface.

## Implemented terrain foundations

- Sparse signed-coordinate chunk data and explicit bounded generation.
- Covered, Exposed, Supported, Unsupported, Unknown, and Unavailable classification.
- Material definitions with independent movement-blocking and navigation-support flags.
- Background and foreground Tilemap rendering.
- A collision-only Tilemap with composite collider generation.
- Whole-body rotated-capsule clearance queries.
- Local refresh and navigation invalidation seams for controlled development mutations.
- Natural Earth/Stone grip, Prepared speed, and non-grippable Sky behavior.

Milestone 3.3B connected these foundations to player designations and autonomous excavation work. Milestone 3.3C connects committed yield to the excavator's inventory with a recoverable loose-drop fallback. Milestone 3.3D lets ants deliver those physical materials into filtered one-slot Material Storage Background tiles.

Milestone 3.4A adds player-designated Background conversion. Completion consumes builder-staged Soil/Stone, preserves foreground Solid occupancy, refreshes the Background Tilemap and invalidates local navigation through the existing mutation seam.

Milestone 3.4C exposes ordinary Colony View **Excavate**, six-recipe **Build**, and **Cancel designation** tools through the authored player HUD. Single-cell and bounded-drag input still delegates to the accepted registries. Cancellation retains the existing conserving material behavior, and uGUI/developer-IMGUI hit testing blocks designation start or release over interface panels.

## Implemented excavation and material handling

Excavate mode creates registry-owned jobs for eligible foreground Solid cells. Autonomous ants select them through the accepted priority/path-cost rules, reserve exclusively, reach a valid adjacent anchor and make incremental progress. Safe interruption retains progress. Completion removes only the foreground Solid and preserves the Background.

The stable job-keyed transaction prepares material ownership before terrain removal. Yield fills the excavator's compatible inventory capacity first; any remainder becomes a recoverable world drop. Inventory-only, drop-only and split output all commit once. A failed output preparation leaves the Solid intact, and retries cannot duplicate yield. The affected visual/collision cell and local navigation are refreshed after commit.

Loose Soil and Stone stacks have stable identity, quantity, cell and provisional maximum stack 10. A selected ant can right-click a drop, reserve an eligible quantity, navigate adjacent and pick it up. Developer Only controls can drop a selected carried quantity at the ant's current valid anchor. Compatible same-cell stacks merge; failed or cancelled transfers preserve material and release reservations.

Material Storage remains physical rather than a global wallet. Each functional Background cell owns one compatible stack and incoming reservation. Autonomous haulers reserve source and destination, carry partial loads through personal inventory, and deposit only after reaching a valid adjacent anchor. Already-carried material produces an owner-only self-deposit task.

## Surface Preparation

Preparation is now one 3.4A resource-backed Background conversion target. It costs a provisional one Soil and four simulation work hours. It does not unlock natural climbing: intact exposed Earth and Stone are already grippable. Prepared Background retains the provisional `1.25x` travel-speed benefit. Final balance remains unresolved.

## Future World View terrain

> **Approved direction, not implementation.** World View will be a top-down, continuous chunk-based surface. Its terrain representation, obstacle/elevation rules, chunk data and local mutation model still require a separately approved specification.

The first surface slice must be a small playable area around a known nest entrance. It should prove surface presentation, traversal, a narrow collection/return loop and persistent state before additional chunks or environments are introduced. Read-only queries must not generate missing surface chunks or treat unloaded space as empty, matching the existing conservative availability rule without requiring both views to share the same tile layers.

Nest entrances map explicit Colony View locations to explicit World View locations. Excavation near an underground entrance and surface changes near its paired exit will eventually need coordinated validity rules, but entrance construction, blockage and relocation behavior remain unresolved.

## Not implemented

- Procedural generation, durable chunk streaming, or save/load.
- World View surface terrain, nest-entrance transitions and cross-view terrain persistence.
- Resource crafting, demolition, rooms and door construction.

See [Ant Movement and Navigation](ant-movement-navigation.md) for mutation response and [Colony Construction and Rooms](colony-construction-rooms.md) for functional Background conversion.
