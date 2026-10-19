# Ant AI and Job Management

## WV-B3D.3B completion and immediate hauling

New Material Storage now enters hauling selection immediately after its direct
authoritative Placement completion. The existing Hauling authority can deposit
a compatible ground stack into the delegated slot and can later withdraw from
that same slot to stage another Construction Site. Construction still performs
work only after all materials are staged; it does not transport resources.
Terminal completion replay does not award XP or consume staging twice, and a
blocked completion retains the Site for retry. Automated suites passed; the
manual acceptance checklist remains pending.

## WV-B3D.3A Storage authority closure

WV-B3D.3A is complete after user-confirmed 29/29 Unity manual acceptance.
Hauling and Material Transfer behavior remains unchanged: existing promoted
Storage delegates its one physical slot to an authoritative Placement handle,
while `MaterialStorageWorld` continues to own quantities and reservations.
Partial/multi-source hauling and Storage-to-Construction withdrawals retain
their exact-once behavior. Construction remains work-only after Hauling stages
all requirements. Newly completed Storage remains legacy until WV-B3D.3B.

## WV-B3C.2B transfer lifecycle — complete

The B3C.2B implementation keeps the existing shared job registry,
priority-before-path-cost ordering, bounded fair scan, retry/backoff and
exclusive competition. A dynamic source job starts one single-destination run;
that run can collect the same resource from several nearby ground stacks before
delivery. Completed Material Storage also creates a source job when compatible
Construction staging exists. Storage never supplies Storage.

