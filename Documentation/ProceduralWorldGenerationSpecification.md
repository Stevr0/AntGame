# Procedural World Generation Specification

## Resource and Node System v1.0 amendment — 16 October 2026

[`ResourceAndNodeSystem.md`](ResourceAndNodeSystem.md) now governs generated
resource identity and quantity. Every target Solid is a finite Node with a
specific Resource Definition and positive actual quantity. Generation chooses
that quantity deterministically from a named, versioned seed domain containing
the world/generation identity, definition and stable Node location/identity;
iteration order must not influence the result. The Node retains that actual
quantity until an authorised gameplay transaction changes it.

Normal Solid Nodes use whole-node Extraction. Water is finite and, when emitted
as a Solid Node, follows the same depletion rule while requiring a
liquid-compatible output representation. Future timed Surface events may create
explicit new Nodes or loose stacks, but may not regenerate extracted terrain.

The current generator remains compatibility implementation: Colony Soil/Stone
records are quantity one, Surface Stone/Seed/Twig/Freshwater remain
`GenerationOnly`, Water also appears as navigation Background/obstruction data,
and no active variable-quantity or liquid transaction exists. This documentation
amendment changes no runtime generation or version-2 seed results.

## WV-B3B runtime publication clarification — 15 October 2026

Validated generated records now initialise existing Colony and Surface
authorities and are exposed through one atomically replaced read-model pair.
They remain generation provenance, not a second mutable runtime store. Generated
Surface nodes expose their IDs and initial/remaining generated quantity only as
`GenerationOnly`; extraction and consumption remain deferred. Camera switching
does not publish or regenerate either region. The host's read-model pair is
all-or-nothing, while broader service construction still uses the existing
development-host rebuild/restore boundary rather than a persistence transaction.

## WV-B3A runtime publication clarification

`UnifiedRuntimeCellSpecification.md` now owns the post-generation runtime-cell
contract. `GeneratedWorldSnapshot` is immutable initial/provenance data after
validation. Publication must privately build both authoritative region states,
atomically expose the complete pair, then initialise navigation/rendering and
ants/residency before simulation begins. Generated records must never remain a
second mutable Node-quantity or occupancy authority, and camera switching must
never regenerate or republish a world. The current compatibility projections
remain implemented behavior until separately approved WV-B3 slices.

## WV-B2B-ARCH-001 logistics and terminology addendum

Extraction is the single authoritative node-removal activity and skill. Soil,
Stone, Seed and Twig generated node records declare `extraction` for both
activity and skill. Colony Soil/Stone interaction is implemented through
compatibility-named `Excavation*` classes; Surface Seed/Twig interaction remains
approved future behavior and is not implemented in this correction.

Node work commits only after recoverable loose output can be represented. The
next ownership transfer is a separate destination-aware Hauling transaction to
Construction Site staging or compatible Material Storage. Construction is
eligible only after its site owns every required quantity. Storage is not a
bootstrap prerequisite. This supersedes Construction self-supply and does not
start WV-C1.

## Status and authority

> **WV-B2B — COMPLETE (15 October 2026).** The approved
> contract is implemented by `ProceduralWorldGenerator` version `2`, configuration
> `wv-b2b.default.002`. Version 2 guarantees an exposed Stone node on the
> founding chamber's west face. The user accepted the seed-`2203` generated
> terrain, queen, Colony production, view and entrance workflow. Current full
> Unity XML passes 551/551 Edit and 74/74 Play, closing `WV-B1-REG-001`. The
> queen's later survival death remains deferred WV-C1 scope. This status does not
> authorise WV-C1.

### Implemented configuration record

- The authoritative seed is signed 64-bit metadata. FNV-1a domain derivation
  creates independent `surface` and `colony` SplitMix64 streams.
- Surface categories use provisional targets derived from 82/8/6/4 weights:
  Grass base, connected Water Background clusters, clustered Stone nodes and
  spaced natural-obstacle nodes. Seed, Twig and Freshwater are generation-only
  node definitions with configurable provisional quantities.
- Colony columns `0..127` map to X `-64..63`; depths `0..127` map to Y `-1..-128`.
  Four-by-four signed chunks span `(-2,-4)` through `(1,-1)`. Soil/Stone nodes
  project to the existing `solid.soil`/`solid.stone` definitions.
- Surface approach/arrival are `(32,31)`/`(32,32)`. Colony approach/arrival are
  `(0,-2)`/`(0,-1)`. The chamber occupies columns `61..67`, depths `5..9`; the
  passage occupies columns `63..65`, depths `0..4`; queen spawn is `(0,-8)`.
  A quantity-one `solid.stone` node is guaranteed at `(-4,-8)`, adjacent to the
  chamber, so the opening excavation loop never depends on a buried cluster.
- Generated placements and loose stacks are empty. Existing functional
  Background, storage and multi-stack authorities are deliberately not migrated.

### Unified cell/resource amendment — approved 15 October 2026

> **IMPLEMENTED FOR GENERATED DATA; LEGACY MIGRATION DEFERRED.** This amendment is authoritative for
> WV-B2B and later work. It unifies the logical cell, natural-resource and
> placement model across Surface and Colony without merging their coordinate,
> navigation, movement or rendering implementations. Existing functional
> Background content remains supported through future adapters until an explicit
> migration is implemented and accepted.

Every resident cell has these logical occupancy slots:

| Slot | Cardinality | Authority |
| --- | ---: | --- |
| Natural Background | exactly one | Stable natural environment and its movement/presentation metadata |
| Solid / Resource Node | zero or one | Natural removable/extractable node, quantity and obstruction |
| Construction / Placement | zero or one | Player-created or authored functional placement and any placement-owned capacity |
| Loose Resource Object stack | zero or one | One physical resource type and bounded quantity on the ground |

These are data responsibilities, not a requirement for four Unity Tilemaps.
Region-specific renderers may combine or separate visuals as appropriate. An
available cell with none of the three optional occupants still retains its
Background. Background remains present behind every optional occupant and is
revealed, not created, when a node is depleted or removed.

### Background contract

A Background is the required natural environment of a cell: for example Grass
on the Surface and Soil or Stone underground. It has a stable definition ID and
may retain region-appropriate presentation, movement permissions, grip, speed,
hazard and terrain-compatibility metadata. Background presence alone does not
imply that the cell is traversable; node obstruction, construction obstruction,
region-specific clearance and local movement rules also participate.

Sleeping, Food Storage, Water Storage, Material Storage, Nursery, doors and
future production/burial functions are conceptually Constructions/Placements,
not natural Backgrounds. The current project stores those functions in
`BackgroundTileMaterialDefinition.Function` and replaces Background IDs during
construction. That representation remains implemented compatibility data only;
future migration must preserve the natural Background and project the legacy
functional Background as a placement without silently deleting existing
fixtures, contents, reservations or construction progress.

### Solid / Resource Node contract

“Solid” and “Resource Node” are one authoritative optional cell occupant. A node
definition determines whether it blocks or supports movement, what work can act
on it and what happens on depletion. A node instance contains:

- stable `NodeId`, or a deterministic ID derived from generation identity,
  typed region and cell;
- stable node/resource definition ID;
- typed region and cell location;
- initial/maximum quantity and mutable remaining quantity;
- obstruction/support and extraction/excavation properties from its definition;
- required activity, skill and work amount;
- output resource definition and output quantity rule;
- depletion behavior, normally removing the node and revealing the unchanged
  Background.

Soil and Stone tiles are nodes. Seed, Twig and other future finite Surface
sources are also nodes. Not every node blocks movement: a Soil wall may block,
while a seed source may permit traversal. Extraction is node-defined. Normal
Solid terrain uses whole-node Extraction: one completed job removes/depletes the
Node and commits its complete remaining quantity. Partial harvesting is allowed
only for a future definition that explicitly authorises it; it cannot silently
replace the normal Solid rule. Generating a Node does not generate a loose stack.

### Construction / Placement contract

A Construction/Placement is independent of the natural Background. Unless its
definition explicitly allows an exception, designation and commit require:

- a resident cell with no Solid/Resource Node;
- no existing Construction/Placement;
- no incompatible loose stack;
- a valid accessible work position under local navigation rules;
- required physical materials delivered through authoritative reservations and
  transfers;
- an ant completing the Construction work.

Construction preserves staged physical materials, interruption recovery and
learn-by-doing Construction skill; destination-aware Hauling owns supply.
Completion creates the placement and consumes exactly the recipe quantity; it
does not replace the natural Background. Placement definitions own obstruction,
capacity, allowed resources and any explicit Background exception.

Storage is a Construction. One storage placement provides one functional
capacity unit/slot where the established 1:1 rule applies. “One capacity unit”
means one placement-owned resource slot, not one individual item. The slot holds
zero or one resource type at a time up to the capacity/stack limit defined by
the storage and resource rules, supports reservations, deposits, withdrawals
and filters, and remains physically conserved. Stored contents belong to the
Construction; they are not the cell's independent loose ground stack. The
current Material Storage maximum of 10 is provisional balance, not a universal
rule. Food and Water storage must converge on the same placement-owned principle
when their future interaction contract is implemented.

### Loose Resource Object contract

A loose stack is a physical, recoverable resource object produced by extraction,
excavation, dropping or cancellation recovery. It may occupy an accessible,
traversable cell without Storage. Exactly one loose resource type is allowed per
cell. Same-type units merge up to the applicable capacity; a different type
cannot coexist in that cell. A whole-node yield may reserve several
deterministically ordered compatible cells. If complete output cannot be
prepared, a recoverable pending transaction retains the Node/output reservation
and the source operation does not deplete the Node.

Loose ownership key, resource ID, quantity, capacity and reservations are
authoritative. Pickup, transport, drop and storage use prepared exact-once
transactions. Interruption releases only reservations and never duplicates or
destroys physical quantity. Resource decay is approved future design but is not
part of WV-B2B and has no timer or rate here.

### Shared node-interaction transaction

All extraction-like work uses one logical transaction boundary while node
definitions select the activity, skill, duration, quantity and output:

| Node example | Activity | Skill | Physical result |
| --- | --- | --- | --- |
| Soil | Extraction | Extraction | Soil loose resource |
| Stone | Extraction | Extraction | Stone loose resource |
| Seed source | Extraction | Extraction | Seed loose resource (interaction future) |
| Twig source | Extraction | Extraction | Twig loose resource (interaction future) |

