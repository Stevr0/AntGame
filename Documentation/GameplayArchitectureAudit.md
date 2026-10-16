# Gameplay Architecture Audit

**Audit date:** 15 October 2026

**Repository revision:** `b280744c15a708e1afedcb64faea6d882d000d4f`

**Scope:** read-only gameplay/code/test audit plus this documentation report and
wiki navigation. No runtime code, Unity asset, scene, prefab, serialized data,
test, balance or milestone status was changed.

## Post-audit disposition — 15 October 2026

**16 October 2026 amendment:**
[`ResourceAndNodeSystem.md`](ResourceAndNodeSystem.md) now resolves the target
resource model: every Solid is a finite variable-quantity Node; normal Solid
Extraction removes the whole Node and commits all remaining quantity into
recoverable physical output; Water is finite; and natural Backgrounds have no
colony function. WV-B3C.1 is complete after 17/17 manual acceptance, but its
registry is inactive and quantity-one. The original audit evidence remains a
historical snapshot; the gaps below are reclassified, not rewritten as runtime
implementation.

WV-B3A now supersedes the audit's recommended mutable-cell design work with the
authoritative `UnifiedRuntimeCellSpecification.md`. It resolves the design
questions of runtime ownership, snapshot versus projection, transaction
boundaries, publication order and migration slicing; it does not resolve them
in code. The audit's findings about quantity-less Colony Solids, Surface source
projections, functional Backgrounds and same-cell loose records therefore remain
current implementation gaps until WV-B3B/B3C/B3D are separately approved and
accepted. The scheduler fairness follow-up is now manually accepted.

The audit below remains the evidence snapshot for revision `b280744c`, but two
recommended actions have now been completed:

- The user manually accepted the seed-`2203` generated founding-colony workflow.
  `WV-B2B`, `WV-B2B-DEF-001/002/003` and `WV-B2B-ARCH-001` are closed. The queen's
  later Day 8 death at Health 0, Hunger 0, Thirst 0 and Fatigue 100 is the known
  pre-WV-C1 survival limitation, not a proven hunger-only WV-B2B defect.
- The first-32 selector finding is resolved by the existing selector's resumable
  deterministic per-ant pass. Each evaluation examines at most 32 registry
  entries; a choice is made after the complete pass, so higher-priority eligible
  work and closest reachable path cost remain authoritative. Appends join the
  current pass; removals and priority revisions restart safely; status changes
  are revalidated.
- A current XML-backed rerun passed **551/551 complete graphics-enabled Edit
  Mode** and **74/74 complete Play Mode**, closing `WV-B1-REG-001`. Focused
  scheduler coverage passed **40/40 Edit** and **1/1 Play**, including job 33,
  mutation/repriority, exclusive multi-ant claims, pause/3× behavior and bounded
  32/64/256/1,024-entry scans.

The scheduler correction is **Complete — Manually Accepted** against the
14-step checklist in `CurrentGameState.md`; that follow-up did not reopen
WV-B2B. Generated Surface source interaction, natural rest, typed cross-region
job targets, persistence and the remaining architectural findings stay deferred.
Statements below that call these now-resolved items open describe the original
audit state and are retained intentionally for traceability.

## 1. Executive summary

Ant Game has a sound, maintainable **Colony-side production foundation**, but it
is still a deliberately transitional architecture rather than the complete
five-stage game. The strongest current seam is the physical resource chain:

`Colony Solid → loose Soil/Stone → ant inventory → Construction staging or Material Storage → Construction consumption`

The 15 October `WV-B2B-ARCH-001` correction makes Extraction, Hauling and
Construction genuinely separate. Extraction preflights loose output before it
removes terrain; Hauling reserves a source and an explicit destination;
Construction starts only when site-owned staging is complete. Those boundaries
are small, testable and suitable for extension.

The largest architectural gap is that the approved unified cell model currently
exists only in immutable/generated records. `GeneratedCellRecord` projects
Colony nodes into quantity-less `ChunkedWorldData` Solids and projects Surface
nodes only into blocking navigation plus temporary presentation. Runtime
placements, loose drops, storage and construction remain separate Colony-only
authorities addressed by untyped `Vector2Int`. This is acceptable for WV-B2B,
but it is not yet a complete shared runtime for WV-C1, Foraging or Refining.

The founding queen can, subject to the still-open manual acceptance, perform the
implemented Colony bootstrap alone: extract Soil, haul it directly into an
unsupplied Material Storage site, construct the first Storage and then haul into
it. She cannot yet perform the intended full founding-colony loop because
Surface discovery, Surface extraction, natural-source survival, autonomous
cross-region routing, brood and Refining are not implemented.

### Highest-risk findings

| Priority | Finding | Consequence |
| --- | --- | --- |
| P0 | WV-B2B manual seed-`2203` acceptance and the WV-B1 graphics/manual evidence remain open. | The new-game bootstrap and view integration are not accepted even though automated results are recorded. |
| P1 | `ColonyJobSelectionService` stops collecting after the first 32 claimable jobs at a priority before reachability is evaluated (`ColonyJobs.cs:296-305`). | Reachable work can starve behind 32 earlier inaccessible jobs; dictionary insertion order becomes a gameplay input. |
| P1 | Generated nodes are not the mutable runtime node authority; `RemainingQuantity` has no mutation path and Colony projection discards node identity/quantity (`ProceduralWorldGeneration.cs:31-56,137-159`). | WV-C1 cannot safely add shared Surface/Colony extraction by attaching behavior to the generation snapshot. |
| P1 | Jobs, drops, storage and construction use Colony-only `Vector2Int` locations. | Cross-region autonomous work would invite coordinate collisions or a second scheduler if added without typed destinations. |
| P1 | Reservations have no timeout, persistence or load-recovery contract. | Death, save/load and destroyed executors can leave ownership stranded unless every lifecycle path explicitly releases it. |
| P1 | Hauling destinations are a concrete two-value enum with destination-specific branches. | Refining inputs/outputs would multiply branching unless a narrow destination contract is introduced first. |
| P2 | Foraging is metadata/development-synthetic only; there is no visibility, knowledge, sensing or pheromone authority. | The intended discovery loop cannot yet gate Extraction or recruitment. |
| P2 | Several wiki sections still describe superseded Excavation terminology or builder self-supply. | Contributors can implement against historical behavior despite the current GDD correction. |

