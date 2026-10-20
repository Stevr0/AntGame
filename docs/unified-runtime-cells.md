# Unified Runtime Cells

## WV-B3D.3B direct-completion snapshots

Newly completed Storage cells now resolve function from the authoritative
Storage Placement plus its delegated slot, not from a functional Background.
The snapshot exposes Placement identity/lifecycle/revision, delegated slot
state, the Placement visual-definition ID and resolved visual state. Unknown HP
selects the completed visual state; this is presentation metadata only and does
not invent durability, capacity or gameplay authority. One committed completion
publishes after the Placement, slot and closed Site state exist. Blocked or
rolled-back attempts publish no partial Placement-cell mutation.

Automated coverage passed **615/615 Edit** and **84/84 Play**, including three
fault/replay checks. WV-B3D.3B is complete after the user confirmed **43/43**
authoritative Unity Play Mode checks; Codex did not independently perform them
or rerun Unity during this documentation-only closure.

## WV-B3D.3A delegated Storage snapshots

**Complete after 29/29 user-confirmed Unity manual checks.** The retained
automated suites passed with zero failures, skips or inconclusive results; Codex
did not rerun Unity or independently perform the manual checks for closure.

Promoted fixture cells expose authoritative Placement identity plus
`DelegatedMaterialStorage` function authority. The same snapshot projects the
single slot's revision, filter, resource, quantity, available capacity and
reservation totals without owning them. Slot commits emit one Placement-cell
change; reads are inert. Legacy/new Storage reports `LegacyBackground`, and old
handles are invalid after scenario replacement.

## WV-B3D.2 Placement snapshots

Colony snapshots now resolve Placement identity through the scenario registry
and expose definition, context, anchor, footprint, authority/lifecycle,
revision, nullable current/max HP and condition. Legacy projections expose no
Placement-owned contents/capacity and report their obscured natural Background
and HP as unknown. One Background conversion produces one combined
`Background | Placement` change; reads and camera switches produce none.
Scenario replacement retires old handles. Existing Node, stack, Construction
Site and function owners remain authoritative.

## WV-B3D.1 Placement migration contract

The [Placement Migration](placement-migration.md) documentation gate is
complete. A future scenario-local registry will own Placement identity,
footprint, lifecycle and revision; function ledgers will be keyed by Placement
ID without duplicating Storage or reservation state. Initial implementation is
one-cell only and read-only legacy projections come before any authority
cutover. No Placement runtime was added by WV-B3D.1; the later WV-B3D.2 registry
implements only the read-model foundation described above.

## WV-B3C.3B committed changes

**Complete.** Automated validation passed and the user confirmed all **8/8
authoritative manual acceptance groups**. Codex did not independently perform
those checks or rerun Unity for this documentation-only closure.

Whole-node Extraction now drives the existing Colony terrain, Node and loose
stack events. The runtime-cell adapter observes those authorities and publishes
their cell changes; it owns no gameplay quantity. Prepared output is invisible,
failed plans publish no mutation, and Surface Nodes remain generation-only.

## WV-B3C.2A canonical ground stacks

**Complete.** Automated validation passed and the user confirmed all **23/23
authoritative manual checks passed**. This documentation-only closure did not
rerun Unity tests.

The Colony loose occupant now reads one canonical `WorldMaterialDropRegistry`
stack rather than aggregating operation-keyed records. Each active stack exposes
its scenario-local ID, typed Colony cell, Soil/Stone resource ID, quantity,
reserved and available quantities, provisional definition capacity 10,
revision and provenance. Compatible commits merge under one identity; mixed
types, overflow, obstructing Nodes, completed functional Placements and unknown
runtime compatibility are rejected before source consumption by ordinary
producers. The explicit inventory-drop command keeps its validated legacy
functional-Background anchor behavior until Placement migration.

Creation, merge, reservation, release, pickup and removal publish one affected
cell change. Reads, failed transactions and view switching do not. A scenario
rebuild creates a new ID context and drops old reservations. Hauling, Storage
and staged Construction retain their current authorities. B3C.2B coordinates partial transfer identity,
allocation and recovery without becoming a cell writer. General overflow and
B3C.3B whole-node transaction cutover is implemented for Colony Soil/Stone;
Surface Nodes remain
`GenerationOnly` and `WV-B3B-LIM-001`
continues to apply.

