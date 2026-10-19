# Design Decision Log

## 2026-10-19 — Cut over new Material Storage completion

- Complete the existing 1 Soil, 4-hour recipe directly into a one-cell
  authoritative Storage Placement; never create an interim functional
  Background.
- Coordinate Placement preparation, the delegated empty Storage slot and Site
  closure so rejection rolls back unpublished identity/occupancy and replay
  cannot duplicate capacity, material consumption or XP.
- Preserve the natural Background and keep `MaterialStorageWorld` as the sole
  filter/quantity/capacity/reservation ledger.
- Permit unknown HP only for this compatibility path until approved Storage
  durability values exist; do not infer damage, Repair or destruction rules.
- Expose the existing Placement visual-definition/state selection in runtime
  snapshots and diagnostics without adding renderer behavior or art assets.
- Record **615/615 Edit** and **84/84 Play** passed; retain status as
  **Implemented — Automated Tests Passed, Manual Acceptance Pending** and do
  not begin WV-B3D.4A.

## 2026-10-19 — Close WV-B3D.3A after authoritative acceptance

- Mark WV-B3D.3A **Complete** after 15/15 Placement Registry Edit, 28/28
  Storage/Hauling Edit, 3/3 Placement Registry Play, 6/6 Storage/Hauling Play,
  612/612 complete Edit and 84/84 complete Play passed with no failures, skips
  or inconclusive results, followed by user-confirmed **29/29** manual checks.
- Record the acceptance as user-reported Unity Play Mode verification in two
  stages; Codex inspected retained XML but did not independently perform or
  rerun the manual checks for this documentation-only closure.
- Preserve the delegated one-ledger Storage model, unknown migrated HP, legacy
  completion for new Storage, and open `WV-B3B-LIM-001`.
- Keep WV-B3D.3B and all other functional migrations, damage, Repair,
  destruction and broader deferred systems separately gated.

## 2026-10-19 — Cut over fixture Material Storage by delegation

- Promote the complete authored Storage batch only at quiescent scenario setup,
  retaining stable Placement IDs and rejecting partial migration.
- Keep `MaterialStorageWorld` as the sole mutable ledger and bind existing slot
  objects to Placement handles; never copy quantities or reservations.
- Treat the retained functional Background as presentation compatibility on
  promoted cells; at that point new/generated Storage remained legacy until
  WV-B3D.3B.
- Preserve unknown migrated HP and defer damage, Repair, destruction and
  contents recovery gameplay.
- Use one visual-definition base with category-specific assets so Backgrounds
  do not expose irrelevant Placement state fields.

## 2026-10-19 — Close WV-B3D.2 after authoritative acceptance

- Mark WV-B3D.2 **Complete** after 11/11 focused Edit, 3/3 focused Play,
  608/608 complete Edit and 84/84 complete Play passed with no failures, skips
  or inconclusive results, followed by user-confirmed **7/7** manual groups.
- Record the manual result as user-reported, not independently performed by
  Codex. The final reconciliation confirms zero initial completed Placements in
  generated seed `2203` and no Placement-owned contents or functional capacity
  in legacy projections.
- Preserve existing functional authorities and `WV-B3B-LIM-001`. Damage,
  Repair, destruction, dismantling, multi-cell gameplay and all functional
  authority cutovers remain separately gated.
- Treat `75bc4441` as test-working-tree parent provenance, not the final
  implementation commit `b04034f`.

## 2026-10-19 — Implement WV-B3D.2 as a non-functional Placement read model

- Make `PlacementRegistry` the scenario-scoped owner of definitions, identity,
  footprint occupancy, lifecycle, revision and HP-ready instance state only.
- Derive the six initial definitions from existing recipe data and project the
  six legacy constructed Background types without moving function/capacity.
- Use registry-context handles for stale-scenario rejection and deterministic
  IDs for identical legacy fixture state.
- Represent hidden natural Background provenance and legacy HP as explicit
  unknowns; do not invent Earth/Stone or final durability values.
- Permit future authoritative instances only with approved positive Max HP;
  clamp HP and revise only on committed changes.
- Defer damage, Repair, destruction, contents recovery and removal. Material
  Storage authority cutover remains a separate candidate milestone.

## 2026-10-19 — Specify Placement migration before moving function authority

- Complete WV-B3D.1 as documentation only; no Placement registry or functional
  cutover was implemented by that gate.
