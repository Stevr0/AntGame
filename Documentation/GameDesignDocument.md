# ANT COLONY

## WV-B2B-ARCH-001 authoritative resource logistics — 15 October 2026

This approved correction supersedes every earlier Excavation-versus-Extraction
distinction and every builder self-supply rule. **Extraction** is the one skill
and activity for removing Soil, Stone, Seed, Twig and future resource nodes.
Successful Extraction always creates conserved loose ground output and never
fills inventory, Storage or Construction staging.

The authoritative flow is **Resource Node → Loose Ground Resource → Hauling →
Construction Site staging / Material Storage → Construction**. Hauling is the
only normal transport job. A Construction Site owns its required, reserved and
delivered quantities, may stage multiple recipe resource types, and becomes
eligible for Construction work only when every requirement is satisfied.
Storage is an alternative destination, not a prerequisite: the founding queen
can extract Soil, haul it directly to an unsupplied Storage construction site,
then construct the first empty Storage. Existing `Excavation*` implementation
names remain temporary compatibility names; player-facing terminology and the
authoritative skill/category are Extraction. Surface Seed/Twig interactions are
approved to use this contract but remain unimplemented; WV-C1 is not started.

## WV-B2B procedural world-generation foundation — 15 October 2026

> **IMPLEMENTED — AWAITING VALIDATION.** A versioned 64-bit seed now builds and
> validates the approved `64×64` Surface and `128×128` underground Colony before
> runtime publication. The explicit generated scenario contains one founding
> queen, one centred entrance, a `7×5` chamber and three-wide passage. The
> Mara/Pip/Tala fixture remains the default separate scenario. Generated source
> nodes are records and temporary visuals only; consumption, extraction,
> hauling, storage migration, brood and cross-region autonomy remain deferred.

## WV-B2A procedural world-generation contract — 15 October 2026

> **HISTORICAL WV-B2A APPROVAL RECORD; IMPLEMENTED BY WV-B2B ABOVE.** The authoritative technical
> contract is `ProceduralWorldGenerationSpecification.md`. One world seed will
> deterministically produce separate top-down Surface and side-on underground
> Colony regions. The Surface is predominantly Grass; the Colony is
> predominantly solid Dirt/Soil. The Surface entrance is approximately central,
> and the Colony exit is approximately central on the topmost underground
> boundary. Exactly one small chamber and its minimum connecting passage begin
> excavated, with the founding queen spawning validly inside that chamber.

This approved start supersedes older new-game text that places the founding
queen on the Surface or begins with no excavated chamber. It does not alter the
Mara/Pip/Tala development fixture, the WV-B1
`entrance.founding-nest.001` transaction, existing excavation behavior or any
accepted runtime system. Terrain, natural sources, loose physical resources and
the entrance remain separate records. Procedural generation, source interaction,
persistence, streaming and autonomous cross-view routing are not implemented by
this decision.

### Approved unified cell, node and placement model — 15 October 2026

> **APPROVED DESIGN; IMPLEMENTED FOR GENERATED DATA BY WV-B2B.** This WV-B2A amendment supersedes older
> conceptual descriptions of Surface sources as a separate world model and of
> Sleeping/Storage/Nursery functions as natural Background replacements. Existing
> runtime representations and development fixtures remain intact until an
> authorised migration.

Every resident Surface or Colony cell has exactly one natural Background and may
have zero or one of each: Solid/Resource Node, Construction/Placement and loose
physical resource stack. Compatibility rules decide which optional occupants
may coexist. These are logical data layers, not a requirement for four Tilemaps.

- **Background:** natural environment that always remains, such as Grass, Soil or
  Stone. Removing/depleting a node reveals the same Background.
- **Solid/Resource Node:** one finite node with stable identity, definition,
  location, quantity, work/skill/output and depletion rules. Soil/Stone walls and
  non-blocking Seed/Twig sources use the same node principle; obstruction is
  definition-specific.
- **Construction/Placement:** a player-created or authored function such as a
  Sleeping Spot, Storage, Nursery or Door. Construction normally requires no
  node, no existing placement and no incompatible loose stack, consumes physical
  materials and preserves the natural Background.
- **Loose stack:** one resource type per cell, bounded by its stack rules.
  Compatible units merge; different loose types cannot share a cell. Storage is
  not required for a valid ground stack.

Storage is a Construction-owned capacity, not a Background or an independent
ground stack. Where the 1:1 rule applies, one Storage placement provides one
resource slot/capacity unit; the slot may hold one compatible type up to its
approved stack/capacity. Extraction/excavation uses one exact-once node
transaction: prepare a legal physical output, reduce quantity once, remove the
depleted node while retaining Background, and create/merge output once. Work
completion never auto-fills inventory. Direct eating/drinking remains a separate
source-consumption transaction. Decay is approved for later design but excluded
from WV-B2B.

## WV-B1 two-view foundation — 14 October 2026

> **IMPLEMENTED — AWAITING VALIDATION.** WV-B1 adds the first bounded top-down Surface, manual Colony/World presentation switching, Surface-local movement and one physical capacity-one nest transition. It deliberately adds no Surface resources, foraging, procedural generation, cross-view autonomous work, unloaded simulation or additional entrances.

`AntRecord` remains authoritative for identity, needs, skills and inventory. A separate residency registry owns one typed Colony or Surface location for each ant. The development fixture keeps Mara, Pip and Tala and the existing Colony world loaded; changing the camera does not move an ant, reset its state or alter the simulation clock. The Surface is a deterministic `64×64` logical grid with a `32×32` future partition seam and fixed temporary obstacles.

The first entrance is `entrance.founding-nest.001`, with capacity one. A selected ant travels through its current local navigator to the authored approach cell before an idempotent residency transaction can commit. Pause prevents commit; invalid/occupied destinations and representation activation failure retain or restore source residency. WV-B1 reuses the same bound development GameObject while switching its enabled local motor instead of destroying and recreating a projection. This is a narrow reversible implementation choice; the stable ant record, binding claim and residency—not the GameObject—remain authoritative.

The authoritative WV-A rule that Colony navigation retains its accepted grip and falling behavior still applies. WV-B1 adds no gravity or falling to Surface movement and does not refactor Colony movement.

## Approved WV-A two-view foundation decisions — 14 October 2026

> **DESIGN HISTORY:** WV-A approved this contract before WV-B1 runtime implementation. WV-B1 now implements only the typed regions, bounded Surface, cameras, local movement and one entrance. Resource, rest, cross-view autonomy and persistence portions remain deferred.

1. **Initial entrance:** One pre-authored, bidirectional nest entrance links one explicit Colony View endpoint with one World View endpoint. Transition capacity is one ant at a time. Additional entrances and player-built entrances are deferred.
2. **Player view switching:** Manual Colony View / World View selection, remembering separate camera positions. Changing camera/view changes presentation and input context only; it must not teleport ants, restart a region or pause the shared simulation.
3. **Physical ant travel:** An ant travels through its current region to the entrance before a validated transition to the other region. The same authoritative ant, needs, skills, job and inventory persist; a failed transition must leave recoverable state with no double residency or duplicated materials.
4. **Initial natural sustenance:** The first small surface will contain finite seed and freshwater sources near the entrance. Foraging discovers sources, extraction produces physical recoverable output; direct consumption at a source is allowed without constructing storage.
5. **Provisional water transport:** One unit of physically accounted liquid payload may use existing ant inventory/transfer accounting as an initial simplification. Water extraction, containment, recovery/drop semantics and source/storage transactions require explicit design in WV-A/WV-C; do not silently treat water like a loose Stone block or invent containers.
6. **Natural rest:** Ants may rest at safe accessible natural positions before sleeping facilities exist. A constructed Sleeping Placement provides a dedicated one-ant spot with faster recovery; quality-room bonuses remain future work.

Both small starting regions should initially remain loaded and share the accepted clock, autonomy and authoritative ant records. Unloaded-region simulation, larger surface chunks, predators, cross-view autonomous work, production bootstrap and brood remain future separately approved milestones. The Mara/Pip/Tala development test scene remains intact.

The implementation-ready technical contract is `WorldViewTechnicalSpecification.md`. Its typed region/location boundaries, capacity-one entrance transaction, single-residency guarantee, manual camera/input lifecycle, finite-source accounting, recoverable one-unit liquid payload, natural-rest validation, failure matrix and WV-B/WV-C acceptance gates are authoritative technical refinements of these decisions. Its explicitly labelled recommendations still require the approvals listed in that document; it does not authorise runtime work.


## Approved founding-colony survival direction — 14 October 2026

> **APPROVED DESIGN; IMPLEMENTATION DEFERRED.** This supersedes any assumption that a new founding colony starts with prebuilt food, water or sleeping fixtures. The three-ant development scene remains a testing fixture.

