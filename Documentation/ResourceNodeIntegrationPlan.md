# WV-B3C Resource Node and Loose Resource Integration Plan

**Status:** B3C.1 and B3C.2A are complete after their recorded automated and
user-confirmed manual acceptance. B3C.2B partial Hauling and replay-safe stack
transactions and their acceptance recovery are implemented from
[`PartialHaulingIntegrationPlan.md`](PartialHaulingIntegrationPlan.md) and await
revised manual Unity acceptance after a partial first pass. B3C.3 remains planning only.

**Prepared:** 15 October 2026

**Foundation:** WV-B3A specifies the target architecture. WV-B3B is complete
and supplies typed `RegionCellId` values, immutable snapshots, read-only region
adapters, compatibility validation, revisions/change feeds and atomic
publication of the Colony/Surface read-model pair.

**v1.0 amendment — 16 October 2026:**
[`ResourceAndNodeSystem.md`](ResourceAndNodeSystem.md) is authoritative where
this earlier plan differs. Every target Solid is a finite variable-quantity
Node; normal Solid Extraction commits the complete remaining quantity; output
may require several deterministic physical stacks; Water is finite; and natural
Backgrounds own no colony function. The current quantity-one Soil/Stone registry
and functional Backgrounds are compatibility implementations, not the end-state.
Completed improvements and their migration are defined separately in
[`ConstructionPlacementSpecification.md`](ConstructionPlacementSpecification.md);
no Placement migration belongs to B3C.2 or B3C.3.

**B3C.2B implementation amendment — 16 October 2026:** Partial Hauling uses
exact single-destination runs over the B3C.2A canonical stack authority. One run
may commit several independently identified compatible ground pickups, and
completed Material Storage may supply Construction staging through its existing
withdrawal authority.
The transfer ID is distinct from Stack, job, ant and producer operation IDs;
pickup operation IDs are distinct children; exclusive reservations become
transfer-owned; committed carrier quantity stays in `AntInventory`; replay and
recovery are explicit. Evaluation is bounded and route-cost-aware, and Storage
never supplies Storage. Deterministic prepared
general multi-cell placement remains deferred until a separately approved gate;
B3C.3 has not activated variable whole-node output. The approved 0–100 Hauling
capacity bands and existing shared skill-decay contract apply to carrier space.

## 1. Scope and decision labels

This plan covers the smallest safe migration of Colony Soil/Stone Nodes and
loose ground resources into the WV-B3B cell contract. It is based on read-only
inspection of the current source and tests. No C#, Unity content, test, scene,
prefab, asset or project-setting change is part of this planning task.

Statements are labelled as follows:

- **Approved contract** restates an accepted GDD, procedural-world or unified
  runtime-cell rule.
- **Recommended implementation** is the proposed way to satisfy those
  contracts and still requires an implementation milestone approval.
- **Unresolved decision** must be settled explicitly before the affected slice
  is implemented; it must not be silently chosen in code.

The plan does not implement Surface Extraction, Foraging, Fog of War,
pheromones, Freshwater consumption, persistence, region unloading, Placement
migration or WV-B3D. It does not change Extraction progression, Hauling
destination selection, Construction staging, priorities, scheduler fairness,
movement, navigation, identity/residency, entrance travel, rendering isolation,
the shared clock or procedural seed determinism.

## 2. Current mutable ownership

### 2.1 Cross-cutting owners

