# ANT COLONY

**Game Design Document | Version 3.4 — resource logistics, 12 October 2026**

Working title • Unity 6 • Single-player • 2D side-view • PC-first prototype

Design intent: Build a small, finishable colony simulation in which individual ants matter. The player directs excavation and assigns work; ants physically perform tasks and develop through experience.

## Authoritative design realignment — 12 October 2026

> **Status: APPROVED DESIGN, NOT IMPLEMENTATION CLAIM.** This section supersedes contradictory design assumptions elsewhere in this document, the roadmap and AGENTS.md. Earlier milestone and testing narratives are retained as historical records. No gameplay code has been changed as part of this design update. Numerical balance and unresolved details remain provisional.

### Vision and control

- Ant Game is a single-player, scrollable/zoomable 2D side-view colony simulation in a procedural, persistent, chunk-loaded destructible world. Players can manage colony-wide assignments **or directly select and control any adult ant**, including the founding queen; other ants remain autonomous.
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

- **Functional Background tiles define colony uses and capacity**, including Nursery, Sleeping, Food Storage, Water Storage, Production and Burial. A functional tile generally grants one matching capacity slot (**1:1**). Food/water stockpile slots may hold a physical item/stack; exact stack limits **TBD**.
- Player designates conversion of an accessible exposed Background tile; an ant **physically performs a Construction skill job using required resources**. Converting a functional tile later requires a new job and accounted material consumption. Construction improves through doing.
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

### Implementation guardrail

Before another gameplay milestone, perform a **read-only architecture/code-to-design audit** comparing the present Unity 6 project with this new baseline. Record each affected system as Keep / Refactor / Replace / Defer, identify potentially invalid tests and migration steps, and propose incremental milestones. Preserve the manually validated 2.3B movement baseline as historical evidence; it does **not** verify the new natural-background movement rule. Do not refactor, delete scripts, modify assets or implement this design without approval.

---

## Approved construction and physical-resource logistics — 12 October 2026

> **Status: approved design only.** This section augments the authoritative design realignment above and supersedes incompatible older MVP logistics assumptions. No Unity code, assets, tests or prefabs are implemented by this documentation update. Follow the pragmatic, explicit, physical-job-and-stockpile style of *RimWorld*, without presuming its exact numerical balancing or architecture.

### Physical materials and inventories

- Excavation produces physical resources. On successful excavation, the excavating ant **automatically picks up** the resulting item or amount **if its available inventory capacity permits**; otherwise the resource **drops physically at a valid recoverable world position**. Neither action can silently destroy or duplicate resources. Placement when the work cell becomes blocked or full must have deterministic safe fallback rules (**TBD**).
- One inventory slot may hold a **stack of identical compatible items**. Stack maximum per material is **TBD**. Early carrying limits use **slot count only**, not a weight or mass simulation. The existing concept of Hauling skill increasing carrying slots remains a balancing/design constraint to be reconciled with stacks and later saddlebags.
- Resources retain physical locations and quantities in world drops, ant inventory, storage tiles and delivered construction-site materials. The colony may display aggregated totals, but totals are **derived views**, not teleportable resources.
- Add **Material Storage Background** as a functional tile. As with other functional storage, one tile grants **one physical storage slot** (a compatible stack). Optional player-configured material filters can restrict accepted resource types. Food and Water Storage remain distinct shared stockpile types. Stored items are physical and require adjacency to deposit/withdraw.
- If an occupied storage tile is removed or changed, contents become loose, recoverable physical items, consistent with prior design decisions.

### Resource-backed construction and conversion

- Player designation creates a resource-requiring task for doors or functional-background conversions. All such jobs require an accessible adjacent interaction position and use **Construction skill** for work and progression.
- **The constructing ant must gather and deliver its own required materials.** No separate hauler can satisfy a construction job's material-delivery obligation as its primary workflow; generic hauling to stockpiles is still permitted. The assigned builder obtains usable resources from reachable shared Material Storage, other permitted storage, nearby recoverable drops or its own carried inventory, transports them to the worksite, and then personally performs the construction.
- For multi-trip jobs, the **same builder** may make multiple supply trips. Once materials are on site, another builder taking over an interrupted/released job may reuse those already-delivered physical materials; material provenance and reservation must stay valid. This is a proposed implementation interpretation of single-builder ownership, **not authorisation for a separate job-specific hauling workforce**.
- Construction jobs must explicitly represent at least: designation, awaiting materials, gathering/transport, worksite delivery, constructing, completion, pause/interruption, cancellation and inaccessible/invalid states. Exact state names and internal object structure are implementation choices.
- Reserve **physical quantities** rather than abstract colony totals so two jobs cannot consume the same item. A reservation is not consumption. Materials delivered to a construction site are **consumed only in an accounted transaction** as construction successfully progresses/completes according to later-approved accounting rules.
- **Cancelling a construction job leaves all unused materials physical and recoverable** in ant inventory, at the worksite, or as loose world items; do not teleport them to former stockpiles. Job cancellation must release any unconsumed resource reservations. Already irreversibly consumed resources are not automatically refunded; stage of consumption is **TBD**.
- Recover safely from loss of builder, interrupted work, death, inaccessible stores, unloaded chunks, invalidated terrain, full carrying slots and world changes. On ant death, carried items should remain physical and recoverable; exact drop placement policy is **TBD**. A construction job must never silently swallow cargo or duplicate materials.

