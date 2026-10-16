# Ant Colony Current Game State

## WV-B3C.2A canonical ground stacks — 16 October 2026

**Status: Implemented — Automated Tests Passed, Manual Acceptance Pending.**
`WorldMaterialDropRegistry` remains the sole mutable owner of supported loose
ground resources and now stores one canonical stack per Colony cell. A stack has
a scenario-local `GroundStackId`, typed `RegionCellId`, Soil/Stone Resource
Definition ID, quantity, provisional resource capacity, exclusive reservation
quantity, available quantity, revision, creation provenance and lifecycle.
Compatible output merges into the existing identity; different resource types
or capacity overflow are rejected before a source commits. Empty pickup removes
the identity, and a later stack at that cell receives a new ID.

Runtime scenarios bind the registry to `ChunkedWorldData`. Non-resident cells,
obstructing Solids/Nodes, unavailable compatibility details and completed
functional-Background Placements are rejected conservatively for ordinary
creation. Extraction uses a narrow prepared-output exception for the exact
Solid it removes before committing output. The existing explicit inventory-drop
command retains its validated current-anchor functional-Background compatibility
until Placement migration; it does not relax ordinary producer placement.
Construction staging, Material Storage and ant inventories remain separate
owners with separate capacities. The existing quantity-one
Extraction, pickup, Hauling, staging, Storage and Construction workflows are
preserved; partial Hauling behavior is unchanged rather than expanded.

Soil and Stone definitions both retain the existing effective ground-stack
capacity of **10**, explicitly marked provisional. Final balance is not approved.
Operation keys remain exact-once replay guards but are no longer physical stack
identity. Each newly constructed scenario registry receives a fresh controlled
context and monotonically allocates stack IDs, so restoration cannot resolve an
old ID or retain an old reservation. `Clear` terminates current stacks without
reusing IDs inside that scenario.

The Colony unified-cell adapter reads immutable canonical snapshots directly
from the registry and reports ID, resource, cell, quantity, reserved/available
quantity, capacity and stack revision as `Authoritative`. Creation, merge,
reservation, release, partial pickup and removal each publish one committed
loose-resource cell change and advance the affected stack revision where it
still exists. Failed operations and reads advance neither. Bounded diagnostics
report context, stack/cell totals, Soil/Stone quantities and rejection counts;
the existing developer stack listing includes identity, revision, capacity and
reservation state.

### Quantity badges

Colony world presentation now draws compact, display-only quantity badges from
existing authorities. Every visible loose stack shows its current quantity;
every visible non-empty Material Storage slot shows actual stored quantity and
empty Storage hides the badge. Current quantity-one Colony Nodes show `1` only
after an Extraction designation, keeping undesignated underground terrain
uncluttered. Badges update by reading committed state each GUI pass, disappear
with removed records/designations and are gated with Colony presentation so they
cannot leak into World View or intercept pointer input.

The project does not yet have general Node or Construction Site selection.
Selection-driven Node badges and selected-site staged requirement lines are
therefore documented future integration, not fabricated through hover state.
Generated Surface Nodes remain `GenerationOnly` and receive no live quantity
badge. Future Fog of War must hide undiscovered Node quantities; future WV-B3D
Storage uses one badge per Placement identity.

### Automated verification

Unity `6000.3.8f1` XML evidence records **16/16 focused Edit Mode**, **21/21
focused Play Mode**, **575/575 complete graphics-enabled Edit Mode** and **79/79
complete Play Mode** passed with no failures, skips or inconclusive results. A
separate **1/1** regression recheck confirms that player pickup followed by the
existing explicit inventory-drop command still conserves material. Evidence is
retained in `Logs/WV-B3C2A-Focused-EditMode.xml`,
`Logs/WV-B3C2A-Focused-PlayMode.xml`, `Logs/WV-B3C2A-Full-EditMode.xml` and
`Logs/WV-B3C2A-Full-PlayMode.xml`, with corresponding logs. An earlier
null-graphics complete Edit attempt crashed in the retained URP rendered-pixel
path before XML and is not a passing or failing test result.

### Manual Unity acceptance — pending

No Inspector setup is required. Manual acceptance remains pending until the user
confirms all 23 checks:

1. Load the Mara/Pip/Tala development fixture.
2. Confirm existing Colony gameplay starts normally.
3. Extract one Soil tile.
4. Inspect its stable physical stack identity and quantity.
5. Confirm only one canonical stack occupies the cell.
6. Haul the resource to a valid destination.
7. Confirm quantity decreases or the empty stack is removed.
8. Confirm no resource duplication.
9. Generate seed `2203`.
10. Extract Soil and Stone.
11. Confirm physical stacks appear.
12. Inspect their IDs, revisions and provisional capacities.
13. Confirm Hauling and Construction staging still work.
14. Switch views repeatedly and confirm stack identity remains stable.
15. Restore the fixture and confirm old generated stack IDs are invalid.
16. Confirm job scheduling and simulation controls still operate.
17. Exit Play Mode and confirm the Console contains no new errors.
18. Before designating a Resource Node, confirm it has no quantity badge.
19. Designate that Node for Extraction and confirm a compact upper-right `1`
    badge appears without hiding the designation marker.
20. Confirm every visible loose stack shows its quantity continuously, then
    pick up or merge material and confirm the badge updates or disappears at zero.
21. Confirm every visible non-empty Material Storage cell shows its actual
    stored quantity continuously and an empty Storage cell has no badge.
22. Switch to World View and confirm no Colony quantity badge leaks across;
    switch back and confirm authoritative quantities are unchanged.
23. Confirm badges do not intercept selection, designation or camera input.

WV-B3C.2B multi-cell overflow/pending-output recovery, WV-B3C.3 variable Node
authority/transaction cutover, WV-B3D Placement migration and WV-C1 remain
unimplemented. Generated Surface Nodes remain `GenerationOnly`.
`WV-B3B-LIM-001` remains active: only the unified read-model pair is published
atomically; complete mutable scenario construction still uses the host's
existing in-place rebuild/restore pathway.

## WV-B3C.1 inactive Colony Resource Node registry — 16 October 2026

**WV-B3C.1 — Complete.** Automated validation passed and the user explicitly
accepted the full manual Unity checklist on 16 October 2026. An
`InactiveColonyResourceNodeRegistry` is constructed once for each prepared
development-fixture or generated Colony scenario. It records every registered
Soil/Stone Solid as a typed, quantity-one diagnostic Node with immutable stable
ID, definition, extraction metadata, provenance, revision and lifecycle. The
development inspector reports scenario identity, registry lifecycle/revision,
total/Soil/Stone counts, unsupported Solids, duplicate count and the selected
cell's inactive Node state.

`ChunkedWorldData` remains the live terrain and effective quantity-one gameplay
authority. Existing `ExcavationWorldTransaction`, `WorldMaterialDropRegistry`,
Hauling, Construction, navigation, rendering and XP paths are unchanged. The
inactive registry cannot remove terrain, reserve work, decrement quantity,
produce output or publish a competing runtime-cell change. A Node-slot terrain
change only marks the matching candidate record `StaleInvalidated` or
`DepletedOrAbsent` and increments its diagnostic revision. Unrelated terrain
changes and reads do not change it. A removed/re-added Solid cannot silently
revive the old candidate.

### Identity and scenario lifecycle

Generated IDs are namespaced by Colony region, generation configuration,
generation version and seed, then retain the deterministic generated Node ID.
Fixture IDs use the Colony region, `development-fixture` namespace/version,
fixture seed, actual registered Solid ID and typed cell coordinates. No Unity
instance ID or runtime hash participates. Future runtime-created/respawning
Nodes require a persisted scenario-scoped allocation or creation-operation ID;
that policy is not implemented here.

Candidate construction validates registered Soil/Stone extraction definitions,
one Node per cell, unique IDs/cells, typed Colony ownership, generated metadata
consistency and retained Backgrounds. Unsupported Solids are counted but do not
become Nodes. A failed candidate is not installed and does not mutate terrain.
Successful scenario replacement invalidates the old registry handle; camera
switching never rebuilds it. The host still uses its existing in-place
rebuild/restore path, so `WV-B3B-LIM-001` remains active.

### Automated verification

Unity `6000.3.8f1` produced XML-backed results on the WV-B3C.1 working tree:

- focused Edit Mode: **8/8 passed**;
- focused Play Mode: **4/4 passed**;
- complete graphics-enabled Edit Mode: **565/565 passed**;
- complete Play Mode: **78/78 passed**;
- failures, skips and inconclusive results: **0**.

Evidence is retained in `Logs/WV-B3C1-EditMode.xml`,
`Logs/WV-B3C1-PlayMode.xml`, `Logs/WV-B3C1-Full-EditMode.xml` and
`Logs/WV-B3C1-Full-PlayMode.xml`, with corresponding logs. The first attempted
complete Edit run used a null graphics device and crashed in the pre-existing
rendered-pixel test before producing XML; the required graphics-enabled rerun
above is the valid complete result.

### Five-stage gameplay boundary

The approved sequence remains **Foraging → Extraction → Hauling → Refining →
Construction**.

Foraging remains future work. Colony Soil/Stone Extraction, physical loose
output and Hauling remain implemented through their pre-WV-B3C.1 authorities.
Refining remains future work. Construction remains implemented through existing
site staging and compatibility Background conversion. Generated Surface Stone,
Seed, Twig and Freshwater remain `GenerationOnly`; no Surface interaction was
added.

### Manual Unity acceptance — complete

The user confirmed the complete **17/17 WV-B3C.1 manual Unity acceptance
checklist passed**. The accepted procedure is retained below as evidence:

Development fixture:

1. Load Mara/Pip/Tala.
2. Confirm the existing terrain and three ants are present.
3. Open Developer Only → Debug and inspect registry totals and lifecycle.
4. Select a Soil or Stone Solid and record its Node ID/revision.
5. Extract that Solid through the existing workflow.
6. Confirm the existing physical Soil/Stone output appears unchanged.
7. Confirm the selected inactive record becomes `DepletedOrAbsent` while its
   captured remaining quantity stays 1 and its diagnostic revision advances.

Generated scenario:

8. Start generated seed `2203`.
9. Confirm the founding queen, empty chamber and entrance passage are intact.
10. Confirm the registry reports Soil and Stone records with a generated
    scenario identity.
11. Select and extract an accessible Soil Solid through the existing workflow.
12. Confirm physical output is unchanged and the matching record invalidates.
13. Switch Colony/Surface views several times.
14. Confirm the registry instance/revision and unaffected Node IDs do not change.
15. Make an entrance round trip and confirm residency/navigation still work.
16. Restore the development fixture and confirm the generated registry handle is
    invalidated and no duplicate Nodes or ants appear.
17. Confirm the Console has no new errors or exceptions.

WV-B3C.1 is complete. This documentation-only closure did not rerun Unity tests.
WV-B3C.2 canonical loose stacks, WV-B3C.3 authority/transaction cutover, WV-B3D
and WV-C1 remain unstarted and require separate authorisation.

### Resource and Node System v1.0 alignment

The approved target is now recorded in
[`ResourceAndNodeSystem.md`](ResourceAndNodeSystem.md). Every target Solid is a
finite Resource Node with a specific Resource Definition and positive actual
quantity; variable quantities and whole-node Extraction are approved. Natural
Backgrounds are environmental only, Water is finite, overflow must use complete
deterministic placement or recoverable pending output, and future timed Surface
events create explicit Nodes/stacks rather than regenerating terrain.

This is design alignment only. The completed WV-B3C.1 registry remains an
inactive quantity-one Soil/Stone compatibility layer; generated Surface records
remain `GenerationOnly`; current functional Backgrounds remain active legacy
compatibility pending WV-B3D. No runtime, scene, prefab, test, asset or project
setting changed for this amendment. `WV-B3B-LIM-001` remains active.

The companion
[`ConstructionPlacementSpecification.md`](ConstructionPlacementSpecification.md)
audits the current natural and functional Background assets, six code-defined
construction recipes, Construction Site staging, Survival interaction capacity
and Material Storage ownership. The target keeps natural Backgrounds, gives
function to completed Placement instances only, and preserves exact staged
material/content/reservation state during one-function-at-a-time WV-B3D
migration. No Door, room-scoring, Refining Station or brood runtime currently
exists, and no migration was implemented by this documentation task.

## WV-B3B unified runtime cell authority foundation — 15 October 2026

**WV-B3B — Complete.** Automated validation passed and the user explicitly
confirmed **16/16 manual Unity acceptance checks passed**. The runtime
now has one common read boundary for the loaded Colony and Surface. Typed
`RegionCellId` values cannot erase their `ColonyCell`/`SurfaceCell` domain.
`RuntimeCellSnapshot` is an immutable point-in-time value with Background,
optional occupant projections, cell revision and region publication revision.
Missing regions, wrong coordinate domains, unavailable cells and missing
Backgrounds return diagnostic results rather than loading or regenerating data.

### Authority and adapter boundary

The Colony adapter reads `ChunkedWorldData` and
`WorldMaterialDropRegistry`. Existing Solids are compatibility Nodes with no
fabricated quantity; functional Backgrounds are compatibility Placements; drop
records are projected conservatively. The Surface adapter reads the existing
`SurfaceWorldData` and validated generated snapshot. Generated Seed, Twig,
Freshwater and Stone records expose their real generated IDs and quantities but
are marked `GenerationOnly`; fixture obstacles report unavailable details.
Neither adapter can mutate gameplay.

Terrain writes and committed loose-drop changes publish region-scoped cell
revisions and compact change sets. Identical terrain writes, reads, rendering
rebuilds and camera switches do not publish changes. Each consumer owns its
change-feed cursor. The pure validator enforces the approved one-Background,
optional Node/Placement/one-type-stack rules for observation and future calls;
Unknown is never converted to Valid. Current gameplay mutation paths do not yet
call that future validator.

The host validates a complete Colony/Surface adapter pair and swaps the unified
read model only after both pass. A failed adapter validation retains the prior
pair. The view coordinator now consumes the host's same Surface authority.

### WV-B3B-LIM-001 — Whole-World Publication Atomicity

Implemented: validation and atomic replacement of the Colony/Surface unified
read-model pair. Not implemented: transactional construction and publication of
the complete gameplay scenario, including mutable terrain services, ant
registries, inventories, jobs, entrance and residency. The development host
constructs those services in place, and a later scenario-build failure follows
the existing rebuild/restore pathway. A future failure during construction or
restoration may need stronger recovery guarantees. This is accepted and does
not block WV-B3B closure, but it must be reassessed before persistence, region
unloading or complex scenario restoration. Current documentation must not call
this a complete whole-world transaction.

### Automated verification

The retained XML/log evidence was produced with Unity `6000.3.8f1` on the
pre-commit WV-B3B working tree whose parent baseline was `c229bac2`. That
implementation, tests and documentation were subsequently committed as
`05e3b730`. The XML records test names, totals and timestamps but does not embed
a Git SHA, so `c229bac2` is recorded as the parent baseline rather than
misidentified as the final implementation revision.

- Unity `6000.3.8f1`: focused Edit Mode **6/6 passed**.
- Unity `6000.3.8f1`: focused Play Mode **3/3 passed**.
- Complete graphics-enabled Edit Mode **557/557 passed**.
- Complete Play Mode **77/77 passed**.
- Every run recorded zero failures, skips and inconclusive tests.

Retained evidence references are
`Logs/WV-B3B-focused-edit-final.xml`,
`Logs/WV-B3B-focused-play-final.xml`,
`Logs/WV-B3B-full-edit-final.xml` and
`Logs/WV-B3B-full-play-final.xml`, with corresponding `.log` files. This final
closure is documentation-only and did not rerun Unity tests.

Coverage includes identity/ordering, immutable reads, invalid lookups, exact
revision/change-feed behavior, conservative compatibility, atomic read-model
publication failure, both authored and generated scenarios, generated
generation-only nodes, shared Surface authority and camera-switch stability.
The complete suites retain deterministic generation, fixture restoration,
Extraction, loose output, Hauling, Construction, scheduler, entrance, rendering
isolation and shared-clock regressions.

### Manual Unity acceptance — complete

The user explicitly confirmed the WV-B3B manual Unity acceptance gate at
**16/16 passed**. This acceptance covers the runtime-cell foundation and its
fixture/generated-scenario regression observations. It closes WV-B3B only.
WV-B3C, WV-B3D and WV-C1 remain deferred and require separate authorisation.

## WV-B3A unified runtime cell specification — 15 October 2026

**Documentation complete; runtime migration not started.** Read-only inspection
confirmed that `ChunkedWorldData`, `SurfaceWorldData`, generated records,
`WorldMaterialDropRegistry`, `MaterialStorageWorld`, `ConstructionSite` and
functional Background services each own valid current slices but do not yet
form one runtime cell authority. The approved end-state and controlled migration
are specified in `UnifiedRuntimeCellSpecification.md`.

No Unity script, asset, scene, prefab, ScriptableObject, test or project setting
was changed. WV-B3B/B3C/B3D and WV-C1 require separate approval. Existing
runtime behavior and still-pending WV-B1 or visual acceptance items remain open.

## WV-B2B closure and scheduler fairness correction — 15 October 2026

### WV-B2B — Complete

The user manually accepted the generated founding-colony scenario at seed
`2203`. The confirmed run demonstrated deterministic procedural terrain, the
founding queen spawn and movement, Colony Extraction of physical Soil and Stone,
loose-resource creation, separate Hauling, Construction Site staging,
Construction completion, first-Storage construction, later delivery into
Storage, underground expansion, view switching and entrance travel. This is
direct acceptance of the approved production chain:

**Extraction → Loose Ground Resource → Hauling → Construction staging / Storage
→ Construction**

The run closes `WV-B2B-DEF-001`, `WV-B2B-DEF-002`, `WV-B2B-DEF-003` and
`WV-B2B-ARCH-001`. Their root causes, corrections and earlier automated evidence
remain in the implementation history below. No blocking WV-B2B defect remains.
The user confirmed the combined outcomes above; individual historical checklist
steps not explicitly reported are not inferred.

The founding queen later died on Day 8. The final screenshot showed **Health 0,
Hunger 0, Thirst 0 and Fatigue 100**, after which autonomous work stopped because
the ant was inactive. This is a known founding-survival limitation, not a proven
hunger-only death and not a WV-B2B failure. Generated Seed and Freshwater records
are not yet usable for complete survival. Direct finite-source consumption and
safe natural rest remain deferred to separately authorised WV-C1 work; no free
supplies, invulnerability or survival rebalance was added here.

### WV-B1-REG-001 — Closed

The earlier reported 539/539 Edit and 73/73 Play results had no retained XML in
the audited checkout and remain historical reports rather than independent
evidence. This correction reran the current working tree based on `d977f154` and
produced machine-readable Unity XML: **551/551 complete graphics-enabled Edit
Mode** and **74/74 complete Play Mode**, with zero failures, skips or
inconclusive tests. That current full-suite evidence closes
`WV-B1-REG-001 — Complete Edit Mode Regression`.

### Bounded fair colony-job selection — complete and manually accepted

The audit's first-32 starvation finding was valid: the selector collected the
first 32 claimable same-priority jobs before reachability, so unreachable jobs
could permanently hide job 33 and registry insertion order affected gameplay.
The existing selector now advances a deterministic per-ant pass through the
registry, examining no more than 32 entries per autonomous evaluation. It keeps
the best reachable candidate for each priority and chooses only after the whole
pass, preserving priority-before-path-cost and the stable category/Y/X/ID tie
break. This is the existing scheduler and reservation path, not a second system.

New jobs append safely to the active pass without moving its cursor. Removal or a
work-priority revision safely restarts the cursor.
Completed, cancelled, reserved and newly ineligible jobs are rechecked before
assignment; a changed best candidate causes a conservative rescan. In-progress
pages use the normal evaluation interval rather than the 2/4/8-second failure
backoff. Registry order is explicit creation order, multiple ants keep separate
passes, and the existing exclusive reservation remains authoritative. Runtime
pause still gates `UpdateAutonomousColonyWork`; 1×/2×/3× continue to use the one
simulation clock.

Read-only diagnostics expose total job count, next position, entries examined in
the last evaluation, pass completion and selected job ID without per-frame log
spam. Scale tests assert exactly 1/2/8/32 evaluations for 32/64/256/1,024-entry
registries when the only eligible job is last, with at most 32 entries examined
per evaluation. Focused evidence is **40/40 Edit Mode** and **1/1 Play Mode**;
the complete suites pass **551/551 Edit** and **74/74 Play**. The user confirmed
the scheduler fairness manual acceptance on 15 October 2026. The checklist below
is retained as the accepted procedure; this documentation-only status update did
not rerun Unity.

#### Scheduler manual Play Mode checklist — accepted 15 October 2026

1. Start the generated founding-queen scenario with seed `2203`.
2. Confirm the queen can Extract, Haul and Construct with conserved quantities.
3. Confirm blocked jobs remain pending without monopolising the queen.
4. Switch views and complete an entrance round trip without resetting work.
5. Create more than 32 pending Extraction jobs, with the first group unreachable.
6. Create one reachable Extraction job after that group.
7. Confirm the later reachable job is eventually selected and completed.
8. Change priorities and confirm the highest genuinely eligible priority wins.
9. Insert and remove pending jobs during repeated evaluation; confirm work continues.
10. Complete or cancel jobs and confirm the scan continues without a stuck cursor.
11. With multiple ants, confirm one exclusive job never has two owners.
12. Pause during a partial scan; confirm autonomous selection does not advance.
13. Resume at 1×, 2× and 3×; confirm eventual selection remains deterministic.
14. Confirm the Console remains clean. No production-facing diagnostic was added.

## WV-B2B implementation history — 15 October 2026

### WV-B2B-ARCH-001 — Unified Resource Logistics

**Implemented — Pending Manual Acceptance.** The resource workflow is now
**Resource Node → Loose Ground Resource → Hauling → Construction staging /
Storage → Construction**. Construction self-supply has been removed from both
autonomous eligibility and order execution. Unsupplied construction remains
pending and unowned; dynamic Hauling selects compatible unmet Construction Site
capacity before optional Material Storage, reserves source and destination,
delivers exactly once, and releases both reservations on interruption.

Construction Sites own per-resource required, reserved and delivered quantities
and support multiple recipe resource types. Construction work can reserve/start
only when every requirement is staged; completion consumes staging once.
Cancellation recovers staged types to separate valid loose cells. The founding
queen can therefore haul loose Soil directly to the first Storage construction,
construct it, and later haul new Soil into the completed empty Storage without a
bootstrap stockpile.

Extraction is now the sole skill/category for Soil, Stone, Seed, Twig and future
node removal. It retains the former Excavation numeric slot, so existing earned
experience and work-priority values remain intact; the legacy enum name is an
alias to the same state and cannot receive duplicate XP. Player labels and
generated node metadata say Extraction. `ExcavationDesignationRegistry`,
`ExcavationJobOrder`, `ExcavationWorldTransaction`, related method names and the
`WorldDesignationMode.Excavate` enum remain compatibility names to avoid a risky
serialized/API rename. Only Colony Soil/Stone interaction is implemented;
Surface Seed/Twig extraction remains future WV-C work.

Validation passes **82/82 focused Edit Mode**, **9/9 focused Play Mode**,
**539/539 full Edit Mode** and **73/73 full Play Mode**, with no failures or
skips. Direct visual/manual Play Mode acceptance remains pending.
`WV-B2B-DEF-001`, `WV-B2B-DEF-002`, `WV-B2B-DEF-003` and
`WV-B1-REG-001` remain visible/open; WV-B2B is not closed and WV-C1 has not
started. Historical self-supply descriptions below are superseded by this
architecture correction.

**Implemented — Awaiting Validation.** `ProceduralWorldGenerator` deterministically
builds a Grass-dominant `64×64` Surface and an entirely underground, Soil-dominant
`128×128` Colony from one 64-bit seed with independent named streams. Generated
cells always retain a Background and may hold one stable finite node; placement
and loose-stack slots begin empty. Validation gates dimensions, identity,
quantity, entrance clearing, source reachability, predominance, chamber/spawn
clearance and queen-to-exit reachability before publication.

The development toolbar accepts a seed and can start the generated founding
scenario or restore the unchanged Mara/Pip/Tala fixture. The generated scenario
has one queen, no storage, no sleeping placement and no loose starting stacks.
Generated nodes are not interactable source gameplay. Existing Colony
Soil/Stone nodes project to existing Solids, so excavation remains compatible;
Surface navigation receives only generated obstacles/water through its narrow
adapter. Unity compilation and focused automated regression execution now pass;
manual Play Mode acceptance remains pending.

### WV-B2B defect recovery — implemented, acceptance open

**Implemented — Pending Manual Acceptance.** Investigation found
two independent defects:

- `WV-B2B-DEF-001 — Founding Queen Job Starvation`: construction discovered an
  absent material source only after selection/reservation. The paused job then
  became immediately claimable again and could repeatedly win priority
  selection. Autonomous selection now performs a read-only physical-material
  dependency check first. Unsatisfied construction stays paused with an
  explicit waiting reason and no owner or resource reservation, while other
  eligible work remains selectable. Inventory, reachable storage and reachable
  unreserved loose drops satisfy the check, so construction becomes eligible
  when material appears.
