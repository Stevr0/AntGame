# Milestone History

This history records the strongest confirmed evidence without implying that later code reran every earlier checklist.

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

**Complete.** Authoritative ant records own independent stack-based inventories; excavation output atomically enters the excavator's inventory, falls back to a recoverable world drop, or splits explicitly. Player-directed adjacent pickup and Developer Only dropping use reservations and source-to-destination conservation.

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