No evidence justifies a wholesale terrain rewrite. The recommended approach is
to preserve the current authorities and introduce typed, shared contracts at
their boundaries in small migrations.

## 2. Authority and audit method

Design intent was taken from `Documentation/GameDesignDocument.md`, milestone
order from `Documentation/DevelopmentRoadmap.md`, reported implementation and
acceptance from `Documentation/CurrentGameState.md`, and runtime truth from the
current C# source, tests, `.meta` GUIDs, scenes and prefabs. The technical and
procedural specifications plus the `docs/` wiki were checked for compatibility
and stale statements.

Status terms in this report mean:

- **Implemented:** executable source exists at the audited revision.
- **Automated evidence recorded:** project documentation records an executed
  passing result. This audit did not rerun the expensive Unity suites.
- **Accepted:** the required direct Unity/manual checks were confirmed by the
  user. WV-B2B does not meet this bar.
- **Planned:** approved design without current runtime behavior.

## 3. Five-stage gameplay vision

| Stage | Approved responsibility | Current state | Assessment |
| --- | --- | --- | --- |
| Foraging | Discover sources and world knowledge; improve visibility and exploration endurance; establish Surface recruitment trails. | Skill/category metadata and development-only synthetic jobs exist. No discovery, visibility, sensing, persistent knowledge or pheromones. | **Not implemented; foundation-compatible.** Keep discovery separate from node mutation. |
| Extraction | Work one finite node, reduce it exactly once and create loose physical output while retaining Background. | Colony Soil/Stone Solids use compatibility-named `Excavation*` APIs. Exact-once loose output and Background preservation are implemented. Surface generated nodes are not interactable. | **Compliant for Colony Soil/Stone; partial overall.** |
| Hauling | Reserve, pick up and deliver physical resources to Construction, Storage and future explicit destinations. | Separate job, source/destination reservation, partial load, direct first-Storage staging, safe carried-state recovery and 5 XP/unit are implemented for Soil/Stone. | **Substantially compliant.** Destination abstraction and cross-region addressing are missing. |
| Refining | Bills transform staged ingredients into recoverable products; Hauling supplies and removes products. | No recipes, bills, workstation, production executor or output transaction. `Production` is a work category without a skill. | **Not implemented; reusable seams exist.** |
| Construction | Designate placement, receive all materials from Hauling, perform builder-only work, consume staging once, recover on cancellation. | Work-only construction and multi-type staging are implemented. The result still replaces a functional Background rather than creating a placement over natural Background. | **Workflow compliant; cell representation is a migration adapter.** |

## 4. Current skills and jobs register

### 4.1 Shared progression formula

All seven skills use `AntSkillProgressionSettings.Provisional`
(`AntSkills.cs:79-216`):

- Level cap: `100`.
- XP required from level `L` to `L+1`: `100 + 5L`.
- Total XP at level `L`: `100L + 2.5L(L-1)`.
- Exact level interpolates linearly within the current threshold.
- Inactivity grace: 24 simulation hours.
- Above level 50, decay is 0.25 exact levels per inactive hour and stops at 50.
- A completed Rested state multiplies work speed by up to `1.15x` and XP by up
  to `1.25x`; both fade to zero as Fatigue rises from 20 to 80
  (`AntNeeds.cs:150-177`). Critical dehydration blocks ordinary XP through
  `AntRecord.TryAwardSkillExperience`.

### 4.2 Skill inventory

| Stable ID | Display name | Implemented | Real associated jobs/actions | XP source and quantity | Actual level benefit | Quality / unlocks / needs | Owner |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `extraction` | Extraction | **Yes, Colony partial** | Real Colony Soil/Stone Extraction; development synthetic Extraction | `20 × applied job progress`; a full block yields 20 base XP | Work speed `1 + 0.005 × level`, then Rested multiplier | No quality, yield, hardness or eligibility benefit. Dehydration blocks XP. | `AntSkills`, `ExcavationJobOrder`, `ExcavationWorldTransaction` |
| `surface_preparation` | Surface Preparation | **Metadata only** | Development synthetic job only; Prepared conversion is performed as Construction | Synthetic: `20 × applied progress` | Synthetic executor uses `1 + 0.005 × level`; no production benefit | No quality, unlock or special needs effect | `AntSkills`, `SyntheticColonyJobOrder` |
| `construction` | Construction | **Yes** | Fully staged Background-conversion Construction | `20 × applied job progress`; a full job yields 20 base XP | Work speed `1 + 0.005 × level`, then Rested multiplier | No quality, botch chance, advanced placement or eligibility benefit | `AntSkills`, `ConstructionJobOrder` |
| `hauling` | Hauling | **Yes** | Replay-safe partial loose-stack/personal-inventory delivery to Construction Site or Material Storage | `5 × committed delivered units`; no XP for travel, reserve, pickup or cancellation | Capacity bands: 2×2 at 0–19; 3×2 at 20–39; 3×3 at 40–59; 4×4 at 60–74; 5×5 at 75–89; 6×6 at 90–100 | No movement-speed benefit. Resource-specific slot limits still cap the band allowance; dehydration blocks XP. | `AntSkills`, `AntInventory`, `MaterialTransferRegistry`, `MaterialHaulingOrder` |
| `foraging` | Foraging | **No gameplay** | Development synthetic jobs only | Synthetic: `20 × applied progress` | Synthetic speed only; no visibility or fatigue benefit | No discovery, sensing, unlock or needs modifier | `AntSkills`, `SyntheticColonyJobOrder` |
| `combat` | Combat | **No gameplay** | Development synthetic jobs only | Synthetic: `20 × applied progress` | Synthetic speed only | No attack, accuracy, damage or unlock benefit | `AntSkills`, `SyntheticColonyJobOrder` |
| `brood_care` | Brood Care | **No gameplay** | Development synthetic jobs only | Synthetic: `20 × applied progress` | Synthetic speed only | No brood quality, rate or eligibility benefit | `AntSkills`, `SyntheticColonyJobOrder` |