- **Start (superseded by WV-B2A on 15 October 2026):** The earlier Surface/no-chamber start is historical. The current approved new-game start is one founding queen in one small excavated underground chamber directly beneath the top-boundary Colony entrance, with no preconstructed Sleeping, Food Storage, Water Storage, Material Storage or Nursery placements.
- **Foraging discovers** food, water and other resource sources; **extraction physically removes/collects** finite resources and generates recoverable loose output. **Hauling** transports extracted materials. **Construction** self-supplies physical materials from inventory, storage or loose drops.
- **Direct survival:** Ants may eat/drink at an accessible natural food/water source without requiring constructed storage. Natural sources must obey future finite-source/access/consumption rules; extraction and water transport need their own approved technical specification. Do not assume water is stackable like stone or that discovery automatically extracts it.
- **Rest:** Ants may rest at a physically safe, accessible natural position before any dedicated sleeping space exists. Constructed Sleeping Placements grant one dedicated resting position per cell with improved recovery efficiency; later enclosed-room quality may enhance efficiency but never gates basic use.
- **Storage and nursery:** Food Storage, Water Storage, Material Storage and Nursery are Constructions/Placements built through resource-backed Construction jobs; each placement provides its previously approved 1:1 capacity where applicable. The current functional-Background representation is a compatibility implementation awaiting migration. Unenclosed initial chambers can function; future doors/enclosure provide efficiency bonuses.
- **Progression intent (updated by WV-B2A):** Begin in the generated starting
  chamber → reach nearby natural sustenance through the linked entrance → expand
  the chamber through excavation → recover/rest naturally → construct sleeping
  and food/water storage → transport resources → prepare nursery → authorise
  first brood → expand workforce. Brood, generated-world runtime, natural-source
  resource interactions and storage efficiency bonuses are not implemented by
  this decision.
- **Testing exception:** Preserve finite pre-authored Food, Water and Sleeping fixtures for Mara/Pip/Tala in AntGameDevelopment; they are not the eventual new-game bootstrap.


## Approved resource acquisition separation — 14 October 2026

> **Status: APPROVED AND IMPLEMENTED BY MILESTONE 3.5B; MANUAL ACCEPTANCE PENDING.** This decision supersedes every older excavation auto-pickup, inventory-first and inventory-capacity-gated rule retained later in this document as historical context.

- **Foraging** discovers or identifies resources and their locations; it does not collect them automatically.
- **Extraction** mines, harvests or otherwise removes an identified resource. Successful Colony View Soil or Stone excavation always creates the defined quantity as a physical, recoverable loose world drop at the excavated authoritative cell. It never adds output directly to the excavator's inventory, regardless of available capacity or storage.
- **Hauling** reserves, picks up and transports already-extracted physical material through the existing inventory and Material Storage rules. Excavation completion does not assign a hauling job.
- **Construction** performs building work only after Hauling has delivered every required material to site-owned staging; material need not pass through Storage first.
- Output preparation and foreground removal are one exact-once transaction. A stable job key prevents duplicate output; if the configured yield cannot be represented safely under the existing bounded stack rules, the foreground Solid remains unchanged.
- **Implemented compatibility behavior:** excavation output currently uses the
  excavated cell, fills compatible partial stacks and may create one new bounded
  stack; the current registry permits multiple traversable stacks at one cell.
  **Approved target:** one loose resource type/stack per cell. Migration must use
  a validated deterministic alternate cell or preserve the node/terrain and fail
  without material loss. Existing fixture data is not silently rewritten by
  this documentation amendment.

This separation applies to future World View extraction as a design rule, but it does not authorise World View terrain, navigation, foraging AI or new resource types.

## Milestone status update — 14 October 2026

**3.4C Player Colony HUD & Developer UI Separation is MANUALLY ACCEPTED.** All 13 grouped Unity manual checks passed, including resolution checks and clean Console. This confirms the current Colony View player uGUI, shared simulation controls and optional developer IMGUI; it does not authorise or implement World View, entrances, surface resources or cross-view routes. The separate art foundation awaits visual acceptance.


## Implementation status update — 14 October 2026

**3.4B Unified Simulation Clock & Gameplay Mode is manually ACCEPTED.** All 20 grouped Unity Play Mode acceptance checks passed, including normal autonomous gameplay, pause/resume safety, developer override compatibility and clean Console. This status update does not change the approved mechanics or the earlier six design decisions.

**3.4C Player Colony HUD & Developer UI Separation is manually ACCEPTED.** Milestone 3.4D now implements the approved global colony work-priority matrix over the same authoritative ant records; its manual acceptance remains pending. Unity uGUI is the player-facing interface and optional IMGUI remains reserved for developer tests. Mara/Pip/Tala remain temporary test ants and the single founding-queen start remains a future progression task.


## Approved player gameplay and interface direction — 14 October 2026

> **Status: 3.4B and 3.4C MANUALLY ACCEPTED; 3.4D IMPLEMENTED — PENDING MANUAL ACCEPTANCE.** This section remains authoritative and supersedes earlier contradictory suggestions. Existing production features and the accepted developer test harness remain preserved.

## Approved two-view gameplay architecture — 14 October 2026

> **Status: APPROVED DESIGN, NOT IMPLEMENTED.** This section defines the intended relationship between the existing underground game and a future surface game. It does not authorise a World View implementation milestone, select final technical class names, or claim that cross-view travel, surface terrain, streaming or offscreen simulation exists.

Ant Game has two interconnected perspectives over one persistent colony simulation:

| Perspective | Presentation | Primary responsibilities |
| --- | --- | --- |
| **Colony View** | Existing 2D side-on underground view | Excavation, construction, room designation, brood management, storage and other colony activities |
| **World View** | Future 2D top-down surface view | Exploration, surface foraging, resource collection and, later, predators, rival colonies and wider environmental interactions |

### Colony View

Colony View preserves the established permanent-Background/optional-Solid terrain model and its grounded, gripping, climbing and falling movement rules. It remains the detailed nest-management space: the player shapes underground terrain, constructs functional colony areas, manages physical storage and eventually supports rooms and brood. Adding World View must not flatten or replace these side-on terrain and locomotion rules.

### World View

World View is a large, continuous, chunk-based surface environment presented from above. Its local terrain representation and top-down movement rules may differ from Colony View and require a separately approved specification. The first playable World View milestone must deliberately use a small bounded surface area; additional chunks, biomes and interactions expand incrementally only after the small-area loop is stable. Predators, rival colonies and broader environmental systems remain later design direction, not part of the initial surface slice.

### Shared ants and simulation

- An ant is one persistent individual across both views. Identity, colony, caste, attributes, skills, needs, inventory, lifecycle and job information remain authoritative and must not be copied into view-specific records.
- View-specific visuals and movement representations are projections of that shared ant. A transition must move one ant between region residencies without creating a second authoritative or live representation.
- Nest entrances connect specific underground and surface locations. They are the required boundary for ordinary cross-view travel rather than an unexplained teleport between unrelated coordinates.
- Switching the player's camera/view changes presentation and input context, not the simulation clock. Both regions continue simulating while the player observes either view.
- Autonomous work must eventually be able to plan through an entrance and execute a route composed of local Colony View travel, a transition, and local World View travel.
- Loaded regions may use their full local simulation. Future unloaded chunks require a deterministic lightweight simulation policy that preserves important ant, need, inventory, job and travel outcomes without pretending to run full physics or frame-by-frame navigation.

### Cross-view gameplay loop

The colony creates reasons to visit the surface; the surface returns resources, knowledge and risk to the colony. Early cross-view play should remain narrow: leave through a known nest entrance, explore a small nearby surface, forage or collect a supported resource, and return through an entrance with the same ant and inventory. Later expansion may add distant environments, multiple entrances, predators, rival colonies and longer autonomous logistics routes. Each extension requires its own scoped design and acceptance criteria.

### Resolved WV-A foundations and remaining decisions

WV-A resolves one known pre-authored entrance, capacity one, manual view switching with independent remembered cameras, physical approach travel, atomic single residency, both initial regions loaded on one clock, finite nearby seed/freshwater, direct consumption, a recoverable one-unit liquid payload and safe natural rest. View switching is presentation only and never moves an ant.

WV-B1 has since fixed the development Surface dimensions/origin, future partition
seam, obstacle rules and authored composition. WV-B2A now specifies the generated
layout contract. Remaining approvals are narrower: generated Colony dimensions
and chamber geometry, final terrain/source weights and IDs, generation versions,
entrance animation/timing, initial source discovery presentation, seed stacking,
water-cache presentation, natural-rest rate, revised restoration values and
eventual save format. Cross-view autonomous work is WV-D; unloaded simulation is
WV-F.

