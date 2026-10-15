# Milestone History

## 15 October 2026 — WV-B2B Closure and Scheduler Fairness

**WV-B2B Complete.** The user accepted the seed-`2203` deterministic generated
scenario and confirmed the founding queen could Extract Soil/Stone, create loose
resources, Haul separately, stage and complete Construction, build first Storage,
deliver later resources, expand the Colony, switch views and use the entrance.
`WV-B2B-DEF-001/002/003` and `WV-B2B-ARCH-001` are closed. Her later Day 8 death
at Health 0, Hunger 0, Thirst 0 and Fatigue 100 is retained as the known pre-WV-C1
survival limitation, not classified as hunger-only.

The post-closure scheduler correction replaces first-32 truncation with a
deterministic resumable pass bounded to 32 entries per evaluation. Appends join
the active pass; removal and priority revisions reset safely, terminal/status
changes are revalidated, and
priority-before-path-cost plus exclusive ownership remain unchanged. Focused
tests pass 40/40 Edit and 1/1 Play; complete XML-backed regressions pass 551/551
Edit and 74/74 Play. This closes `WV-B1-REG-001`. Scheduler manual stress
acceptance remains pending; WV-C1 has not started.

## 15 October 2026 — WV-B2B-ARCH-001 Unified Resource Logistics

**Implemented — Pending Manual Acceptance.** Extraction replaces Excavation as
the official skill/category while preserving its numeric data slot. Soil/Stone
Extraction creates loose output; destination-aware Hauling reserves and delivers
to Construction staging or Storage; fully staged Construction performs work
only. This supersedes builder self-supply and enables first-Storage bootstrap
without existing Storage. Validation passed 82/82 focused Edit, 9/9 focused
Play, 539/539 full Edit and 73/73 full Play. Direct Play Mode acceptance remains
pending. WV-B2B and its existing defect/regression records stay open;
WV-C1 is not started.

## 15 October 2026 — WV-B2B Founding Storage Bootstrap Recovery

- Reproduced destination-less Hauling repeatedly winning work selection before
  failing because the founding colony had no completed Material Storage.
- Added a reservation-free Hauling eligibility check for source access, carrying
  capacity, route and compatible Storage destination.
- Preserved the loose Soil for existing Construction self-supply, exact-once
  staging/consumption and automatic hauling reconsideration after Storage exists.
- Automated evidence: focused Material Storage/Hauling Edit Mode passed `17/17`;
  broader relevant Edit Mode passed `75/75`, World View Play Mode passed `5/5`,
  and affected Commands/Edit Mode assemblies compile.
- Status: **Implemented — Pending Manual Acceptance**. `WV-B2B-DEF-003`, earlier
  WV-B2B defects, WV-B2B and `WV-B1-REG-001` remain open; WV-C1 has not started.

## 15 October 2026 — WV-B2B Founding-Queen Defect Recovery

- Prevented construction with no obtainable physical material from repeatedly
  winning autonomous selection; it now stays paused, unowned and reconsiderable.
- Advanced generation to version `2` / `wv-b2b.default.002` and guaranteed one
  actual Stone foreground node at `(-4,-8)` beside the founding chamber.
- Added specific excavation rejection diagnostics and controller, recovery,
  generated-Stone, Background-preservation and conservation regressions.
- Automated evidence: affected assemblies compile; relevant Edit Mode passed
  `58/58`; World View Play Mode passed `5/5`; strict docs build passed.
- Status: **Implemented — Pending Manual Acceptance**. `WV-B2B-DEF-001`,
  `WV-B2B-DEF-002`, WV-B2B itself and `WV-B1-REG-001` remain open. WV-C1 has not
  started.

## 15 October 2026 — WV-B2B Procedural World Generation Foundation

- Added deterministic versioned Surface/Colony generation from one 64-bit seed,
  unified generated cells/nodes, validation and existing-system adapters.
- Added the separate one-queen generated scenario, seed/start/restore controls,
  validated entrance/chamber geometry and temporary isolated presentation.
- Added deterministic Edit Mode coverage and a generated-scenario Play Mode
  integration regression. Assemblies compile and a four-seed direct probe passed;
  Unity Test Runner execution was blocked by a Licensing Client crash.