| Concern | Current authority | Current behavior and limitation |
| --- | --- | --- |
| Colony natural terrain and Solid occupancy | `ChunkedWorldData` | One Background material and optional Solid material per resident cell. A Solid has no stored instance ID or quantity. Per-cell terrain revision and `CellChanged` are authoritative for terrain writes. |
| Surface bounds and obstruction | `SurfaceWorldData` | Immutable-sized obstacle set used by `SurfacePathfinder` and presentation. It has no Node identity, quantity, revision or mutation API. |
| Generated identity and initial quantities | `GeneratedWorldSnapshot` / `GeneratedResourceNodeRecord` | Deterministic generation provenance. `RemainingQuantity` has no runtime mutation path. Generated Colony identity/quantity is discarded when projected into terrain; generated Surface data is read-only metadata. It must not become a second mutable runtime authority. |
| Loose Colony material | `WorldMaterialDropRegistry` | WV-B3C.2A canonical authority. Owns one typed Soil/Stone stack per cell, scenario-local Stack IDs, quantity, provisional capacity, exclusive pickup reservation, stack revision, provenance/lifecycle and committed operation keys. Runtime instances validate resident terrain conservatively. |
| Unified read model | `ColonyRuntimeCellRegionAdapter` and `SurfaceRuntimeCellRegionAdapter` | Read-only projection. The Colony adapter exposes canonical Stack ID/cell/resource/quantity/reservation/capacity/revision directly and derives cell revisions from committed registry events. The Surface adapter exposes generated Nodes as `GenerationOnly`. Neither adapter owns gameplay mutation. |
| Extraction work | `ExcavationDesignationRegistry`, `ExcavationJobOrder`, `ExcavationWorldTransaction` | The designation/job owns work, exclusivity and progress. At completion the transaction preflights output, removes the whole Solid and commits one loose output under the job ID. Failure leaves progress and terrain recoverable. |
| Pickup and transport | `MaterialPickupOrder`, `MaterialHaulingOrder` | The drop is reserved before pickup. Inventory acceptance occurs before source reduction. Hauling separately reserves its explicit Construction Site or Storage destination and preserves carried material after interruption. |
| Construction material | `ConstructionSite` | Owns requirements, incoming reservations and staged quantities. It is not loose-ground ownership and is outside WV-B3C migration. |
| Stored material | `MaterialStorageWorld` | Owns placement-like slot contents, filters and reservations. It remains outside WV-B3C until WV-B3D. |

### 2.2 Resource-by-resource map

| Resource Node | Existing quantity authority | Stable Node identity | Runtime location | Extraction transaction | Physical output owner | Cell revision and consumers |
| --- | --- | --- | --- | --- | --- | --- |
| Soil | The live Solid is effectively quantity one in `ChunkedWorldData`; the generated quantity-one record is only provenance. | Generated worlds contain a deterministic generated ID, but the live Solid does not retain it. Authored fixtures have only cell/material identity. | Colony `Vector2Int` in terrain; the WV-B3B adapter exposes a typed Colony cell. | `ExcavationWorldTransaction` removes the Solid after output preflight. | `WorldMaterialDropRegistry`, as `material.soil`. | `ChunkedWorldData` supplies terrain revision; the adapter advances its own cell/region revision for terrain and drop events. Colony navigation, clearance, collision/render projections, designations and the development inspector read terrain. Hauling/pickup/Construction read drops. |
| Stone | Colony Stone has the same quantity-one compatibility authority as Soil. Generated Surface Stone has quantity-six metadata by default but no interactive mutable authority. | Generated Colony and Surface records have deterministic IDs; live Colony terrain retains only `solid.stone`. | Colony terrain cell for live Extraction; typed generated Surface location for generation-only Stone. | `ExcavationWorldTransaction` for Colony only; none for Surface. | `WorldMaterialDropRegistry`, as `material.stone`, for Colony output; none for Surface. | Colony uses the same terrain, adapter, navigation, rendering and logistics consumers as Soil. Blocking Surface Stone is projected into `SurfaceWorldData` and exposed by the Surface adapter as `GenerationOnly`, with no runtime revision. |
| Seed | `GeneratedResourceNodeRecord.RemainingQuantity` equals initial quantity but is not an interactive mutable authority. | Deterministic generated ID. | Typed Surface location in the generated snapshot only. | None. | No loose Seed implementation or catalog mapping. | Surface adapter publishes generated metadata as `GenerationOnly`; no mutation/change source. Non-blocking Seeds are absent from `SurfaceWorldData`; presentation reads generated data. |
| Twig | Same generation-only metadata status as Seed. | Deterministic generated ID. | Typed Surface location in the generated snapshot only. | None. | No loose Twig implementation or catalog mapping. | Same generation-only Surface projection and no runtime revision. |
| Freshwater | Generated finite source metadata; water Background/obstruction is separately projected into `SurfaceWorldData`. It is not an interactive quantity authority. | Deterministic generated ID where a Freshwater Node is generated. | Typed Surface location in generated data; water obstruction is a navigation projection, not quantity. | None. Direct drinking is contractually separate from Extraction. | No approved ordinary loose-water representation. | Surface navigation consumes obstacle projection; the runtime-cell adapter exposes generated metadata only. No source mutation/change feed exists. |