The scenario-local transfer ID owns source/destination reservations and each
source has its own replay-safe pickup operation ID. Planning evaluates at most
24 sources, visits at most eight and rejects excessive detours. After pickup, quantity remains in the
carrier's authoritative inventory; cancellation, inactivity or destination loss
records recovery-required cargo for the owner-only self-deposit route. Scenario
replacement invalidates old transfer IDs. A transfer-owned inventory allocation
prevents unrelated work from consuming in-flight cargo. The first manual pass
identified the recovery defects; after correction, the user confirmed the full
authoritative checklist passed **30/30**. See the
[`Partial Hauling Integration Plan`](https://github.com/Stevr0/AntGame/blob/main/Documentation/PartialHaulingIntegrationPlan.md).

## WV-B3C.2A source identity and reservation visibility

Loose-resource jobs now carry the canonical scenario-local Stack ID returned by
`WorldMaterialDropRegistry`; producer operation keys remain replay guards and do
not become a second resource owner. Exclusive pickup/Hauling reservations update
the stack's reserved/available quantities and revision, while failed creation,
reservation or destination commits preserve the source. Existing partial loads,
destination-aware Construction staging and Material Storage delivery are
unchanged. Rebuilding a scenario creates a new stack context, so old job source
IDs and reservations cannot target restored resources.

## Resource and Node v1.0 job boundary

Normal Solid Extraction is whole-node work: after complete output capacity is
prepared, it depletes the Node and commits every remaining unit. Hauling alone
moves physical output to Construction staging, Storage or future Refining.
Construction never self-supplies. WV-B3C.3A implemented variable Colony quantity
ownership; WV-B3C.3B now implements whole-node output and deterministic overflow
stacks with safe pre-commit blockage rather than a post-depletion pending-output
ledger.
See [Resource and Node System](resource-and-node-system.md).

## WV-B3C.3A deferred Extraction eligibility

At B3C.3A closure, generated variable-quantity Colony Nodes could be designated
and inspected, but the shared selector rejected them before reservation with the
explicit WV-B3C.3B dependency reason. They remained pending and unowned, so the
queen could select other eligible work and did not loop on impossible legacy
Extraction. WV-B3C.3B now supersedes that temporary gate with whole-node
eligibility while retaining quantity-one fixture behavior.

The user confirmed this scheduling boundary in Unity as part of the completed
WV-B3C.3A **25/25** manual acceptance: while variable-quantity Extraction stayed
deferred, the queen could undertake and progress other eligible work. Codex did
not independently perform the manual check.

## WV-B3B authority preservation

Runtime-cell snapshots and change feeds are read-only inputs for future job
integration. The shared `ColonyJobRegistry`, bounded fair selection passes,
per-ant priorities, eligibility, path-cost comparison and exclusive reservation
remain authoritative and unchanged. WV-B3B does not add Surface jobs or make
generation-only nodes eligible for Extraction.

## Scheduler fairness acceptance and WV-B3A boundary

The bounded resumable full-registry scheduler correction is now manually
accepted. It remains the one Colony job selector; WV-B3A adds no scheduler.
Future Node, Stack and Placement jobs use typed region targets and the existing
reservation/priority model through narrow adapters. Pause and interruption keep
quantity at its last committed owner. See [Unified Runtime
Cells](unified-runtime-cells.md).

## Unified resource-job eligibility

Extraction, Hauling and Construction are distinct shared-registry jobs.
Extraction always creates loose output. Hauling is eligible only when an
unreserved transferable source and reachable reservable Construction Site or
Material Storage destination both exist; pending construction staging is chosen
before optional Storage. Construction is eligible only when its site is fully
staged. Failed dependencies remain pending/unowned and are reconsidered when
physical state changes, preventing rejected-job loops without another scheduler.

## WV-B1 region boundary

WV-B1 does not add autonomous cross-view routing or Surface jobs. Existing survival and Colony-work evaluators continue to advance persistent ant needs from the shared clock, but they issue Colony-local navigation/work only when the ant's authoritative residency is the Colony region. A Surface-resident ant can receive direct player Surface movement or a manual entrance request; it cannot accidentally claim a Colony `Vector2Int` destination from the inactive view. Existing Colony jobs and reservations are not reset by camera switching or representation handoff.

## Approved job model

Future autonomous work uses a per-ant matrix of priorities `1–4` plus **Disabled**. The highest eligible priority wins before the closest reachable job by path cost. Skills change efficiency; they do not override player-assigned priority.

Ants may also receive temporary explicit orders. Critical survival may interrupt ordinary work at a safe point. Valid partial progress persists, and exclusive reservations prevent two ants from consuming or owning the same work target. Drafted survival intervention remains unresolved.

## Current implementation

Milestones through 3.3D are accepted, and 3.4A extends that foundation with physical Background construction:

- Every ant record owns independent priorities for Excavation, Surface Preparation, Construction, Hauling, Foraging, Production, Combat, and Brood Care.
- `ColonyJobRegistry` owns synthetic job identity, target, status, reservation, progress, and failure state outside GameObjects.
- Highest enabled priority is evaluated before actual A* route cost. Equal costs use stable deterministic ties.
- Selection uses a deterministic per-ant pass through explicit registry creation
  order. Each staggered evaluation examines at most 32 entries, but the pass
  continues on later evaluations until every pending entry had fair access.
- The selector retains the best reachable candidate for each priority during a
  pass and assigns only after the pass completes. This preserves highest
  priority before actual A* route cost and stable category/Y/X/ID ties without
  pathfinding the complete list in one simulation tick.
- New jobs append to a partial pass without moving its cursor. Removal and
  work-priority revisions restart safely. Completed, cancelled, reserved or
  newly ineligible candidates are revalidated before assignment.
  Scan-in-progress uses the normal evaluation interval; genuine no-work/failure
  results retain the existing 2/4/8-second backoff.
- `SyntheticColonyJobOrder` remains as a regression fixture. Real designations use `ExcavationJobOrder` through the same registry, selector, motor, adjacency, priority and interruption seams.
- Extract mode accepts eligible foreground Resource Nodes by world coordinate, supports bounded area drags, and blocks ordinary selection/orders while active.
- Soil and Stone extraction uses a stable job-keyed transaction. The complete yield always becomes loose output at the excavated cell; excavation never mutates inventory or assigns hauling. Output failure preserves the Solid; successful removal preserves Background and refreshes one visual/collision/navigation locality.
- Work takes a provisional four simulation hours, uses Rested and relevant-skill speed, and awards proportional XP only for committed progress.
- Player orders and critical survival pause work and release ownership while preserving progress. Ordinary survival waits for active work.
- Explicit player pickup reserves one loose drop quantity and travels through the same shared navigation/adjacency seam. This is an order, not an autonomous Hauling job.
- Autonomous Hauling jobs reserve one or more compatible loose sources and one Construction/Storage destination, then use the same motor to visit each valid source anchor before one destination commit. Completed Material Storage may be a Construction source; already-carried Soil/Stone creates an owner-only self-deposit task.
- Dynamic Hauling performs read-only source, carrying-capacity, bounded route and compatible-destination checks before autonomous selection. A compatible Construction Site is valid without Storage; stored material never creates a Storage-to-Storage route. Otherwise the haul remains paused/unowned and is reconsidered when source, staging or Storage capacity changes.
- Actual A* travel cost selects the closest reachable compatible storage with deterministic ties. Disabled Hauling blocks dynamic claims; critical survival and explicit player orders interrupt safely.
- The development scene exposes deterministic job, excavation and loose-drop fixtures plus priority, inventory, cancellation, reset, and diagnostic controls.
- Background Conversion designations create real Construction jobs. The exclusive builder uses inventory first, otherwise reserves reachable storage/drop material, stages it at the site, performs timed work and commits the Background transaction.
- Autonomous selection screens construction for obtainable physical material before it reserves an ant. A job with no carried, reachable stored or reachable unreserved loose material remains paused and unowned with an explicit waiting reason, allowing lower-priority eligible work to proceed. The same job is reconsidered when material becomes reachable.
- A construction builder with no usable inventory capacity temporarily unloads unnecessary cargo through a reusable recovery operation: closest reachable reserved storage first, validated recoverable current-anchor drop second, then automatic resumption of the same job. Critical survival and player orders may interrupt this state safely.
- Construction interruption preserves progress, carried material and staged material. Cancellation releases reservations and exposes staged material as a recoverable drop.

Foraging discovers resource locations, Extraction physically removes resource nodes, Hauling collects/transports loose material to staging or Storage, and Construction performs fully staged work. Extraction is the authoritative runtime work category; `Excavation*` type names are compatibility seams.

Milestones 3.4A, 3.4B and 3.4C are manually accepted. Milestone 3.4D is **Implemented — Pending Manual Acceptance**. Its global player-facing matrix lists active authoritative ant records whether or not their Colony View visual is loaded. Extraction, Construction and Hauling are the functional columns. Cells display mapped skills and edit the existing record-owned 1–4/Disabled values; developer diagnostics read the same values.

Changing a matrix value resets subsequent autonomous evaluation but never cancels an active job, changes a skill, pauses simulation or changes a developer autonomy override. Current activity and real failure/waiting text are displayed where the existing agent/job state exposes them. Surface Preparation, Foraging, Production, Combat and Brood Care stay out of the functional player matrix until real executors exist.

The fairness correction exposes read-only diagnostics for total entries, next
position, entries examined in the last step, pass completion and selected job.
Focused tests cover a reachable job after 32 unreachable jobs, deterministic
priority/tie behavior, insert/remove/complete/repriority, multiple ants,
exclusive reservations, pause and 3× resume. Scale cases prove a final eligible
entry is found in 1/2/8/32 evaluations for 32/64/256/1,024 entries while never
examining more than 32 entries in one evaluation. Manual stress acceptance is
still pending; normal registries of 32 or fewer complete in one evaluation.

## Future decision flow

```mermaid
flowchart TD
  A[Ant available] --> S{Critical survival?}
  S -- Yes --> I[Interrupt safely and reserve survival interaction]
  S -- No --> O{Explicit order?}
  O -- Yes --> E[Validate and execute order]
  O -- No --> P[Find highest enabled work priority]
  P --> R[Filter eligible reachable unreserved jobs]
  R --> C[Choose lowest path cost]
  C --> X[Reserve, travel, work, and commit]
  I --> X
  E --> X
  X --> A
```

## Required architecture boundaries

- Job choice must not live in UI code.
- Navigation calculates routes; it does not decide why an ant travels.
- Work progress and physical resources remain authoritative outside visuals.
- Cancellation and interruption must release reservations exactly once.
- Construction remains distinct and begins only after Hauling completes staging.
- Hundreds-of-ants goals require controlled update rates and shared data, but do not justify DOTS/ECS before profiling.

## Future cross-region work boundary

WV-A preserves the active job/order identity and valid partial work while an ant changes regions. A cross-view destination is region-scoped and composes a source-local route, one capacity-one entrance transaction and a destination-local route. The current Colony `Vector2Int` job targets and A* service remain unchanged; they are not valid Surface coordinates. An occupied or invalid entrance produces a recoverable waiting/failure state and releases reservations only through the owning job's existing cancellation rules. Autonomous cross-view selection and cost comparison are deferred to WV-D. See [World View Foundation](world-view-foundation.md).

## Not implemented

Excavation, personal inventory, explicit pickup, Material Storage, autonomous Soil/Stone hauling and functional Background construction are now real. Gathering, production, combat, brood care, forced-work orders, Draft Mode, group tactics, local avoidance, persistence, rooms and production-scale indexing require later milestones.
