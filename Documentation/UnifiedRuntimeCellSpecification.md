# Unified Runtime Cell Architecture and Migration Specification

## WV-B3D.1 Placement migration clarification — 19 October 2026

The detailed implementation sequence and authority-transfer rules are in
[`PlacementMigrationTechnicalSpecification.md`](PlacementMigrationTechnicalSpecification.md).
WV-B3D.1 changes no runtime writer. A future scenario-local Placement registry
will own completed Placement identity, footprint, lifecycle and revision while
function ledgers remain single-owned by Placement ID. The Colony adapter will
observe that registry; it must never keep a legacy functional Background and a
replacement Placement active for the same function/cell. Initial implementation
is one-cell only, but the anchor/footprint contract supports future multi-cell
identities. `WV-B3B-LIM-001` remains active.

## WV-B3C.3B committed mutation integration — 19 October 2026

**Status: Complete.** The user confirmed all **8/8 authoritative manual
acceptance groups** after the passing automated suites. Codex did not
independently perform those checks or rerun Unity for this documentation-only
closure.

Whole-node Colony Extraction now publishes through the existing authority
events: guarded Solid removal advances terrain/navigation state, explicit Node
depletion advances Node and registry revisions, and each created or merged
ground stack advances loose-resource cell state. The Colony runtime adapter
continues to own only its read-model revisions/change feed; no second regional
change authority was added. Failed preparation and replay publish no mutation.

Prepared stack destinations remain invisible until commit. The complete plan is
reserved and revalidated before any terrain change. If capacity is unavailable,
the Node remains interactive-but-blocked with its quantity and Solid intact.
Surface cells remain generation-only and `WV-B3B-LIM-001` remains retained.

**Milestone:** WV-B3A  
**Date:** 15 October 2026  
**Status:** WV-B3B and WV-B3C.1 complete; later mutable migrations unstarted
**Implementation boundary:** Read-only adapters, revisions, compatibility validation and atomic read-model publication

## 1. Purpose and authority

This document is the authoritative technical contract for migrating Colony View
and World View to one conceptual runtime cell architecture without replacing
their local navigation, movement, rendering, cameras or entrance transaction.
It refines the approved unified-cell model in the GDD and procedural generation
specification. Where older technical proposals conflict with this document,
this document controls the WV-B3 migration. WV-B3B implements the read-only
foundation, WV-B3C.1 established the Node seam, WV-B3C.2 completed canonical
stacks/Hauling transactions, and WV-B3C.3A now activates variable Colony Node
quantity ownership. The resource-specific end-state is governed by
[`ResourceAndNodeSystem.md`](ResourceAndNodeSystem.md). Whole-node Colony
Soil/Stone Extraction is complete through WV-B3C.3B. WV-B3D, WV-C1 and their
later gameplay migrations remain unimplemented.

The migration must preserve the working resource chain:

`Node → loose ground resource → ant inventory → Construction staging / Storage → consumption`

Extraction creates physical loose output. Hauling alone transports it.
Construction performs work only after every requirement is staged. Existing
`Excavation*` names remain compatibility names for Extraction until a separately
validated API migration.

### Status vocabulary

- **Approved architecture:** required end-state and migration constraints in
  this specification.
- **Current implementation:** behavior found in the current C# and Unity
  composition.
- **Compatibility implementation:** working behavior retained temporarily while
  its state is adapted to the target model.
- **Proposed implementation:** candidate class/interface shape for a later
  authorised slice; names may change without changing this contract.
- **Future gameplay:** readiness requirement only, not implemented behavior.
- **Unresolved:** a choice that still requires approval or measured evidence.

### WV-B3B implementation record

`RegionCellId`, `RuntimeCellSnapshot`, `IReadOnlyRuntimeCellRegion`, bounded
change feeds, pure compatibility validation and the atomic
`RuntimeCellPublicationRegistry` are implemented. Colony and Surface adapters
read existing authorities without accepting writes. Colony terrain and loose
drops provide exact committed-cell invalidation; current Surface data has no
runtime mutation API, so it changes only on region republication. Generated
nodes retain actual initial quantities and are marked generation-only.

The publication guarantee is deliberately narrow: a fully validated pair
replaces the prior unified read model or neither adapter is published. The
development host itself still assembles several existing mutable services in
place; a later failure is recovered by rebuilding the previous scenario where
`TryStartScenario` supports it. This is not claimed as general persistence-grade
rollback. Unity evidence passes 6/6 focused Edit, 3/3 focused Play, 557/557
complete Edit and 77/77 complete Play. The user confirmed 16/16 manual Unity
acceptance checks passed, completing WV-B3B.

