# Construction and Placement Specification

**Status:** Approved design contract; legacy migration not implemented.

**Approved:** 16 October 2026

This specification complements
[`ResourceAndNodeSystem.md`](ResourceAndNodeSystem.md). That document defines
natural Backgrounds, Nodes and loose resources; this document defines built
improvements, Construction Sites and completed Placement instances. Existing
functional Background behavior remains compatibility implementation until an
explicit WV-B3D cutover is implemented and accepted.

**WV-B3D.2 runtime amendment — 19 October 2026:** The scenario-scoped
`PlacementRegistry` now implements definition lookup, stable identity,
one-cell/future-footprint occupancy, lifecycle/revisions, compatibility,
read-only legacy projections and runtime-cell publication. Existing functional
Background owners remain exclusive; projected capacity and contents are not
activated or copied. Definition and instance contracts include structural HP,
but legacy projections report it as unknown and no final HP values are set.
Damage, Repair, destruction, removal and stored-content recovery are future
transactions, not WV-B3D.2 gameplay.

**WV-B3D.1 technical amendment — 19 October 2026:**
[`PlacementMigrationTechnicalSpecification.md`](PlacementMigrationTechnicalSpecification.md)
provides the implementation-level registry, identity, footprint, completion,
function-by-function cutover, recovery, testing and legacy-retirement contract.
This approved documentation gate changes no runtime authority. Its smallest
recommended implementation successor is WV-B3D.2 one-cell Placement Registry
Foundation with read-only legacy projections only.

### Quantity display compatibility amendment

Until WV-B3D, each visible legacy Material Storage Background cell represents
one physical slot and displays one compact badge for its actual stored quantity;
zero is hidden. Ground stacks and Construction staging are not counted as
Storage contents. Future multi-cell Storage shows one badge per Placement
identity, not one per footprint cell. When a Construction Site selection model
exists, the selected site displays each staged requirement independently (for
example `Soil 2/5`); WV-B3C.2A does not invent that selection authority or
migrate Backgrounds into Placements.

## 1. Audited current inventory

### Background definitions

| Existing ID | Display name | Current classification | Function/capacity |
| --- | --- | --- | --- |
| `background.default` | Default Background | Legacy generic natural Background | None / 0 |
| `background.sky` | Sky | Natural Background | None / 0 |
| `background.earth.unprepared` | Natural Earth | Natural Background | None / 0 |
| `background.stone.unprepared` | Natural Stone | Natural Background | None / 0 |
| `background.prepared.grippable` | Prepared Grippable Background | Constructed conversion with movement-speed effect; not a `BackgroundFunction` | None / 0; current speed multiplier `1.25` |
| `background.function.sleeping` | Sleeping | Legacy functional Background | `Sleeping` / 1 exclusive sleeping position |
| `background.function.food-storage` | Food Storage | Legacy functional Background | `FoodStorage` / 1 resource slot |
| `background.function.water-storage` | Water Storage | Legacy functional Background | `WaterStorage` / 1 resource slot |
| `background.function.material-storage` | Material Storage | Legacy functional Background | `MaterialStorage` / 1 filtered material slot |
| `background.function.nursery` | Nursery | Legacy functional Background | `Nursery` / declared capacity 1; brood runtime absent |

`BackgroundFunction` contains only `None`, `FoodStorage`, `WaterStorage`,
`Sleeping`, `MaterialStorage` and `Nursery`. No Door, Workshop, Refining,
Production or Burial definition exists in the inspected runtime/assets.

All listed non-Sky authored backgrounds currently permit horizontal, upward and
downward movement and use existing grip settings. Natural/Prepared movement
metadata must remain on the retained Background or a separate surface-improvement
effect after migration; it must not be accidentally transferred into functional
Placement ownership.

### Current Construction recipes

The development host builds these code-defined, provisional single-cell recipes.
Every recipe uses Construction skill through the existing job, takes four
simulation hours before skill/Rested modifiers, and currently accepts any exposed
source Background because `RequiredSourceMaterialId` is empty.

