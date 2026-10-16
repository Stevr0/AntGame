# Excavation and Terrain

## Resource and Node v1.0 target

Every target Solid is a finite variable-quantity Resource Node. Normal Solid
Extraction removes/depletes the whole Node, preserves its natural Background and
commits every remaining unit into recoverable physical output. A yield larger
than one stack uses deterministic additional compatible cells or remains a
recoverable pending transaction. Partial harvesting requires an explicitly
approved special Node definition. See [Resource and Node System](resource-and-node-system.md).

## WV-B3C.1 diagnostic Node records

Each prepared Colony scenario now builds an inactive, read-only quantity-one
Node record for every registered Soil/Stone Solid. The record exposes stable
typed identity, captured quantity, extraction metadata, provenance, revision
and lifecycle for diagnostics. Existing `ExcavationWorldTransaction` still
preflights output, removes the authoritative Solid and commits the physical
drop. That terrain removal marks the matching diagnostic record
`DepletedOrAbsent`; it does not decrement registry quantity or route gameplay
through the registry. WV-B3C.1 is complete after 17/17 manual acceptance;
B3C.2 canonical stacks and B3C.3 transaction cutover have not started. The
inactive quantity-one records remain a transitional foundation.

## WV-B3B read projection

Existing Soil/Stone Solids are now visible as compatibility Node snapshots and
committed `ChunkedWorldData` writes advance the affected runtime-cell revision.
Extraction authority, output preflight, Background preservation, XP and
navigation invalidation are unchanged. Successful physical drop commits also
publish a loose-resource cell change; reservations alone do not. Quantity-less
legacy Solids remain explicit compatibility data rather than fabricated finite
Nodes. Mutable Node migration belongs to WV-B3C.

## WV-B3A Node migration boundary

The approved runtime target represents Soil, Stone, Seed, Twig and future
natural resources as finite Nodes over an unchanged natural Background.
Current Colony Solids remain quantity-one compatibility Nodes until later
WV-B3C gates are separately approved. Extraction must prepare complete
compatible ground output before exact-once Node reduction; failed placement retains a
recoverable pending transaction and never fills inventory. See [Unified Runtime
Cells](unified-runtime-cells.md).

> **Compatibility title.** Extraction is the official skill, activity and
> player-facing term. Existing `Excavation*` types and this page slug remain to
> avoid unnecessary serialized/API and link churn.

WV-B2B-ARCH-001 preserves the accepted terrain mutation and exact-once loose
output transaction, but removes Construction self-supply. Soil and Stone work
award the one Extraction skill. A separate Hauling job reserves the loose stack
and delivers it to Construction Site staging or compatible Material Storage.
Construction itself begins only after all staging requirements are satisfied.

## WV-B2B compatibility projection

Generated underground Soil/Stone nodes have stable IDs and quantity-one
generation records. At scenario publication they project to the existing
registered `solid.soil` and `solid.stone` foreground materials over retained
Earth/Stone Backgrounds. The starting chamber and passage omit nodes without
creating excavation output. Existing exact-once excavation remains the runtime
interaction authority; Surface source extraction is not implemented.

Generation version 2 guarantees a real Stone foreground node at `(-4,-8)` on
the founding chamber boundary. It uses the registered `solid.stone` definition
and ordinary adjacent-work path. A Stone-looking natural Background without a
foreground Solid is not excavatable and now produces a specific diagnostic.

## Unified Background and node model

**Approved design; not implemented.** Every resident Surface and Colony cell has
one natural Background. An optional Solid/Resource Node owns finite quantity,
obstruction, required activity/skill/work, physical output and depletion. Soil,
Stone, Seed and Twig sources use this common concept; definitions decide whether
a node blocks movement. Depletion removes the node and reveals the unchanged
Background.

The current Colony Solid material/yield model is the compatibility baseline:
legacy Solids adapt as deterministic quantity-one nodes. Existing exact-once
excavation remains the conservation pattern—prepare output, remove/reduce once,
create physical loose output once. Future normal Solid transactions use the
whole remaining quantity; partial harvesting is definition-specific only.
Generated Nodes never create loose resources merely by existing.

## Generated underground contract

WV-B2A approves an entirely underground Colony: no Sky row, above-ground layer
or generated falling shaft. The recommended initial bounds are `128×128` cells
in four-by-four existing `32×32` chunks. Every cell retains a permanent natural
Background and initially has a Solid except the protected top-boundary entrance
passage and exactly one starting chamber. Existing `solid.soil` is the provisional
Dirt majority and `solid.stone` forms clustered deposits; excavation continues
to create the existing physical Soil/Stone drops and invalidate local navigation.

Sand and Mud remain disabled until supported definitions, yields, art and tests
are approved. The `128×128` Colony, `7×5` chamber and three-cell passage are now
approved; weights remain provisional. See [Procedural World
Generation](procedural-world-generation.md).

## Colony View two-layer world model