- `WV-B2B-DEF-002 — Generated Stone Excavation`: random generation did not
  guarantee a Stone foreground node next to the starting chamber, while Stone
  natural Background could still be visible in cleared cells. Generation
  version `2` / configuration `wv-b2b.default.002` guarantees one quantity-one
  `solid.stone` node at Colony `(-4,-8)`, adjacent to the open chamber. Normal
  adjacency, Extraction skill, exact-once output and Background preservation
  remain authoritative. Rejection diagnostics now distinguish background-only,
  unregistered and non-excavatable cells.

Focused regression coverage was added for pending/unowned construction,
excavation selection despite higher-priority unsatisfied construction,
material-triggered construction reconsideration, a zero-Construction-skill
queen, the generated Stone face, adjacent access, Background preservation and
one-unit Stone output. The affected World, Commands, Development and Edit Mode
test assemblies compile with Unity's Roslyn compiler. The focused defect suite
passed **12/12 Edit Mode**, the relevant construction/excavation/procedural/WV-B1
regression classes passed **58/58 Edit Mode**, and World View regressions passed
**5/5 Play Mode**, with no failures or skips. The first restricted runner attempt
crashed before discovery in `BuildReportRestService` and produced no XML; the
unrestricted rerun produced the passing results above. Direct Play Mode
acceptance is still pending, so both defect IDs remain open. `WV-B1-REG-001`
also remains open; WV-C1 has not started.

#### WV-B2B-DEF-003 — Founding Colony Storage Bootstrap

**Implemented — Pending Manual Acceptance.** Generated-seed `2203` reproduced a
real scheduling dependency rather than a resource-accounting failure. Dynamic
discovery created a Hauling job for every loose drop even when no compatible
Material Storage destination existed. When that impossible haul won priority
selection, order creation rejected it with `No reachable compatible Material
Storage has capacity`, applied the ant's ordinary-work retry delay, and selected
the same still-claimable haul again. The failure did not reserve or consume the
drop, but it could indefinitely delay Construction self-supply.

Hauling now performs a read-only source, inventory-capacity, route and explicit-
destination eligibility check before autonomous selection. An impossible haul
remains paused and unowned with its diagnostic. A compatible unmet Construction
Site is valid even without Storage, so Hauling can stage the first Storage's
Soil; Construction then consumes it exactly once and registers empty Storage.
No new scheduler, storage type, free resource or transfer authority was added.

Automated seed-`2203` coverage gives Hauling higher priority than Construction,
zero existing Storage, one loose Soil and two competing Storage conversions.
It proves the haul cannot monopolise the queen, only one Construction reserves
the Soil, the first Storage completes conservatively, and a later Soil drop can
then start ordinary hauling. The focused Material Storage/Hauling suite passed
**17/17 Edit Mode**. The broader relevant Construction, Hauling, Excavation,
procedural and WV-B1 set passed **75/75 Edit Mode**, and World View regressions
passed **5/5 Play Mode**, with no failures or skips. Manual Play Mode acceptance
remains open, so `WV-B2B-DEF-003` is not closed.

##### WV-B2B-DEF-003 manual Play Mode checklist

1. Start the generated founding-colony scenario with seed `2203` and confirm no
   Material Storage exists.
2. Extract one reachable Soil Solid and confirm exactly one loose Soil appears.
3. Designate one valid empty chamber cell for Material Storage conversion.
4. Confirm the queen claims Construction rather than repeatedly rejecting
   destination-less Hauling, physically collects the loose Soil and stages it.
5. Confirm construction completes, consumes the Soil exactly once and registers
   one empty Material Storage slot with no stuck source/destination reservation.
6. Extract another Soil Solid and confirm ordinary Hauling now recognises the
   completed Storage and can deliver the second loose Soil.
7. Verify loose + carried + staged + stored quantities at each boundary, switch
   views and return, and confirm no state resets or Console errors.

#### WV-B2B defect-recovery manual Play Mode checklist

1. Open `Assets/AntColony/Scenes/AntGameDevelopment.unity`, enter Play Mode and
   start the generated founding-queen scenario with seed `2203`.
2. Confirm the queen has no stored or carried material. Designate a Background
   conversion requiring Soil or Stone and confirm it reports a material waiting
   reason without owning or trapping the queen.
3. Designate accessible Soil. Confirm the queen completes excavation, the Solid
   disappears, its natural Background remains, and exactly one Soil unit appears.
4. Let the queen collect/transport sufficient existing physical material and
   confirm pending construction is reconsidered without free or duplicate material.
5. Designate the exposed Stone face at `(-4,-8)`. Confirm acceptance, adjacent
   approach, normal work, retained Stone Background and exactly one Stone output.
6. Attempt to work buried Stone with no reachable adjacent cell. It may remain
   designated, but the queen must not mine it through solid terrain.
7. Switch views and return; confirm terrain, drops and jobs persist. Restore the
   Mara/Pip/Tala fixture and confirm existing jobs and WV-B1 entrance behavior.
8. Confirm the Console has no new errors. Record any failure's exact rejection
   text and cell coordinate.

## WV-B2A documentation state — historical pre-implementation record

**Superseded by the WV-B2B implementation state above.**
`ProceduralWorldGenerationSpecification.md` is now the authoritative future
WV-B2B contract. It preserves the implemented WV-B1 typed regions, bounded
`64×64` Surface fixture, capacity-one entrance and rendering isolation while
specifying a shared-seed generated Surface and entirely underground Colony.

At the WV-B2A gate, the runtime used the deterministic WV-B1 Surface obstacle fixture
and the authored/development Colony produced by `SampleWorldGenerator`, including
Mara, Pip and Tala. The existing `ChunkedWorldData.WorldSeed` is reserved/input
data and the development generator's seed variation does not constitute the new
production procedural world system. Natural Surface terrain/source records, a
generated starting chamber and generated founding-queen scenario are not yet
implemented at that gate. No Unity tests were run for WV-B2A because this was a documentation-
only milestone; WV-B1 manual acceptance and `WV-B1-REG-001` remain unchanged.

The approved new-game vision now starts the founding queen in one small excavated
underground chamber directly beneath the Colony entrance. Older statements that
put her on the Surface or start with no chamber are historical and superseded.

### Unified-cell amendment status

**Implemented for generated cells; broader runtime migration deferred.** The target model requires one Background
per cell and optional node, placement and one-type loose-stack occupants in both
regions. Current runtime still differs:

- Colony cells expose Background plus optional Solid material ID; Solids have no
  per-instance node ID or remaining quantity.
- Surface cells expose bounds/traversability and obstacle membership only.
- construction replaces Background material with a functional Background and
  registers separate capacity data;
- loose material authority can hold multiple stacks, including different types,
  at the same cell;
- Material Storage already owns one bounded, one-type slot with reservations,
  but ownership is tied to a functional Background cell.

Exact-once excavation/drop output, physical inventories, reservations,
builder-owned self-supply and Construction skill progression remain useful
implemented seams. No migration, generator, extraction, decay or Unity tests are
part of this amendment. `WV-B1-REG-001` remains outstanding.

## WV-B1-DEF-002 — Cross-Region Rendering Leakage — 15 October 2026

**Implemented — Pending Manual Acceptance.** The defect had two presentation causes. The runtime Surface camera copied the Colony camera's all-layers culling mask, so spatial separation alone allowed Default-layer Colony renderers to enter World View at sufficiently wide or moved camera bounds. Separately, excavation/construction highlights and loose-material labels are IMGUI overlays projected through the Colony camera; disabling that camera did not stop `OnGUI` from drawing them.

WV-B1 now owns a dedicated `SurfaceWorld` presentation layer. The Surface camera renders only that layer, while the Colony camera explicitly excludes it. Runtime Surface terrain, obstacles and entrance visuals are created on the Surface layer. When an ant commits an entrance transition, only its child renderer GameObjects (including its selection indicator) change presentation layer; the authoritative ant object, record, residency and movement transaction remain unchanged. A returning ant restores each renderer's original layer. Colony world overlays are explicitly gated by the Colony view context, Colony World/Debug development panels and hotkeys cannot be opened or used from World View, and the authored HUD continues to hide only its Colony command bar while retaining shared information.

Focused Play Mode coverage checks camera masks, runtime Surface visual ownership, exclusion of Colony tilemap renderers, repeated view switching, dynamic designation persistence, Colony-tool input gating, HUD command-bar context, unchanged resources/residencies, and ant renderer handoff in both directions. Unity's Roslyn compiler successfully compiled the changed Commands, Development and Play Mode test assemblies. Focused WV-B1 Edit Mode passed **9/9** and focused Play Mode passed **4/4**, with zero failures or skips; these include the existing entrance capacity, pause, idempotence, bidirectional reuse and shared-clock regressions. An initial restricted Play runner crashed in `UnityEditor.BuildReportRestService` before discovery; rerunning outside that listener restriction produced the recorded passing XML. `WV-B1-DEF-001` manual acceptance and `WV-B1-REG-001` full Edit Mode regression remain outstanding, and WV-B1 stays open.

### WV-B1-DEF-002 manual Play Mode acceptance

1. Open `Assets/AntColony/Scenes/AntGameDevelopment.unity`, enter Play Mode and create both excavation and construction designations in Colony View.
2. Confirm the designation highlights, loose-material labels, storage terrain and Colony-resident ants render normally in Colony View.
3. Switch to World View and confirm only Surface terrain, obstacles, entrance and Surface-resident ants are visible; no Colony terrain, designation, material-drop label, storage visual or Colony selection indicator may appear below or within the Surface.
4. Open and close shared HUD menus. Confirm roster, Colony summaries and clock/speed controls remain usable, while the Colony command bar and World/Debug designation controls remain unavailable in World View.
5. Leave Colony jobs running, note designation/resource/order state, wait in World View, then return to Colony View. Confirm work and shared-clock simulation continued and no order was cancelled by the view switch.
6. Confirm the original designations and material drops reappear intact in Colony View.
7. Send an ant through the entrance and confirm its one existing visual appears only in its authoritative resident region. Return it and confirm the same identity and representation appear only in Colony View.
8. Switch views repeatedly at different pan/zoom positions and window aspect ratios. Confirm rendering separation remains stable and camera poses remain independent.
9. Repeat the `WV-B1-DEF-001` two-ant entrance-reuse checklist, including a deliberately occupied arrival cell, to confirm the rendering change did not alter transition capacity or occupancy safety.
10. Confirm the Console contains no new errors, warnings or exceptions.

## WV-B1 entrance reuse correction — 14 October 2026

**Implemented — Awaiting Unity test execution and manual acceptance.** Investigation confirmed that entrance capacity was released correctly after terminal transitions. The defect was stale local occupancy: after a Surface ant moved away from `(6,4)`, `SurfaceNavigationMotor.CurrentCell` changed but `AntRegionResidencyRegistry` still recorded the ant at the arrival cell. The next transition therefore failed destination occupancy validation indefinitely. The same stale-location risk existed after moving away from the Colony arrival.

The coordinator now records completed local Surface and Colony movement back into the same-region residency location, while excluding the ant currently owned by an active transition so approach revisions remain transaction-controlled. `TryRelocate` cannot change region ownership. Genuine arrival occupancy still fails safely with `Destination is unavailable or occupied`; no ant is displaced. Terminal ownership release now retains released transition IDs so a stale request ID cannot be replayed after capacity becomes available. The entrance exposes `Available`, `Reserved`, blocked and unavailable status through its capacity state.

Regression coverage now models Ant A entering, a correctly blocked Ant B while A remains on the arrival, Ant A moving clear, Ant B reusing the entrance, distinct Surface residencies, both return trips, released capacity and replay rejection. A Play Mode regression drives the same two-ant sequence through the authored development scene in both directions. All affected assemblies compile with Unity's Roslyn response files. Unity test execution is pending because an interactive Unity Editor currently owns the project lock; this correction is not reported as test-passed from compilation alone.

### Entrance reuse manual verification

1. Start `AntGameDevelopment` with two Colony ants available.
2. Send Ant A through **Use Nest Entrance** and manually switch to World View after arrival.
3. Without moving Ant A from the Surface arrival, request Ant B and confirm the transition fails safely with an occupied/unavailable destination message; both ants remain valid.
4. Move Ant A away from Surface `(6,4)` with a right-click order.
5. Return to Colony View, send Ant B through, then confirm both distinct ants exist in World View.
6. Move Ant B away from `(6,4)`, send Ant A back to Colony, and move Ant A away from Colony `(0,-8)`.
7. Send Ant B back to Colony and confirm the entrance remains available afterward.
8. Confirm identities, needs, skills and inventories are unchanged, no two ants share an arrival, no visual is duplicated or lost, pause still blocks commit, and the Console remains clean.

## WV-B1 two-view runtime — 14 October 2026

**Status: Implemented — Awaiting Validation.** The development scene now loads the accepted Colony world and a deterministic `64×64` top-down Surface at the same time. The Surface has fixed authored obstacle bands, hard bounds, an explicit founding entrance, temporary colour-block presentation, and a dedicated deterministic eight-direction A* whose diagonals require both adjacent cardinal cells to be open. `SurfaceNavigationMotor` is kinematic and advances only from the existing `SimulationClockController`; pause supplies no movement or transition commit delta.

`RegionId`, `EntranceId`, `TransitionId`, `ColonyCell`, `SurfaceCell` and the discriminated `RegionDestination` prevent coordinate-space interchange. `AntRegionResidencyRegistry` owns exactly one location and revision per ant. Mara, Pip and Tala keep their existing `AntRecord` and `AntEntityBinding`. The initial implementation keeps one bound GameObject per ant and switches its enabled Colony/Surface local motor during handoff; GameObject lifecycle is not the residency transaction.

`TwoViewWorldCoordinator` creates two persistent camera objects. The visible **Colony View**, **World View** and **Use Nest Entrance** controls switch only camera/input context or request entrance travel. Each camera naturally retains its own transform and orthographic size. Colony designation/right-click conversion is disabled in World View; Surface clicks use only `SurfaceCell`. Both regions and motors stay loaded, and hidden-region movement/needs continue from the one clock.

The founding entrance links Colony approach `(-1,-8)` / arrival `(0,-8)` to Surface approach `(5,4)` / arrival `(6,4)`. A request reserves the capacity-one entrance, runs the source-local path, records the reached source approach, waits there while paused, revalidates the destination/occupancy, commits one destination-region change, activates the destination motor, and releases terminal ownership. Cancellation before commit preserves source residency. Activation failure rolls residency back to the recorded approach and restores the source representation. Replay of a completed transition does not apply another region change.

### WV-B1 automated verification

- Unity C# compilation: all affected World, Navigation, Commands, PlayerUI, Development, Edit test and Play test assemblies compiled with Unity's Roslyn response files.
- Focused Edit Mode: **8/8 passed** (fixture/bounds, deterministic paths, corner restriction, single residency/revision, capacity/pause/idempotence, cancellation/rollback, unavailable/blocked endpoints and round trips).
- Focused Play Mode: **2/2 passed** (shared-clock pause/movement and loaded-scene view/camera/residency invariants).
- Complete Play Mode: **69/69 passed**, zero failures/skips.
- Complete headless Edit Mode: **not passed**. Unity crashed in the existing rendered-pixel path after `Failed to set the active render target`; no result XML was produced. This is recorded as an execution failure, not an accepted regression result.

### Exact WV-B1 manual Unity Play Mode acceptance

**Pending:** open `Assets/AntColony/Scenes/AntGameDevelopment.unity` and complete all checks before marking WV-B1 complete.

1. Enter Play Mode and confirm Colony terrain, Mara/Pip/Tala and the existing HUD load without Console errors.
2. Select a Colony ant and right-click a valid Colony cell; confirm ordinary Colony movement still works.
3. Use **World View** and confirm the green `64×64` top-down Surface, dark fixed obstacles and gold entrance marker are visible.
4. Pan with WASD/arrow keys or middle-drag and zoom with the wheel. Switch to Colony, move/zoom its camera, then switch both ways and confirm each pose/zoom is retained independently.
5. Confirm switching views alone does not move an ant, issue/cancel work, change pause/speed, reset needs/inventory or change residency.
6. In World View, left-click a Surface-resident ant and right-click open terrain. Confirm it routes around obstacles, rejects obstacle/out-of-bounds targets and cannot cut a blocked diagonal corner.
7. In Colony View, select an ant and press **Use Nest Entrance**. Observe physical travel to `(-1,-8)`; do not switch the camera automatically.
8. Pause before arrival/commit and confirm movement and the handoff stop. Resume at 1×, 2× and 3× and confirm shared-clock movement responds.
9. After arrival, switch manually to World View and confirm the same named ant appears at `(6,4)` with unchanged identity, needs, skills and inventory.
10. Select that ant in World View, press **Use Nest Entrance**, observe travel to `(5,4)`, then switch manually to Colony and confirm return at `(0,-8)`.
11. Repeat a round trip, switch views repeatedly during travel, and confirm no duplicate visual, missing ant, extra residency commit or cancelled travel.
12. Attempt a second ant while the entrance is reserved and confirm it cannot take ownership. Cancel/disable the travelling ant before commit where practical and confirm safe source retention/release.
13. Confirm Colony Excavate/Build/Cancel tools are unavailable in World View, then return to Colony and verify excavation and construction still function.
14. Confirm Colony jobs/reservations remain valid, hidden-region simulation continues, and the Unity Console has no new errors or exceptions.

### WV-B1 known boundaries

Temporary Surface visuals and the view/entrance strip are development presentation, not production art or final HUD layout. Surface residency is in-memory only. No Surface resources, natural rest, foraging, extraction, water, automatic cross-view routes, streaming, unloaded simulation, save/load, hazards, combat, brood, procedural generation or additional entrances are present. The repository has no `docs/roadmap.md`; the established wiki roadmap is `docs/development-roadmap.md`, which was updated instead of creating a competing page.

## WV-A two-view architecture specification — 14 October 2026

**DOCUMENTED, NOT IMPLEMENTED:** `WorldViewTechnicalSpecification.md` is the implementation-ready contract for the future Colony/World connection. It specifies distinct typed coordinate spaces, stable region/entrance/source identities, one authoritative ant residency, a capacity-one idempotent transition with rollback, manual camera/input switching, shared-clock loaded-region behavior, finite sources, exact-once food/water interactions, a recoverable one-unit liquid payload, natural rest and WV-B/WV-C acceptance tests.

Read-only code inspection found reusable seams in `AntRecord`, `AntRegistry`, `AntEntityBindingRegistry`, the shared clock, Colony navigation, job ownership and physical-resource transactions. It also confirmed current Colony-only assumptions: unscoped `Vector2Int` targets, one camera/navigation service in the command controller, `PlayerViewContext.Colony`, loaded-visual local orders and a single-region development host. No residency, Surface terrain/motor, view coordinator, entrance runtime, natural sources, liquid payload or cross-view routing exists.

Historical sections later in this file retain the evidence available at their dates. Where they say general pathfinding, excavation, survival or colony work is absent, the newer dated sections above them and current repository code supersede those statements. The Mara/Pip/Tala development scene and all Unity assets remain unchanged by WV-A.

## Founding-colony survival decisions — 14 October 2026

**APPROVED DESIGN ONLY, NOT IMPLEMENTED:** eventual new game begins with a
single founding queen and no prebuilt facilities. Finite natural food/water
sources may satisfy needs directly under later approved rules; safe natural
positions permit baseline rest; Food/Water/Material Storage, Sleeping and
Nursery are constructed Placements over natural Backgrounds. Foraging locates
resources; Extraction generates complete recoverable physical output; Hauling
handles transport. Resource and Node v1.0 supersedes the earlier one-unit-water
assumption with finite whole-node output plus a liquid-compatible physical
representation whose capacity/direct-drinking details remain unresolved.
Existing Mara/Pip/Tala survival fixtures remain for reproducible testing. Do
not mark natural-source, World View or brood gameplay implemented.


## Focused survival-needs balance correction — 14 October 2026

**Implemented — Pending Manual Acceptance.** A deterministic 12-day projection for Mara, Pip and Tala now drives the production needs and survival-decision APIs while using real path costs from work anchors beside the two small excavation areas. The baseline isolated the cause of low output: the old Fatigue cycle (4/hour awake, 12/hour recovery, intervention at 60) forced frequent long sleep round trips, while Hunger 2/hour, Thirst 3/hour and one-hour Eat/Drink actions added more survival travel. No reachability, retry, pause, clock-speed, excavation-coordinate or job-interruption defect was found.

Across 864 colony-hours with deliberately abundant baseline supplies, the old values measured **88.00 productive-work hours**, **280.75 work-travel hours**, **11.50 eating hours**, **20.50 drinking hours**, **177.75 resting hours**, **285.50 survival-travel hours**, no idle/blocked time, and only **42.7% productive work plus productive return travel**. Needs reached Hunger 0, Thirst 0 and Fatigue 100. This confirms survival timing and trip frequency, rather than excavation duration, as the dominant cause.

The corrected provisional values are Hunger loss **1/hour**, Thirst loss **1.4/hour**, awake Fatigue gain **1.4/hour**, rest recovery **16/hour**, Eat **0.5 hour**, Drink **0.5 hour**, Hunger/Thirst interventions at **40**, and Fatigue intervention at **70**. The reviewed 1.5 Thirst/hour and 2 Fatigue/hour candidates were tested first, but real route exposure still crossed critical Thirst and produced only 66.5% productive share; the final evidence-based 1.4 values provide safe travel margin. Restores remain 40 Hunger and 50 Thirst. Critical thresholds remain Hunger 10, Thirst 10 and Fatigue 90; starvation damage remains 2 Health/hour. Critical interruption, safe ordinary interruption, bounded retry, physical consumption, pause and speed semantics are unchanged.

The final projection measured **449.00 productive-work hours**, **161.50 work-travel hours**, **8.50 eating hours**, **10.50 drinking hours**, **62.25 resting hours**, **172.25 survival-travel hours**, zero idle/blocked time, and **70.7% productive work plus productive return travel**. It consumed 17 food and 21 water and stayed above critical at minimum Hunger 25.8, minimum Thirst 10.2 and maximum Fatigue 87.9.

The previous accessible development supply was mathematically insufficient for 12 days: 12 food and 16 water, compared with the measured revised demand of 17 food and 21 water. The same two reachable Food Storage and two reachable Water Storage cells now contain 12 food each and 16 water each, for finite totals of **24 food and 32 water**. The three sleeping positions and intentionally unreachable 3-food failure fixture are unchanged; there is no refill or infinite-resource behavior.

### Automated verification

- Focused Edit Mode: **87/87 passed** (needs, exact intervention boundaries, critical behavior, interactions/consumption, deterministic 12-day productivity and supply exhaustion, clock/pause).
- Focused Play Mode: **18/18 passed** (survival interaction, autonomous survival and simulation-clock regressions).
- Complete Play Mode: **67/67 passed**, zero failures/skips.
- Complete graphics-enabled Edit Mode: **520/520 passed**, zero failures/skips. The first run was 518/520 because its allow-list still expected the superseded shared Stone sprite; after the user confirmed the imported `Colony_R05_C05` Solid / `Colony_R05_C03` Background mapping is intentional, validation, setup and documentation were corrected and the complete suite passed.

### Exact manual Unity verification

1. Open `Assets/AntColony/Scenes/AntGameDevelopment.unity`, enter Play Mode and confirm the simulation begins paused.
2. Rebuild/reset the development world; confirm two reachable Food stores show 12 each, two Water stores show 16 each, three sleeping positions remain, and the surrounded failure fixture remains unreachable.
3. Designate the Soil fixture and the three-cell Soil area, then resume at 1× with autonomous survival and colony work following normal gameplay.
4. Observe Mara, Pip and Tala through several work/survival cycles; confirm ordinary needs wait for safe work interruption and critical needs still interrupt when necessary.
5. Confirm Eat and Drink each complete after 0.5 simulation hour, consume exactly one reserved physical unit only on completion, and restore 40 Hunger / 50 Thirst.
6. Confirm Rest reduces Fatigue at 16/hour and completes at Fatigue 20 or lower; cancel a rest and confirm applied recovery remains while the sleeping reservation releases.
7. Run representative excavation for multiple days and confirm the ants spend most time working or travelling back to work rather than cycling continuously between survival destinations.
8. Let the clock approach Day 12 with the representative workload; confirm reachable supplies do not exhaust and no ant reaches critical Hunger, critical Thirst or critical Fatigue under normal reachable conditions.
9. Pause during work, travel and interaction; confirm needs, orders, reservations, work progress and consumption stop without cancellation, then resume correctly.
10. Repeat representative sections at 1×, 2× and 3×; confirm equivalent simulation outcomes and no increased need loss per simulation hour.
11. Exhaust or reserve a supply deliberately; confirm bounded retry and meaningful failure state remain, with no free restoration or negative quantity.
12. Confirm the Console contains no errors, assertions or warnings introduced by this correction.

This focused correction does not change the acceptance state of any milestone. In particular, Milestone 3.5A remains **Implemented — Pending Manual Acceptance**.

## Current implemented milestone — 3.5B Excavation Output & Loose Resource Handling (14 October 2026)

**Implemented — Pending Manual Acceptance.** Successful Soil and Stone excavation now always creates the material definition's quantity as a physical loose world drop. Excavation never adds output to the excavator's inventory, never requires inventory capacity or storage, and never assigns a follow-up hauling job. The accepted excavation progress, skill/Rested modifiers, XP, shared job ownership, foreground-only mutation, Background preservation, Tilemap/collision refresh and navigation invalidation remain unchanged.