- Preserve one natural Background plus optional Node, Placement and loose stack.
  A site stages materials but provides no completed function.
- Begin later implementation with a one-cell Placement registry and read-only
  legacy projections. Migrate Material Storage first in separate authority and
  Construction-completion slices.
- Require explicit per-instance dual-function protection and exact transfer of
  contents/reservations before disabling a legacy owner.
- Recommend Prepared Surface as an exclusive non-obstructing Placement, subject
  to explicit approval before its implementation slice.
- Retain `WV-B3B-LIM-001`; defer Door, rooms, dismantling, Refining, brood
  runtime, persistence and Surface Placements.

## 2026-10-19 — Close WV-B3C.3B after authoritative acceptance

- Mark WV-B3C.3B **Complete** after focused Edit/Play 43/43 and 22/22, complete
  Edit/Play 597/597 and 81/81, and user-confirmed **8/8** manual acceptance
  groups. Codex did not independently perform the manual checks.
- Accept pre-commit output-capacity validation and safe blockage as the recovery
  policy. Clearing capacity makes the blocked job eligible again; do not add a
  post-depletion pending-output ledger.
- Record complete F12 operation/phase/plan/reservation/pending/replay/blocked
  diagnostics and a clean Console in the user-reported acceptance evidence.
- Retain `WV-B3B-LIM-001`, Surface resource deferrals, Placement migration and
  all later milestones. WV-B3D remains the next recommendation, not authorised
  work.

## 2026-10-19 — Use safe pre-commit blockage for WV-B3C.3B output

- Implement whole-node Colony Soil/Stone Extraction through existing Node,
  terrain, ground-stack, job and runtime-cell authorities.
- Prefer compatible existing stack capacity, then the extracted cell, then
  reachable nearby empty cells in bounded deterministic order.
- Reserve and revalidate the complete capacity plan before terrain or quantity
  mutation. If it cannot fit, release reservations and retain the Node/Solid;
  do not create a post-depletion pending-output record in this milestone.
- Preserve capacity 10, separate Hauling, staged Construction, quantity-one
  fixtures, `WV-B3B-LIM-001`, Surface deferrals and Placement migration scope.

## 2026-10-19 — Close WV-B3C.3A active Colony Nodes

- Accept WV-B3C.3A as **Complete** after 11/11 focused Edit, 16/16 focused Play,
  594/594 complete Edit and 81/81 complete Play passed with no failures, skips or
  inconclusive results, followed by **25/25** user-confirmed Unity manual checks.
- Record the manual result as user-reported, not independently performed by
  Codex. The final confirmations include other eligible work progressing while
  variable-quantity Extraction is deferred and no new Console errors or
  exceptions after leaving Play Mode.
- Preserve XML/log evidence under `Logs/WV-B3C3A-*`. The tested implementation
  working tree was based on parent revision `1fab6541`; that revision is
  provenance, not the final implementation commit.
- Keep whole-node Extraction, exact-once complete loose output, multi-stack and
  pending-output recovery in WV-B3C.3B. Preserve `WV-B3B-LIM-001` and all other
  separately deferred resource, Placement, Surface and simulation work.

## 2026-10-16 — Activate Colony Node quantity before whole-node output

- Make the established scenario-local Colony registry the sole mutable
  Soil/Stone Node quantity owner; keep `ChunkedWorldData` authoritative for
  supporting Solid occupancy and natural Backgrounds.
- Derive generated Soil 8–12 and Stone 5–15 quantities independently per Node
  from stable generation inputs. Retain quantity-one fixture compatibility.
- Guard all Solid removal/replacement. Permit only narrow one-use fixture/debug
  and quantity-one Extraction tokens.
- Allow variable Nodes to be inspected/designated, but keep legacy one-unit
  Extraction unowned and reject it without terrain, quantity, output or XP
  changes until WV-B3C.3B.
- Publish authoritative immutable Node snapshots and one change set per
  committed mutation. Preserve Surface `GenerationOnly` state and
  `WV-B3B-LIM-001`.

## 2026-10-16 — Extend one Hauling run across bounded sources

- Keep one resource, carrier and destination per run, while giving every ground
  or Storage pickup its own child operation identity and exact-once commit.
- Evaluate at most 24 source candidates and visit at most eight, using reachable
  route cost, deterministic ties and an excessive-detour guard. Depart with a
  partial load when no further useful source exists.
- Allow completed filtered Material Storage to supply Construction staging
  through its existing withdrawal authority. Never create Storage-to-Storage
  or multi-destination routes.
