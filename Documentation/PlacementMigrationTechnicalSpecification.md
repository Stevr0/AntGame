# WV-B3D.1 Construction and Placement Foundation Technical Specification

**Status:** Complete documentation gate; no runtime Placement migration implemented.

**Date:** 19 October 2026

**Authority:** This document is the implementation-ready migration companion to
[`ConstructionPlacementSpecification.md`](ConstructionPlacementSpecification.md),
[`UnifiedRuntimeCellSpecification.md`](UnifiedRuntimeCellSpecification.md) and
[`ResourceAndNodeSystem.md`](ResourceAndNodeSystem.md). Those approved product
contracts control if this document is read in isolation. Historical rules that
make a Background provide colony function are compatibility descriptions only.

## 1. Scope and fixed outcome

WV-B3D.1 specifies, but does not implement, the transition from functional
Backgrounds to completed Placement occupants. The target cell is:

`one natural Background + optional Node + optional Construction/Placement + optional loose stack`

The natural Background always remains. Storage, Sleeping, Nursery, future Door
and Refining functions belong to a Placement, never to that Background. A
Construction Site is the incomplete predecessor of one Placement and supplies
no completed function.

This gate does not add a Placement registry, migrate live state, change recipes,
run Unity tests, or begin WV-B3D.2. It retains `WV-B3B-LIM-001`: publication of
the Colony/Surface read-model pair is atomic, but rebuilding every mutable
gameplay owner is not one whole-world transaction.

## 2. Verified implementation inventory

### 2.1 Terrain, cells and compatibility

- `ChunkedWorldData` owns one permanent Background material ID and one optional
  Solid material ID per resident Colony cell. `SetBackgroundMaterial` currently
  performs the legacy conversion and publishes a Background change.
- `BackgroundTileMaterialDefinition` still carries movement data plus the legacy
  `BackgroundFunction` enum: `None`, `FoodStorage`, `WaterStorage`, `Sleeping`,
  `MaterialStorage` and `Nursery`. Every non-`None` function reports capacity 1.
- Natural Earth and Stone are grippable at `1.0x`; Prepared Grippable is `1.25x`.
  Sky is a retained fixture/background definition without grip and must not be
  introduced into the approved entirely underground Colony generation model.
- `RuntimeCellSnapshot` already has separate Background, Node, Placement and
  loose-stack fields. `RuntimeCellCompatibilityValidator` rejects standard
  Construction beside an obstructing Node, incompatible loose stack or existing
  Placement, and returns `Unknown` when legacy details are insufficient.
- `ColonyRuntimeCellRegionAdapter` currently creates a
  `CompatibilityProjected` Placement only when a Background has a function. Its
  synthetic ID is cell-derived and it has no Placement revision, footprint or
  function-state snapshot. Prepared Grippable is not projected as a Placement.

### 2.2 Construction

- `ConstructionRecipeCatalog` and six code-defined recipes live in the
  development host. They are not Placement Definitions or ScriptableObjects.
- `ConstructionDesignationRegistry` owns a cell-indexed view over the shared
  `ColonyJobRegistry`. It rejects non-resident, covered, same-result,
  inaccessible, actively designated or occupied functional cells.
- `ConstructionSite` owns required, reserved incoming and delivered quantities
  per resource type. It can already stage multiple types even though every
  authored recipe currently uses one.
- `MaterialHaulingOrder` alone moves ground/carried/Material-Storage resources
  into site staging. `ConstructionJobOrder` starts only when every requirement
  is staged, performs timed adjacent work, preserves progress on interruption
  and awards Construction XP only for applied work.
- `ConstructionWorldTransaction` validates the old Background, consumes staging
  exactly once, replaces the Background ID and registers the legacy function.
  It uses the job ID as its replay guard but does not create a Placement ID.
- Cancellation prepares recoverable ground outputs for all staged types before
  cancelling. It is bounded to nearby valid cells and retains the site if safe
  recovery cannot be prepared.

