# WV-B3C.2B Partial Hauling and Canonical Stack Transaction Plan

**Status:** Implemented on 16 October 2026; automated validation recorded in the
current-state/roadmap evidence and manual Unity acceptance pending.

**Prepared:** 16 October 2026

**Foundation:** WV-B3C.2A is complete. Its canonical scenario-local
`GroundStackId`, one-stack-per-cell rule, immutable snapshots, revisions and
Colony change publication remain authoritative. This plan does not reopen that
milestone or move Resource Node, Placement, Surface, persistence or whole-world
publication authority.

## 1. Scope and terminology

This plan defines the smallest safe integration of partial autonomous Hauling
with canonical ground stacks. It covers exact quantity selection, source and
destination reservations, replay-safe pickup and delivery, successive trips,
interruption recovery, same-resource merging and test-only large stacks.

The following terms are intentionally distinct:

- **stack identity** identifies one physical ground stack;
- **job identity** identifies one scheduler opportunity;
- **ant identity** identifies the carrier;
- **transfer identity** identifies one exact source-to-carrier-to-destination
  operation; and
- **producer operation identity** prevents Extraction/drop output replay.

One value must not substitute for another. In particular, an ant ID is not a
safe replay key and a producer operation key is not a `GroundStackId`.

This document began as the implementation plan and is retained as the contract
and migration history. WV-B3C.2B now implements its scenario-local transfer
ledger, transfer-owned reservations, carrier allocation, partial commits,
successive trips and recovery states. General multi-cell output placement was
not activated because production Nodes remain quantity-one and a failed
inventory drop already preserves its current owner; that distributor remains a
future variable-output requirement.

The approved addendum supersedes the earlier slot-only carry assumptions. The
authoritative 0–100 Hauling bands are 2×2 at 0–19, 3×2 at 20–39, 3×3 at 40–59,
4×4 at 60–74, 5×5 at 75–89 and 6×6 at 90–100. The existing shared skill-decay
implementation is reused unchanged. Threshold loss retains over-capacity cargo,
blocks new pickup and permits delivery/recovery.

### Explicit exclusions

- variable Resource Node quantities and active Node authority;
- whole-node Extraction cutover;
- Surface Extraction or Surface Hauling;
- Placement migration or changes to legacy functional Backgrounds;
- partial Construction work before full staging;
- new resources, balance changes or production Node-yield changes;
- persistence, region unloading or whole-world transaction publication; and
- concurrent multi-ant claims against one stack in the first implementation.

`WV-B3B-LIM-001` remains active. Scenario-local transfer atomicity does not make
construction or publication of the complete mutable gameplay world atomic.

## 2. Current implementation audit

The runtime already calculates a partial amount, but it does not yet have a
canonical replay-safe Hauling transaction.

- `MaterialHaulingOrder.TryCreate` bounds a trip by source quantity, compatible
  ant-inventory capacity and the chosen destination's available capacity.
- `WorldMaterialDropRegistry.TryCommitReservedPickup` can subtract less than the
  stack quantity and preserves the `GroundStackId` while quantity remains.
- `MaterialStorageWorld` and `ConstructionSite` accept a bounded reserved
  delivery, and inventory APIs can add or remove exact quantities.
- One source reservation and one incoming reservation are allowed at a time.
  They are owned by ant ID, not by an immutable transfer ID.
- Pickup and delivery are conserving callback transactions, but successful
  commits have no transfer ledger. Replaying a request is therefore treated as
  stale or could be confused with a later operation owned by the same ant.
- A dynamic hauling job is keyed by the stack's producer ownership key. One
  delivery completes that job; the next refresh removes it and may create a new
  job for a remaining stack. This can support incidental successive trips, but
  the lifecycle and replay boundary are implicit rather than specified.
- Cancellation after pickup and destination invalidation leave material in the
  carrier inventory and release destination capacity. Owner-only self-deposit
  work can later recover it, but no transfer record explains the handoff.
- Badges already read committed ground-stack and Storage quantities. They do not
  subtract reservations from the displayed amount.

