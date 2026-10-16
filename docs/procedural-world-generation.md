# Procedural World Generation

## Resource and Node v1.0 generation amendment

Every target Solid is a finite Resource Node with a specific definition and
positive actual quantity. Future generation selects variable quantity through a
named, versioned seed domain containing generation identity, definition and
stable Node location/identity; iteration order cannot affect it. Water is finite
and a Solid Water Node follows whole-node depletion. Current version-2 Colony
Soil/Stone quantity-one records and generation-only Surface sources remain
unchanged compatibility behavior. See [Resource and Node System](resource-and-node-system.md).

## WV-B3B publication boundary

The validated Colony and Surface outputs are now exposed together through the
unified read registry. Publication is an atomic read-model swap; a rejected
adapter pair leaves the previous pair visible. Generated data is immutable
provenance after initialisation, and generated Surface nodes are explicitly
generation-only. Views consume the published Surface authority and never
regenerate or republish the seed.

## WV-B3A publication boundary

Generated data becomes immutable initial/provenance data after validation.
Future runtime publication privately creates both region authorities, publishes
them atomically, then initialises projections and ants/residency before starting
simulation. Camera switching never regenerates and runtime mutations are never
read back from the initial snapshot. See [Unified Runtime
Cells](unified-runtime-cells.md).

## Status

**WV-B2B complete; manual acceptance confirmed (15 October 2026).** The
authoritative contract is
`Documentation/ProceduralWorldGenerationSpecification.md`. This page is the
wiki summary, not a second source of truth.

The accepted seed-`2203` run demonstrated deterministic terrain, the founding
queen, Colony Extraction, loose output, separate Hauling, staged and completed
Construction, first Storage, later Storage delivery, expansion, view switching
and entrance use. Surface Seed/Freshwater survival interaction remains WV-C1.

Version `2` / `wv-b2b.default.002` uses one signed 64-bit seed, domain-derived
`surface` and `colony` SplitMix64 streams and a validate-before-publish result.
Version 2 also guarantees a quantity-one Stone foreground node at Colony
`(-4,-8)`, immediately west of the starting chamber. Stone-coloured natural
Background in a cleared cell is not itself an excavation target.
Use the top development toolbar's **Seed**, **Start Generated**, and **Restore
Fixture** controls. Generated source markers are data/visuals only; WV-C1
interactions are not present.

## Accepted manual scenario record

The user confirmed the combined scenario outcomes above. The detailed checklist
below is retained as the original acceptance procedure; unreported individual
observations are not invented.

1. Open `AntGameDevelopment`; confirm the default fixture still has selectable
   Mara, Pip and Tala.
2. Enter a known signed 64-bit value in **Seed**, then choose **Start Generated**.
   Confirm the status reports successful validation and exactly one founding
   queen appears.
3. In World View, confirm mostly Grass, connected Water, Stone/obstacle/source
   variation, a dark central nest hole and an open `5×5` clearing.
4. In Colony View, confirm the top-centred exit, three-wide passage, exact `7×5`
   chamber, Soil majority and clustered Stone. Move the queen in the chamber.
5. Resume the clock, send the queen through the entrance and back, then clear
   each arrival. Confirm identity, needs and state persist and the entrance is
   reusable.
6. Excavate an accessible generated Solid, switch views twice and confirm the
   cell remains excavated. Confirm camera poses/zoom remain independent and no
   renderers leak between regions.
7. Start the same seed again and compare the initial layout; then start a
   different seed and confirm meaningful variation.
8. Choose **Restore Fixture**, confirm Mara/Pip/Tala return, and verify the Unity
   Console has no new errors.

The user accepted the combined scenario outcomes recorded above; this retained
checklist is historical procedure rather than an unpassed current gate.

## Approved world shape

- One seed deterministically produces a top-down Surface and side-on underground
  Colony.
- The initial Surface remains `64×64`, predominantly Grass, with an entrance near
  `(32,32)`.