### 2.3 Functional owners

| Function | Implemented authority | Current state | Target status |
| --- | --- | --- | --- |
| Prepared Surface | `ChunkedWorldData` Background plus movement evaluators | `1.25x` speed; grip/directions remain Background-derived | Non-obstructing constructed Placement modifier; representation fixed below |
| Sleeping | `SurvivalInteractionWorld` | Registered cells and one exclusive ant reservation | Placement-owned one-user capacity |
| Food Storage | `SurvivalInteractionWorld.SurvivalStorageSlot` | Stable food resource ID, quantity and per-ant withdrawal reservations; no maximum | Placement-owned contents ledger; capacity remains unresolved |
| Water Storage | `SurvivalInteractionWorld.SurvivalStorageSlot` | Stable development-water ID, quantity and per-ant withdrawal reservations; no maximum | Placement-owned liquid-compatible contents after Water contract |
| Material Storage | `MaterialStorageWorld.MaterialStorageSlot` | Soil/Stone/all filter, quantity, capacity 10, one incoming and one withdrawal reservation | First functional cutover candidate |
| Nursery | Background enum/material/recipe only | Declared capacity 1; no brood record, reservation or executor | Placement data capacity only until brood milestone |
| Door | None | No definition, recipe, state, navigation or tests | Future separate milestone |
| Refining Station | None | No definition, bill, staging, executor or output | Future separate milestone |
| Room enclosure/quality | None | No room identity, flood-fill, boundary, score or bonus authority | Future system after Doors |
| Dismantling | None | Completed conversion cannot be cancelled | Future Construction job |

`AntInventory` remains the carrier authority. `WorldMaterialDropRegistry` remains
the canonical ground-stack authority. Construction staging remains site-owned.
Neither becomes Placement contents.

### 2.4 Verified recipes and work

These are preserved migration inputs, not newly approved balance:

| Existing recipe | Result compatibility ID | Material | Base work |
| --- | --- | ---: | ---: |
| `conversion.prepared` | `background.prepared.grippable` | 1 Soil | 4 simulation hours |
| `conversion.nursery` | `background.function.nursery` | 1 Soil | 4 simulation hours |
| `conversion.sleeping` | `background.function.sleeping` | 1 Soil | 4 simulation hours |
| `conversion.food-storage` | `background.function.food-storage` | 1 Soil | 4 simulation hours |
| `conversion.water-storage` | `background.function.water-storage` | 1 Stone | 4 simulation hours |
| `conversion.material-storage` | `background.function.material-storage` | 1 Soil | 4 simulation hours |

All use Construction skill. `RequiredSourceMaterialId` is currently empty, so
the legacy recipes accept any exposed source Background. A Placement migration
must not silently narrow or broaden that behavior; definition compatibility must
be approved and characterized per slice. Door and Refining recipes remain unset.

## 3. Placement Definition contract

A `PlacementDefinition` is immutable shared data. It contains:

- stable definition ID, schema version, display name and category;
- supported `RegionId`/coordinate kinds;
- anchor convention, one or more relative footprint offsets, and allowed
  orientations;
- compatible natural Background definitions or predicates;
- Node, existing Placement, loose-stack and Construction-Site compatibility;
- material recipe and Construction work amount;
- required work category/skill and valid work interaction offsets;
- traversal, obstruction, support and navigation-cost modifiers;
- valid completed-use interaction offsets;
- function kind, capacity kind and capacity amount where applicable;
- allowed contents/filter policy or production behavior;
- room-quality eligibility and modifiers, without owning room state;
- removal prerequisites, dismantling work and explicitly approved refunds;
- renderer/presentation key, anchor/footprint visual policy and sorting data; and
- persistence schema/version and definition-migration key.