- Reserve the combined destination before travel. Cancellation after any pickup
  releases unused source/destination reservations but leaves collected material
  physically owned and recoverable in the carrier inventory.
- Make the seven-Soil developer fixture reusable with fresh producer keys,
  deterministic fallback cells and visible success/failure identity details.
- This acceptance recovery does not change capacity bands, XP/decay, Node yields,
  Surface gameplay, Placement authority or `WV-B3B-LIM-001`.

## 2026-10-16 — Implement partial Hauling around one exact transfer identity

- Keep physical quantity in the current ground, ant inventory, Material Storage
  or Construction staging authority; a transaction coordinator records phases
  and replay outcomes without becoming a resource wallet.
- Use an identity separate from Stack, job, ant and producer-operation identity.
  Make one run carry one exact quantity bounded by eligible sources, carrier and
  reserved destination capacity. The later recovery decision above adds child
  pickup identities without replacing this parent identity.
- Preserve exclusive first-slice reservations and the existing shared fair job
  scheduler. A positive stack remainder keeps its identity and creates a later
  trip rather than one permanently blocked whole-stack job.
- Keep cancellation/invalidation cargo in authoritative carrier inventory and
  recover it through an explicit owner-only transfer. Badges continue to show
  committed quantities, not reservations.
- Implement the approved 0–100 Hauling bands (2×2, 3×2, 3×3, 4×4, 5×5 and
  6×6), reuse shared decay above 50, retain threshold-crossing cargo as
  over-capacity and block only new pickup.
- Runtime implementation is complete, automated validation is recorded and the
  user confirmed the authoritative manual Unity checklist passed **30/30**.
  Variable Nodes, general multi-cell output,
  Surface resources, Placement migration, persistence and `WV-B3B-LIM-001`
  remain outside the slice.

## 2026-10-16 — Keep high-density Node quantities contextual

- Show a Colony Resource Node quantity only after that Node is selected or
  designated; the current runtime supports the designation half without
  inventing a new Node-selection authority.
- Show visible loose-stack and non-empty Material Storage quantities
  continuously because those mutable holdings need immediate operational
  readability.
- Keep badges display-only and Colony-view-gated. Generated Surface Nodes remain
  `GenerationOnly`; future discovery and Placement identity rules stay deferred.

## 2026-10-16 — Canonicalise physical ground stacks in the existing authority

- Keep `WorldMaterialDropRegistry` as the sole mutable loose-resource owner and
  enforce one typed stack per occupied Colony cell.
- Separate controlled scenario-local `GroundStackId` from exact-once producer
  operation keys; preserve identity through merge and partial pickup, terminate
  it at zero, and allocate a new ID after cell reuse or scenario replacement.
- Retain provisional Soil/Stone ground capacity 10 in Resource Definitions;
  reject mixed type, overflow and invalid or unknown runtime compatibility
  before consuming the incoming source.
- Publish immutable ID/cell/resource/quantity/reservation/capacity/revision data
  through the unified adapter. Keep inventory, Storage and Construction staging
  separate. The later approved B3C.2B scope implemented partial Hauling and
  replay-safe stack transfers; general overflow recovery and the B3C.3 Node
  cutover remain deferred.

## 2026-10-16 — Define Construction and Placement metadata and migration

**Decision:** Natural Backgrounds remain present and own no colony function.
Construction Sites own recipes, staged resources and work but expose no function
before completion. Exact-once completion consumes staging and creates a
Placement instance. Placement Definitions own stable metadata, footprint,
compatibility, recipe/work, obstruction, interaction, capacity, function, room
modifiers and lifecycle; instances own changing contents, reservations, users
and revision. The current Sleeping/Food/Water/Material/Nursery Backgrounds and
Prepared conversion are legacy representations. Doors and Refining Stations are
future Placements, not invented current code.

**Consequences:** Preserve one capacity unit for Sleeping, Food/Water/Material
Storage and Nursery where applicable, but do not assign generic capacity to
Doors, Prepared Surface or Refining Stations. Migrate one function at a time,
starting with Material Storage, and never run legacy and replacement function in
the same cell. Room bonuses apply to appropriate completed Placements; an
unenclosed Nursery keeps base function. No runtime migration or WV-B3D work is
authorised.

## 2026-10-16 — Approve Resource and Node System v1.0 and close WV-B3C.1