### 2.3 Existing duplication and provisional projections

- A generated Colony Node record and a live Solid describe the same natural
  occupant, but only Solid presence changes at runtime. Treating both
  `RemainingQuantity` values as mutable would create forbidden dual authority.
- Generated Surface Node records and `SurfaceWorldData` obstacles are different
  facets: generation metadata versus navigation projection. Neither is a live
  interactive Node authority.
- A Colony unified snapshot aggregates every legacy same-cell drop and marks
  `RepresentsMultipleLegacyRecords`; this is a compatibility projection, not
  proof that the approved one-stack invariant is satisfied.
- `WorldMaterialDropRegistry.GroundCellChanged` lets the Colony adapter advance
  a cell revision, but the drop authority itself cannot currently validate an
  expected cell revision.
- The excavation job ID currently acts as both replay key and possible new drop
  key. Once output can merge into one canonical stack, operation identity and
  stack identity must be separate.

## 3. Recommended Node authority

### 3.1 Ownership boundary

**Approved contract:** a Node definition owns static rules. A Node instance owns
stable identity, typed location, initial/remaining quantity and revision. The
natural Background survives depletion. There is one mutable quantity owner.

**Recommended implementation:** add a private, region-scoped
`RuntimeResourceNodeRegistry` beside the current region authorities. It owns
immutable `RuntimeResourceNodeSnapshot` results and internal mutable records:

- `NodeId`, `DefinitionId` and `RegionCellId`;
- `InitialQuantity`, `RemainingQuantity` and `NodeRevision`;
- definition-derived obstruction, interaction, work, output and depletion data;
- one active extraction reservation, identified by operation ID and quantity.

The registry is the sole mutable Node-quantity authority. `ChunkedWorldData`
continues to own Colony Backgrounds, Solid occupancy, collision inputs and
terrain revisions. While the compatibility Solid exists, its presence is a
required physical projection of an undepleted obstructing Colony Node; it is
not a second quantity store. One coordinator validates and changes both facets.
No caller may independently decrement a Node or remove its projected Solid.

Do not put mutable quantity back into `GeneratedResourceNodeRecord`. Generation
creates immutable initial records; scenario construction copies approved Node
fields into the runtime registry before the read model is published. The
generated snapshot remains provenance and deterministic rebuild input.

### 3.2 Quantity-one Colony migration

For generated Colony worlds, initialise Soil/Stone runtime Nodes using the
existing generated ID, typed location, definition and quantity one. For authored
fixtures, derive a deterministic compatibility ID from a versioned scenario
identity, Colony region ID, cell and Solid material ID. The derivation must be a
named, tested policy; do not use process hashes or dictionary order.

At publication validate a one-to-one relation:

1. every excavatable Soil/Stone Solid has exactly one live runtime Node;
2. every compatibility Colony Node has the expected Solid material at its cell;
3. quantity is one and revision starts deterministically;
4. duplicate Node IDs or cells fail candidate publication;
5. generated records are copied, not retained as writable aliases.

For B3C.1, Colony Soil/Stone remain quantity-one. The implemented inactive
record keeps its captured remaining quantity at one and becomes
`DepletedOrAbsent` when existing Extraction removes the authoritative Solid; it
does not perform the depletion. At the future B3C.3 cutover, the coordinator
will remove the Solid only when authoritative Node quantity reaches zero. This
preserves current gameplay and leaves the schema ready for later multi-quantity
Nodes without changing balance now.

### 3.3 Read-model integration

Extend the Colony adapter to read the Node registry for identity, quantity and
Node revision while continuing to read Background/Solid projection from
`ChunkedWorldData`. Generated-ID lookup in the adapter then becomes unnecessary.
The adapter must fail validation when live Node and Solid projection disagree.

Keep the Surface adapter on `GenerationOnly` records during the smallest
Colony-only delivery. The registry and transaction contracts should accept
typed Surface identities, but no mutable Surface registry should be published
until Surface interaction, navigation invalidation and lifecycle policy are
separately approved.

## 4. Recommended loose-stack integration

### 4.1 Preserve the working authority and callers

**Approved contract:** one ground cell has at most one loose stack and one
resource type. Compatible output merges up to capacity; incompatible or full
output uses a validated destination or does not commit. Pickup and Hauling
remain separate exact-once transfers.

