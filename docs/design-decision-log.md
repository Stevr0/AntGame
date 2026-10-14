# Design Decision Log

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
- Final need rates, skill curve, decay, and interaction values.

## Adding an entry

Record only decisions explicitly approved by the user or already marked authoritative in the GDD. Include the date, the decision, and its implementation consequence. If a topic is unresolved, add it to **Open decisions** instead of silently choosing a default.