### WV-B3B-LIM-001 — Whole-World Publication Atomicity

WV-B3B atomically replaces the validated Colony/Surface unified read-model pair.
It does not transactionally construct or publish the entire gameplay scenario.
Mutable terrain services, ant registries, inventories, jobs, entrance and
residency continue to be constructed in place by the development host; a later
failure uses the existing rebuild/restore pathway. This accepted limitation is
not a WV-B3B blocker. Reassess it before persistence, region unloading or
complex scenario restoration, and never describe the current boundary as
complete whole-world transaction atomicity.

## 2. Current runtime authority findings

| Concern | Current source of truth | Current projection or adapter | Gap to target |
| --- | --- | --- | --- |
| Colony natural Background and Solid occupancy | `ChunkedWorldData` / `WorldChunk` | Tilemap, collision and grid-navigation projections | No node ID, quantity, placement slot, loose slot or mutation version |
| Generated initial cells and nodes | `GeneratedWorldSnapshot` | `CreateColonyWorld`, `CreateSurfaceNavigationWorld`, generated visuals | Records contain mutable-looking remaining quantity but must become immutable initial data after publication |
| Surface obstacles | `SurfaceWorldData` | `SurfacePathfinder`, `SurfaceNavigationMotor`, temporary visuals | Obstacle set is a navigation authority, not a complete mutable cell authority |
| Colony loose Soil/Stone | `WorldMaterialDropRegistry` | HUD and unified read adapter | WV-B3C.2A owns one canonical typed stack per cell with scenario-local ID, quantity, reservation, capacity and revision; legacy `Vector2Int` remains only at compatibility caller seams |
| Material Storage | `MaterialStorageWorld` | Functional Background plus HUD | Slot/content authority is separate from placement occupancy |
| Construction | `ConstructionDesignationRegistry` and cell-owned `ConstructionSite` | Background-conversion recipe and `ConstructionWorldTransaction` | Completion replaces natural Background instead of creating a Placement |
| Survival functions | `SurvivalInteractionWorld` | Functional Background definitions | Function and natural Background are coupled |
| Ant identity/inventory | `AntRecord`, `AntRegistry`, `AntInventory` | `AntEntityBinding`, HUD and controllers | Correct independent authority; retain |
| Region residency | `AntRegionResidencyRegistry` | local motors and `TwoViewWorldCoordinator` | Correct typed, revisioned seam; retain |
| Colony navigation | `GridNavigationWorldService` over `ChunkedWorldData` | map, scheduler and motors | Retain as a region-specific projection |
| Surface navigation | `SurfaceWorldData` / `SurfacePathfinder` | Surface motor | Retain separately; rebuild/invalidate from cell changes later |
| Time | `SimulationClockController` | all loaded simulation consumers | Correct shared authority; retain |

`AntGameDevelopmentWorldHost` is the current composition and scenario-publication
boundary, not a permanent global world database. `TwoViewWorldCoordinator`
builds Surface navigation/presentation from the generated snapshot and switches
view context; camera switching does not generate terrain. `PlayerHudController`,
development inspectors, Tilemaps and temporary Surface GameObjects are read
models. They must never become cell authorities.

The principal duplication risk is generated node state coexisting with a
projected Colony Solid or Surface obstacle/source marker. After runtime
publication, generated records are provenance/initialisation input only. They
must not accept gameplay mutation or answer current-quantity queries.

## 3. Unified conceptual cell contract

Every resident cell has exactly these logical slots:

| Slot | Cardinality | Meaning |
| --- | --- | --- |
| Natural Background | exactly one | Persistent natural environment |
| Solid / Resource Node | zero or one | Finite natural occupant, optionally obstructing |
| Construction / Placement | zero or one | Built or authored functional occupant |
| Loose ground-resource stack | zero or one | Recoverable physical resource of one type |

These are data layers, not a requirement for four Unity Tilemaps.

Examples:

| Cell | Background | Node | Placement | Loose stack |
| --- | --- | --- | --- | --- |
| Untouched underground Soil | Soil | Soil node | none | none |
| Excavated tunnel | Soil | none | none | none |
| Stone deposit | compatible natural Background | Stone node | none | none |
| Surface Grass | Grass | none | none | none |
| Surface Seed source | Grass | Seed node | none | none |
| Material Storage | Soil | none | Storage | none |
| Storage with contents | Soil | none | Storage owning contents | none |
| Excavated material | Soil | none | none | Soil stack |