Before commit, validate node identity/revision, requested quantity, work
completion and a legal output stack destination. The atomic commit reduces node
quantity exactly once, removes it if depleted, creates/merges the physical output
exactly once and invalidates relevant navigation/presentation. If output cannot
be represented, the node and quantity remain unchanged. Output never enters an
ant inventory merely because work completed. Direct eating/drinking is a
separate consumption transaction against the authoritative source/node and does
not implicitly create a loose item.

### Shared concepts, region-specific behavior

Surface and Colony share slot meanings, stable resource definitions, node
quantity/depletion rules, loose accounting and placement principles. They may
differ in visuals, generated materials and weights, navigation graphs, movement,
clearance, obstruction interpretation and available interactions. Typed
`SurfaceCell` and `ColonyCell`, their explicit conversions, `SurfacePathfinder`,
the Colony grid pathfinder, residency and the WV-B1 entrance transaction remain
separate. A unified cell model does not authorise merging local A* systems.

### Existing implementation compatibility assessment

| Current implementation | Difference from approved model | Required treatment |
| --- | --- | --- |
| `ChunkedWorldData` | Owns required Background plus optional Solid material ID; no construction/loose slots and no per-Solid identity/quantity | Preserve as the Colony terrain adapter initially; add shared logical cell records and node instance authority without breaking current terrain queries |
| `BackgroundTileMaterialDefinition.Function` and `ConstructionWorldTransaction` | Functional placements are encoded as Background definitions and construction replaces the Background ID | Keep legacy content readable; adapter exposes the natural/source Background plus a legacy placement. Later transaction writes a placement record instead of replacing Background |
| `ConstructionDesignationRegistry` | Calls work “Background conversion” and owns site staging | Preserve jobs, recipes, staging and skill XP; Hauling supplies sites and Construction begins only when fully staged |
| `SurfaceWorldData` | Stores only bounds and an obstacle set | WV-B2B must provide a unified-cell Surface data adapter with required Backgrounds and optional generated nodes while leaving Surface A* separate |
| `SolidTileMaterialDefinition` | Defines excavatable material/yield per type, while a Solid instance has no node ID or remaining quantity | Adapt each legacy Solid as a quantity-one node using a deterministic cell identity; introduce explicit node instances for generated worlds |
| `ExcavationWorldTransaction` | Compatibility-named exact-once Extraction seam; removes a complete Soil/Stone Solid and creates loose output | Generalize later behind the shared node transaction for Seed/Twig without creating a second skill or scheduler |
| `WorldMaterialDropRegistry` | Keyed physical stacks with reservations, but multiple stacks and different material types can occupy one cell | Before shared extraction/drop use, enforce one cell-owned loose stack, same-type merging and deterministic alternate/reject behavior; migrate fixtures without loss |
| `MaterialStorageWorld` | One Background-owned slot, one type at a time, bounded quantity and strong reservations | Rebind ownership to a Storage placement while retaining filters, exact quantities, reservations and provisional stack 10 |
| `SurvivalInteractionWorld` | Food/Water/Sleeping depend on functional Background IDs; food/water slots allow quantities but are separate from Material Storage | Adapt as legacy placements; later converge content ownership and source consumption on the placement/node transaction contracts |
| `AntInventory` and hauling/construction orders | Use typed bounded slots and prepared source/destination commits | Preserve physical ownership; Hauling alone moves resources to site staging or Storage |

The current drop registry specifically contradicts the newly approved
single-type-per-cell rule. Existing documentation that permitted multiple
traversable stacks at one cell is superseded. No current data is silently
rewritten by this amendment.

### Migration sequencing

**Essential for WV-B2B procedural generation**

1. Define a region-neutral logical cell snapshot/record with required Background
   and optional node/placement/loose slots, wrapped by typed region coordinates.
2. Add stable Background/node/resource definition boundaries and deterministic
   generated node identities. Legacy Colony Solids adapt as quantity-one nodes.
3. Generate `64×64` Surface and `128×128` Colony Background/node occupancy into
   temporary unified records; placement and loose slots begin empty.
4. Provide separate Surface and Colony adapters so existing pathfinders and
   Colony terrain rendering can consume the generated world without merging.
5. Preserve the explicit generated-versus-Mara/Pip/Tala scenario selection and
   run structural validation. WV-B2B need not implement extraction, pickup,
   construction or migration of the development fixture.

**Required before WV-C1 resource interaction**

1. Implement the shared revision/id-guarded node transaction and direct
   consumption distinction.
2. Enforce one loose type/stack per cell, compatible merge capacity and
   deterministic fallback/rejection in every output/drop/cancellation path.
3. Define approved Seed/Freshwater/Twig resource IDs, quantities, skills,
   interaction work and terrain compatibility.
4. Add cross-region resource/quantity conservation tests while retaining local
   navigation boundaries.

**Required before construction is enabled in the generated scenario**

1. Introduce placement authority and shared occupancy validation.
2. Project or migrate legacy functional Backgrounds to placements without
   losing storage contents, reservations, sleep reservations, job progress or
   fixture identities.
3. Rebind Material/Survival Storage ownership to the placement and leave the
   natural Background unchanged on build/cancel/remove.

**Safely deferred**

- decay and preservation bonuses;
- richer node hardness/tools and partial-yield balance;
- doors, production, burial, nursery and room-quality behavior beyond adapters;
- save migration, streaming, unloaded simulation and additional entrances;
- replacing legacy fixture serialization after adapter acceptance.