`AntSkillId.Excavation` is an alias of numeric slot zero, not an eighth skill
(`AntSkills.cs:10-21`). This correctly preserves data and prevents duplicate XP.

### 4.3 Work-category inventory

The work matrix has eight stable categories: Extraction, Surface Preparation,
Construction, Hauling, Foraging, Production, Combat and Brood Care
(`AntWorkPriorities.cs:7-68`). `Production` deliberately has no mapped skill.
Priorities are Disabled or 1–4; lower numeric priority wins.

Only these categories have production executors:

| Category | Real executor | Player-facing matrix | Notes |
| --- | --- | --- | --- |
| Extraction | `ExcavationJobOrder` | Yes | Compatibility class names remain. |
| Construction | `ConstructionJobOrder` | Yes | Eligible only when staging is complete. |
| Hauling | `MaterialHaulingOrder` | Yes | Dynamic jobs target sites before Storage. |

The other categories can be inserted as `ColonyJob` records and completed by
`SyntheticColonyJobOrder`, but that class is explicitly development-only and
does not mutate gameplay. They must not be reported as implemented jobs.

Survival Eat, Drink and Rest are real autonomous interactions, but they are a
separate emergency/needs decision path rather than work categories. Entrance
travel and player move orders are navigation actions, not skills.

### 4.4 Future progression recommendations (proposals, not balance changes)

| Skill | Small future rule to test | Reason |
| --- | --- | --- |
| Foraging | +0.25 cell sensing/visibility radius per 10 levels; 2% less exploration fatigue per 10 levels, capped at 20%. | Noticeable without making a high-level ant reveal whole chunks or ignore Rest. Physical line of sight and underground sensing must remain separate. |
| Extraction | Retain the implemented +0.5% work speed/level; add definition-owned hardness tiers at provisional levels 0/25/50/75. | Keeps current balance stable while allowing nodes to express eligibility. Skill must not increase output unless explicitly designed. |
| Hauling | Keep the approved six capacity bands initially; do not add movement speed until route economics are profiled. | Carry capacity is already meaningful. A separate Hauling skill is defensible, but it could become only a work category if equipment later owns all capacity progression. |
| Refining | Use +0.5% work speed/level as the first consistency baseline; gate advanced recipes at definition-owned thresholds. | Matches existing work skills without inventing quality yet. |
| Construction | Retain +0.5% work speed/level; use placement-definition eligibility thresholds before adding quality or botch loss. | Avoids destructive resource waste while foundations are still being validated. |

## 5. RimWorld workflow comparison

The comparison is to vanilla RimWorld as a workflow reference, not an
implementation dependency. Current reference pages confirm that RimWorld uses
manual 1–4/disabled work priorities, learn-by-doing skills, direct prioritized
orders, blueprints/frames, stockpile filters and production bills. They also
confirm an intentional difference: a RimWorld builder may deliver materials as
part of Construction even when ordinary Hauling is disabled. Ant Game instead
requires an independent Hauling job for every normal material delivery.