Existing tests prove partial pickup caused by limited carrier space, source
identity preservation, reservation exclusion, cancellation conservation,
destination invalidation safety, exact Storage delivery and B3C.2A merge/
capacity rules. They do not prove idempotent replay, a durable transfer phase,
multiple successive scheduler trips from a deliberately large stack, or
inactivity recovery.

## 3. Existing quantity authority map

| Quantity or constraint | Current mutable authority | Identity/revision | Current transfer APIs |
| --- | --- | --- | --- |
| Loose ground quantity | `WorldMaterialDropRegistry` | scenario-local `GroundStackId`; stack revision; producer ownership key retained as provenance/replay guard | `TryAdd`, `TryAddFromSource`, `TryReserve`, `TryCommitReservedPickup` |
| Ground pickup reservation | `WorldMaterialDropRegistry` | one `DropReservation` per stack, currently owned by ant ID | reserve, release, reserved pickup commit |
| Carried quantity | `AntRecord.Inventory` / `AntInventory` | ant ID plus resource/slot data; no inventory revision or transfer reservation | exact/partial `TryAdd`, exact `TryRemoveResource`, selected-slot removal |
| Material Storage quantity | `MaterialStorageWorld` / one `MaterialStorageSlot` per cell | Storage cell; no public content revision | incoming/withdrawal reservation and conserving commit |
| Construction staged quantity | `ConstructionSite` / `ConstructionMaterialRequirement` | construction job/site and resource ID; no public staging revision | incoming reservation, reserved delivery, cancellation recovery |
| Destination capacity | `ConstructionSite` remaining requirement or `MaterialStorageSlot` free compatible capacity | destination identity plus current contents/reservations | `GetAvailableCapacity`, `TryReserveIncoming` |
| Runtime-cell loose snapshot | read-only Colony adapter | `RegionCellId`, stack ID and stack revision | projection only; never transfers quantity |
| Quantity badges | Colony presentation | none; reads current committed authorities | read-only presentation |

### 3.1 Current quantity-moving operations

1. Extraction/output adds or merges quantity into a ground stack.
2. Explicit pickup and autonomous Hauling move ground quantity into an ant
   inventory after a source reservation.
3. Explicit inventory drop moves a selected inventory quantity into a compatible
   ground stack.
4. Autonomous Hauling moves carrier inventory into Construction staging or
   Material Storage after a destination reservation.
5. Construction cancellation moves staged quantity back to recoverable ground
   output.
6. Legacy construction recovery can withdraw from Storage into builder inventory
   or move unneeded inventory into Storage/ground. The builder self-supply path is
   historical compatibility and must not be reintroduced into normal staged
   Construction.
7. Construction completion consumes fully staged quantities exactly once.

Every future mutation must continue to occur through the authority that owns the
source and destination. The unified read model and badges remain consumers.

## 4. Recommended transaction authority

Add one small, scenario-local `MaterialTransferRegistry` composed by the existing
development host. It coordinates current owners; it does not store a second copy
of physical resource quantity.

Each transfer has a stable immutable payload and exposes immutable snapshots of
its mutable phase/revision record. The payload/snapshot contract should contain:

- scenario-local `MaterialTransferId`;
- source kind and stable source identity/revision;
- destination kind and stable destination identity/revision where available;
- carrier `AntId`;
- Resource Definition ID and resource-specific maximums;
- exact transfer quantity;
- current phase and monotonically increasing transfer revision;
- scheduler job ID and creation provenance; and
- terminal reason plus replay outcome.

Recommended phases are:

`Prepared → Reserved → PickedUp → Delivered`

with terminal alternatives `CancelledBeforePickup`, `RecoveryRequired` and
`CancelledAfterRecovery`. A failed request that changes no owner remains in its
current committed phase and records a diagnostic result; `Failed` must not imply
that quantity has no owner.

The registry records coordination and replay results only:

- in `Reserved`, quantity remains in the source;
- in `PickedUp` or `RecoveryRequired`, quantity remains in the carrier inventory;
- in `Delivered`, quantity belongs to Storage or Construction staging.

Transfer records are bounded scenario diagnostics. Terminal records may be
evicted only after no job/order references them. A new scenario uses a new
context and rejects every old transfer ID.

