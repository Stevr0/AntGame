# Ant Colony Development Roadmap

## WV-B3B — Implemented; manual acceptance pending (15 October 2026)

The approved authority-foundation slice is implemented. `RegionCellId` retains
the region and Colony/Surface coordinate type; immutable snapshots are served by
read-only Colony and Surface adapters. Exact committed Colony terrain/drop
changes advance only the affected cell revision and enter a bounded,
multi-consumer change journal. A pure compatibility validator supports
diagnostics and future transaction integration without rejecting current
gameplay actions.

`AntGameDevelopmentWorldHost` validates both region adapters before atomically
replacing the unified read-model pair. The view coordinator consumes the same
published `SurfaceWorldData`; camera switches never republish. Gameplay writers
and quantities remain in their pre-WV-B3B owners. Focused Unity evidence passes
**6/6 Edit** and **3/3 Play**; complete regressions pass **557/557 Edit** and
**77/77 Play**, all with zero failures, skips or inconclusive tests. Manual Unity
acceptance remains pending. WV-B3C, WV-B3D and WV-C1 are not started.

The smallest recommended WV-B3C slice is one authoritative mutable Node type
plus its exact-once Extraction-to-loose-output transaction behind the published
cell contract. It must not combine Placement migration or Surface gameplay.

## WV-B3A — Complete documentation gate (15 October 2026)

`UnifiedRuntimeCellSpecification.md` now defines the approved end-state,
authority boundaries, exact-once ownership transfers, deterministic generation
publication, compatibility migration and acceptance gates for shared Colony and
Surface runtime cells. WV-B3A changed documentation only; current runtime
authorities remain intact.

Candidate follow-ups are WV-B3B authority foundation, WV-B3C Node/loose-resource
integration and WV-B3D Placement migration. They are not automatically approved.
The recommended next slice is read contracts, revisions, central compatibility
and atomic region publication with read-only adapters; it must not move current
gameplay writes yet. WV-C1 remains deferred.

## WV-B2B — Complete; scheduler fairness follow-up accepted (15 October 2026)

The user accepted the generated seed-`2203` founding-colony run. Deterministic
generation, founding-queen movement, Soil/Stone Extraction, loose output,
separate Hauling, Construction staging/completion, first-Storage bootstrap,
later Storage delivery, colony expansion, view switching and entrance travel all
worked in the observed scenario. `WV-B2B-DEF-001`, `WV-B2B-DEF-002`,
`WV-B2B-DEF-003` and `WV-B2B-ARCH-001` are closed with their history retained
below. WV-B2B is **Complete**.

The queen's Day 8 death at Health 0, Hunger 0, Thirst 0 and Fatigue 100 records
the known absence of usable generated sustenance and natural rest. It is not a
proven hunger-only failure and does not reopen WV-B2B. WV-C1 remains deferred and
requires separate authorisation.

The post-closure scheduler correction replaces first-32 truncation with a
resumable deterministic full-registry pass bounded to 32 entries per autonomous
evaluation. Priority-before-path-cost, eligibility, Colony residency and
exclusive reservation remain authoritative. Appends join the active pass;
removal and priority changes restart safely; terminal/status changes are
revalidated. Scale coverage proves
1/2/8/32 evaluations for 32/64/256/1,024 entries. Automated evidence passes
**40/40 focused Edit**, **1/1 focused Play**, **551/551 complete graphics-enabled
Edit** and **74/74 complete Play**. The user confirmed scheduler fairness manual
acceptance on 15 October 2026, closing the follow-up without changing WV-B2B.

The current full-suite XML rerun supersedes the audit's missing-artifact concern
and closes `WV-B1-REG-001`. Other explicitly pending WV-B1 visual/manual defect
acceptance is unchanged.

## WV-B2B-ARCH-001 unified resource logistics — closure history

The following WV-B2B subsections preserve the implementation and pre-acceptance
record. The completion status above supersedes their pending/open wording.

The approved pre-closure correction replaces Construction self-supply with
**Resource Node → Loose Ground Resource → Hauling → Construction Site staging /
Material Storage → Construction**. Extraction is now the single skill and job
category for Soil, Stone, Seed, Twig and future node removal. Existing earned
Excavation experience/priorities are preserved in the same numeric slot; legacy
`Excavation*` names are compatibility seams only.

Hauling now has explicit Construction Site and Material Storage destinations,
prioritises unmet compatible staging, and owns source/destination reservations.
Construction is work-only and stays pending until all site requirements are
staged. This resolves the first-Storage bootstrap without free stock or prior
Storage. Surface resource interaction is still deferred; do not begin WV-C1.
Validation passes **82/82 focused Edit**, **9/9 focused Play**, **539/539 full
Edit** and **73/73 full Play**. Direct seed-`2203` Play Mode acceptance remains
required. Keep `WV-B2B-DEF-001/002/003` and
`WV-B1-REG-001` open until that acceptance is recorded.

## WV-B2B procedural generation foundation — implementation history (15 October 2026)

WV-B2B now provides frozen SplitMix64-derived `surface`/`colony` streams, unified
generated cells, whole-world validation, compatibility projection into
`ChunkedWorldData` and `SurfaceWorldData`, an explicit fixture/generated
scenario switch, one generated queen, temporary visuals and deterministic test
coverage. Unity assemblies compile through the Editor's Roslyn response files.
The defect-focused suite passed 12/12 Edit Mode; relevant construction,
excavation, procedural and WV-B1 classes passed 58/58 Edit Mode; World View
regressions passed 5/5 Play Mode. Manual Play Mode acceptance and
`WV-B1-REG-001` remain open. Do not begin WV-C1.

**Defect recovery implemented; acceptance remains open.**
`WV-B2B-DEF-001 — Founding Queen Job Starvation` now filters construction with
no obtainable physical material before autonomous selection, records an
unowned paused waiting reason, permits excavation/other eligible work, and
reconsiders construction when carried, stored or loose material becomes
reachable. `WV-B2B-DEF-002 — Generated Stone Excavation` advances the generation
identity to version `2` / `wv-b2b.default.002` and guarantees one real Stone
foreground face at `(-4,-8)` beside the starting chamber. Existing adjacency,
skill, transaction, Background-preservation and conservation rules are unchanged.
Both defects remain open until Unity automated and manual acceptance are
confirmed. `WV-B1-REG-001` remains outstanding, WV-B2B is not closed, and WV-C1
is not started.

`WV-B2B-DEF-003 — Founding Colony Storage Bootstrap` remains **Implemented —
Pending Manual Acceptance** and is now corrected by `WV-B2B-ARCH-001`.
Destination-aware Hauling can reserve an unmet Storage construction site's
staging without completed Storage, then Construction performs only the supplied
work. Seed-`2203` focused evidence is included in the current 82/82 Edit Mode
suite. Preserve `WV-B2B-DEF-001`, `WV-B2B-DEF-002`, `WV-B2B-DEF-003` and
`WV-B1-REG-001`; do not close WV-B2B or start WV-C1 before manual acceptance.

## WV-B2A procedural generation specification — complete documentation gate (15 October 2026)

WV-B2A documents the approved shared-seed generation contract for separate
Surface and Colony regions in
`ProceduralWorldGenerationSpecification.md`. It specifies deterministic named
random domains, bounded dimensions, coordinate/chunk ownership, weighted and
clustered terrain, the top-boundary entrance, one starting chamber and founding
queen, source records, validation/correction, runtime boundaries, fixture
preservation and future acceptance tests. No Unity runtime or asset content was
changed.

The historical recommended next implementation was **WV-B2B**; it is now
implemented with validation still pending. Its approved scope used the
remaining numerical/configuration choices: a pure bounded `64×64` Surface and
recommended `128×128` Colony builder/validator, existing Soil/Stone materials,
one protected entrance/chamber and an explicit generated-versus-development
scenario selector. WV-B2B must stop before resource interaction, foraging,
hauling, persistence, streaming or autonomous cross-view routing. WV-B1 manual
acceptance and `WV-B1-REG-001` remain independently open and must not be
relabelled as passed by this documentation work.

### WV-B2A unified-cell amendment — complete documentation gate

The approved logical model now gives every Surface and Colony cell one natural
Background plus optional Solid/Resource Node, Construction/Placement and
one-type loose stack. Generated records now implement that shape; existing functional Background
conversions, quantity-one implicit Solids, bounded Surface obstacles and
multi-stack world drops require adapters/migration rather than silent rewrites.

The minimum WV-B2B slice is limited to unified generated Background/node records,
empty placement/loose slots, deterministic IDs, structural validation, separate
region adapters and scenario selection. Shared extraction and strict loose
occupancy are required before WV-C1 interaction. Functional-Background-to-
placement migration is required before construction is enabled in the generated
scenario. Resource decay and broader migration remain deferred.

## WV-B1-DEF-002 cross-region rendering leakage — correction awaiting manual acceptance (15 October 2026)

