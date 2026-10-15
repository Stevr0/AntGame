# Procedural World Generation Specification

## Status and authority

> **WV-B2A — APPROVED DESIGN, IMPLEMENTATION DEFERRED (15 October 2026).**
> This document is the implementation-ready contract for a future WV-B2B
> seed-based generator. It does not describe implemented procedural generation
> and does not authorise changes to Unity scripts, scenes, prefabs,
> ScriptableObjects or tests.

This specification supersedes older new-game statements that place the founding
queen on the Surface or begin with no excavated Colony space. A new generated
game begins with one founding queen in one small underground starting chamber.
It does not replace the existing Mara/Pip/Tala development fixture or change the
accepted WV-A/WV-B1 entrance transaction.

## 1. Approved product rules

- World View and Colony View are separate full-screen regions. World View is
  top-down; Colony View is side-on and entirely underground.
- One authoritative world seed deterministically produces both initial regions.
- Surface terrain is predominantly Grass. Underground foreground terrain is
  predominantly Dirt, represented initially by the existing `solid.soil`
  definition unless a separate Dirt definition is later approved.
- The Surface entrance is approximately central. The Colony exit occupies the
  approximately central cell of the topmost underground boundary.
- Exactly one small chamber and its minimum connecting passage begin excavated.
  The founding queen starts at a valid location in that chamber.
- The rest of the initial Colony is solid and is expanded through existing
  excavation mechanics.
- Terrain, resource sources, loose resources and the entrance are different
  kinds of records. A resource does not replace its underlying terrain.
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
3. Build Surface terrain and feature candidates into temporary data.
4. Build Colony Background/Solid data into temporary data.
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

Use an initial `128×128` underground region because the implemented development
world already proves four-by-four `32×32` Colony chunks and the current grid
services at that scale. This is a technical recommendation requiring approval
before WV-B2B implementation.

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

### Terrain versus features

Every Surface cell has exactly one terrain classification. Resources and the
nest entrance are separate records placed on compatible cells. Initial semantic
terrain categories are Grass, Water, Stone ground and Natural Obstacle. WV-B2B
must define stable Surface terrain IDs and presentation mappings; the existing
Colony material definitions must not be reused merely because names overlap.

Illustrative, **unapproved balance** for candidate terrain cells outside
protected masks:

| Surface terrain | Provisional base weight |
| --- | ---: |
| Grass | 82 |
| Water | 8 |
| Stone ground | 6 |
| Natural obstacle | 4 |

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

Surface source placement runs after terrain. Scattered items use coordinate
hashes plus minimum spacing; clustered sources choose deterministic centres and
offset tables. Twigs, seeds/food and Surface stone are candidate source types,
but only approved stable resource definitions may be emitted. A freshwater
source is associated with a Water feature record; Water terrain itself is not a
loose water item.

## 7. Underground generation

Create all `128×128` Background cells and all initial Solid cells before carving
protected starting space. Background and Solid IDs should correspond (Earth
Background with Soil Solid; Stone Background with Stone Solid) unless a later
material rule explicitly allows another pairing.

Illustrative, **unapproved balance** for non-protected underground cells:

| Solid material | Provisional base weight | Depth tendency |
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

The exact `7×5` chamber, three-cell passage width and endpoint/spawn cells are
technical recommendations that require final WV-B2B approval. The fixed rules
are: exactly one starting chamber, minimum connection to the top-boundary exit,
sufficient whole-body clearance, a valid queen spawn and no other initial
excavated areas.

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

## 9. Generated resource-source contract

Generation produces source records separately from terrain:

| Field | Contract |
| --- | --- |
| `SourceId` | Stable ID derived from generation identity, region, source kind and canonical placement ordinal or coordinate; never a runtime instance ID |
| `RegionId` | Typed owning region |
| `Location` | Typed Surface or Colony-local location; no ambiguous `Vector2` |
| `ResourceTypeId` | Registered stable resource/source type |
| `InitialQuantity` | Positive configured quantity |
| `RemainingQuantity` | Initially equals `InitialQuantity`; mutable at runtime |
| `DiscoveryState` | Initial explicit state; guarantee sources may be known or readily discoverable according to later WV-C rules |
| `DepletionState` | Initially not depleted; becomes derived/updated when remaining reaches zero |
| provenance | World seed, generation version, configuration ID/revision and generator domain |

Record categories remain distinct:

1. terrain material determines Surface traversal/presentation;
2. underground Solid material determines blockage, excavation and existing yield;
3. a Surface source is finite natural availability at a location;
4. a loose physical resource is runtime inventory-independent output with its
   own ownership/conservation rules;
5. the entrance is a specialised linked world feature.

WV-B2B may create the records required for validation, but it does not implement
discovery, consumption, extraction, hauling, foraging or AI.

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
evidence for that accepted run, not evidence for WV-B2B. `WV-B1-REG-001` remains
open until complete execution evidence exists; WV-B2B must not relabel it.

## 13. Future WV-B2B test and acceptance strategy

### Edit Mode

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

1. `128×128` initial Colony dimensions and the local-depth-to-signed-cell mapping.
2. Exact `7×5` chamber, three-cell passage, endpoint and queen-spawn cells.
3. Stable Surface terrain IDs/presentation and whether “Dirt” formally maps to
   existing `solid.soil` or receives a renamed/new definition.
4. Final terrain/material weights, Grass/Dirt minimum thresholds, noise
   frequency, cluster sizes and depth curve.
5. Natural source stable IDs, quantities, discovery defaults, compatible
   terrain and early path-cost budget.
6. Whether the entrance clears underlying terrain to Grass or uses a dedicated
   compatible entrance terrain category, plus final art.
7. Generation/configuration version numbering and the frozen integer PRNG/hash
   implementation.

Sand, Mud, extra resource types, persistence, streaming, unloading, additional
entrances and balance outside the initial guarantee remain later decisions.

## 15. Smallest recommended WV-B2B implementation

After approval, implement one pure, scene-independent generated-world builder
and validation result for a bounded `64×64` Surface plus bounded `128×128`
Colony. Use only Grass/Water/Stone/obstacle semantic Surface terrain, existing
Soil/Stone Colony definitions, one protected entrance/chamber, one queen spawn
and source **records** for guaranteed food/freshwater. Add an explicit scenario
selector that leaves the development fixture untouched, then bind the completed
result through existing world, residency, navigation and coordinator seams.

Stop before consumption, extraction, hauling, foraging AI, persistence,
streaming or autonomous cross-view routing. That is the smallest slice that can
prove deterministic generation without absorbing WV-C1 or later work.