**Recommended implementation:** evolve `WorldMaterialDropRegistry` in place
rather than replace it. Keep its public role for `MaterialPickupOrder`,
`MaterialHaulingOrder`, construction recovery and developer drop actions. Move
its internal index to one canonical record per typed Colony cell:

- stable `StackId` independent of producer operation ID;
- typed `RegionCellId`, resource ID, quantity, maximum capacity and stack
  revision;
- source pickup reservation and prepared output-capacity reservations;
- a separate committed-operation ledger for replay protection;
- per-cell revision plus one post-commit change event.

Compatibility overloads may continue to accept Colony `Vector2Int` while they
immediately convert to `RegionCellId`. Existing callers should not be rewritten
until the new registry behavior passes its gate.

### 4.2 Merge, placement and recovery

An output destination is valid only if the cell is resident, compatible under
the central validator, accessible under Colony navigation rules, outside
protected entrance endpoints, and either empty or holds the same resource with
capacity for its prepared portion. The approved v1.0 whole-node rule permits one
logical operation to reserve several deterministic output cells when the
complete yield exceeds one stack's capacity. Every required reservation must
succeed before depletion commits.

Search order must be deterministic and versioned. First test the source cell,
then a bounded sequence ordered by local path/adjacency cost, stable cardinal
then diagonal direction order, and typed coordinate tie-break. The exact radius,
candidate cap, interaction origin and hazard rule remain unresolved before
B3C.2/B3C.3.

If the complete output set cannot be reserved, preserve the prepared operation
and its Node reservation.
Do not decrement the Node, shrink output, fill inventory, Storage or staging, or
discard material. Retry only after an affected cell/stack revision, explicit
recovery request or lifecycle restoration. Cancellation releases reservations
and leaves Node quantity unchanged.

### 4.3 Legacy record migration

At scenario construction, group existing legacy drops by Colony cell. One
record becomes one canonical stack without changing quantity. Several same-type
records may coalesce only if their total fits one agreed capacity and no source
reservation is live. Mixed types, conflicting capacities, over-capacity totals
or live reservations fail candidate publication and preserve the previous
scenario/read model; they are not silently deleted, split or relocated.

The compatibility API may temporarily resolve an old ownership key to the
canonical stack ID so an already-created pickup/haul plan remains valid within
the same scenario. New producer operation IDs belong only in the replay ledger.
This prevents merged output from creating duplicate physical owners.

### 4.4 Pickup and change publication

Retain the current destination-first pickup rule: validate the stack ID,
reservation owner, quantity and expected stack revision; commit inventory; then
reduce/remove the stack exactly once. Failed inventory acceptance changes
nothing. Cancellation releases only the reservation. Existing Hauling
destination selection and post-pickup carried-material recovery do not change.

A successful add, merge, pickup or removal increments the ground-cell and stack
revision once and publishes one `LooseResource` change for each affected cell.
The unified adapter consumes that authoritative revision/event instead of
manufacturing a drop revision from an unversioned callback.

## 5. Extraction transaction coordination

### 5.1 Stable identities and expected revisions

Separate three identities:

- `JobId` owns work progress and scheduler reservation;
- `NodeId` owns the finite source;
- `ExtractionOperationId` owns one exact-once quantity transfer and replay
  record.

Derive or allocate the operation ID once when completed work first prepares a
transfer, store it with the job/order state, and reuse it on retry. It must not
be regenerated per frame. The operation records Node ID, typed source cell,
quantity, output resource/quantity, policy version, expected Node revision,
expected terrain-cell revision and expected destination stack/cell revision.

### 5.2 Prepare

The shared `ResourceExtractionTransactionCoordinator` should adapt the existing
`ExcavationWorldTransaction` entry point and perform all fallible work first:

1. Return the recorded success for an already committed operation ID; reject a
   conflicting payload under that ID.
2. Validate job completion, Node identity/location/definition, available
   quantity and expected Node revision.
3. Validate the expected compatibility Solid and terrain revision for Colony.
4. Acquire the Node quantity reservation under the operation ID. The existing
   job reservation remains the ant/work claim; it is not the quantity claim.
5. Select a deterministic output cell and reserve complete compatible capacity
   against its expected ground-cell/stack revision.
6. Store an immutable prepared operation. If placement is unavailable, retain a
   recoverable pending operation without changing physical quantity.