Potential regressions include changed traversability where node/placement
obstruction is misclassified; lost functional capacities or contents during
Background-to-placement migration; duplicate/lost resources when consolidating
same-cell drops; stale reservations; construction cancellation failing because a
cell already has a different loose type; fixture/test IDs changing; and
Surface/Colony coordinate leakage. Each requires explicit regression coverage.

This specification supersedes older new-game statements that place the founding
queen on the Surface or begin with no excavated Colony space. A new generated
game begins with one founding queen in one small underground starting chamber.
It does not replace the existing Mara/Pip/Tala development fixture or change the
accepted WV-A/WV-B1 entrance transaction.

## 1. Approved product rules

- World View and Colony View are separate full-screen regions. World View is
  top-down; Colony View is side-on and entirely underground.
- One authoritative world seed deterministically produces both initial regions.
- Surface Backgrounds are predominantly Grass. Underground Backgrounds are
  predominantly Soil, while removable Solid/Resource Nodes are predominantly
  Soil nodes with smaller Stone deposits. Existing `background.earth.unprepared`
  and `solid.soil` are the provisional compatibility mappings until naming is
  separately approved.
- The Surface entrance is approximately central. The Colony exit occupies the
  approximately central cell of the topmost underground boundary.
- Exactly one small chamber and its minimum connecting passage begin excavated.
  The founding queen starts at a valid location in that chamber.
- The rest of the initial Colony is solid and is expanded through existing
  excavation mechanics.
- Required Background, optional Solid/Resource Node, optional Construction and
  optional loose stack are distinct occupancy slots. The entrance is a linked
  feature. Removing a node never removes the Background.
- The existing `entrance.founding-nest.001`, typed endpoints, capacity-one
  ownership, idempotent commit and rollback behavior remain authoritative.
- Both starting regions remain resident on the shared simulation clock. Camera
  switching is presentation-only and never regenerates either region.

## 2. Current architecture and integration findings

### Implemented seams to preserve

| Existing authority | Current behavior | WV-B2B integration |
| --- | --- | --- |
| `ChunkedWorldData` | Scene-independent sparse Colony data; explicit resident `32×32` chunks; one permanent Background ID and zero or one Solid ID per cell; reads never create chunks | Generate a complete `GeneratedColonyRegion`, validate it, then populate explicit chunks through current creation/mutation APIs or a narrowly equivalent bulk builder |
| `WorldTileCoordinates` | Owns signed world-cell/chunk/local conversions, including mathematical floor division for negative cells; one-unit cell centres | Remains the only conversion authority after generated Colony-local depth coordinates are mapped to signed Colony world cells |
| `SurfaceWorldData` | Bounded `64×64` logical grid with fixed obstacle cells and a future `32×32` partition seam | Extend or replace fixture construction behind the same bounded/traversability responsibility; do not route Surface terrain through Colony `ChunkedWorldData` by assumption |
| `SurfacePathfinder` | Deterministic eight-direction A* with no diagonal corner cutting | Consume generated Surface traversability; generation must guarantee its required start, entrance and source approaches are traversable |
| `GridNavigationWorldService` / `GridPathfinder` | Shared Colony map/scheduler over authoritative terrain; local invalidation follows terrain mutation | Initialize once from generated Colony data; keep excavation-driven invalidation unchanged |
| `AntRegionResidencyRegistry` | One revisioned, typed location per ant; local relocation cannot change region | Register the founding queen only after the generated world and spawn are valid |
| `NestEntranceTransitionService` | Capacity-one, idempotent, revision-guarded region transaction with rollback on failed presentation handoff | Receive generated typed endpoints while retaining the stable `EntranceId` and transaction semantics unchanged |
| `TwoViewWorldCoordinator` | Composes two loaded regions, cameras, local motors and rendering isolation | Select a fixture provider or generated-world provider; never own random generation rules or regenerate on view changes |
| `AntGameDevelopmentWorldHost` | Owns the authored Mara/Pip/Tala Colony development composition and current world services | Remain the regression fixture; do not turn it into the production new-game generator |
| excavation/material systems | `solid.soil` and `solid.stone` are excavatable and yield physical loose Soil/Stone drops; mutation notifies navigation | Generated solids use registered material IDs and the same removal, drop and invalidation paths |

The current `SampleWorldGenerator` is a deterministic bounded **development
fixture builder**, not the production procedural-generation contract. It creates
a `4×4` chunk (`128×128` cell) signed Colony course, authors test passages and
uses seed-based surface-height variation. Its useful lessons are explicit chunk
creation, stable material IDs and data-before-presentation, but its above-ground
Sky rows, test geometry and direct fixture edits are not the generated Colony
layout described here.

### Current material and resource limits

- Production-relevant Colony Background IDs include
  `background.earth.unprepared` and `background.stone.unprepared`.
- Production-relevant Solid IDs are `solid.soil` and `solid.stone`; both yield
  one existing physical material unit when excavated.
- Sand and Mud do not have registered production material definitions or yield
  contracts. They are future configuration options, not WV-B2B defaults.
- Existing food/water IDs and stores are development fixtures, not natural
  Surface source records. Stable natural-source IDs must be introduced in the
  resource slice that consumes this contract.

### Historical statements superseded by current authority

- “Founding queen starts on the Surface” and “no excavated chamber” are replaced
  by the approved underground chamber start in this specification.