## WV-B3C.1 inactive Node foundation

WV-B3C.1 is **complete** after automated validation and user-confirmed **17/17
manual Unity acceptance**. Each prepared Colony scenario gets an inactive diagnostic
registry containing deterministic, typed, quantity-one records for registered
Soil and Stone Solids. Relevant terrain mutation invalidates only the matching
record; scenario replacement invalidates the old registry handle; reads and
camera switches do not advance revisions. Developer Only → Debug exposes
bounded registry and selected-Node diagnostics.

This is not an authority cutover. `ChunkedWorldData`, the existing Extraction
transaction and `WorldMaterialDropRegistry` remain authoritative. The registry
cannot change quantity, terrain, jobs, output, navigation or cell change feeds.
Surface Stone, Seed, Twig and Freshwater remain `GenerationOnly`. Automated
results pass 8/8 focused Edit, 4/4 focused Play, 565/565 complete Edit and 78/78
complete Play. This documentation-only closure did not rerun Unity tests. B3C.2A later
completed and B3C.2B later completed with **30/30** manual acceptance. B3C.3A
subsequently activated Colony quantity authority and completed **25/25** manual
acceptance; B3C.3B was unstarted at that closure point and is now implemented as
recorded above. `WV-B3B-LIM-001` remains active.

## WV-B3C.3A active Colony Node authority

Generated Colony Soil/Stone snapshots now come from the active registry and
include resource identity, initial/remaining quantity, Node revision, lifecycle
and Extraction status. Soil ranges 8–12 and Stone 5–15 through stable per-Node
derivation. Guarded terrain writes prevent Solid removal from bypassing
quantity. One committed authority mutation produces one Colony Node change;
reads, failures, replay and view switching produce none.

Quantity-one fixture Nodes remain interactive. Variable Nodes were designatable
but unowned at B3C.3A closure; B3C.3B now makes them interactive through
full-quantity output.
Automated suites pass 11/11 focused Edit, 16/16 focused Play, 594/594 full Edit
and 81/81 full Play, with no failures, skips or inconclusive results. The user
confirmed **25/25** authoritative manual Unity checks passed; Codex did not
independently perform them. The XML evidence under `Logs/WV-B3C3A-*` came from
the implementation working tree based on parent revision `1fab6541`.

## Status