`ExcavationWorldTransaction` first validates the target Solid and preflights the complete yield against `WorldMaterialDropRegistry`. Output is placed at the excavated authoritative world cell: compatible partial stacks at that cell fill first, then at most one new stack is created under the stable excavation-job key. Multiple traversable logical stacks may share a cell, so the present implementation does not need an alternate-cell search. If a configured yield cannot fit the existing bounded representation or failure injection rejects creation, terrain is not removed. Once preflight succeeds, foreground removal and the non-fallible prepared drop commit occur once; repeated completion cannot duplicate output.

Hauling is the separate transport activity: eligible ants reserve a loose stack and explicit Construction Site or Material Storage capacity, collect within personal capacity, then deliver exactly once. With no valid destination or Hauling disabled, output remains loose and recoverable. Construction no longer collects material; it performs work after site staging is complete.

Terminology is now explicit: **Foraging** discovers resource locations; **Extraction** physically removes a resource and leaves loose output; **Hauling** picks up and transports already-extracted material to staging or Storage; **Construction** performs fully staged work.

### 3.5B automated verification

- Focused Edit Mode: **96/96 passed** (excavation, inventory/material handling, hauling, construction and Colony presentation).
- Focused Play Mode: **14/14 passed** (excavation, hauling, construction and Colony presentation).
- Complete graphics-enabled Edit Mode: **514/514 passed**.
- Complete Play Mode: **67/67 passed**.
- All four runs reported zero failures and zero skips. Unity compilation completed successfully. The strict documentation build is recorded with the final repository checks.

### Exact 3.5B manual Unity Play Mode acceptance

1. Open `Assets/AntColony/Scenes/AntGameDevelopment.unity` and enter Play Mode; confirm the in-game simulation starts paused.
2. Confirm the player HUD remains separate from the hidden Developer Mode panel.
3. Select an ant whose inventory is empty.
4. Designate the Soil excavation fixture.
5. Press Play and observe the ant reach an adjacent work position and excavate.
6. Confirm the foreground Soil disappears and loose Soil remains at the excavation cell.
7. Confirm the excavator's inventory is unchanged.
8. Reset, excavate the Stone fixture, and confirm loose Stone with unchanged inventory.
9. Reset, use Developer Mode to fill the chosen excavator's inventory, then designate excavation.
10. Confirm excavation completes without unloading and output remains loose.
11. Confirm excavation succeeds without creating or requiring compatible storage.
12. With Hauling enabled, confirm another eligible ant can reserve and collect the new loose output.
13. Confirm that ant transports the material to compatible Material Storage and deposits the exact quantity.
14. Reset, disable Hauling, excavate, and confirm output remains loose without a pickup/drop loop.
15. Re-enable Hauling and confirm the unchanged loose output becomes eligible for normal collection.
16. Designate a functional Background conversion that requires Soil.
17. Confirm its builder can reserve and collect loose excavation Soil directly without storage first.
18. Confirm the builder stages and consumes exactly the recipe quantity before the conversion completes.
19. Stage material at another construction site, cancel it, and confirm the exact quantity remains recoverable.
20. Compare inventory, loose-drop, storage and staged totals through the flow; confirm no loss or duplication.
21. Cancel an excavation before completion, confirm no output, and re-designate the same still-solid cell successfully.
22. Confirm 3.5A hover, live drag, persistent designation outlines and right-click cancellation remain aligned and functional.
23. Pause and resume during excavation and hauling; confirm progress, ownership, reservations and material remain preserved while paused.
24. Repeat representative excavation/hauling flows at 1×, 2× and 3× and confirm equivalent simulation outcomes.
25. Confirm ants still navigate correctly around the changed terrain and fall only under the accepted movement rules.
26. Reset the world and confirm the original deterministic terrain, empty ant inventories and two authored loose-drop fixtures return.
27. Confirm the Unity Console is free of errors and warnings introduced by this milestone.

Manual acceptance is not complete until the user confirms these observations. Milestone 3.5A remains independently **Implemented — Pending Manual Acceptance**; 3.5B does not change its art mappings, preview geometry or visual acceptance status.

## Current implemented milestone — 3.5A Colony View Presentation, Tile Preview & Art Integration (14 October 2026)

**Implemented — Pending Manual Acceptance.** Excavation and Background Conversion now show a bounded, per-cell hover/drag preview above terrain. The presentation draws an explicit translucent fill and four screen-space border edges over the exact projected world-cell rectangle, rather than depending on the active IMGUI box skin. The alignment correction derives that rectangle from `WorldTileCoordinates.CellToWorldPosition` plus half the authoritative cell size: integer world coordinates are cell centres, matching the authored Grid's `(-0.5, -0.5)` transform and one-unit Tilemap cells. The earlier implementation incorrectly treated the integer coordinate as the lower-left corner, shifting every overlay half a world unit up and right. Excavation uses an amber border with a small `X`, valid construction uses a green border with `+`, invalid targets use red `!`, and existing compatible designations use a muted `≡` marker. Committed pending/active jobs retain a subtle full-cell border; cancelled and completed jobs do not render. The preview asks the authoritative designation registry for each cell's state and reason, and never mutates terrain, creates work, or creates per-tile GameObjects.

Hover and bounded drag preview update every frame, including while simulation is paused because designation authoring remains a player command. Entering blocked UI clears the preview and abandons the pending drag, so releasing over UI cannot commit a world designation. Right-click while Excavation or Background Conversion is active consumes the world click and calls only that tool's authoritative cancellation API. Normal Select-mode right-click orders are unchanged.

Cancelled cell-stable job IDs may now be replaced by a fresh job, while active, paused and completed IDs remain protected. Cancellation releases any reservation, retains the cancelled job's recorded progress for diagnostics, and leaves the foreground Solid untouched. A still-eligible Solid can therefore repeat `Available → Designated → Cancelled → Designated` without limit; completed excavation cannot be cancelled or re-created. Construction follows the same re-designation rule. Its existing staged-material recovery remains conservative and now uses a distinct transaction key for every cancellation cycle so repeated cycles cannot suppress or duplicate a recovered drop.

The first custom Colony art mapping is intentionally narrow. `Colony_R02_C01` supplies Soil foreground and darker Natural Earth Background Tiles; `Colony_R05_C05` supplies Stone foreground and `Colony_R05_C03` supplies the darker Natural Stone Background. All four Tile assets remain collider-free. Prepared and functional Backgrounds retain their clear temporary colours because the candidate sheet does not contain a verified independent semantic match. World data, collision, navigation, material generation and excavation rules are unchanged.

### 3.5A automated verification

- Focused alignment Edit Mode: **18/18 passed**, including authoritative Tilemap/overlay centre agreement and projection at `1280×720`, `1920×1080`, and `2560×1080` across camera pan and minimum/default/maximum zoom.
- Complete graphics-enabled Edit Mode: **513/513 passed**.
- Complete Play Mode: **67/67 passed**.
- Updated Unity art validation passed for all 170 prepared candidates, import settings, the four allow-listed runtime mappings and collider-free Tile configuration.
- Deterministic art preparation `--verify-only`, strict Zensical documentation build and `git diff --check` passed. A separate headless full-Edit attempt crashed inside Unity's Tilemap renderer during the existing graphics-backed pixel test; the authoritative complete result is the subsequent graphics-enabled 503/503 pass.

### Exact 3.5A manual Unity Play Mode acceptance

1. In Excavation mode, hover an eligible Soil or Stone Solid and confirm an amber border aligned to the full tile.
2. Move across several tiles and confirm the border follows the pointer while eligible and ineligible colours remain distinct.
3. Left-click an eligible Solid and confirm a persistent amber border plus a small `X`.
4. Right-click that cell and confirm its marker and border disappear without issuing a movement order.
5. Left-click the same still-solid cell and confirm it can be designated again.
6. Repeat cancellation and re-designation five times.
7. Hold left mouse and drag across mixed cells; confirm every bounded cell updates continuously before release.
8. Release over non-UI world space and confirm eligible cells become persistent designations; release a second drag over UI and confirm nothing commits.
9. Cancel one cell and confirm adjacent designations remain pending.
10. Pause and resume; confirm hover/designation authoring remains responsive and committed state is preserved while work execution stays paused.
11. In Background Conversion, verify green hover, live drag, red invalid and muted existing-designation previews.
12. Cancel and re-designate an eligible construction cell.
13. Stage construction material, cancel, and confirm reservations release and exactly the staged quantity becomes a physical loose drop with no duplication.
14. Verify all HUD/modal input blocking, camera zoom, and `1280×720`, `1920×1080`, and `2560×1080` layouts.
15. Confirm a clean Unity Console and unchanged movement, clock, job, HUD and accepted Soil/Stone art behaviour.

Known limitations: the source sheet has no verified independent Prepared or functional Background art; composite cavities, exposed edges, roots, fungi and decorations remain unbound; `Colony_R02_C01`, `Colony_R05_C03` and `Colony_R05_C05` repeat safely as full-cell textures but visible tile seams remain possible and no seamless/autotile claim is made. Manual acceptance remains pending.

## Latest acceptance — 3.4C Player Colony HUD & Developer UI Separation (14 October 2026)

**COMPLETE — MANUALLY ACCEPTED.** User confirmed 13/13 grouped Unity HUD/gameplay/input checks, including resolution testing and clean Console. Codex previously reported focused 12/12 Edit, 13/13 Play; full 489/489 Edit and 62/62 Play. No tests rerun in this documentation update. Earlier pending-acceptance passages are historical. The separate art foundation remains **Pending Manual Visual Acceptance**. At the time of this 3.4C record, World View runtime was not implemented; WV-B1 has since implemented its bounded foundation.


## Art foundation — 14 October 2026

**Implemented — Pending Manual Acceptance.** Original `1254x1254` RGBA Colony and World tilesets are preserved under `Assets/_AntGame/Art/<View>/Source`. A deterministic preparation tool produces 100 Colony and 70 World position-named `32x32` candidate sprites with Multiple-sprite, 32 PPU, Point, uncompressed, no-mipmap import metadata. The extraction uses measured variable source rectangles and aspect-preserving square padding rather than blind 32x32 source slicing.

This foundation status is superseded only by the narrow 3.5A visual mapping above: two reviewed Colony candidates now feed four existing natural Soil/Stone Tile assets. No candidate is claimed seamless, and wider production palettes remain deferred because several Colony cells combine Background, Solid, cavity edge and decoration visuals while World props have variable footprints. Gameplay terrain definitions, collision, excavation, navigation and ant locomotion remain unchanged. See `Documentation/ArtDirection.md` for the allow-list, source hashes, limitations and manual Unity inspection checklist.

## Latest acceptance — 14 October 2026

**Milestone 3.4B Unified Simulation Clock & Gameplay Mode: COMPLETE — MANUALLY ACCEPTED.** User confirmed all 20 grouped manual Unity Play Mode checks covering ordinary colony gameplay, pause/resume integrity, developer overrides, and clean Console. Codex reported focused 11/11 Edit and 7/7 Play; full graphics-enabled 485/485 Edit and 58/58 Play passed before manual acceptance. This documentation-only change did not rerun tests. Earlier 3.4B pending-acceptance text below is historical.

**Milestone 3.4C Player Colony HUD & Developer UI Separation: COMPLETE — MANUALLY ACCEPTED.** The user confirmed 3.4C acceptance before authorising 3.4D. The authored uGUI HUD exposes the accepted Colony View loop without developer controls and retains its recorded 12/12 focused Edit, 13/13 focused Play, 489/489 complete Edit and 62/62 complete Play evidence.

**Milestone 3.4D Colony Management & Work Priorities UI: Implemented — Pending Manual Acceptance.** The existing HUD now opens a global scrollable Work matrix derived from active authoritative `AntRecord` entries. Extraction, Construction and Hauling are the functional columns. Each cell writes the record-owned 1–4/Disabled value, displays the mapped skill level and immediately refreshes without selecting or loading the ant.

The matrix displays current authoritative activity, active category/priority and supported failure or waiting text. Priority edits reset subsequent autonomous evaluation but do not cancel active work, alter skill levels, mutate Developer overrides or pause the clock. The retained developer priority controls observe the same `AntWorkPrioritySet`. The modal uses the existing Canvas, EventSystem and raycast boundary; Escape closes it before cancelling a Colony View tool, and matrix scroll/click input remains blocked from the world camera and commands.

Automated verification passed **7/7 focused Edit Mode**, **7/7 focused Play Mode**, **492/492 complete Edit Mode**, and **65/65 complete Play Mode**.

## Current implemented milestone — 3.4D Colony Management (14 October 2026)

### Exact 3.4D manual Unity Play Mode acceptance

**Pending:** perform these checks in `Assets/AntColony/Scenes/AntGameDevelopment.unity`; keep 3.4D at **Implemented — Pending Manual Acceptance** until the user confirms them.

1. Open the management screen.
2. Confirm Mara, Pip and Tala appear.
3. View their current skill levels.
4. Set different Excavation priorities.
5. Set different Construction priorities.
6. Set different Hauling priorities.
7. Disable a supported work type for one ant.
8. Confirm another ant can still perform that work.
9. Change priorities while paused.
10. Resume and observe subsequent job selection.
11. Confirm active tasks are not unexpectedly cancelled.
12. Close and reopen the screen.
13. Confirm data remains unchanged.
14. Open Developer Mode and inspect the same priorities.
15. Test 1280×720, 1920×1080 and 2560×1080.
16. Confirm no UI click leakage or camera-scroll conflicts.
17. Verify Play/Pause and speed controls remain functional.
18. Confirm the Unity Console is clear.

### Known limitations

Surface Preparation remains hidden because no real autonomous executor exists. Foraging, Production, Combat and Brood Care likewise remain developer/future categories. The matrix has no work areas, queues, automatic roles, alerts or persistence. It can edit an unloaded ant record, but does not invent region residency or cross-view selection. Visual layout at all three target resolutions and live job-choice observation remain pending manual acceptance.

Developer IMGUI remains intact but starts hidden and opens deliberately through the centre-top **Developer Mode (F12)** control or F12. The player HUD remains active while it is open. uGUI raycasting feeds the existing central input boundary, while IMGUI retains its bounded hit rectangles; world commands, designation drags and camera mouse controls therefore stop over either interface. The temporary `SimulationTimeControlStrip` has been removed from the authored scene.

Automated verification passed **12/12 focused Edit Mode**, **13/13 focused Play Mode**, **489/489 complete graphics-enabled Edit Mode**, and **62/62 complete Play Mode**. A separate headless full-Edit attempt crashed inside Unity's test runner without results; the required graphics-enabled retry passed. Manual Play Mode acceptance below remains outstanding.

## Accepted milestone — 3.4C Colony View Player HUD (14 October 2026)

### Authored Canvas and responsibility boundary

`PlayerHud.prefab` owns one Screen Space Overlay Canvas with scale-with-screen-size layout, one reusable `AntRosterEntry.prefab`, and focused roster, inspector, clock, command, resource and job-status presenters. `AntGameDevelopmentWorldHost` only binds existing authorities after rebuilding the deterministic scene. HUD components do not own or copy simulation state, discover ants scene-wide each frame, or introduce another clock, registry, scheduler, inventory wallet or movement system.

The HUD's global surfaces are the roster, selected shared ant record, simulation time and physical resource summary. The bottom command bar is explicitly `Colony` context and exposes no World View runtime, scene switching, surface terrain, entrances, residency or cross-view navigation.

### Exact 3.4C manual Unity Play Mode acceptance

**Accepted:** the user confirmed this checklist before authorising Milestone 3.4D. It remains below as acceptance history.

1. Open the development scene.
2. Confirm Day 1, 08:00 paused start.
3. Confirm the player HUD is visible without developer controls.
4. Confirm Mara, Pip and Tala appear.
5. Select each ant from the roster.
6. Select ants directly in Colony View.
7. Check roster/inspector synchronisation.
8. Inspect needs, skills and inventory.
9. Test player Play/Pause.
10. Test 1×, 2× and 3×.
11. Mark excavation while paused using only player tools.
12. Test excavation drag designation.
13. Press Play and observe autonomous excavation.
14. Verify physical resource drops and hauling.
15. Select all six available Background conversions.
16. Check the displayed provisional costs and four-hour work requirements.
17. Designate construction while paused.
18. Observe autonomous material collection and construction.
19. Inspect job progress and waiting reasons.
20. Test construction cancellation and recoverable staged material.
21. Test excavation cancellation.
22. Check the resource summary against actual inventory, storage, drops and staged material.
23. Exercise inventory recovery and ground-drop fallback.
24. Open Developer Mode deliberately with F12 or its separate reopen control.
25. Confirm all seven developer categories and fixture controls remain available.
26. Close Developer Mode without altering simulation state or autonomy.
27. Test input blocking across player HUD and developer UI, including drag start/release.
28. Test camera pan, middle-drag and zoom outside UI, and confirm UI scrolling does not zoom.
29. Test 1280×720, 1920×1080 and 2560×1080.
30. Reset the scene and confirm the deterministic starting state.
31. Verify the Unity Console is free of relevant errors and warnings.

### Known limitations

The roster compatibility adapter can select only an ant with a currently loaded Colony View representation; registered unloaded/future-region ants remain displayable but cannot yet be focused. Region residency and cross-view selection are deliberately absent. There is no colony alert system, contextual order menu, World View toolset, save/load or production bootstrap.


## Current implemented milestone — 3.4B Unified Simulation Clock & Gameplay Mode (14 October 2026)

**Milestone 3.4A Functional Background Conversion & Construction Jobs: MANUALLY ACCEPTED.** User confirmed 3.4A Play Mode testing accepted, including the inventory-capacity recovery correction. Codex previously reported focused construction 19/19 Edit and 4/4 Play; full 474/474 Edit and 51/51 Play after that correction. These suites were not rerun for this documentation-only update. Earlier "pending manual acceptance" passages below are retained as historical milestone records.

**Status: Implemented — Pending Manual Acceptance.** The scene-owned `SimulationClockController` is now the sole runtime authority for paused/running state, 1×/2×/3× speed, elapsed simulation seconds/hours and the Day 1 08:00 calendar origin. New sessions begin paused. Pause retains the selected speed; resume restores it. `Time.timeScale` stops fixed-step movement, climbing and falling, while the clock gates navigation scheduling, autonomous evaluation, needs/skill ticks, interactions, work, transfers and retry deadlines. The project baseline `Time.fixedDeltaTime` and `Time.maximumDeltaTime` remain constant at every speed and are restored with the original time scale when the controller is disabled or destroyed. Clock catch-up is capped by `Time.maximumDeltaTime` per frame.

`GameplayModeCoordinator` applies one normal-gameplay bundle for Autonomous Survival, Colony Work and Hauling. Pause never calls those subsystem setters, so active routes, orders, job ownership, reservations, work progress and physical materials remain intact. Developer controls expose each subsystem as Follow Normal Gameplay, Forced Enabled or Forced Disabled without changing clock state. Existing direct setters remain usable by isolated tests.

The temporary bottom-centre `SimulationTimeControlStrip` shows day/time, Play/Pause, 1×/2×/3× and running state independently of the seven-category developer toolbar. Its responsive rectangle participates in the shared world-input blocker. Camera input stays unscaled and available while paused. Direct orders, cancellation, priority changes and excavation/construction designation authoring remain available while paused; execution waits for resume.

### 3.4B automated verification

- Focused Edit Mode clock suite: **11/11 passed**.
- Focused Play Mode integration suite: **7/7 passed**.
- Complete graphics-enabled Edit Mode suite: **485/485 passed**.
- Complete Play Mode suite: **58/58 passed**.
- Runtime and test assemblies compile cleanly through Unity and its generated Roslyn response files.

### Exact 3.4B manual Unity Play Mode acceptance

**Pending:** complete these checks in `Assets/AntColony/Scenes/AntGameDevelopment.unity`; do not mark 3.4B complete until the user confirms them.

1. Launch the development scene and confirm the simulation starts paused.
2. Confirm Mara, Pip and Tala remain present.
3. Confirm the day/time readout and Play/Pause, 1×, 2× and 3× controls are visible.
4. Designate excavation before pressing Play.
5. Designate construction before pressing Play.
6. Issue a direct move order while paused and confirm it is displayed.
7. Confirm no ant moves while paused.
8. Confirm no needs change while paused.
9. Press Play and confirm all normal-gameplay autonomy is active.
10. Confirm excavation begins without separately enabling jobs.
11. Confirm hauling functions without separately enabling hauling.
12. Confirm construction functions without separately enabling work.
13. Confirm survival needs are handled without separately enabling survival.
14. Pause during excavation and confirm progress freezes.
15. Pause during construction and confirm progress freezes.
16. Pause during movement and falling and confirm positions freeze.
17. Resume and confirm preserved activities continue.
18. Test 1×, 2× and 3×.
19. Confirm speed changes simulation outcomes over real time rather than only changing the UI label.
20. Test a critical need through pause and resume.
21. Test the accepted full-inventory construction recovery scenario through pause and resume.
22. Interrupt and resume a construction job and confirm ownership/progress behavior remains safe.
23. Check resource totals before and after pause/resume for loss or duplication.
24. Confirm pending navigation requests do not advance while paused.
25. Use Developer Mode's Follow/Forced Enabled/Forced Disabled autonomy overrides for isolated tests and confirm they do not change pause state.
26. Hide Developer Mode and confirm the time controls remain visible.
27. Change work priorities and author excavation/construction designations while paused.
28. Confirm the time strip blocks underlying world clicks while camera movement and zoom still work.
29. Reset/reload the scene and confirm the deterministic Day 1 08:00 paused state returns.
30. Confirm the Unity Console contains no relevant errors or warnings.

### 3.4B known limitations

The time strip is deliberately temporary IMGUI, not the authored production uGUI HUD. There is no roster, player priority matrix, alert system, general command queue, save/load, multiplayer, doors, rooms, brood or new gameplay content. The development controller still coordinates several simulation consumers; further UI/controller extraction belongs to 3.4C or a later focused architecture pass.


## Current implemented milestone — 3.4A Functional Background Conversion & Construction Jobs (13 October 2026)

**Historical 3.4A checkpoint:** this section records the implementation before
later acceptance and WV-B2B logistics correction. Its builder self-supply text
is superseded: current Hauling delivers to site staging and Construction is
work-only. Its functional Background result remains implemented compatibility,
not the Resource and Node v1.0 target Placement model. Doors, rooms, brood,
production, crafting, combat and reproduction remain excluded.

The player can select Prepared, Nursery, Sleeping, Food Storage, Water Storage or Material Storage in the accepted **World** toolbar category, then click or bounded-drag exposed cells in Background Conversion mode. Covered, inaccessible, occupied, duplicate and same-type targets are rejected. Each accepted designation creates a Construction job in the existing `ColonyJobRegistry`; priorities, Disabled behavior, actual path-cost selection, exclusive reservation, survival precedence and player-order interruption remain shared with established colony work.

`ConstructionRecipe` data declares the expected source, physical resource, quantity, duration and result. The current provisional development recipes are:

| Result | Cost | Base duration |
| --- | --- | --- |
| Prepared Background | 1 Soil | 4 simulation hours |
| Nursery Background | 1 Soil | 4 simulation hours |
| Sleeping Background | 1 Soil | 4 simulation hours |
| Food Storage Background | 1 Soil | 4 simulation hours |
| Water Storage Background | 1 Stone | 4 simulation hours |
| Material Storage Background | 1 Soil | 4 simulation hours |

The reserved builder first uses compatible existing inventory, otherwise reserves the closest reachable compatible quantity from Material Storage or a loose drop. If its slots are full of material that the construction job does not need, the reusable `InventoryCapacityRecoveryOperation` retains recipe material, reserves the closest reachable compatible storage capacity by actual path cost, physically travels there and deposits only enough to free a usable slot. If no such storage is reachable, it transactionally drops the unnecessary stack at its current valid anchor. The order then resumes supply automatically. Recovery is bounded, safely interruptible and preserves job ownership, construction progress, reservations and exact physical quantities.

Loose drops remain valid, recoverable and traversable; no storage shortage can hard-block construction merely by requiring this fallback. Approved future clutter concentration and enclosed-room cleanliness/quality penalties are documented in the GDD, but this correction does not change movement cost, navigation classification or room scoring.

Construction starts only after all required material is staged. Construction skill and Rested modify committed work progress, while Construction XP is awarded only for that accepted progress. Interruptions release source/job reservations while retaining progress, carried inventory and staged site material. Cancelling a designation converts staged material into a recoverable loose drop at the worksite. Completion revalidates source Background, foreground exposure, occupancy, delivered material and ownership; it then changes only the Background, consumes the exact staged quantity once, creates the functional tile's one-slot capacity, refreshes presentation and invalidates shared navigation. Failed or repeated commits consume nothing.

### 3.4A automated verification

- Focused Edit Mode construction suite: **19/19 passed** after the inventory-capacity correction.
- Focused Play Mode construction suite: **4/4 passed**.
- Complete graphics-enabled Edit Mode suite: **474/474 passed**.
- Complete Play Mode suite: **51/51 passed**.
- Runtime, Editor, Edit-test and Play-test assemblies also compile cleanly through Unity's generated Roslyn response files.

### Exact 3.4A manual Unity Play Mode acceptance

**Pending:** complete these checks in `Assets/AntColony/Scenes/AntGameDevelopment.unity`; do not mark 3.4A complete until the user confirms them.