## 5. Transfer invariants

For one transfer quantity `Q`, exactly one physical owner holds all `Q` units at
each committed phase boundary. Reservations constrain future mutations but do
not move ownership.

The implementation must enforce:

1. `Q > 0`.
2. `Q <= unreserved source quantity`.
3. `Q <= compatible free carrier capacity` for a ground pickup.
4. `Q <= reserved compatible destination capacity`.
5. The Resource ID and relevant capacity definition match at both ends.
6. A successful commit removes `Q` from exactly one source and adds `Q` to
   exactly one destination.
7. Failed validation or destination callback changes neither owner.
8. Repeating the same transfer ID and identical payload returns the recorded
   result without applying quantity again.
9. Reusing a transfer ID with different source, destination, resource, carrier
   or quantity is rejected as a payload mismatch.
10. Ground quantity, inventory quantity, Storage quantity and staged quantity
    never become negative or exceed their own distinct capacities.

Ground-stack capacity, each inventory slot's capacity/natural slot count,
Storage capacity and Construction remaining requirement are separate constraints
even when their current numeric values happen to match.

## 6. Partial pickup transaction

### 6.1 Prepare and reserve

For a ground-source haul, calculate:

`Q = min(source available, carrier compatible free capacity, destination reservable capacity, resource transport limit)`

The current Soil/Stone transport limit is the compatible inventory capacity; no
new weight or caste rule is approved. Future Resource Definitions may add an
explicit transport constraint.

Preparation is read-only. Commit reservations in this order:

1. reserve exact destination capacity for the transfer ID;
2. reserve exact source quantity for the same transfer ID; and
3. reserve the existing scheduler job for the carrier.

If a later reservation fails, release every earlier reservation owned by that
transfer. The first implementation should preserve one active pickup reservation
per ground stack and one active incoming reservation per destination line. This
keeps current exclusive competition semantics and is enough for successive
trips. Multiple simultaneous quantity reservations on one stack are deferred
until scale evidence justifies the extra bookkeeping.

### 6.2 Pickup commit

At the valid adjacent source anchor:

1. validate scenario, transfer phase, Stack ID, stack/resource identity,
   reservation ownership and exact `Q`;
2. recheck compatible inventory capacity;
3. add exactly `Q` to `AntInventory` and subtract exactly `Q` from the canonical
   stack through one conserving commit;
4. end the source reservation and mark the transfer `PickedUp`; and
5. retain the destination reservation during travel.

If positive quantity remains, the source keeps the same `GroundStackId` and
advances its revision once. At zero, it enters `Removed`, its cell mapping is
removed and the old ID never resolves again. Partial pickup never creates a new
ground stack.

The transfer registry must also reserve or earmark `Q` matching units in the
carrier inventory while the transfer is in flight. Other inventory operations
may read those units but may not remove them. This prevents an explicit drop or
unrelated recovery operation from consuming cargo promised to a destination.

## 7. Partial delivery transaction

“Partial delivery” means an operation may deliver less than the carrier's total
matching inventory, less than the original ground stack and less than the
destination's total recipe/capacity. One transfer itself still commits its exact
reserved `Q` in full. It must not silently shrink after pickup.

At a valid adjacent destination anchor:

1. validate transfer, carrier allocation, resource, destination identity/function
   and exact incoming reservation;
2. remove exactly `Q` from the carrier and add exactly `Q` to the destination in
   one conserving callback transaction;
3. release the carrier allocation and destination reservation;
4. mark the transfer `Delivered`; and
5. complete the one-trip scheduler job and award Hauling XP once for `Q`.

Construction staging may reserve at most its still-undelivered requirement.
Storage may reserve at most its compatible free slot capacity. If an ant already
carries more matching material than one destination can accept, this transfer
delivers only `Q`; the remainder stays in inventory and produces an owner-only
self-deposit opportunity for another trip.

Construction remains ineligible until every requirement's committed delivered
quantity equals its required quantity. Reserved or carried material is not
staged material. Construction must never collect its own supply.

## 8. Replay, interruption and recovery rules