References: [Work](https://rimworldwiki.com/wiki/Work),
[Skills](https://rimworldwiki.com/wiki/Skills),
[Orders](https://rimworldwiki.com/wiki/Orders),
[Bills](https://rimworldwiki.com/wiki/Bill),
[Stockpile zones](https://rimworldwiki.com/wiki/Stockpile_zone), and
[Deterioration](https://rimworldwiki.com/wiki/Deterioration).

| Topic | RimWorld vanilla | Ant Game current/intended | Finding |
| --- | --- | --- | --- |
| Work assignment | Per-colonist enabled work or manual priorities 1–4; work-type order also matters. | Per-ant 1–4/Disabled; priority then reachable path cost then deterministic tie-break. | Familiar and understandable. Ant Game exposes only real executors in player UI, which is clearer at this stage. |
| Direct orders | Right-click priority work can interrupt current activity; queues are possible. | Single temporary orders exist for movement/interactions; full explicit work orders, queues and Draft Mode are not complete. | Useful future model, but queueing is not required for the first finished game. |
| Skills | Learn by doing; skill affects speed and often yield/quality/eligibility. Hauling has no skill. | Seven universal skills; current real benefits are Extraction/Construction speed and Hauling slots. | Ant Game intentionally gives Hauling a skill. Reassess only after equipment/carrying design. |
| Mining/harvest | Designation creates work; output becomes map items. Mining skill affects speed/yield. | Extraction designation creates loose Soil/Stone exactly once; no inventory auto-fill. | Similar physical output, with stricter output conservation. Surface harvesting is missing. |
| Hauling | Haulers move items to stockpiles and frames, but builders can fetch materials as part of construction. | Hauling exclusively supplies Construction and Storage. | Intentional stricter separation; do not describe it as RimWorld parity. |
| Construction | Blueprint/frame receives materials and work, can be cancelled/deconstructed under game rules. | Site owns staging; Hauling fills it; builder performs work only; cancellation recovers staging. | Strong conceptual match and cleaner separation. Current Background replacement is transitional. |
| Production | Ordered bills, ingredient filters/radii, worker/skill restrictions, repeat modes and output destinations. | No refining/workstation/bill runtime. | Bills are the useful reference. Ant Game does not need RimWorld's full filter complexity initially. |
| Reservations/interruption | Internal job reservations avoid duplicate claims; jobs can be interrupted and reconsidered. | Explicit job/source/destination reservations and preserved progress/cargo. | Good foundation; timeout/save recovery is missing. |
| Blocked work | Pawns skip unavailable bills/jobs and may display reasons. | Read-only eligibility skips unsupplied Construction and destination-less Hauling with retry/backoff. | Correct direction; 32-candidate cap can still starve work. |
| Deterioration | Many exposed items deteriorate based on environment. | Loose-resource decay/deterioration is not implemented. | Optional later pressure, not foundational. Do not add before conservation and storage UX are accepted. |
| Fog/exploration | Vanilla maps are generally visible; RimWorld is not the specification for Ant Game's personal visibility and pheromones. | Personal visibility, underground sensing, persistent knowledge and recruitment trails are approved future features. | Intentional divergence; avoid importing modded fog behavior as vanilla. |

Useful RimWorld ideas: visible reasons for blocked work, bill suspension/order,
ingredient filters and radius, worker skill bands, output destination policy,
and an inspectable queued-job list. Unnecessary near-term complexity includes
passions, quality/botch waste, hundreds of stockpile filters, mental-break job
logic and the full bill configuration surface.

## 6. Autonomous job lifecycle

### 6.1 Implemented path

`Designation → read-only eligibility → priority → path-cost choice → job/source/destination reservations → navigation → work/transfer → exact commit → release/completion`

- Designations add stable cell-derived jobs to `ColonyJobRegistry`.
- Selection loops priority 1 through 4 and skips Disabled work
  (`ColonyJobs.cs:296-327`). Within a priority, reachable path cost wins; ties
  use category, Y, X and stable ID (`ColonyJobs.cs:307-341`).
- Extraction reserves the job, travels adjacent, accumulates persistent progress
  and commits output at completion.
- Hauling proves source capacity, route and destination before selection,
  reserves destination then source then job, and commits pickup/deposit through
  callbacks (`MaterialHauling.cs:141-169,240-305`).
- Construction stays unowned until fully supplied and performs work only
  (`ConstructionJobs.cs:917-1044`).
- Critical survival can cancel ordinary player/work activity at an explicit
  boundary; ordinary needs wait. Player orders otherwise take precedence.
- Failure/backoff is bounded to 2/4/8 seconds and jobs remain inspectable.

### 6.2 Findings

| Lifecycle point | Status | Evidence and risk |
| --- | --- | --- |
| Designation | Compliant for Colony Extraction/Construction | Stable IDs and duplicate guards; Surface designations absent. |
| Eligibility | Partial | Material dependencies and destination availability are checked without reservation. No skill/hardness eligibility exists. |
| Priority | Compliant at small scale | 1–4/Disabled works as approved. The first-32 collection cap can hide later reachable jobs. |
| Reservation | Strong but lifecycle-limited | Job, loose source, storage input/withdrawal and site input are exclusive. No expiry/save recovery. |
| Assignment | Deterministic | Path cost then deterministic tie-break. Dictionary order still decides which 32 are considered. |
| Navigation | Colony-only | Shared Colony grid navigation is reused. No cross-region route planner or typed job target. |
| Work | Compliant for three real categories | Progress persists; Rested and skills affect work, not priority. |
| Completion | Strong | Stable commit keys and preflighted physical transfers prevent ordinary duplicate completion. |
| Recovery | Partial | Cancellation preserves progress/cargo and recovers site staging. Executor destruction/save-load recovery is not generalised. |
| Multiple ants | Substantially compliant | Exclusive jobs/sources/destinations prevent double use; different jobs can proceed independently. Contention fairness beyond retry ordering is not proven. |
| Region rules | Not ready | Current job target is untyped `Vector2Int` and autonomous evaluation requires Colony residency. |

The founding queen can execute the implemented one-ant Colony bootstrap, but not
the complete future workflow. Multiple ants can specialise through independent
matrices without violating current Soil/Stone ownership. A RimWorld-familiar
player should understand the matrix; blocked-reason surfacing and a job queue
would improve explainability.

## 7. Unified cell model

| Concern | Classification | Current evidence | Required migration |
| --- | --- | --- | --- |
| Generated Surface/Colony record shape | **Compliant at generation time** | Required Background plus optional node/placement/loose fields (`ProceduralWorldGeneration.cs:58-74`). | Preserve as generation DTO; do not make it a second mutable world. |
| Colony Background | **Compliant** | `ChunkedWorldData` always stores resident Background. Extraction removes only Solid. | Keep. |
| Colony Solid/node | **Inactive foundation implemented; active target incomplete** | WV-B3C.1 adds stable typed quantity-one diagnostic records, but generated identity/quantity still projects to a material-only live Solid and `ChunkedWorldData` remains effective authority. | Add Resource Definitions and active variable quantity without replacing navigation/world data, then cut over atomically. |
| Surface Background | **Partially compliant** | Generated Grass/Water is retained in snapshot/presentation, while `SurfaceWorldData` is navigation-only. | Add a Surface runtime cell/source authority for WV-C1. |
| Surface obstacles | **Compatible through adapter** | Obstructing nodes and Water become blocked Surface cells (`ProceduralWorldGeneration.cs:122-135`). | Preserve adapter until shared mutable nodes own obstruction updates. |
| Surface finite sources | **Not implemented at runtime** | Seed/Twig/Freshwater records exist, but there is no consumption/extraction mutation. `RemainingQuantity` is read-only after construction. | Essential for WV-C1/C2. |
| Functional Backgrounds | **Incompatible target representation; active compatibility implementation** | Nursery/Sleeping/Storage functions replace Background material. | Migrate to placements before enabling generated-scenario construction; preserve contents/reservations. |
| Construction staging | **Compliant** | Site owns multiple requirement lines, delivery reservations and quantities. | Generalise only the destination seam needed by Refining. |
| Loose resources | **Partially compliant** | Registry rejects different types on a cell and bounds each stack, but can hold multiple same-type ownership records at one cell. | v1.0 requires one canonical stack record/type per cell plus deterministic multi-cell overflow or pending-output recovery. |
| Storage ownership | **Compatible through adapter** | One functional Background cell owns one filtered material slot. | Move slot to placement during functional-Background migration. |
| Occupancy enforcement | **Partial** | Construction checks Solid and active functional capacity. It does not implement the final general node/placement/loose compatibility matrix. | Add one validation service before broader placement types. |

Essential before WV-C1: a mutable finite Surface source authority, persistent
typed source identity/location, exact source consumption, and separation of
visibility/knowledge from physical data. Essential before generated-scenario
Construction: placement migration. Safe to defer: replacing the Colony
navigation grid, removing compatibility names, one universal renderer, decay,
streaming and a wholesale terrain rewrite.

## 8. Resource ownership and conservation

### 8.1 Current ownership diagram

```text
Generated Colony node record (initial description only)
        │ projection discards node ID/quantity
        ▼
ChunkedWorldData Solid material
        │ ExtractionWorldTransaction preflights output
        ▼
WorldMaterialDropRegistry (ground owner + source reservation)
        │ MaterialHaulingOrder reserved pickup
        ▼
AntRecord.Inventory (carrier owner)
        ├── reserved delivery ──► ConstructionSite requirements/staging
        │                              │ ConstructionWorldTransaction
        │                              ▼
        │                         consumed once into functional Background
        └── reserved delivery ──► MaterialStorageWorld slot
```

The future path should extend the same physical chain:

```text
Node → loose output → Hauling → workstation input staging
     → Refining transaction → loose/recoverable product → Hauling
     → Storage / another workstation / Construction Site
```

### 8.2 Failure-mode matrix

| Failure | Current behavior | Duplication/loss assessment | Gap/action |
| --- | --- | --- | --- |
| Output cell incompatible/full | Extraction fails before Solid removal (`WorldMaterialDrops.cs:57-96`, `:289-318`). | Conserving. | Keep preflight/commit boundary. |
| Repeated Extraction completion | Stable committed-output key rejects repeat. | Conserving. | Runtime node quantity must join this key when finite multi-unit nodes arrive. |
| Two ants choose one loose drop | One drop reservation owner. | Conserving. | Reservation timeout/load recovery missing. |
| Destination fills/changes before pickup | Creation/arrival validation fails; reservations release. | Source remains or carried item remains. | Good. |
| Haul interrupted before pickup | Source and destination reservations release. | Source remains world-owned. | Good. |
| Haul interrupted after pickup | Material remains in ant inventory; self-haul job can be generated. | Conserving, potentially stranded if ant becomes permanently unavailable. | Death/unload recovery required before death gameplay. |
| Deposit callback succeeds but job progress update unexpectedly fails | Destination owns material; order fails/pauses after delivery. | Material is not duplicated, but job state may no longer describe the transfer. | Add a regression or make delivery completion one explicit transaction before expanding destinations. |
| Construction interrupted | Progress and site staging remain; job releases. | Conserving. | Good. |
| Construction designation cancelled | All recoveries are preflighted, then staged types become separate nearby drops. | Conserving in current single-thread model. A mid-loop exceptional failure has no rollback. | Keep bounded; test multi-type failure injection before multi-material recipes ship. |
| Construction result registration fails | Background is restored and prior empty function is re-registered (`ConstructionJobs.cs:588-605`). | Expected conserving because occupied functions were rejected. | Add explicit rollback regression for every placement type during migration. |
| Scenario restore/reset | Registries and inventories are explicitly cleared/rebuilt. | Intended development reset, not persistence. | Save/load semantics remain absent. |
| View switch | Authorities stay loaded and presentation changes only. | No intended ownership change. | Manual WV-B2B/WV-B1 verification remains open. |
| No destination | Hauling remains paused/unowned and does not reserve source. | No loss; avoids monopolisation. | Good after DEF-003. |

The current route most likely to **strand** resources is cargo on an ant that
dies, unloads or loses its binding before a recovery job is created. The current
route most likely to **starve** resources is the 32-candidate selection cap. No
ordinary tested route intentionally duplicates Soil/Stone.

## 9. Foraging, visibility and Fog of War readiness

### Surface

The bounded 64×64 data, typed `SurfaceCell`, local pathfinder, residency and
separate presentation are useful foundations. They do not store hidden/revealed
state, per-ant visibility, persistent exploration, discovery ownership or
pheromone trails. The whole generated presentation is currently visible.
Generated Seed/Twig/Freshwater nodes are immutable records/markers, not gameplay
sources.

### Colony

The 128×128 generated array and resident `ChunkedWorldData` can support cell
queries and tunnel geometry, but there is no line-of-sight, unexplored state or
concealed-deposit knowledge. Solid Stone/Soil is physically present from the
start. A future sensing system must not reveal all cells inside a radius through
rock.

### Recommended separation

1. **Physical visibility:** per-ant, region-specific unobstructed cells. Surface
   may use grid line/raycast rules; Colony must respect Solid occlusion and
   tunnel geometry.
2. **Underground sensing:** a separate Foraging-derived query that can disclose
   a deposit marker through limited solid distance without revealing the
   Background or unrelated cells.
3. **Persistent exploration knowledge:** stable per-cell discovery facts that
   remain after the observing ant leaves.
4. **Colony-shared knowledge:** a deliberate merge/publication step from personal
   observations; do not make renderer visibility the authority.
5. **Pheromone recruitment:** a decaying Surface graph/trail referencing known
   source identity and successful return events, not a replacement for jobs or
   source reservation.

The smallest safe order is knowledge records → physical visibility → Surface
discovery → underground sensing → pheromone recruitment. Rendering should
project knowledge and current visibility rather than own either.

## 10. Refining readiness

Reusable pieces already exist:

- multi-resource `ConstructionSite` requirement/reservation/delivery behavior;
- exact source-to-destination callbacks in Hauling;
- physical inventory, loose output and Storage;
- job priorities, path measurement, cancellation and persistent progress;
- stable resource IDs and exact-once commit-key patterns.

Do not make Refining a kind of Construction. Construction commits a placement;
Refining consumes recipe ingredients and creates products repeatedly. The
smallest future architecture should contain:

1. authored `RefiningRecipeDefinition` data (inputs, outputs, duration, skill and
   eligibility), with no runtime ownership;
2. a workstation placement with input staging and bounded output capacity;
3. ordered production bills (`Do X`, then later `Until X`), suspension and
   worker/skill eligibility;
4. one Refining executor whose partial progress belongs to the bill/work item;
5. an exact transaction that consumes all staged inputs once and creates all
   recoverable outputs once;
6. a narrow haul-destination/source contract implemented by Construction,
   Storage and workstation buffers, replacing the growing enum/branch only when
   Refining is authorised.

Initial scope does not need quality, ingredient hit points, arbitrary filters,
authors, unfinished-item ownership or every RimWorld bill mode. Output blockage
must pause work without consuming ingredients. Products should appear in a
workstation output buffer or adjacent legal loose cell and then require Hauling.

## 11. Procedural generation and scenarios

The implemented generator satisfies its WV-B2B structural contract:

- signed 64-bit seed in metadata;
- version `2`, configuration `wv-b2b.default.002`;
- independent deterministic Surface/Colony SplitMix64-derived streams;
- Surface 64×64, Colony 128×128, with 32×32-compatible Colony chunks;
- one centred linked entrance;
- three-cell-wide passage and exact 7×5 chamber;
- one queen spawn and guaranteed exposed Stone face at `(-4,-8)`;
- validation before snapshot publication;
- placement and loose slots empty at generation;
- explicit generated/fixed-fixture scenario selection.

The snapshot duplicates some state by design: its Colony nodes are projected to
`ChunkedWorldData`; Surface obstruction is projected to `SurfaceWorldData`; the
original generated records remain for identity, markers and diagnostics. That
is safe while the snapshot is immutable, but it cannot become mutable gameplay
truth alongside those projections.

The generated scenario can become the normal startup after source interaction,
placement migration, survival bootstrap and manual acceptance. The Mara/Pip/Tala
fixture should remain a named test fixture rather than a hidden dependency.
Many current Edit/Play tests directly reference `SampleWorldGenerator`, so its
removal is neither safe nor useful now.

## 12. Legacy and compatibility inventory

Classification:

- **A — Safe removal candidate:** no significant candidate proven safe in this
  audit. Text search alone is insufficient and every inspected legacy subsystem
  has source, test, scene, prefab or serialized dependencies.
- **B — Migration required:** active obsolete representation/API that conflicts
  with the approved target.
- **C — Retain temporarily:** fixture, adapter or regression baseline still in use.
- **D — Keep:** active approved system.

| Class | File/symbol | Original/current purpose and references | Serialized/test/runtime evidence | Risk and recommendation |
| --- | --- | --- | --- | --- |
| B | `ExcavationDesignationRegistry`, `ExcavationJobOrder`, `ExcavationWorldTransaction`, `WorldDesignationMode.Excavate` | Former Excavation API now implements authoritative Extraction. Called by controller, HUD and many tests. | Runtime-created plain C# objects; enum/API may be persisted later. `AntSkillId.Excavation` aliases Extraction. | Keep through WV-C1. Rename only with save/API migration and full regression; behavior is active, not obsolete. |
| B | Functional `BackgroundTileMaterialDefinition.Function` plus `SurvivalInteractionWorld`/`MaterialStorageWorld` | Implements Nursery/Sleeping/Food/Water/Material Storage by replacing Background. | Active development scene, recipes, construction, storage/survival tests and presentation. | Migrate contents, reservations and occupancy to placements before generated-scenario Construction. Do not delete first. |
| B | Quantity-less Colony Solid projection | Adapts generated Soil/Stone nodes to existing `ChunkedWorldData` material IDs. | Active generator adapter and Extraction tests. | Add node identity/quantity beside existing world data before multi-unit/shared nodes; retain material definitions for physics/yield. |
| B | Surface obstacle/source compatibility projection | Converts generated Water/obstructing nodes to blocked cells; sources remain markers. | Generated scenario and procedural tests. | Replace only as WV-C1 introduces mutable Surface cells/sources. |
| C | `SyntheticColonyJobOrder` | Development proof for categories without real executors. Controller falls back to it; tests validate scheduling/progression. | No serialized MonoBehaviour; direct runtime/controller and test references. | Retain until real executors replace each synthetic category; then remove fallback with targeted tests. |
| C | `SampleWorldGenerator` and Mara/Pip/Tala | Deterministic integration fixture and accepted gameplay regression environment. | Development host, setup, dozens of Edit/Play tests. | Keep indefinitely as explicit test fixture; remove only production-default dependency after generated startup acceptance. |
| C | `NavigationTestWorldHost` and `AntNavigationPrototype.unity` | Two-layer/navigation classification and renderer diagnostic fixture. | Script GUID `823e6a4cfabd44c4b00c6e87c846ea7d` is serialized in `AntNavigationPrototype.unity`; editor tooling/tests reference host constants. | Retain until equivalent focused fixture coverage is demonstrably better. |
| C | `AntAutonomousMotor` / `AutonomousAnt.prefab` | Pre-production bounded direct-route/grip/gravity prototype and legacy comparison ant. | GUID `fc6f74fffc248e543a0df083a1949bf3` in prefab; host, setup, editor and Play tests reference it. | Retain as regression tool; ensure production prefab continues to exclude it. |
| C | `AntSurfaceMotor`, `PlayerMovementInput`, `PlayerAnt.prefab`, Milestone 1 scene/setup | Historical WASD surface/corner/step-over prototype. | GUIDs `08fec83e33026954e81fbab10f56a6ee` and `a4baee83ff753694f9a1bb0e0d3f3a90` serialized in `PlayerAnt.prefab`; extensive editor regression and tests. | Retain developer-only until approved migration explicitly retires the baseline. Never reintroduce into normal ant prefab. |
| C | `ConstructionOrderPhase.UnloadingInventory` / `TravellingToSource` | Numeric compatibility values from builder self-supply. No current construction order enters them. | Enum values may affect debug/serialized expectations; comments mark compatibility. | Candidate for a future versioned serialization cleanup, not safe removal now. |
| C | Storage-only `MaterialHaulingOrder.TryChooseDestination` overload and `TravellingToStorage` alias | Keeps older callers/tests stable after destination-aware hauling. | Source/test calls and enum compatibility. | Remove only after callers migrate and save/debug versioning is established. |
| D | `AntRecord`, `AntRegistry`, `AntEntityBinding` | Stable identity, skills, needs, work priorities, inventory and visual binding. | Active host/UI/tests. | Keep and extend for persistence. |
| D | `ColonyJobRegistry` and current real executors | Shared job ownership/progress/reservation. | Active controller and tests. | Keep; repair candidate enumeration and add typed locations incrementally. |
| D | `WorldMaterialDropRegistry`, `MaterialStorageWorld`, Construction staging | Physical ownership and exact transfers. | Active runtime and conservation tests. | Keep; extract narrow source/destination contracts only when new consumers require them. |
| D | `ChunkedWorldData`, grid navigation and region residency/entrance services | Accepted Colony world/navigation and WV-B1 region seams. | Active scenes/prefabs/tests. | Keep; do not replace with generated DTOs. |

## 13. Tests and fixture rationalisation

### 13.1 Evidence available at this revision

`CurrentGameState.md` records **82/82 focused Edit**, **9/9 focused Play**,
**539/539 full Edit** and **73/73 full Play**, with no failures or skips, for
`WV-B2B-ARCH-001`. HEAD is the commit that contains the architecture correction
and those documentation claims.

This audit found no retained XML/TRX result files for those runs. Root Unity log
files are untracked and dated 12–14 October, before the audited 15 October HEAD,
so they do not independently prove the 539/73 claims. Source inspection finds
536 literal Edit test attributes and 73 literal Play test attributes; parameter
expansion can explain a higher executed Edit count, but attribute counting is
not test execution evidence.

Therefore the accurate status is: **passing automated evidence is recorded at
the current source revision, but this audit did not independently reproduce it
and no machine-readable result artifact is available in the checkout.**

### 13.2 Open records

| Record | Recommended status | Reason |
| --- | --- | --- |
| `WV-B2B-DEF-001` | Keep open; implementation complete, manual verification pending | Automated coverage is recorded; seed-2203 observation remains unchecked. |
| `WV-B2B-DEF-002` | Keep open; implementation complete, manual verification pending | Generated Stone node and Background preservation need direct Play observation. |
| `WV-B2B-DEF-003` | Keep open; implementation complete, manual verification pending | First-Storage bootstrap and subsequent ordinary hauling need direct Play observation. |
| `WV-B1-REG-001` | Candidate to close only after evidence attachment/recheck | A full 539-test Edit result at the corrected revision would satisfy the broad regression intent, but the checkout lacks XML and the record explicitly remains open. Do not silently infer closure. |

### 13.3 Keep, consolidate later, and add

Keep these regression families even after migrations:

- exact-once Extraction/output failure and Background preservation;
- source/destination reservation competition and cancellation;
- first-Storage bootstrap with no existing Storage;
- multi-ant priority-before-distance and unreachable-target skipping;
- entrance idempotence/rollback and view isolation;
- persistent identity/inventory through rebinding;
- generated fingerprint, dimensions, chamber/entrance and guaranteed sources;
- legacy movement tests while their developer fixtures remain supported.

Potential consolidation, only after coverage mapping:

- repeated fixture-construction helpers across Construction/Hauling tests;
- historical tests that assert builder self-supply, if any remain outside clearly
  labelled historical docs (the current runtime tests should assert Hauling);
- duplicated structural scene checks where one editor validator and one Play
  smoke test prove the same static wiring.

Missing high-value tests:

1. More than 32 same-priority jobs with the first 32 unreachable and a later
   reachable job.
2. Executor death/destruction while holding job, source and destination
   reservations, and while carrying material.
3. Save/load or reconstructed-world reservation reconciliation when persistence
   is introduced.
4. Multi-type construction cancellation with injected failure after one
   recovery output is prepared/committed.
5. Deposit succeeds but job-finalisation fails, proving state reconciliation.
6. Typed same-coordinate jobs in Surface and Colony before cross-region work.
7. Mutable multi-unit node depletion and exact output across interruption.

### 13.4 Cleanup sequence and rollback gates

1. Capture named Unity XML for the current full Edit/Play suites and archive the
   revision/hash with it. No code cleanup first.
2. Add characterization tests for every B/C item above, including serialized
   prefab/scene GUID validation.
3. Migrate one authority boundary at a time behind existing public behavior.
4. Re-run focused tests, full graphics-enabled Edit/Play, prefab missing-script
   checks, scene validators and the exact manual checklist.
5. Remove a compatibility symbol only after `rg`, GUID/serialized-reference
   inspection, reflection/dynamic creation review and test coverage all agree.
6. Keep each removal in a reversible commit; restore the prior adapter if the
   migration fails conservation, visual or manual acceptance.

## 14. Architecture gap matrix

| Gameplay system | Approved design | Actual behavior | RimWorld comparison | Compliance | Missing functionality | Risk | Action | Milestone |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Foraging | Discover; personal radius; fatigue benefit; sensing/trails | Metadata/synthetic only | Vanilla has no equivalent fog/trails | Not implemented | Knowledge, visibility, sensing, source discovery, recruitment | Medium until started | Specify knowledge contract first | Post-WV-C foundation |
| Extraction | Finite shared nodes; loose output; one skill | Colony Soil/Stone Solid removal and loose output | Similar map-item output | Partial compliant | Mutable Surface/multi-unit nodes; hardness | High for WV-C1 | Add runtime node authority incrementally | WV-C1/C2 prerequisite |
| Hauling | Only normal transport; explicit destinations | Construction/Storage destination-aware Soil/Stone hauling | Stricter than RimWorld | Substantially compliant | Destination interface, typed regions, death recovery | Medium-high | Generalise only destination seam | Before Refining/cross-region work |
| Refining | Bills, staged inputs, recoverable outputs | Absent; Production metadata only | RimWorld bills are useful reference | Not implemented | Entire executor/bill/workstation flow | Medium | Build smallest bill/staging slice later | Future Refining milestone |
| Construction | Fully staged builder work; placements | Fully staged builder work; Background replacement | Similar blueprint workflow | Workflow compliant, representation partial | Placement migration, eligibility tiers | High before generated construction | Migrate function to placement | Before generated Construction |
| Work priorities | 1–4/Disabled per ant | Implemented, UI shows real executors | Familiar RimWorld model | Compliant small scale | Fair enumeration, blocked reason UX | High at scale | Fix >32 candidate starvation | P1 foundation |
| Skills | Learn by doing; efficiency not priority | Shared curve; three real benefits | Similar philosophy | Partial | Most skill effects/unlocks | Low now | Add only with real executor | Per gameplay milestone |
| Survival | Needs override safely; natural bootstrap | Fixture interactions/autonomy; generated natural sources inactive | Similar needs scheduling, different model | Partial | Natural sources/rest, death recovery | High for playable start | WV-C1 bounded slice | WV-C1 |
| Unified cells | One natural Background + optional node/placement/loose | Generated DTO complies; runtime adapters diverge | N/A | Partial | Mutable shared contracts | High | Preserve adapters, add runtime identities | WV-C1 and placement migration |
| Persistence | Durable ants/jobs/world/reservations | Stable IDs but no save/load | RimWorld persists full simulation | Not implemented | Serialization/reconciliation | High before long games | Define ownership recovery | Later foundation before content scale |

## 15. Prioritised recommendations

| Priority | Problem and evidence | Benefit / scope | Dependencies and regression risk | Milestone | Approval |
| --- | --- | --- | --- | --- | --- |
| P0 | Complete the documented seed-2203 WV-B2B checklist and WV-B1 view/graphics checks. | Establishes whether the current generated bootstrap is acceptable. No code scope unless a defect appears. | Interactive Unity; risk is observational only. | WV-B2B acceptance | Existing approval; user must perform/confirm manual acceptance. |
| P0 | Preserve open defect/regression IDs until evidence is attached and reviewed. | Keeps status honest. | Obtain XML or rerun targeted/full suite only if needed. | WV-B2B closure | User approval needed to close. |
| P1 | Replace first-32 pre-reachability truncation with bounded fair/pageable evaluation or a maintained eligible index. | Prevents deterministic job starvation without changing priority rules. | Characterization and >32 regression; scheduler risk medium. | Post-WV-B2B foundation | Yes, runtime change. |
| P1 | Continue beyond the accepted inactive WV-B3C.1 registry to Resource Definitions, canonical stacks, active variable Node quantity and whole-node Extraction. | Enables exact shared Node depletion and complete physical output while preserving world/navigation. | Must retain generation IDs, current authorities/tests and staged gates; risk high. | WV-B3C.2A–B3C.3B | Separate approval required. |
| P1 | Introduce typed `RegionDestination` at the job/resource boundary before cross-region autonomy. | Prevents coordinate collision and duplicate schedulers. | WV-B1 residency/local navigators; risk medium. | Before WV-D | Yes. |
| P1 | Migrate functional Backgrounds to placements, one function at a time, with contents/reservation rollback. | Aligns natural Background permanence and generated construction. | Node/occupancy validation and full construction/survival regressions; risk high. | Before generated-scenario Construction | Yes. |
| P1 | Define reservation reconciliation for inactive/deceased ants and future load. | Prevents stranded jobs/resources. | Death/persistence decisions; risk medium. | Before death/save gameplay | Yes. |
| P1 | Correct stale wiki sections that still say Excavation or builder self-supply. | Prevents architectural regression by documentation. | No runtime dependency. | Documentation maintenance | Yes for changing historical wording; supersession notes are safe. |
| P2 | Implement WV-C1 knowledge + finite natural-source slice without pheromones. | Makes founding survival playable while containing scope. | P0 acceptance; mutable source authority; visibility decisions. | WV-C1 | Separate explicit approval required. |
| P2 | Add physical visibility, then underground sensing and shared knowledge. | Creates clear Foraging value without revealing rock incorrectly. | Knowledge contract and renderer projection. | Foraging milestones | Separate approval required. |
| P2 | Add narrow haul source/destination interfaces when Refining begins. | Reuses reservations/staging without enum-branch growth. | Stable Construction/Storage behavior. | Refining foundation | Separate approval required. |
| P2 | Build minimal workstation bills and exact input/output transaction. | Enables products without conflating install and manufacture. | Haul interface, placement model, Refining skill decision. | Refining | Separate approval required. |
| P3 | Add loose-resource deterioration/clutter only after storage UX and conservation are accepted. | Optional logistics pressure/polish. | Balance and room-quality decisions. | Later polish | Yes. |
| P3 | Remove old prototype scenes/classes only after replacement coverage and explicit retirement. | Reduces maintenance eventually. | Serialization/GUID audit and accepted replacement. | Cleanup milestone | Yes. |

## 16. Recommended cleanup and development sequence

1. **Accept or defect WV-B2B:** execute the manual generated-scenario checklist,
   retain exact evidence and keep all open IDs visible.
2. **Evidence hardening:** retain Unity XML by revision and add the >32-job
   starvation regression before scheduler work.
3. **Small scheduler correction:** make same-priority enumeration fair/complete
   without changing 1–4/Disabled or path-cost ordering.
4. **WV-C1 data foundation:** introduce mutable typed finite source/node state and
   persistent knowledge records; preserve both region adapters.
5. **Bounded founding survival:** direct finite source interaction and natural
   rest, with explicit water ownership. Stop before broad Foraging AI.
6. **Placement migration:** move one functional Background type at a time,
   starting with Material Storage because it exercises contents and
   reservations; keep compatibility reads during migration.
7. **Foraging slices:** physical visibility, discovery, sensing, then trails.
8. **Refining foundation:** haul destination abstraction, one workstation, one
   recipe/bill, exact product output; then expand bill modes.
9. **Persistence/death recovery:** reconcile every reservation and carried item
   before permanent death or save/load becomes player-facing.
10. **Legacy retirement:** remove adapters only after runtime, tests, scenes,
    prefabs and manuals no longer depend on them.

## 17. Unresolved design decisions requiring approval

1. Is Hauling permanently a levelled skill, or eventually only a work category
   with equipment/attributes owning capacity?
2. Does “one loose stack per cell” require exactly one runtime record, or merely
   one resource type with multiple ownership keys internally?
3. What are the approved node hardness tiers and whether skill may ever affect
   yield rather than only speed/eligibility?
4. What information is personal versus colony-shared, and when does a returning
   Forager publish it?
5. How do physical line of sight, underground sensing radius and obstruction
   differ numerically?
6. What creates, reinforces and decays pheromone trails, and can multiple sources
   share a route?
7. Which Refining bill modes are required for the first slice: fixed count,
   repeat forever, or maintain stock?
8. Where does a blocked workstation hold finished product, and may production
   pause at 100% before consuming inputs?
9. What reservation timeout/recovery rules apply on death, unload and save/load?
10. Should explicit ordered work ignore a Disabled category, and what exact
    manual work-speed bonus applies?
11. When can obsolete enum values/API names be broken, and what save-version
    migration policy will exist?

## 18. Audit conclusion

Ant Game does not need a new gameplay framework. Its `AntRecord`, job registry,
navigation, loose-resource, storage and staged-construction seams are appropriate
foundations. The next architectural work should make current ownership typed and
fair across scale, not replace it. WV-C1 should begin only after WV-B2B manual
acceptance and explicit approval, using incremental adapters and conservation
tests. This audit makes no recommendation executable by itself.