| Existing recipe ID | Current result ID | Cost | Current result |
| --- | --- | ---: | --- |
| `conversion.prepared` | `background.prepared.grippable` | 1 Soil | Prepared movement surface |
| `conversion.nursery` | `background.function.nursery` | 1 Soil | Nursery compatibility Background |
| `conversion.sleeping` | `background.function.sleeping` | 1 Soil | Sleeping compatibility Background |
| `conversion.food-storage` | `background.function.food-storage` | 1 Soil | Food Storage compatibility Background |
| `conversion.water-storage` | `background.function.water-storage` | 1 Stone | Water Storage compatibility Background |
| `conversion.material-storage` | `background.function.material-storage` | 1 Soil | Material Storage compatibility Background |

These costs, durations and stack capacities are existing provisional values,
not newly approved balance. No Door or Refining Station recipe exists.

### Current runtime ownership and interaction

- `ConstructionDesignationRegistry` owns the designation view over the shared
  `ColonyJobRegistry`; `ConstructionSite` owns required, reserved and delivered
  staged quantities.
- Destination-aware `MaterialHaulingOrder` delivers loose/carried resources to
  the site. Construction never gathers its own material.
- `ConstructionJobOrder` performs work only after all site requirements are
  staged. `ConstructionWorldTransaction` consumes staging exactly once and
  currently replaces the Background ID, then registers compatible capacity.
- `SurvivalInteractionWorld` owns Food/Water quantities and reservations and
  exclusive Sleeping reservations. Food/Water slots accept their one configured
  resource identity; the class has no approved maximum stack capacity.
- `MaterialStorageWorld` owns one filtered Soil/Stone/general slot, contents and
  incoming reservations. Its current maximum quantity is the provisional
  `MaterialResourceCatalog.ProvisionalMaximumStack` (`10`).
- Nursery currently has only the enum/material/recipe and declared one-unit
  capacity. No brood owner or Nursery interaction is implemented.
- Prepared currently changes the Background and movement speed. It owns no
  functional capacity.
- Rooms, enclosure detection, doors and quality scoring are not implemented.

Primary regression coverage is in `ConstructionJobTests`,
`ConstructionPlayModeTests`, `MaterialStorageHaulingTests`,
`SurvivalInteractionTests`, `SurvivalInteractionPlayModeTests`, autonomous
survival tests, runtime-cell authority tests, development-scene setup validation
and related navigation/Background refresh tests.

## 2. Approved cell and Placement architecture

Every resident cell has exactly one natural Background. A compatible optional
Construction/Placement occupies a definition-sized footprint over that
Background. Function activates only after successful Construction completion.
Construction never replaces or owns the natural Background.

A site is not a completed Placement. Before completion it owns only its stable
identity, footprint reservation, recipe, staged resources, work progress,
reservation state and lifecycle. It provides no Storage, Sleeping, Nursery,
Refining, Door or other functional capacity.

On exact-once completion, the transaction consumes the staged recipe once,
creates the Placement once, closes the site, and publishes the final compatible
occupancy. A failed completion preserves or rolls back all staged material and
does not expose partial function.

## 3. Placement Definition contract

A reusable Placement Definition contains immutable metadata:

- stable Placement Definition ID and display name;
- category and completed function;
- compatible region types;
- one or more typed footprint offsets and an orientation policy;
- Background, Node, other Placement and loose-object compatibility rules;
- physical construction-material recipe;
- Construction work amount, skill and interaction requirements;
- movement obstruction/passability/support properties;
- valid work/use interaction positions;
- capacity kind and amount where meaningful;
- completed functionality and allowed contents;
- room-quality modifiers and whether enclosure is required for a bonus; and
- lifecycle, removal, refund, content recovery and failure rules.

A runtime Placement instance owns changing state: stable instance ID,
definition ID, typed anchored footprint, orientation, lifecycle, revision,
functional contents/capacity reservations, users/occupants, damage/condition if
later approved, and room association/bonus snapshot. Definition metadata never
stores a particular Placement's contents, reservations or work progress.