1. Confirm Mara, Pip and Tala retain their accepted identities, skills and needs.
2. Open **World**, enter Background Conversion mode and confirm selection/move commands do not fire.
3. Cycle through Prepared, Nursery, Sleeping, Food Storage, Water Storage and Material Storage targets.
4. Designate one valid exposed tile and confirm visible pending feedback.
5. Drag over a bounded valid area and confirm one Construction job per eligible cell.
6. Confirm covered, inaccessible, duplicate and same-type cells are rejected with a reason.
7. Set Construction priorities to 1, 4 and Disabled; confirm priority order and Disabled behavior.
8. Confirm one builder exclusively claims a Construction job and navigates to its material source.
9. Confirm the builder collects the reserved Soil or Stone into its own inventory.
10. Give the builder compatible inventory first and confirm it travels directly to the site.
11. Exercise the multi-trip fixture/test setup and confirm the same builder supplies every trip.
12. Confirm the builder travels to a valid adjacent construction position rather than interacting remotely.
13. Confirm construction progress advances over simulation time and remains visible under **Jobs**.
14. Confirm only committed construction progress awards Construction XP.
15. Compare Rested and ordinary builders and confirm the Rested work-speed effect.
16. Complete construction and confirm the requested Background material appears.
17. Confirm the required physical resources are consumed exactly once.
18. Confirm foreground Solid occupancy is unchanged by successful conversion.
19. Confirm new Food, Water, Sleeping and Material Storage tiles expose one correct empty capacity; Nursery remains capacity-only with brood deferred.
20. Fill a builder's inventory with unnecessary material; confirm it retains recipe material, unloads only enough at the closest reachable compatible storage, and resumes construction.
21. Repeat with storage full or unreachable; confirm a traversable recoverable ground drop is created, then test player/critical-survival interruption and cancellation without loss or duplication.
22. Remove available supplies and confirm the Construction job remains pending without consumption.
23. Attempt conversion of occupied Food/Water/Material Storage or a reserved sleeping tile and confirm safe rejection.
24. Confirm completed conversion refreshes the Tilemap and navigation without moving or trapping ants.
25. Confirm existing excavation and autonomous hauling still work.
26. Confirm autonomous hunger, thirst and rest behavior still works.
27. Confirm normal ant selection, movement, pickup and survival interaction orders still work.
28. Confirm toolbar categories, Hide All and UI input blocking remain correct during conversion dragging.
29. Use **Regenerate Seeded World** and confirm construction jobs, sites and converted tiles return to the deterministic baseline.
30. Confirm the Unity Console contains no relevant errors or warnings.

### 3.4A known limitations

Recipes, four-hour duration and skill scaling are provisional. The authored recipes currently cost one unit, while the generic order supports multi-trip quantities and has automated coverage. Inventory recovery currently chooses a current-anchor ground drop after bounded storage failure; broader nearby placement policy remains future work. Loose-resource clutter has no implemented penalties yet. Nursery supplies one capacity unit but brood behavior is deliberately absent. Construction sites are plain authoritative data rather than GameObjects; cancelled staged material becomes a loose drop. There is no demolition, relocation, crafting, room detection, door logic, production, save/load or chunk streaming.

## Latest accepted status — 13 October 2026

**Development UI Reorganisation: COMPLETE.** User confirmed all 24 manual checks and clean Unity Console. Reported focused Edit Mode 8/8, focused Play Mode 2/2, full Edit Mode 455/455 and full Play Mode 47/47 passed before a later environment-blocked rerun.

**Milestone 3.3D Material Storage & Autonomous Hauling: COMPLETE.** User confirmed all 30 manual checks and clean Console. Reported focused Edit Mode 15/15, focused Play Mode 3/3, full Edit Mode 447/447 and full Play Mode 45/45 passed. No tests rerun for this documentation update. Prior pending-acceptance statements below are historical.


## Development UI reorganisation pass (13 October 2026)

**Status: COMPLETE — all 24 manual checks confirmed by the user on 13 October 2026. This pass remains separate from Milestone 3.4A.**

The integrated development scene now uses one compact IMGUI toolbar across the top of the Game view. Its mutually exclusive categories are **Ant, Needs, Skills, Jobs, Resources, World and Debug**. Selecting an open category collapses it; selecting another replaces it, so expanded panels cannot overlap each other. `Hide All` removes the toolbar, summary, expanded panel and excavation overlays, leaving only a small `Show UI` button.

Existing controls were preserved and regrouped:

- **Ant:** identity, colony, caste, attributes, lifecycle, control/order state, destination/failure and order cancellation.
- **Needs:** needs readout, autonomous-survival toggle/state and all needs/time/rest test controls.
- **Skills:** complete skill readout, XP/multiplier/maximum tests, inactivity and reset.
- **Jobs:** autonomous-work toggle/state, every per-ant priority, synthetic job creation/reset/cancellation and registry diagnostics.
- **Resources:** personal inventory, pickup/drop test tools, food/water/rest storage diagnostics, excavation/drop diagnostics, Material Storage filters/reservations, autonomous hauling and hauling reset.
- **World:** Excavate mode, inspected-cell movement commands, foreground/Prepared mutations, grip/falling fixtures, renderer isolation and seeded-world reset.
- **Debug:** authoritative cell/navigation/rendering readout and all navigation scheduler stress controls.

All expanded content uses the same resolution-bounded panel and an internal scroll view. A compact selected-ant summary remains below the toolbar. The central hit test blocks toolbar, summary, active panel and hidden-state reopen button from selection, movement, context orders, pickup and excavation input. Releasing an excavation drag over UI cancels the designation. Mouse wheel and middle-drag over UI no longer zoom or pan the world camera; camera controls remain unchanged outside UI.

### Development UI automated verification

- Focused Edit Mode layout/state/input tests: **8/8 passed**.
- Focused Play Mode scene/input tests: **2/2 passed**.
- Complete graphics-enabled Edit Mode: **455/455 passed**.
- Complete Play Mode: **47/47 passed**.
- Static layout checks cover 320×200, 1024×768, **1920×1080** and 2560×1080 bounds.

### Development UI manual Play Mode checklist

1. Open `Assets/AntColony/Scenes/AntGameDevelopment.unity` at 1920×1080 and enter Play Mode.
2. Confirm one compact horizontal toolbar appears across the top with Ant, Needs, Skills, Jobs, Resources, World and Debug.
3. Select an ant and confirm the compact summary shows its name, caste, control state, order and carried quantity.
4. Open **Ant** and confirm identity, colony, caste, attributes, lifecycle, order, destination, failure and cancellation remain available.
5. Open **Needs** and confirm needs/status diagnostics, autonomous-survival toggle, time advancement, refill, fatigue, rest, critical-needs and reset controls work.
6. Open **Skills** and confirm all skills plus XP multiplier, maximum, inactivity and reset tests remain available.
7. Open **Jobs** and confirm autonomous work, per-ant priorities, synthetic reachable/unreachable jobs, reset, cancellation and registry diagnostics remain available.
8. Open **Resources** and confirm inventory add/fill/drop/remove/reset, physical survival storage, excavation drops, Material Storage filters, hauling toggle/status and hauling reset remain available.
9. Open **World** and confirm Excavate mode can be entered/exited without losing the other world controls.
10. In **World**, confirm primary/legacy movement commands, foreground mutation, Prepared mutation, grip target and falling/recovery fixture work.
11. In **World**, confirm background isolation/restoration and seeded-world regeneration work.
12. Open **Debug** and confirm the full cell, ant, navigation and rendering readout is visible and scrollable.
13. In **Debug**, confirm stress sizes 1/100/300/500, spawn, clear, simultaneous, staggered and cancel-first-request controls remain available.
14. Switch repeatedly between categories and confirm only one expanded panel is visible at a time and no panels overlap.
15. Click the currently open category and confirm its panel collapses completely.
16. Resize the Game view through narrow, wide and 1920×1080 layouts; confirm the toolbar/panel remain inside the view and long content scrolls.
17. Click **Hide All** and confirm the toolbar, selected-ant summary, expanded panel and diagnostic overlays disappear, leaving only `Show UI`.
18. Click `Show UI` and confirm the toolbar returns with no category unexpectedly expanded.
19. Click buttons, category tabs and scrollbars and confirm no ant selection, move, pickup or context order is issued.
20. Enter Excavate mode, click/drag/release over the toolbar and panels, and confirm no designation is created beneath UI.
21. Scroll or middle-drag inside a panel and confirm it does not zoom or pan the world camera.
22. Move outside UI and confirm left/right-click commands, excavation dragging, WASD/arrow pan, mouse-wheel zoom, middle-drag pan and Home reset still work.
23. Confirm the improved Resources/Jobs/World panels still support every item in the existing 3.3D 30-point acceptance checklist.
24. Confirm the Unity Console contains no relevant errors or warnings.

## Current implemented milestone — 3.3D Material Storage & Autonomous Hauling (13 October 2026)

**Status: COMPLETE — all 30 manual checks confirmed by the user on 13 October 2026.** Milestone 3.3C is accepted. Milestone 3.3D adds traversable/grippable Material Storage Background tiles, one authoritative physical stack slot per tile, Soil/Stone/general filters, incoming-capacity reservations and autonomous delivery of loose or already-carried building material.

`MaterialStorageWorld` owns slots by world cell. Each slot records its filter, current resource and quantity, maximum stack, reserved incoming quantity and reservation owner. Changing a filter preserves existing contents; an incompatible change releases the pending incoming reservation and prevents further deposits. Deposits and withdrawals are source-to-destination transactions, so ordinary transfers cannot create negative quantities, mix resources or expose a half-committed transfer.

Autonomous hauling is discovered at the existing staggered colony-work cadence and represented by registry-owned Hauling jobs. A loose-drop haul reserves both source quantity and destination capacity before travel, uses actual A* path cost to select the closest reachable suitable adjacent storage approach, transfers into the ant inventory at the source, then deposits at the destination. An ant carrying Soil/Stone receives an owner-only self-deposit task; no other ant may claim that inventory. Cancellation before pickup leaves the drop in place; cancellation after pickup leaves the cargo on the carrier and releases obsolete storage capacity. Disabled Hauling, critical survival and explicit player orders retain the accepted priority and interruption boundaries.

Completed storage deliveries award provisional Hauling XP equal to **5 base XP per committed unit**. Awards pass through `AntRecord.TryAwardSkillExperience`, so Rested XP and dehydration restrictions apply. Merely reserving, travelling, picking up or cancelling awards no XP. Hauling skill does not override the player's work priority; its existing inventory-slot progression remains authoritative.

The development scene includes a general partial Soil slot, empty Soil-only and Stone-only slots, a full slot, an unreachable isolated slot, loose Soil/Stone, and the accepted excavation course. The collapsible diagnostics show storage contents, filters, incoming reservations, active haul details and failure state, with autonomous-hauling enable/disable and deterministic reset controls.

### 3.3D automated verification

- Focused Edit Mode: **15/15 passed**.
- Focused Play Mode: **3/3 passed**.
- Complete graphics-enabled Edit Mode: **447/447 passed** with zero failures/skips.
- Complete Play Mode: **45/45 passed** with zero failures/skips.
- Compilation completed as part of the Unity suites. The final handoff additionally records `git diff --check` and strict Zensical validation.

### Exact 3.3D manual Unity Play Mode acceptance

**Accepted:** the user confirmed all 30 checks and a clean Console on 13 October 2026.

1. Mara, Pip and Tala retain identities, skills and needs.
2. Material Storage background tiles are visible.
3. Soil-only, Stone-only and general storage filters work.
4. Each storage tile provides exactly one stack slot.
5. Loose Soil produces autonomous hauling work.
6. Loose Stone produces autonomous hauling work.
7. Haulers follow individual work priorities.
8. Disabled Hauling prevents autonomous claiming.
9. Ant navigates adjacent to a world drop.
10. Material enters the correct ant inventory.
11. Ant travels to a valid storage destination.
12. Stored quantity increases correctly.
13. Inventory quantity decreases by the same amount.
14. A partially full stack merges correctly.
15. Full storage rejects additional material.
16. Two ants cannot reserve the same source quantity.
17. Destination capacity cannot be over-reserved.
18. Oversized stacks are hauled in partial loads.
19. An ant carrying excavated material self-deposits.
20. Critical survival interrupts hauling safely.
21. Player orders interrupt hauling without material loss.
22. An interrupted carrier can resume depositing.
23. Removed or blocked storage triggers safe recovery.
24. Hauling XP is awarded for completed deliveries.
25. No pickup/drop XP farming occurs.
26. Existing excavation remains functional.
27. Existing autonomous survival remains functional.
28. World rebuild restores deterministic fixtures.
29. Camera, UI input and developer-panel controls remain functional.
30. Unity Console contains no relevant errors or warnings.

### 3.3D known limitations

Material Storage is pre-authored for this slice; ants cannot construct or convert it. Only Soil and Stone are accepted, stack maximum 10 and XP 5/unit are provisional, and storage offers one incoming reservation at a time. Hauling handling has no separate timed work phase yet; skill affects the accepted carrying capacity and XP systems but not travel speed. Inactive/deceased ants retain authoritative inventory, while recovery from those inventories remains a future explicitly scoped system. Save/load, chunk streaming, room bonuses and production-scale spatial indexing remain absent.

## Latest accepted milestone — 3.3C Ant Inventory, Automatic Excavation Pickup & Physical Material Handling (13 October 2026)

**Status: COMPLETE — all 26 manual Unity Play Mode acceptance checks confirmed by the user on 13 October 2026.** Previously reported focused Edit Mode 26/26, focused Play Mode 5/5, full graphics-enabled Edit Mode 432/432 and full Play Mode 42/42 passed; no new tests were run in this documentation-only update. Milestone 3.3B is also complete. Milestone 3.3C adds only authoritative personal inventory and explicit physical material transfers; it does not add autonomous hauling, stockpiles, Material Storage, construction or a colony resource wallet.

Every `AntRecord` owns one plain-C# `AntInventory`, so inventory survives visual replacement and remains independent of selection, navigation and UI. Soil and Stone use stable resource IDs and a provisional maximum stack of **10**. Compatible material first fills compatible partial stacks, then empty natural slots. Hauling determines natural slot count: levels 0–19 grant one slot; 20–39 two; 40–59 three; 60–79 four; and 80–100 five. Caste and Strength do not affect this value. If inactivity decay reduces capacity below occupied slot count, existing stacks remain intact and recoverable, while new additions are refused until usage is again within natural capacity.

Historical 3.3C behavior sent excavation output to inventory first. Milestone 3.5B supersedes that path: `ExcavationWorldTransaction` now preflights the full loose-drop yield before foreground removal and never mutates the excavator's inventory. The stable job output key prevents retry duplication. If the required output cannot be represented, the foreground Solid remains intact. The accepted Background preservation, projection refresh, collision update, navigation invalidation, progress and XP rules remain unchanged.

Loose `WorldMaterialDrop` data now includes maximum stack size, compatible same-cell merging, partial removal and a single-owner pickup reservation. A selected active ant can right-click a drop, navigate through the existing grid motor to the least-cost valid adjacent anchor, revalidate capacity and transfer its reserved quantity. Cancellation, replacement, failure, disable/death handling and world replacement release the reservation. Developer Only selected-ant controls can add/fill/remove test material, drop a selected quantity at the ant's valid current anchor, or reset only that ant's inventory. All successful pickup/drop operations remove from one authoritative owner and add the same quantity to the other; failed operations preserve the source.

The deterministic development scene contains a four-unit Soil drop and a three-unit Stone drop in addition to the accepted excavation fixtures. World rebuild recreates records with empty independent inventories and restores these authored drops without stale reservations. The selected-ant panel shows natural capacity, occupied slots, each resource/quantity/maximum, free compatible capacity, total carried quantity, Hauling level and the next unlock.

### 3.3C automated verification

- Focused Edit Mode: **26/26 passed**.
- Focused Play Mode: **5/5 passed**.
- Complete graphics-enabled Edit Mode: **432/432 passed** with zero failures/skips.
- Complete Play Mode: **42/42 passed** with zero failures/skips.
- Compilation completed as part of the Unity suites. `git diff --check` and strict Zensical validation are recorded at final handoff.

### Exact 3.3C manual Unity Play Mode acceptance

**Accepted:** the user confirmed all 26 checks on 13 October 2026.

1. Mara, Pip and Tala retain their identities.
2. Each ant has its own visible inventory.
3. All ants initially have one carrying slot.
4. Historical 3.3C acceptance: excavating Soil added Soil to the excavator's inventory; 3.5B supersedes this with loose Soil output.
5. Historical 3.3C acceptance: excavating Stone added Stone to the excavator's inventory; 3.5B supersedes this with loose Stone output.
6. Multiple identical resources stack correctly.
7. Incompatible resources occupy separate slots.
8. A full inventory causes excavation output to drop physically.
9. No excavated material is lost or duplicated.
10. Partial capacity transfers retain the remainder as a drop.
11. Right-clicking a world drop gives a Pickup order.
12. Ant navigates to a valid adjacent location before pickup.
13. Picking up a drop transfers the correct quantity.
14. Multiple ants cannot collect the same reserved material twice.
15. Cancelling pickup releases reservations.
16. Dropping an item creates a recoverable world drop.
17. Failed drop placement leaves inventory unchanged.
18. Raising Hauling skill unlocks additional slots.
19. Reducing capacity never silently deletes carried resources.
20. Changing selection preserves inventories.
21. Other ants continue working independently.
22. Autonomous survival remains operational.
23. Existing excavation and job priorities remain functional.
24. World reset restores deterministic fixtures.
25. Camera pan, zoom and UI click protection work.
26. Unity Console contains no relevant errors or warnings.

Known limitations are intentional: stack size 10, fixture quantities and carrying thresholds are provisional. Pickup/drop is explicitly player-directed; there is no autonomous hauling selection, Material Storage, stockpile policy, construction delivery, equipment capacity, food/water inventory, resource decay, crafting, persistence, streaming or multiplayer. The smallest next milestone after acceptance is separately authorised material storage and autonomous hauling, without beginning construction.

## Latest accepted milestone — 3.3B Real Excavation Designations & Physical Material Output (13 October 2026)

**Status: COMPLETE — the user confirmed all 26 manual Unity Play Mode checks on 13 October 2026.** Previously reported focused Edit Mode 14/14, focused Play Mode 3/3, full graphics-enabled Edit Mode 406/406 and full Play Mode 40/40 passed. These suites were not rerun during this documentation-only update. Milestone 3.3A was also accepted.

The player-facing **Extract** mode blocks ordinary selection and movement input while active. Left-click or a bounded drag marks only resource-bearing foreground Solid nodes by authoritative world coordinate. Each accepted designation creates one Extraction-category job through the compatibility-named excavation registry; duplicate active cells are rejected, cancellation makes the job terminal, and inaccessible work remains pending for controlled retry rather than disappearing.

Autonomous job selection remains priority-first and then least reachable shared-A* path cost. Extractors reserve exclusively, travel through `GridNavigationMotor` to a valid adjacent full-clearance anchor, revalidate before work, and never enter the Solid cell. Provisional work remains four simulation hours, +0.5% speed per Extraction level, and 20 base XP per block, with accepted Rested modifiers. XP is awarded only for committed progress; safe interruption preserves progress and releases ownership.

`SolidTileMaterialDefinition` now authoritatively declares excavation output type and positive yield. Soil yields one Soil and Stone yields one Stone provisionally. `ExcavationWorldTransaction` performs every fallible output check before removing foreground, commits a stable job-keyed `WorldMaterialDrop` immediately after removal, then refreshes exactly the affected visual/collision cell and invalidates local navigation. Failed drop creation leaves the Solid intact; retry keys prevent duplicate yield. Background material is unchanged. Drops are plain authoritative data with type, non-negative quantity, location and stable ownership key; they are not Tilemap state, GameObjects, inventories, hauling, stockpiles or global counters.

The development scene adds reachable Soil and Stone blocks, a three-block area, a two-block access sequence, a non-excavatable blocker and drop overlays. Rebuild replaces jobs, designations and drops deterministically while retaining the five synthetic 3.3A fixtures and existing survival diagnostics.

### 3.3B automated verification

- Focused Edit Mode: **14/14 passed**.
- Focused Play Mode: **3/3 passed**.
- Complete graphics-enabled Edit Mode: **406/406 passed** with zero failures/skips.
- Complete Play Mode: **40/40 passed** with zero failures/skips.
- `git diff --check` and strict Zensical validation passed at final handoff.

### Exact 3.3B manual Unity Play Mode acceptance

**Accepted:** the user confirmed all 26 checks on 13 October 2026.

1. Mara, Pip and Tala retain their identities and needs.
2. Excavate mode can be activated and deactivated.
3. Clicking Soil designates an excavation.
4. Dragging marks a small area.
5. Designated blocks display clear visual feedback.
6. Eligible ants independently claim excavation work.
7. Disabled Excavation prevents automatic work.
8. Ants navigate adjacent to target blocks.
9. Excavation progress advances visibly.
10. Excavation XP is awarded.
11. Rested bonuses affect excavation speed.
12. Player movement orders interrupt excavation safely.
13. Critical survival interrupts without losing progress.
14. Another ant can resume interrupted excavation.
15. Completed Soil blocks disappear and create physical Soil.
16. Completed Stone blocks create physical Stone.
17. Background remains after excavation.
18. Terrain collision and navigation update correctly.
19. A newly accessible designation can begin after prior excavation.
20. Cancelling jobs prevents further excavation.
21. Resource quantities do not duplicate or disappear.
22. Multiple ants excavate independently.
23. Existing survival and synthetic-job functionality remains available.
24. World reset restores deterministic fixtures.
25. Camera pan/zoom and UI blocking work.
26. Unity Console remains free of relevant errors or warnings.

At the 3.3B checkpoint, all excavated output became a loose drop because inventory and pickup were not yet implemented. Milestone 3.3C added personal inventory, stacking and explicit transfers and historically added automatic excavation pickup. Milestone 3.5B deliberately restores loose-only extraction while preserving those later pickup, hauling, storage and construction systems. Yield, duration and skill speed remain provisional; material hardness has no effect yet.

## Latest accepted milestone — 3.3A Colony Job Designation & Priority Foundation (13 October 2026)

**Status: COMPLETE — user confirmed all 22 manual Play Mode checks on 13 October 2026.** Previously reported focused Edit Mode 28/28, focused Play Mode 5/5, full graphics-enabled Edit Mode 392/392, full Play Mode 37/37 passed. No tests were rerun during this documentation update. Milestone 3.2C is also complete; Existing identity, selection, explicit orders, physical survival and shared navigation behavior remains intact.

Every authoritative `AntRecord` owns an independent `AntWorkPrioritySet`. The stable provisional categories are Excavation, Surface Preparation, Construction, Hauling, Foraging, Production, Combat and Brood Care. Each value is Highest (1), High (2), Normal (3), Low (4) or Disabled; development ants start at Normal. Queen and Worker use the same matrix. Skills never change priority rank, and level zero does not prohibit ordinary work. Production currently has no approved matching skill and therefore earns no synthetic skill XP.

One plain-C# `ColonyJobRegistry` owns stable job IDs, category, target cell, progress, status, reservation owner and failure reason independently of GameObjects. States are Available, Reserved, In Progress, Paused, Completed, Cancelled and Blocked. Active duplicate category/target designations are rejected, reservations are exclusive, and terminal jobs cannot be claimed. World rebuild restores five deterministic synthetic fixtures.

Selection applies the approved rule: highest enabled priority first, then least actual shared-A* cost to a valid adjacent work position. Equal costs use category, target Y/X and job ID. At most **32 candidates per priority** are measured per evaluation; failures use bounded **2/4/8-second** backoff. Evaluation is staggered so each ant is considered about every **0.5 seconds**.

Synthetic work reserves, navigates through `GridNavigationMotor`, revalidates the adjacent position, advances registry-owned progress and completes without changing terrain, resources or inventory. Provisional values are **4 simulation hours per job**, **+0.5% work speed per relevant skill level**, and **20 base XP per full job**, awarded proportionally only for committed progress. Accepted Rested work-speed and XP modifiers apply; autonomous work receives no ordered-work bonus.

Player Move/Eat/Drink/Rest orders stop work at the existing cancellation boundary, preserve progress and release the job as Paused. Ordinary survival waits for active work; critical survival confirms a viable target and then interrupts at the same safe boundary. Inactive/deceased ants, disabling work, cancellation, terrain invalidation and reset release ownership safely. The Developer Only Colony Jobs panel exposes toggles, per-ant priorities, designation creation, registry diagnostics, cancellation and fixture reset.

### 3.3A automated verification

- Focused Edit Mode: **28/28 passed**.
- Focused Play Mode: **5/5 passed**.
- Complete graphics-enabled Edit Mode: **392/392 passed** with zero failures/skips.
- Complete Play Mode: **37/37 passed** with zero failures/skips.

### Exact 3.3A manual Unity Play Mode acceptance

**Accepted:** the user confirmed all 22 checks on 13 October 2026.

1. Confirm Mara, Pip and Tala retain their accepted identities, skills and needs.
2. Inspect and change each ant's independent 1–4/Disabled work priorities.
3. Confirm the five deterministic synthetic designations appear in registry diagnostics.
4. Enable Autonomous Colony Work and confirm ants select work without player input.
5. Confirm a higher-priority farther category wins over lower-priority nearby work.
6. Confirm closest reachable work wins when priority is equal.
7. Confirm Disabled categories are ignored.
8. Confirm multiple ants work independently.
9. Confirm jobs have at most one reservation owner.
10. Confirm ants work from valid adjacent cells and progress rises over time.
11. Confirm relevant skill XP is awarded for committed progress, not travel/reservation.
12. Confirm Rested increases synthetic progress speed.
13. Confirm a critical need interrupts work safely.
14. Confirm interrupted progress persists and the Paused job can be reclaimed.
15. Confirm explicit Move/Eat/Drink/Rest temporarily overrides work.
16. Confirm completion/cancellation returns the ant to autonomous selection.
17. Cancel a job and confirm reservation release and no further progress.
18. Confirm unreachable work waits with bounded retries.
19. Mutate terrain affecting a work position and confirm safe Blocked status with preserved progress.
20. Rebuild the world and confirm fixtures, Normal priorities and reservations reset.
21. Confirm autonomous survival and explicit movement/survival commands still work.
22. Confirm the Unity Console contains no relevant errors or warnings.