**Decision:** Every Solid terrain cell is a finite Resource Node with a specific
Resource Definition and positive actual quantity. Normal Solid Extraction
removes the whole Node, preserves the natural Background and commits its full
remaining quantity to recoverable physical output. Variable quantities are
approved. Output beyond one stack uses deterministic additional cells or a
recoverable pending transaction. Water is finite and needs a liquid-compatible
representation. Natural Backgrounds own no colony function; all Nursery,
Sleeping, Storage, Workshop, Refining, Door and similar functions belong to
constructed/placed improvements. Future timed Surface events create explicit
Nodes or loose stacks and never regenerate extracted terrain.

**Consequences:** `Documentation/ResourceAndNodeSystem.md` is authoritative.
Current quantity-one Soil/Stone and functional Background systems remain
compatibility implementations. B3C.2/B3C.3 should be split into smaller
definition/stack, overflow-recovery, active-Node and transaction-cutover gates.
No runtime work is authorised by this decision. The user accepted all 17/17
WV-B3C.1 manual checks after its recorded green automated suites, so WV-B3C.1 is
Complete. `WV-B3B-LIM-001` remains active.

## 2026-10-16 — Add inactive Colony Node identity without moving writers

**Decision:** Implement WV-B3C.1 as an inactive, scenario-scoped diagnostic
registry for registered Colony Soil/Stone Solids. Generated IDs retain
configuration/version/seed and generated provenance; fixture IDs use a
versioned fixture namespace, seed, actual Solid ID and typed cell. Records are
quantity one and revisioned, but cannot mutate gameplay.

**Consequences:** `ChunkedWorldData` remains terrain/effective quantity-one
authority and existing Extraction/output remains unchanged. Relevant Solid
mutation invalidates the matching diagnostic record without bidirectional
synchronisation or a competing cell event. Old scenario handles reject reads
after replacement. Surface Nodes, canonical loose stacks and authority cutover
remain deferred to later gates; `WV-B3B-LIM-001` remains accepted. Automated
tests pass. The later user confirmation closed the manual 17/17 gate; see the
newer decision above.

## 2026-10-15 — Close WV-B3B and retain whole-world atomicity limitation

**Decision:** Close WV-B3B after automated validation passed and the user
confirmed **16/16 manual Unity acceptance checks passed**. The completed scope is
the typed, immutable, read-only runtime-cell foundation with region revisions,
independent change-feed cursors, compatibility validation and atomic publication
of the Colony/Surface read-model pair. Existing gameplay writers remain in
place; WV-B3C and WV-B3D are not authorised by this closure.

**Accepted limitation — `WV-B3B-LIM-001`:** Atomic read-model replacement is not
transactional construction/publication of the whole mutable gameplay scenario.
The development host retains its in-place construction and rebuild/restore
path. Reassess this before persistence, region unloading or complex scenario
restoration.

## 2026-10-15 — WV-B3B publishes a read model without moving writers

**Decision:** Implement the shared cell foundation as immutable region adapters
over current authorities. Publish Colony and Surface adapters as one validated
pair, use caller-owned cursors over bounded change journals, and classify legacy
data as compatibility-projected or details-unavailable rather than inventing
missing state.

**Consequences:** Terrain and loose-drop commits can invalidate exact Colony
cells; current Surface changes only by republication. Generated nodes remain
generation-only. Compatibility validation is pure and does not alter existing
placement rules. The development host retains its existing rebuild/restore
limitation, and WV-B3C/B3D are required before mutable Nodes or Placements move
into the unified authority.

## 15 October 2026 — Approve unified runtime cell authority and staged migration

**Decision:** Each resident Colony or Surface cell has one natural Background
and optional Node, Placement and one-type loose stack under one region-scoped
mutation boundary. Generated snapshots are immutable initial data; local
navigation/rendering are projections. Existing runtime owners migrate through
read-only adapters and one authority at a time.

**Consequence:** WV-B3A completes documentation only. Candidate WV-B3B/B3C/B3D
slices require separate approval. Colony/Surface coordinates and navigators,
entrance/residency, current exact-once logistics and fixtures are preserved.
Functional Backgrounds cannot run alongside an active replacement Placement.
Foraging knowledge and Fog of War remain separate from physical state; Refining
remains separate from Construction.

**Authority:** `Documentation/UnifiedRuntimeCellSpecification.md`.

## 15 October 2026 — Bound fair colony-job candidate evaluation