- “World View runtime is not implemented” is historical before WV-B1; the
  bounded WV-B1 foundation exists, while procedural generation remains absent.
- “`64×64` Surface dimensions and `32×32` partition still await WV-B approval”
  is historical; WV-B1 fixed them for the bounded foundation and WV-B2A preserves
  them for the generated initial Surface.
- `SampleWorldGenerator` Sky/above-ground rows and large carved courses are
  development fixtures, not the generated underground Colony design.
- Development Food/Water storage fixtures are not evidence that natural Surface
  sources or their interactions exist.

## 3. Generation lifecycle and ownership

Generation is an explicit new-game operation:

1. Select `WorldGenerationConfiguration`, `WorldSeed` and `GenerationVersion`.
2. Derive independent named random domains.
3. Build Surface Background/node/feature candidates into temporary unified cells.
4. Build Colony Background/node data into temporary unified cells.
5. Apply protected entrance, passage, chamber and queen-spawn masks.
6. Place source records and other features without replacing terrain.
7. Validate the complete cross-region result.
8. Apply deterministic repairs allowed by this specification and revalidate.
9. Publish the immutable initial result to runtime owners, register the queen and
   construct region-specific presentation.

No partially generated region becomes live. On validation failure, creation
returns a structured diagnostic for the original seed/configuration/version; it
must not silently choose another seed.

After publication, authoritative runtime data is mutable. Excavation, depletion
and movement modify that in-memory instance. The original seed is provenance,
not a command to recompute current state. Switching views, rebuilding a
Tilemap/presentation or querying an unavailable area must not rerun generation.

## 4. Seed and deterministic random contract

### Authoritative inputs

The generation identity is the tuple:

`(worldSeed: signed 64-bit integer, generationVersion: positive integer, configurationId: stable string, configurationRevision: positive integer)`

The same tuple and the same ordered registry of referenced stable IDs must
produce byte-for-byte equivalent logical initial-world records on all supported
platforms. Frame count, Unity time, camera state, scene object order, dictionary
iteration, ant state and execution order are forbidden inputs.

### Recommended generator

Use a small project-owned integer generator with a frozen algorithm, such as
PCG-XSH-RR 32 seeded through SplitMix64. Do not rely on `UnityEngine.Random`, the
global random state or unspecified `System.Random` implementation behavior.
Golden-vector tests must freeze the first values for known seeds.

Derive sub-seeds with a documented stable 64-bit hash over the generation
identity plus an ASCII domain label. At minimum, reserve:

- `surface.terrain`
- `surface.water`
- `surface.resources`
- `surface.obstacles`
- `colony.materials`
- `colony.deposits`
- `entrance-and-start`
- `guaranteed-sources`

Surface and Colony output must therefore be independent of which is generated
first. Within a domain, either use a coordinate hash for each cell/feature or
consume a named stream in canonical order (`y`, then `x`, then stable feature
kind). Never enumerate an unordered collection to consume random values.

Generation-version changes intentionally permit different output from the same
world seed. Configuration-only balance changes increment the configuration
revision. A future save must retain the full generation identity and the
material/source registry versions, but permanent save/load is outside WV-B2B.

## 5. Region dimensions, coordinates and chunks

### Surface

- Initial dimensions are the approved WV-B1-compatible `64×64` cells.
- Valid `SurfaceCell` coordinates remain `(0,0)` through `(63,63)`.
- One Surface cell remains one logical movement cell. Presentation scale stays
  an adapter responsibility.
- Candidate chunk identity is `(regionId, chunkX, chunkY)` with `32×32` cells,
  producing initial chunks `(0,0)`, `(1,0)`, `(0,1)` and `(1,1)`.
- WV-B2B remains bounded. Coordinates outside the initial bounds are unavailable,
  not Grass, water or empty space.

### Colony

The approved initial Colony is `128×128`. The implemented development world
already proves four-by-four `32×32` Colony chunks and the current grid services
at that scale.

Do not give the two views an implicit shared coordinate system. Define generated
Colony-local coordinates as `(column, depth)`, with `column` and `depth` in
`0..127`, origin at the upper-left and depth increasing downward. A single
conversion adapter maps them to existing signed `ColonyCell`/world cells:

- `worldX = column - 64`
- `worldY = -1 - depth`

The initial signed bounds are therefore `x = -64..63` and `y = -128..-1`,
exactly chunks `x = -2..1`, `y = -4..-1`. The topmost underground boundary is
local depth `0` / world `y = -1`; there is no row above it in Colony data and no
generated Sky Background. `WorldTileCoordinates` owns signed world/chunk/local
conversion after this adapter has produced a world cell.

Future expansion adds explicitly generated neighbouring chunks using the same
generation identity and global Colony-local coordinates. It may not mutate
existing resident chunks or depend on generation order. Streaming, unloading
and offscreen simulation remain out of scope.

## 6. Surface generation

### Backgrounds, nodes and features

Every Surface cell has exactly one natural Background plus optional unified
occupants. Grass and connected Water areas are Background/terrain categories.
Stone deposits, seed/twig sources and natural obstacles are Solid/Resource Nodes
when they are removable or finite; a definition may be non-blocking. The nest
entrance is a specialised linked feature. Loose stacks and Constructions begin
absent. WV-B2B must define stable Surface Background/node IDs and presentation
mappings; Colony material definitions are not reused merely because names
overlap.