The Surface camera inherited an all-layers mask and Colony IMGUI overlays continued drawing through the disabled Colony camera. The correction assigns runtime Surface presentation and Surface-resident ant renderers to a dedicated `SurfaceWorld` layer, gives the two cameras mutually exclusive Surface visibility, and gates Colony overlays and Colony-only World/Debug controls by active view without disabling any authority or simulation. Focused regressions cover layer/mask ownership, repeated switching, dynamic designation persistence, input context, unchanged state and bidirectional ant visual handoff. The affected assemblies compile with Unity Roslyn; focused WV-B1 Edit Mode passed **9/9** and focused Play Mode passed **4/4**, all with zero failures or skips. An initial restricted Play runner attempt crashed in `BuildReportRestService` before discovery, but the unrestricted rerun completed and produced passing XML. The documented manual Play Mode checklist remains pending.

WV-B1 remains open for its separately recorded visual/manual defects. Preserve
`WV-B1-DEF-001` entrance-reuse manual acceptance. `WV-B1-REG-001` is now closed
by the current 551/551 graphics-enabled Edit Mode rerun. Do not begin WV-C1 or
later World View milestones without separate approval.

## WV-B1 entrance reuse defect — correction awaiting validation (14 October 2026)

The entrance correctly released capacity, but completed local movement did not update residency occupancy, leaving a moved Surface or Colony ant recorded on the arrival cell. The coordinator now synchronises completed same-region movement through a region-preserving residency API; genuine occupied arrivals still fail safely, and released transaction IDs cannot be replayed. Focused Edit and Play regression cases were added and all affected assemblies compile. Unity test execution and the repeatable two-ant Play Mode checklist remain pending because an interactive Editor owns the project lock. WV-B1 stays open; no WV-C or later work is authorised.

## WV-B1 implementation — awaiting validation (14 October 2026)

WV-B1 is implemented as the first runtime slice of the approved two-view architecture. It provides distinct `ColonyCell`/`SurfaceCell` destinations and stable region/entrance IDs; one authoritative ant residency per `AntId`; a deterministic bounded `64×64` Surface with fixed obstacles and a `32×32` future partition seam; dedicated top-down Surface A* and kinematic movement; two independently retained cameras with exclusive input contexts; and one bidirectional, capacity-one physical-approach entrance transaction with pause guard, revision guard, idempotence and rollback.

Focused verification passed **8/8 Edit Mode** and **2/2 Play Mode**; the complete Play Mode regression passed **69/69**. A complete headless Edit Mode regression attempt reached an existing rendered-pixel test path and the Unity process crashed after `Failed to set the active render target`; it produced no complete result and is not counted as passed. Manual Unity Play Mode acceptance also remains pending, so the milestone status is **Implemented — Awaiting Validation**.

Next action is WV-B1 acceptance only: execute the checklist in `CurrentGameState.md`, rerun broad graphics-enabled regressions where available, and correct any WV-B1 defect found. Do not begin WV-C1/C2 resources, WV-D cross-view autonomy, streaming, unloaded simulation or further entrances without approval.

## WV-A technical specification complete — documentation baseline (14 October 2026)

`WorldViewTechnicalSpecification.md` now defines the approved typed region and coordinate contracts, one-ant entrance transaction, ant residency/representation handoff, manual view/camera lifecycle, local-navigation boundaries, shared-clock behavior, finite seed/freshwater records, direct consumption, extraction conservation, one-unit recoverable water payload, natural rest, founding-queen bootstrap, failure recovery and acceptance strategy. Read-only inspection confirmed useful existing seams and current single-Colony assumptions. No Unity scripts, scenes, prefabs, ScriptableObjects, terrain, art bindings or tests were changed.

WV-A does **not** authorise implementation. The recommended next scope is **WV-B1: typed two-region foundation, one bounded deterministic Surface fixture, manual view switch, one capacity-one entrance and one round-trip ant transition**, with both regions loaded on the accepted clock. Stop after WV-B1 acceptance before WV-C1 natural survival or WV-C2 extraction/transport.

## Approved WV-A entry contract — 14 October 2026

**WV-A was authorised as a DOCUMENTATION-ONLY TECHNICAL SPECIFICATION and is now complete as documentation, not runtime work.** Approved decisions: one pre-authored bidirectional one-ant-at-a-time entrance; manual Colony/World camera switching with separate remembered cameras; ants physically approach entrance before transition; finite seeds and freshwater near the entrance; provisional one-unit liquid payload using existing inventory accounting; safe natural resting with constructed Sleeping backgrounds faster. These decisions do not authorise Unity scenes, scripts, prefabs, world generation or movement changes.

**Completed WV-A deliverables:** typed region/local coordinate contract and entrance endpoint mapping; source/destination transition validation, atomic ownership handoff, capacity and retries; manual view-switch/input/camera behaviour; one clock and simultaneously loaded starting regions; local vs cross-region navigation responsibilities; source discovery, extraction, direct consumption and exact physical seed/water accounting (including a non-lossy liquid cache/transfer policy); safe natural resting definition; deterministic failure/test cases; decision register and staged WV-B1 / WV-C1 / WV-C2 acceptance criteria.

**Later:** WV-B constructs only one small bounded surface with one entrance and same-ant round trip; WV-C1 introduces finite natural consumption/discovery; WV-C2 introduces extracting and hauling surface resources including water, subject to WV-A approvals. Preserve Colony View and development fixture baselines. Brood, broad streaming/unloaded simulation, hazards and more entrances remain deferred.


## Approved founding-colony survival specification — 14 October 2026

**Design agreed; not implemented.** New-game start will be one founding queen in
one small excavated underground chamber without prebuilt
Food/Water/Sleeping/Material Storage or Nursery areas. Natural food/water sources
permit direct eating/drinking, and safe natural ground permits baseline rest.
Constructed storage/sleeping improves organisation and efficiency; functional
Background 1:1 capacities and later room-quality enhancements remain. Foraging
is discovery; extraction generates loose physical output; hauling transports it.
Keep three-ant development fixtures unchanged.

**Prerequisites for playable founding start:** WV-B2A now specifies the generated
region/entrance/chamber/source-record contract. Finite source interaction, food
extraction and transport, physical water accounting, natural-rest balance and
brood bootstrap still require their separately authorised WV-B2B/WV-C/later
milestones. The implemented WV-B1 World View foundation does not authorise those
systems.


## Focused survival-needs balance correction — 14 October 2026

**Implemented — Pending Manual Acceptance.** Investigation found no navigation, retry, pause, clock-speed or excavation-order defect. A production-API, real-path-cost 12-day projection showed the old provisional survival balance left only 42.7% of time for productive work plus return-to-work travel and allowed all three needs to reach their critical extremes. The dominant cost was frequent Fatigue-driven sleep trips, compounded by faster Hunger/Thirst loss and one-hour consumption actions.

Final provisional values are Hunger **-1/hour**, Thirst **-1.4/hour**, awake Fatigue **+1.4/hour**, rest **-16 Fatigue/hour**, Eat/Drink **0.5 hour**, interventions at Hunger **40**, Thirst **40** and Fatigue **70**, with restores and all critical thresholds/consequences unchanged. The original 1.5 Thirst/hour and 2 Fatigue/hour candidates failed route-aware safety/productivity checks and were reduced to 1.4 from measured evidence. The revised projection reaches **70.7%** productive work/return travel, consumes **17 food / 21 water**, and remains at minimum Hunger **25.8**, minimum Thirst **10.2**, maximum Fatigue **87.9**.

Finite reachable development supply is now **24 food / 32 water** instead of 12/16, held in the same four reachable storage fixtures. Three sleeping positions and the unreachable failure fixture remain unchanged. Verification passed **87/87 focused Edit**, **18/18 focused Play**, **520/520 complete graphics-enabled Edit**, and **67/67 complete Play**. The complete Edit rerun includes the user-confirmed intentional `Colony_R05_C05` Solid Stone / `Colony_R05_C03` Stone Background mapping. Detailed evidence and manual acceptance steps are recorded in `CurrentGameState.md`. This correction does not start a new gameplay milestone and leaves Milestone 3.5A **Implemented — Pending Manual Acceptance**.

## 3.5B implementation status — 14 October 2026

**Implemented — Pending Manual Acceptance.** Extraction completion converts eligible foreground Soil/Stone into an exact-once recoverable loose drop at the authoritative cell. It never fills inventory or a destination. Existing same-cell merge/stack bounds preflight the yield; an unrepresentable output leaves terrain unchanged. Under WV-B2B-ARCH-001, Hauling separately supplies Construction staging or Storage and Construction is work-only.

Verification passed **96/96 focused Edit Mode**, **14/14 focused Play Mode**, **514/514 complete graphics-enabled Edit Mode** and **67/67 complete Play Mode**, all with zero failures/skips. The 27-step Unity manual checklist in `CurrentGameState.md` remains pending. Milestone 3.5A remains separately **Implemented — Pending Manual Acceptance**; its visuals, mapping and interaction geometry were not changed.

The smallest next step is manual 3.5B acceptance together with the still-separate 3.5A visual checks. Do not expand into World View, new Foraging AI, new extraction types, Dump Zones, clutter penalties or another hauling scheduler.

## 3.5A implementation status — 14 October 2026