Ant Game's player experience follows **Pause → Plan → Play → Observe → Adjust**. The normal player interface must be distinct from developer testing controls, using RimWorld as a reference for understandable colony management rather than copying its world model. The game's side-view excavatable terrain, climbing ants, permanent Background and functional background conversions remain distinct.

### Approved decisions

1. **New game starts paused.** Player may inspect ants and terrain, move/zoom camera, designate excavation or Construction/Placement work, and change work priorities before time advances.
2. **Direct orders may be issued while paused.** They are accepted and displayed immediately, but must not execute, move ants, advance a route, perform work, complete transfers, or expire retries before resuming. Safe order replacement/cancellation semantics still apply.
3. **Production UI uses Unity uGUI Canvas.** The existing IMGUI developer toolbar is retained only for optional Developer Mode. Normal play does not require opening developer tools.
4. **Display only implemented work categories in the player UI.** Future/synthetic categories remain absent from the normal work interface until their real executors exist; developer diagnostics may still expose them.
5. **Mara, Pip and Tala remain the temporary three-ant development fixture** while building and testing the HUD. This does not supersede the approved eventual single founding queen beginning.
6. **Colony alerts notify rather than automatically pause.** Automatic pausing for future critical events requires its own design decision.

### Unified simulation contract (3.4B)

Normal gameplay has one simulation Play/Pause state and provisional 1×, 2×, 3× running speeds; on running, autonomous survival, colony work and hauling are activated as one default gameplay policy. These are *not* separate player switches. Developer Mode may retain explicit individual overrides for testing. Pausing preserves order/job ownership, reserved and staged materials, construction progress and pending navigation rather than disabling autonomous systems or cancelling tasks. Simulation needs, skill decay, job work, interactions, movement, physics, navigation scheduling, retry/backoff and reservations must not advance while paused. Player-facing camera, inspection, selection, designation, priority and direct-order authoring remain interactive.

A shared authoritative simulation clock should replace gameplay use of unscaled wall-clock AI/retry timing. The simulation clock, physics, navigation and work progress must be tested for comparable simulation outcomes at 1×, 2× and 3×. Unity Editor Play Mode is distinct from the in-game simulation Play state.

### UI separation and phased scope

- **3.4B:** Authoritative simulation clock, gameplay automation bundle, consistent pause/speed semantics, minimal temporary player time-control strip. Avoid wholesale controller refactor or full HUD.
- **3.4C:** Authored uGUI player HUD with roster, selected-ant information, resource summaries, excavation and background-conversion tools, time/speed controls; developer IMGUI hidden by default but accessible.
- **Later:** Work-priority matrix, needs/skills/inventory views, actionable alerts and contextual work orders, incrementally.
- **Deferred:** Doors, enclosed rooms/quality, brood, production, combat/Draft Mode, queued orders beyond the approved paused immediate-order acceptance. No new game systems should be claimed as implemented by this UI approval.


**Game Design Document | Version 3.6 — order-based control, 12 October 2026**

Working title • Unity 6 • Single-player • interconnected 2D side-on and top-down views • PC-first prototype

Design intent: Build a small, finishable colony simulation in which individual ants matter. The player directs excavation and assigns work; ants physically perform tasks and develop through experience.

## Authoritative design realignment — 12 October 2026

> **Status: APPROVED DESIGN, NOT IMPLEMENTATION CLAIM.** This section supersedes contradictory design assumptions elsewhere in this document, the roadmap and AGENTS.md. Earlier milestone and testing narratives are retained as historical records. No gameplay code has been changed as part of this design update. Numerical balance and unresolved details remain provisional.

### Vision and control

- Ant Game is a single-player, scrollable/zoomable 2D colony simulation with a side-on underground Colony View and a top-down Surface World View in one persistent world. WV-B1 implements the bounded two-view foundation; procedural generation and later expansion remain future work. Players can manage colony-wide assignments **or directly select and control any adult ant**, including the founding queen; other ants remain autonomous.
- Manual and AI control use the same movement, excavation, hauling, survival and combat rules. Manually performed tasks receive a modest explicit productivity bonus (percentage and eligible actions **TBD**); that bonus must not duplicate rewards or permit impossible actions.
- Each ant is a persistent individual with stats, skills, equipment, health, energy, hunger and thirst. All ordinary work skills start at 0 and improve through practice, not fixed classes. Roles affect autonomous job priorities, not normal task eligibility. Retain previously approved 0–100 and above-50 skill-decay conventions until explicitly revised.
- Individual ant death is permanent. The fate of the colony after queen death remains undecided.

### Founding queen, brood and nursery

- The first controllable ant is a **working founding queen**, not an ordinary worker awaiting transformation.
- Early loop: excavate an accessible chamber; designate/conversion-build at least **three Nursery Background tiles**; explicitly authorise the initial brood of **three larvae**; physically feed them on multiple occasions; surviving brood eventually matures into three individually controllable general-purpose workers.
- Subsequent reproduction requires **explicit player authorisation**; it is never triggered solely by spare food/space. The queen lays eggs/brood; precise egg→larva→pupa→adult presentation, timing, feeding thresholds, further brood sizes and brood mortality are **TBD**.
- Each functional Nursery Background tile provides exactly **one brood slot**. A nursery works while unenclosed; enclosing it properly can grant room-quality efficiency bonuses, not extra base capacity. Removing occupied nursery capacity pauses development without silently deleting brood. Need a defined overflow/reassignment policy.

### Authoritative terrain and movement changes

- Preserve the authoritative generated chunk/background/foreground data model. Foreground Solid tiles block physical occupancy as configured; digging removes eligible foreground only and never the permanent Background Tile.
- **Every exposed, intact natural earth/stone background is intrinsically grippable and traversable**, subject to actual body clearance, collision, valid movement directions and chunk availability. Background preparation is **not** required to climb. Preparing a background improves movement speed (road-versus-gravel analogy); its initial multiplier is **TBD**. Sky or a background material explicitly defined as without grip does not grant grip; falling remains possible without ground or grip.
- Natural, Prepared and Functional Background materials must support shared ant traversal; function and locomotion are distinct properties. Do not use prepared-only climbing as a gameplay rule. Adapt existing capability evaluators and motors only in a separately approved implementation milestone with regression tests.
- The world must eventually support procedural generation, durable tile and entity state, and chunk loading/unloading without treating unknown chunks as exposed empty space.

### Functional backgrounds, construction and physical goods

- **Construction/Placement records define colony uses and capacity**, including Nursery, Sleeping, Food Storage, Water Storage, Production and Burial. A placement generally grants one matching capacity slot (**1:1**). Food/water stockpile slots may hold one physical compatible type/stack; exact stack limits **TBD**.
- Player designates construction on a compatible accessible cell; an ant **physically performs a Construction skill job using required resources** while the natural Background remains. Replacing/removing a placement later requires a new job and accounted material handling. Construction improves through doing. Existing functional Background conversions are implemented legacy compatibility, not the target data model.
- Excavated eligible Solid tiles yield **physical haulable resources**; no automatic global inventory credits or resource deletion. Ants transport them to storage/work sites. Interaction requires adjacency and physically present items at storage locations.
- At need, autonomous ants target the **closest reachable available** relevant tile by path cost, unless a valid player-assigned spot overrides it (e.g. a personal sleeping spot). Avoid simultaneous capacity claims with reservation/occupancy rules.
- Shared colony Food and Water Storage supplies are accessible to all ants. Each ant has Hunger and Thirst values that decrease over time and are replenished by consuming physical supplies at valid accessible storage. Lack of food eventually decreases health; lack of water blocks skill experience gains. Exact thresholds/rates and the relationship between water shortage and other stats remain **TBD**.
- Each ant needs access to sleeping accommodation to gain Rested benefits; assigned sleeping spots are supported. Rest gives increased work speed and skill experience, with bonuses gradually wearing off as fatigue returns. Deleting a sleeping tile wakes its occupant; deleting storage tiles detaches contents into loose items; removing nursery support pauses affected brood development.

### Enclosed rooms and scoring

- A **room is a fully enclosed contiguous space**, bounded by blocking terrain/structures, whose every entrance/opening has an installed door. Ants construct doors from physical materials using the Construction skill.
- Room function is derived from the tile functions **inside it**; mixed-use rooms are allowed. Enclosed, specialised rooms receive RimWorld-inspired room-quality efficiency bonuses based on room scoring (**formula and thresholds TBD**). No door or enclosure is needed for base functionality, including the founding nursery.
- Functional tile identity controls **what work is possible**; room detection/scoring controls **how efficiently** work occurs. Room quality does not change the 1:1 physical capacity rule. Door passability, enemy interactions, detection of openings, mixed-use scoring and large/chunk-spanning room constraints require design/technical specification.

### Enemies and equipment