Illustrative, **unapproved balance** for candidate terrain cells outside
protected masks:

| Surface generation category | Provisional base weight |
| --- | ---: |
| Grass | 82 |
| Water | 8 |
| Stone node/deposit candidate | 6 |
| Natural obstacle node candidate | 4 |

These are weighted inputs, not promised final percentages. Protected entrance
and traversal cells override the draw. Validation checks that Grass is the
strict majority of non-protected Surface cells; a recommended initial threshold
is at least 65%, requiring balance approval.

### Spatial distribution

Independent random selection per cell is prohibited for the final terrain pass.
Use deterministic low-frequency value/simplex-style noise or coordinate-hashed
fields, then threshold and region-grow features:

- Grass is the base fill.
- Water starts from a small bounded number of seed cells and grows by
  deterministic frontier order into connected patches. Single-cell water is
  rejected unless specifically configured.
- Stone ground uses a second correlated field and bounded connected deposits.
- Natural obstacles use blue-noise-like minimum spacing plus small optional
  clusters, and are excluded from protected routes.
- A deterministic smoothing pass may remove isolated spikes, but its exact
  neighbour order and iteration count belong to the versioned configuration.

Surface node placement runs after Background generation. Scattered nodes use
coordinate hashes plus minimum spacing; clustered nodes choose deterministic
centres and offset tables. Twigs, seeds/food and Surface Stone are candidate node
types, but only approved stable definitions may be emitted. A freshwater source
node/record is associated with compatible connected Water Background; Water
Background itself is not a loose water item.

## 7. Underground generation

Create all `128×128` predominantly Soil Background cells and their initial
Solid/Resource Nodes before carving protected starting space. Most occupied
cells contain a Soil node; smaller connected deposits contain Stone nodes.
Background/node compatibility is definition-driven and does not require that
their display names match.

Illustrative, **unapproved balance** for non-protected underground cells:

| Solid/Resource Node | Provisional base weight | Depth tendency |
| --- | ---: | --- |
| Dirt via `solid.soil` | 88 | Highest near the entrance; remains the majority at every depth band |
| `solid.stone` | 12 | Increases gradually with depth, capped so Dirt remains predominant |

Generate deposits from deterministic seed points and bounded flood growth, not
independent speckle. Recommended initial deposit sizes are Soil base fill and
Stone clusters of 4–24 cells, with the first 12 depth rows capped at smaller
clusters. Deposit growth cannot cross protected entrance/chamber/passage masks.
Stone hardness, excavation duration and alternative yields are not changed by
generation.

Sand and Mud weights must be zero until their material definitions,
traversability, excavation compatibility, physical yields, art and tests are
approved. Generation validates every material through `WorldMaterialRegistry`.

There is no Colony sky, above-ground row or generated falling shaft. Exposed
cells are underground cells whose Solid has been removed while their permanent
natural Background remains. Existing clearance, grip, support and excavation
rules continue to determine movement after generation.

## 8. Entrance, chamber and queen

### Surface endpoint

Use `SurfaceCell(32,32)` as the provisional entrance feature location. The exact
visual can be a dark hole with a distinct rim/terrain transition, but art does
not affect terrain authority. Reserve a `5×5` clear-approach mask centred on the
entrance: no blocking obstacle, source or unrelated special feature may occupy
it. The entrance cell and typed approach/arrival cells must be explicitly stored
and traversable under `SurfacePathfinder` rules.

If terrain generation would put Water, blocking Stone or an obstacle in the
mask, the protected entrance terrain deterministically wins and is recorded as
a correction, not as a new seed. The entrance does not replace the cell's
underlying compatible terrain.

### Colony endpoint and starting chamber

Use the approximately central top boundary at generated Colony-local `(64,0)`
as the provisional exit feature. Carve a three-cell-wide vertical passage from
depth `0` through depth `3`, then one `7×5` clear rectangular chamber spanning
columns `61..67` and depths `4..8`. Keep natural Background tiles; remove only
Solid tiles in the protected carve mask. Rounded corners may later be approved,
but may not reduce validated clearance.

The Colony approach is inside the passage/chamber and the arrival is clear of
the boundary transfer cell. Place the founding queen at a deterministic clear
anchor near the chamber centre, provisionally local `(64,6)`, only after the
grid navigation validator confirms clearance and a route to the Colony
approach. No door, prepared Background or functional room is required.

The `7×5` chamber and three-cell passage width are approved generation settings.
The fixed rules are: exactly one starting chamber, minimum connection to the
top-boundary exit, sufficient whole-body clearance, a valid deterministic queen
spawn and no other initial excavated areas. Exact endpoint/spawn cells may move
only if implementation validation finds a genuine clearance incompatibility and
the change is documented.

The carve mask has higher precedence than material deposits and source
placement. Any extractable material removed by initial carving was never a
runtime resource and must not create loose drops. This avoids overwriting or
double-counting generated resources.

### Cross-region guarantees

- Exactly one definition uses `entrance.founding-nest.001`.
- It stores one typed Surface endpoint set and one typed Colony endpoint set;
  coordinates are not arithmetically converted between views.
- Surface and Colony approach/arrival cells are traversable and unoccupied.
- The entrance remains bidirectional and capacity one.
- No source or loose item blocks either endpoint.
- The queen can reach the Colony approach and, after transition, the Surface
  arrival can reach the guaranteed early-source access cells.