### 3.1 Region-scoped identity

A cell key is `(RegionId, typed local cell)`. Public APIs must preserve
`ColonyCell` and `SurfaceCell` types. A discriminated `RegionCellId` may contain
one of them, but no API may reinterpret a Colony `(x,y)` as a Surface `(x,y)`.
Signed Colony chunk coordinates and the bounded Surface origin remain intact.

Each authoritative occupant has its own stable ID. A cell ID is not a Node,
Placement or Stack ID; an occupant may be removed while the resident cell and
Background continue to exist.

### 3.2 Central invariants

The runtime cell mutation boundary enforces, in one place:

1. Every resident cell has exactly one valid natural Background.
2. Removing/depleting a Node preserves its Background.
3. Every Node has one stable ID, definition, location and non-negative quantity.
4. Node quantity changes commit exactly once under a stable operation ID.
5. Normal Construction requires no incompatible Node, Placement or loose stack.
6. Placement creation never replaces the natural Background.
7. A ground stack has one resource type and positive quantity within capacity.
8. A cell has no more than one authoritative ground stack.
9. Same-type output may merge only within the destination stack's capacity.
10. Storage contents belong to the Storage Placement.
11. Construction staging belongs to its Construction Site.
12. Staged or stored resources are not ground stacks.
13. One physical quantity has exactly one authoritative owner at a time.
14. Region coordinates remain typed and region scoped.

Subsystems request mutations; they do not each reproduce compatibility logic.
A central compatibility policy evaluates definitions and occupants and returns
a typed success/failure reason. Navigation and rendering receive immutable cell
views and change notifications, never mutable occupant references.

### 3.3 Compatibility policy

The default safe rules are:

- natural Background always coexists with every other slot;
- a normal obstructing Node excludes a Placement and ground stack unless that
  Node definition explicitly approves a special interaction;
- a normal Placement requires the Node slot clear;
- a normal ground stack requires an accessible, non-hazardous output location
  and no incompatible Placement;
- a Storage Placement's internal contents do not occupy the ground-stack slot;
- a Construction Site is the in-progress state that reserves the future
  Placement slot, and its staging is site-owned;
- an entrance is a specialised linked feature and is not inferred from a
  generic Node or loose stack.

Specific coexistence exceptions must be declared by definitions and validated
by the same policy. Callers cannot bypass the policy with direct dictionary or
Tilemap writes.

## 4. Runtime cell authority

### 4.1 Recommended ownership design

Extend `ChunkedWorldData` into the Colony backing store behind a shared,
region-scoped cell-access contract; do not replace it. Add a Surface backing
store implementing that same contract. A small world/region registry resolves a
`RegionId` to exactly one store. The registry is routing and lifecycle
authority; each region store owns its resident cells and occupants.

Candidate narrow contracts for a later implementation are:

```csharp
public interface IReadOnlyRuntimeCellRegion
{
    RegionId RegionId { get; }
    bool TryGetCell(RegionCellId cell, out RuntimeCellView view);
    long RegionRevision { get; }
}

public interface IRuntimeCellMutationService
{
    CellMutationResult TryCommit(CellMutationRequest request);
}
```

`RuntimeCellView` is an immutable snapshot containing Background identity,
optional occupant summaries and cell revision. `CellMutationRequest` carries an
operation ID, expected revisions, preconditions and intended ownership changes.
This is not a generic event-sourcing framework: it is a narrow atomic boundary
for current game operations.

### 4.2 Authority, snapshot and projection

- **Authority:** the one region store plus its owner-specific ledgers for cell
  occupants and transactions. Gameplay writes occur only here or through an
  atomic participant invoked by its mutation service.
- **Snapshot:** immutable initial generation data or eventual save data. It can
  construct authority but never remains a competing mutable owner.
- **Projection:** navigation maps, Tilemaps, colliders, temporary Surface
  visuals, HUD summaries and inspector text. Projections may cache a source
  revision and rebuild/invalidate, but cannot accept authoritative writes.

Cell revision increments for an occupant or Background mutation. Region
revision increments once per committed transaction batch. Occupants also carry
their own revisions where reservation/quantity checks require them. Failed or
replayed requests do not increment revisions.

### 4.3 Atomic mutation behavior

Every mutation follows: resolve typed region → read expected state → validate
compatibility and capacity → prepare every participant → commit all ownership
changes once → increment versions → emit one change set. A rejected prepare
changes nothing. A commit replay returns the recorded terminal result. No
renderer, navigator or camera callback participates in the authoritative
commit; projections update afterward.