- Status: **Implemented — Awaiting Validation**; manual checklist and
  `WV-B1-REG-001` remain open.

## 14 October 2026 — 3.5B Excavation Output & Loose Resource Handling

- Replaced historical inventory-first excavation output with an exact-once loose drop at the excavated authoritative cell.
- Kept excavation independent of ant capacity and storage while preserving progression, XP, terrain refresh and navigation invalidation.
- Preserved the separate hauling scheduler and accepted construction builder self-supply from inventory, storage or loose drops.
- Added empty/partial/full inventory, same-cell merging and real autonomous loose-output regressions while retaining conservation/failure coverage.
- Automated evidence: focused Edit `96/96`, focused Play `14/14`, complete graphics-enabled Edit `514/514`, complete Play `67/67`; zero failures/skips.
- Status: **Implemented — Pending Manual Acceptance** (27 steps).

Milestone 3.5A remains separately **Implemented — Pending Manual Acceptance**; this milestone changed no presentation geometry or art mappings.

## 14 October 2026 — 3.5A Colony View Presentation, Tile Preview & Art Integration

- Added registry-backed hover and bounded drag previews for excavation and all six Background Conversion recipes.
- Added compatible-only right-click cancellation without changing Select-mode orders or the existing Cancel tool.
- Bound reviewed Soil/Earth and Stone candidates through existing collider-free Tile assets; deferred composites, decorations and unverified semantic mappings.
- Automated evidence: focused Edit `11/11`, complete graphics-enabled Edit `503/503`, complete Play `65/65`, art validator, deterministic preparation, strict docs build and diff check passed.
- Status: **Implemented — Pending Manual Acceptance**.

This history records the strongest confirmed evidence without implying that later code reran every earlier checklist.

## 14 October 2026

### 3.4D — Colony Management & Work Priorities UI

**Implemented — Pending Manual Acceptance.** A global scrollable matrix on the existing player Canvas lists every active authoritative colony record, caste, mapped skills and current activity. Excavation, Construction and Hauling provide 1–4/Disabled controls over the existing `AntWorkPrioritySet`; unimplemented categories remain absent. Edits affect subsequent autonomous selection without cancelling active work, changing skills, pausing the clock or altering developer overrides. The same state remains visible to developer diagnostics, and unloaded records remain manageable without inventing region residency.

- Focused Edit Mode: 7/7 passed.
- Focused Play Mode: 7/7 passed.
- Complete Edit Mode: 492/492 passed.
- Complete Play Mode: 65/65 passed.
- Manual Play Mode checklist: 18 steps pending.

### 3.4C — Colony View Player HUD & Developer UI Separation

**Complete — Manually Accepted.** An authored uGUI Canvas and reusable roster-entry prefab present authoritative ant records, selected shared/current-view data, the one global clock, physical resource ownership and readable current work. Colony View Select, Excavate, six functional Background recipes and safe area cancellation delegate to existing gameplay APIs. Developer IMGUI starts hidden, retains all seven categories and opens separately through F12/reopen control. One EventSystem/GraphicRaycaster boundary combines with existing IMGUI hit testing so UI clicks, scroll and designation drags do not leak into world or camera input. No World View runtime was added.

- Focused Edit Mode: 12/12 passed.
- Focused Play Mode: 13/13 passed.
- Complete graphics-enabled Edit Mode: 489/489 passed.
- Complete Play Mode: 62/62 passed.
- Manual Play Mode checklist: accepted before 3.4D authorisation.

### Custom tileset art foundation

**Implemented — Pending Manual Acceptance.** Preserved `1254x1254` RGBA Colony and World sources, deterministic aspect-preserving 32-pixel preparation, 170 named candidate sprites and Unity import metadata were added without changing terrain bindings or gameplay. No candidate is certified seamless; Tile assets and palettes remain gated on manual visual and semantic review.

### 3.4B — Unified Simulation Clock & Gameplay Mode

**Implemented — Pending Manual Acceptance.** A scene-owned Day 1 08:00 clock provides pause and 1×/2×/3× time, with one normal gameplay policy for survival, colony work and hauling. Pause gates simulation progression, navigation scheduling and physics without cancelling preserved work. Planning, selection, direct orders, priorities and camera controls remain available. A temporary independent time strip supports acceptance while the full player uGUI HUD remains deferred to 3.4C.