**Implemented — Pending Manual Acceptance.** The 3.5A acceptance corrections provide explicit camera-aligned full-cell fills and borders for hover, live drag and persistent pending/active excavation and construction designations. The alignment follow-up now projects the authoritative integer-centred cell bounds (`centre ± 0.5`) instead of incorrectly treating the cell coordinate as its lower-left corner. This matches the authored one-unit Tilemap Grid offset by `(-0.5, -0.5)` at every camera position, zoom and supported resolution. UI-blocked pointer entry clears and abandons a drag. Cancelled stable cell jobs can be replaced repeatedly, reservations are released, completed excavation remains terminal, and repeated construction cancellation retains exact staged-material recovery. The accepted custom Soil/Stone Tilemap mapping and all gameplay timings, costs and authorities are unchanged. Prepared/functional Background art, decoration, composite-cavity separation and seamless autotiling remain deferred. Milestone 3.4E congestion recovery remains explicitly parked.

Alignment verification: focused presentation Edit Mode **18/18**, complete graphics-enabled Edit Mode **513/513**, and complete Play Mode **67/67** passed. Projection coverage includes the three requested resolutions, camera pan, and minimum/default/maximum zoom. Sprite inspection confirmed centred pivots, 32 PPU, 32×32 source rects and a full-cell Soil alpha footprint, so no art mapping changed. Manual visual/gameplay acceptance remains pending.

The smallest subsequent presentation milestone should be a manual seam/readability acceptance and, only for failed mappings, production-ready independent Background/Solid replacement art. It must not expand into World View runtime, surface movement or new gameplay systems.

## 3.4C manually accepted — 14 October 2026

**COMPLETE — 3.4C Player Colony HUD & Developer UI Separation.** User confirmed 13/13 grouped HUD, colony gameplay, input and resolution manual checks and clean Console. Codex-reported full suites passed 489/489 Edit and 62/62 Play. Existing uGUI player HUD and optional F12 developer IMGUI are accepted; World View runtime has not begun. Older pending-acceptance text is historical.

**Next:** Separate manual visual acceptance for the art foundation remains outstanding. Further Colony View UX work or World View technical specification (WV-A) requires separately agreed scope.


## Parallel art foundation — 14 October 2026

**Implemented — Pending Manual Acceptance.** Custom Colony and World source sheets are preserved separately and a repeatable tool prepares 32-pixel candidate libraries with Unity import metadata. This does not replace current development visuals, create World View gameplay or alter terrain behavior. Next art step: complete the documented Unity Sprite Editor and edge-repeat review, then approve semantic layer separation before creating production Tile assets and separate Colony/World palettes.

## 3.4D implementation status — 14 October 2026

**3.4B — ACCEPTED.** Unified simulation clock and gameplay mode passed user manual acceptance: 20/20 grouped checks, normal gameplay, pause/resume, developer overrides and clean Console. Earlier Codex-reported tests: 11/11 focused Edit, 7/7 focused Play, 485/485 complete Edit (graphics-enabled), 58/58 complete Play. No rerun for these docs. Any older 3.4B pending status below is superseded.

**3.4C — ACCEPTED.** The user confirmed manual acceptance before authorising 3.4D. Its recorded automated evidence remains 12/12 focused Edit, 13/13 focused Play, 489/489 complete graphics-enabled Edit and 62/62 complete Play.

**3.4D — IMPLEMENTED, PENDING MANUAL ACCEPTANCE: Colony Management & Work Priorities UI.** One global scrollable matrix on the existing HUD lists active authoritative ant records, current activity and mapped skills. Only real autonomous executors are functional columns: Excavation, Construction and Hauling. Priority 1–4/Disabled changes write the existing per-ant `AntWorkPrioritySet`, affect subsequent autonomous selection, preserve active work and remain shared with developer diagnostics. Verification passed 7/7 focused Edit, 7/7 focused Play, 492/492 complete Edit and 65/65 complete Play.

**Smallest next step:** complete the 18-step 3.4D Play Mode checklist and address only observed matrix usability or layout defects. After acceptance, scope multiple-ant local avoidance and congestion recovery separately; do not begin World View runtime work.


## Accepted baseline and UI transition roadmap — 14 October 2026

**3.4A — ACCEPTED:** User completed and accepted manual Play Mode testing of Functional Background Conversion & Construction Jobs, including inventory-capacity recovery and storage-first/ground-drop fallback. Codex-reported updated full regressions were 474/474 Edit Mode and 51/51 Play Mode. Historic pending-acceptance text below is superseded by this status.

**3.4B — IMPLEMENTED, PENDING MANUAL ACCEPTANCE: Unified Simulation Clock & Gameplay Mode.** The development scene now begins at Day 1 08:00 paused, uses one authoritative pause/1×/2×/3× clock, and activates survival, colony work and hauling as one normal-gameplay bundle. Physics, navigation scheduling, autonomous evaluation, retry time, needs, skills, interactions, work and transfers stop without cancelling preserved state. Orders, designation/cancellation, priorities, selection and camera control remain available while paused. A compact independent time strip supports acceptance; the existing seven-category developer toolbar retains explicit Follow/Forced On/Forced Off autonomy overrides. Verification passed 11/11 focused Edit, 7/7 focused Play, 485/485 full graphics-enabled Edit and 58/58 full Play.

**3.4C — ACCEPTED: Player Colony HUD.** The authored uGUI Canvas provides authoritative roster/inspection, global time controls, physical resource ownership, job feedback, and Colony View excavation/conversion/cancellation. Existing IMGUI development tooling remains separate and hidden by default. Mara, Pip and Tala remain the temporary test population. Alerts and contextual menus remain deferred; the work-priority matrix is now implemented by 3.4D.

**Subsequent scoped milestones:** All-ant work priorities, information/alerts/storage UX, contextual right-click work orders and incremental controller responsibility separation. Keep doors, rooms, brood, production, combat and larger game features deferred. The approved eventual starting ant remains one founding queen; three ants are only the development fixture.

## Approved future milestone family — Colony View and World View

**Design status: approved direction; no World View milestone is implemented or authorised for coding.** Colony View is the existing side-on underground game. World View is a future top-down, continuous chunk-based surface game connected through nest entrances. Both views share persistent ants and one simulation timeline. Detailed scope and acceptance criteria must be approved separately for every phase below.

### Prerequisites

Before the first playable World View slice:

- preserve the existing Colony View terrain, movement, construction, logistics and simulation-clock baselines;
- keep ant identity, stats, skills, needs, inventory and jobs independent from a particular visual or movement representation;
- preserve the WV-A typed region/coordinate and capacity-one entrance transition contract;
- preserve one authoritative residency and at most one live representation per ant;
- keep separate local-navigation responsibilities behind the specified cross-view composition boundary;
- implement the specified loaded-region residency rules before designing WV-F persistence for unloaded surface chunks; and
- keep the first surface course small enough to validate travel, return and conservation before expanding generation or population scale.

### Proposed phased milestones

- **WV-A — Two-view technical specification:** Documentation complete. No runtime implementation or Unity acceptance claim.
- **WV-B1 — Two-region and entrance vertical slice:** Proposed next approval. Typed locations/residency, bounded Surface fixture, Surface-local movement, manual cameras/input and one safe bidirectional transition. No resources or autonomous cross-view work.
- **WV-C1 — Founding survival:** Finite source discovery, direct eating/drinking and safe natural rest without starting facilities.
- **WV-C2 — Physical return loop:** Seed extraction/parcels, one-unit recoverable liquid payload, hauling and constructed Food/Water Storage.
- **WV-D — Autonomous cross-view routing/work:** Compose source local route, entrance edge and destination local route without replacing Colony A*.
- **WV-F — Unloaded-region simulation:** Reduced-fidelity deterministic catch-up and reconciliation only after loaded two-region gameplay is accepted.

| Phase | Proposed outcome | Depends on | Explicitly deferred |
| --- | --- | --- | --- |
| **WV-A — Two-view technical specification** | Approve region identity, coordinate-space boundaries, entrance mapping, transition invariants, simulation residency and validation strategy. Documentation/prototypes only unless separately authorised. | Current persistent ant identity and unified simulation clock | Playable surface, procedural expansion, enemies |
| **WV-B1 — Small playable surface and entrance** | One bounded top-down surface area, one known entrance, manual camera/view switching, Surface-local traversal and one same-ant round trip. Colony View continues simulating. | WV-A; stable Colony View regressions; single-owner ant representation | Natural resources, general streaming, autonomous cross-view jobs, threats |
| **WV-C1 — Founding survival** | Finite source discovery, direct eating/drinking and safe natural rest without constructed facilities. | WV-B1; approved source and rest balance | Extraction, hauling, large biomes, combat |
| **WV-C2 — Physical surface return loop** | Recoverable seed/water extraction, one-unit liquid payload, hauling and constructed Food/Water Storage. | WV-C1; approved output and transfer presentation | Production ecology, combat, multiple colonies |
| **WV-D — Autonomous cross-view work** | Jobs can select and execute composed routes through entrances while preserving ownership, reservations, needs and cancellation semantics. | WV-C2; cross-region route and failure policy; scheduler budgets | Unbounded job search, global path graph assumptions |
| **WV-E — Incremental chunk expansion** | Add surface chunks/environments gradually, with persistence and bounded loading around the initial playable area. | WV-D; chunk save/version rules; performance evidence | Whole-world generation in one step |
| **WV-F — Lightweight unloaded simulation** | Deterministic reduced-fidelity progress for ants/jobs in unloaded chunks, reconciled safely when chunks reload. | WV-E; approved fidelity/catch-up rules and save support | Full offscreen physics or frame-by-frame navigation |
| **WV-G — Surface threats and wider ecology** | Incremental predators, rival colonies and environmental interactions after exploration/logistics are stable. | Earlier World View phases plus separately approved combat/ecology design | Any unapproved enemy or combat scope |