- Every adult ant can be manually controlled for combat under the same damage, range and cooldown rules as autonomous ants. Rival colonies, surface and underground enemies, individual equipment (e.g. boots and saddlebags), and deeper terrain materials remain part of the vision but detailed balancing/design are **not yet approved for implementation**.

### Open dependency register (design not yet approved)

1. Exact grippable-background material definitions, speed modifiers, sky/open-air transitions and interaction with the existing 2.3B tests.
2. Procedural generation topology, streaming boundaries, persistence/versioning and offscreen simulation.
3. Door placement, opening/enclosure detection, chunk boundaries, automatic door operation and room scoring/specialisation formula.
4. Physical item/stack and carrying capacity rules; storage overflows, reservations, conversion resource refunds/interruptions.
5. Construction recipes, time/cost, permitted build positions and tile replacement when occupied.
6. Larva maturation stages/timing, feeding quantities, egg mechanics, brood care assignment, death/overflow policies and later brood sizes.
7. Individual Hunger/Thirst/Energy decay rates, Rested bonus/fatigue curve, skill-loss exceptions and assigned-spot fallback behaviour.
8. Job scheduler priorities, nearest-by-path destination choice, interruption/recovery and direct-control takeover.
9. Manual productivity bonus scope/value and effect stacking with skills, equipment and room-quality bonuses.
10. Queen death/colony continuity, ant equipment/inventory, combat and enemy-colony progression.
11. World View surface terrain and coordinate model, nest-entrance rules, cross-view routing and unloaded-chunk simulation fidelity.

### Implementation guardrail

Before another gameplay milestone, perform a **read-only architecture/code-to-design audit** comparing the present Unity 6 project with this new baseline. Record each affected system as Keep / Refactor / Replace / Defer, identify potentially invalid tests and migration steps, and propose incremental milestones. Preserve the manually validated 2.3B movement baseline as historical evidence; it does **not** verify the new natural-background movement rule. Do not refactor, delete scripts, modify assets or implement this design without approval.

---

## Approved construction and physical-resource logistics — 12 October 2026

> **Status: approved design only.** This section augments the authoritative design realignment above and supersedes incompatible older MVP logistics assumptions. No Unity code, assets, tests or prefabs are implemented by this documentation update. Follow the pragmatic, explicit, physical-job-and-stockpile style of *RimWorld*, without presuming its exact numerical balancing or architecture.

### Physical materials and inventories

- Excavation produces physical resources under the 14 October 2026 rule above: successful output is always a recoverable loose world drop and never an automatic inventory transfer. Neither extraction nor later pickup can silently destroy or duplicate resources.
- One inventory slot may hold a **stack of identical compatible items**. Stack maximum per material is **TBD**. Early carrying limits use **slot count only**, not a weight or mass simulation. The existing concept of Hauling skill increasing carrying slots remains a balancing/design constraint to be reconciled with stacks and later saddlebags.
- Resources retain physical locations and quantities in world drops, ant inventory, storage tiles and delivered construction-site materials. The colony may display aggregated totals, but totals are **derived views**, not teleportable resources.
- Add **Material Storage** as a Construction/Placement. As with other functional storage, one placement grants **one physical storage slot** (one compatible type/stack). Optional player-configured material filters can restrict accepted resource types. Food and Water Storage remain distinct shared stockpile types. Stored items are placement-owned physical contents, not ground stacks, and require adjacency to deposit/withdraw.
- If an occupied storage tile is removed or changed, contents become loose, recoverable physical items, consistent with prior design decisions.
- When an ant requires inventory capacity for its current task, it automatically unloads **unnecessary** carried material. It retains material required by the current task, reserves and physically travels to the closest reachable compatible storage with capacity, deposits only enough to free the required capacity, then resumes the original task. If no compatible reachable storage is available, it places the unnecessary material at a valid nearby recoverable world-drop position. Bounded retries apply when neither transfer is currently possible. Player orders and critical survival may interrupt this recovery without duplicating or deleting resources.
- Task-reserved material and material already staged at a worksite are not eligible for automatic unloading. Partial stacks and mixed-resource inventories follow the same physical reservation and conservation rules.

### Loose-resource clutter

- Loose resources are allowed and must never block ordinary excavation, construction or movement solely because storage is unavailable. World drops remain traversable.
- Future loose-resource concentration may apply a movement-speed penalty, and loose resources inside an enclosed room may reduce cleanliness or room quality. Stored materials do not count as loose-resource clutter.
- These future penalties are incentives for organised hauling and storage, not hard progression restrictions. Concentration thresholds, movement penalties, cleanliness values and room-quality deductions remain **provisional/TBD** and are not part of Milestone 3.4A.

### Resource-backed construction and conversion

- Player designation creates a resource-requiring task for doors or functional-background conversions. All such jobs require an accessible adjacent interaction position and use **Construction skill** for work and progression.
- **Hauling supplies Construction Sites.** A hauler reserves one physical source quantity and one explicit destination capacity, carries the material, and commits it to that site's staging. Pending site needs take priority over optional Storage when compatible.
- Staged material belongs only to its site. Another builder may perform the later Construction work, but Construction never searches for, collects or transports resources.
- Construction jobs represent designation, awaiting fully staged materials, constructing, completion, pause/interruption, cancellation and inaccessible/invalid states. Hauling owns gathering and transport states.
- Reserve **physical quantities** rather than abstract colony totals so two jobs cannot consume the same item. A reservation is not consumption. Materials delivered to a construction site are **consumed only in an accounted transaction** as construction successfully progresses/completes according to later-approved accounting rules.
- **Cancelling a construction job leaves all unused materials physical and recoverable** in ant inventory, at the worksite, or as loose world items; do not teleport them to former stockpiles. Job cancellation must release any unconsumed resource reservations. Already irreversibly consumed resources are not automatically refunded; stage of consumption is **TBD**.
- Recover safely from loss of builder, interrupted work, death, inaccessible stores, unloaded chunks, invalidated terrain, full carrying slots and world changes. On ant death, carried items should remain physical and recoverable; exact drop placement policy is **TBD**. A construction job must never silently swallow cargo or duplicate materials.

### First-colony bootstrap and unresolved balancing

- The founding queen must be able to extract, haul and construct initial functional placements from reachable resources without existing workers, Storage or prior stockpiles. Initial nursery is functional before enclosure and doors.
- **Unresolved (do not hardcode as approved):** construction recipes and costs, exact materials available at start, stack-size limits, carrying-slot progression and any equipment modifiers, conversion durations, reservation expiry, exact nearby placement choice when every candidate drop location is invalid, point of material consumption, whether partial construction persists, stockpile filters UI, clutter thresholds/penalties and performance update cadence.
- Testing covers resource conservation (extract/pick up/drop/stage/store/consume), stacking, one-slot capacity, multi-trip Hauling, reservation contention, cancellation at multiple stages, ant death, tile removal, persistent save/load and bootstrap without stockpiles.

## Approved job and priority system — 12 October 2026

> **Status: approved design; not implemented or verified.** This section extends the authoritative design realignment and construction/resource logistics above. Contradictory older AI-only or fixed-role descriptions are superseded.

### Work settings, roles and decisions

- Every adult ant has an individual **work priority matrix**, with each ordinary work type assigned priority **1 (highest), 2, 3, 4 (lowest), or Disabled**. Disabled means do not autonomously select that work type; it does not erase skills or forbid direct/manual commands. Exact list of categories and restrictions is **TBD**.
- Colony-wide designations and task demand coexist with ant-specific priority matrices. Roles are preferences/default configurations, **not rigid classes or prohibitions**. All ants can develop skills through doing. Skills improve effectiveness but **do not automatically overrule explicit player-set priorities**.
- An idle available ant first filters feasible, accessible, unreserved, enabled work. It chooses the **highest-priority work category with available valid jobs**, then the **closest reachable available job** within that category, using actual viable path cost rather than straight-line distance. Deterministic tie-breaking, job ageing/fairness, unreachable retry throttling and route cache invalidation remain **TBD**.
- The player can both **directly control an adult ant** and issue a **specific automated job order** to that ant without steering it. In direct control, the ant stops selecting autonomous jobs; the rest of the colony keeps operating. Direct/manual tasks use the same physical actions and resource rules as AI, with the separately agreed modest manual productivity bonus (**numeric value and applicability TBD**).
- Work matrices and player orders are durable individual/colony state for eventual saving. Specific order precedence, cancellation, persistence across takeover, and whether Disabled work can be explicitly ordered are **TBD** and must not be invented during coding.

### Personal needs and emergencies