| Event | Required result |
| --- | --- |
| Repeated prepare | Return the existing compatible transfer or a clear conflict; do not add reservations twice. |
| Repeated pickup before commit | Remain `Reserved`; no quantity changes. |
| Repeated pickup after commit | Return the recorded pickup success; do not subtract or add again. |
| Repeated delivery before pickup | Reject as wrong phase; ownership remains at source. |
| Repeated delivery after commit | Return the recorded delivery success; do not remove or add again and do not award XP again. |
| Interrupted travel before pickup | Pause without changing ownership. Retain reservations while the order is valid; explicit cancellation releases both. |
| Cancellation before pickup | Release source, destination and job reservations; source quantity and ID remain unchanged; mark `CancelledBeforePickup`. |
| Interruption after pickup | Quantity remains in reserved carrier inventory. A temporary pause retains a still-valid destination reservation. |
| Explicit cancellation after pickup | Release obsolete destination/job reservations, release the transfer's destination promise, keep committed quantity in the carrier, and mark `RecoveryRequired`. |
| Destination invalidation before pickup | Cancel safely and release both quantity reservations; source stays unchanged. |
| Destination invalidation after pickup | Keep cargo in the carrier, release the invalid destination reservation, mark `RecoveryRequired`, then create an owner-only self-deposit transfer to a newly selected valid destination. |
| Ant becomes temporarily inactive | Do not delete or teleport cargo. Release the destination if it cannot remain valid and keep quantity in that ant's authoritative inventory pending reactivation/recovery. |
| Ant permanently dies/inactivates | Keep the inventory as physical owner. Schedule explicit recovery to a compatible ground cell or destination; never silently credit Storage. |
| No valid recovery ground cell | Keep quantity on the inactive ant and expose a pending recovery reason. Retry only after relevant cell/destination revision or explicit recovery action. |
| Stack depleted by the winning transfer | Terminate the stack once; stale competitors fail by Stack ID/revision and release their own reservations. |
| Reservation competition | One transfer wins each exclusive source/destination reservation. Losers remain unowned/paused and retry through existing backoff/fair scheduling. |
| Pause | Simulation time, travel and commits stop. Reservations and phases remain unchanged. |
| Scenario replacement | Quiesce/cancel active transfers through their phase rules before replacement where possible. The new scenario rejects old Stack/transfer IDs; rebuild failure still follows `WV-B3B-LIM-001`. |

Recovery is an explicit transaction, not an exception that writes directly to a
destination. The source for recovery is the carrier inventory and the transfer
amount is bounded again by the newly selected destination.

## 9. Ground merging, capacity and overflow

The B3C.2A ground rules remain unchanged:

- one canonical stack and one Resource ID per cell;
- compatible same-resource additions merge without replacing the Stack ID;
- a merge advances the existing stack revision and publishes one committed cell
  change;
- different resources cannot occupy the same cell;
- a full stack accepts nothing; and
- any failed merge leaves the current owner unchanged.

For an inventory-to-ground or future Node-to-ground operation larger than the
first cell's remaining capacity, use a separate prepared
`GroundPlacementOperation`:

1. validate the source quantity and resource;
2. test the requested cell first;
3. test a bounded, versioned deterministic sequence of compatible resident
   cells;
4. prepare exact per-cell quantities and expected cell/stack revisions for the
   complete amount;
5. revalidate all destinations; and
6. consume the source once, then commit all prepared merges/creations as one
   logical operation with one replay identity.

No candidate may be chosen from hash/dictionary iteration order. New Stack IDs
are allocated in prepared candidate order from the current scenario context.
Existing compatible stack identities remain stable.

The smallest WV-B3C.2B implementation needs this prepare/revalidate primitive
for inventory recovery and tests, but must not activate variable Node output.
If complete placement cannot be prepared, the current physical owner retains the
entire quantity. A future whole-node Extraction that cannot retain its source
requires the recoverable pending-output record already specified for B3C.3; the
Node must not deplete until that representation is safely prepared.

## 10. Scheduler and successive-trip integration

Retain `ColonyJobRegistry`, per-ant work priorities, priority-before-path-cost,
the bounded fair candidate pass, deterministic ties and current retry/backoff.
Do not add a transfer-specific scheduler.