Durability metadata consists of nullable positive Max HP, destructibility,
repairability and future resistance metadata. A future authoritative completed
instance requires known Max HP and starts full; current HP is bounded to
`0..Max`, condition is derived, and only a committed change advances its
revision. Legacy projections expose unknown HP rather than fabricate it.
Zero-HP destruction and Repair remain separately approved workflows. Destroyed
Storage may not lose or duplicate contents, and Repair must use a distinct job,
physical staging where approved, Construction skill and exact-once consumption.

## 4. Initial Placement catalogue

The proposed IDs below are target design identities, not existing Unity asset
IDs and not permission to add runtime definitions.

| Target Placement | Proposed definition ID | Current implementation | Target capacity and interaction | Outstanding decisions |
| --- | --- | --- | --- | --- |
| Prepared Surface | `placement.surface.prepared` | `conversion.prepared` replaces Background; movement speed `1.25` | No functional capacity; applies approved movement improvement to one cell | Whether this is a Placement, surface-treatment slot or Background modifier; removal/refund |
| Sleeping Spot | `placement.sleeping-spot` | Functional Background plus `SurvivalInteractionWorld` sleeping set/reservation | One ant reservation/use at a time; adjacency and existing timed Rest | final recipe/work, assignment, removal while occupied, quality bonus |
| Food Storage | `placement.storage.food` | Functional Background plus one `SurvivalStorageSlot` | One compatible physical food stack/type and source-owned reservations; adjacency for Eat/deposit/withdraw | concrete food definitions, stack capacity, filters, removal recovery |
| Water Storage | `placement.storage.water` | Functional Background plus one `SurvivalStorageSlot` | One liquid-compatible capacity unit with reservations; adjacency for Drink/deposit/withdraw | carrier/cache rules, capacity, filters, removal recovery |
| Material Storage | `placement.storage.material` | Functional Background plus `MaterialStorageWorld` slot | One filtered physical material stack/type; current Soil/Stone/all filters and provisional max `10` | final capacity/filter UX and removal recovery |
| Nursery | `placement.nursery` | Material/enum/recipe only; no brood runtime | One brood slot; unenclosed use allowed | brood lifecycle, interaction, recipe/work, occupied removal/reassignment, quality bonus |
| Door | `placement.door` | Not implemented; no material, recipe, runtime class or test | No generic capacity unit; passability/opening ownership and interactions apply | footprint/orientation, recipe, movement states, access, damage, room boundary behavior |
| Refining Station | `placement.refining-station` | Future design only | No assumed 1:1 user/item capacity; owns approved bills, input staging and output transaction | footprint, recipes, bills, workers, output blocking, capacity, room bonuses |

The one-Placement-to-one-capacity-unit rule applies to Sleeping, Food Storage,
Water Storage, Material Storage and Nursery where stated. It does not assign an
artificial capacity unit to Doors, Prepared Surface or Refining Stations.

## 5. Construction workflow

The approved sequence is:

**Designation → Hauling → Material Staging → Construction Work → Completed Placement**

1. Designation validates every footprint cell and creates one site with no
   completed function.
2. Hauling reserves one or more compatible physical sources and site capacity,
   then delivers one exact combined load into site-owned staging. Current
   sources may be canonical ground stacks or completed filtered Material Storage;
   Storage retains quantity ownership until its reserved withdrawal commits.
3. Construction becomes eligible only after every requirement is delivered.
4. The builder works from valid interaction positions using Construction skill.
5. One transaction revalidates footprint/site revisions, consumes staging and
   creates the completed Placement exactly once while retaining Backgrounds.
6. The Placement authority registers its function/contents. Consumers observe no
   intermediate state in which materials are gone but function is absent.

Cancellation releases reservations and returns staged resources to recoverable
physical output. Interruption preserves committed staging and valid work
progress. Construction never calls loose pickup, Storage withdrawal or inventory
collection on its own.