The implemented terrain foundation belongs to the underground **Colony View**. Each cell in a resident 32×32 chunk contains exactly one permanent Background Material and zero or one foreground Solid Material. Missing chunks are unavailable rather than silently generated during a query.

The two layers have different responsibilities:

| Layer | Purpose | Mutation rule |
| --- | --- | --- |
| Background | Persistent natural substrate, grip and movement speed | May be prepared; never owns colony function or disappears from a resident cell |
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

Milestone 3.4C exposes ordinary Colony View **Extract**, six-recipe **Build**, and **Cancel designation** tools through the authored player HUD. Single-cell and bounded-drag input still delegates to the compatibility-named registries. Cancellation retains the existing conserving material behavior, and uGUI/developer-IMGUI hit testing blocks designation start or release over interface panels.

Milestone 3.5A adds a read-only hover/drag preview. Excavation cells are amber when the excavation registry reports eligible, red when invalid and muted with an explicit marker when already designated. Background Conversion uses the same pattern with green valid cells and the selected recipe's authoritative construction validation. The preview does not create jobs or terrain objects and clears on tool exit, blocked pointer focus or pause.

Right-click in Excavation mode cancels only an excavation designation under the pointer; right-click in Background Conversion cancels only construction. The click is consumed even when the target is empty, preventing a movement order from dispatching through the tool. Select-mode contextual right-click and the existing Cancel tool remain unchanged.

## Implemented excavation and material handling

Excavate mode creates registry-owned jobs for eligible foreground Solid cells. Autonomous ants select them through the accepted priority/path-cost rules, reserve exclusively, reach a valid adjacent anchor and make incremental progress. Safe interruption retains progress. Completion removes only the foreground Solid and preserves the Background.

The stable job-keyed transaction preflights the complete loose output before
terrain removal. The excavator's inventory and storage availability are
deliberately irrelevant. The current compatibility implementation merges into
partial same-cell stacks and may create another bounded stack; it permits
multiple traversable stacks on a cell. The approved target instead permits one
loose type/stack per cell and therefore requires a deterministic validated
alternate or an uncommitted failure. A failed/unrepresentable output leaves the
node/Solid intact, and retries cannot duplicate yield. Successful commit
preserves Background, refreshes presentation/collision and invalidates local
navigation.

Loose Soil and Stone stacks have stable identity, quantity, cell and provisional maximum stack 10. A selected ant can right-click a drop, reserve an eligible quantity, navigate adjacent and pick it up. Developer Only controls can drop a selected carried quantity at the ant's current valid anchor. Compatible same-cell stacks merge; failed or cancelled transfers preserve material and release reservations.

Foraging means discovering or identifying resources. Extraction means physically removing them and producing loose material. Hauling is the separate pickup/transport activity and may deliver to site staging or Storage. Construction performs work only after staging is complete. A no-storage colony can haul directly to its first Storage construction site.

Material Storage remains physical rather than a global wallet. Each functional Background cell owns one compatible stack and incoming reservation. Autonomous haulers reserve source and destination, carry partial loads through personal inventory, and deposit only after reaching a valid adjacent anchor. Already-carried material produces an owner-only self-deposit task.

## Surface Preparation

Preparation is now one 3.4A resource-backed Background conversion target. It costs a provisional one Soil and four simulation work hours. It does not unlock natural climbing: intact exposed Earth and Stone are already grippable. Prepared Background retains the provisional `1.25x` travel-speed benefit. Final balance remains unresolved.

## Future World View terrain

> **Historical WV-A direction; WV-B1 now implements the bounded foundation.**
> World View is top-down with distinct typed Surface coordinates, a deterministic
> bounded first region and a Surface-local terrain/navigation authority. WV-B1
> fixed the development dimensions, cell scale, future partition seam,
> obstacle/diagonal rules and authored composition. WV-B2A now specifies the
> generated terrain contract; Surface mutation and expansion remain future work.

The first surface slice must be a small playable area around a known nest entrance. It should prove surface presentation, traversal, a narrow collection/return loop and persistent state before additional chunks or environments are introduced. Read-only queries must not generate missing surface chunks or treat unloaded space as empty, matching the existing conservative availability rule without requiring both views to share the same tile layers.

The first stable `EntranceId` maps one typed Colony endpoint to one typed Surface endpoint and has transition capacity one. Endpoint blockage invalidates approach/arrival and leaves the ant source-resident. Entrance construction, relocation, multiple entrances and terrain changes that would remove an endpoint remain deferred beyond the first slice.

## Not implemented

- Procedural generation runtime, durable chunk streaming, or save/load. WV-B2A
  specifies future generation but does not implement it.
- World View surface terrain, nest-entrance transitions and cross-view terrain persistence.
- Resource crafting, demolition, rooms and door construction.

See [World View Foundation](world-view-foundation.md), [Ant Movement and Navigation](ant-movement-navigation.md) and [Colony Construction and Rooms](colony-construction-rooms.md).