- Preserve the shared `ColonyJobRegistry`, selector, work priorities, path-cost
  ordering and exclusive reservation; do not create a second scheduler.
- Replace first-32 pre-reachability truncation with a deterministic per-ant pass
  over explicit registry creation order, examining at most 32 entries per
  autonomous evaluation and selecting only after the full pass.
- Keep the best reachable candidate per priority so 1–4/Disabled remains
  authoritative before closest path cost and stable category/Y/X/ID ties.
- Let append-only additions join a partial pass; restart after removal or
  priority revision, and recheck terminal, reservation, eligibility and
  reachability state before assignment.
- Treat an unfinished page as normal scheduler progress, not a failed job, while
  retaining the existing failure backoff for genuine no-work/failure results.
- Expose bounded read-only diagnostics and keep pause plus 1×/2×/3× behavior on
  the existing simulation clock. Manual scheduler stress acceptance remains
  separate from the now-complete WV-B2B milestone.

## 15 October 2026 — WV-B2B-ARCH-001 unified resource logistics

- Make Extraction the single authoritative skill/category for Soil, Stone,
  Seed, Twig and future resource-node removal; retain former Excavation numeric
  values and compatibility names so earned experience/priorities survive.
- Use Resource Node → loose ground resource → Hauling → Construction staging or
  Material Storage → Construction. Extraction never fills a destination.
- Make Hauling the sole normal transporter with explicit source and destination
  reservations; prefer compatible unmet Construction staging over optional
  Storage.
- Make Construction work-only and eligible only after every site-owned material
  requirement is staged. This supersedes all builder self-supply decisions.
- Preserve exact-once output/consumption, cancellation recovery, shared jobs,
  navigation, the founding queen and existing recipes. Defer Surface interaction
  and WV-C1.

## 15 October 2026 — Destination-aware Hauling eligibility

- Keep dynamic loose-resource Hauling in the shared job registry, but require a
  reachable compatible Construction Site or Storage destination before selection.
- Record an unmet destination as an unowned paused dependency without reserving
  the drop, destination, job or ant.
- This entry's former builder-self-supply bootstrap is superseded by
  WV-B2B-ARCH-001 destination-aware Hauling to Construction staging.

## 15 October 2026 — WV-B2B generation version 2 defect recovery

- Advance generation identity to version `2` / `wv-b2b.default.002` because the
  guaranteed founding-chamber Stone face changes deterministic output.
- Guarantee one quantity-one `solid.stone` node at Colony `(-4,-8)` beside the
  open chamber; retain ordinary excavation adjacency and conservation rules.
- Filter construction with no obtainable physical material before autonomous
  reservation so the founding queen can select other eligible work. Keep the
  job paused, unowned and reconsiderable rather than adding another scheduler.

## 2026-10-15 — Implement WV-B2B through validated compatibility adapters

- Freeze generation version `1` and configuration `wv-b2b.default.001` with
  independent domain-derived SplitMix64 Surface and Colony streams.
- Publish only a fully validated generated snapshot; preserve the previous
  scenario and seed on a failed development start.
- Project generated underground nodes to existing Soil/Stone Solids and Surface
  hazards to the existing obstacle navigator instead of replacing runtime
  excavation, construction, storage or movement systems.
- Keep generated placements/loose stacks empty and source definitions
  interaction-free until separately approved WV-C work.
- Retain the Mara/Pip/Tala fixture as the default scenario and reuse the WV-B1
  entrance transaction with scenario-supplied endpoints.

## 2026-10-15 — Unify logical cells, nodes, placements and loose occupancy

- Require one natural Background in every resident Surface and Colony cell.
- Permit zero or one Solid/Resource Node, Construction/Placement and loose stack,
  subject to compatibility; one loose resource type is allowed per cell.
- Treat Soil/Stone walls and Surface sources as node instances whose definitions
  own obstruction, activity, skill, work, output and depletion.
- Keep natural Background after node depletion and Construction; reclassify
  Sleeping/Storage/Nursery/doors as placements.
- Make Storage placement-owned one-type capacity, preserving the applicable
  one-placement-to-one-slot rule and physical accounting.
- Preserve exact-once physical output, reservations, builder self-supply and
  Construction skill progression. Do not auto-fill inventory on extraction.
- Retain typed region coordinates, separate local navigation, WV-B1 entrance
  transactions and Mara/Pip/Tala fixtures.
- Approve `128×128` Colony bounds, `7×5` chamber and three-cell passage. Weights
  remain provisional. Defer code migration, interaction and decay.