## 5. Natural Background authority

A Background record contains a stable definition ID and region applicability.
Generation supplies the initial value. Background remains resident through Node
extraction, loose pickup, construction, Placement removal and view switching.
Only an explicit natural-terrain transformation may mutate it, using a validated
definition transition; construction is not such a transformation.

Colony navigation continues deriving grip, prepared speed and support from
Background definitions plus Solid/Node geometry. Surface presentation continues
deriving Grass/Water appearance and hazards from Surface Background definitions.
The same cell contract does not make those rules identical.

### 5.1 Functional Background migration

Current Sleeping, Nursery, Food/Water/Material Storage and other functions are
working compatibility representations. Migration must be one function type at
a time:

1. Characterise current recipes, designation states, construction progress,
   skill XP, capacity, contents, reservations, survival behavior, room scoring
   inputs, presentation and fixtures.
2. Add a read-only compatibility projection that exposes one legacy functional
   Background as one Placement view while the legacy owner remains authoritative.
3. Prevent the new Placement executor from activating for an adapted legacy
   cell. One cell must never run both legacy and new function logic.
4. Atomically create the Placement record and transfer contents, capacity,
   reservations and stable references; keep the natural Background unchanged.
5. Switch readers and writers to the Placement authority, then disable the
   legacy function path for that cell.
6. Retire the adapter only after automated, serialized-reference and manual
   proof.

The end state stores function/capacity/contents on the Placement. Existing
construction recipes remain provisional data; WV-B3A invents no costs.

## 6. Solid / Resource Node authority

Each runtime Node contains:

- stable Node ID and definition ID;
- typed region cell;
- initial and remaining quantity;
- obstruction and interaction-anchor rules;
- base work duration/quantity per operation;
- Extraction skill requirement and work modifier rules;
- output resource ID and output quantity;
- depletion behavior and revision.

The definition owns static rules; the Node instance owns identity, location,
remaining quantity and revision. Soil, Stone, Seed, Twig and future natural
resources use the authoritative Extraction skill. Direct eating/drinking is a
separate source-consumption transaction and must not masquerade as Extraction.

Active generated Colony Solids map to deterministic variable-quantity Nodes;
the Mara/Pip/Tala fixture retains quantity-one compatibility. Stable IDs derive
from generation IDs when available, otherwise from scenario/version, region and
cell. The Solid material definition continues supplying collision and
excavatability while the Node registry owns quantity. Generated Surface node
records remain `GenerationOnly` and do not initialise a mutable runtime Node.

Extraction reserves an available quantity, performs work, prepares recoverable
loose output, then atomically reduces Node quantity and creates/merges output.
At zero it removes only the Node slot. It never writes inventory, Storage or
Construction staging.

## 7. Loose-resource authority

One region-scoped ground-stack registry, integrated with cell occupancy, owns
each stack's stable ID, cell, resource type, quantity, capacity, reservations
and revision. One cell has at most one record. Existing multiple-key same-cell
merge behavior is a compatibility mismatch and must not be copied into the new
authority.

Other physical owners remain distinct:

| Owner | Authoritative state |
| --- | --- |
| Ground | runtime cell stack record |
| Ant | `AntInventory` on `AntRecord` |
| Construction | site staging ledger |
| Storage | contents ledger on Storage Placement |
| Future Refining | station input/output staging ledgers |

### 7.1 Failed output placement

Extraction first tries the interaction/source cell if compatible. If it cannot
hold the output, evaluate a deterministic bounded candidate sequence by:

1. increasing local path/adjacency cost from the source interaction area;
2. stable cardinal then diagonal direction order;
3. typed coordinate order as the final tie-break.

Candidates must be resident, accessible under the owning region's navigation
rules, non-hazardous, outside protected entrance endpoints, compatible with
occupants, and able to merge or accept the complete output without changing its
quantity. The initial implementation should inspect a small approved radius
and cap candidate count; exact bound remains an implementation decision for
WV-B3C.

If none succeeds, retain a pending output transaction containing operation ID,
Node ID/revision, reserved quantity, resource/output quantity and candidate
policy version. The Node quantity remains uncommitted and unavailable to other
operations only through its reservation. Retry on relevant cell/stack revision,
explicit recovery or restored lifecycle state. Never discard output, reduce its
quantity, or insert it into inventory automatically.

## 8. Construction / Placement authority