The phases intentionally begin with one small playable surface and expand outward. They do not require Colony View and World View to share one terrain schema or one local pathfinding graph; they require shared ant truth, explicit transitions and compatible job/navigation contracts.


## Current milestone — 3.4A Functional Background Conversion & Construction Jobs

**Implemented — Pending Manual Acceptance (13 October 2026).** The accepted shared job registry, priority matrix, grid navigation, ant inventory, world drops and Material Storage now drive real Construction jobs. A designated exposed Background is supplied personally by its reserved builder, staged physically at the site, worked over time with Construction skill and Rested modifiers, then committed through an accounted Background-only transaction. Prepared, Nursery, Sleeping, Food Storage, Water Storage and Material Storage conversions use provisional one-unit Soil/Stone recipes and four-hour base durations.

**Inventory-capacity recovery correction:** a builder whose slots are full now retains construction material and unloads unnecessary stacks through a reusable task-owned operation. It physically deposits at the closest reachable compatible reserved storage, falls back to a conserving traversable world drop when storage is unavailable, and resumes the same construction job. Recovery uses bounded attempts and safe interruption without changing construction progress or ownership. Approved future loose-resource clutter penalties remain design-only; navigation costs and room scoring are unchanged.

Focused verification passed **19/19 Edit Mode** and **4/4 Play Mode** after the inventory-capacity correction. Complete regression verification passed **474/474 graphics-enabled Edit Mode** and **51/51 Play Mode**. The exact 30-step manual checklist is in `CurrentGameState.md`. Do not mark 3.4A complete until the user confirms it. Doors, rooms, brood, production, crafting, demolition and reproduction were not started.

## Accepted milestones and next development scope — 13 October 2026

**Development UI Reorganisation COMPLETE:** User confirmed 24/24 manual checks and clean Console. Earlier reported 455/455 Edit Mode and 47/47 Play Mode passed.

**Milestone 3.3D COMPLETE:** User confirmed 30/30 manual hauling checks and clean Console. Earlier reported 447/447 Edit Mode and 45/45 Play Mode passed.

**Current Milestone 3.4A — Functional Background Conversion & Construction Jobs:** implemented as described above; manual acceptance is pending.


## Development UI reorganisation — 13 October 2026

**COMPLETE — 24/24 manual checks accepted. Not Milestone 3.4A.** The two independent side panels were replaced by a compact top IMGUI toolbar with mutually exclusive Ant, Needs, Skills, Jobs, Resources, World and Debug categories. One bounded scrollable panel is possible at a time; Hide All leaves only a small reopen control. Existing simulation controls and diagnostics were regrouped without changing their gameplay APIs.

Focused verification passed **8/8 Edit Mode** and **2/2 Play Mode**. Complete regression verification passed **455/455 graphics-enabled Edit Mode** and **47/47 Play Mode**. The exact 24-step manual interface checklist is in `CurrentGameState.md`.

Milestone 3.3D is **COMPLETE — 30/30 manual checks accepted**.

## 3.3D implementation status — 13 October 2026

**3.3D Material Storage & Autonomous Hauling: COMPLETE — 30/30 manual checks accepted.** Milestone 3.3C is COMPLETE: the user confirmed all 26 manual Unity checks, and its previously reported 26/26 focused Edit, 5/5 focused Play, 432/432 full Edit and 42/42 full Play evidence remains accepted.

3.3D adds one-slot-per-tile physical Material Storage, Soil/Stone/general filters, source and destination reservations, actual-path-cost destination choice, partial loads, automatic transport of loose resources and owner-only self-deposit. Completed deliveries award provisional Hauling XP at 5 base XP per unit. Cancellation preserves the resource at its current owner. Construction and other later mechanics remain excluded.

Verification passed **15/15 focused Edit Mode**, **3/3 focused Play Mode**, **447/447 complete graphics-enabled Edit Mode** and **45/45 complete Play Mode**, all without failures or skips. The user subsequently accepted all 30 manual checks.


## 3.3C implementation status — 13 October 2026

**3.3C Ant Inventory, Automatic Excavation Pickup & Physical Material Handling: COMPLETE — manual 26/26 accepted; auto-pickup superseded by 3.5B.** Each authoritative ant record owns independent stack-based inventory. Hauling levels 0/20/40/60/80 grant one through five natural slots; Soil and Stone provisionally stack to 10. Capacity decay retains occupied overflow without permitting further additions. The historical excavation inventory/split rule is no longer current; 3.5B always produces a loose drop. Selected ants can still reserve and pick up a reachable drop or use Developer Only controls to drop a chosen quantity at their current valid anchor. Compatible world stacks merge, partial transfer is conserved, failures preserve the source and stable output keys prevent retries from duplicating resources.

Automated verification passed **26/26 focused Edit Mode**, **5/5 focused Play Mode**, **432/432 complete graphics-enabled Edit Mode** and **42/42 complete Play Mode**, all with zero failures/skips. The user subsequently accepted all 26 manual checks. At the 3.3C boundary autonomous hauling and Material Storage were absent; 3.3D now implements that narrow follow-up. Construction and broader resource scheduling remain absent, while stack limits, fixture quantities and carrying balance remain provisional.

**Completed follow-up:** 3.3D reuses these physical ownership, reservation and transfer boundaries without beginning construction or creating a global resource wallet.


## 3.3B implementation status — 13 October 2026

**3.3B Real Excavation Designations & Physical Material Output: COMPLETE (all 26 manual Play Mode checks confirmed on 13 October 2026).** Excavate mode creates real Excavation jobs in the accepted registry from eligible foreground Solids, including bounded area drags and deterministic duplicate/cancellation handling. Priority/path-cost selection, exclusive reservation, shared navigation, adjacent work, partial progress, Excavation XP, Rested speed and survival/player interruption reuse the 3.3A foundation.

Completion uses a narrow transaction: validate the authored Solid output, prepare a stable job-keyed loose drop, remove foreground only, commit the non-fallible drop record, refresh the single visual/collision cell and invalidate local navigation. Failure leaves terrain intact; retry cannot duplicate output; permanent Background is preserved. Soil and Stone provisionally yield one matching unit. All output is temporarily a world drop because inventories and hauling are excluded.

Verification passes **14/14 focused Edit Mode**, **3/3 focused Play Mode**, **406/406 complete graphics-enabled Edit Mode** and **40/40 complete Play Mode**, all with zero failures/skips. The exact 26-step manual checklist is in `CurrentGameState.md`.

**3.3A Colony Job Designation & Priority Foundation: COMPLETE.** The user confirmed all 22 manual Unity Play Mode checks on 13 October 2026.

**Next smallest milestone after acceptance:** define and implement the narrow ant inventory/pickup boundary required by the already-approved “excavator collects if capacity permits; otherwise drops” rule. Do not begin hauling, storage or construction in that slice without separate approval.

## 3.3A acceptance and proposed excavation milestone — 13 October 2026

**3.3A Colony Job Designation & Priority Foundation: COMPLETE.** The user confirmed all 22 manual Unity Play Mode checks. Previously reported focused Edit Mode 28/28, focused Play Mode 5/5, full Edit Mode 392/392, full Play Mode 37/37 and documentation validation passed. This documentation update did not execute tests.

**Next proposed milestone 3.3B — Real Excavation With Physical Material Output:** Translate player excavation designations into real existing job-registry jobs; ant reaches adjacent safe work position, progresses using skill/Rested and safe interruption, and removes an eligible foreground Solid only in a resource-conserving transaction that also creates recoverable physical output. Reuse shared navigation and authoritative world mutation/projection; do not add hauling, inventory, construction or other jobs yet. Exact output recipes/yields remain provisional. Implementation separately authorised by user.


## Latest accepted milestone and next proposed scope — 13 October 2026

**3.2C Autonomous Survival Behaviour: COMPLETE.** User confirmed all 20 manual Play Mode acceptance checks. Previously reported focused 24/24 Edit Mode, 6/6 Play Mode and full 364/364 Edit Mode, 32/32 Play Mode passed, with the focused Edit Mode suite rerun after final diagnostic cleanup. No new test execution in this documentation-only change.

**Next proposed milestone 3.3A — Colony Job Designation & Priority Foundation:** Introduce per-ant work matrices (1 highest through 4 lowest, plus Disabled), simple synthetic colony jobs, priority-before-nearest-reachable assignment, exclusive claims, partial progress, and safe survival/player-order arbitration. Keep physical excavation, construction, material hauling, brood and Draft Mode excluded. Must be separately approved before coding.


## Milestone status — 13 October 2026

**3.3A Colony Job Designation & Priority Foundation: COMPLETE.** Each `AntRecord` owns stable Excavation, Surface Preparation, Construction, Hauling, Foraging, Production, Combat and Brood Care priorities using 1–4 or Disabled. A shared plain-C# registry owns synthetic designation identity, status, exclusive reservation and persistent progress. Selection uses highest enabled priority before actual shared-A* travel cost and deterministic ties. The 15 October fairness follow-up pages across the complete registry at 32 entries per evaluation; genuine failures retain 2/4/8-second retry backoff.