- **Critical survival overrides ordinary work** (and may interrupt player-issued autonomous jobs). Ordinary hunger/thirst/fatigue needs wait for a **safe work interruption**, preventing constant task switching. Exact danger priorities, thresholds, starvation damage, dehydration/skill blockade, emergency cancellation and direct-player-control safety semantics are **TBD**.
- If a need cannot be satisfied because supplies or a destination are missing, ants must indicate the reason rather than spin in an impossible request loop. Existing approved shared food/water storage, individual gauges, sleeping spot, gradual Rested buff decay and survival effects remain authoritative.
- **Current provisional development balance (14 October 2026):** Hunger loses 1/hour, Thirst loses 1.4/hour and awake Fatigue gains 1.4/hour; rest recovers 16 Fatigue/hour. Autonomous intervention begins at Hunger 40, Thirst 40 or Fatigue 70. Eat and Drink each require 0.5 simulation hour and one physical unit, restoring the unchanged 40 Hunger or 50 Thirst. Critical Hunger 10, critical Thirst 10, critical Fatigue 90 and starvation damage 2 Health/hour are unchanged. These are measured development values, not final production balance.
- The development fixture remains finite: two reachable Food stores hold 12 units each, two reachable Water stores hold 16 units each, and three sleeping positions allow the three-ant test population to rest concurrently. The unreachable food fixture remains failure-test data and is excluded from supply sufficiency.

### Job execution, ownership and recovery

- Job claims/reservations prevent two workers from consuming the same materials, taking the same exclusive destination, or working on an exclusive tile simultaneously. Job completion, failure, loss of access, death, changed tile state, assignment changes and manual takeover must release or transfer reservations without duplication.
- **Valid partial job progress persists across safe interruption or handoff** so a later eligible ant can continue rather than restart; the definition of valid/invalid progress and expiry remains **TBD**.
- Preserve the `WV-B2B-ARCH-001` separation: Extraction creates loose output, Hauling alone supplies staging or Storage, and Construction performs work only at a fully supplied site.
- For initial implementation, prefer event-driven/controlled-rate decisions over every-ant/every-frame global job scans. Proposed components or job-state names are architectural candidates, **not approved class names**.

### Open dependencies — do not silently choose defaults

1. Final job categories, per-ant defaults, global work demand/order priority and Disabled/manual-order interaction.
2. Forced automated-order preemption, queueing, cancellation and return to prior work when manually controlled.
3. Need thresholds, critical emergency precedence (combat/danger versus hunger/thirst/rest), safe interruption definition and noninterruptible stages.
4. Path-cost comparison across chunks, tie-breakers, reservation timeouts, inaccessible job reporting and contention/fairness.
5. What progress persists for digging, construction, crafting, brood care and hauling; how resources are handled during handoff.
6. Rest/work-speed and skill-XP multipliers, stacking with manual bonus, and impact of zero water on XP.
7. Save/load of work matrices, jobs, current actions and reservation recovery.

### Acceptance expectations for a future authorised implementation

Demonstrate two or more autonomous ants with different priority matrices choose priority-before-distance; an inaccessible nearest target is skipped; explicit automated order versus direct control; critical needs override ordinary work only when warranted; interrupted Extraction/Construction progress persists; exclusive source/site/job reservations avoid double consumption; Hauling supplies sites before Construction begins; other ants continue working during direct control.

## Approved command-based player control — 12 October 2026

> **APPROVED design; not yet implemented.** This is the authoritative replacement for prior direct WASD gameplay control descriptions, including any earlier approved continuous player-control plans. Existing prototype movement controls remain developer-only regression tools.

### Three control states
- **Autonomous (default):** ant chooses jobs according to its individual 1–4/Disabled work matrix, colony tasks and personal needs.
- **Ordered (temporary):** player selects an individual ant and issues an explicit action/job order (such as excavate, haul, build, feed brood). The ant autonomously navigates and executes the commanded task using the same movement, reservation, resource and action systems as ordinary AI. The temporary order takes precedence over normal work priorities; when successfully completed, cancelled or impossible, the ant returns to autonomous job selection. Critical personal survival needs may interrupt work orders according to safe-interruption rules. Player may give specific automated orders without manually steering the ant.
- **Drafted (persistent tactical mode):** player drafts one or multiple ants to give tactical movement, attack, hold-position, defend-area and retreat orders for threats, defence and raids. Drafted ants do not select ordinary colony work until explicitly undrafted. Draft status persists through individual tactical commands. Hunger, thirst and fatigue still progress with warnings; exact forced-survival intervention during drafting remains unresolved.

### Explicit orders and productivity
- **Normal gameplay has no continuous WASD direct steering** and no second player-only locomotion implementation. Selecting an ant enables inspection and explicit orders; navigation and execution remain ant-driven.
- The previously approved manual productivity advantage is now an **explicitly ordered work-task bonus**: a modest work-speed benefit during eligible ordered task execution only, not travel, idle time, or unrelated work. Reissuing an order must not stack bonuses. **Exact percentage, eligible tasks and combination with skill/equipment/room bonuses are TBD**, not settled at 25%.
- Direct and autonomous combat share the same damage, targeting, weapon, range and cooldown rules. Drafting changes command authority and task selection, not combat statistics.
- The player can select multiple ants for tactical commands in Draft Mode. Group formation, path congestion, targeting precedence, drafted ant needs intervention, and queueing orders are TBD.
- The existing player-controlled movement prototype, including `PlayerMovementInput` and surface/corner/step-over tests, is historical developer tooling to preserve until approved migration. It must **not** be treated as evidence that player-facing continuous steering remains in the shipped design.

### Order lifecycle and dependencies
- Each ordered task must be observable (target, owner, state, reason for failure) and return the ant to autonomy on success/cancel/impossibility. Drafting and undrafting must not duplicate work, items or reservations.
- Releasing previous reservations and preserving valid partial work must follow the approved job/reservation rules. Exactly when previous autonomous work is paused, released or resumed on issuing a manual order is **TBD**.
- Exact UI interaction (right-click context menu, command panel, hotkeys), multiplayer selection semantics (not relevant: game is single-player), direct order eligibility when category is Disabled, drafted-ant emergency behaviour, queuing, and pursuit/retreat semantics remain **unapproved details**.
- Future validation should show autonomous → ordered → autonomous transitions, forced-order safe interruption, drafted status persisting between move/attack actions, undraft restoring work, multiple drafted ant commands, and developer-only movement input not leaking into normal play.

## 1. Game at a glance

| Item | Definition |
| --- | --- |
| Genre | 2D side-view colony simulation / management |
| Starting scenario | The player begins as an ordinary ant. Becoming a queen is a later progression event whose requirements are still to be designed. |
| Primary interaction | Select ants, designate jobs, excavate foreground Solid Tiles and prepare exposed Background Materials. |
| Defining feature | Each ant is a persistent, selectable individual with its own needs, stats, skills and autonomous behaviour. |
| Target | Small single-player game; initial colony target of up to approximately 20 ants. |
| Status | Core movement and navigation rules agreed; balance, queen transformation and later progression details remain provisional. |

## 2. Experience pillars

- A colony built physically: tunnels and chambers exist because ants excavate and move real soil.
- Individual ants, not disposable units: skills improve through doing; health, hydration, food and energy influence productivity.
- Colony management with individual visibility: the player directs work globally or assigns a specific task, while ants navigate and act autonomously.
- Readable, satisfying simulation: worker behaviour is observable and the player understands why an ant is idle or struggling.
- Finishability first: implement and polish one complete play loop before adding broad simulation features.

## 3. Player fantasy and game loop

The player begins with a single ordinary ant and gradually develops a functioning colony. Becoming a queen is a possible later progression event whose requirements are still to be designed; it is not part of the initial gameplay. The player manages the ant and later colony through selection, inspection, designations, priorities and task assignments rather than direct movement control.

Core loop: Designate accessible work → an ant travels to a valid work position → excavates foreground material or prepares exposed background → moves required or produced resources → returns for another job. As the colony grows, workers also gather food and water, rest, and improve through successful work.

## 4. World tiles, movement and navigation

The game uses a 2D side-view grid divided into sparse 32×32 chunks. A generated, resident chunk contains one permanently present **Background Tile** at every 1×1 coordinate, whose material may be replaced, and may additionally contain one foreground **Solid Tile** at each coordinate. Chunks can be created at positive or negative coordinates, so the world can expand in all four directions without allocating one enormous rectangular array. Coordinates in chunks that have not been generated are unavailable; they are not silently treated as exposed terrain.

The authoritative chunk model remains independent of the current authored `DirtBlock` course and never imports that legacy terrain. Implementation and verification status belongs in `CurrentGameState.md`; the rules below define the approved target design.

### 4.1 Two-layer world model

#### Background Tile layer