Competing preparations may not reserve the same Node quantity or overbook stack
capacity. A changed expected revision invalidates that preparation and forces a
fresh validation with the same operation ID.

### 5.3 Commit

Commit occurs through one coordinator-controlled synchronous mutation boundary:

1. Revalidate the prepared token, reservations and all expected revisions.
2. Apply the Node decrement internally.
3. If depleted, remove the compatibility Solid through an internal authorised
   terrain operation; always retain the Background.
4. Create or merge the canonical ground stack using its reserved capacity.
5. Mark the operation committed, clear its reservations and publish one batched
   change set after all state is consistent.

After prepare succeeds, the apply path must contain no ordinary fallible
callbacks. Internal mutation methods should be callable only by the
coordinator. Node, terrain and loose-stack events are accumulated during the
batch so consumers observe one final `Node | LooseResource` change for the
source cell, plus a `LooseResource` change for a distinct output cell. Terrain
and navigation invalidation still occur when an obstructing Node depletes.

### 5.4 Cancellation, replay and partial failure

- Cancellation before commit releases Node and output-capacity reservations,
  preserves completed work under existing job rules, and changes no quantity.
- Replaying a committed operation returns its recorded result without another
  decrement or output.
- A stale prepared operation is cancelled/reprepared against current revisions;
  it is never force-applied.
- An unexpected exception during the non-fallible apply section records the
  operation as recovery-required and blocks further mutation of its reserved
  Node/cells. Recovery inspects the operation ledger and both authorities to
  finish the missing side or prove rollback safe; it never guesses from output
  quantity alone.
- Persistence and crash-durable journalling remain outside WV-B3C. In-memory
  recovery does not satisfy save/load or whole-world transaction guarantees.

## 6. Future Surface compatibility

The contracts should use `RegionCellId`, resource IDs and region-specific
placement/navigation policies from their first implementation. That allows a
future Surface registry to initialise Seed/Twig/Freshwater Nodes without
changing transaction identity or snapshot shape.

It does not authorise those features now:

- Seed and Twig need approved work/output definitions, loose resource catalog
  entries, capacities, presentation and Surface interaction anchors.
- Freshwater needs an explicit decision between direct source consumption,
  containerised output or another physical representation. Direct drinking is
  not Extraction and must use its own exact-once consumption operation.
- Foraging owns discovery/knowledge, not Node quantity. Fog of War and
  pheromones are knowledge/presentation/recruitment systems, not physical cell
  occupants.
- Surface obstruction changes need a mutable projection and path invalidation
  contract before a blocking Surface Node can deplete.

Until those decisions are approved, all generated Surface Nodes remain
`GenerationOnly`, stable across view switching and absent from gameplay jobs.

## 7. Proposed implementation slices

The three slices should be separate implementation milestones with separate
commits and gates. B3C.1 and B3C.2 establish independently testable authorities;
B3C.3 is the first slice that changes live Extraction behavior. Do not call
WV-B3C complete until all three are accepted.

### B3C.1 — Inactive Colony Node foundation — complete

**Implementation update — 16 October 2026:** Implemented as the inactive,
unpublished authority foundation recommended here. It creates immutable
quantity-one candidate records, deterministic generated/fixture IDs, local
stale/depleted invalidation, context invalidation and bounded inspector
diagnostics. It does not switch the WV-B3B adapter or any gameplay writer.
Focused tests pass 8/8 Edit and 4/4 Play; complete suites pass 565/565 Edit and
78/78 Play. The user confirmed the complete 17/17 manual checklist passed.

**Systems affected:** new Node definition/registry records; generated scenario
construction; candidate-publication validation; narrow read-only inspection in
development tooling. The live Colony adapter does not switch snapshot source in
this slice.

**Interfaces:** immutable Node snapshot/query; deterministic initialization;
build report; registry diagnostics; explicit context invalidation. No mutation,
reservation or competing `NodeChanged` event was added in this slice.

**Mutation ownership:** B3C.1 builds the schema for a future sole quantity owner but keeps it
inactive and unpublished while existing excavation remains the live writer.
`ChunkedWorldData` therefore remains the effective quantity-one compatibility
authority until B3C.3 performs one atomic writer/read cutover. The recommended
safe delivery is deterministic initialization plus parity validation behind a
disabled mutation path. The WV-B3B adapter continues its current Solid
projection, so an intervening excavation cannot leave a published Node snapshot
stale and there are never two active mutable quantity stores.