One dynamic ground-stack job represents one trip, not an instruction to empty an
arbitrary stack. Its eligibility calculation must use the current unreserved
quantity and a currently compatible destination. After a successful partial
delivery:

- complete that trip and its transfer;
- if the source stack still has unreserved quantity, remove/recreate the terminal
  dynamic job deterministically on the normal refresh;
- reuse the existing stable opportunity job ID only for the fresh job record,
  while allocating a new transfer identity for the next trip; and
- never reuse the prior delivered transfer ID.

If no ant can carry any unit or no destination can reserve any unit, keep the
opportunity paused and unowned with a concrete dependency reason. Do not reserve
the whole stack, repeatedly fail the same order or permanently block later jobs.
Reservation losers use the established failure backoff and re-enter the fair
candidate pass.

An owner-only self-deposit job remains the recovery route for carried material.
Its quantity is recalculated per destination reservation, so a partly filled
Storage slot or nearly complete Construction Site cannot strand the remainder.

## 11. Change publication and quantity badges

Presentation always shows committed ownership, not promises:

- source reservation: ground badge is unchanged;
- partial pickup commit: ground badge decreases once; it disappears only at
  committed zero;
- merge commit: the surviving ground badge increases once;
- delivery reservation: destination badge/staging display is unchanged;
- Storage delivery commit: Storage badge increases once and continues to hide
  when quantity is zero;
- Construction delivery commit: selected-site staging progress increases once;
  reserved incoming quantity is shown only in developer diagnostics, never as
  delivered material; and
- replay/failure/cancellation without a quantity commit changes no quantity
  badge.

Ground reservation changes may continue to advance stack revision and publish a
loose-resource change because reservation state is part of the immutable unified
snapshot. Each successful ground quantity mutation emits one committed
affected-cell change per changed ground cell. A multi-cell placement operation
publishes one coherent ground batch. Storage and Construction staging remain
their own owners; their presentation refreshes from committed owner state rather
than fabricating runtime-cell occupancy. Failed/replayed no-op operations publish
nothing new.

Current production UI has no general Node selection or selected Construction
Site staging panel. Those displays remain future integration. This milestone
must not fabricate them or alter the accepted rule that Node badges are
contextual while loose-stack and non-empty Storage badges are continuous.

## 12. Automated test strategy

### 12.1 Focused Edit Mode

Add or extend tests for:

1. partial pickup selects the minimum of source, carrier, destination and
   resource transport bounds;
2. a transfer delivers only its exact quantity and leaves unrelated carrier
   inventory untouched;
3. partial pickup preserves Stack ID and advances revision once;
4. zero pickup removes the stack and stale ID access fails;
5. same-resource ground additions merge into the existing ID;
6. full-stack, mixed-resource and over-capacity placement reject without source
   mutation;
7. two source claims produce one winner and a reservation-free loser;
8. two destination claims cannot overbook Storage or staging;
9. cancellation before pickup preserves the source and releases all reservations;
10. cancellation after pickup preserves exact carrier quantity and creates a
    recoverable owner-only path;
11. pause/interruption retains the correct phase and explicit cancellation
    releases exactly once;
12. identical pickup/delivery replays are no-ops with the original outcome,
    while mismatched payload replays fail;
13. temporary/permanent inactivity never loses cargo and recovery waits safely
    when no compatible ground/destination exists;
14. old Stack and transfer IDs fail after depletion/scenario replacement;
15. multiple partial deliveries satisfy Construction staging exactly, and
    Construction remains ineligible until all requirements are committed;
16. partial Storage delivery respects filter, resource identity and free
    capacity;
17. badge rules/read projections observe committed quantities only and do not
    mutate state;
18. scenario rebuild creates fresh identities, clears reservations and follows
    the existing restore path on injected failure; and
19. existing ground stack, pickup, Hauling, Construction, scheduler, runtime-cell
    and complete Edit/Play regressions remain green.

Also test successive trips from one stack, terminal-job cleanup/recreation,
single XP award per delivered transfer, exact change counts, deterministic
multi-cell candidate order and all-or-nothing no-fit behavior.