- The approved initial Colony is `128×128`, predominantly existing
  `solid.soil`, with Stone deposits and no Sky/above-ground row.
- The Colony exit is on the topmost underground boundary. One minimum passage
  leads to exactly one small starting chamber containing the founding queen.
- Required Backgrounds, optional nodes, optional placements, optional loose
  stacks and the entrance remain separate authorities.

The `7×5` chamber and three-cell passage are approved. Surface weights
82/8/6/4 and Colony node weights 88/12 remain provisional balance.

## Unified cell model

Every generated cell has one natural Background and optional node, placement and
loose-stack slots. Surface and Colony share these meanings while retaining typed
coordinates and separate navigation. Grass/Water are Surface Backgrounds;
Stone/obstacles/finite sources are compatible nodes where removable or finite.
Underground begins with predominantly Soil Background plus Soil/Stone nodes in
most cells. Chamber/passage carving removes nodes only and keeps Backgrounds.

WV-B2B generates Backgrounds/nodes with placement and loose slots empty. Colony
Soil/Stone nodes project into the existing excavation system; WV-B2B adds no new
Surface extraction or source-consumption system. Generated nodes never emit
loose resources merely by existing.

## Determinism contract

Generation is identified by world seed, generation version, configuration ID
and configuration revision. A frozen project-owned integer PRNG and stable hash
derive named Surface, Colony, entrance and guarantee domains. Generation order,
frame rate, camera state, dictionary order and ant movement cannot affect output.

Generation builds temporary data, applies protected entrance/chamber masks,
places node records, validates the complete result and only then publishes it.
Validation never rerolls a failed seed. Once published, excavation and depletion
mutate the in-memory world; switching views never regenerates it.

## Existing integration seams

`ChunkedWorldData`, `WorldTileCoordinates`, the material registry, shared Colony
navigation and excavation remain Colony authorities. `SurfaceWorldData` and
`SurfacePathfinder` remain separate Surface authorities. Typed region locations,
`AntRegionResidencyRegistry`, `entrance.founding-nest.001` and the WV-B1
capacity-one transaction are preserved. `TwoViewWorldCoordinator` binds a
completed scenario but does not own random generation, simulation or persistence.

The current `SampleWorldGenerator` remains a deterministic development fixture
builder. The Mara/Pip/Tala scenario and its authored survival, storage and job
fixtures stay available separately from the generated founding-colony scenario.

## Terrain and sources

Provisional, unapproved example weights are 82 Grass Background / 8 Water
Background / 6 Stone-node / 4 obstacle-node candidates on the Surface and 88
Soil-node / 12 Stone-node underground. Correlated
noise, deterministic region growth and bounded clusters avoid uniform speckle.
Sand and Mud remain disabled because the current project has no supported
production definitions or yield contracts for them.

At least one finite food node and freshwater access/source node must be reachable
near the Surface entrance without constructed infrastructure. Sources are
nodes on compatible Backgrounds, not replacement Backgrounds or loose drops. Exact
types, quantities and balance belong to a later approved resource slice.

## Validation gate

Before runtime publication, validate unique linked endpoints, protected clear
approaches, one connected starting chamber, a clear/reachable queen spawn,
reachable early sources, registered IDs, compatible terrain/source placement,
valid chunk ownership, bounds and Grass/Dirt predominance. Deterministic bounded
repairs may restore protected masks or relocate guarantee sources; structural or
configuration failures reject the original seed with diagnostics.

## WV-B2B boundary

The smallest future implementation is a pure bounded unified-cell
builder/validator plus an explicit scenario selector and separate region
adapters. It generates Background and node slots only. It does
not include source consumption, extraction, hauling, foraging, persistence,
streaming, unloaded simulation, extra entrances or autonomous cross-view routes.

See also [World View Foundation](world-view-foundation.md), [Technical
Architecture](technical-architecture.md), [Excavation and
Terrain](excavation-terrain.md), and [Development Roadmap](development-roadmap.md).