- Focused Edit Mode: 11/11 passed.
- Focused Play Mode: 7/7 passed.
- Complete graphics-enabled Edit Mode: 485/485 passed.
- Complete Play Mode: 58/58 passed.
- Manual Play Mode checklist: 30 steps pending.

## 13 October 2026

### 3.4A — Functional Background Conversion and Construction Jobs

**Implemented — Pending Manual Acceptance.** Player click/drag conversion designations create real Construction jobs in the shared registry. The reserved builder uses inventory first, otherwise reserves and physically collects reachable storage/drop material, stages it at the site, performs timed skill/Rested-modified work and commits an exact-once Background-only transaction. Cancellation preserves carried material and exposes staged material as a recoverable drop. Prepared, Nursery, Sleeping, Food Storage, Water Storage and Material Storage recipes are provisional; doors, rooms, brood and production were not added.

- Focused Edit Mode: 14/14 passed.
- Focused Play Mode: 3/3 passed.
- Complete graphics-enabled Edit Mode: 469/469 passed.
- Complete Play Mode: 50/50 passed.
- Manual Play Mode checklist: 30 steps pending.

### Development UI reorganisation (non-milestone pass)

**Complete — all 24 manual checks accepted.** Existing IMGUI controls were regrouped into a compact top toolbar with Ant, Needs, Skills, Jobs, Resources, World and Debug. A single bounded scrollable panel prevents overlap; Hide All leaves a reopen button; shared input blocking protects world commands and camera mouse controls.

- Focused Edit Mode: 8/8 passed.
- Focused Play Mode: 2/2 passed.
- Complete graphics-enabled Edit Mode: 455/455 passed.
- Complete Play Mode: 47/47 passed.
- Manual UI checklist: all 24 checks accepted.

### 3.3D — Material Storage and Autonomous Hauling

**Complete — all 30 manual checks accepted.** Pre-authored functional Background tiles own one physical storage slot each with Soil/Stone/general filters. Existing colony jobs, priorities, inventory and grid navigation drive reserved loose-drop delivery and owner-only self-deposit. Cancellation preserves the material at its current physical owner; completed delivery awards 5 base Hauling XP per unit.

- Focused Edit Mode: 15/15 passed.
- Focused Play Mode: 3/3 passed.
- Complete graphics-enabled Edit Mode: 447/447 passed.
- Complete Play Mode: 45/45 passed.
- Manual Play Mode checklist: all 30 checks accepted.

### 3.3C — Ant Inventory, Automatic Excavation Pickup and Physical Material Handling

**Complete historical milestone; its auto-pickup rule is superseded by 3.5B.** Authoritative ant records own independent stack-based inventories. At this checkpoint excavation output entered the excavator's inventory, fell back to a recoverable world drop, or split explicitly. Player-directed adjacent pickup and Developer Only dropping still use reservations and source-to-destination conservation; current excavation always leaves loose output.

- Focused Edit Mode: 26/26 passed.
- Focused Play Mode: 5/5 passed.
- Complete graphics-enabled Edit Mode: 432/432 passed.
- Complete Play Mode: 42/42 passed.
- Manual Play Mode checklist: all 26 checks accepted.

### 3.3B — Real Excavation Designations and Physical Material Output

**Complete.** Player area designations create real registry jobs; autonomous ants reach valid adjacent positions, preserve progress through interruption, and atomically exchange eligible foreground Soil/Stone for stable loose material drops while preserving Background and invalidating local projections/navigation.

- Focused Edit Mode: 14/14 passed.
- Focused Play Mode: 3/3 passed.
- Complete graphics-enabled Edit Mode: 406/406 passed.
- Complete Play Mode: 40/40 passed.
- Manual Play Mode checklist: all 26 checks accepted.

### 3.3A — Colony Job Designation and Priority Foundation

**Complete.** Per-ant 1–4/Disabled priorities, a shared synthetic job registry, deterministic priority/path-cost selection, exclusive reservations, persistent progress, skill/Rested work effects, safe survival/player interruption, and development diagnostics were added and manually accepted.