Definition data never stores instance quantity, reservations, users, progress,
damage, room membership or a scenario-specific location. The existing proposed
IDs (`placement.surface.prepared`, `placement.sleeping-spot`,
`placement.storage.food`, `placement.storage.water`,
`placement.storage.material`, `placement.nursery`, `placement.door` and
`placement.refining-station`) remain candidate stable IDs until the implementing
slice explicitly freezes them. They must not be inferred from display names or
legacy Background IDs.

## 4. Placement instance and registry contract

A completed instance is mutable scenario state and contains:

- stable `PlacementId` unique within a scenario context;
- definition ID/version;
- typed `RegionCellId` anchor, orientation and complete typed footprint;
- lifecycle (`Active`, future `Dismantling`, `Removed`, `Invalidated` or
  `ScenarioRetired`);
- monotonically increasing Placement revision;
- function-active flag derived from lifecycle and migration ownership;
- function-specific state reference/ledger;
- incoming, outgoing, use and dismantling reservations where supported;
- current users/occupants where supported; and
- optional room association/quality snapshot only after a room authority exists.

One scenario-local `PlacementRegistry` must be the only owner of Placement
identity, footprint occupancy, lifecycle, revision and active-function routing.
It indexes every occupied footprint cell back to one anchor identity. Function
ledgers may be focused components keyed by `PlacementId`, but must not copy the
same contents into the registry and a legacy cell dictionary.

IDs must include or be validated against the scenario context. Scenario rebuild
retires the old registry, releases only uncommitted reservations through their
owners and makes every old ID reject mutation. Reads return immutable snapshots.

## 5. Footprint and orientation rules

WV-B3D implementation begins with one-cell footprints, anchor offset `(0,0)`
and no runtime rotation requirement. The contract nevertheless supports future
multi-cell definitions:

1. A definition supplies a canonical set of unique relative offsets containing
   `(0,0)`.
2. Orientation transforms those offsets deterministically; the transformed set
   is stored or reproducible from definition version and orientation.
3. Every transformed cell must use the same region and coordinate kind and be
   resident before designation.
4. The site reserves the complete future footprint as one unit. No cell may be
   independently designated, completed or dismantled.
5. Work/use interaction offsets are definition-owned and resolved around the
   transformed footprint. At least one reachable valid anchor is required for
   ordinary Construction.
6. Navigation derives obstruction, passability, support and cost modifiers from
   the one Placement definition. It does not create one obstacle per identity.
7. One renderer/presentation owner represents the instance and may draw several
   footprint cells. Child visuals never become additional Placements.
8. Dismantling locks and removes the entire identity. Partial footprint removal
   is invalid unless a future damage system explicitly defines it.

Adding multi-cell definitions later must not replace `PlacementId`, the
footprint index or the anchor/orientation contract.

## 6. Central occupancy validation

Designation and completion must call the same pure, region-aware validator.
For every footprint cell, standard Construction requires:

- a resident typed cell and a present natural Background;
- definition-supported region and compatible Background;
- no obstructing Solid/Node and no unknown Node obstruction;
- no existing Placement footprint;
- no incompatible loose stack;
- no conflicting active Construction Site/future-footprint reservation;
- no protected entrance endpoint or definition-specific exclusion; and
- at least one reachable valid work interaction position for the whole site.

`Invalid` means a known rule rejects the request. `Unknown` means required
authority detail is unavailable and must fail closed. A definition-specific
exception must name the operation and compatible occupant; there is no global
“allow overlap” flag.

Completed Storage contents are Placement-owned and do not occupy the loose-stack
slot. Site staging is site-owned and does not occupy that slot. A loose stack on
a standard target is incompatible unless a future definition explicitly allows
coexistence and its tests prove interaction and rendering behavior.

## 7. Construction lifecycle and completion transaction

The preserved workflow is:

**Designation → Hauling → Material Staging → Construction Work → Completed Placement**

### 7.1 Designation and staging

Designation resolves the definition, transforms its footprint, captures expected
Background/Node/occupancy revisions, reserves the future Placement slot and
creates one stable site/job identity. The site copies the required recipe lines
and owns staged quantities and incoming reservations. It exposes no completed
function.