## 2026-10-15 — Specify one-seed Surface and underground Colony generation

- Approve one versioned world seed with independent deterministic Surface and
  Colony random domains; generation order and runtime timing cannot affect
  output.
- Keep the initial Surface `64×64`, predominantly Grass and approximately
  centre its one entrance.
- Make the Colony entirely underground and predominantly solid Dirt/Soil. Put
  its exit on the approximately central topmost boundary and begin with exactly
  one minimum passage and small chamber containing the founding queen.
- Preserve `entrance.founding-nest.001`, typed endpoints, capacity-one handoff,
  existing excavation/navigation authorities, rendering isolation and the
  Mara/Pip/Tala development fixture.
- Keep terrain, extractable Solid material, finite source records, loose
  physical resources and entrance features separate.
- Defer implementation to WV-B2B. `128×128` Colony bounds, exact chamber geometry,
  numerical weights, stable Surface/source IDs and PRNG/version details remain
  implementation approvals, not reopened product vision.

## 2026-10-14 — Implement WV-B1 with a reversible loaded-representation adapter

- Approve the proposed `64×64` Surface, `(0,0)` logical origin, one-unit cells, `32×32` future partition seam, fixed obstacles and no-corner-cut diagonal movement for WV-B1.
- Keep both regions and one GameObject/binding per development ant loaded. A handoff switches the enabled local motor only after one guarded destination-region commit; failure restores the recorded source approach. This is a replaceable adapter for future unloading, not a change in authority.
- Use two camera objects so position/zoom memory is independent by construction. Only the active camera and local pointer context are enabled; switching does not touch orders, residency or clock state.
- Keep accepted Colony grip/falling unchanged and add no gravity to Surface movement. The WV-B1 prompt's conflicting broad gravity sentence does not supersede the approved WV-A local-navigation contract.
- Defer every resource, natural-rest, foraging, extraction, autonomous cross-view, streaming, persistence and additional-entrance system.

## 2026-10-14 — Specify the first two-view and founding-survival contract

- Use distinct typed Colony and Surface coordinates under stable region identities; preserve the existing Colony integer-centre convention and local A* implementation.
- Keep one authoritative ant record and one region residency. A capacity-one entrance transaction performs physical approach, exact-once residency commit and rollback-safe representation handoff.
- Switch Colony/World cameras and input manually while preserving per-view camera state; switching never pauses, orders or teleports.
- Keep both first regions loaded on the accepted clock. Cross-view autonomous jobs are WV-D and unloaded simulation is WV-F.
- Use finite non-regenerating seed/freshwater sources for the first slice, direct consumption without storage, recoverable extraction output, a non-solid one-unit water payload/cache and safe natural rest.
- Recommend, but do not yet approve, a `64×64` initial Surface, `32×32` candidate Surface chunks and natural rest at half the constructed recovery rate.

## 2026-10-14 — Separate extraction output from collection and transport

- Successful Soil/Stone excavation always creates physical loose output and never adds it directly to the excavator's inventory.
- The excavated cell is the authoritative output location. Existing compatible same-cell stacks fill first, followed by at most one new bounded stack; an unrepresentable yield leaves the Solid intact.
- Foraging discovers resources, Extraction removes them, Hauling collects and transports loose resources, and Construction retains builder-owned self-supply from inventory, storage or drops.
- Excavation does not require storage, unload inventory or assign a hauling job. The accepted hauling scheduler and construction reservation/conservation rules remain unchanged.
- This supersedes the 12 October excavation auto-pickup rule without changing stack limits, recipes or wider World View scope.

## 2026-10-14 — Keep designation preview and first Colony art binding presentation-only

- Excavation and construction registries expose non-mutating cell evaluation used by both preview and commit, preventing duplicated terrain/occupancy rules.
- The command controller owns only a bounded transient preview and consumes compatible-tool right-click before normal context orders.
- Existing cancellation APIs remain responsible for ownership, reservation and staged-material recovery.
- Only `Colony_R02_C01` (Soil/Earth) and `Colony_R04_C01` (Stone) are allow-listed; Background variants are darker visual tints, all mapped Tiles are collider-free, and no seamless/autotile acceptance is implied.
- Composite cavities, decorations, Prepared/functional Background art and World View runtime remain deferred.

This log summarises approved decisions. It does not approve unresolved details or replace the GDD.

## Approved decisions