- Every coordinate in a generated chunk contains exactly one valid Background Tile.
- The layer is permanently present: its material may be replaced, but the coordinate can never be left without a Background Tile.
- Background Tiles cannot be excavated or collected and remain beneath foreground Solid Tiles. Removing a Solid exposes the existing Background without automatically changing it.
- The Background Material is the primary authority for voluntary movement directions, intrinsic climbing grip and applicable gravity behaviour at its coordinate.
- Background Materials may also hold visual material, biome, depth, temperature, moisture, environmental modifiers and decorative information.
- Physical occupancy remains separate: Background permission or grip never allows an ant to pass through a blocking foreground Solid Tile.

#### Foreground Solid Tile layer

A coordinate contains zero or one foreground Solid Tile. Solid Tiles represent natural terrain, resource deposits, constructed solids, obstacles and structural elements. A Solid Material's `BlocksMovement` property controls physical occupancy. Its `SupportsNavigation` property remains independent and may describe applicable physical support; neither flag is inferred from the other, and not every Solid automatically provides climbing grip.

Possible Solid Tile materials include soil, sand, clay, stone, rock and later excavatable or non-excavatable materials. Material data may define hardness, durability, excavation requirements, whether it is excavatable, resource yield, movement blocking, navigation support and structural properties.

#### Standard terminology

| Term | Definition |
| --- | --- |
| **Background Tile** | The permanently present layer entry at every generated coordinate; its Background Material may be replaced. |
| **Solid Tile** | An optional foreground tile occupying the same coordinate as its Background Tile. |
| **Covered Space** | A coordinate containing a movement-blocking foreground Solid Tile. |
| **Exposed Space** | A coordinate whose Background Tile is not covered by a foreground Solid Tile. |
| **Traversable Space** | Exposed Space the ant can occupy and enter under applicable Background movement, grip, grounding, gravity, whole-body-clearance and obstacle rules. |
| **Unsupported Space** | Exposed Space with neither applicable physical ground support nor valid Background grip for controlled locomotion; it may still be physically clear or traversed directionally by falling. |
| **Reachable Space** | Traversable Space connected to the ant's current navigation region by a valid route. |
| **Prepared Background** | An exposed Background Material whose completed preparation grants configured grip and movement permissions. |
| **Qualifying Geometry** | Solid terrain or an explicitly configured object that provides applicable physical support; it is not the universal source of climbing permission. |

Exposed does not automatically mean physically clear, voluntarily traversable or reachable. Background existence alone grants no permission: its material properties determine movement and grip. A resident generated coordinate always has a Background Tile; an ungenerated chunk is unavailable rather than exposed.

### 4.2 Player control and hybrid task management

Normal play uses **Hybrid Task Management**:

- Players establish colony-level job priorities and designations.
- Players may assign a particular eligible task to an individual ant.
- Ants choose eligible work according to role, skills, availability and colony priorities, then navigate and perform it autonomously.
- Selecting an ant is for inspection and task management, not ordinary steering.

The player can select an ant and inspect its identity, attributes, skills, health, needs, assigned task and current activity. The player can also designate excavation and construction areas, mark resources for gathering, manage colony development and defence, and override global priorities with a valid individual assignment.

Developer-only manual movement remains available for testing and regression work. It is not an intended player-facing control mode and grants no movement or traffic priority.

### 4.3 Background-driven movement and gravity

- An ordinary ant has a nominal physical footprint of approximately one tile.
- Ants move smoothly and continuously through permitted space rather than hopping between tile centres.
- Movement may be horizontal, vertical or diagonal when the current Background Material permits it. Navigation may connect eight neighbouring tiles, but diagonal corner-cutting through solid or blocked geometry is prohibited.
- Background movement permissions, intrinsic grip, physical grounding and gravity are separate inputs. A blocking foreground Solid always prevents occupancy regardless of Background permissions.
- Solid terrain and configured solid objects remain authoritative collision boundaries.
- Clearance checks use the ant's whole footprint rather than only its centre point.
- The entire path segment, not only its endpoints or tile centres, must remain within navigable space and provide adequate body clearance.
- Future ant types may have larger footprints and may require wider passages.

The previous strict rule that every controlled position must be cardinally adjacent to a supporting Solid is superseded. Foreground support remains relevant for grounding and some movement, but prepared Background grip can permit controlled two-dimensional movement without immediate Solid adjacency. **Reachability** remains separate: a locally valid position may still be disconnected from the ant's current directed navigation region.

Initial Background behavior targets are:

| Background Material | Horizontal | Upward | Downward | Gravity |
| --- | --- | --- | --- | --- |
| Sky | Grounded walking only | No intrinsic climbing | Falling permitted | Applies when unsupported |
| Unprepared Earth | Grounded walking only | No intrinsic climbing | Falling permitted | Applies when unsupported |
| Unprepared Stone | Grounded walking only | No intrinsic climbing | Falling permitted | Applies when unsupported |
| Prepared Grippable Surface | Permitted | Permitted | Permitted | Suppressed while valid grip is maintained |

Horizontal movement on an unprepared Background requires an appropriate walkable surface. Falling downward is not voluntary downward climbing. Exact grounding, walking and gravity algorithms are deferred to movement-capability milestones.

### 4.4 Movement states

- **Grounded:** a suitable floor or structure physically supports the ant. It may walk where permitted, and gravity cannot move it through the floor.
- **Gripping:** the occupied Background Material supplies completed grip. Subject to clearance, the ant may move in the material's permitted directions and gravity is suppressed while grip remains valid.
- **Falling:** the ant has neither suitable ground support nor valid grip. Gravity controls vertical motion through physically clear space until it becomes grounded or regains valid grip.

Prepared grip permits left, right, up, down and valid diagonal movement within exposed, clearance-valid space. It never bypasses foreground collision. Navigation connections may be directional: an ant can fall down a smooth shaft without being able to climb back up, while progressively preparing the shaft can create a return route. Do not assume every connection is bidirectional.

### 4.5 Tunnels, chambers and supporting geometry

- Excavating Solid Tiles creates Exposed Spaces but does not automatically create grip. Newly exposed unprepared Background remains subject to its movement and gravity rules.
- Tile occupancy is not exclusive: multiple ants may occupy the same traversable tile when their continuous positions and collision constraints permit it.
- Unprepared chamber space may be walkable where grounded, may be entered by falling, and is not freely climbable merely because a Solid exists nearby.
- Completed prepared Background can create controlled climbing routes through otherwise non-grippable exposed space, subject to collision and clearance.
- A surface gap is not voluntarily crossable unless the applicable Background, grounding, grip and directional-connection rules permit it. The prototype's automatic step-over behavior is not an exception.

Objects have separate collision, navigation-support, interaction and pass-through properties. Not every collider or interactable provides navigation adjacency.

| Object type | Initial physical-support rule |
| --- | --- |
| Solid earth and rock | Qualifies |
| Structural walls and platforms | Qualifies |
| Ants | Do not qualify |
| Food items | Do not qualify initially |
| Water | Does not qualify |
| Large constructed obstacles | Explicitly configurable |

### 4.6 Multiple ants and hybrid separation

- Friendly ants attempt local avoidance when space permits.
- Temporary visual or physical overlap between friendly ants is acceptable when needed to prevent persistent deadlocks.
- Avoidance must never push an ant into blocking terrain or a movement state invalid under the applicable Background, grounding, grip and gravity rules.
- Crowding may reduce movement efficiency, and route selection may favour less congested alternatives.
- No ant may remain permanently blocked without bounded recovery or a reported navigation failure.
- Task urgency, carried loads, danger and fairness may influence yielding.
- Directly controlled ants receive no special priority because manual control is a developer-only tool.

Rigid ant-to-ant collision is not required to serve as the initial traffic-management solution. All autonomous ants use the same terrain movement rules regardless of colony membership.

### 4.7 Enemy colonies and hostile encounters

Colony relationships affect decisions, not basic terrain traversability. Friendly, neutral and hostile ants use the same tile-adjacent navigation model. On encountering a hostile ant, an ant may engage, retreat, avoid, defend or request assistance according to its aggression, role, assignment and colony orders. Combat-focused ants may deliberately defend chokepoints. Detailed combat behaviour is deferred.

### 4.8 Current prototype transition

The current `AntSurfaceMotor` provides manually tested surface movement, corner transitions and automatic step-over behaviour. It is a prototype baseline, not the final player movement design. Preserve it until tile-adjacent autonomous movement is implemented and validated. Preserve `PlayerMovementInput` for developer testing. Future work should replace or adapt these systems incrementally with regression coverage; existing contour following and automatic step-over do not override the approved navigation rules above.

## 5. Excavation and soil logistics

The player marks one or more excavatable Solid Tiles for removal using a dig-designation tool. Excavation workers select reachable designations beside Exposed Space; inaccessible Solid Tiles remain queued until prior digging creates a valid route.