Synthetic work travels through the production grid motor, works from a revalidated adjacent position, preserves progress across safe interruption and awards proportional relevant-skill XP only for committed progress. Provisional balance is 4 simulation hours, +0.5% speed per skill level and 20 base XP per full job; Rested multipliers apply. Ordinary survival waits for active work, while critical survival and explicit player orders interrupt at the cancellation boundary and release a Paused job. Five deterministic fixtures and Developer Only controls/diagnostics are present. No real excavation, preparation, construction, hauling, resource or inventory behavior is implemented.

Verification passes **28/28 focused Edit Mode**, **5/5 focused Play Mode**, **392/392 complete graphics-enabled Edit Mode** and **37/37 complete Play Mode**, all with zero failures/skips. The exact 22-step manual checklist is in `CurrentGameState.md`.

**3.2C Autonomous Survival Behaviour: COMPLETE (acceptance recorded 13 October 2026).** The user confirmed the implemented individual Eat/Drink/Rest decision slice by approving the transition to 3.3A. Its thresholds, hysteresis, player-order rules, least-cost physical destination choice and bounded retry behavior remain accepted.

**Historical proposal — 3.2A Individual Needs & Survival Foundation.** This proposal has since been implemented and accepted; the retained text below is historical context.



**3.1C Individual Skills & Learn-by-Doing Foundation: COMPLETE (all 18 manual Play Mode checks accepted on 13 October 2026).** Each authoritative ant record owns all seven stable GDD skills from level 0–100. Successful future activities can explicitly award base XP through a validated multiplier boundary; a deterministic provisional curve handles level progress and safe maximum capping. Independently tracked inactivity uses explicit simulation hours, a provisional 24-hour grace period and 0.25 levels/hour decay above 50, with an exact floor of 50. The selected-ant panel displays every skill and contains collapsible Developer Only XP, multiplier, maximum, decay and reset controls. No actual jobs, work effects, needs, inventory or save/load were added.
**Historical proposal — 3.2C Autonomous Survival Behaviour:** This proposal has since been implemented and accepted as recorded above.



**3.2B Physical Food, Water & Rest Interactions: COMPLETE (all 20 manual Play Mode checks accepted on 13 October 2026).** Three authored 1:1 functional Backgrounds now provide Food Storage, Water Storage and Sleeping without Prepared speed. A narrow location-owned store tracks stable food/water IDs, integer quantities and explicit stable-ant reservations independently of Tilemap visuals. Context right-click reserves a unit/bed, chooses the least-cost reachable adjacent anchor with the shared A*, travels through `GridNavigationMotor`, revalidates on arrival, performs timed Eat/Drink/Rest, commits needs/resources and returns to Autonomous / Idle. Cancellation, failure, replacement, inactive/deceased state and world reset release reservations. Deterministic fixtures provide two accessible food stores, two accessible water stores, three sleeping positions and one surrounded unreachable food target.

Provisional values: Eat consumes 1 unit after 1 simulation hour and restores 40 Hunger; Drink consumes 1 unit after 1 simulation hour and restores 50 Thirst; Rest retains the accepted 12 Fatigue/hour recovery and completes at the accepted Fatigue 20 Rested qualification. Focused verification passes **25/25 Edit Mode** and **5/5 Play Mode**; complete graphics-enabled Edit Mode passes **340/340** and complete Play Mode passes **26/26**, all with zero failures/skips. The exact 20-step manual checklist is in `CurrentGameState.md`. Do not mark 3.2B complete until the user confirms it.

**3.2A Individual Needs & Survival Foundation: COMPLETE (manual acceptance confirmed 13 October 2026).** Each authoritative ant record owns Health, Hunger, Thirst, Fatigue and Rested qualification. Explicit elapsed-hour simulation depletes Hunger/Thirst, adds awake Fatigue, applies threshold-crossing starvation damage, and exposes deterministic rest recovery. Critical Thirst supplies a zero effective multiplier to the accepted skill API; qualified Rested state exposes fading work-speed and XP multipliers without changing movement. The selected-ant panel displays all values/warnings and contains collapsible Developer Only simulation, recovery, critical-condition and reset controls. No physical supplies, sleeping places, survival jobs or actual death gameplay were added.

Provisional values: Health/Hunger/Thirst/Fatigue use 0–100; new ants start 100/100/100/20; Hunger loses 2/hour, Thirst 3/hour and awake Fatigue gains 4/hour; rest recovers 12 Fatigue/hour; Hunger at or below 10 removes 2 Health/hour; Thirst at or below 10 blocks XP; a completed rest reaching Fatigue 20 unlocks up to 1.15x work speed and 1.25x XP, fading to 1.0x by Fatigue 80. Final automated evidence: focused Edit Mode **63/63 passed**, focused Play Mode **7/7 passed**, complete graphics-enabled Edit Mode **315/315 passed**, and complete Play Mode **21/21 passed** with zero skips. The earlier licensing-client IPC blocker cleared when Unity ran with access to its local license service. The user subsequently confirmed all 18 manual checks.

**3.1C Individual Skills & Learn-by-Doing Foundation: COMPLETE (acceptance recorded 13 October 2026).** Each authoritative ant record owns all seven stable GDD skills from level 0–100. Successful future activities can explicitly award base XP through a validated multiplier boundary; a deterministic provisional curve handles level progress and safe maximum capping. Independently tracked inactivity uses explicit simulation hours, a provisional 24-hour grace period and 0.25 levels/hour decay above 50, with an exact floor of 50. The selected-ant panel displays every skill and contains collapsible Developer Only XP, multiplier, maximum, decay and reset controls. No actual jobs, work effects, inventory or save/load were added.

Provisional curve: XP from level `L` to `L+1` is `100 + 5L`; cumulative XP at level `L` is `100L + 2.5L(L-1)`, capped at 34,750 XP for level 100. Automated evidence: focused Edit Mode **40/40 passed**, focused Play Mode **6/6 passed**, complete Edit Mode **292/292 passed**, and complete Play Mode **20/20 passed**. Manual acceptance is recorded by the approved transition to 3.2A.

**3.1B Persistent Ant Identity & Attributes: COMPLETE (manual acceptance confirmed 13 October 2026).** A new plain-C# `AntColony.Ants` assembly owns stable `AntId`/`ColonyId` values, one shared `AntRecord` model for Queen and Worker, provisional 1–10 Strength/Agility/Endurance values, lifecycle state and a duplicate-safe per-colony registry. Runtime visuals resolve existing records through `AntEntityBinding`; a separate visual-claim registry prevents two live GameObjects from representing one ant, while destroying a visual never removes its record. The three deterministic development ants are Mara (Queen, 7/5/8), Pip (Worker, 4/8/5) and Tala (Worker, 6/6/7). The selection panel displays identity, colony, caste, attributes, lifecycle and the existing movement status. Deceased ants retain their records and reject movement orders. No save/load, progression, needs, jobs, reproduction, inventory, combat or death gameplay was added.

Automated evidence: focused 3.1B Edit Mode **16/16 passed**, focused selection/identity Play Mode **5/5 passed**, complete Edit Mode **268/268 passed**, and complete Play Mode **19/19 passed**. The graphics-enabled full Edit Mode run was required because the retained rendered-pixel regression crashes under `-nographics`. The user subsequently confirmed all 12 manual checks, completing 3.1B.

**3.1A Ant Selection & Command Integration: COMPLETE.** The user confirmed all 14 manual Play Mode acceptance checks passed. Previously reported automation: focused Edit Mode 6/6, focused Play Mode 4/4, full Edit Mode 258/258 and full Play Mode 18/18. Three selectable ants, independent right-click movement orders, UI click protection, order completion and development-tool separation have been accepted. This is a documentation-only status change; no new tests executed.

**Historical next action:** the 3.3A and 3.3B checklists were accepted. The current next action is the 3.3C manual checklist recorded at the top of `CurrentGameState.md`.


## Current accepted milestone and experimental next step — 12 October 2026

**Revised 2.4B Grid Navigation Proof of Concept: Complete.** A separate ordinary-ant experiment derives grippable and grounded anchors from authoritative world data, caches whole-body-safe eight-direction edges with diagonal restrictions, searches them with movement-time-weighted deterministic A*, and interpolates smoothly between anchors. Stable traversal excludes the experimental bodies from Physics2D; only Falling enables Rigidbody2D gravity, followed by controlled anchor recovery. Local terrain edits invalidate a bounded cache neighbourhood. Moving agents, local avoidance and enemies are not baked into static terrain navigation. The production 2.4A motor remains intact for comparison. The user confirmed manual Unity Play Mode acceptance after the automated results below.

Automated evidence: 11/11 focused grid EditMode, 238/238 full EditMode, 3/3 focused grid PlayMode and 9/9 full PlayMode passed. The 500-agent representative batch recorded 645.436 ms for 500 independent uncached A* searches, 24.277 ms to simulate all complete smooth routes, 1.086 ms for 100 Physics2D steps with stable bodies excluded, 1,875,968 retained managed bytes, 250,500 examined nodes and 500/500 correct arrivals. This validated the execution model but also made path-request budgeting and common-route reuse a prerequisite for a production hundreds-of-ants milestone. The user later completed manual Play Mode acceptance; this proof of concept still does not replace production movement.