### First-colony bootstrap and unresolved balancing

- The founding queen must be able to excavate, collect, transport and convert at least three Nursery Background tiles from initially reachable resources without relying on existing workers or prior stockpiles. The queen may build directly from carried materials or recoverable nearby drops. Initial nursery is functional before enclosure and doors.
- **Unresolved (do not hardcode as approved):** construction recipes and costs, exact materials available at start, stack-size limits, carrying-slot progression and any equipment modifiers, conversion durations, reservation expiry, placement when drop location is invalid, point of material consumption, whether partial construction persists, stockpile filters UI, logistics fallback when missing materials, and performance update cadence.
- Testing when implementation is separately authorised should cover resource conservation (excavate/pick up/drop/store/withdraw/consume), stacking, one-slot capacity, builder self-supply over multiple trips, reservation contention, cancellation at multiple stages, builder death, tile removal, persistent save/load and nursery bootstrap without stockpiles.

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

For the first playable resource model, excavating one resource-bearing foreground Solid Tile produces one transportable item representing that material (for example Soil or Stone). The excavating ant places the item into its personal inventory rather than destroying or automatically depositing it. Each item occupies one slot and does not stack. An excavation that would produce an item must not complete unless the resulting item can be safely accommodated; cargo must never silently disappear. The ant can subsequently deliver the item to storage, place it, or supply it to a construction or Surface Preparation job. An initial known deposit point may be used as a simple storage destination; player-designated storage zones are future work.

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
| Excavation | Work progresses over time, potentially modified by excavation skill later. |
| Excavated cargo | One resource-bearing Solid Tile yields one item, added to the excavating ant's inventory after a valid completion; no stacking. |
| Hauling | Ants transport carried tile-derived resources or objects to storage or to applicable work sites. |
| Deposit | A known surface storage point is acceptable initially; player-designated zones later. |
| Full inventory | Do not complete an item-producing excavation without a free slot or a separately approved safe transfer; redirect to unloading first. |
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
| Skills | Excavation, Surface Preparation, Construction, Hauling, Foraging, Combat and Brood Care. |
| Needs | Health, hunger/food, hydration/water and energy/rest. |
| Jobs | Excavator, forager and hauler initially; brood-care later. |
| Selection | Click an ant or use the roster to inspect it and manage its work. |

Autonomous priority: immediate danger → critical personal needs → assigned job → idle behaviour. Ants should automatically eat, drink and rest when supplies are available. Job interruption and resumption must be predictable. Skill may influence efficiency and job selection, but it does not prohibit an ordinary assignment; an unskilled nearby worker may reasonably be preferred over a highly skilled distant worker.

## 7. Queen transformation, brood and colony progression

The player starts with an ordinary ant rather than a queen. Becoming a queen may happen later through a progression event whose trigger, costs and presentation are open design questions. After transforming, the queen may establish the colony and later lay eggs. Brood matures through simplified stages until workers become selectable individuals. Once workers exist, the queen primarily focuses on reproduction and brood care; individual inspection and task assignment remain available.

The exact egg-production timings, mating biology, nutrition costs and brood-care systems are open design decisions, not implementation requirements for the first prototype.

## 8. Universal skills and progression

Every ant has every ordinary skill, represented from **0 to 100**, and begins at 0. Level 0 never prevents successful ordinary work: inexperience may make work slower, less resource-efficient, less productive or less consistent where appropriate, but it must not create a failure loop that prevents learning.

Skills improve through meaningful successful work rather than conventional level-point spending. Successful excavation improves Excavation; completed Surface Preparation improves Surface Preparation; delivered resources improve Hauling; gathered food improves Foraging; and applicable completed construction improves Construction. Starting and cancelling jobs must not award exploitable experience.

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
- Excavated items remain in the excavating ant's inventory until stored, placed, delivered for use or otherwise transferred by an approved interaction.
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

The exact grace period and decay rate are not yet defined. For example, an ant with Excavation 85, Surface Preparation 72, Hauling 43 and Foraging 18 will gradually retain 50 in the two unused high skills, while Hauling and Foraging remain unchanged. Hauling decay can reduce future carrying capacity, but must never delete cargo already held.

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
- **Terrain/Excavation System:** removes completed excavatable Solid Tiles, produces configured resources, updates collision and announces affected regions.
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