A Placement has stable ID, definition ID, typed cell, lifecycle/state revision
and definition-owned compatibility/function rules. Function-specific state—such
as Storage contents—belongs to that Placement or a component ledger keyed by its
ID, not to the natural Background.

The preserved lifecycle is:

`Designation → requirements → Hauling → site staging → Construction work → Placement`

- A designation reserves the future Placement slot but does not create its
  function.
- The site owns requirements, incoming reservations, staged quantities and work
  progress.
- Hauling alone moves ground/carried resources into staging.
- Construction is ineligible until every requirement is staged.
- Completion atomically consumes staging once and creates one Placement while
  preserving Background.
- The completed Placement begins with transferred/initial function state exactly
  once.

Cancellation before staging releases reservations and the future slot. Partial
or full staging is recovered through prepared ground outputs; if every recovery
output cannot be represented, cancellation remains pending with site ownership
unchanged. Work interruption preserves progress and staging. Completed removal
is a separate exact-once operation: validate removal rules, prepare recoverable
contents/materials if approved, deactivate function, remove Placement and then
publish changes. Recipes, refunds and removal yields remain unresolved and must
not be invented during migration.

## 9. Shared transaction and reservation integration

Existing prepare-before-commit and callback-based transfers are retained behind
narrow adapters. Do not broadly rewrite `ExcavationWorldTransaction`,
`WorldMaterialDropRegistry`, `AntInventory`, `MaterialStorageWorld`,
`ConstructionSite` or `MaterialHaulingOrder` in one slice.

| Transfer | Prepared source | Prepared destination | Atomic commit |
| --- | --- | --- | --- |
| Extraction | Node reservation and expected revision | compatible stack capacity/cell | decrement Node; create/merge stack |
| Pickup | stack quantity reservation | ant inventory capacity | add inventory; decrement/remove stack |
| Delivery to site | ant inventory quantity | site incoming reservation | add staging; remove inventory |
| Delivery to Storage | ant inventory quantity | Placement storage capacity | add contents; remove inventory |
| Construction completion | full staged ledger | compatible Placement slot | consume staging; create Placement |
| Cancellation recovery | staged/content ledger | all required output cells/stacks | create outputs; clear source owner |
| Stack merge | source stack/operation quantity | compatible target capacity | increase target; remove/decrease source |

Every request uses a stable operation ID; reservations use a distinct stable
reservation ID and owner. Source quantity, destination capacity, expected
revisions and terminal result are recorded. Commit is idempotent. Rollback
releases only prepared reservations and leaves quantity at its current owner.

Pause freezes movement, work, transaction retry/backoff and any future expiry.
Interrupted travel leaves quantity at source or carrier according to the last
committed transfer. Ant inactivity releases uncommitted reservations only after
a bounded recovery policy; carried inventory is not erased. Region unload and
save/load are deferred, so no implementation may pretend an unloaded commit
occurred.

## 10. Colony and Surface integration

Shared cell authority means shared occupancy vocabulary and mutation safety,
not shared world geometry.

### Colony View retains

- signed `ColonyCell` coordinates and `32×32` chunks;
- side-on Solid/Node blocking and body-clearance rules;
- Background grip, prepared speed, floors/walls/ceilings and gravity;
- `GridNavigationWorldService`, shared map/scheduler and motor;
- adjacency-based Extraction and construction anchors;
- Colony Tilemap/collider projections.

### World View retains

- bounded `SurfaceCell` coordinates for the current `64×64` region;
- top-down obstacles, diagonal A* and no-corner-cut rule;
- Grass/Water presentation and Surface-local motor/camera;
- future resource interaction, construction and Fog of War projections.

Surface obstacle state becomes a derived navigation projection of Background,
Node and Placement definitions. Colony navigation remains a separate derived
projection. The cell change set invalidates the owning local navigator only.
The WV-B1 entrance, ant residency revisions, representation handoff and shared
clock do not change.

## 11. Foraging and Fog of War readiness

Physical cell state and knowledge are separate authorities. Future interfaces
may read immutable cells to calculate:

- per-ant visibility radius modified by Foraging;
- Surface line of sight and hazards;
- underground tunnel visibility through open/exposed space;
- underground sensing of permitted resource categories through Solid terrain;
- personal discovery events and persistent explored cells;
- colony-shared knowledge publication and resource discovery state;
- pheromone trail references to typed cells/edges.