**2.4A Natural Background Movement Realignment: COMPLETE.** User reports Roslyn compilation, focused 56/56 Edit Mode, full graphics-enabled 227/227 Edit Mode, 6/6 physics Play Mode and scene validators passed. User has confirmed the manual Unity Play Mode acceptance. Natural earth/stone grip works without preparation; prepared terrain increases movement speed by provisional 1.25×; sky remains non-grippable. Older milestones remain historical. This status is based on user-reported local work and validation; GitHub may need the associated Unity source/tests pushed.

**Latest approved scope: 3.3A Colony Job Designation & Priority Foundation — Implemented, pending manual acceptance.** Only development synthetic work is authorised. Real excavation, construction, hauling, inventories and resource production remain excluded.

**2.4C implementation status: Complete.** A shared main-thread scheduler advances deterministic resumable A* with a configurable 128-node/update total budget and 8-node/request slice. Its weighted High/Normal/Low service cycle prevents starvation; duplicate suppression, replacement/cancellation, terrain revisions and request identity prevent stale delivery. The experimental motor has explicit waiting, following, blocked, falling and recovery lifecycle states with two bounded automatic retries. The development scene can spawn 1/100/300/500 grid ants, exercise simultaneous or staggered requests, cancel one request, mutate terrain and display scheduler diagnostics. Friendly overlap is intentional; stable grid ants do not pay Physics2D contact cost, and the accepted 2.4A ant is retained. The user subsequently confirmed manual Play Mode acceptance.

Unity Bee response-file Roslyn compilation passed for the World, Development, EditMode-test and PlayMode-test assemblies. Focused scheduler EditMode tests passed 11/11. Focused PlayMode tests independently passed shared three-ant request ownership/cancellation, active-route mutation/replan, and falling interruption/recovery. A combined focused run passed four tests before the mutation test exceeded its original 20-second test-only bound; that test passed with a 35-second bound. Later combined/full reruns were blocked when Unity repeatedly lost its Licensing Client and crashed before emitting results, so no post-fix combined result is claimed. Deterministic 1/100/300/500 request batches completed with 1/100/300/500 successes and zero failures. At 500 requests, total batch time was 187.776 ms, accumulated scheduler work 171.911 ms, maximum scheduler update 3.197 ms, average/worst completion latency 176.614/219 updates, total expansions 25,500 over 219 updates, peak queue 500, retained managed-memory lower bound 1,118,208 bytes, and 100 disabled-body Physics2D steps cost 1.143 ms. These editor measurements validate bounded scheduling, not production frame rate. Full results and the exact 13-step manual checklist are recorded in `CurrentGameState.md`.

**2.4D implementation status: Implemented — Pending Manual Acceptance.** `PrimaryGridAnt.prefab` uses the grid motor by default with no input, surface or continuous-autonomous motor. `AntColony.Navigation` contains the production motor plus one per-world `GridNavigationWorldService`; that service owns the shared map/scheduler and processes existing bounded budgets independently of UI. The motor exposes the small MoveTo/CancelMovement/status boundary now used by 3.1A. The previous licensing blocker cleared: final verification passes 258/258 Edit Mode and 18/18 Play Mode, including production grid and retained 2.4A movement regressions. The 2.4C benchmark remains the latest performance baseline because 3.1A changes command interaction, not scheduler performance. Exact checks and limitations are recorded in `CurrentGameState.md`.

**3.1A implementation status: Complete.** `AntColony.Commands` adds a focused selection target and selection/command controller above the unchanged production motor. `AntGameDevelopment.unity` automatically spawns three separated cyan/pink/green ants, provides a following yellow selection ring, rejects UI-through clicks, sends right-click destinations only to the selected ant, and shows selected ant/order/destination/failure status. Stress ants remain a separate developer group and the developer panel starts collapsed. Focused verification passed 6/6 Edit Mode and 4/4 Play Mode; the complete suites passed 258/258 Edit Mode and 18/18 Play Mode. The user subsequently confirmed manual acceptance.


**Historical construction/resource addendum (12 October 2026):** This introduced item stacks, slot-only carrying, Material Storage, physical reservations and **builder self-supply**. Its excavator auto-pickup rule is superseded by the approved 14 October 2026 loose-only extraction rule implemented in 3.5B. Numerical recipes and stack sizes remain provisional.

**Job/priority design addendum (12 October 2026):** Per-ant 1–4/Disabled matrices; priority before closest reachable job; direct control plus specific automatic orders; critical survival overrides ordinary work with safe-interruption handling; persistent valid partial progress; skills improve efficiency but never silently outrank settings. Read-only audit must cover job dispatch, path cost, exclusive reservations, handoff, saved state and construction builder self-supply. Detailed thresholds and job categories remain provisional. No implementation authorised.

**Control design amendment — 12 October 2026:** Approved normal-play controls are now RimWorld-style command-based: autonomous ants, temporary explicit ordered work (automatic return to autonomy), persistent tactical Draft Mode for single/multi-ant movement, defence and attack. Continuous WASD steering is developer-only prototype functionality, not an intended gameplay feature. Reinterpret former manual productivity bonus as modest nonstacking ordered-task work-speed bonus, percentage TBD. In the read-only audit, flag player/AI locomotion separation, order lifecycle, reservation handoffs, draft-state persistence, multi-ant route contention and UI implications. No Unity implementation is approved by this amendment.

## Design realignment notice — 12 October 2026

The authoritative design is the **Authoritative design realignment** section in `Documentation/GameDesignDocument.md`. Existing implementation milestones below may retain historical terminology. Milestone 2.3B was manually accepted under its previous rules; Milestone 2.4A implements the revised natural-background rules and has been manually accepted.

**Next milestone: documentation-led, read-only design/architecture audit** (no code changes). Inspect all Unity systems, compare each to the new design, classify Keep/Refactor/Replace/Defer, identify migration sequencing and tests affected, and propose a smallest safe vertical slice. Proposed future slices (not authorised): shared ant/player/AI control; natural-grip movement correction; functional tile conversion + physical material logistics; room/door detection; individual needs and Rested; founding queen nursery/brood/first workers; autonomous jobs and persistence. Do not mark an unimplemented design feature complete.

## Roadmap status

This roadmap reflects the repository as inspected on 9 October 2026. Milestone 1 is in progress. The user has confirmed floor and isolated wall movement plus resolution of the reported corner deadlock in Play Mode. Ceiling/corner route acceptance and the newly implemented Auto Step-Over slice still await complete Play Mode acceptance.

`GameDesignDocument.md` version 3.2 is the primary design reference. Milestone 1 records the existing manually driven movement prototype as a regression baseline. The approved future foundation is a two-layer world model—one permanently present but replaceable Background Material at every coordinate plus an optional foreground Solid Tile—combined with Background-driven movement, directional navigation and Hybrid Task Management.

## Milestone 1 — Player Ant Movement Prototype

**Status:** In progress. Directional input, floor movement, isolated wall movement, and the corner-deadlock fix are Play Mode confirmed. Ceiling movement, intentional junction selection, the complete corner route, and bounded Auto Step-Over are implemented but not yet fully Play Mode accepted. Camera follow remains unimplemented.

**Goal:** Preserve and finish validating the existing directly controlled movement prototype as a technical baseline. This milestone does not define the final player control scheme. Becoming a queen is later progression and is outside this milestone.

### Scope

- One ordinary player ant in one small test scene.
- Directional WASD movement intent.
- Horizontal floor movement.
- Vertical wall climbing.
- Upside-down ceiling movement.
- Smooth transitions between connected floor, wall, and ceiling surfaces.
- Natural rotation so the ant aligns with the current surface.
- Configurable Auto Step-Over across a physically valid one-tile opening.
- Collision and whole-footprint clearance checks against terrain and blocking test obstacles.
- A simple 2D follow camera.
- Placeholder visuals are acceptable while movement is being proved.

### Explicitly out of scope

- Excavation and terrain modification.
- Food, water, rest, health, reproduction, or other survival systems.
- Multiple ants, control handover, autonomous tasks, route planning, traffic management, or deadlock recovery.
- Skills, attributes, inventory, resources, dumping, or colony management.
- Multiplayer or networking.

### Proposed implementation sequence

Each step should be tested in the Unity Editor before the next step begins.

1. **Author the test scene and player-ant prefab**
   - Create a small tunnel from simple 2D collider shapes or a Tilemap with a `TilemapCollider2D`.
   - Create a placeholder player ant with a `SpriteRenderer`, `Rigidbody2D`, and a simple 2D collider.
   - Put traversable tunnel geometry on a dedicated `ClimbableSurface` layer.
   - Confirm that the scene opens without errors and its collision geometry is visible and correct.

2. **Add horizontal floor movement**
   - Add a focused ant movement script that reads the existing Input System `Move` action.
   - Move the ant with `Rigidbody2D` in `FixedUpdate` so physics remains predictable.
   - Detect the nearby floor and keep the ant attached without adding jumping or free flight.
   - Verify starting, stopping, reversing direction, and remaining attached on a flat floor.

3. **Separate directional intent from reusable movement**
   - Interpret WASD as an intended world direction for the selected ant.
   - Keep direct-input interpretation separate from the physical surface motor so future autonomous navigation can submit movement intent without receiving different collision rules.
   - Do not implement selection, autonomous behaviour or pathfinding during this step.

   **Implementation status:** Implemented in code and prefab. Unity compilation passed, and the user has confirmed the directional floor/wall prototype in Play Mode.