Colony View presents a live, camera-aligned per-cell hover and bounded-drag preview without creating authoritative work until release. Overlay bounds use the authoritative convention that an integer world coordinate is the cell centre and extend half the shared cell size on every side; they do not use sprite padding or screen-pixel offsets. Pending and active designations retain a subtle full-cell border plus a compact status marker. Cancelled or completed designations have no persistent overlay. UI-owned pointer positions cancel a pending drag and cannot leak designation or movement commands into the world.

Cancellation releases the job reservation but does not remove the foreground Solid or reverse physically completed excavation. A cancelled designation on a still-eligible Solid may be designated again repeatedly under the same stable cell identity; active duplicates and completed jobs remain protected. Background Conversion follows the same cancellation/re-designation presentation contract, while staged construction material must remain physically conserved through recoverable loose output.

For the current resource model, extracting one resource-bearing foreground Solid Tile produces the node definition's quantity of Soil or Stone as a loose stack at the extracted cell. The extractor's inventory is not part of completion. A later explicit or autonomous Hauling action may reserve and deliver that stack to Construction staging or compatible Storage. Extraction must not complete if its physical loose output cannot be represented safely.

When excavation work completes:

1. Remove only the foreground Solid Tile.
2. Leave its existing Background Tile unchanged and visible.
3. Produce a transportable resource when the removed material defines one.
4. Update terrain collision.
5. Reevaluate clearance, movement capability and navigation data in the affected region.
6. Invalidate or recalculate affected ant routes when required.

Excavation never removes or destroys a Background Tile and never automatically makes it grippable. Newly exposed Earth, Stone or another unprepared Background remains non-grippable until a separate preparation job completes.

| Rule | MVP behaviour |
| --- | --- |
| Designation | Click or drag to mark soil Solid Tiles; visually distinguish pending, in-progress and complete. |
| Access | Workers can only dig Solid Tiles with a reachable exposed face. |
| Job ownership | A Solid Tile excavation job is reserved by at most one worker at a time. |
| Extraction | Work progresses over time and uses the Extraction skill. |
| Excavated cargo | One resource-bearing Solid Tile yields its defined quantity as a physical loose stack at the excavated cell; the excavator's inventory is unchanged. |
| Hauling | Ants transport carried tile-derived resources or objects to storage or to applicable work sites. |
| Deposit | A known surface storage point is acceptable initially; player-designated zones later. |
| Full inventory | Inventory capacity is irrelevant to extraction; a full ant can excavate without unloading because output remains loose. |
| Failure recovery | If path or dig location becomes invalid, release reservation and retry another valid order. |

### 5.1 Surface Preparation

**Surface Preparation** is an approved future colony job that replaces an exposed, unprepared Background Material with a completed grippable Background Material. It does not remove foreground terrain.

- Work begins only from a physically accessible nearby position after a player designation or colony work decision.
- Preparation consumes configured resources, such as stone embedded into the Background, and takes time to complete.
- Partial progress provides no grip. Movement permissions change only after successful completion replaces the authoritative Background Material.
- A prepared coordinate remains a Background Tile; the layer is replaced, never removed.
- Collision, movement capability and affected navigation may need updating after completion.
- Resource quantities, work duration, quality effects and exact work positions remain balancing and implementation decisions.

Vertical routes must be prepared progressively. A worker can stand at an accessible base, prepare the first reachable section, climb only after it completes, then work on the next section from a newly valid nearby position. The system must never require a worker to occupy an inaccessible target merely to prepare it.

Stone obtained by excavating a Stone Solid Tile may be carried by an ant and embedded into an exposed Background Tile during Surface Preparation. Such use transfers or consumes the Stone item as part of a valid, completed work transaction; interrupted work must retain or account for materials without silent loss. Resource reservation, delivery, quantities and work times are future implementation details, not yet implemented systems.

## 6. Ants: individuality, roles and needs

All ants are persistent individuals. A caste may define biological capabilities; attributes describe innate differences; learned skills change through repeated activity; and an assigned job describes the ant’s current responsibility. Every ant can attempt every ordinary colony task at Skill 0. Roles express preferences and work priorities rather than hard eligibility restrictions.

| Category | Initial concept |
| --- | --- |
| Individual identity | Unique ID/name, caste, status, assigned job. |
| Innate attributes | Strength, movement speed and endurance (final list to be tested). |
| Skills | Extraction, Surface Preparation, Construction, Hauling, Foraging, Combat and Brood Care. |
| Needs | Health, hunger/food, hydration/water and energy/rest. |
| Jobs | Excavator, forager and hauler initially; brood-care later. |
| Selection | Click an ant or use the roster to inspect it and manage its work. |

Autonomous priority: immediate danger → critical personal needs → assigned job → idle behaviour. Ants should automatically eat, drink and rest when supplies are available. Job interruption and resumption must be predictable. Skill may influence efficiency and job selection, but it does not prohibit an ordinary assignment; an unskilled nearby worker may reasonably be preferred over a highly skilled distant worker.

## 7. Queen transformation, brood and colony progression

The player starts with an ordinary ant rather than a queen. Becoming a queen may happen later through a progression event whose trigger, costs and presentation are open design questions. After transforming, the queen may establish the colony and later lay eggs. Brood matures through simplified stages until workers become selectable individuals. Once workers exist, the queen primarily focuses on reproduction and brood care; individual inspection and task assignment remain available.

The exact egg-production timings, mating biology, nutrition costs and brood-care systems are open design decisions, not implementation requirements for the first prototype.

## 8. Universal skills and progression

Every ant has every ordinary skill, represented from **0 to 100**, and begins at 0. Level 0 never prevents successful ordinary work: inexperience may make work slower, less resource-efficient, less productive or less consistent where appropriate, but it must not create a failure loop that prevents learning.

Skills improve through meaningful successful work rather than conventional level-point spending. Successful Soil/Stone/Seed/Twig resource removal improves Extraction; completed Surface Preparation improves Surface Preparation; delivered resources improve Hauling; gathered food improves Foraging; and applicable completed construction improves Construction. Starting and cancelling jobs must not award exploitable experience.

Gains may slow as a skill approaches 100. Higher skill may reduce completion time or waste, or improve productivity, effectiveness or consistency, but each skill uses only effects relevant to its task. Exact gain formulas, success criteria, difficulty modifiers and performance curves remain balancing decisions.

### 8.1 Personal inventory and Hauling capacity

Every ant begins with **one personal inventory slot**, and the number of slots is determined by its **Hauling skill (0–100)**. Gain one additional slot at Hauling skill thresholds **20, 40, 60 and 80**, to a maximum of **five slots**. Skill 100 does not award a sixth slot.

| Hauling skill | Available slots |
| --- | ---: |
| 0–19 | 1 |
| 20–39 | 2 |
| 40–59 | 3 |
| 60–79 | 4 |
| 80–100 | 5 |

- Each slot holds exactly one tile-derived resource or transportable object; **items do not stack** in the initial model.
- Extracted items remain loose until a separate Hauling interaction transfers them into an inventory and then to explicit staging or Storage.
- Resource transport, storage, placement and consumption must conserve items. Background preparation may use carried or delivered Stone to create a completed grippable background.
- If Hauling skill decays across a capacity threshold, carried items are **never destroyed**. Existing cargo is temporarily retained even if occupied slots exceed the new effective capacity; the ant cannot collect additional items until occupancy falls below its current capacity.
- The one-item-per-slot rule also applies to transportable objects in the first playable colony. Larger-object capacity rules are deliberately deferred.
- Hauling slots, cargo transfer, stockpiles and construction consumption are **approved design**, not implemented systems.

### 8.2 Inactivity decay above 50

- Each skill tracks successful use independently for each ant.
- Skills at or below 50 never decay through inactivity.
- Skills above 50 decay slowly while unused and stop at exactly 50.
- Successful relevant work can raise a decayed skill again.
- Decay never produces a negative value or reduces a skill below 50.
- A configurable inactivity grace period should prevent ordinary rest, eating or short task changes from causing decay.

The exact grace period and decay rate are not yet defined. For example, an ant with Extraction 85, Surface Preparation 72, Hauling 43 and Foraging 18 will gradually retain 50 in the two unused high skills, while Hauling and Foraging remain unchanged. Hauling decay can reduce future carrying capacity, but must never delete cargo already held.

## 9. Food, water and productivity

Food and water are survival and logistics resources. Hunger and dehydration reduce productivity before eventually harming health. Energy falls during work and recovers during rest. Avoid fast-draining meters that turn play into micromanagement.

Initial implementation principle: ant needs may be displayed early, but full food/water production loops follow the proven movement-and-excavation prototype.

## 10. UI and controls

| Interaction | Proposed behaviour |
| --- | --- |
| Select ant | Click an ant or select from colony roster. |
| Mark excavation | Activate dig tool; click/drag across excavatable Solid Tiles. |
| Assign work | Set colony priorities or assign a specific eligible task to the selected ant. |
| Direct movement | Not part of normal play; developer-only manual input is retained for testing. |
| Inspect | View attributes, skills, health, needs, assigned task and current activity. |
| Camera | Pan/zoom and centre on selected ant. |