Knowledge records key by ant/colony plus `RegionCellId` and source revision as
needed. They may hide, reveal or mark stale information; they never create,
remove or mutate a physical Background, Node, Placement or Stack. Numeric radii,
sharing rules, fatigue reduction, sensing occlusion and pheromone behavior remain
future approvals. WV-B3A implements none of them.

## 12. Refining readiness

Refining remains a distinct job and skill. A refining station is a Placement
with recipe/bill state, input staging and physical output preparation. It may
reuse resource definitions, reservation contracts, Hauling source/destination
adapters, work priorities, inventory transfers and exact-once output placement.

The intended flow is:

`Hauling → refining input staging → Refining work → physical output → Hauling`

Manufacturing Glue, components, Fans or Pipes creates physical products; a
later Construction job may install them. Refining must not create a Placement
directly or consume a Construction Site's staging. Bill modes, recipes, blocked
output behavior and XP balance remain unresolved.

## 13. Procedural generation publication

Version `2` / `wv-b2b.default.002` remains the accepted configuration: one
signed 64-bit seed; `64×64` Surface; `128×128` Colony; `32×32` chunks; `7×5`
initial chamber; three-cell-wide passage; centred Surface entrance; top-centred
Colony exit; deterministic founding-queen spawn; and separate Mara/Pip/Tala
fixture.

The required publication sequence is:

1. Generate a complete immutable initial snapshot from seed/version.
2. Validate all cells, IDs, quantities, entrance endpoints and guarantees.
3. Build private authoritative Colony and Surface runtime-region states.
4. Atomically publish the complete region set and provenance, or retain the
   previous valid scenario unchanged.
5. Initialise navigation, collision, rendering and HUD projections.
6. Initialise ant records/visual bindings, entrance and region residency.
7. Begin the shared simulation only after every required authority is ready.

Failure in steps 1–3 publishes nothing. Failure after the publish boundary must
roll back to the previous complete world or keep simulation disabled with one
diagnostic; no partially active region is allowed. Camera/view switching reads
published regions and never invokes generation. Runtime mutations are never
written back into or overwritten from the initial snapshot.

## 14. Persistence and lifecycle readiness

Future save data must identify schema, generator configuration/seed and region
provenance, then persist either complete authoritative state or a validated
baseline-plus-delta. It requires stable cell, Node, Placement, Stack, ant,
construction-site, job, operation and reservation IDs; occupant/cell revisions;
inventories; staged/stored quantities; pending outputs; and paused/interrupted
job state.

Load reconstructs private region authorities, validates conservation and unique
ownership, reconciles reservations to live owners, builds projections, and only
then publishes. It must not regenerate over saved mutations or count both a
snapshot quantity and a delta. Region unloading, streaming and unloaded
simulation remain unimplemented and require a separate lifecycle contract.

## 15. Controlled migration plan

### WV-B3B candidate — Runtime Cell Authority Foundation

Scope: typed cell IDs and immutable views; region registry; Colony store adapter
over `ChunkedWorldData`; Surface runtime store initialised from generated cells;
central compatibility validation; revisions/change sets; atomic scenario
publication; read-only adapters for existing navigation/rendering.

Do not migrate Extraction, loose quantities or functional Background writes in
this slice. Completion requires one resident Background per cell, no duplicate
published region, unchanged Colony/Surface navigation and view/entrance behavior,
and the full existing regression suites plus manual scenario/view checks.

### WV-B3C candidate — Node and Loose Resource Integration

Scope: Resource Definitions; variable-quantity runtime Node instances; active
Colony Solid projection; later separately gated Surface Nodes; one-stack-per-cell
authority; deterministic multi-cell overflow, pending-output recovery; narrow
adapters for Extraction, pickup and Hauling.

Completion requires exact conservation under success, replay, cancellation and
failed placement; Background preservation; Surface and Colony typed identity;
and proof that generated records no longer own mutable quantity.

#### WV-B3C.3A final status — 19 October 2026

**Complete — Automated and Manual Acceptance Passed.** The Colony
adapter now reads its Node projection from the active scenario-local registry,
including Node/resource identity, initial and remaining quantity, Node revision,
lifecycle and Extraction status. Soil 8–12 and Stone 5–15 quantities are derived
independently per generated Node. `ChunkedWorldData` still owns the Solid and
Background; guarded terrain writes prevent the quantity authority being
bypassed.

Committed Node authority changes publish one region-scoped Node change. Failed
and replayed changes, reads and view switching publish none. Fixture
quantity-one Nodes remain interactive. Generated variable Nodes can be
designated and inspected but the legacy one-unit transaction rejects before
terrain, quantity, output or completion XP changes. Surface records remain
`GenerationOnly`.