Hauling reserves physical sources and site capacity, then commits material into
the site. It remains the only normal transport authority. Construction never
withdraws from ground, inventory or Storage by itself and remains ineligible
until every recipe line is fully staged.

### 7.2 Work, completion and atomicity

Construction work uses existing adjacent approach, progress, skill speed,
Rested modifier and XP rules. Before the terminal progress step, completion must:

1. resolve operation/site/definition identity and reject terminal replay;
2. revalidate the complete footprint and expected revisions;
3. prepare one Placement identity and empty/initial function state;
4. confirm all staged quantities without consuming them;
5. commit Placement registration and footprint occupancy exactly once;
6. consume all staged materials exactly once;
7. close/retire the site and its future-slot reservation;
8. activate the Placement function only after coherent registration; and
9. publish Placement cell changes for every footprint cell, then allow
   presentation/navigation updates.

Consumers must never observe consumed staging with no recoverable site or
Placement. If the current owners cannot provide a true multi-owner transaction,
the coordinator records a stable prepared operation and terminal result, with
compensating rollback limited to actions that have not exposed function. Replay
returns the recorded result and never awards XP twice.

### 7.3 Cancellation and interruption

Travel/work interruption preserves staging and progress and releases only the
builder reservation. Cancellation releases incoming reservations and prepares
all staged recovery stacks before clearing staging or the site. If complete
recovery capacity is unavailable, cancellation remains blocked with the site
and its quantities intact. Recovery follows the established deterministic,
capacity-validated physical-output policy; it never inserts resources directly
into inventory or Storage.

## 8. Migration matrix

| Legacy function | Existing owner | Target definition | State/capacity to preserve | Interactions/reservations | Recipe | Safe sequence and rollback | Required evidence |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Prepared Grippable | Background ID and movement evaluators | Prepared Surface candidate | natural Background identity; `1.25x` movement effect; one-cell constructed identity | traversal only; navigation invalidation | 1 Soil / 4h | create non-obstructing Placement state privately; switch speed lookup; retain old material adapter until rollback proves original speed/rendering | movement capability, route cost, render refresh, wall/ceiling/fall, construction/cancellation, fixture tests |
| Sleeping | `SurvivalInteractionWorld` cell set/reservation | Sleeping Spot | one exclusive user, active reservation, applied rest progress remains ant-owned | adjacent Rest; owner release on cancel/lifecycle | 1 Soil / 4h | transfer reservation keyed to new `PlacementId`; switch target discovery/validation; on failure retain legacy set and reservation | survival Edit/Play, autonomy, interruption, removed target, selection, rebuild |
| Food Storage | `SurvivalStorageSlot` | Food Storage | resource ID, quantity, all reservation owners/amounts; capacity is not currently bounded | adjacent Eat; autonomous nearest reachable source | 1 Soil / 4h | freeze new reservations, copy+validate ledger, redirect existing reservation IDs, atomically flip authority; rollback leaves legacy ledger active | conservation, independent reservations, need restore, autonomy, HUD, rebuild |
| Water Storage | `SurvivalStorageSlot` | Water Storage | development Water ID, quantity, reservations; no approved liquid capacity | adjacent Drink; autonomous nearest reachable source | 1 Stone / 4h | same as Food only after Water Droplet/carrier/capacity contract; do not reinterpret as Soil/Stone stack | hydration/XP recovery, liquid compatibility, conservation, autonomy, rebuild |
| Material Storage | `MaterialStorageWorld` | Material Storage | filter, resource ID, quantity, maximum 10, incoming and withdrawal owner/quantity | adjacent deposit/withdraw; Hauling destination/source; badges | 1 Soil / 4h | snapshot slot and expected revisions; quiesce new reservations; create Placement+ledger with same owners; switch routing once; invalidate legacy slot only after success; rollback keeps legacy active | first-Storage bootstrap, partial/multi-source hauling, filters, both reservations, Storage-to-site withdrawal, badges, conservation, replay, reset |
| Nursery | Background function only | Nursery | one declared capacity unit and constructed/authored presence; no invented brood | no current runtime interaction/reservation | 1 Soil / 4h | create Placement capacity with no brood ledger; disable legacy function; rollback restores adapter only | construction, occupancy, one-capacity snapshot, fixture/rendering; future brood tests separately |