WV-B3B is **complete**. Automated validation passed and the user confirmed
**16/16 manual Unity acceptance checks passed**. Typed read contracts, immutable
snapshots, revisions/change feeds,
pure compatibility checks and atomic read-model publication now exist. Current
gameplay authorities remain unchanged. The later WV-B3C authority/loose-resource
cutover, WV-B3D and WV-C1 require separate approval. The authoritative detail is
[`Documentation/UnifiedRuntimeCellSpecification.md`](https://github.com/Stevr0/AntGame/blob/main/Documentation/UnifiedRuntimeCellSpecification.md).

## Cell model

Every resident Colony or Surface cell has exactly one natural Background and
may contain zero or one Node, Placement and one loose ground-resource stack.
The layers are logical data, not a requirement for separate Tilemaps.

The approved [Resource and Node System](resource-and-node-system.md) additionally
requires every Solid to be a finite variable-quantity Node, whole-node
Extraction and complete recoverable physical output. Natural Backgrounds own no
colony function; current functional Backgrounds are compatibility only.

| Layer | Owns | Key invariant |
| --- | --- | --- |
| Background | Natural terrain/environment | Always present; survives extraction and construction |
| Node | Finite natural resource and obstruction | Stable ID and exact-once quantity |
| Placement | Built/authored function | Preserves Background; at most one per cell |
| Loose stack | Recoverable ground resource | One type and one stack per cell |

Storage contents belong to a Storage Placement. Construction staging belongs to
the Construction Site. Neither is an independent ground stack. One quantity can
have only one authoritative owner.

## Authority boundary

The recommended design keeps `ChunkedWorldData` as the Colony backing store
behind a shared read contract and adds a Surface store behind the same contract.
A region registry routes typed `ColonyCell` and `SurfaceCell` requests. A central
mutation service validates compatibility, expected revisions, reservations and
capacity before one atomic commit.

Generated snapshots initialise runtime regions and then become immutable
provenance. Navigation maps, Tilemaps, colliders, temporary Surface visuals, HUD
and inspectors are projections. There must never be two mutable copies of Node
quantity, Placement occupancy or a ground stack.

## Preserved gameplay flow

`Node → ground stack → ant inventory → site staging / Storage → consumption`

- Extraction prepares physical output before reducing the Node.
- Hauling owns pickup and delivery.
- Construction begins only when all site requirements are staged.
- Completion consumes staging once and creates a Placement.
- Output larger than one stack reserves deterministic additional cells.
- Failed complete output placement retains a recoverable pending transaction; it never
  discards output or auto-fills inventory.

Current `Excavation*` APIs, quantity-one Colony Solids, functional Backgrounds,
drop registry, Storage world and construction sites are compatibility seams.
They remain active until their state is transferred and the replacement is
proven.

## Region boundaries

Colony and Surface share occupancy vocabulary, not geometry. Colony retains
signed chunks, side-on support/grip/gravity and grid navigation. Surface retains
its bounded top-down cells, obstacles, diagonal A* and separate motor/camera.
The nest entrance, residency revision and shared clock are unchanged.

Future Fog of War stores knowledge separately from physical cells. Future
Refining uses Placement-owned input staging and physical output, and remains
distinct from Construction installing a manufactured product.

## Generation publication

Version `wv-b2b.default.002` follows this target sequence:

1. Generate immutable initial data.
2. Validate the complete world.
3. Build private authoritative runtime regions.
4. Publish both regions atomically.
5. Initialise navigation/rendering projections.
6. Initialise ants, entrance and residency.
7. Begin shared simulation.

View switching never regenerates. Runtime mutations are never overwritten from
the initial snapshot.

## Accepted limitation: WV-B3B-LIM-001

Atomicity applies to replacement of the validated Colony/Surface unified
read-model pair. It does not cover transactional construction and publication
of every mutable gameplay service, ant registry, inventory, job, entrance and
residency. The development host still constructs these in place and uses its
existing rebuild/restore pathway after a later scenario-build failure. This did
not block WV-B3B acceptance, but must be reassessed before persistence, region
unloading or complex scenario restoration.

## Implemented foundation and candidate migration slices

1. **WV-B3B — authority foundation (complete):** typed read contracts,
   revisions, central compatibility, atomic read-model publication and
   read-only adapters. Automated evidence: 6/6 focused Edit, 3/3 focused Play,
   557/557 complete Edit and 77/77 complete Play; manual acceptance 16/16.
2. **WV-B3C.1 — inactive Colony Nodes (complete):** stable
   quantity-one candidate records and invalidation without moving writers.
3. **WV-B3C.2A — canonical stacks (complete):** Resource Definitions, stable
   stack identity, one stack/type per cell, capacity, reservations and revisions.
4. **WV-B3C.2B — Hauling acceptance recovery (complete):** replay-safe parent
   transfers with bounded multi-source pickup
   operations, completed Storage-to-Construction withdrawal, one final delivery,
   carrier allocation/recovery and stale-scenario rejection. General multi-cell output
   placement remains deferred. See the
   authoritative
   [`Partial Hauling Integration Plan`](https://github.com/Stevr0/AntGame/blob/main/Documentation/PartialHaulingIntegrationPlan.md).
5. **WV-B3C.3A — active Nodes (complete):** variable Colony
   quantity authority with guarded legacy Extraction.
6. **WV-B3C.3B — whole-node Extraction (complete):** coordinated depletion,
   Solid removal and complete recoverable output.
7. **WV-B3D — Placements:** site/Placement occupancy and one-function-at-a-time
   migration of functional Backgrounds, beginning with Material Storage.

These are candidate scopes, not implementation authorisation. Each slice keeps
its previous adapter until focused/full automation and its Unity manual gate
pass. Legacy classes are removed only after source, serialized GUID, reflection,
fixture and save-compatibility proof.