| Date | Decision | Consequence |
| --- | --- | --- |
| 12 Oct 2026 | Begin with a working founding queen | Older ordinary-ant-to-queen start plans are superseded |
| 12 Oct 2026 | Use autonomous, temporary-order, and future Draft control states | WASD movement remains a developer-only regression tool |
| 12 Oct 2026 | Share navigation and action mechanics between player orders and AI | Command systems call existing services instead of creating parallel movement |
| 12 Oct 2026 | Natural Earth and Stone are intrinsically grippable | Preparation improves speed rather than unlocking climbing |
| 12 Oct 2026 | Preserve permanent Background plus optional foreground Solid | Excavation removes only eligible foreground material |
| 12 Oct 2026 | Make functional colony tiles resource-backed Background conversions | Function, locomotion, and foreground occupancy stay separate |
| 12 Oct 2026 | Use one capacity unit per functional Background tile | Nursery, storage, and sleeping capacity remain spatial and explicit |
| 12 Oct 2026 | Require a door at every opening of an enclosed room | Room quality cannot assume open gaps are sealed |
| 12 Oct 2026 | Allow mixed-use rooms | Specialisation may improve quality but does not invalidate base function |
| 12 Oct 2026 | Model resources and reservations physically | Avoid unexplained global counters and duplicate consumption |
| 12 Oct 2026 | Make the builder self-supply construction | Superseded by WV-B2B-ARCH-001 on 15 Oct 2026 |
| 12 Oct 2026 | Use per-ant work priorities 1–4 plus Disabled | Priority precedes path-cost choice; skills affect efficiency, not rank |
| 12 Oct 2026 | Preserve valid partial work across safe interruptions | Critical needs can interrupt without erasing completed progress |
| 12 Oct 2026 | Require explicit brood authorisation | Spare food or nursery capacity never triggers reproduction automatically |
| 12 Oct 2026 | Start the first brood at three larvae and three Nursery cells | Early colony growth has a concrete spatial dependency |
| 13 Oct 2026 | Store identity, skills, and needs on authoritative ant records | Visual replacement and selection changes do not transfer or erase ant state |
| 13 Oct 2026 | Keep 3.2B interactions narrow and location-owned | No generic inventory, global stockpile, or autonomous job authority is implied |
| 13 Oct 2026 | Implement 3.3A work as synthetic registry-owned jobs | Validate priority, path-cost, ownership and interruption seams without implying real excavation, construction or hauling |
| 13 Oct 2026 | Commit 3.3B excavation output and foreground removal as one boundary | A resource-bearing Solid remains intact on output failure, and stable job keys prevent duplicate loose output |
| 13 Oct 2026 | Keep 3.3C inventory on `AntRecord` with Hauling-based natural slots | Visual rebinding cannot lose cargo; capacity decay retains overflow and blocks additions instead of deleting resources |
| 13 Oct 2026 | Use a provisional Soil/Stone stack maximum of 10 for 3.3C | Stacking and partial transfer can be tested without claiming final material balance |
| 13 Oct 2026 | Keep 3.3D storage physical and cell-owned | Each Material Storage Background tile owns one filtered stack and reservation rather than contributing to a global wallet |
| 13 Oct 2026 | Award 5 provisional Hauling XP per committed stored unit | Travel, reservation, pickup and cancellation cannot farm XP; Rested and hydration rules still apply |
| 13 Oct 2026 | Reorganise development IMGUI into one mutually exclusive top toolbar | Existing controls remain in place without overlapping side panels or a UI Toolkit migration; one shared hit test blocks world and camera mouse input over UI |
| 13 Oct 2026 | Implement 3.4A construction as builder-owned self-supply over existing authorities | Construction uses the shared job registry/navigation and physical inventory, storage and drop ownership; staged material remains cell-owned and no crafting wallet or second scheduler is introduced |
| 13 Oct 2026 | Automatically recover task inventory capacity without discarding resources | Retain task material, prefer physically reached reserved storage, fall back to a validated recoverable drop, then resume the interrupted executor through a reusable operation |
| 13 Oct 2026 | Treat loose-resource clutter as a future soft penalty | Loose drops remain traversable and never hard-block progression; future movement and enclosed-room quality penalties have provisional thresholds and values |
| 14 Oct 2026 | Use one scene-owned simulation clock with paused, 1×, 2× and 3× states | Gameplay time, AI deadlines and navigation share one authority; physics uses time scale while fixed-step settings remain constant and are restored |
| 14 Oct 2026 | Keep normal gameplay policy separate from pause | Play enables survival, colony work and hauling together; pausing preserves orders, reservations, progress and materials instead of toggling subsystems |
| 14 Oct 2026 | Accept direct orders and planning edits while paused | Current single-order replacement/cancellation semantics apply immediately, but movement, work, transfers and timers wait for resume |
| 14 Oct 2026 | Use interconnected Colony View and World View perspectives | Underground colony management remains side-on; future surface exploration and foraging use a top-down view |
| 14 Oct 2026 | Preserve one ant identity and simulation across both views | Stats, skills, needs, inventory and jobs remain shared; changing view or region cannot duplicate an ant |
| 14 Oct 2026 | Connect regions through explicit nest entrances while both continue simulating | Cross-view travel must compose local routes and a controlled transition; switching presentation does not pause the hidden region |
| 14 Oct 2026 | Introduce World View from one small playable surface and expand chunks incrementally | Large continuous exploration remains the target, while unloaded chunks eventually use bounded lightweight simulation rather than full physics |
| 14 Oct 2026 | Separate the authored player uGUI HUD from retained developer IMGUI | Global roster, ant, resource and clock presentation reads existing authorities; Colony View commands remain contextual, while developer tools start hidden and open independently |
| 14 Oct 2026 | Route player uGUI hit testing into the existing world-input boundary | One EventSystem and GraphicRaycaster block selection, designation and camera mouse leakage without making gameplay commands depend on uGUI classes |
| 14 Oct 2026 | Present only work categories with real autonomous executors in the player matrix | Excavation, Construction and Hauling are editable; metadata-only and future categories remain developer-only until implemented |
| 14 Oct 2026 | Edit the record-owned work matrix without interrupting active work | Player and developer interfaces share one priority authority; changes influence subsequent selection and do not alter skills, clock state or overrides |
| 14 Oct 2026 | Preserve custom art sources and gate runtime binding on visual and semantic review | Prepared 32-pixel candidates stay separate by view; no composite Colony tile or unverified edge becomes gameplay presentation by assumption |
| 14 Oct 2026 | Separate extraction from pickup and hauling | Excavation always commits loose output at the excavated cell; inventory transfer occurs only through later physical collection |
| 14 Oct 2026 | Use typed region locations and one capacity-one entrance transaction | Colony and Surface coordinates cannot mix; transition commit is idempotent and rollback preserves the source ant |
| 14 Oct 2026 | Keep manual view switching separate from ant travel and simulation | Cameras remember independent poses while both loaded regions continue on one clock |
| 14 Oct 2026 | Bootstrap survival from finite natural sources and safe natural rest | Starting storage/sleeping construction improves logistics but is not required to remain alive |
| 14 Oct 2026 | Represent extracted water as one recoverable liquid unit, not a solid material drop | Source, cache, inventory and Water Storage ownership must conserve exactly one unit through interruption |