The current compatibility `MaterialStorageWorld` may supply Construction but
never another Storage destination. This preserves filters, prevents automatic
Storage reorganisation loops and does not begin Placement migration. A future
Placement cutover must retain both incoming-capacity and outgoing-quantity
reservations plus in-flight Hauling recovery.

## 6. Rooms and quality

A room may contain multiple Placement types. It is fully enclosed only when
valid terrain/structure boundaries surround it and every opening has a completed
constructed Door. Background identity never supplies enclosure, room function or
capacity.

Appropriate Placements may receive room-quality efficiency modifiers. Mixed-use
rooms are allowed. An unenclosed Nursery remains functional at its base one-slot
capacity but receives no enclosure-dependent bonus. Quality must modify a
Placement's permitted effects, never duplicate contents, create capacity, or
make an incomplete site functional. Exact room detection, scores, modifier
curves and eligible Placement categories remain unapproved/unimplemented.

## 7. Legacy migration matrix

| Legacy representation / authority | Target definition | State that must survive | Cutover and regression requirements |
| --- | --- | --- | --- |
| `background.prepared.grippable`; `ChunkedWorldData` movement metadata | `placement.surface.prepared` or approved treatment slot | underlying natural Background; speed effect; navigation revision; authored fixture intent | Decide representation first; migrate movement tests and Tilemap refresh without losing natural grip |
| `background.function.sleeping`; `SurvivalInteractionWorld` sleeping positions/reservations | `placement.sleeping-spot` | stable cell/instance mapping, active reservation, Rest interaction/progress | WV-B3D survival slice; survival Edit/Play, interruption and removal tests |
| `background.function.food-storage`; `SurvivalStorageSlot` | `placement.storage.food` | resource ID, quantity, reservations and consumers | WV-B3D survival-storage slice; conservation and autonomous-survival regressions |
| `background.function.water-storage`; `SurvivalStorageSlot` | `placement.storage.water` | resource ID, quantity, reservations and liquid representation | WV-B3D survival-storage slice after Water representation approval |
| `background.function.material-storage`; `MaterialStorageWorld` | `placement.storage.material` | filter, resource ID, quantity, capacity and incoming reservations | First recommended WV-B3D slice; Hauling, first-Storage, Construction recovery and HUD regressions |
| `background.function.nursery`; no brood owner | `placement.nursery` | authored/constructed cell and one-slot capacity intent; no invented brood state | WV-B3D Nursery slice before brood; construction/occupancy tests and later brood tests |
| no legacy Door | `placement.door` | none | Separate Door milestone after definition/room-navigation approval |
| no legacy Refining Station | `placement.refining-station` | none | Separate Refining foundation after bill/input/output approval |

Each migrated cell is validated against Node, loose-stack, footprint and region
compatibility. Candidate state is built privately, including contents and
reservations, before one cutover disables the legacy function and enables the
Placement. A cell may never run both. Failed migration leaves the legacy state
active and unchanged; removal occurs only after serialization/source/fixture and
full regression proof.

## 8. Recommended migration order

1. Placement Definition/read model and compatibility validation only.
2. Material Storage, because it exercises contents, filters, reservations,
   Hauling and Construction bootstrap.
3. Sleeping, then Food/Water Storage after resource/liquid contracts are ready.
4. Nursery before brood implementation.
5. Prepared Surface only after its representation decision.
6. New Doors and Refining Stations in separately approved feature milestones.

This ordering is planning guidance only. WV-B3D.1 documentation is complete;
no runtime Placement implementation has begun.

## 9. Remaining approvals

- final target stable IDs and serialization/version policy;
- Prepared Surface as Placement versus treatment/modifier;
- final recipes, work amounts and refund/recovery rules;
- multi-cell footprints, orientation and interaction-anchor rules;
- obstruction/passability and Door state/access behavior;
- Food/Water/Material stack capacities and filters;
- Nursery/brood ownership and occupied-removal recovery;
- room enclosure algorithm, quality metrics and modifiers; and
- Refining Station bills, staging, outputs and capacities.

`WV-B3B-LIM-001` remains active: atomic read-model publication is not complete
whole-world construction/restoration atomicity.