**Compatibility adapter:** quantity-one Soil/Stone Solid mapping, generated IDs
where available and versioned fixture IDs otherwise. Surface stays
`GenerationOnly`.

**Automated tests:** deterministic IDs; one-to-one Solid/Node validation;
quantity-one snapshots; duplicate/mismatch rejection; generated data cannot
mutate runtime state; publication failure retains the prior pair; no change
events on reads/view switches.

**Manual acceptance:** fixture and seed `2203` show the same Soil/Stone cells,
Backgrounds, visuals, collision and paths; repeated view switches and entrance
travel do not recreate Nodes; clean Console.

**Rollback:** remove the unpublished/feature-gated registry and retain the
existing WV-B3B Solid adapter. No save migration or gameplay quantity has moved.

**Unresolved before implementation:** class/assembly names; fixture scenario-ID
source and ID version; the exact feature gate and parity diagnostics used before
B3C.3 makes the atomic writer/read cutover.

### B3C.2 — Canonical loose-resource authority

**B3C.2A closure — 16 October 2026:** complete after automated validation and
user-confirmed **23/23 authoritative manual acceptance**. `GroundStackId` uses
a fresh controlled scenario context plus monotonic local creation sequence; operation
keys remain separate exact-once replay guards. The registry now enforces one
stack/type per cell, stable identity across merges and partial pickup,
resource-defined provisional Soil/Stone capacity 10, reservation/availability
visibility, revisions, provenance and lifecycle. Empty removal terminates the
identity, a reused cell receives a new ID, and replacement scenarios cannot
resolve old identities or reservations.

The runtime host binds terrain compatibility: missing cells, obstructing
Solids/Nodes, completed functional Placements and unknown details fail before
source consumption. Extraction retains a narrow preflight exception for its
exact source Solid. The explicit inventory-drop command also retains its
validated current-anchor behavior on legacy functional Backgrounds until WV-B3D;
ordinary producers cannot use that compatibility exception. The unified read
adapter exposes authoritative immutable stack details. Existing
pickup/Hauling/Storage/Construction callers use the
returned canonical ID; no parallel registry or legacy alias authority exists.
Generated Surface Nodes remain `GenerationOnly`.

This implements B3C.2A only. B3C.2B deterministic multi-cell overflow,
prepared capacity reservation and recoverable pending output remains future.
Variable Node quantities and Extraction cutover remain B3C.3.

For safer v1.0 delivery, approve this as two gates: **B3C.2A** introduces
Resource Definitions, stable canonical Stack IDs, one-stack/type/capacity rules,
revisions and legacy migration; **B3C.2B** adds deterministic multi-cell output
reservation, overflow placement, replay protection and recoverable pending
output. Neither gate changes Node yields or live Extraction without separate
approval.

**Systems affected:** internals of `WorldMaterialDropRegistry`; Colony adapter;
pickup compatibility lookup; construction-cancellation/developer-drop output;
host scenario construction.

**Interfaces:** typed cell stack query; canonical stack ID; cell/stack revision;
prepared capacity reservation; committed-operation ledger; compatibility
overloads for current callers.

**Mutation ownership:** the evolved registry remains the only loose-ground
quantity owner. Inventory, Construction Site staging and Storage keep their
separate ownership.

**Compatibility adapter:** current creation callers retain operation keys for
replay prevention and receive the canonical Stack ID used by pickup and
Hauling. Existing transport behavior is retained without a second identity map.

**Automated tests:** one record/type per cell; same-type merge; capacity and
mixed-type rejection; legacy coalescing/failure cases; competing reservations;
exact pickup; drop/cancellation recovery; one revision/change per committed
cell; current destination selection and conservation suites unchanged.

**Manual acceptance:** extract/drop fixture material through the still-current
path, merge it, partially pick it up, haul to first-site staging and Storage,
interrupt before/after pickup, and confirm one visible stack with conserved
quantity.

**Rollback:** retain the current registry implementation behind the same public
callers until canonical migration passes. A failed candidate build must preserve
the previous world under the current rebuild/restore behavior.

**Resolved by B3C.2A:** scenario-local controlled-sequence IDs; clean scenario
construction rather than silent legacy coalescing; provisional definition
capacity 10 for both active resource types; scenario replacement discards old
reservations. Multi-cell overflow recovery remains deliberately unresolved for
B3C.2B.