Focused Unity evidence passes 11/11 Edit and 16/16 Play; complete suites pass
594/594 Edit and 81/81 Play, with no failures, skips or inconclusive results.
The user confirmed all **25/25** authoritative manual Unity checks passed;
Codex did not independently perform them. Evidence under `Logs/WV-B3C3A-*` was
captured from the implementation working tree based on parent revision
`1fab6541`, not from the parent commit itself. At that B3C.3A closure point,
WV-B3C.3B still had to coordinate whole-node depletion and complete physical
output; the completed integration is recorded at the top of this document.

#### WV-B3C.1 implementation status — 16 October 2026

**Complete — Automated and Manual Acceptance Passed.** The
Colony-only inactive registry foundation now builds deterministic quantity-one
Soil/Stone candidate records from prepared terrain. It validates actual Solid
definitions, typed cells, generated/fixture provenance, IDs, Backgrounds and
duplicates; observes only relevant terrain Node changes to invalidate stale
records; and invalidates the complete context on scenario replacement.

This is intentionally not the active Node authority described in section 6.
The WV-B3B Colony adapter still projects `ChunkedWorldData` Solids,
`ChunkedWorldData` remains terrain/effective quantity-one authority, and the
existing Extraction/output transaction remains unchanged. The registry emits
no competing runtime-cell changes and Surface Nodes remain `GenerationOnly`.
B3C.2 and B3C.3 are still required for canonical loose stacks and the atomic
writer/read cutover.

Focused Edit `8/8`, focused Play `4/4`, complete Edit `565/565` and complete
Play `78/78` passed with zero failures, skips or inconclusive results. The user
confirmed all **17/17** manual Unity checks passed. This documentation-only
closure did not rerun Unity.

#### WV-B3C.2A implementation status — 16 October 2026

**WV-B3C.2A — Complete.** Automated validation passed and the user confirmed all
**23/23 authoritative manual acceptance checks passed**. The
existing `WorldMaterialDropRegistry` remains sole mutable authority and now
stores one canonical Soil/Stone stack per resident Colony cell. Controlled
scenario-local `GroundStackId` allocation is separate from exact-once operation
keys. Immutable snapshots expose typed cell/resource identity, quantity,
reserved/available quantity, provisional definition capacity 10, revision,
provenance and lifecycle. Compatible merges preserve identity; mixed type,
overflow, stale identity and invalid or unknown placement fail without a
quantity commit.

The Colony adapter reports this occupant as `Authoritative` and publishes one
loose-resource cell change for each committed creation, merge, reservation,
release, pickup or removal. Reads, failures and camera switches publish none.
Scenario replacement constructs a new context with no old reservations.
WV-B3C.2B now adds scenario-local transfer identity, individually identified
multi-source pickup operations, one exact single-destination delivery, completed
Storage-to-Construction withdrawal, carrier allocation and recovery outcomes
without moving any runtime-cell write authority. Ground and Storage change feeds
still publish only their owners' committed quantity changes. General multi-cell overflow/pending
output and B3C.3B whole-node transaction cutover were unimplemented at this
B3C.3A closure point. B3C.3A
active variable Node authority is complete. Surface Nodes remain
`GenerationOnly`; `WV-B3B-LIM-001` is
unchanged. WV-B3C.2B is complete after user-confirmed **30/30** authoritative
manual Unity acceptance and the recorded green automated suites.

Resource and Node v1.0 further required variable quantities and complete
whole-node output, including several deterministic stacks when one stack could
not hold the yield. The implementation split completed B3C.2A
definitions/canonical stacks, B3C.2B partial Hauling/transfer recovery, B3C.3A
active variable Colony Nodes and B3C.3B whole-node output. B3C.3B uses safe
pre-commit blockage when the complete output cannot fit; it does not use a
recoverable post-depletion pending-output record.

### WV-B3D candidate — Construction / Placement Integration

The detailed target definition, audited legacy inventory and migration matrix
are in [`ConstructionPlacementSpecification.md`](ConstructionPlacementSpecification.md).

Scope: Placement slot and IDs; designation/site slot reservation; atomic
staging-consumption/Placement creation; migration of functional Backgrounds one
type at a time; Storage contents/capacity and survival functions; compatibility
retirement only after proof.

Material Storage should be the first migrated function because it exercises
contents, filters and reservations. Sleeping/survival functions follow only
after Storage migration is accepted. A single all-functions conversion is too
large and is not recommended. WV-B3D may need named sub-slices rather than one
implementation batch.