- Focused Edit Mode: 28/28 passed.
- Focused Play Mode: 5/5 passed.
- Complete graphics-enabled Edit Mode: 392/392 passed.
- Complete Play Mode: 37/37 passed.
- Manual Play Mode checklist: all 22 checks accepted.

### 3.2C — Autonomous Survival Behaviour

**Complete.** Acceptance was recorded through the approved transition to 3.3A. Previously verified results were 24/24 focused Edit Mode, 6/6 focused Play Mode, 364/364 complete Edit Mode, and 32/32 complete Play Mode.

### 3.2B — Physical Food, Water and Rest Interactions

**Complete.** Location-owned supplies, reservations, path-cost approach selection, timed interactions, and context orders were integrated with accepted needs and grid movement. Manual acceptance was subsequently confirmed.

- Focused Edit Mode: 25/25 passed.
- Focused Play Mode: 5/5 passed.
- Complete graphics-enabled Edit Mode: 340/340 passed.
- Complete Play Mode: 26/26 passed.
- Compilation and structural scene validation passed.
- Manual Play Mode checklist: all 20 checks accepted.

### 3.2A — Individual Needs and Survival Foundation

**Complete.** Three ants received independent Health, Hunger, Thirst, Fatigue, and Rested state.

- Focused Edit Mode: 63/63 passed.
- Focused Play Mode: 7/7 passed.
- Complete Edit Mode: 315/315 passed.
- Complete Play Mode: 21/21 passed.
- All 18 manual checks confirmed.

### 3.1C — Individual Skills and Learn-by-Doing

**Complete.** Seven universal skills, deterministic progression, and provisional inactivity decay were added to authoritative ant records.

- Focused Edit Mode: 40/40 passed.
- Focused Play Mode: 6/6 passed.
- Complete Edit Mode: 292/292 passed.
- Complete Play Mode: 20/20 passed.
- Acceptance recorded through the approved 3.2A transition.

### 3.1B — Persistent Identity and Attributes

**Complete.** Stable records, visual binding, castes, attributes, and duplicate protection were accepted.

- Focused Edit Mode: 16/16 passed.
- Focused Play Mode: 5/5 passed.
- Complete Edit Mode: 268/268 passed.
- Complete Play Mode: 19/19 passed.
- All 12 manual checks confirmed.

## 12 October 2026

### 3.1A — Ant Selection and Command Integration

**Complete.** Three selectable ants, independent move orders, UI click protection, and order lifecycle were accepted.

- Focused Edit Mode: 6/6 passed.
- Focused Play Mode: 4/4 passed.
- Complete Edit Mode: 258/258 passed.
- Complete Play Mode: 18/18 passed.
- All 14 manual checks confirmed.

### 2.4A–2.4D — Production movement transition

- **2.4A complete:** natural Earth/Stone grip, Prepared `1.25x` speed, and Sky falling; 56/56 focused Edit Mode, 227/227 full Edit Mode, 6/6 Play Mode, validators, and manual acceptance.
- **2.4B complete:** shared-map A* proof, smooth route following, falling/recovery, and recorded 1/100/300/500-agent benchmarks plus manual acceptance.
- **2.4C complete:** bounded fair scheduler, cancellation, terrain revisions, recovery, diagnostics, and successful deterministic request batches through 500 agents plus manual acceptance.
- **2.4D implemented, manual pending:** production prefab and per-world service integration. Later complete suites include 258/258 Edit Mode and 18/18 Play Mode, but the dedicated manual checklist remains open.

## 9 October 2026 and earlier

- Two-layer world data, classification, Tilemap projection, collision, and whole-body clearance progressed through accepted milestones 2.1B–2.2B.
- 2.2C capability automation passed 37/37 focused, 192/192 complete Edit Mode, 2/2 movement regressions, and scene validation; its dedicated manual Scene-view checklist remains pending.
- Bounded autonomous ground, grip, and gravity milestones established the reusable movement states later realigned by 2.4A.
- The original surface-movement prototype established a regression baseline, but its complete milestone acceptance remains unfinished.

For exact checklists, blockers, benchmark context, and retained limitations, consult `Documentation/CurrentGameState.md`.