### B3C.3 — Exact-once Node-to-output transaction

For safer v1.0 delivery, approve this as **B3C.3A** active variable-quantity
Colony Node ownership followed by **B3C.3B** whole-node Extraction cutover. The
first establishes and validates sole quantity ownership; the second coordinates
Node depletion, Solid removal, multi-stack output and coherent publication.

**Systems affected:** `ExcavationWorldTransaction`, `ExcavationJobOrder`, Node
registry, loose registry, authorised terrain mutation seam, runtime-cell change
publication and navigation/render invalidation.

**Interfaces:** prepared extraction operation/token; expected-revision values;
Node and output reservations; deterministic placement policy; batch change-set
publisher; replay/recovery result query.

**Mutation ownership:** the coordinator is the only path that couples Node
quantity, depleted Solid projection and loose output. Jobs continue to own work;
the underlying authorities own their final records.

**Compatibility adapter:** keep the `Excavation*` names and callers while
delegating final commit to the shared coordinator. Soil/Stone remain
quantity-one; Surface actions remain disabled.

**Automated tests:** success, cancellation, replay, stale revisions, competing
reservations, full/mixed output cell, bounded alternate placement, pending
recovery, injected pre-commit failure, recovery-required state, Background
retention and exact batched change categories/cells/revisions. Rerun all
Excavation, runtime-cell, Hauling, Construction, navigation, generation,
selection and survival tests plus complete Edit/Play suites.

**Manual acceptance:** complete the WV-B3C manual matrix in section 8.4 using
fixture and generated seed `2203`.

**Rollback:** keep the old `ExcavationWorldTransaction` selectable until the new
coordinator passes automated and manual acceptance. Do not maintain both as
active writers. Remove compatibility only in a later explicit cleanup.

**Unresolved before implementation:** alternate-output radius/candidate cap;
accessibility origin and protected-cell rules; operation-ID lifecycle; pending
operation UI/lifecycle; in-memory recovery state; exact event-batching API.

## 8. Validation and acceptance strategy

### 8.1 Focused Edit Mode

- Node initialization: generated and fixture IDs, typed locations, quantity,
  revision, one-to-one Solid relation and deterministic rebuild.
- Exact-once Extraction: prepare/commit/replay/cancel, stale Node/terrain/stack
  revisions, competing reservations and mismatched replay payload.
- Depletion: Node removal, Solid removal only at zero, unchanged Background,
  navigation invalidation and no duplicate output.
- Loose occupancy: one canonical stack, one resource type, compatible merge,
  capacity enforcement, deterministic alternate destination and no-fit pending
  recovery.
- Pickup: owner-only reservation, competition, destination rejection,
  partial/exact pickup and cancellation conservation.
- Change feeds: exact affected typed cells/categories, monotonic cell/region
  revisions, one batch per transaction and independent consumer cursors.
- Cross-region: Colony mutation never changes Surface revisions; Surface
  generated Nodes remain `GenerationOnly` and deterministic.
- Scenario construction: invalid Node/Solid or legacy-stack migration retains
  the previous published pair and follows the documented restore path.

### 8.2 Focused Play Mode

- Autonomous and player-directed Soil/Stone Extraction leave one recoverable
  output stack and preserve Background/visual state.
- View switching during prepared, pending and completed operations never resets
  quantity or republishes a scenario.
- Collision, routes and visuals update after depletion; loose stacks remain
  traversable.
- Pickup/Hauling interruption before and after collection conserves physical
  quantity and releases only the correct reservations.
- Generated seed `2203` can still bootstrap first Storage through Construction
  staging without requiring existing Storage.
- Entrance round trip, residency, pause/speed and shared clock remain unchanged.

### 8.3 Regression suites

Run focused runtime-cell, generation, Excavation, material handling/Hauling,
Construction, navigation, selection, survival and world-view suites. Then run
the complete Edit Mode and Play Mode suites. Record XML, Unity version, tested
working-tree revision/baseline and zero failure/skip/inconclusive counts; do not
infer a Git SHA from a baseline alone.

Also run strict Zensical build, internal Markdown link validation and
`git diff --check`. Inspect the final diff to prove no unrelated scene, prefab,
asset, project setting or generated site output is included.

### 8.4 Manual acceptance