For every row, the replacement Placement is the exclusive functional authority
for a migrated cell. A temporary adapter may project or route legacy state, but
an explicit per-instance authority state (`Legacy`, `Placement`, `Retired`) must
prevent both executors from registering capacity or accepting reservations.

## 9. Material Storage cutover contract

Material Storage is first because it exercises the complete physical economy.
The cutover must preserve exactly:

- Soil/Stone/all filter and same-resource single-slot rule;
- resource ID, quantity, provisional maximum 10 and visible badge;
- incoming owner/resource/quantity reservation;
- outgoing withdrawal owner/quantity reservation;
- transfer IDs, child pickup IDs, source/destination revisions and replay state;
- partial/multi-source collection and carrier allocation;
- Storage-to-Construction withdrawal and the no Storage-to-Storage loop;
- first-Storage bootstrap with direct ground-to-site Hauling; and
- total material conservation through success, cancellation and rebuild.

The narrowest safe implementation keeps `MaterialHaulingOrder` and
`MaterialTransferRegistry` unchanged and gives their source/destination lookup a
small adapter that resolves either an unmigrated legacy slot or a Placement-owned
slot, never both. Existing in-flight transfer IDs keep their destination/source
identity during cutover or the cutover waits until they are terminal. No slot is
removed while it has contents or reservations unless those exact records have
been transferred and revalidated.

## 10. Sleeping, Nursery and survival Storage

### Sleeping Spot

The Placement owns one use reservation; the ant owns its needs state and rest
progress. Target discovery still chooses a reachable valid adjacent interaction
position, and cancellation/inactivity releases the reservation. Natural rest in
World View remains a separate future interaction and must remain available when
no Sleeping Placement exists; it is not represented by a fake Placement.

### Nursery

One Placement supplies one brood-capacity unit. Because no brood runtime exists,
the migration creates no brood, laying, feeding, reassignment or development
state. An unenclosed Nursery remains usable at base capacity once brood exists;
room quality may later modify efficiency but never create extra base slots.

### Food and Water Storage

Food and Water contents move to ledgers keyed by `PlacementId`. Existing
resource reservations must survive. Food capacity remains unresolved because the
current owner has no maximum. Water cutover waits for the approved Water Droplet
and carrier representation; it must not borrow the building-material stack rule.

## 11. Prepared Surface decision

For the smallest architecture consistent with the approved one-Placement slot,
Prepared Surface is a **non-obstructing Placement subtype**, not a second
independent improvement layer. It occupies the same optional Placement slot as
Storage or Sleeping, applies a definition-owned movement-cost multiplier, and
leaves grip, movement directions, gravity and the natural Background definition
unchanged. Therefore a cell cannot simultaneously contain Prepared Surface and
another Placement.

This recommendation avoids a fifth mutable cell layer and duplicate Placement
identity. It must be explicitly accepted before WV-B3D.6 because it prevents
combining a prepared surface with a functional Placement on one cell. If the
product later requires such combinations, approve a typed child-treatment slot
before implementation rather than allowing untracked overlapping Placements.

## 12. Rooms, Doors and quality readiness

A future room authority may derive rooms from resident geometry and completed
Placement states. It owns room identity, members, boundary revision, enclosure
and quality; no Placement or Background owns the room graph.

- Rooms may contain mixed Placement definitions.
- Every opening must contain an appropriate completed Door for enclosure.
- The founding Nest Entrance remains a specialised cross-region feature, not a
  Door or ordinary player Placement.