4. **Add surface detection and alignment**
   - Detect nearby climbable surfaces with bounded 2D physics queries.
   - Derive the ant's outward direction from the chosen surface normal.
   - Rotate toward that orientation at a configurable rate rather than snapping instantly.
   - Validate enough free space for the ant's whole collider, not only its centre point.

   **Implementation status:** Implemented. Floor, side-wall, and ceiling casts expose contact point/normal state, attachment, gravity handling, and motor-controlled alignment. Floor and wall behavior is Play Mode confirmed by the user; ceiling behavior is pending acceptance.

5. **Support walls, ceilings and directional junction choice**
   - Move along the tangent of the selected surface instead of assuming world-horizontal movement.
   - Verify movement on isolated floor, wall and ceiling sections before testing corners.
   - At a junction, use the player's intended world direction to choose among reachable connected surfaces.
   - Ensure the ant does not drift away, tunnel through terrain or acquire an inaccessible surface.

   **Implementation status:** Implemented in code. Floor and wall movement is Play Mode confirmed. Ceiling A/D movement and intent-projected junction choice are implemented, including the rule that W alone at a wall/ceiling join does not choose a horizontal direction. Ceiling and junction behavior remains pending Play Mode acceptance.

6. **Support connected-surface transitions**
   - Add short forward and around-corner checks so the ant can acquire the next connected surface.
   - Tune attachment, query distance, rotation speed and collider size for smooth inside and outside corners.
   - Verify clockwise and counter-clockwise transitions across floor-to-wall, wall-to-ceiling, ceiling-to-wall and wall-to-floor joins.

   **Implementation status:** Implemented in code and authored test content. Live testing exposed a transition-state deadlock; the state exit, reversal, failed-step recovery, and unchanged-input retry defects are fixed. The focused Unity Edit Mode regression passed 1/1, while manual reproduction testing and wider corner acceptance remain pending.

7. **Add Auto Step-Over**
   - Add a configurable maximum crossing distance rather than hard-coding a tile count into movement logic.
   - Cross only when the opposite surface is reachable and the ant's entire swept footprint is clear.
   - Verify that a valid one-tile opening can be crossed continuously and that wider openings or crossings blocked by a test obstacle are rejected.
   - Do not add jumping, flying, teleportation or unrestricted falling.

   **Implementation status:** Implemented in the motor, player prefab, authored scene, setup validator, and focused regression harness. The motor measures openings relative to floor/wall/ceiling tangents, accepts only a configured one-tile parallel landing, prevalidates and continuously checks the whole collider path, prioritises explicit perpendicular input, pauses on release, reverses, recovers safely, and suppresses unchanged rejected retries. Automated Unity execution is pending because the local Licensing Client lost IPC during both command-line attempts; manual Play Mode acceptance is also pending.

8. **Add the camera follow**
   - Add one small camera-follow script with configurable smoothing and optional position offset.
   - Keep the camera upright; the ant's surface rotation must not rotate the view.
   - Verify that the ant stays visible through the complete tunnel route.

9. **Validate and document the milestone**
   - Run the complete route repeatedly in Play Mode in both directions.
   - Check the Console for errors and warnings.
   - Record Inspector values and any remaining limitations in `CurrentGameState.md`.
   - Mark the milestone complete only after all completion criteria have been observed in Unity.

### Completion criteria

- The player ant can traverse a continuous authored route containing floors, vertical walls, and ceilings in both directions.
- W, A, S and D express world-direction intent, and the player can deliberately choose a reachable surface at a junction.
- Surface transitions do not require teleporting, scene-specific trigger scripts, or manual Inspector changes during play.
- The ant remains aligned and attached without obvious jitter, tunnelling, or unintended detachment.
- A valid one-tile opening is crossed through configured Auto Step-Over, while wider or obstructed openings remain uncrossable without a connected surface route.
- Terrain and blocking obstacles are never bypassed, and clearance checks account for the ant's collider footprint.
- The camera follows without inheriting the ant's rotation or showing disruptive jitter.
- The Unity Console remains free of errors during the test.
- Required scene objects, components, layers, controls, and Inspector values are documented.

### Minimal proposed project structure

Create these folders only as their contents are needed:

```text
Assets/
  AntColony/
    Art/
      Placeholder/
    Prefabs/
      Ants/
    Scenes/
    Scripts/
      Ants/
      Camera/
    Tests/
      EditMode/
      PlayMode/
```

The initial script set should remain small:

- `AntSurfaceMotor.cs` — accepts a normalised `Vector2` movement intent, detects floors/walls/ceilings, selects connected surfaces from world-direction intent, and owns attachment, collision-checked corner motion, gravity, and Rigidbody2D alignment.
- `PlayerMovementInput.cs` — reads the existing `Move` action, normalises the result, and supplies it to `AntSurfaceMotor`. It contains no physics or autonomous navigation logic.
- `CameraFollow2D.cs` — follows a target position with simple smoothing while keeping the camera upright.

The existing root-level Unity template assets can remain where they are until the authored Ant Colony scene replaces the template scene. Milestone 1 itself has no Bootstrap. The later integrated development scene uses a small authored world coordinator while preserving the rule against a global manager or replacement authority.

## Milestone 2 — World Tiles and Autonomous Navigation

**Status:** In progress. Milestones through 2.4C retain their recorded automated/manual evidence. Milestone 2.4D is **Implemented — Pending Manual Acceptance**; Unity suite and benchmark execution remains environment-blocked.

**Goal:** Establish the approved two-layer world representation, validate physical body clearance independently, derive Background-based walking/gripping/falling capability, build directional connections, then prove that one ant can move autonomously without direct movement input.

Milestone 2 must retain `AntSurfaceMotor` and `PlayerMovementInput` as prototype and developer-test tools until the replacement movement is validated. Existing corner-following and Auto Step-Over behaviour are not authoritative navigation rules.

### 2.1 — World Tile Model and Navigation Visualisation

- **2.1A — Architecture audit:** Completed. The existing authored `DirtBlock` collider course remains the movement prototype and is not authoritative tile data.
- **2.1B — Chunk data foundation:** Complete. A scene-independent sparse dictionary owns explicitly created 32×32 chunks. Each resident chunk contains 1,024 Background Tile IDs and zero or one Solid Tile ID per cell. Coordinate conversion supports positive and negative chunk positions, and missing chunks return an unavailable status without implicit generation.
- Stable ScriptableObject material definitions provide the initial `background.default` and `solid.soil` IDs. The 2.1B data object merely stores a seed and never generates on query; the later integrated checkpoint invokes its bounded generator explicitly.
- The Milestone 2.1B Unity Edit Mode suite passed 25/25, and the existing ant movement remained functional in the user's Play Mode verification.
- **2.1C — Navigation classification:** Complete. Direct, uncached queries over authoritative world data distinguish Unavailable, Covered, and Exposed occupancy, while exposed cells report Supported, Unsupported, or Unknown cardinal support. Unknown prevents missing adjacent chunks from being mistaken for definitively unsupported terrain. The user-confirmed acceptance run passed 50/50 Edit Mode tests.
- **2.1D — Navigation visualisation:** Complete. The separate `AntNavigationPrototype` developer scene uses a deterministic eight-chunk fixture and one bounded batched mesh to display the actual classifier's Covered, Supported, Unsupported, Unknown, and Unavailable results. The automated suite passed 61/61 and the user confirmed all 11 manual visual acceptance checks plus unchanged original ant movement.
- **2.1E — World Tilemap integration:** Complete. Separate Background and foreground Solid Tilemaps project explicitly requested resident chunks from `ChunkedWorldData` through registered material definitions. Explicit cell, chunk, and bounded-region refreshes preserve Background visuals during Solid mutation, diagnose missing visuals safely, align signed cells through an explicit half-cell Grid offset, and add no collision components.
- The 2.1E Edit Mode suite passed 89/89, including 28 Tilemap projection cases, the expanded scene validator passed, and the user confirmed all 10 manual visual checks.
- The existing Supported classification remains a useful foreground-adjacency diagnostic, not the complete movement-capability rule; body clearance, Background permissions, grip, grounding, gravity, directional connectivity and Reachable Space are separate work.
- Preserve Background Tiles when a coordinate is covered by a Solid Tile.
- Visualise which cardinal Qualifying Geometry makes an Exposed Space navigation-eligible.
- Keep local traversability distinct from connectivity and Reachable Space.
- Confirm classification in the current prototype scene, including open sky, tunnels and unsupported chamber centres.
- Do not change existing movement behaviour.

**Acceptance:** Every inspected resident coordinate has one Background Tile and no more than one Solid Tile; visualisation consistently distinguishes Covered, Supported, Unsupported, Unknown, and Unavailable cells, rejects diagonal-only or Background-Tile-only support, and changes no ant movement. Supported does not imply clearance, connectivity, or reachability.

### 2.2 — Clearance, Movement Capability and Autonomous Navigation