Normal gameplay does not manually steer ants. Destination requests arise from autonomous needs and jobs, global designations and priorities, or a specific task assigned to an individual.

## 11. Small-game scope and win condition

The initial full playable game should offer a single level with a small colony and an achievable victory. A proposed win condition is: maintain a colony of 20 ants, excavate at least three chambers, and accumulate 100 units of food. These targets are provisional and should be adjusted after playtesting.

Lose/pressure conditions are deliberately light in v1: a failed colony may occur if the queen dies or all ants perish, but balance should emphasise learning and rebuilding rather than frequent failure.

## 12. Roadmap and acceptance gates

| Stage | Deliverable / test |
| --- | --- |
| Milestone 1 — Movement prototype baseline | Preserve and finish validating the current manually driven surface-movement prototype as regression evidence; it is not the final player control design. |
| Milestone 2 — World tiles and autonomous navigation | Build on the sparse 32×32 two-layer chunk foundation, validate physical clearance separately from Background movement capability, create directional connections, then move one ant autonomously. |
| Selection and task management | Inspect individual ants and support both colony-wide priorities and individual task assignments. |
| Multi-ant movement | Add hybrid separation, congestion-aware routing and bounded recovery. |
| Autonomous work | Perform excavation, Surface Preparation, resource transport, gathering and needs-driven behaviour. |
| Population and conflict | Scale toward hundreds of simulated ants, then support enemy-colony movement and high-level hostile responses. |
| Finished slice | Add onboarding, a clear objective, win/lose feedback, save/load and a usability pass. |

## 13. Out of scope until after the first finished game

- Multiplayer and Unity Netcode for GameObjects.
- Procedurally generated worlds or very large maps.
- Multiple ant species, elaborate caste systems, genetics and detailed pheromone modelling.
- Complex predators, combat campaigns or weather and seasons.
- Physical dirt barriers, sealing entrances and placing soil back into the terrain.
- Advanced storage logistics, colony diplomacy or a large technology tree.

## 14. Engineering principles

- Unity 6 + C#; single-player first. Represent the permanently present, replaceable Background Material separately from optional foreground Solid Tiles.
- Use one navigation model for all autonomous ants, with authoritative collision, whole-body clearance, Background movement capability, grounding, grip and gravity evaluated as separate concerns.
- Keep world state (tiles, designated jobs, carried loads, ant stats) independent of visual presentation.
- Keep local traversability distinct from connectivity and reachability.
- Update navigation locally after terrain edits where practical; profile only once the core loop works.
- Track job claims to avoid ants competing for one tile. Plan explicit cancellation and recovery paths.
- Keep task selection, route requests, shared tile navigation data, movement execution, local avoidance and physics conceptually separate, but do not require a heavyweight framework or specific class name for each responsibility.
- Prefer event-driven navigation invalidation and bounded replanning over continuous full-route calculation.
- Design shared navigation data, nearby-ant queries, controlled update frequencies and optional pathfinding batches for potentially hundreds of independently simulated ants.
- Do not introduce DOTS/ECS or another substantial architecture dependency unless profiling demonstrates a need.
- Keep components small and comments educational. Avoid premature generic frameworks.

## 15. Autonomous navigation and traffic design

Autonomous movement is approved but not implemented. It must support excavation work, soil delivery, food and water access, rest, chamber travel, construction and hostile encounters.

The design separates these conceptual responsibilities. They describe boundaries, not mandatory class names:

- **World Tile Data:** stores resident 32×32 chunks containing one permanently present but material-replaceable Background Tile per coordinate, plus an optional foreground Solid Tile and their configured properties. Missing chunks remain explicitly unavailable.
- **Extraction System** (currently implemented by compatibility-named `Excavation*` classes): removes completed extractable Solid/Resource Nodes, produces configured resources, updates collision and announces affected regions.
- **Surface Preparation System:** reserves accessible work, consumes configured resources over time, and replaces an exposed Background Material only when preparation completes.
- **Colony Task System:** decides what work exists and which ants may perform it, using designations, priorities, roles, skills, availability and explicit individual assignments.
- **Ant Navigation Agent:** selects destinations, requests paths, follows routes and reports invalid or unreachable results.
- **Physical Clearance:** determines whether the full body fits in available, non-blocking space without deciding grip, grounding or reachability.
- **Movement Capability:** derives voluntary directions, prepared grip, grounding and gravity eligibility from Background and applicable support data.
- **Shared Navigation Map:** builds directional connections and Reachable Space from clearance, movement capability and relevant obstacles.
- **Ant Movement Controller:** moves one ant continuously toward route waypoints while keeping the whole body inside valid space.
- **Local Avoidance:** handles nearby ants, congestion, yielding and hybrid separation without creating invalid movement.
- **Physics/Collision:** prevents penetration of solid terrain and relevant objects.

These responsibilities may begin as small cooperating components or data structures. They do not require global managers, generic frameworks or DOTS/ECS.

### 15.1 Traffic and deadlock rules

- Friendly ants use local avoidance where space permits, but temporary overlap is acceptable to break deadlocks.
- Avoidance and yielding never move ants through terrain or into a movement state invalid under the applicable Background, grounding, grip and gravity rules.
- Waiting is valid temporarily, but excessive waiting must trigger bounded recovery such as reversing to a wider area, choosing an alternate route or replanning.
- Repeated collision attempts are not the primary navigation strategy.
- Replanning requests must be rate-limited or otherwise bounded to avoid uncontrolled repeated pathfinding.
- Recovery rules must prevent endless cycles where two ants repeatedly yield to one another.
- Task urgency, carrying state, danger and fairness may contribute to deterministic yielding decisions.

### 15.2 Dynamic terrain and preparation updates

Removing a foreground Solid Tile exposes its unchanged Background Tile. This can open physical space without granting controlled grip. Completing Surface Preparation replaces the authoritative Background Material and can add new voluntary directions or suppress gravity while gripping. Collision, clearance, movement capability and directional connections must update for the affected region, and ants whose routes are affected must reconsider them safely. Runtime navigation therefore cannot rely on a once-baked static map.

### 15.3 Scalability expectations

- Avoid unnecessary per-frame physics queries for every ant.
- Reuse shared navigation information rather than rebuilding equivalent route data per ant or scanning the full map for every request.
- Use an efficient nearby-ant lookup for local avoidance.
- Keep high-level task decisions separate from low-level motion.
- Use efficient path requests, spatial partitioning for nearby-ant queries, and reduced AI update frequencies where appropriate.
- Control update frequencies and spread or batch expensive work across simulation ticks where appropriate.
- Keep traffic and recovery decisions deterministic enough to reproduce and debug.
- Do not add optimisation systems before profiling demonstrates a need.

## 16. Open design questions

- Which concrete pathfinding representation best supports directional walking, gripping and falling connections with incremental updates at the target scale?
- How should the approved Background Tile and Solid Tile layers map onto the project's eventual Unity Tilemaps or other runtime data without duplicating authoritative state?
- What production clearance margin and swept-body test are accurate enough for corners and non-square geometry without becoming too costly?
- What Background Material data best represents allowed directions, grip strength and gravity behavior without hardcoding individual materials?
- What Surface Preparation resource quantities, work duration, quality effects and nearby work-position rules produce useful progression?
- What stockpile, transfer and full-inventory recovery behaviour should be prioritised in the first playable resource loop?
- What skill gain curve, successful-work definition, inactivity grace period and decay rate preserve specialisation without excessive maintenance?
- Which congestion cost and yielding policy best balances task urgency, carrying status, danger and fairness?
- Which qualifying constructed objects should provide navigation support in the first playable slice?
- What failure feedback should be shown to the player when a task destination is invalid or unreachable?
- What event, requirements and costs allow the starting ordinary ant to become a queen?
- After transformation, does the queen dig and haul before workers hatch, and how is her energy replenished?
- How is brood nurtured, and what inputs are needed to hatch the first worker?
- Are ants individually named by the player or automatically numbered?
- Should stockpiled soil ever have mechanical value before placement is introduced?
- What are the minimum food/water supply systems needed for a satisfying completed game?

## 17. Scope-control rule

An idea is not added to the current milestone unless it directly enables its acceptance test or fixes a blocker. New ideas go into the backlog, and the project remains single-player until a finished small game exists.

Immediate implementation sequence: preserve remaining Milestone 1 regression acceptance, finish reconciling Milestone 2.2B physical clearance with its support-independent definition, then implement 2.2C Background movement capability and gravity eligibility before directional connections or autonomous movement. Excavation jobs, Surface Preparation logistics, skill systems, queen transformation, reproduction and needs simulation remain later work.