## 9. Generated Solid/Resource Node contract

Generation produces optional node instances over required Backgrounds:

| Field | Contract |
| --- | --- |
| `NodeId` | Stable ID derived from generation identity, region, node definition and canonical placement ordinal or coordinate; never a runtime instance ID |
| `RegionId` | Typed owning region |
| `Location` | Typed Surface or Colony-local location; no ambiguous `Vector2` |
| `NodeDefinitionId` | Registered stable node definition containing obstruction, activity, skill, work, output and depletion rules |
| `OutputResourceTypeId` | Registered stable physical output resource type, when extraction produces output |
| `InitialQuantity` | Positive configured quantity |
| `RemainingQuantity` | Initially equals `InitialQuantity`; mutable at runtime |
| `DiscoveryState` | Initial explicit state; guarantee sources may be known or readily discoverable according to later WV-C rules |
| `DepletionState` | Initially not depleted; becomes derived/updated when remaining reaches zero |
| provenance | World seed, generation version, configuration ID/revision and generator domain |

Record categories and cell slots remain distinct:

1. Background is always present and supplies natural environment metadata;
2. the optional node supplies finite quantity, obstruction and extraction rules;
3. the optional Construction supplies player-created function/capacity;
4. the optional loose stack is runtime inventory-independent physical output;
5. the entrance is a specialised linked world feature.

WV-B2B creates Backgrounds and node records required for generation validation,
with Construction/loose slots empty. It does not implement discovery,
consumption, extraction, hauling, foraging, construction interaction or AI.

### Starting guarantees

- Reserve deterministic candidate rings outside the entrance `5×5` mask and
  inside a provisional path-cost budget of 24 orthogonal-equivalent Surface
  steps.
- Place at least one finite food source and one finite freshwater access/source
  record on compatible, distinct cells. Their exact types, quantities,
  discovery defaults and path budget require WV-C balance approval.
- Validate access from the Surface arrival without crossing Water or obstacles.
- Keep sufficient nearby Soil/Dirt around the starting chamber for excavation;
  do not reserve the whole near-start area as Stone.
- Basic food/water access and natural rest cannot require constructed storage,
  a nursery or prior hauling, preventing a bootstrap circular dependency.

If weighted placement does not satisfy a guarantee, search compatible cells in
a canonical expanding ring ordered by path cost, then `y`, then `x`. Place the
guaranteed source at the first valid cell and record the correction. Never reroll
the world seed.

## 10. Validation and deterministic correction

Validation runs on temporary data before publication and returns stable error
codes with affected IDs/cells.

### Required checks

- exactly one entrance ID and exactly one endpoint set per region;
- endpoints match their typed region, are in bounds, clear and not occupied by
  an incompatible resource or Solid;
- protected Surface approach and Colony passage/chamber masks are intact;
- exactly one connected starting chamber exists and no unrelated Solid removal
  created another initial cavern;
- queen spawn is in bounds, clear, locally stable and connected to the Colony
  approach under existing navigation/clearance rules;
- required Surface arrival, food and freshwater access cells share a reachable
  component;
- all terrain, Background, Solid and resource IDs resolve in their registries;
- Surface sources are compatible with underlying terrain;
- underground Background/Solid pairings and yields are valid;
- every placement belongs to exactly one expected chunk/cell and no record is
  out of bounds or duplicated;
- Grass and Dirt predominance thresholds are met;
- all generated IDs are unique and stable when regeneration is repeated.

### Correction policy

Protected structural masks are applied deterministically before validation.
Allowed repairs are limited to:

- clearing an incompatible obstacle/source from a protected entrance cell;
- restoring the specified passage/chamber carve mask;
- relocating a guarantee source with the canonical search above;
- replacing an invalid non-protected material with the configured base material
  only when the invalidity is an explicitly supported missing-weight fallback.

Repairs must be deterministic, bounded and reported. Duplicate entrances,
invalid configuration/IDs, impossible bounds, failure to find required sources,
unreachable required topology after allowed repairs or an invalid queen spawn
reject the generated result. Rejection preserves the requested seed and gives a
diagnostic; it never retries with another seed.

## 11. Runtime separation

Keep these responsibilities separate:

- `WorldGenerationConfiguration`: dimensions, weights, thresholds, cluster
  rules, protected geometry and stable IDs;
- deterministic generators: pure initial-data construction from generation
  identity and registries;
- generated region data: terrain/material cells, features, source records,
  provenance and validation report;
- runtime terrain/source mutation: existing Colony removal/invalidation and
  future Surface/source mutation authorities;
- ant identity/residency: `AntRegistry` and `AntRegionResidencyRegistry`;
- local navigation: existing region-specific services;
- rendering: region-specific projections only;
- UI and clock: existing owners, never a generator dependency.

`TwoViewWorldCoordinator` may select and bind a completed scenario, but the
generators must not own the simulation clock, ant registry, HUD, camera or input.
The WV-B1-DEF-002 isolation remains mandatory: each camera renders only its own
world presentation and the shared HUD; inactive-region overlays and labels stay
hidden. Rebuilding presentation reads current authoritative data and cannot
reset terrain, source quantities or ant state.

## 12. Fixture preservation and scenario selection

Introduce an explicit scenario/provider boundary in WV-B2B rather than changing
the current fixture in place:

- `DevelopmentFixture`: current deterministic Surface obstacles, current
  Colony course, Mara/Pip/Tala, authored survival/storage/job fixtures and their
  existing expectations.
- `FoundingColonyGenerated`: generation identity, generated Surface and Colony,
  one founding queen, one entrance and no constructed infrastructure.

The scenario is selected before world publication. Tests name the scenario they
require. No test may infer production generation merely from a nonzero
`ChunkedWorldData.WorldSeed`. The historical 33/33 WV-B1 manual record remains
evidence for that accepted run, not evidence for WV-B2B. At this specification
gate `WV-B1-REG-001` remained open; it was later closed by the 15 October current
working-tree 551/551 graphics-enabled Edit Mode rerun.

## 13. Future WV-B2B test and acceptance strategy

### Edit Mode

- every resident Surface and Colony cell always has one Background;
- node removal/depletion preserves that Background;
- node quantity reduction, depletion and deterministic identity are exact;
- shared extraction commits node reduction and one physical output exactly once,
  including interruption/retry cases;
- one loose resource type/stack occupies a cell; same-type capacity merges and
  different-type/full-stack output follows the approved fallback/reject policy;
- Construction designation/commit rejects a node, existing placement or
  incompatible loose stack and preserves the Background;
- Construction consumes exactly its staged materials and retains skill XP,
  progress and recoverability behavior;
- Storage contents are placement-owned, one type at a time and conserved across
  deposit, withdrawal, cancellation and placement migration;
- equivalent logical occupancy invariants hold through Surface and Colony
  adapters without merging their coordinate/pathfinding types;
- golden PRNG vectors and stable sub-seed derivation;
- same identity produces identical Surface cells, Colony cells, source records,
  feature IDs and validation report;
- Surface-first and Colony-first generation are identical;
- different seeds vary meaningful non-protected terrain while preserving all
  guarantees;
- generated IDs are stable and entrances never duplicate;
- all configured material/resource IDs validate;
- Grass/Dirt predominance and configured weight tolerances over a fixed seed
  corpus;
- Water and underground deposits obey connectivity/size/depth rules;
- resources obey terrain compatibility and protected masks;
- entrance locations, chamber geometry, queen spawn and starting-resource paths
  satisfy the contract;
- correction order is deterministic and rejected worlds retain the same seed;
- signed Colony-local/world/chunk round trips cover boundaries and negatives;
- existing WV-B1, Colony navigation, excavation/drop and fixture tests remain
  unchanged and pass.

### Play Mode

- bind each generated region once and initialize existing local navigators;
- founding queen spawns clear in the chamber and can reach the entrance;
- bidirectional transition preserves identity, needs, inventory and residency;
- camera switches do not regenerate either region;
- excavation of generated Soil/Stone creates existing physical drops, updates
  navigation and persists across camera switches;
- Surface navigation reaches both guaranteed source access cells;
- simulation continues in the hidden region and rendering stays isolated;
- the development fixture still loads Mara/Pip/Tala and its job/survival data.

### Manual acceptance

For at least three named seeds, inspect overall variation, Grass/Dirt dominance,
connected Water, clustered Stone, clear entrance presentation, chamber scale and
queen clearance. Complete a queen chamber-to-Surface-and-back traversal, excavate
one generated Soil and one Stone cell, switch views repeatedly and confirm no
map reset or cross-region presentation leakage. Also rerun the applicable WV-B1
round-trip, pause, blocked-arrival, occupancy and camera checks.

Automated success, Unity compilation and manual Play Mode acceptance must be
reported separately. This WV-B2A documentation work executes none of those
future Unity tests.

## 14. Remaining approvals

The approved vision does not require reconsideration. WV-B2B still needs explicit
approval of these implementation details:

1. Stable Surface Background/node IDs and presentation, plus whether “Soil”
   formally maps to
   existing `solid.soil` or receives a renamed/new definition.
2. Final Background/node weight balance, Grass/Soil minimum thresholds, noise
   frequency, cluster sizes and depth curve.
3. Natural node stable IDs, quantities, skills/work, discovery defaults,
   compatible Backgrounds and early path-cost budget.
4. Whether the entrance uses Grass Background or a dedicated compatible
   entrance Background category, plus final art.
5. Generation/configuration version numbering and the frozen integer PRNG/hash
   implementation.
6. Loose-stack capacity and deterministic alternate-cell policy beyond the
   approved one-type-per-cell rule.
7. Exact placement obstruction/compatibility exceptions and migration timing
   for legacy functional Backgrounds.

Sand, Mud, extra resource types, persistence, streaming, unloading, additional
entrances and balance outside the initial guarantee remain later decisions.

## 15. Smallest recommended WV-B2B implementation

Implement one pure, scene-independent unified-cell builder and validation result
for the approved bounded `64×64` Surface plus `128×128` Colony. Generate required
Backgrounds and optional nodes only; placement and loose slots start empty. Use
Grass/Water Surface Backgrounds, compatible Stone/obstacle/source nodes, existing
Soil/Stone Colony compatibility definitions, the approved `7×5` chamber and
three-cell passage, one queen spawn and guaranteed food/freshwater nodes. Add an
explicit scenario selector and separate region adapters, leaving the development
fixture untouched.

Stop before consumption, extraction, hauling, foraging AI, persistence,
streaming or autonomous cross-view routing. That is the smallest slice that can
prove deterministic generation without absorbing WV-C1 or later work.