## Open decisions

- Exact ordered-work productivity bonus and eligible actions.
- Job categories, interrupt thresholds, and explicit-order precedence.
- Drafted-ant survival intervention.
- Final resource stack sizes, recipes, work durations, and carrying balance.
- Room enclosure algorithm, door behavior, and quality formulas.
- Brood stages, timing, feeding thresholds, mortality, and overflow policy.
- Queen-death colony outcome.
- Combat, equipment, enemy behavior, and defence detail.
- Save format and chunk streaming lifecycle. WV-B2A now specifies bounded
  procedural-generation rules; final numerical/configuration approvals remain.
- Final generated Colony dimensions/coordinate adapter, chamber geometry,
  terrain/source stable IDs, weights, clustering and PRNG/version details.
- Entrance animation/duration, retry timing and presentation; capacity one, physical approach and atomic rollback are fixed.
- Seed parcel stacking, water-cache naming/art, natural-rest rate and initial source discovery presentation.
- Cross-view autonomous route selection/cost policy beyond the specified local-plus-entrance composition.
- Unloaded-chunk simulation fidelity, cadence and reconciliation (WV-F); loaded-hidden regions use full shared-clock simulation.
- Final need rates, skill curve, decay, and interaction values.

## Adding an entry

Record only decisions explicitly approved by the user or already marked authoritative in the GDD. Include the date, the decision, and its implementation consequence. If a topic is unresolved, add it to **Open decisions** instead of silently choosing a default.