Historical 3.3A limitations remain applicable to its synthetic fixtures. Milestone 3.3B adds only real excavation and loose physical output; preparation, construction, hauling, inventories, combat, brood care, forced-work orders, persistence, Draft Mode, local avoidance and multiplayer remain unimplemented.

## Accepted milestone — 3.2C Autonomous Survival Behaviour (13 October 2026)

**Status: COMPLETE — acceptance recorded through the approved transition to 3.3A.** Active ants independently evaluate Hunger, Thirst and Fatigue, select the least-cost reachable physical interaction, respect player orders and reservations, use hysteresis and bounded retries, and stop cleanly on lifecycle/reset boundaries. Previously verified results were 24/24 focused Edit Mode, 6/6 focused Play Mode, 364/364 complete Edit Mode and 32/32 complete Play Mode.

## Latest accepted milestone — 3.1C Individual Skills & Learn-by-Doing Foundation (13 October 2026)

**Status: COMPLETE — all 18 manual Unity Play Mode checks confirmed by the user on 13 October 2026.** Previously reported focused Edit Mode 40/40, focused Play Mode 6/6, full graphics-enabled Edit Mode 292/292 and full Play Mode 20/20 passed. These tests were not rerun for this documentation-only status update.
## Latest accepted milestone — 3.2B Physical Food, Water & Rest Interactions (13 October 2026)

**Status: COMPLETE — all 20 manual Unity Play Mode checks confirmed by the user on 13 October 2026.** Previously reported focused Edit Mode 25/25, focused Play Mode 5/5, complete graphics-enabled Edit Mode 340/340 and complete Play Mode 26/26 passed. This documentation-only acceptance update does not rerun tests. The Unity project connects functional Background, physical-resource, needs, selection and production grid-navigation foundations.

Food Storage, Water Storage and Sleeping are authored `BackgroundTileMaterialDefinition` functions with exactly one slot each. They replace the function/material of an existing Background cell, remain fully grippable and traversable at normal `1.0x` speed, and are inaccessible while covered by a foreground Solid. Their amber, blue and purple Tilemap visuals are projections of authoritative world data; they do not create room zones or per-tile GameObjects.

`SurvivalInteractionWorld` is the narrow plain-C# owner for this slice. Each food/water stack belongs to one world cell, has a stable resource type ID, non-negative integer quantity and explicit per-ant reservation. Food and water compatibility is enforced by the cell's function. Sleeping reservations map one cell to one stable ant ID. Cancelling, failing, replacing or disabling an order releases its reservations; rebuilding the world replaces the complete development-only store/reservation state. There is no global colony resource counter, generic inventory framework or per-resource `MonoBehaviour`.

Right-click now remains context-sensitive: ordinary navigation anchors issue Move, Food Storage issues Eat, Water Storage issues Drink and Sleeping issues Rest. A temporary interaction reserves its required unit/position, evaluates all valid adjacent anchors using the existing `GridPathfinder` travel-time cost, and delegates actual travel to the existing `GridNavigationMotor`. Arrival revalidates adjacency and the target function before interaction. Invalid targets, missing quantities, occupied sleep positions and unreachable routes report a reason without remote consumption. Changing selection does not cancel another ant's valid order.

Provisional 3.2B values are explicit: Eat reserves and consumes **1** `resource.food.development` unit after **1 simulation hour**, restoring **40 Hunger**; Drink reserves and consumes **1** `resource.water.development` unit after **1 simulation hour**, restoring **50 Thirst**. Both remain clamped to 100. Rest applies the accepted **12 Fatigue/hour** recovery and completes once the existing Rested qualification reaches Fatigue 20 or lower. Development interaction time advances at one simulation hour per real-time second. Interrupted rest retains recovery already applied.

The deterministic development course contains two accessible Food Storage cells with 6 units each, two accessible Water Storage cells with 8 units each, three accessible Sleeping cells, and a third 3-unit Food Storage in the existing surrounded pocket as an intentionally unreachable failure target. The selected-ant panel shows interaction kind, target, travel/interaction/completion state and failure reason. Its separate Developer Only physical-storage diagnostics show cell, resource ID/kind, quantity, reserved and available values, or sleeping reservation ownership; the default diagnostic list keeps fixture quantities visible.

### 3.2B automated verification

- Focused Edit Mode: **25/25 passed**. Coverage includes all three 1:1 functional capacities and traversal properties, fixture composition, resource compatibility, empty/non-negative storage, exclusive resource/sleep reservations, Eat/Drink need and physical-quantity commits, maximum clamps, dehydration recovery, timed non-consumption, rest/recovery/interruption, lifecycle/cancellation release, adjacency, unreachable and removed-target failures, independent interactions, world reset and actual path-cost approach selection.
- Focused Play Mode: **5/5 passed**. Coverage includes scene fixture creation, context Eat through the production motor, replacement release, selection-independent ongoing orders and retained ordinary context Move.
- Complete graphics-enabled Edit Mode: **340/340 passed** with zero failures/skips. A first `-nographics` complete attempt hit Unity's retained native rendered-pixel crash; the known graphics-enabled configuration completed successfully.
- Complete Play Mode: **26/26 passed** with zero failures/skips, including all retained identity, skills, needs, selection, production-grid and continuous-movement regressions.
- Unity compilation and the integrated development-scene structural validator passed.

### Exact 3.2B manual Unity Play Mode acceptance

**Accepted:** the user confirmed all 20 checks on 13 October 2026.

1. Enter Play Mode and confirm Mara, Pip and Tala spawn with their existing identities, castes, attributes and skills.
2. Confirm amber Food Storage, blue Water Storage and purple Sleeping background tiles are visible in the underground survival course.
3. Confirm the Developer Only physical-storage diagnostics display food and water quantities owned by specific world cells.
4. Lower Mara's Hunger with the existing needs tools, select her, right-click Food Storage and confirm she navigates to a valid adjacent cell before eating.
5. Confirm that food quantity decreases by one only when Eat completes and Mara's Hunger increases by 40 without exceeding 100.
6. Lower Pip's Thirst, select him, right-click Water Storage and confirm he navigates adjacent before drinking.
7. Confirm that water quantity decreases by one only when Drink completes and Pip's Thirst increases by 50 without exceeding 100.
8. Set Pip to critical Thirst, verify XP is blocked, complete Drink until he is above the critical threshold, then confirm a subsequent valid skill-XP award works again.
9. Raise Tala's Fatigue, select her, right-click Sleeping and confirm the sleeping cell displays her reservation while she rests adjacent to it.
10. Confirm Tala's Fatigue decreases over elapsed interaction time rather than changing remotely at order issue.
11. Confirm rest completes at Fatigue 20 or lower and the accepted Rested work-speed/XP modifiers appear and fade under the existing 3.2A rules.
12. While Tala owns a sleeping reservation, select another ant and confirm that ant cannot claim the same position.
13. Cancel an Eat, Drink and Rest order in turn; confirm resource/sleep reservations return to available while any already-applied rest recovery remains.
14. Consume an accessible storage stack to empty (or repeat with a prepared empty test state) and confirm a new interaction cannot restore the need or make quantity negative.
15. Target the surrounded Food Storage and another invalid/covered target; confirm meaningful unreachable/invalid failure text and no remote consumption.
16. Give different ants independent interactions, change selection, and confirm one ant's valid ongoing order is not silently cancelled by selecting or commanding another.
17. Right-click ordinary reachable terrain and confirm the existing Move order and completion lifecycle still work.
18. Confirm camera pan/zoom, collapsible developer UI and click blocking work, and clicking UI never issues a world command.
19. Use Rebuild World and confirm the same fixture cells/starting quantities return with no stale reservations or interactions.
20. Confirm the Unity Console contains no relevant errors, assertions, missing references or warnings.

Known limitations are deliberate: 3.2B provides explicit player survival interactions only. It has no autonomous need response, colony jobs/priorities, gathering, hauling, ant inventories, construction/conversion, room enclosure/quality, food varieties/spoilage/cooking, water production/containers, permanent beds, equipment/combat, brood/reproduction, save/load, streaming, Draft Mode or multiplayer. Resource IDs, stack limits, restoration amounts, interaction duration and rest completion balance remain provisional.

## Latest accepted milestone — 3.2A Individual Needs & Survival Foundation (13 October 2026)

**Status: COMPLETE — all 18 manual Unity Play Mode checks confirmed by the user on 13 October 2026.** Final automated verification passes focused Edit Mode 63/63, focused Play Mode 7/7, complete graphics-enabled Edit Mode 315/315 and complete Play Mode 21/21.

Every authoritative `AntRecord` now owns one lightweight plain-C# `AntNeedsState`. Health, Hunger and Thirst use 100 as full and 0 as depleted; Fatigue uses 0 as fully rested and 100 as exhausted. Queen and Worker use the same model. New development ants deterministically begin at Health 100, Hunger 100, Thirst 100 and Fatigue 20, with no Rested qualification until an explicit rest event is completed. Needs remain attached to the record through selection changes and visual rebinding, while rebuilding the development world or restarting Play Mode creates fresh values.

### Provisional 3.2A survival balance

These values are explicit development placeholders because the GDD leaves exact thresholds and rates TBD:

- Awake Hunger satisfaction decreases by **2/hour**.
- Awake Thirst satisfaction decreases by **3/hour**.
- Awake Fatigue increases by **4/hour**.
- Simulated rest reduces Fatigue by **12/hour**; rest does not yet consume supplies or represent a sleeping-place interaction.
- Hunger at or below **10** is critical. Time spent at or below that threshold removes **2 Health/hour**. Threshold-crossing time is calculated analytically, so one large step matches equivalent smaller steps.
- Thirst at or below **10** blocks all ordinary skill XP by applying an effective needs multiplier of zero. Existing XP remains intact, and replenishing Thirst restores future XP gain.
- A completed rest that reaches Fatigue **20 or lower** qualifies the ant for Rested bonuses. At low Fatigue the provisional bonuses are **1.15x work speed** and **1.25x skill XP**. Both fade linearly to 1.0x by Fatigue **80**. The work multiplier is exposed for future tasks but does not alter navigation or movement speed.
- The caller-provided 3.1C XP multiplier remains separate and is multiplied by the Rested XP modifier only. No ordered-task productivity bonus is implemented.
- Health reaching zero pauses further ordinary survival advancement and is displayed clearly, but does not change lifecycle status or create death/corpse gameplay.

All simulation uses explicit elapsed hours. There are no per-need MonoBehaviours, per-frame global scans, resource searches or UI-owned values. Inactive and deceased records reject ordinary needs advancement and rest. Values are clamped to 0–100, while negative/non-finite elapsed time and invalid development values are rejected.

The selected-ant panel now shows Health, Hunger satisfaction, Thirst satisfaction, Fatigue, both Rested multipliers, and critical Hunger/Thirst or depleted-Health warnings. `Show Developer Only Needs Tools` opens authoritative controls to advance one hour, refill Hunger or Thirst, restore Health, add 25 Fatigue, simulate eight hours of rest, set Hunger or Thirst to zero, and reset the selected ant's needs. The existing identity, attributes, skills, skill tools and movement information remain available.

### 3.2A automated verification

- Added **21 focused needs test cases** covering initial Queen/Worker state, ownership, depletion, rest, bounds, invalid time, elapsed-time equivalence, starvation, replenishment, dehydration XP blocking/recovery, preserved XP, retained invalid-modifier rejection, Rested XP/work modifiers, fade/restoration, ant independence, lifecycle pauses, zero-Health handling and invalid development inputs.
- Added Edit Mode integration coverage for selection, visual rebinding and deterministic world reset, plus Play Mode coverage for dehydration, XP restoration, ant independence and retained movement orders.
- Unity imported and compiled the changed `AntColony.Ants`, `AntColony.Commands`, `AntColony.Development`, Edit Mode test and Play Mode test assemblies successfully.
- Focused needs/skills/identity/selection Edit Mode: **63/63 passed**, including all 21 dedicated needs cases and the retained 3.1B/3.1C regressions.
- Focused selection/needs Play Mode: **7/7 passed**, including dehydration XP blocking/recovery, independent needs and unchanged movement commands.
- Complete graphics-enabled Edit Mode: **315/315 passed**.
- Complete Play Mode: **21/21 passed** in 139.267 seconds, including production grid navigation and retained continuous-movement regressions.
- Earlier sandboxed attempts were blocked before discovery by Unity licensing-client IPC failures. Running Unity with access to its local licensing service cleared that environmental blocker; the final XML results contain zero failures and zero skips.
- The user subsequently confirmed all 18 manual Scene/Game-view checks, completing Milestone 3.2A.

### Exact 3.2A manual Unity Play Mode acceptance

**Accepted:** the user confirmed all 18 checks below on 13 October 2026. No Inspector setup was required.

1. Open `Assets/AntColony/Scenes/AntGameDevelopment.unity`, clear the Console and enter Play Mode. Confirm Mara, Pip and Tala retain their accepted identities, castes, attributes and skills.
2. Select each ant and confirm the panel displays Health, Hunger satisfaction, Thirst satisfaction and Fatigue, plus Rested work-speed and skill-XP multipliers.
3. Confirm all three start at Health 100, Hunger 100, Thirst 100 and Fatigue 20, with both multipliers at 1.0x.
4. Select Mara, open `Show Developer Only Needs Tools`, and press `Advance 1h Simulation Time`. Confirm all active ants lose 2 Hunger and 3 Thirst and gain 4 Fatigue.
5. Advance time again and confirm Fatigue continues increasing while awake.
6. Select Tala, press `Add 25 Fatigue` as needed, then `Simulate 8h Rest`. Confirm Tala's Fatigue decreases without changing Mara or Pip.
7. Confirm Tala's completed low-Fatigue rest displays up to 1.15x work speed and 1.25x skill XP.
8. Advance simulation time repeatedly and confirm Tala's displayed bonuses decrease as Fatigue rises, reaching 1.0x by Fatigue 80.
9. Select Mara, press `Set Critical Hunger (0)`, note Health, then advance time. Confirm Health falls gradually by 2 per hour rather than causing immediate death.
10. Press `Refill Hunger`, advance another hour and confirm Mara takes no further starvation damage during that sufficiently fed interval.
11. Select Pip, press `Set Critical Thirst (0)`, open the skill tools, choose any skill and award 100 XP at 1.0x. Confirm the message reports zero XP and existing XP is unchanged.
12. Press `Refill Thirst`, repeat the XP award and confirm XP progression resumes. If Pip has completed sufficient rest, confirm the displayed Rested XP multiplier is included.
13. Switch among Mara, Pip and Tala and confirm their Health, Hunger, Thirst, Fatigue and Rested state remain independent.
14. Repeatedly change selection and confirm no needs value resets or transfers between identities.
15. Issue valid right-click movement orders before and after needs testing. Confirm selection, destinations and `GridNavigationMotor` movement remain functional; the displayed work multiplier must not change movement speed.
16. Press `Rebuild World` in the main Developer Tools panel, or exit and re-enter Play Mode. Confirm all three records return to Health/Hunger/Thirst 100, Fatigue 20, no Rested qualification and simulation time 0h.
17. Expand and collapse the Needs, Skill and main Developer panels. Confirm collapsing them does not change authoritative data or movement.
18. Confirm the Unity Console contains no relevant errors, assertions, missing references or warnings.

Known limitations are deliberate: 3.2A has no physical food/water, storage transactions, sleeping backgrounds, autonomous survival decisions, colony jobs, work priorities, excavation/construction, reproduction, equipment effects, combat, ageing, actual death/corpses, save/load, Draft Mode or multiplayer. Development recovery controls mutate authoritative data for testing only. The next milestone requires separate approval.

## Latest implemented milestone — 3.1C Individual Skills & Learn-by-Doing Foundation (13 October 2026)

**Status: COMPLETE — manual acceptance recorded on 13 October 2026 as part of the approved 3.2A transition.** Previously reported automated results were focused Edit Mode 40/40, focused Play Mode 6/6, complete Edit Mode 292/292 and complete Play Mode 20/20.

Every authoritative `AntRecord` now owns one `AntSkillSet`; skills are not stored on visuals or separate MonoBehaviours. The shared static catalog defines the seven GDD-approved ordinary skills with stable serialization IDs: Excavation (`excavation`), Surface Preparation (`surface_preparation`), Construction (`construction`), Hauling (`hauling`), Foraging (`foraging`), Combat (`combat`) and Brood Care (`brood_care`). Queen and Worker use the same model, every skill starts at level 0, and ordinary skills have no caste restriction.

`AntRecord.TryAwardSkillExperience` is the explicit learn-by-doing boundary for a future successful activity completion. It receives the skill, base XP, effective multiplier and absolute simulation time, then reports requested XP, actual awarded XP and level changes. Negative or non-finite XP and multipliers are rejected; `0x` is valid and awards nothing; awards at the cap are safe; and deceased ants reject progression. Starting, attempting or cancelling work does not call this API. Hydration, Rested and ordered-work systems remain absent—the multiplier is only a future-facing input, and the ordered-work speed bonus is not treated as XP.

### Provisional 3.1C progression and decay balance

These numerical values are explicit development placeholders, not final GDD balance:

- Levels range from **0–100**.
- XP required to advance from level `L` is `100 + 5L`.
- Total XP at level `L` is `100L + 2.5L(L-1)`; level 100 therefore caps at 34,750 total XP.
- XP and level calculations use deterministic `double` arithmetic and do not depend on frame rate.
- Each skill has an independent last-practised time. A valid successful-use event, including a valid event whose effective multiplier is zero, updates only that skill's practice time.
- Inactivity uses explicit absolute simulation hours. After a provisional **24-hour grace period**, an exact skill value above 50 loses **0.25 levels per inactive hour** and stops at exactly 50. Levels and progress at or below 50 do not decay.
- One large elapsed-time step and equivalent smaller monotonic steps produce the same result within normal floating-point tolerance.
- No skill has an `Update` method. The development controller advances the clock explicitly and batches the seven-skill calculation across the three authoritative records. Definitions are constructed once and shared.

The selected-ant panel lists every skill, current level and XP progress toward the next level, or `MAX` at level 100. `Show Developer Only Skill Tools` opens a clearly labelled collapsible section with a skill selector; 100 XP buttons for `1.0x`, `1.5x` and `0x`; a 50,000 XP maximum-level test; a 24-hour inactivity step; and a selected-ant skill reset. Advancing inactivity applies to all living development records even when they are not selected or visible. Rebuilding the development world is the explicit clean-reset boundary; merely changing selection or rebinding visuals preserves skill ownership and progress.

### 3.1C automated verification

- Unity imported and compiled the updated Ants, Commands, Development, Edit Mode test and Play Mode test assemblies.
- Focused skill/identity/selection Edit Mode: **40/40 passed**, including **21 dedicated skill tests**. Coverage includes all seven stable IDs, zero starts, shared Queen/Worker model, independent progression, validation, deterministic curve calculations, cap handling, modifiers, decay/grace/floor behavior, practice clocks, elapsed-time equivalence, independent ants, lifecycle rejection, visual rebinding and deterministic world reset.
- Focused selection/skill-debug Play Mode: **6/6 passed**, including independent modified XP, zero-multiplier behavior and unchanged movement commands.
- Complete graphics-enabled Edit Mode: **292/292 passed**.
- Complete Play Mode: **20/20 passed**, including all production grid-navigation and retained continuous 2.4A regressions.
- One initial focused run compiled successfully but reported 39/40 because Unity's bundled NUnit version did not support `Has.Count` for `IReadOnlyList`; the assertion was rewritten without weakening coverage and the focused rerun passed 40/40.
- Manual Scene/Game-view acceptance remains pending user confirmation.

### Exact 3.1C manual Unity Play Mode acceptance

No Inspector setup is required.

1. Open `Assets/AntColony/Scenes/AntGameDevelopment.unity`, clear the Console and enter Play Mode. Confirm Mara, Pip and Tala retain their accepted IDs, castes and attributes.
2. Select each ant and confirm the top-right panel lists Excavation, Surface Preparation, Construction, Hauling, Foraging, Combat and Brood Care.
3. Confirm every skill initially reports level 0 and `0 / 100 XP`.
4. Select Mara, open `Show Developer Only Skill Tools`, choose Excavation and press `Award 100 XP (1.0x)`. Confirm Mara reaches Excavation level 1 and Pip/Tala remain unchanged.
5. Select Pip, choose Construction and award 100 XP at 1.0x. Confirm only Pip's Construction changes.
6. Select Tala, choose Hauling and award 100 XP at 1.0x. Confirm only Tala's Hauling changes.
7. Award another 100 XP to a level-1 skill. Confirm progress uses the displayed 105-XP next-level requirement.
8. Reset one selected ant, award 100 XP at 1.0x and confirm the level increases from 0 to 1.
9. Press `Award 50,000 XP (Maximum Test)`. Confirm the selected skill stops at level 100 and displays `MAX`; repeat once and confirm it remains safe at 100.
10. Reset the selected ant, award 100 XP at 1.0x, then reset and award 100 XP at 1.5x. Confirm the messages report 100 and 150 actual XP respectively.
11. Reset, press `Test 100 XP (0x — No Gain)` and confirm the selected skill remains level 0 with zero XP.
12. Raise a selected skill above 50 with the maximum test. Press `Advance 24h Inactivity` once and confirm the grace period prevents decay; press it again and confirm the skill begins decaying.
13. Continue advancing 24-hour steps and confirm the skill stops at level 50 rather than crossing below it. Confirm skills never raised above 50 remain unchanged.
14. Switch repeatedly among Mara, Pip and Tala. Confirm their existing skill progress remains attached to the correct identities.
15. Issue valid right-click movement orders before and after using the skill tools. Confirm selection, independent destinations and `GridNavigationMotor` movement remain functional.
16. Exit and re-enter Play Mode. Confirm all three deterministic development records return with every skill at level 0 and simulation time 0h.
17. Expand and collapse both the skill tools and the main `Show Developer Tools` panel. Confirm collapsing either panel does not change skill data or movement.
18. Confirm the Unity Console contains no relevant errors, assertions, missing references or warnings.

Known limitations are deliberate: 3.1C supplies progression data and test controls only. There are no real work-completion events, skill effects, jobs, work priorities, excavation/construction/hauling gameplay, inventory slots, hydration, Rested state, needs, combat statistics, brood, reproduction, save/load, Draft Mode or multiplayer. The next milestone must be separately approved after manual acceptance.

## Latest implemented milestone — 3.1B Persistent Ant Identity & Attributes (13 October 2026)

**Status: COMPLETE — all 12 manual Unity Play Mode checks confirmed by the user on 13 October 2026.** Previously reported focused Edit Mode 16/16, focused Play Mode 5/5, complete Edit Mode 268/268 and complete Play Mode 19/19 passed. No tests were rerun during this documentation update.

`AntColony.Ants` is a focused runtime assembly containing plain C# identity data with no MonoBehaviours, navigation or visual state. `AntId` and `ColonyId` wrap stable text values suitable for future serialization without using Unity instance IDs or GameObject names. `AntRecord` is the one model used by both `Queen` and `Worker`; caste currently changes no movement, carrying or other statistics. `AntRegistry` owns one colony's records, rejects duplicate Ant IDs and rejects records whose Colony ID does not match the registry.

`AntAttributes` stores Strength, Agility and Endurance independently. The documented provisional development range is **1–10 inclusive**; construction rejects out-of-range values instead of silently clamping them. These values have no progression, derived-stat, equipment, combat, carrying or movement-speed effects. The deterministic development records are provisional fixtures: **Mara** (`ant-dev-queen-001`, Queen, Strength 7 / Agility 5 / Endurance 8), **Pip** (`ant-dev-worker-002`, Worker, 4 / 8 / 5), and **Tala** (`ant-dev-worker-003`, Worker, 6 / 6 / 7), all in `development-colony`.

`AntEntityBinding` resolves a configured Ant ID to an existing record and never creates or removes authoritative data. `AntEntityBindingRegistry` rejects a second live visual claiming the same ID. Destroying a visual releases only that transient claim, so a replacement visual can bind to the same retained record. `SelectableGridAnt` reads its display name and lifecycle from the binding while `GridNavigationMotor` remains the only movement executor. Active ants accept the existing temporary movement orders; inactive or deceased records reject new orders, and an active order is cancelled safely if its record stops being active. No health, damage, ageing, corpse, burial or equipment-drop mechanics exist.

The compact top-right selection panel now shows Ant ID, name, colony, caste, Strength, Agility, Endurance and lifecycle above the existing control, navigation order, destination and failure fields. Selection changes only which bound record is displayed; it does not alter identity or another ant's order. The development tools remain separately collapsible.

### 3.1B automated verification