These milestone names and scopes are recommendations, not automatic approvals.
Each slice must remain independently reversible by retaining its prior adapter
until the replacement passes its gate.

## 16. Acceptance strategy

### Automated matrix

| Area | Required Edit Mode / integration evidence | Required Play Mode evidence |
| --- | --- | --- |
| Cells | one Background; typed IDs; compatibility rejection; revisions; deterministic creation | projections match authority after mutations |
| Nodes | unique stable IDs; exact-once decrement; output/depletion; Background retained; no generated/runtime dual authority | visible Colony and Surface obstruction/source updates |
| Loose resources | one type/stack; merge/capacity; pickup/delivery; failed placement and cancellation recovery | no disappearing/duplicated output through interruptions |
| Construction | placement compatibility; staging; eligibility; exact consumption; cancellation; adapter exclusivity | existing functions, contents, capacity, XP and presentation preserved |
| Cross-region | region-scoped ownership; separate navigators; entrance/residency unchanged; rendering isolation | switch views and entrance round trip without reset/duplication |
| Publication | generation validation and all-or-nothing publish; old scenario preserved on failure | scenario switch creates one complete playable world |
| Regression | complete Edit/Play suites; WV-B1 entrance/rendering; WV-B2B generation/logistics; scheduler fairness; movement/extraction | existing fixture and generated scenario remain usable |

Test totals are evidence recorded per revision, not fixed acceptance numbers.

### Manual gate for WV-B3B

Start fixture and generated seed `2203`; inspect both region Backgrounds; move
ants locally; switch views repeatedly; make an entrance round trip; pause and
change speeds; confirm cameras do not rebuild state; confirm existing Colony
Extraction/Hauling/Construction still work; confirm clean Console.

### Manual gate for later mutable WV-B3C slices

Extract Soil and Stone; inspect Node identity/quantity and Background retention;
verify canonical output stacks and merges; fill/block output candidates and observe a
recoverable pending state; unblock and retry; interrupt pickup/delivery; confirm
quantity conservation; verify Surface nodes remain stable across view switches.

### Manual gate for WV-B3D

For each migrated function, construct it from staged material; inspect natural
Background plus one Placement; use its capacity; preserve Storage contents and
reservations through migration/rebuild; cancel at zero/partial/full staging;
interrupt work; remove only under approved rules; confirm no duplicated legacy
function; run room/survival fixtures and clean Console.

## 17. Legacy retirement proof

No class is removable merely because its name contains `Excavation` or
`Background`. Before deletion or rename:

1. map all source callers and tests with repository search;
2. inspect scene/prefab/asset YAML GUID and serialized enum/value references;
3. inspect reflection, editor setup tools and runtime composition;
4. add characterization tests for the behavior the adapter preserves;
5. migrate live fixture/generated paths and any future save version;
6. pass focused and complete suites plus missing-script/scene validation;
7. complete the slice's manual checklist;
8. prove no cell can activate both old and new authorities;
9. remove in a separate reversible change.

Retain `SampleWorldGenerator`, Mara/Pip/Tala, movement prototypes and accepted
fixtures while they supply unique regression coverage. Retire compatibility
enum values and overloads only with an approved versioning policy.

## 18. Remaining technical decisions

The architecture does not require these answers before WV-B3B foundation work,
but later slices do:

1. final names and assembly placement for the region registry and mutation API;
2. exact WV-B3C alternate-output radius/candidate cap and accessible-cell rule;
3. whether stable generated IDs are stored verbatim or mapped through a versioned ID service;
4. per-definition legal Node/Placement/Stack coexistence exceptions;
5. Placement removal/refund recipes and failure behavior;
6. reservation recovery for death, unload and save/load;
7. knowledge-sharing, visibility, sensing and pheromone numbers;
8. Refining bill modes, recipes and blocked-output policy;
9. persistence format and compatibility policy.

None of these may be silently decided by a migration implementation.

## 19. Recommended next authorised milestone

The smallest safe WV-B3B is authority foundation only: introduce typed
region-scoped cell read contracts, revisions/change sets, central compatibility
validation and all-or-nothing publication; adapt existing Colony and Surface
stores read-only; keep every existing gameplay mutation on its current owner.
Stop after proving projections, navigation, entrance/residency, fixtures and
generated seed `2203` behave unchanged. Node quantity, loose-stack and Placement
writes belong to later separately approved slices.