Using both the authored fixture and generated seed `2203`:

1. Inspect stable typed Soil/Stone Node IDs, quantity and revisions.
2. Extract Soil and Stone and confirm Background remains.
3. Confirm exactly one output stack, correct quantity and compatible merging.
4. Fill the source stack and verify deterministic alternate placement.
5. Block every bounded candidate and verify a recoverable pending operation
   with unchanged Node/output quantities.
6. Unblock a candidate, retry the same operation and verify one commit.
7. Replay/interrupt around completion and confirm no duplicate output.
8. Compete for Node/output/pickup reservations with two ants.
9. Interrupt pickup before and after collection, then finish delivery to a
   Construction Site and Storage; confirm conservation.
10. Verify collision/path/render changes after depletion and no view leakage.
11. Switch views repeatedly and make an entrance round trip; confirm Surface
    generated Nodes remain stable and `GenerationOnly`.
12. Rebuild/restore both scenarios and confirm one published read-model pair,
    no duplicate ants/resources, expected initial state and clean Console.

## 9. Regression risks

| Risk | Control |
| --- | --- |
| Node and Solid diverge | Validate one-to-one at publication; permit coupled writes only through the transaction coordinator; fail closed on revision mismatch. |
| Output duplicates after retry | Persistent-in-scenario operation ID and committed result ledger, separate from Stack ID. |
| Output disappears after depletion | Reserve/validate complete capacity before mutation; no ordinary failure point after apply starts; recovery-required ledger for exceptional partial failure. |
| Multiple same-cell records survive | Canonical cell index and strict migration validation; never hide invalid legacy state by snapshot aggregation. |
| Current Hauling/Construction behavior changes | Preserve public registry role and current destination/staging owners; characterization and full regression tests. |
| Change feeds double-publish | Batch/suppress component events inside coordinator and publish one final combined change set. |
| Navigation/rendering becomes stale | Keep `ChunkedWorldData` as Colony physical projection and preserve its invalidation path when a Node depletes. |
| Generated data becomes live authority | Copy once into a private runtime registry; never mutate or re-read generated remaining quantity after publication. |
| Surface scope expands accidentally | Keep Surface Nodes `GenerationOnly`; no Surface job, mutation or resource catalog work in the smallest implementation. |
| Scenario failure is mistaken for atomic rollback | Retain and test `WV-B3B-LIM-001`; do not claim whole-world transaction atomicity. |

## 10. Outstanding technical decisions

The following block implementation but do not invalidate this plan:

1. final Node registry/coordinator names and assembly placement;
2. versioned fixture Node-ID and canonical Stack-ID derivation;
3. the exact B3C.3 atomic writer/read cutover and adapter integration;
4. exact alternate-output radius, candidate cap, direction order constants,
   accessibility origin, hazard query and protected entrance cells;
5. legacy same-cell capacity reconciliation and active-reservation handling;
6. operation-ID allocation/lifetime and completed-ledger retention;
7. pending/recovery-required operation ownership, retry trigger and developer UI;
8. internal event-batching/suppression mechanism;
9. future Seed/Twig definitions, capacities and presentation;
10. Freshwater's physical/consumption representation;
11. save/load, death, region-unload and crash recovery policy.

## 11. WV-B3B-LIM-001 remains active

Atomic replacement still covers only the validated Colony/Surface unified
read-model pair. The development host constructs mutable terrain, Node/drop
registries, jobs, ants, inventories, entrance and residency services in place.
A later scenario-build or restoration failure still follows the existing
rebuild/restore pathway. WV-B3C must test and preserve that behavior, must not
describe it as whole-world transaction atomicity, and must reassess it before
persistence, region unloading or complex restoration.

## 12. Recommended smallest implementation prompt

The B3C.1 prompt above is complete. The next smallest candidate is a separately
authorised **B3C.2A only** prompt for Resource Definitions and canonical
one-stack ground-resource internals while preserving
existing pickup, Hauling, Construction recovery and developer-drop callers. It
must settle canonical Stack ID, legacy same-cell reconciliation and reservation
migration before code changes. It must not implement Node transaction cutover,
Surface gameplay, Placements or WV-B3D.

The completed first slice creates a reversible authority seam and proves
identity without changing accepted gameplay. B3C.2 and B3C.3 should follow only
after their unresolved decisions are approved and the preceding slice is
accepted.