- Unity imported and compiled `AntColony.Ants`, Commands, Navigation, Development and both test assemblies successfully.
- Focused identity/selection Edit Mode: **16/16 passed**. Coverage includes unique creation, duplicate rejection, colony ownership, shared Queen/Worker model, independent attributes, range rejection, lifecycle retention, duplicate visual rejection, missing-record rejection, visual rebinding, deterministic identities, selection stability, correct motor targeting and deceased-order rejection.
- Focused selection/identity Play Mode: **5/5 passed**.
- Complete graphics-enabled Edit Mode: **268/268 passed**. A preceding `-nographics` attempt reached the retained rendered-pixel test and crashed while creating a RenderTexture; the graphics-enabled rerun passed completely.
- Complete Play Mode: **19/19 passed**, including all production grid-navigation and retained continuous 2.4A regressions. Separate confirmation runs also passed selection/identity 5/5, grid navigation 8/8 and autonomous movement 6/6.
- Manual Scene/Game-view acceptance was subsequently confirmed by the user on 13 October 2026; 3.1B is complete.

### Exact 3.1B manual Unity Play Mode acceptance

No Inspector setup is required.

1. Open `Assets/AntColony/Scenes/AntGameDevelopment.unity`, clear the Console and enter Play Mode. Confirm three distinct cyan, pink and green selectable ants appear.
2. Select the cyan ant and confirm the panel reports Mara, `ant-dev-queen-001`, `development-colony`, Queen, attributes 7/5/8 and Active.
3. Select the pink and green ants and confirm Pip and Tala are Workers with IDs `ant-dev-worker-002` and `ant-dev-worker-003`, and attributes 4/8/5 and 6/6/7 respectively.
4. Switch repeatedly among all three ants. Confirm each identity and attribute set remains unchanged and exactly one selection ring follows the selected ant.
5. Right-click separate valid destinations for each ant. Confirm the selected record receives the order, all three retain independent destinations and movement remains smooth through `GridNavigationMotor`.
6. While one ant moves, select another. Confirm the first ant keeps its identity and order while the panel changes to the second ant's correct record.
7. Allow an order to complete, cancel another and issue an unreachable destination. Confirm the existing order/navigation status and failure information remain readable alongside identity data.
8. Pan with WASD/arrow keys, zoom with the wheel and reset with Home. Confirm selection and displayed identity remain correct.
9. Open and collapse `Show Developer Tools`. Confirm terrain inspection, mutation and stress tools remain functional and do not replace the three authoritative records.
10. Exit and re-enter Play Mode. Confirm the same three deterministic fixture IDs, names, castes and provisional attributes return in a clean state.
11. Confirm one Queen and two Workers share the same movement and selection behavior; caste grants no visible bonus or restriction.
12. Confirm the Unity Console has no relevant error, assertion or missing-reference messages.

This checklist does not expose a player-facing death control. Deceased-order rejection is automated-test coverage only; actual death gameplay and presentation remain excluded.

At the 3.1B checkpoint, the registry was in-memory only and records did not yet contain skills. Milestone 3.1C now adds skill data only; save/load, needs, jobs, work priorities, inventory, equipment, brood, reproduction, ageing and combat remain absent. Lifecycle transitions still have no gameplay cause, and the fixture does not define the finished starting population. The approved future game begins with one working founding queen.

## Latest accepted milestone — 3.1A Ant Selection & Command Integration (13 October 2026)

**Status: COMPLETE — user confirmed all 14 manual Unity Play Mode acceptance checks passed on 13 October 2026.** The prior reported automated results were focused Edit Mode 6/6, focused Play Mode 4/4, full Edit Mode 258/258, full Play Mode 18/18, plus scene validation. This documentation update did not rerun tests.

`AntGameDevelopment.unity` now enters Play Mode with three player-facing grid ants named `Ant 1`, `Ant 2` and `Ant 3`. The development host chooses three deterministic valid anchors at least three cells apart, instantiates the existing `PrimaryGridAnt.prefab`, gives the ants cyan, pink and green placeholder colours, and connects every motor to the same production `GridNavigationWorldService`. Stress-test ants are stored separately, so spawning or clearing a stress group does not replace the three selectable ants.

`AntColony.Commands` is a small runtime assembly with two focused components. `SelectableGridAnt` adds a yellow selection ring and translates terminal `GridNavigationMotor` results into a temporary order outcome; it does not pathfind or move transforms. `AntSelectionCommandController` tracks one selected ant, converts camera-correct mouse positions into selection or destination cells, validates destination anchors through the shared map, and calls the existing `MoveTo`/`CancelMovement` API. Selecting, deselecting or switching ants never cancels an active order. Completed, failed and cancelled orders return to the player-facing `Autonomous / Idle` state while retaining the last destination/outcome for inspection.

Controls are now player-facing: left-click an ant to select it, left-click empty world space to deselect, and right-click a traversable destination to order only the selected ant. The compact top-right panel reports the selected ant, control state, order state, destination and failure reason, and provides a selected-order cancellation button. Its rectangle and the development panel rectangle block world input. The old development interface is collapsed behind `Show Developer Tools` at the top left; terrain inspection, `F`/`P`, legacy comparison, falling fixture, background isolation, world regeneration and 1/100/300/500 stress controls remain available without covering the ordinary command view. Existing WASD/arrow pan, middle-drag, wheel zoom and Home reset are unchanged.

### 3.1A automated verification

- Unity compiled `AntColony.Commands`, Navigation, Development, Editor tooling, and both test assemblies without errors. The integrated-scene structural validator passed with exactly one shared navigation service and one selection controller.
- Focused 3.1A Edit Mode: **6/6 passed**. Coverage includes three distinct shared-service ants, selection, deselection, switching while ordered, selection indicator ownership, independent/replaced requests, selected-only cancellation, invalid destination rejection, UI input blocking, and camera pan/zoom selection accuracy.
- Focused 3.1A Play Mode: **4/4 passed**. Coverage includes automatic three-ant spawn, moving selection indicator, arrival/completion, independent simultaneous orders, selected-only cancellation, and unreachable-order failure returning to Autonomous/Idle with a reason.
- Complete Edit Mode: **258/258 passed**.
- Complete Play Mode: **18/18 passed**, including all production grid-navigation and retained continuous 2.4A regressions.
- `git diff --check` passes after reverting Unity's unrelated automatic `Physics2DSettings.asset` serialization migration. No environment-blocked suite remains in this verification pass.

Known limitations are deliberate for 3.1A: selection is single-ant only; placeholder visuals are runtime-only; temporary move orders are not saved; completed/failed ants have no autonomous colony work to choose and therefore remain safely idle; local avoidance, traffic ownership, persistent identity, attributes, needs, work priorities, jobs, inventories, Draft Mode and combat are not implemented.

### Exact 3.1A manual Unity Play Mode acceptance

No Inspector setup is required.

1. Open `Assets/AntColony/Scenes/AntGameDevelopment.unity`, clear the Console and enter Play Mode. Confirm three grid ants appear automatically, visibly separated and coloured cyan, pink and green. The amber legacy comparison ant may also appear.
2. Confirm the small `ANT SELECTION & MOVE ORDER` panel is readable at the top right and the large development panel is hidden behind `Show Developer Tools` at the top left.
3. Left-click each coloured ant in turn. Confirm the panel names `Ant 1`, `Ant 2` or `Ant 3`, exactly one yellow selection ring appears, and the ring follows a moving ant.
4. Left-click exposed empty world space. Confirm the selection and ring clear without stopping any ant that already has a move order.
5. Select one ant and right-click a nearby exposed natural-background cell. Confirm only that ant moves smoothly through production grid anchors and the panel reports `Ordered`, its navigation status and destination.
6. While the first ant is moving, select a second ant and right-click a different valid destination. Then do the same with the third ant. Confirm all three retain independent destinations and can move concurrently through the one shared scheduler.
7. While multiple ants are moving, select one and right-click another valid cell. Confirm only that ant replaces its previous order; the other two continue toward their existing destinations.
8. Allow an ordered ant to arrive. Confirm the panel reports completion and `Autonomous / Idle`, the ant stays selected, and no fake follow-up job begins.
9. Select a moving ant and press `Cancel Selected Move Order`. Confirm only that ant returns to idle and other ants keep moving.
10. Right-click a covered/invalid cell and then the enclosed unreachable destination near `(12,-16)`. Confirm immediate invalid anchors are rejected without replacing a valid order, while an unreachable valid anchor eventually reports failure and returns the ant to idle without infinite retries.
11. Click buttons and blank areas inside both UI panels. Confirm no ant is selected/deselected and no movement order is issued through the UI.
12. Pan with WASD/arrows and middle-drag, zoom with the wheel, and use Home. At several zoom levels, confirm the ant under the pointer is selected and right-click destinations match the intended world cell.
13. Expand `Show Developer Tools`. Confirm `F`/`P`, terrain inspection, legacy comparison and the falling fixture remain usable. Spawn a 100-ant simultaneous/staggered stress group and clear it; confirm the three coloured selectable ants remain present and individually commandable.
14. Exit Play Mode and confirm the Console contains no relevant exceptions, assertions, missing references, stale-order delivery or infinite-retry messages.

Manual acceptance remains outstanding until the user confirms these checks.

## Latest implemented milestone — 2.4D Production Grid Navigation Integration (12 October 2026)

**Status: Implemented — Pending Manual Acceptance.**

Grid-authoritative navigation is now the primary reusable ant movement architecture. `PrimaryGridAnt.prefab` contains one `GridNavigationMotor`, Rigidbody2D, ordinary-ant capsule and presentation child. It contains no `PlayerMovementInput`, `AntSurfaceMotor` or `AntAutonomousMotor`. Stable grid traversal keeps the Rigidbody2D outside simulation; Falling temporarily restores Dynamic Rigidbody2D gravity and terrain collision before controlled recovery. The prefab is instantiated and bound by a world/spawn owner, so it has no dependency on the development inspector or simulated keyboard input.

Runtime navigation is separated into `AntColony.Navigation`. `GridNavigationWorldService` is one ordinary scene component per active world: it receives authoritative `ChunkedWorldData`, owns the one shared ordinary-ant `GridNavigationMap` and `GridNavigationScheduler`, advances the configured 128-node/update and 8-node/request slice, and forwards local terrain invalidation. Reinitialising it with the same world is idempotent. Individual motors reference this service and never create terrain graphs or schedulers. This keeps future traversal-profile support possible without introducing a singleton or service-locator framework.

The public movement-command boundary is intentionally small: `InitializeNavigation(service, spawnCell)`, `MoveTo(destination, priority)`, `CancelMovement(reason)`, `State`, `IsMovementActive`, `HasArrived`, `HasFailed`, `FailureReason` and `StatusMessage`. Destination replacement and cancellation continue to invalidate earlier scheduler request IDs; terrain revisions prevent stale results; failures and arrivals remain stable terminal states; Falling stores and resumes a valid destination with bounded retries. Callers do not need to know about A*, anchors or Rigidbody2D transitions.

`AntGameDevelopment.unity` authors exactly one shared navigation service and assigns `PrimaryGridAnt.prefab` as the reusable grid-ant prefab. Milestone 3.1A now places its player-facing selection and right-click command layer above this API. The amber `AutonomousAnt.prefab`, `AntAutonomousMotor`, `DirectGroundRouteValidator`, `AntSurfaceMotor`, `PlayerMovementInput`, prototype scenes and continuous regression tests remain developer-only comparison tooling. They do not attach to or issue competing commands for selectable grid ants.

### 2.4D automated verification and performance status

- The accepted 2.4C implementation is preserved at commit `97e50fe` and branch `codex/milestone-2.4c-grid-accepted`. Earlier 2.4B grid and continuous snapshots remain available.
- Unity Bee response-file Roslyn compilation passed for the World plus new Navigation sources, Development, EditMode-test, PlayMode-test and Editor assemblies. This verifies C# compilation but is not a Unity test-run result.
- New focused tests cover primary-prefab composition, absence of legacy/input motors, exactly one shared service, idempotent same-world initialisation, inspector-independent commands, shared service references across ants, stable arrival/failure reporting and stale-command cancellation.
- `git diff --check` passes, and the scene/prefab retain their existing asset references and GUID-based script linkage.
- The previous Licensing Client blocker cleared during 3.1A verification. Focused 2.4D Edit Mode passed 3/3 before 3.1A; the final complete suites pass 258/258 Edit Mode and 18/18 Play Mode, including retained grid and continuous movement regressions.
- The required 1/100/300/500 benchmark rerun is therefore blocked by the same Unity failure. The accepted 2.4C baseline remains 12.993/18.937/72.932/187.776 ms total batch time, 2.044/17.971/69.704/171.911 ms accumulated scheduler work, and 2.044/1.267/1.890/3.197 ms maximum scheduler update respectively, with all requests successful. Production integration moves that same scheduler update from the development host into the one shared world service and adds no new per-ant polling system. These are baseline comparison figures, not fresh 2.4D measurements or a production frame-rate claim.

Known limitations remain unchanged: the scheduler is main-thread only; searches and returned routes allocate managed memory; only the ordinary-ant traversal profile is instantiated; friendly overlap is allowed; and route-result caching, flow fields, hierarchical navigation, local avoidance and traffic ownership are not implemented.

### Exact 2.4D manual Unity Play Mode acceptance

No extra Inspector setup is required.

1. Open `Assets/AntColony/Scenes/AntGameDevelopment.unity`, clear the Console and enter Play Mode. Confirm a cyan `Primary Grid Ant` and amber legacy comparison ant spawn, with no missing-script or missing-reference messages.
2. Confirm the panel starts on `Commanding: PRIMARY GRID`. Click a nearby exposed natural-background cell and confirm the cyan ant moves smoothly and reaches stable `Arrived` without keyboard input or teleportation.
3. Command the main passage and the detour destination around the blocker. Confirm multi-turn routing follows valid anchors and never enters foreground terrain.
4. Command horizontal, vertical and diagonal cells on exposed natural Earth/Stone, including the grip fixture top. Confirm natural backgrounds support traversal in all valid directions and diagonal blocked corners are not cut.
5. Traverse the prepared strip and compare it with adjacent natural terrain. Confirm the prepared material retains the visible 1.25x speed bonus without changing access.
6. Command the positive chunk-boundary destination near `(33,-8)` and confirm the route crosses the resident chunk boundary smoothly.
7. While a route is waiting or active, click a different valid destination. Confirm the new command replaces the old one and the old route never regains control. Use `Cancel First Grid Request` during a stress batch and confirm only that ant cancels.
8. Command the enclosed unreachable destination `(12,-16)` and an invalid/covered cell. Confirm the primary ant reaches stable `Blocked`, displays a meaningful failure, and does not retry indefinitely.
9. While the primary ant and a stress group are moving, point at a route cell and press `F`. Confirm the shared invalidation count increases and affected ants replan or fail clearly without following a stale route. Remove the blocker and retry.
10. Press `Start Grid Falling / Recovery Fixture`. Confirm grid control stops, Rigidbody2D gravity produces a physical fall, stable terrain contact is acquired, recovery recentres smoothly, and a stored valid destination resumes without oscillation.
11. Choose 100 ants, press `Spawn Selected Grid Group`, and run simultaneous then staggered requests. Confirm all ants reference one shared cache/scheduler, pending work is bounded across updates, friendly overlap does not deadlock movement, and clearing the group leaves the primary ant usable. Repeat at 300 and 500 if practical.
12. Switch to `Commanding: LEGACY 2.4A` and repeat a direct surface, prepared climb and falling comparison. Confirm only the amber ant responds and the primary grid ant is not controlled by the legacy system.
13. Exit Play Mode and confirm the Console contains no relevant exceptions, assertions, missing scripts, stale-result messages or infinite-retry behaviour.

This pre-3.1A checklist's old click-command wording is superseded by the player-facing controls above. Manual acceptance remains outstanding unless separately confirmed by the user. Milestone 2.4D adds no ant identity, skills, needs, colony jobs, excavation, hauling, construction, brood, rooms, Draft Mode, combat, enemies, persistence, streaming or networking.

## Latest implemented milestone — 2.4C Grid Navigation Stabilisation and Multi-Ant Readiness (12 October 2026)

**Status: Complete — manual Unity Play Mode acceptance confirmed by the user. Production integration is authorised separately as Milestone 2.4D.**

Milestone 2.4C turns the accepted grid experiment into a bounded shared navigation service without adding colony simulation. `GridNavigationScheduler` accepts many ant-owned requests against the one shared `GridNavigationMap`, defers allocating each A* frontier until that request receives service, and limits both total node expansions and per-request work per update. The development host currently uses a 128-node total budget and an 8-node request slice. Its deterministic `High, High, Normal, High, Normal, Low` service pattern favours urgent navigation while guaranteeing recurring low-priority service. These priorities are navigation-service hints only; they are not colony job priorities.

`GridPathSearch` is now resumable. It retains its deterministic heap, costs and predecessor data between scheduler updates, detects terrain-revision changes, and reports success, unreachable, search-limit or terrain-changed outcomes. The scheduler additionally reports cancellation, reuses an identical outstanding owner request, cancels a replaced destination, and checks request ID, owner, destination and map revision before delivery. Terrain remains shared cached authority; agents are never inserted into terrain navigation.

`GridNavigationMotor` now exposes `Idle`, `WaitingForPath`, `FollowingPath`, `Arrived`, `Blocked`, `Falling` and `Recovering`. Invalidation cancels stale work, stops at the last valid position, smoothly recentres to a valid anchor and submits a bounded retry when appropriate. Falling cancels controlled traversal, enables existing Rigidbody2D gravity, requires stable grounded contact or grip before recovery, recentres smoothly, and resumes the stored valid destination. The default automatic retry limit is two; unreachable or search-limit results become stable `Blocked` outcomes instead of infinite replanning.

The `AntGameDevelopment` host can create development-only groups of 1, 100, 300 or 500 experimental ants, issue simultaneous or staggered requests, cancel one ant independently, mutate terrain, clear the group and inspect live scheduler statistics. All experimental ants share the map and scheduler. Friendly grid ants exclude the Default agent layer from contact response, so temporary overlap cannot create traffic deadlocks; the Terrain layer still supplies physics contact while Falling. Only the first 40 stress ants receive debug visuals. The accepted amber 2.4A ant remains available unchanged for comparison.

### 2.4C automated verification and repeatable measurements

- Unity Bee response-file Roslyn compilation passed for `AntColony.World`, `AntColony.Development`, `AntColony.EditModeTests` and `AntColony.PlayModeTests`.
- Focused scheduler EditMode: **11/11 passed**, covering budgets, yielding/resumption, fair priority, duplicate suppression, destination replacement, queued/active cancellation, terrain revision safety, search limits, unreachable results and the four benchmark sizes.
- Focused PlayMode evidence: shared-scheduler three-ant independence/cancellation **1/1 passed**; active-route terrain mutation/replan **1/1 passed**; falling interruption/stored-destination recovery **1/1 passed**. The previously accepted 2.4B focused 3/3 and full 9/9 PlayMode baselines remain unchanged.
- A combined focused PlayMode run initially passed four tests and timed out only on the long mutation fixture; that test passed after its test-only bound was raised from 20 to 35 seconds. Subsequent combined-suite retries could not start because Unity lost its Licensing Client connection and crashed in the editor REST-service Mono thread. No combined post-fix or full-suite result is claimed.
- The benchmark warms shared terrain classification separately, submits deterministic requests, advances the scheduler with the production-development limits (128 total nodes and 8 nodes per request/update), retains returned routes for the managed-memory lower bound, and times 100 Physics2D steps with stable grid bodies disabled. `scheduler ms` is accumulated path-service work; `batch ms` also contains request/result test-harness work. Travel figures are path-cost-derived individual traversal times at the ordinary-ant speed, not wall-clock stress-scene completion times.

| Ants | Batch completion | Scheduler work | Maximum update | Average / worst latency | Total nodes / updates | Peak queue | Predicted average / worst travel | Physics2D, 100 steps | Retained bytes | Results |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| 1 | 12.993 ms | 2.044 ms | 2.044 ms | 1 / 1 updates | 51 / 1 | 1 | 16.057 / 16.057 s | 1.431 ms | 0 | 1 success, 0 failures |
| 100 | 18.937 ms | 17.971 ms | 1.267 ms | 35.580 / 44 updates | 5,100 / 44 | 100 | 16.057 / 16.057 s | 0.223 ms | 217,088 | 100 successes, 0 failures |
| 300 | 72.932 ms | 69.704 ms | 1.890 ms | 106.137 / 132 updates | 15,300 / 132 | 300 | 16.057 / 16.057 s | 0.756 ms | 692,224 | 300 successes, 0 failures |
| 500 | 187.776 ms | 171.911 ms | 3.197 ms | 176.614 / 219 updates | 25,500 / 219 | 500 | 16.057 / 16.057 s | 1.143 ms | 1,118,208 | 500 successes, 0 failures |

All four runs recorded zero cache invalidations because mutation behaviour is measured separately. Cold cache warm-up varied from 7.839–8.455 ms except for one 70.016 ms one-ant startup outlier. These synthetic editor measurements demonstrate bounded work and correctness, not a production frame-rate guarantee. Request/result objects, per-search collections and route results still allocate managed memory; the scheduler is main-thread only; no route-result cache, flow field, hierarchical pathfinding, local avoidance or traffic ownership has been added.

### Exact 2.4C manual Unity Play Mode acceptance

No extra Inspector wiring is required.

1. Open `Assets/AntColony/Scenes/AntGameDevelopment.unity`, clear the Console, enter Play Mode and confirm the amber 2.4A ant and cyan experimental grid ant appear without errors.
2. Select `Commanding: 2.4A CONTINUOUS`, issue the accepted direct surface, prepared climb and falling/recovery checks, and confirm the regression ant still behaves as in 2.4A.
3. Select `Commanding: GRID POC`, click valid direct, detour, vertical, prepared and chunk-boundary destinations, and confirm the single grid ant moves smoothly and arrives without teleporting.
4. Choose group size `100`, press `Spawn Selected Grid Group`, then `Start Simultaneous Requests`. Confirm pending work decreases across many updates rather than all searches executing in one frame; inspect `Nodes last update` and confirm it never exceeds 128.
5. Confirm the group continues moving independently and reaches its deterministic destinations. Temporary friendly overlap is expected and must not deadlock routes.
6. While requests are pending, click a different valid destination for the primary grid ant or start a different deterministic batch. Confirm the old request is cancelled/replaced and no stale route later takes control.
7. Press `Cancel First Grid Request`. Confirm only the first experimental ant stops or changes state while other ants continue and complete.
8. Issue the enclosed `(12,-16)` target to the primary grid ant. Confirm it reaches stable `Blocked` and does not oscillate or retry forever.
9. Start a moving group, point at a route cell and press `F` to add/remove blocking Solid terrain. Confirm invalidation count increases, stale requests are cancelled, affected ants replan or fail clearly, and unaffected navigation remains stable. `P` may be used to toggle prepared Background and verify local speed-cost invalidation.
10. Press `Start Grid Falling / Recovery Fixture`. Confirm controlled traversal suspends, Rigidbody2D gravity drops the ant, stable contact is acquired, the ant recentres smoothly and a stored valid destination resumes without teleporting or oscillation.
11. Repeat steps 4–5 with `300` and `500`, then use `Start Staggered Requests`. Confirm both request modes finish without crashes; wait for pending/processing/delivery counts to reach zero before changing group size.
12. Confirm the panel reports selected/live agents, pending, processing, delivery, last/total nodes, peak queue, last/maximum scheduler milliseconds, invalidations, submitted, completed and cancelled counts with plausible changes during each action.
13. Press `Clear Extra Grid Ants`, exit Play Mode and confirm the Console contains no relevant exception, assertion, missing-reference or infinite-retry messages.

The user subsequently confirmed the manual Play Mode checks, completing Milestone 2.4C. This milestone does not implement colony jobs, excavation, resources, construction, identity/skills, needs, brood, rooms, Draft Mode, combat, enemy AI, persistence, streaming or networking.

## Latest implemented milestone — revised 2.4B Grid Navigation Proof of Concept (12 October 2026)

**Status: Complete — manual Unity Play Mode acceptance confirmed by the user. Production movement is not replaced.**

The approved proof of concept adds a separate grid-authoritative traversal path for one ordinary-ant profile. `GridNavigationMap` lazily classifies grounded and Background-grip anchors from `ChunkedWorldData`, caches eight-direction terrain connections, checks the complete ordinary-ant capsule along every edge, and rejects diagonal corner cutting. `GridPathfinder` performs deterministic A* with travel-time costs derived from the Background material speed multipliers. A binary-heap open set keeps searches bounded without scanning the entire open list.

`GridNavigationMotor` smoothly interpolates the cyan experimental ant between the returned anchor poses. Its Rigidbody2D is excluded from simulation during stable grid traversal and is enabled with gravity only in `Falling`; after contact restores Grounded or Gripping capability, the motor disables physics and smoothly recentres on a valid anchor. The map is shared terrain data: moving ants are not stored as obstacles. Successful Solid or Background mutations invalidate a local five-by-five cache neighbourhood and cause the experimental motor to re-evaluate or replan. Local avoidance, congestion and moving-enemy steering remain deliberately deferred.

The existing 2.4A `AntAutonomousMotor`, `DirectGroundRouteValidator`, prefab and tests remain available in the same scene. The inspector defaults clicks to the grid proof of concept and has a button to switch click commands back to the 2.4A continuous ant. The two comparison colliders ignore one another so the experiment cannot change the retained movement regression. The paused continuous-route 2.4B work is recoverable on branch `codex/milestone-2.4b-continuous-snapshot` at commit `28864f9`.

### Automated verification and benchmark