- **2.2A — Authoritative terrain collision:** Complete. The dedicated navigation scene has a separate invisible collision Tilemap derived from authoritative Solid Material `BlocksMovement` flags. Background and visual foreground Tilemaps remain collider-free, while `SupportsNavigation` stays independent. One Tilemap collider, static Rigidbody2D and manually refreshed Composite collider merge adjacent blocking cells on `ClimbableSurface` without per-tile GameObjects.
- Explicit cell, chunk and bounded-region collision refreshes process pending Tilemap changes and synchronize Physics2D before validation. The deterministic fixture adds supporting/non-blocking and blocking/non-supporting examples alongside its existing terrain and signed chunk boundaries.
- Unity compilation and the expanded scene/material/collider validator passed. The Edit Mode suite passed 117/117, including 28 Physics2D collision cases, and the user confirmed all 12 manual collision checks.
- **2.2B — Physical ant-body clearance:** Complete. The full rotated-capsule query reads authoritative `BlocksMovement` data only and is independent of support, Background preparation, grip, grounding, gravity and reachability. Open Sky and a smooth support-free shaft may be physically Clear. Compilation, 38/38 focused tests, structural scene validation, 2/2 movement regression tests and the full 155/155 Edit Mode suite passed; the user confirmed all 9 manual clearance checks.
- **2.2C — Background movement capability and gravity eligibility:** In progress, pending manual acceptance. Background Materials explicitly define voluntary directions, completed grip and unsupported gravity. A read-only evaluator reports Grounded, Gripping or Falling while keeping physical clearance separate. Grounding uses a small whole-capsule bottom-contact test against authoritative blocking/supporting Solid top faces; Background replacement updates capability immediately without changing foreground occupancy. The 37/37 focused suite, expanded scene validator, 2/2 movement regressions and full 192/192 Edit Mode suite pass.
- **Integrated sample-world checkpoint — Complete:** `AntGameDevelopment.unity` provides one authored Play Mode environment over a deterministic 4×4-chunk, 128×128-cell world. It reuses authoritative world data, Background/Solid materials, visual Tilemaps, collision-only Tilemap, clearance and movement-capability services. The user confirmed terrain readability, camera controls, inspection, foreground mutation, prepared-Background replacement and the corrected brown-to-green-to-brown rendering round trip in Play Mode. This acceptance does not mark the separate Milestone 2.2C Scene-view checklist complete.
- The older movement and navigation scenes remain available until a separate cleanup milestone can verify they are no longer needed.
- **2.2D — Directional navigation connections:** Not started. Combine full-body clearance with Background permissions, grounding, grip, falling, missing-world handling and applicable obstacles. Connections may be one-way through explicitly non-grippable space and must reject invalid diagonal corner cutting.
- **2.2E — General autonomous destination navigation:** Not started. Extend beyond the bounded 2.3A/2.3B direct segments by routing through valid directed connections, following waypoints continuously and reporting arbitrary invalid or unreachable destinations safely.

**Acceptance:** 2.2B reports physical fit independently of grip; 2.2C reports movement/gravity state; 2.2D produces safe directional connections; and 2.2E moves one ant to valid destinations without teleportation, free flight or an invalid movement state.

### 2.3 — Integrated Movement Validation

- **2.3A — First autonomous ant, ground movement:** Complete. `AntGameDevelopment.unity` spawns exactly one visually distinct ordinary ant only after a bounded nearby pose passes whole-body clearance and Grounded eligibility. The original direct-corridor slice revalidated every immediate grounded step and stopped on new blockers or lost support without input, teleportation or route search. Structural validation, 221/221 Edit Mode tests, 3/3 physics-backed Play Mode tests and all 11 user-confirmed manual checks passed.
- **2.3B — Background grip and gravity movement:** Complete under its historical prepared-grip rules. The user reported its automated run and confirmed all 12 manual checks. Milestone 2.4A supersedes only the prepared-only access assumption while preserving the proven locomotion states, collision handling and reusable motor.
- 2.3A and 2.3B are bounded observable precursors, not the general 2.2D/2.2E navigation solution. Full directional connection generation, waypoints, routed wall/ceiling traversal and route search remain future work.

- Validate routes beside ground, vertical walls, ceilings, corners and overhangs.
- Validate natural and prepared climbing regions plus explicitly non-grippable Sky/void fixtures using Background capability, grounding, grip and falling rules.
- Prevent invalid voluntary movement and diagonal corner-cutting while permitting valid directional falling.
- Validate every path segment for the full ant-body clearance, not only tile centres.
- Confirm invalid navigation states stop, recover or report failure safely.

**Acceptance:** The validation course proves the same movement model across all listed geometry, permits intended directional falling, and rejects every invalid voluntary, obstructed or insufficient-clearance route.

### 2.4 — Dynamic Terrain and Navigation Updates

- **2.4A — Natural Background Movement Realignment:** **Complete.** Natural Earth and Stone provide horizontal, vertical and diagonal grip at normal speed. Prepared Background provides identical access plus a data-driven provisional `1.25x` speed multiplier; Sky remains non-grippable. The existing capability evaluator, bounded route validator, autonomous motor, Rigidbody2D collision and Tilemap projections are reused. Runtime foreground/Background mutations re-evaluate locomotion and invalidate affected active direct routes. Focused EditMode tests passed 56/56, the full graphics-enabled EditMode suite passed 227/227, PlayMode tests passed 6/6, and the user confirmed manual acceptance.
- **2.4B — Grid Navigation Proof of Concept:** **Complete.** The experiment is separate from production 2.4A and limited to one ordinary-ant profile. Shared cached anchors/connections, whole-body clearance, eight-direction corner rules, travel-time A*, smooth anchor following, local invalidation and physics-only Falling are implemented. Deterministic fixtures and automated suites passed as recorded above, and the user confirmed manual Play Mode acceptance. The benchmark supports low stable-movement/physics overhead but rejects an unbudgeted 500-search frame as production-ready.
- **2.4C — Grid Navigation Stabilisation and Multi-Ant Readiness:** **Complete.** Shared incremental scheduling, fair generic priorities, request cancellation/replacement, terrain-revision safety, explicit route/fall recovery, bounded retries, shared-map stress groups and diagnostics are implemented. Focused new automation passed, and the user confirmed manual Play Mode acceptance. Friendly overlap is allowed and colony gameplay remains excluded.
- **2.4D — Production Grid Navigation Integration:** **Implemented — Pending Manual Acceptance.** The primary ant prefab uses the runtime grid motor, one per-world service owns shared navigation, and future callers receive a narrow destination/cancellation/status API. The development scene defaults to grid commands while continuous motors, input, prototype scenes and tests remain developer-only. Static compilation passes; complete Unity execution is blocked by licensing and manual acceptance remains outstanding. Do not add colony gameplay.

- Remove excavatable foreground Solid Tiles in a controlled test while keeping their Background Tiles unchanged.
- Update terrain collision after removal.
- Replace a designated exposed Background Material only after a preparation job completes.
- Reevaluate clearance, movement capability and directional navigation after excavation, preparation or another terrain change.
- Update or invalidate routes whose traversability, connectivity or clearance changed.
- Prefer local, incremental updates over rebuilding the entire map.

**Acceptance:** Controlled excavation removes only the Solid Tile and retains the Background; completed preparation replaces only the Background Material. Both operations update their affected collision/capability/navigation data and cause an impacted ant to replan or report failure without stale route segments.

## Subsequent milestones

These milestones are ordered direction, not approved implementation work. Their detailed acceptance criteria should be written only when each milestone is authorised.

| Milestone | Intended outcome | Status |
| --- | --- | --- |
| 3 — Ant selection and inspection | Accepted foundations through 3.3C remain complete; 3.3D adds physical Material Storage and hauling diagnostics. | 3.3D pending manual acceptance |
| 4 — Hybrid task assignment | Real excavation and hauling use accepted priorities; forced-work orders and construction remain future. | Excavation accepted; hauling implemented |
| 5 — Multiple-ant movement | Allow shared-tile presence, local avoidance, temporary friendly overlap, congestion-aware routing and bounded recovery without invalid displacement. | Not started |
| 6 — Autonomous worker loop | Select and perform eligible work while responding predictably to urgent needs and task invalidation. | Not started |
| 7 — Excavation, Surface Preparation and resources | Excavate reachable foreground Solids, retain their Backgrounds, prepare exposed Background Materials progressively from accessible positions, and reserve/haul/consume configured resources. | Not started |
| 8 — Population and performance | Scale toward potentially hundreds of independently simulated ants using shared navigation data, efficient nearby-ant lookup and controlled update frequencies. | Not started |
| 9 — Enemy colonies and hostile interactions | Reuse the same terrain navigation for other colonies and add high-level engage, retreat, avoid, defend and assistance responses. | Not started |
| 10 — Skills, survival, growth and finished slice | Skills, needs, physical survival, autonomous survival and synthetic work foundations exist. Real work, queen/brood progression, onboarding, objectives and save/load remain future slices. | Synthetic work pending acceptance |

The 3.3C carrying model is implemented: one initial slot plus one at Hauling 20/40/60/80, capped at five, with compatible resource stacks and a provisional maximum of 10 for Soil and Stone.

No new milestone is complete without implementation and validation evidence. Milestone 2.4D remains pending its separately recorded manual acceptance. Milestone 3.4A implements functional Background construction and builder self-supply; rooms, doors, brood, production, persistence, streaming and local avoidance are not implemented.
