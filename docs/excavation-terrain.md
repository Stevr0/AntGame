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

Milestone 3.3B connected these foundations to player designations and autonomous excavation work. Milestone 3.3C added personal inventory and explicit physical pickup/drop transfer; its historical automatic excavation pickup was superseded by Milestone 3.5B. Excavation now always leaves committed yield loose. Milestone 3.3D lets ants deliver those physical materials into filtered one-slot Material Storage Background tiles.

Milestone 3.4A adds player-designated Background conversion. Completion consumes builder-staged Soil/Stone, preserves foreground Solid occupancy, refreshes the Background Tilemap and invalidates local navigation through the existing mutation seam.

Milestone 3.4C exposes ordinary Colony View **Excavate**, six-recipe **Build**, and **Cancel designation** tools through the authored player HUD. Single-cell and bounded-drag input still delegates to the accepted registries. Cancellation retains the existing conserving material behavior, and uGUI/developer-IMGUI hit testing blocks designation start or release over interface panels.

Milestone 3.5A adds a read-only hover/drag preview. Excavation cells are amber when the excavation registry reports eligible, red when invalid and muted with an explicit marker when already designated. Background Conversion uses the same pattern with green valid cells and the selected recipe's authoritative construction validation. The preview does not create jobs or terrain objects and clears on tool exit, blocked pointer focus or pause.

Right-click in Excavation mode cancels only an excavation designation under the pointer; right-click in Background Conversion cancels only construction. The click is consumed even when the target is empty, preventing a movement order from dispatching through the tool. Select-mode contextual right-click and the existing Cancel tool remain unchanged.

## Implemented excavation and material handling

Excavate mode creates registry-owned jobs for eligible foreground Solid cells. Autonomous ants select them through the accepted priority/path-cost rules, reserve exclusively, reach a valid adjacent anchor and make incremental progress. Safe interruption retains progress. Completion removes only the foreground Solid and preserves the Background.

The stable job-keyed transaction preflights the complete loose output before terrain removal. The excavator's inventory and storage availability are deliberately irrelevant. Yield first merges into compatible partial stacks at the excavated authoritative cell, then may create one new bounded stack there. The logical drop model permits multiple traversable stacks on a cell, so this is always the preferred/current placement; no alternate-cell search is needed. A failed or unrepresentable output leaves the Solid intact, and retries cannot duplicate yield. Successful commit preserves Background, refreshes the affected visual/collision cell and invalidates local navigation.

Loose Soil and Stone stacks have stable identity, quantity, cell and provisional maximum stack 10. A selected ant can right-click a drop, reserve an eligible quantity, navigate adjacent and pick it up. Developer Only controls can drop a selected carried quantity at the ant's current valid anchor. Compatible same-cell stacks merge; failed or cancelled transfers preserve material and release reservations.

Foraging means discovering or identifying resources. Extraction means physically removing them and producing loose material. Hauling is the separate pickup/transport activity. Construction remains builder-owned self-supply from carried inventory, storage or loose drops. Excavation never assigns hauling automatically, and a no-storage colony simply retains recoverable loose output.

Material Storage remains physical rather than a global wallet. Each functional Background cell owns one compatible stack and incoming reservation. Autonomous haulers reserve source and destination, carry partial loads through personal inventory, and deposit only after reaching a valid adjacent anchor. Already-carried material produces an owner-only self-deposit task.

## Surface Preparation

Preparation is now one 3.4A resource-backed Background conversion target. It costs a provisional one Soil and four simulation work hours. It does not unlock natural climbing: intact exposed Earth and Stone are already grippable. Prepared Background retains the provisional `1.25x` travel-speed benefit. Final balance remains unresolved.

## Future World View terrain

> **WV-A specified direction, not implementation.** World View will be a top-down, continuous chunk-based surface. WV-A now requires distinct typed Surface coordinates, a deterministic bounded first region, explicit unavailable missing chunks and a Surface-local terrain/navigation authority. Final dimensions, cell scale, obstacle/diagonal rules, scene packaging and mutation model still require WV-B approval.

The first surface slice must be a small playable area around a known nest entrance. It should prove surface presentation, traversal, a narrow collection/return loop and persistent state before additional chunks or environments are introduced. Read-only queries must not generate missing surface chunks or treat unloaded space as empty, matching the existing conservative availability rule without requiring both views to share the same tile layers.

The first stable `EntranceId` maps one typed Colony endpoint to one typed Surface endpoint and has transition capacity one. Endpoint blockage invalidates approach/arrival and leaves the ant source-resident. Entrance construction, relocation, multiple entrances and terrain changes that would remove an endpoint remain deferred beyond the first slice.

## Not implemented

- Procedural generation, durable chunk streaming, or save/load.
- World View surface terrain, nest-entrance transitions and cross-view terrain persistence.
- Resource crafting, demolition, rooms and door construction.

See [World View Foundation](world-view-foundation.md), [Ant Movement and Navigation](ant-movement-navigation.md) and [Colony Construction and Rooms](colony-construction-rooms.md).