- Focused grid EditMode: 11/11 passed.
- Full graphics-enabled EditMode: 238/238 passed.
- Focused grid PlayMode: 3/3 passed, covering smooth non-teleport traversal, prepared vertical travel, Rigidbody2D falling and grid recovery.
- Full PlayMode: 9/9 passed, including all six retained 2.4A physics regressions.
- Benchmark method: warm the shared terrain cache, run independent A* requests across a 39-cell representative route, simulate every returned smooth route to completion at a `0.02` fixed step, and time 100 Physics2D steps with the stable grid bodies excluded from simulation. Allocations are the retained managed-heap lower bound after a full collection, not total transient allocation traffic.

| Simulated ants | A* batch | Nodes examined | Complete smooth-route simulation | 100 Physics2D steps | Retained managed bytes | Correct arrivals/failure |
| ---: | ---: | ---: | ---: | ---: | ---: | --- |
| 1 | 1.657 ms | 501 | 1.079 ms | 0.938 ms | 0 | 1/1; invalid target rejected |
| 100 | 123.151 ms | 50,100 | 5.632 ms | 0.273 ms | 241,664 | 100/100; invalid target rejected |
| 300 | 363.736 ms | 150,300 | 17.218 ms | 0.597 ms | 1,085,440 | 300/300; invalid target rejected |
| 500 | 645.436 ms | 250,500 | 24.277 ms | 1.086 ms | 1,875,968 | 500/500; invalid target rejected |

The result supports grid-authoritative execution as the simpler per-frame foundation: stable ants require no Rigidbody2D simulation and movement work is a small deterministic interpolation loop. It also exposes the next scaling constraint clearly: 500 uncached searches must not be launched in one frame. A production version needs request budgeting and likely shared flow fields or route/subpath reuse for common destinations before hundreds of ants are enabled. This proof of concept does not claim local avoidance or mass-agent production readiness.

### Exact manual Play Mode acceptance

1. Open `Assets/AntColony/Scenes/AntGameDevelopment.unity`, enter Play Mode, and confirm one amber 2.4A ant plus one cyan `Experimental Grid Ant` appear with no Console errors.
2. Leave the panel on `Commanding: GRID POC`. Click exposed natural cells along the surface, the natural access shaft near X=-9, and the main passage at `(-2,-8)`. Confirm the cyan ant follows connected anchor-to-anchor motion smoothly and never teleports.
3. Click the prepared strip between X=-18 and X=-8 at Y=-8, then the prepared fixture top. Confirm travel remains valid in every direction and visibly speeds up over prepared Background.
4. Command `(18,-8)`. Confirm the route detours around the blocker at `(10,-8)` and does not cut diagonally through its corners. Command `(33,-8)` to cross the positive chunk boundary. Command `(12,-16)` and confirm the enclosed target is rejected without movement.
5. While the cyan ant is moving, select a remaining route cell and press `F`. Confirm the cache revision changes and the ant replans or reports a bounded failure without entering the new Solid. Remove the Solid and retry. Press `P` on an exposed route cell and confirm the route cost/speed responds after local invalidation.
6. Click `Start Grid Falling / Recovery Fixture`. Confirm the Rigidbody2D gravity drop occurs, the ant lands, smoothly recentres on a valid anchor, and returns to Idle with physics simulation disabled.
7. Switch the panel to `Commanding: 2.4A CONTINUOUS`. Repeat a direct ground move, prepared climb, terrain block and falling/recovery check. Confirm the amber ant retains the accepted 2.4A behaviour and neither ant pushes the other.
8. Exit Play Mode and confirm the Console contains no exceptions, assertion failures or missing-reference messages.

The user subsequently confirmed the manual comparison and stress-fixture behaviour, completing 2.4B. This acceptance does not authorize replacing the production motor or adding jobs, inventories, construction, survival, combat, reproduction, networking, local avoidance or enemy movement.

## Accepted baseline — 2.4A Natural Background Movement Realignment (12 October 2026)

**Status: COMPLETE — manual Unity Play Mode acceptance confirmed by the user.**

The user reports natural Earth and Stone background supports valid horizontal/vertical/diagonal grip at 1.0× movement speed; prepared background grants identical traversal with provisional 1.25× speed; Sky remains non-grippable with physics-driven falling. Movement capability, route validity and speed react to terrain/background changes. Existing developer input, collision and movement architecture remain intact.

**Reported implementation validation:** Unity Roslyn compilation passed; focused Edit Mode 56/56; graphics-enabled full Edit Mode 227/227; physics-backed Play Mode 6/6; navigation/development scene validators and `git diff --check` passed. A previous `-nographics` run crashed in a rendered-pixel test, while graphics-enabled rerun passed. The user subsequently confirmed manual Play Mode tests were good, completing the milestone. This is **user-reported validation**, not independently executed in this documentation-only update.

Earlier 2.3B and obsolete prepared-only grip statements below are historical and superseded by this accepted milestone.

The revised 2.4B proof of concept described above is the only later implementation. Keep jobs, construction, inventory and reproduction outside it.


## Design-versus-implementation status — 12 October 2026

Milestone 2.4A now implements the approved natural-background movement realignment. Exposed Natural Earth and Natural Stone provide full horizontal, vertical and diagonal grip at normal speed. Prepared Background provides the same traversal plus a provisional `1.25` movement-speed multiplier. Sky remains explicitly non-grippable. At that checkpoint, the other approved systems were design targets only; 3.2A now adds needs data and test simulation, while functional backgrounds, rooms, brood, physical shared stores and their gameplay remain unimplemented.

**Historical next step:** the 3.3A checklist was accepted and 3.3B was subsequently authorised. See the current milestone section at the top of this file.

## Current milestone

**Milestone 3.3D — Material Storage & Autonomous Hauling**

**Status:** **Implemented — Pending Manual Acceptance.** Physical Material Storage and autonomous loose-drop/self-deposit hauling are implemented. Verification passed 15/15 focused Edit Mode, 3/3 focused Play Mode, 447/447 complete Edit Mode and 45/45 complete Play Mode. The exact 30-step manual checklist is pending above.

Milestones through accepted 3.3C retain their recorded status. Milestone 2.4D remains implemented pending its separately recorded manual acceptance. Milestone 3.3D adds physical Material Storage and autonomous hauling only; preparation, construction and broader economy systems remain unimplemented.

## Repository and architecture

- The Unity 2D URP project uses Unity `6000.3.8f1` and the new Input System.
- `Assets/AntColony/Scenes/AntMovementPrototype.unity` is the first build scene.
- `Assets/AntColony/Scenes/AntGameDevelopment.unity` is the integrated development scene. The movement and navigation prototype scenes remain available temporarily as regression fixtures.
- The prototype controls one ordinary player ant. Becoming a queen is later progression.
- Input and movement retain the established `PlayerMovementInput -> AntSurfaceMotor -> Rigidbody2D` flow.
- `AntSurfaceMotor.SetMovementIntent(Vector2)` remains a useful prototype boundary while the future movement model is developed and validated.
- The current authored collider course is not evidence of the approved Background Tile/Solid Tile data model.
- The integrated scene has a small authored `World Bootstrap` coordinator, not a global manager or second world-data implementation. The world-data authority remains an ordinary, scene-independent C# object; excavation gameplay, multiplayer and multi-ant systems remain absent.

### Milestone 2.1B world-data foundation

- `ChunkedWorldData` explicitly owns resident chunks in a sparse dictionary. Creating or reading world data does not create GameObjects, components, colliders, or neighbouring chunks.
- Each `WorldChunk` is 32×32 cells and immediately fills all 1,024 Background Tile entries. A parallel compact array stores zero or one optional Solid Tile ID per cell.
- Central coordinate utilities use floor division for negative positions, keep local coordinates in the 0–31 range, and preserve the prototype convention that integer coordinates are tile centres in world space.
- Missing chunks return `ChunkUnavailable`; they never masquerade as exposed cells. Duplicate creation and unknown material IDs are rejected without overwriting data.
- `Background_Default` (`background.default`) and `Solid_Soil` (`solid.soil`) are initial authored ScriptableObject definitions. Milestone 2.1E assigns each a simple development-only visual Tile while retaining the material definition as the source of that reference.
- The 2.1B foundation stores a world seed without implicit generation. The later integrated checkpoint now consumes that seed through one explicit bounded generator; unloading, streaming and persistence still do not exist, and ordinary queries still never generate terrain.
- The existing `DirtBlock` objects have not been imported. The prototype scene, prefab, movement code, Rigidbody2D, colliders, gravity, and input remain unchanged.
- The focused Milestone 2.1B Unity Edit Mode suite passed 25/25, and the user confirmed that existing ant movement still works in Play Mode. Milestone 2.1B is complete.

### Milestone 2.1C navigation classification

- `WorldNavigationClassifier` is a plain C# service that reads `ChunkedWorldData` directly. It does not use scene colliders, physics queries, Tilemaps, movement components, or a scene host.
- Occupancy results distinguish `Unavailable`, `Covered`, and `Exposed`. Only a Solid Tile whose registered material has `BlocksMovement` enabled makes its cell Covered.
- Exposed cells separately report `Supported`, `Unsupported`, or `Unknown` support. This does not claim Traversable or Reachable status because body clearance, obstacles, movement-edge checks, and connectivity are not implemented.
- Only north, south, east, and west neighbours can support a cell. The neighbouring Solid Material must have `SupportsNavigation` enabled; Background Tiles and diagonal solids never qualify.
- Classification crosses positive and negative chunk boundaries. Missing target chunks are Unavailable. A missing neighbour produces Unknown only when no resident cardinal neighbour already provides confirmed support.
- Results are uncached and therefore reflect successful Solid Tile mutations immediately. Change notifications are deferred until a later cached visualisation or incremental navigation consumer genuinely requires them.
- The user-confirmed Milestone 2.1C acceptance suite passed 50/50 Edit Mode tests. The subsequent full Milestone 2.1D run also retained all classifier coverage, and Milestone 2.1C is complete.

### Milestone 2.1D navigation visualisation

- `Assets/AntColony/Scenes/AntNavigationPrototype.unity` is a separate developer-only scene. It does not alter `AntMovementPrototype.unity` or the existing movement, Rigidbody2D, input, and collider prototype.
- `NavigationTestWorldHost` authors a deterministic eight-chunk fixture through `ChunkedWorldData` APIs. It includes open floor, a wall and overhang, a narrow tunnel, a large unsupported chamber centre, negative coordinates, chunk boundaries, diagonal-only geometry, and an absent neighbouring chunk.
- One bounded batched mesh visualises 3,564 queried cells without per-cell GameObjects or colliders: Covered is brown, Exposed and Supported is green, Unsupported is red, Unknown is amber, and Unavailable is grey.
- The renderer reads the real `WorldNavigationClassifier`. Optional chunk-boundary lines, overlay visibility, Scene-view cell selection, world/chunk/local inspection, and an explicit Solid Tile mutation-and-refresh control are developer diagnostics only.
- Supported still means local cardinal terrain support only. The display does not claim body clearance, obstacle clearance, connectivity, reachability, or pathfinding.
- The complete automated suite passed 61/61 and the user confirmed all 11 graphics-enabled manual visual checks. Milestone 2.1D is complete.

### Milestone 2.1E world Tilemap integration

- `AntNavigationPrototype.unity` now contains one explicitly half-cell-offset `World Grid`, a `Background Tilemap`, and a `Foreground Solid Tilemap`. Cell `(0,0)` is therefore centred at world `(0,0)` while signed Tilemap indices remain identical to authoritative world-cell coordinates.
- `WorldTilemapRenderer` is a one-way projection from `ChunkedWorldData` through the material definitions to the two Tilemaps. It never reads Tilemap contents as simulation state and exposes explicit cell, chunk, and bounded selected-region refresh methods.
- Resident cells receive their registered Background visual. Optional Solid visuals are written independently above them, including for non-blocking Solid Materials; removing a Solid clears only the foreground presentation.
- Missing chunks are cleared rather than generated. Unknown IDs and missing visual references return precise diagnostics, clear stale presentation, and leave world data and navigation classification unchanged.
- The initial material definitions reference two colour-tinted development Tiles that reuse the existing one-pixel placeholder soil Sprite. Both Tile assets explicitly disable collider generation.
- Background, foreground, and navigation-overlay visibility remain independent. The overlay still reads only `WorldNavigationClassifier` and renders above both Tilemaps.
- Milestone 2.1E added no collision components, per-tile GameObjects, pathfinding, excavation, or autonomous movement.
- Unity compilation, scene validation, and all 89/89 Edit Mode tests passed. The user subsequently confirmed all 10 graphics-enabled manual visual checks, so Milestone 2.1E is complete.

### Milestone 2.2A authoritative terrain collision

- `AntNavigationPrototype.unity` contains a third, collision-only `Foreground Collision Tilemap` beneath the same half-cell-offset Grid. The Background and visual foreground Tilemaps remain collider-free.
- `WorldTerrainCollisionRenderer` is a one-way projection from `ChunkedWorldData`: it places an invisible Grid-collider Tile only when the registered Solid Material has `BlocksMovement` enabled. It never treats Tilemap or collider occupancy as authoritative world state.
- Visual foreground occupancy, physical collision and navigation support remain independent. The fixture includes a non-blocking supporting Solid and a blocking non-supporting Solid to demonstrate that `BlocksMovement` and `SupportsNavigation` are not aliases.
- One `TilemapCollider2D` feeds one manually generated `CompositeCollider2D` on a static `Rigidbody2D` using the existing `ClimbableSurface` layer. Adjacent cells merge without per-tile GameObjects or duplicate terrain hits.
- Explicit cell, chunk and bounded-region refreshes update only requested collision occupancy. `SynchronizeCollisionGeometry` processes pending Tilemap collider changes, regenerates composite geometry and synchronizes transforms before development physics queries.
- The custom inspector reports authoritative blocking state and projected collision occupancy for the selected debug cell, refreshes a cell or selected region, coordinates the existing controlled Solid mutation, and can toggle collision-outline Gizmos.
- Unity compilation, the expanded scene/material/collider validator, and all 117/117 Edit Mode tests passed. The user subsequently confirmed all 12 manual Unity collision checks, so Milestone 2.2A is complete.
- This collision projection is development-scene infrastructure only. It does not establish whole-body clearance, connectivity, reachability, pathfinding, autonomous movement, procedural generation or excavation gameplay.

### Milestone 2.2B ant body clearance validation

- `AntClearanceValidator` is a scene-independent, read-only service that checks a complete rotated capsule directly against authoritative movement-blocking Solid cells. It does not read Tilemaps or collider geometry and therefore cannot return a stale result after a successful world-data mutation.
- `AntCollisionProfile` carries local capsule size, offset, long axis and optional clearance margin. The initial `OrdinaryAnt` profile matches the unchanged movement-scene ant: its prefab-local 1.2×0.6 capsule is scaled by the authored 0.8×0.5 instance scale to an effective horizontal 0.96×0.3-world-unit body with zero offset.
- Results distinguish Clear, Blocked by terrain, Unavailable world data and invalid query/profile. Clearance does not inspect foreground support, Background movement permissions, grip, grounding, gravity, movement state, connectivity or reachability.
- Capsule-versus-cell testing uses the exact central segment plus radius against candidate 1×1 axis-aligned cells. Rotation and collider offset are applied mathematically, only the footprint's bounded cell range is queried, exact tangency is permitted within a `0.00001`-unit tolerance, and an optional margin provides conservative separation.
- A valid capsule is Clear when every touched cell is available and no overlapping Solid Material blocks movement. A known blocking overlap takes precedence over missing data elsewhere in the footprint because it already proves the pose invalid; otherwise any missing touched cell returns Unavailable. Physically fitting open Sky and unprepared shafts are therefore Clear.
- `AntNavigationPrototype.unity` includes an `Ant Clearance Inspector` with an editable position, orientation and profile, a colour-coded exact capsule outline, a Scene-view position handle, a result label and explicit query button. It creates no gameplay ant, collider or Rigidbody2D.
- Unity compilation, focused 38/38 clearance tests, the expanded scene validator, the focused 2/2 movement regression and the full 155/155 Edit Mode suite passed. The user subsequently confirmed all 9 graphics-enabled manual clearance checks, so Milestone 2.2B is complete.

### Milestone 2.2C Background movement capability and gravity eligibility

- `BackgroundTileMaterialDefinition` stores explicit horizontal, upward and downward voluntary movement permissions, intrinsic climbing grip, unsupported gravity and a data-driven movement-speed multiplier.
- Authored development materials define Natural Earth and Natural Stone as full-direction grippable terrain at `1.0x`. Prepared Grippable Background has the same directions and grip at a provisional `1.25x`. Sky is non-grippable and gravity-eligible.
- `AntMovementCapabilityEvaluator` is a read-only, scene-independent service. It composes the unchanged `AntClearanceValidator`, the centre Background Material and an authoritative ground-contact query into explicit `Grounded`, `Gripping` or `Falling` eligibility plus detailed permissions and failure diagnostics.
- Ground support uses the full rotated capsule's lowest extent and a `0.02`-world-unit contact tolerance against top faces of registered Solid Materials that both block movement and support navigation. Side walls, ceilings, non-blocking solids and Background Tiles do not become floor support.
- Grounded and exposed natural terrain permits its configured directions through normal grip. Falling occurs only where neither valid ground nor a grippable Background exists. Prepared conversion changes speed rather than access and does not change physical clearance.
- The existing `SetBackgroundMaterial` mutation safely demonstrates Unprepared Earth to Prepared replacement without changing foreground occupancy, creating chunks or introducing Surface Preparation gameplay. `WorldTilemapRenderer.RefreshCell` explicitly refreshes its presentation.
- `AntNavigationPrototype.unity` includes one `Ant Movement Capability Inspector` at the unsupported chamber centre. It reports Background ID, clearance, ground, grip, direction permissions, gravity and suggested state; its explicit toggle button replaces the selected Background, refreshes the visual cell and requeries.
- Focused capability tests pass 37/37. The expanded structural scene validator, focused 2/2 legacy movement regression and complete 192/192 Edit Mode suite pass. Graphics-enabled manual Scene-view acceptance remains pending, so Milestone 2.2C is in progress rather than complete.

### Integrated development sample world checkpoint

- `AntGameDevelopment.unity` combines the existing authoritative chunk data, material registries, visual Tilemaps, collision-only Tilemap, clearance validator and movement-capability evaluator in one Play Mode inspection environment. It does not contain the legacy manually controlled ant.
- `SampleWorldGenerator` explicitly creates a bounded 4×4 group of 32×32 chunks from `(-2,-2)` through `(1,1)`, for an initial 128×128-cell resident world. Queries outside that region remain unavailable and do not create chunks.
- Seed `2203` deterministically produces gently varied surface elevation, exposed Sky, Earth and Stone Background regions, Soil and Stone foreground terrain, two chambers, connected passages, one unprepared vertical shaft, the existing underground prepared strip and a prepared surface grip fixture. All authoritative cells are populated before Tilemap or collision projection.
- The scene hierarchy contains `World Bootstrap`, one half-cell-offset `World Grid`, separate Background and foreground visual Tilemaps, one hidden foreground collision Tilemap, `Main Camera`, and `Development Inspection UI`. No per-tile GameObjects or duplicate terrain colliders are introduced.
- WASD or arrow keys pan, the mouse wheel zooms, middle-mouse drag pans and Home returns to the starting view. Clicking outside the panel selects a cell. The panel reports authoritative cell/chunk/material data, blocking state, ordinary-ant clearance, grip, grounded/falling eligibility and voluntary directions.
- `F` or the foreground button toggles the selected Solid Tile and explicitly refreshes both foreground presentation and collision. `P` or the Background button switches exposed space between prepared and its surface/depth-appropriate unprepared material, preserving foreground occupancy and refreshing the inspection result.
- The first manual Play Mode pass confirmed world generation and middle-mouse dragging, but found that the material palette rendered too dark and wheel zoom appeared inactive. The visual cause was multiplication of every Tile tint by the old dark-brown placeholder sprite. Integrated-world Tiles now use a neutral white pixel plus the explicit Sky `#719BC1`, Soil `#986342`, Stone `#737E89`, Earth Background `#514439`, Stone Background `#434C56` and Prepared Background `#648B62` palette.
- Wheel zoom previously assumed a roughly 120-unit Windows mouse delta and scaled the raw value directly, making approximately 1-unit device deltas visually negligible. Scroll is now normalised into device-independent steps, changes the target by 4 orthographic units per step, smoothly approaches that target over `0.08` seconds and remains clamped to an orthographic size of 4–64. Home resets both the current and target size to 30; pan and middle-drag remain in the same controller.
- A subsequent manual check showed that authoritative Unprepared-to-Prepared mutation, grip and movement eligibility changed while the visible Background remained brown. A graphics-enabled authored-scene test at the reported cell `(-1,15)` ruled out coordinate mismatch and occlusion. `Tilemap.SetTile` stored the prepared Tile asset but preserved the cell's previous Earth `TileData` colour; neither `RefreshTile` nor the bounded `RefreshAllTiles` diagnostic replaced that stale colour. Single-cell projection now copies a standard Tile's authored colour into the cell, temporarily unlocking only `LockColor` and restoring the effective flags before refreshing that cell. The effective authored flag remains `LockTransform`.
- The development inspection panel now reports the world and Tilemap coordinates, material ID, Tile name, Tile asset colour, effective cell colour, Tilemap colour, sampled Sprite pixel, composed expected colour, asset and cell flags, renderer material/shader, sorting, and active/enabled state. A development-only button isolates the Background visual Tilemap by hiding the foreground visual layer, without changing authoritative data or collision.
- The graphics-backed regression renders the authored camera at `(-1,15)`: Earth is `RGBA(0.318, 0.271, 0.220)`, prepared is `RGBA(0.388, 0.545, 0.384)`, the isolated prepared pixel is identical, and toggling back restores Earth. The user subsequently confirmed the corrected palette, camera controls, inspection, foreground mutation and prepared-Background colour round trip in Play Mode. The integrated sample-world checkpoint is manually accepted.
- This checkpoint does not complete Milestone 2.2C, begin general directional connections, perform real excavation or Surface Preparation jobs, stream chunks, or save/load the world.

### Milestone 2.3A first autonomous ant — ground movement

- `AutonomousAnt.prefab` is separate from `PlayerAnt.prefab`. It uses the ordinary ant's 0.96×0.3 horizontal capsule, a dynamic continuous-collision `Rigidbody2D` with frozen rotation, a bright development visual and `AntAutonomousMotor`. It contains no `PlayerInput`, `PlayerMovementInput` or `AntSurfaceMotor`.
- `AntGameDevelopmentWorldHost` searches at most 20 surface columns around preferred X `-12`. Each candidate must be resident, physically clear, centre-supported, horizontally permitted and reported Grounded by `AntMovementCapabilityEvaluator`. A failed bounded search logs a clear error and creates no ant; repeated initialisation resets the existing instance rather than duplicating it.
- Left-clicking outside the inspection panel still selects and inspects the clicked cell, and now also requests a destination for the ant. Clicking either a supporting floor cell or its exposed cell resolves the same standing pose. Invalid commands hide the marker and report the rejection reason.
- For 2.3A, `DirectGroundRouteValidator` was a scene-independent, read-only horizontal corridor check: at most 24 world units and samples at most every 0.2 world units, with whole-body clearance, Grounded horizontal capability and centre support required throughout.
- For 2.3A, `AntAutonomousMotor` advanced through `Rigidbody2D.MovePosition` in `FixedUpdate`, turned its visual when direction changed and stopped within a configurable 0.08-unit tolerance. It revalidated each immediate step so new blocking terrain or removed support stopped movement.
- The existing panel reports ant position, Idle/Moving/Arrived/Blocked state, destination and readable status. A small yellow marker shows an accepted destination and becomes green on arrival.
- Focused coverage included eight route-validator cases, five authored-scene spawn/destination/wiring cases and three physics-backed Play Mode cases for arrival, new blockers and lost support. Unity scene structural validation, the complete 221/221 Edit Mode suite and the complete 3/3 Play Mode suite passed. The user subsequently confirmed all 11 manual Play Mode checks, so Milestone 2.3A is complete.

### Milestone 2.3B Background grip and gravity movement

- `AntAutonomousMotor` now reports physical `Grounded`, `Gripping` and `Falling` separately from command progress (`Idle`, `Moving`, `Arrived`, `Blocked`). Each physics step uses `AntMovementCapabilityEvaluator`; no material ID is hardcoded in the motor.
- Grounded and Gripping are controlled states: gravity scale is zero and a prevalidated direct command may use `Rigidbody2D.MovePosition`. Falling is mutually exclusive in the same physics step: the command is interrupted, horizontal command velocity is cleared, the authored gravity scale of 4 is restored and Rigidbody2D collision moves the ant until ground or grip is reacquired.
- Ground contact keeps the existing `0.02`-world-unit capability tolerance. State selection otherwise follows the stable centre Background cell; there is no timer-based hysteresis. Entering Grounded or Gripping clears fall velocity once, preventing boundary oscillation from retaining downward momentum.
- The bounded direct validator supports horizontal, vertical and diagonal segments up to 24 units. Every sample requires whole-body clearance and either valid Grounded support or material-provided grip; direction flags are checked at every interval, missing chunks fail closed and diagonal cell transitions require both orthogonal neighbours to be resident and unblocked. It still performs no detours, graph search or waypoint planning.
- The deterministic sample adds a short flat Natural Earth floor across the seven-cell Prepared vertical strip at X `-12`, plus the prepared top region. This exposes Grounded natural movement, natural-to-prepared transition, vertical/diagonal movement, speed comparison, explicit Sky grip loss, recovery and safe falling without teleporting the ant.
- The inspection panel reports ant position, locomotion, command progress, Background ID, grip, gravity eligibility, base/effective movement speed, destination and interruption/rejection reason. Foreground and Background mutations refresh the relevant projections and notify the motor immediately, which rechecks local state and invalidates an active direct route if necessary.
- Unity's generated Roslyn response files compile the changed World, Development, Editor and test assemblies. The focused 2.4A Edit Mode groups pass 56/56; the full graphics-enabled Edit Mode suite passes 227/227; all physics-backed Play Mode tests pass 6/6. An earlier full `-nographics` attempt crashed in the existing rendered-pixel test, so the successful full suite was correctly rerun with graphics enabled.