### 12.2 Focused Play Mode

- Run a large-stack autonomous haul through at least two trips and verify the
  same source Stack ID survives until the final pickup.
- Exercise both Construction staging and Material Storage destinations.
- Pause before pickup and after pickup, then resume or cancel.
- Invalidate a destination after pickup and verify owner-only recovery without
  duplication.
- Observe source, Storage and staging displays after commits and view switches.
- Rebuild/restore and verify old identities and reservations cannot act.

### 12.3 Safe large-stack fixture

Do not change production Node quantities. Tests should call the existing
canonical registry API with a stable test-only producer key to place, for
example, **7 Soil** at a validated resident empty Colony cell. Configure the
test ant/destinations so each trip can move only 2–3 units. This exercises
partial Hauling without changing a Resource Definition or generated Node.

For manual acceptance, add a **Developer Only** action in the separately
approved implementation that uses the same validated registry transaction to
create/reset one labelled large Soil stack. The control must be development-
host-only, deterministic, bounded to a known compatible cell and absent from
normal player HUD/gameplay. It must not mutate Node yield or bypass stack
capacity.

## 13. Practical Unity acceptance — generated seed `2203`

After focused and complete automated suites pass:

1. Start seed `2203` in Unity `6000.3.8f1` and confirm the founding scenario,
   queen, shared scheduler and view controls are normal.
2. Use the Developer Only large-stack action to create one 7-unit Soil stack at
   its labelled compatible Colony cell; record its Stack ID and revision.
3. Provide a destination with only 2–3 compatible units of capacity and enable
   Hauling.
4. Confirm one ant reserves only the transferable quantity; the ground badge
   still shows 7 while travel is only reserved.
5. Confirm pickup reduces the badge by exactly the carried quantity and preserves
   the Stack ID while a remainder exists.
6. Pause during source travel and during loaded travel; confirm phases,
   quantities and reservations remain stable, then resume.
7. Confirm delivery increases the chosen Storage or Construction staged quantity
   exactly once and awards XP once.
8. Confirm the remaining source becomes eligible for a later trip rather than a
   terminal or permanently blocked job.
9. Repeat until one destination fills; confirm the next trip selects another
   compatible destination or waits unreserved with a clear dependency.
10. Compete with a second ant and confirm only one transfer owns each exclusive
    source/destination reservation.
11. Cancel one trip before pickup and confirm the stack is unchanged. Cancel one
    after pickup and confirm cargo remains on that ant and later self-deposits.
12. Invalidate a destination after pickup and confirm cargo is recovered through
    a new explicit owner-only transfer.
13. Finish all trips and confirm the source stack disappears only at zero, no
    duplicate quantity exists and Construction starts only when fully staged.
14. Switch Colony/World views repeatedly and make the existing entrance trip;
    confirm no badge leaks and generated Surface Nodes remain `GenerationOnly`.
15. Restore/rebuild the scenario and confirm the prior Stack/transfer IDs no
    longer resolve and no reservation survives.
16. Exit Play Mode and confirm the Console has no new errors or exceptions.

The manual report must distinguish these observations from automated evidence.

## 14. Migration risks and mitigations