- Unenclosed Placements retain base function; only approved definitions receive
  enclosure/quality modifiers.
- Quality never changes base 1:1 capacity or duplicates contents.
- Terrain/Placement/Door changes invalidate only affected room candidates, with
  bounded recomputation defined in a later milestone.

Door footprint/orientation, open/closed navigation, access control, damage,
recipe and boundary semantics remain unresolved. Room scoring, thresholds,
bonuses and chunk-spanning limits also remain unresolved and unimplemented.

## 13. Dismantling and removal contract

Dismantling is a separate Construction-category job with its own stable operation
ID, work progress and reservations. It is not reverse completion.

1. Designation validates an active Placement, definition removal permission and
   an interaction position.
2. Storage contents and committed users must be cleared before work begins.
   Incoming/outgoing/use reservations are rejected or explicitly cancelled by
   their owners; contents are never discarded.
3. Interrupted work preserves progress and leaves function active unless a
   later approved state explicitly disables it.
4. Before irreversible removal, prepare capacity for every approved refund and
   any recoverable function output.
5. Revalidate Placement/footprint revisions, deactivate function, remove the
   whole identity exactly once and publish all footprint changes.
6. Commit refund stacks at the former anchor or deterministic valid nearby
   cells, retaining the natural Background.

No refund percentage, refund recipe, work duration or damage rule is approved.
Until those values exist, definitions are not dismantlable. A blocked output
plan leaves the Placement intact and reports a recoverable blocked reason.

## 14. Runtime-cell projection and change publication

The completed registry extends `RuntimePlacementSnapshot` to expose Placement
ID, definition/version, anchor, complete footprint or anchor-reference, lifecycle,
revision, function status and supported capacity/contents summaries. Detailed
mutable ledgers remain read-only views from their owners; the snapshot must not
become a second Storage or reservation authority.

Creation, activation, function-state change, reservation visibility change,
dismantling state and removal increment the Placement revision and publish a
`Placement` category change for each affected footprint cell. Reads, failed
prepares, camera switches and renderer refreshes publish nothing. The Colony
adapter observes registry events. Surface uses the same contract only when a
separately approved Surface Placement slice supplies an authority.

Natural Background, active Node, canonical ground stack, ant inventory and site
staging owners remain unchanged. Compatibility validation reads their immutable
snapshots and expected revisions before mutation.

## 15. Navigation, interaction and presentation

Colony View retains side-on anchors, body clearance, floor/wall/ceiling grip,
gravity and local invalidation. Definition obstruction and movement modifiers
are projected into `GridNavigationMap`; interactions resolve valid adjacent
anchors around the footprint. Prepared Surface changes travel cost, not grip.

Surface retains top-down movement and its own navigator. Future Surface
Placements use `SurfaceCell` identities and Surface-local compatibility. The
shared registry contract does not merge navigators or make Colony offsets valid
on Surface. Rendering remains a projection of Placement identity and definition;
camera/view changes never rebuild state.

## 16. Scenario publication and recovery

The development host currently reconstructs functional owners before publishing
the runtime-cell pair. A future scenario candidate must build its Placement
definitions, registry, legacy projections and footprint index privately, validate
them against terrain/Nodes/stacks, then publish the Colony/Surface read-model
pair. Only after successful publication may the old Placement context retire.

Mara/Pip/Tala remains the explicit integration fixture. The generated founding-
queen scenario initially has no constructed functions and must remain able to
bootstrap its first Material Storage through loose Soil → Hauling → site staging
→ Construction. Failed scenario creation retains the previously published world.
Old Placement IDs and reservations reject writes after retirement.

This is recovery ordering, not persistence. Save/load, unloaded mutation,
streaming and complete mutable-world rollback remain deferred under
`WV-B3B-LIM-001`.

## 17. Candidate implementation sequence

These slices require separate approval:

1. **WV-B3D.2 — Placement Registry Foundation:** freeze definition/instance IDs;
   implement one-cell registry, footprint index, revisions, snapshots, central
   compatibility and read-only legacy projections. No function cutover.
2. **WV-B3D.3A — Material Storage Authority Cutover:** migrate existing authored
   slots, contents, filters and reservations behind a narrow legacy/Placement
   routing adapter. Keep Construction completion legacy.
3. **WV-B3D.3B — Material Storage Construction Completion:** make its completed
   site create a Placement and empty Placement-owned slot; preserve first-Storage
   bootstrap, cancellation and Hauling. Retire only Material Storage legacy writes.
4. **WV-B3D.4A — Food Storage**, then **WV-B3D.4B — Water Storage** only after
   the liquid representation is approved.
5. **WV-B3D.5A — Sleeping Spot**, then **WV-B3D.5B — Nursery data capacity**.
6. **WV-B3D.6 — Prepared Surface** after explicit acceptance of the single-slot
   non-obstructing Placement decision.
7. **WV-B3D.7 — Legacy Retirement:** remove superseded functional-Background
   behavior one type at a time after proof. Door, room quality, Refining and
   dismantling remain separate later milestones.

Splitting Material Storage authority transfer from new Construction completion
keeps the highest-risk conservation change reversible. Combining Food and Water
is not recommended because Water lacks its final physical representation.

## 18. Test and acceptance strategy

### 18.1 Edit Mode and integration

- Definition validation: stable unique IDs/version, immutable metadata,
  supported region, canonical offsets and legal one-cell definitions.
- Identity: unique scenario-local Placement IDs, one identity per footprint,
  stale-context rejection, immutable snapshots and monotonic revisions.
- Occupancy: valid natural cell; reject Node, loose stack, existing Placement,
  conflicting site, non-resident/unknown and protected entrance; deterministic
  multi-cell transform tests even while gameplay remains one-cell.
- Construction: staging required, multiple recipe lines, exact-once completion,
  replay-safe XP, Background unchanged, cancellation recovery and injected
  failures before/after each prepare boundary.
- Material Storage: exact contents/filter/capacity migration, incoming and
  withdrawal reservations, in-flight transfers, partial/multi-source delivery,
  Storage-to-site withdrawal, no reorganisation loop and conservation.
- Survival: Food/Water reservation migration, Sleeping exclusivity and release,
  nearest reachable selection, interruption and removed-target behavior.
- Nursery/Prepared: one declared capacity without brood; `1.25x` Prepared travel
  cost with unchanged natural grip and Background.
- Publication: one Placement change per affected cell, no changes on reads/failures,
  adapter exclusivity, scenario retirement and previous-pair retention on failure.
- Legacy proof: source callers, serialized GUIDs, reflection/editor tools and
  fixture references mapped before removal.

### 18.2 Play Mode and manual gates

Each slice must construct or migrate only its named function, inspect the natural
Background plus one Placement, use its capacity, interrupt travel/work, cancel at
zero/partial/full staging, rebuild/switch scenarios, switch views, use the Nest
Entrance and leave a clean Console. Material Storage additionally exercises the
first-Storage bootstrap, partial/multi-source Hauling, withdrawals and badges.
Sleeping tests autonomy and exclusive Rest. Prepared tests floors, walls,
ceilings, route speed and loss of surface.

Focused tests precede complete graphics-enabled Edit and complete Play suites.
Retain WV-B1 camera/entrance, WV-B2B generation, WV-B3B cells, WV-B3C Extraction,
WV-B3C.2B Hauling, scheduler fairness, survival, construction, navigation and
development-scene regressions. Do not weaken legacy assertions until a new
authority is active and equivalent behavior is proven.

WV-B3D.1 itself runs documentation validation only; it provides no Unity runtime
evidence or manual acceptance for later slices.

## 19. Legacy retirement register