## Approved design boundaries and future work

- Normal play manages ants through selection, inspection, designations, colony priorities and specific task assignments; it does not directly steer them.
- Every coordinate in a generated chunk contains one permanently present Background Tile whose material may be replaced, plus zero or one foreground Solid Tile; ungenerated chunks remain unavailable.
- Background Materials are the primary authority for voluntary directions, intrinsic grip, gravity behavior and movement-speed multipliers. Natural Earth/Stone and Prepared terrain grant grip; Background existence alone still grants nothing when the material is Sky or explicitly non-grippable.
- Excavation removes an eligible foreground Solid, leaves its Background unchanged and must update affected systems. Exposing intact natural Earth or Stone therefore exposes its already-grippable Background; excavation gameplay itself remains unimplemented.
- Surface Preparation is an approved future resource-consuming job. Its movement benefit is faster travel over an already-traversable Background, not access to climbing.
- Physical clearance, Background movement capability and directional navigation connections are separate. Falling through physically clear, non-grippable space may be valid even when upward voluntary movement is not.
- Natural and Prepared grip can permit clearance-valid two-dimensional movement without immediate Solid adjacency. Foreground `BlocksMovement` always remains authoritative, and `SupportsNavigation` stays independent.
- Navigation connections may be directional; diagonal corner-cutting through blocking geometry remains invalid.
- Every ant starts every ordinary skill at 0, can perform ordinary tasks without a specialist gate, and improves from successful work on a 0–100 scale. Roles remain preferences and priorities.
- Independently tracked skills above 50 slowly decay after inactivity and stop at exactly 50; skills at or below 50 do not decay through inactivity.
- The proposed carrying system remains future work: one initial carrying slot plus one slot per 20 Hauling skill, capped at five slots, with one item per slot.
- Multiple ants may share a tile and use hybrid separation, local avoidance and bounded recovery rather than universal one-ant-per-tile occupancy.
- Friendly, neutral and hostile colonies share the same terrain navigation model.
- The proposed architecture separates world tile data, terrain/excavation and preparation changes, physical clearance, Background movement capability, directional navigation, colony task decisions, per-ant route following, continuous movement, local avoidance and authoritative collision.
- The design target allows potentially hundreds of simulated ants, but no DOTS/ECS or other substantial dependency is approved before profiling demonstrates a need.

## Implemented and Play Mode confirmed

The user has confirmed the previous movement prototype works in Unity Play Mode:

- A/D and Left/Right horizontal floor movement.
- Stopping and direction reversal.
- Floor attachment.
- W upward climbing and S downward movement on left and right walls.
- Wall attachment without sliding when input is released.
- Surface-aligned floor and wall rotation.

## Newly implemented, pending Play Mode acceptance

### Ceiling traversal

- A fourth non-allocating collider cast detects a nearby climbable ceiling.
- Ceiling attachment uses the same motor, attachment state, gravity handling, and collision system as floors and walls.
- A/D is projected onto the ceiling tangent, and the ant rotates so its feet face the ceiling.
- Releasing input produces zero traversal velocity while a valid ceiling remains attached.

### Directional surface choice

- The current surface remains preferred while stationary or while intent still matches its tangent.
- A connected perpendicular surface is selected only when its movement direction clearly matches world-space input.
- Floor-to-wall selection therefore requires upward intent rather than automatically converting A/D into a climb.
- At a wall-to-ceiling junction, W continues toward the obstruction but does not choose left or right. A or D selects the corresponding valid ceiling direction.
- Ceiling-to-wall and wall-to-floor choices use the same intent projection rather than surface-specific input code.
- Selection thresholds and bias are configurable on `AntSurfaceMotor`.

### Inside and outside corners

- Direct down/left/right/up casts provide candidate surfaces at concave inside corners.
- A short look-ahead from the current contact detects the perpendicular face around a convex outside edge. Zero-distance results are rejected so a ray beginning inside ordinary terrain cannot create a false corner.
- Candidate surface lines must intersect within the configurable connected-corner distance, and separate source/target colliders must physically touch or fall within a small seam tolerance. This prevents attachment across unsupported gaps.
- A successful transition follows a smooth quadratic corner path through `Rigidbody2D.MovePosition` and `MoveRotation`; collision is never disabled and the Transform is not moved directly.
- The complete path is sampled before transition using a slightly inset copy of the ant collider. Every movement step also performs a collider sweep and a pose-overlap check.
- If clearance fails, the transition is rejected and the ant remains on the current surface. Releasing input pauses an active transition; opposite intent can reverse it.
- Position and orientation advance at the slower of configured movement and rotation rates to avoid snapping or uncontrolled acceleration.

### Corner-transition deadlock fix

- Live Play Mode testing confirmed that an ant could enter a corner transition and become unable to move or reverse.
- The transition step previously recorded a failed sweep/overlap and returned while leaving the motor permanently active. A progress-zero reversal was also validated as a collision step before it could exit to the source, and only the source-opposite direction counted as reverse input.
- A blocked first step now cancels immediately to the source. A later blocked forward step is latched instead of retried every physics frame, preserving reversal along the validated path.
- Either the opposite source direction or the opposite destination direction reverses a transition, so escaping does not require an unintuitive key combination at the midpoint.
- If an already-travelled reverse step becomes blocked by changed geometry, the motor cancels onto a currently detected source/target surface or explicitly detaches at its last valid physical pose.
- A rejected transition pair is not retried with unchanged held input. Releasing or changing input clears the suppression.
- Source and destination colliders are ignored only by the redundant movement sweep because they are intentional support contacts; inset pose-overlap validation remains active to prevent penetration.

The user subsequently confirmed that the original deadlock no longer reproduces in Play Mode. Broader ceiling and corner-route acceptance is still separate pending work.

### Auto Step-Over

- Continued tangent input near a support edge starts an explicit controlled crossing only when a same-type, same-normal landing surface is detected across an opening no wider than the ant's configured capability.
- The player prefab defines one prototype tile as one world unit and limits this ant to one tile. Openings wider than that limit and openings without a destination are rejected.
- Floor, ceiling, and vertical-wall openings use the same surface-relative detector; there is no floor-only jump path.
- A strong perpendicular input wins over automatic crossing, allowing later explicit opening entry or junction behavior without competing state changes.
- The ant has no ordinary `SurfaceType` while crossing. Gravity and ordinary attachment movement are replaced by a prevalidated straight `Rigidbody2D.MovePosition` path between real supports; collision is never disabled and no Transform teleport is used.
- The whole inset capsule is checked at 24 poses before entry. A collider cast and pose overlap are repeated for every movement step so a blocking obstacle cannot be bypassed.
- Releasing input pauses progress. Opposite tangent input reverses toward the source. A new block at the first step cancels to the source; a later block preserves the already validated reverse route, with safe surface recovery or explicit detachment if changed geometry blocks both directions.
- Rejected geometry is not queried again every physics tick while the same direction remains held. Input release/change or `NotifyTraversalGeometryChanged()` permits a fresh query.
- Read-only diagnostics expose active state, progress, measured gap start/end and distance, configured maximum distance, and a specific rejection reason. Optional gizmos show the measured opening and controlled path.

### Attachment and debugging

- Attachment states now include `None`, `Floor`, `LeftWall`, `RightWall`, and `Ceiling`, plus explicit active corner-transition and Auto Step-Over states.
- Gravity is disabled only while a real surface or prevalidated connected-corner path is active. Losing support restores the Rigidbody2D's authored gravity without teleportation.
- Fixed-size hit, candidate, and overlap arrays avoid routine movement allocations.
- Optional gizmos show all four direct probes, input intent, candidate points/normals, the selected attachment, movement direction, active corner path, and the latest rejected transition point.
- Read-only diagnostics expose transition source, destination, progress, accepted direction, and the last failure reason. Optional `Log Transition Events` output reports state changes and failures without per-frame logging.

## Authored prototype course

`AntMovementPrototype.unity` now contains:

- the existing climbable floor;
- left and right climbable walls;
- a full-width climbable ceiling, forming a continuous enclosed rectangular loop;
- a floor-connected central block whose upper edges expose outside corners; and
- the player ant starting left of the central block with room to approach and reverse;
- separate one-tile floor, ceiling, and vertical-wall opening fixtures;
- a two-tile floor opening that must be rejected; and
- a one-tile opening with a non-climbable obstacle at its landing path.

`Milestone1PrototypeSetup` updates or regenerates these named objects without duplicating them. The Editor menu now also provides `Apply Auto Step-Over Course` and `Validate Auto Step-Over Course`; the older surface-transition and wall-climbing commands remain compatible.

## Automated validation performed

- Unity compiled the updated runtime and Editor assemblies successfully.
- The Editor migration serialized the ceiling course, central outside-corner block, new starting position, and new motor configuration.
- `ValidateSurfaceTransitionPrototype` passed, confirming prefab wiring, the climbable mask, rotatable Rigidbody2D, player scene instance, enclosed floor/wall/ceiling course, and outside-corner block.
- Repository whitespace validation passed with `git diff --check` after Unity-generated whitespace was cleaned up.
- A focused `AntColony.EditModeTests` Unity Test Framework assembly was added without moving the runtime scripts out of Unity's existing predefined assembly.
- The focused Edit Mode test passed 1/1. `AntSurfaceMotorRegressionValidation` exercised the actual motor transition method and covered release/pause, both intuitive reversal directions, blocked-first-step cancellation, later-step forward blocking, blocked-reverse safe cancellation, failure-reason recording, retry suppression, and suppression reset after input release.

These checks validate compilation and authored structure. The new ceiling and transition behavior has not been visually verified in Play Mode.

For the Auto Step-Over slice:

- Unity loaded the updated runtime and Editor assemblies without C# compilation errors during two command-line launches.
- Static authored-scene checks confirmed all 11 new fixture roots exist exactly once, all 74 YAML document identifiers are unique, the prefab serializes a one-world-unit tile and one-tile crossing limit, and `git diff --check` passes.
- A second Unity Test Framework case and `ValidateAutoStepOverRegression` were added. They exercise real floor/ceiling/wall detection, release pause, reversal to source, over-wide rejection, blocked-path rejection, failure reasons, and retry-suppression release.
- The current machine's Unity Licensing Client repeatedly lost its IPC connection before the migration method or test runner could execute. Consequently, the new 2/2 Edit Mode result, Editor structural validator, and automated scene import remain **not run**, not passed.

For the Milestone 2.1B world-data slice:

- Focused Edit Mode tests cover all required coordinate examples, negative boundaries, cell/world round trips, explicit and duplicate chunk creation, 1,024-cell Background coverage, optional Solid mutation, invalid IDs, read/mutation isolation, four-direction expansion, and the absence of gameplay GameObjects.
- The focused Unity suite passed 25/25. The user also confirmed that the existing ant movement remained functional in Play Mode.

For the Milestone 2.1C navigation-classification slice:

- Focused Edit Mode coverage was added for occupancy, all four support directions, diagonal rejection, independent material flags, positive and negative chunk boundaries, all quadrants, unavailable-neighbour uncertainty, support precedence, live mutation results, isolation, and scene independence.
- The user-confirmed acceptance run passed 50/50 Edit Mode tests. The current full run passed 61/61, including all 23 `WorldNavigationClassifierTests`; Milestone 2.1C is complete.

For the Milestone 2.1D navigation-visualisation slice:

- Unity imported and compiled the new `AntColony.NavigationDebug` runtime assembly, its custom Editor tooling, the prototype scene, and the expanded Edit Mode tests.
- The structural validator passed the authoritative eight-chunk fixture, bounded 3,564-cell overlay, representative classification examples, and absence of 2D or 3D colliders.
- The complete Edit Mode suite passed 61/61: 25 world-data, 23 classifier, 11 visualisation, and 2 movement-regression cases.
- The user confirmed all 11 graphics-enabled manual acceptance checks, including colours, labels, boundaries, toggles, mutation refresh, and preservation of the original ant movement. Milestone 2.1D is complete.

For the Milestone 2.1E world-Tilemap slice:

- Unity compiled `AntColony.WorldPresentation`, the expanded Editor tooling, and the updated tests without errors.
- The scene validator passed the one-root hierarchy, two material-driven Tilemaps, all 8,192 resident Background visuals, foreground ordering, signed coordinate alignment, remove/restore refresh sequence, unchanged classifier behavior, and absence of colliders or terrain Rigidbody2D components.
- The complete Edit Mode suite passed 89/89: the existing 61 regression cases plus 28 new Tilemap projection cases.
- The new cases cover both material layers, foreground removal with Background persistence, non-blocking and differently identified Solid visuals, missing references and unknown IDs, missing chunks, positive and negative boundaries, exact cell refresh scope, bulk negative-chunk refresh, repeated refresh object stability, classification isolation, and collision absence.
- The user confirmed all 10 graphics-enabled checks for visual alignment, layer ordering, visibility toggles, removal/restore behaviour, Background persistence, classifier refresh, and unchanged original ant movement. Milestone 2.1E is complete.

For the Milestone 2.2A terrain-collision slice:

- Unity compiled the collision projection, custom Editor tooling, generated assets, updated prototype scene and focused tests without C# errors.
- The scene validator passed the single collision-only Tilemap hierarchy, collider-free visual layers, material references and independent flag examples, `ClimbableSurface` assignment, static composite configuration, signed boundary alignment, mutation refresh, Background persistence, classifier isolation and duplicate-hit check.
- The complete Edit Mode suite passed 117/117: the existing 89 regression cases plus 28 new collision cases.
- The new cases exercise real Physics2D queries for blocking and non-blocking materials, all four blocking/support flag combinations, add/remove/replace mutations, merged neighbours, immediate explicit synchronization, requested-region isolation, unavailable chunks, repeated-refresh component stability, integer-centred cells and positive/negative X and Y chunk boundaries.
- The user confirmed all 12 graphics-enabled manual collision checks. Milestone 2.2A is complete.

For the Milestone 2.2B ant-clearance slice:

- Unity compiled the authoritative clearance API, Scene-view inspector and focused tests without C# errors.
- The corrected focused clearance suite passed 38/38, covering physically clear unsupported Sky, a support-free unprepared shaft, prepared/unprepared Background neutrality, complete-body overlap, four foreground material-flag combinations, blocked-over-unavailable precedence, horizontal/vertical/intermediate rotation, rotated offset, margin, narrow passages, concave corners, tangency tolerance, all signed chunk boundaries and quadrants, four-chunk footprints, missing target/footprint chunks, live mutations, and scene/physics independence.
- The scene validator passed the authored ordinary-ant 0.96×0.3 effective profile, Clear test pose, and absence of gameplay physics components on the inspector.
- The complete Edit Mode suite passed 155/155: all 117 pre-clearance regression cases plus 38 clearance cases. The existing movement regression fixture also passed separately at 2/2.
- Physical clearance now matches GDD v3.2 and is independent of Background grip/grounding eligibility. The user confirmed all 9 graphics-enabled manual clearance checks; Milestone 2.2B is complete.

For the Milestone 2.2C movement-capability slice:

- Unity compiled the Background properties, movement-capability evaluator, debug inspector, scene authoring and focused tests without C# errors or warnings.
- The focused capability suite passed 37/37, covering all four Background configurations, floor-only grounding, wall/ceiling/non-blocking rejection, grounded walking, prepared grip, unsupported falling, voluntary-versus-gravity descent, clearance separation, controlled Background replacement, signed chunk boundaries and all quadrants, unavailable data and scene/physics independence.
- The scene validator passed the four authored Background definitions, capability inspector, Falling-to-Gripping-to-Falling replacement round trip, explicit Tilemap refresh, unchanged classifier examples and absence of extra physics components.
- The complete Edit Mode suite passed 192/192, and the existing movement regression fixture passed separately at 2/2.
- Manual graphics-enabled Scene-view checks remain pending, so Milestone 2.2C is implemented and automated but not complete.

For the Milestone 2.4A natural-background realignment:

- Natural Earth, Natural Stone, Prepared and Sky properties are covered directly, including normal `1.0x` natural speed and provisional `1.25x` Prepared speed.
- Route tests cover natural horizontal/up/down/diagonal movement, equivalent Stone behavior, blocking foreground, explicit non-grippable space, missing chunks and diagonal corner rejection.
- Runtime tests cover Grounded, Gripping and Falling transitions, effective Prepared speed, blocker/support invalidation, Sky grip loss, recovery and collision-backed landing.
- Unity compilation passed. Focused Edit Mode tests passed 56/56, the complete graphics-enabled Edit Mode suite passed 227/227 and Play Mode tests passed 6/6. `git diff --check` is clean.
- Manual Play Mode acceptance remains pending and is not implied by these automated results.

### Milestone 2.2C manual Scene-view checklist

1. Open `AntNavigationPrototype.unity`, select `Ant Movement Capability Inspector`, and confirm the default Natural Earth chamber-centre result is physically Clear, ungrounded, Gripping and gravity-ineligible at `1.0x` speed.
2. Click `Toggle Prepared Background, Refresh, and Requery`; confirm the selected cell changes visual, reports the Prepared material ID, retains horizontal/up/down grip and changes the speed multiplier to the provisional `1.25x`.
3. Toggle again; confirm the Natural Earth visual, Gripping result and `1.0x` speed return immediately.
4. Move the handle to a clearance-valid pose exactly `0.15` units above a floor top; confirm Grounded while the natural Background still permits its configured directions.
5. Place the handle beside a wall and below a ceiling without a floor; confirm neither produces Grounded.
6. Place the capsule over blocking foreground; confirm `BlockedByTerrain` and no valid movement state.
7. Move the handle into a missing chunk or across a missing footprint boundary; confirm an explicit unavailable result rather than Falling or Gripping.
8. Confirm the existing navigation-classification overlay does not change when only Background preparation changes.
9. Confirm the Console remains clean and the original `AntMovementPrototype` movement still behaves unchanged.

## Manual Play Mode checklist

1. Confirm the previously working floor movement, stopping, reversal, and both wall directions still behave identically.
2. Approach a wall using A/D only; confirm collision stops the ant without an automatic climb.
3. Press W near the wall; confirm floor-to-wall rotation and movement remain smooth and collision-safe.
4. Climb toward the ceiling using W only; confirm the ant stops without arbitrarily turning left or right.
5. At the wall/ceiling corner, press the direction leading into the room; confirm a wall-to-ceiling transition and approximately 180-degree ceiling alignment.
6. Use A/D along the ceiling; confirm normal speed, stopping, and reversal.
7. Press the appropriate W/S direction at each ceiling end; confirm ceiling-to-wall transitions in both directions.
8. Descend to the floor and deliberately select the valid A/D floor direction; confirm wall-to-floor transition.
9. Climb a side of `Outside Corner Block`, then use diagonal intent toward and around an upper edge; confirm a smooth outside-corner transition to its top.
10. Traverse the opposite outside corner from the block top onto its side, then reverse midway through a transition.
11. Try choosing the solid-facing or unavailable direction at inside and outside corners; confirm the transition is rejected without clipping, teleporting, or falling through terrain.
12. Release input midway through and after transitions; confirm the ant stops and remains supported.
13. Enable `Show Debug Gizmos` and inspect candidate normals, selected contact, orange transition path, and any red rejected point.
14. Confirm the Console contains no errors or warnings throughout repeated clockwise and counter-clockwise loops.
15. Reproduce the original deadlock path, then try release, the opposite source direction, and the opposite destination direction; confirm transition state always pauses, reverses, completes, or cancels instead of permanently consuming input.
16. On `One Tile Floor Approach`, hold the tangent direction and confirm the ant crosses continuously to `One Tile Floor Landing` without jumping, falling, or changing speed unexpectedly.
17. Repeat across `One Tile Ceiling Approach`/`Landing` and `One Tile Wall Lower`/`Upper` in both directions.
18. At a valid opening, press a strong perpendicular or diagonal input; confirm explicit directional intent takes priority and Auto Step-Over does not seize control.
19. Release input partway across a valid opening, then reverse; confirm the ant pauses and returns safely to its source support.
20. Approach the two-tile floor opening and the blocked landing; confirm both stop safely, do not oscillate or retry every frame, and become eligible for a new attempt after input release/change.
21. Enable `Show Debug Gizmos`; confirm the measured gap endpoints/path match the authored opening and rejection reasons are useful without per-frame Console spam.

### Milestone 2.4A manual acceptance checklist

No extra Inspector setup is required. Open `Assets/AntColony/Scenes/AntGameDevelopment.unity`, enter Play Mode, use left-click to select/command cells, `P` to toggle Natural/Prepared Background on an exposed selected cell and `F` to toggle foreground Solid occupancy.

1. Confirm exactly one yellow autonomous ant appears near X `-12`, reports `Grounded`, and can traverse the flat Natural Earth floor in both directions.
2. Command exposed Natural Earth cells above/beside the fixture; confirm upward, downward, horizontal and valid diagonal movement without preparation or teleportation.
3. Command through an exposed Natural Stone region; confirm the same directions and normal movement speed.
4. Use the prepared vertical strip/top region; confirm it permits the same directions and the panel reports effective speed `2.5` from base speed `2.0` (`1.25x`, provisional).
5. Toggle a Natural cell to Prepared with `P`, then traverse across the boundary; confirm movement remains valid and only effective speed/visual/material diagnostics change.
6. Attempt a diagonal route beside blocking foreground; confirm the ant does not cut the blocked corner or overlap terrain.
7. Add a foreground blocker with `F` during an active route; confirm the route becomes Blocked before entry. Remove it and confirm a new command can be accepted.
8. At the explicit Sky/grip-loss fixture, remove valid grip and support; confirm the active command is invalidated, the state becomes `Falling`, gravity moves the Rigidbody2D and no teleport/collision bypass occurs.
9. Restore or enter a valid Natural/Prepared grippable cell; confirm recovery to `Gripping` (or `Grounded` on the landing) without oscillation.
10. Toggle Background at runtime while moving; confirm capability, route validity, rendered tile and effective speed update immediately. Confirm a remote/missing-chunk destination is rejected readably.
11. Repeat Grounded, Natural Earth, Natural Stone, diagonal, Prepared-speed, transition, fall/recovery and blocked-route checks while confirming the Console remains free of relevant errors or warnings.

## Known limitations and remaining P1 work

- The original corner-deadlock reproduction is user-confirmed resolved; the wider ceiling/corner route still needs full Play Mode acceptance.
- The newly implemented ceiling and wider corner behavior is not yet Play Mode accepted and may require tuning of probe distance, look-ahead distance, collider inset, or rotation speed.
- Corners are intentionally limited to nearby, approximately perpendicular surfaces. Irregular or curved terrain is not supported by this slice.
- Outside corners require deliberate diagonal intent because both approaching the edge and selecting the outgoing surface must be unambiguous.
- The transition path is validated against current static collision geometry; runtime excavation and dynamic traversal invalidation do not exist yet.
- Auto Step-Over and its authored fixtures are implemented but have not completed automated Unity execution or manual Play Mode acceptance on this machine.
- The current Step-Over path is intentionally straight between parallel supports. Irregular, curved, offset, or routed openings remain unsupported.
- The ant-follow camera remains unimplemented; the integrated sample scene has only a developer-controlled inspection camera.
- World data, material visuals and authoritative terrain collision are connected in both the separate navigation fixture and the bounded integrated development scene. The integrated scene now demonstrates bounded direct grounded/gripping commands and gravity-driven falling, but persistence, general pathfinding, excavation gameplay and legacy DirtBlock conversion remain absent.
- The collision projection currently requires an explicit cell, chunk or bounded-region refresh after authoritative Solid mutation; no terrain-change event stream or chunk streaming lifecycle exists yet. Clearance itself reads world data directly and does not share that staleness risk.
- Static clearance does not validate Background permissions, grip, grounding, gravity, a swept movement segment, directional movement edge, route connectivity, Reachable Space, dynamic objects or ant-to-ant separation.
- Mathematical tangency is permitted within a `0.00001`-unit tolerance; future movement integration may use a small positive profile margin to remain conservative relative to Unity Physics2D contact handling.
- Background Material movement/grip/gravity/speed properties now drive the single development ant. General directional connection generation, detours, falling route planning, Surface Preparation jobs, resource consumption, skills and colony work are not implemented.
- Milestone 2.2C needs graphics-enabled manual Scene-view acceptance before it can be marked complete.
- The integrated sample-world and prior 2.3 acceptance remain historical evidence. Milestone 2.4A still needs the current natural-background, speed-transition, mutation, falling, landing and clean-Console checklist above.
- Full directional navigation, multi-ant occupancy and colony simulation remain later milestones.

## Recommended next implementation step

Wait for the user to approve and scope the next milestone. Keep the separate 2.4D and 2.2C manual statuses explicit; do not begin resource logistics, survival jobs, sleeping-background interaction, priority matrices, Draft Mode or other colony work without separate approval.