| Risk | Evidence in current implementation | Mitigation / rollback |
| --- | --- | --- |
| Quantity-one assumptions | Production Soil/Stone Extraction currently yields one; many flows rarely exercise repeated trips. | Use test-only canonical large stacks and keep production yields unchanged. Feature-gate the new coordinator so the old one-trip path can be restored during implementation. |
| Ant ID used as reservation identity | Source, Storage and staging reservations use the ant ID. | Introduce transfer ID ownership through compatibility overloads; keep old APIs until all call sites/tests migrate. |
| No transfer replay ledger | Conserving callbacks are atomic once, but successful requests are not replay-idempotent. | Record immutable payload and terminal outcomes before replacing old entry points. Keep bounded scenario-local diagnostics. |
| Inventory has no in-flight allocation | Matching cargo can be removed by unrelated inventory operations. | Add transfer-owned inventory reservation/allocation checks without moving inventory authority. Preserve existing ordinary inventory APIs through unreserved-quantity adapters. |
| One reservation per stack/destination | It serialises competition and can make `AvailableQuantity` look usable although a stack is exclusively held. | Preserve exclusive first-slice semantics, report exclusive-held availability clearly in scheduler diagnostics, and defer concurrent split claims. |
| Dynamic job terminal lifecycle | One trip completes a stack-keyed job; refresh implicitly recreates it. | Add explicit successive-trip tests and deterministic remove/recreate rules. Never treat job completion as source depletion. |
| Storage one-slot semantics | Each legacy Storage cell accepts one type, capacity 10 and one incoming owner. | Keep filters and capacity authoritative; do not generalise Storage or begin Placement migration. |
| Construction staging semantics | One reservation owner per resource requirement; reserved is not delivered. | Use transfer IDs and exact remaining-requirement bounds; preserve fully-staged eligibility. |
| Deterministic Stack ID allocation | Multi-cell commit order affects allocated IDs. | Freeze source-first, versioned candidate order and assert identities across repeated test construction. |
| Change-set overpublication | Existing reservations and quantity changes each publish immediately. | Define one event per committed single-cell mutation and one coherent batch for multi-cell operations; replays publish none. |
| No valid recovery cell | A dead/inactive carrier cannot safely discard cargo. | Leave quantity in authoritative inventory with `RecoveryRequired`; retry explicit placement/delivery after relevant changes. |
| Scenario replacement is not atomic | Mutable services rebuild in place under `WV-B3B-LIM-001`. | Reject old context IDs, quiesce transfers before replacement, retain existing restore path and add injected-failure coverage. |
| Older whole-stack expectations | Tests may assume job completion means source disappearance or one reservation owns all source quantity. | Search and update only assertions contradicted by the approved partial contract; preserve legacy compatibility overloads during rollout. |

## 15. Recommended smallest implementation sequence

The safest implementation is one focused milestone delivered in reversible
steps, with tests added before each caller migration:

1. Add typed scenario-local transfer identity, immutable transfer snapshots and
   replay tests. No scheduler behavior changes yet.
2. Add transfer-ID compatibility overloads for exclusive ground, Storage and
   Construction reservations plus carrier allocations.
3. Route autonomous ground pickup and destination delivery through the
   coordinator; retain current destination selection and one-trip order shape.
4. Make terminal dynamic-job cleanup/recreation explicitly support successive
   trips and owner-only recovery.
5. Add deterministic prepared ground-placement splitting for conserving
   inventory recovery/test operations, without enabling variable Node output.
6. Add the Developer Only large-stack fixture, diagnostics and badge/read-model
   assertions.
7. Run focused and complete Unity suites, strict documentation validation and
   the seed-`2203` manual checklist before marking WV-B3C.2B complete.

Rollback is removal of the new coordinator routing behind its composition seam,
not restoration of serialized assets. Keep old APIs until full regression and
manual acceptance prove the cutover; remove compatibility paths only in a later
explicit cleanup.

## 16. Decisions fixed for the proposed slice

- One transfer has one exact quantity and commits each leg at most once.
- Reservations are transfer-owned but remain exclusive per source/destination in
  the first slice.
- One scheduler job represents one trip; a remaining stack creates a later trip.
- In-flight quantity remains in `AntInventory`, with a transfer-owned allocation.
- Cancellation after pickup and destination invalidation use owner-only recovery.
- A no-fit ground operation leaves quantity with its current owner.
- Production Node yields and all Surface behavior remain unchanged.
- Quantity badges display committed quantities, never reservations.

## 17. Remaining non-blocking decisions

These do not block the proposed exclusive, scenario-local first implementation:

- whether later scale work permits several simultaneous quantity reservations on
  one source or destination;
- the final production radius/candidate budget for whole-node overflow when
  variable Nodes are activated in B3C.3;
- persistence format and replay-ledger retention across save/load;
- policy/timing for automatically recovering inventory from permanently dead
  ants;
- resource weight, liquid carriers and other resource-specific transport rules;
  and
- final Storage/Placement capacity and selection UI after WV-B3D.

Approval of this plan would authorise none of those future decisions by itself.