The authored development scene directly serializes the six compatibility assets:
Prepared `d01bd2a164fe63f40a9b5e3637cad621`, Food
`f1111111111111111111111111111111`, Water
`f2222222222222222222222222222222`, Sleeping
`f3333333333333333333333333333333`, Material Storage
`f4444444444444444444444444444444` and Nursery
`f5555555555555555555555555555555`. The navigation prototype also serializes
Prepared. All use `BackgroundTileMaterialDefinition` script GUID
`2193439cba2e469792e4f416723d2488`. These references must be migrated or retained
deliberately; deleting a runtime class or `.asset` first would create missing
references or remove fixture presentation.

| Candidate | Current dependencies | Prerequisite | Removal risk and proof |
| --- | --- | --- | --- |
| `BackgroundFunction` values and functional `.asset` files | scene host serialized references, construction recipes, runtime validation, Tilemaps; Construction, survival, movement and scene tests | each function migrated and version/serialization policy approved | High; inspect the GUID/YAML records above, scene validation, runtime/test references and manual visuals before per-value retirement |
| `SurvivalInteractionWorld` cell-keyed Storage/sleep owners | controller, autonomous survival, Construction registration; `SurvivalInteractionTests`, `SurvivalInteractionPlayModeTests`, autonomous-survival tests | Placement ledgers and routing accepted | High; reservation/needs regressions and in-flight owner reconciliation required |
| `MaterialStorageWorld` cell-keyed slots | Hauling, transfer registry, HUD/diagnostics, construction and fixtures; `MaterialStorageHaulingTests` and Play Mode counterpart | Material Storage cutover accepted | Highest conservation risk; prove every quantity and reservation path before removal or retain as Placement-keyed component |
| `ConstructionRecipe.Result` as Background definition | host recipes, catalogue key, designation and transaction | Placement Definition lookup and compatibility adapter | High; migrate callers without changing six recipes/work values |
| `ConstructionWorldTransaction` Background replacement | Construction executor; `ConstructionJobTests` and `ConstructionPlayModeTests` | exact-once Placement completion accepted | High; preserve replay, staging, XP and rollback tests |
| synthetic runtime Placement projection | `ColonyRuntimeCellRegionAdapter`; runtime-cell Edit/Play tests | authoritative registry snapshot published | Medium; remove only when all functional cells resolve one real or explicit legacy projection |
| `ConstructionOrderPhase.UnloadingInventory` / `TravellingToSource` | numeric/debug compatibility | serialization/version audit | Unknown; do not remove in Placement work |
| Prepared Background asset and prototype references | movement/navigation/rendering hosts, scenes and tests | Prepared Placement accepted with equivalent projection | High visual/navigation risk; retain as adapter/fixture until explicit retirement |

No candidate is safe to delete now. A successful `rg` is necessary but not
sufficient: inspect `.unity`, `.prefab`, `.asset`, `.meta` GUIDs, editor tooling,
dynamic/reflection use, focused/full tests and manual acceptance. Removal belongs
in a separate reversible WV-B3D.7 change.

## 20. Decisions still requiring approval

- final Placement Definition IDs, schema version and persistence format;
- explicit acceptance of Prepared Surface as an exclusive non-obstructing
  Placement rather than a separate treatment slot;
- final Background compatibility per definition;
- Food and Water capacities, filters and Water Droplet/carrier representation;
- dismantling work, refunds and output quantities;
- Door footprint/orientation, states, access and navigation behavior;
- room boundary algorithm, scoring, bonuses and chunk limits;
- Nursery occupied-capacity reassignment once brood exists;
- Refining bills, inputs, outputs, worker rules and blocked-output policy; and
- death, unload and save/load reservation reconciliation.

The smallest recommended implementation milestone is **WV-B3D.2 Placement
Registry Foundation** only. It should stop after one-cell definitions, identity,
footprint occupancy, revisions, immutable snapshots, compatibility validation
and read-only legacy projections pass their gates. No functional authority moves
until a later explicitly approved slice.
