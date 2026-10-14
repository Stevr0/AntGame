# Design Decision Log

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
| 12 Oct 2026 | Make the builder self-supply construction | Separate haulers do not fulfil the builder's material-gathering responsibility |
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

## Open decisions

- Exact ordered-work productivity bonus and eligible actions.
- Job categories, interrupt thresholds, and explicit-order precedence.
- Drafted-ant survival intervention.
- Final resource stack sizes, recipes, work durations, and carrying balance.
- Room enclosure algorithm, door behavior, and quality formulas.
- Brood stages, timing, feeding thresholds, mortality, and overflow policy.
- Queen-death colony outcome.
- Combat, equipment, enemy behavior, and defence detail.
- Save format, procedural generation rules, and chunk streaming lifecycle.
- Surface terrain/coordinate rules, nest-entrance behavior and cross-view route costing.
- Loaded-hidden and unloaded-chunk simulation fidelity, cadence and reconciliation.
- Final need rates, skill curve, decay, and interaction values.

## Adding an entry

Record only decisions explicitly approved by the user or already marked authoritative in the GDD. Include the date, the decision, and its implementation consequence. If a topic is unresolved, add it to **Open decisions** instead of silently choosing a default.
