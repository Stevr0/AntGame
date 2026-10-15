# Procedural World Generation

## Status

**WV-B2A specification complete; runtime implementation not started (15 October
2026).** The authoritative contract is
`Documentation/ProceduralWorldGenerationSpecification.md`. This page is the
wiki summary, not a second source of truth.

## Approved world shape

- One seed deterministically produces a top-down Surface and side-on underground
  Colony.
- The initial Surface remains `64×64`, predominantly Grass, with an entrance near
  `(32,32)`.
- The recommended initial Colony is `128×128`, predominantly existing
  `solid.soil`, with Stone deposits and no Sky/above-ground row.
- The Colony exit is on the topmost underground boundary. One minimum passage
  leads to exactly one small starting chamber containing the founding queen.
- Terrain, finite source records, loose physical items and the entrance remain
  separate authorities.

The `128×128` Colony, `7×5` chamber, three-cell passage and exact generation
weights are implementation recommendations awaiting WV-B2B approval.

## Determinism contract

Generation is identified by world seed, generation version, configuration ID
and configuration revision. A frozen project-owned integer PRNG and stable hash
derive named Surface, Colony, entrance and guarantee domains. Generation order,
frame rate, camera state, dictionary order and ant movement cannot affect output.

Generation builds temporary data, applies protected entrance/chamber masks,
places source records, validates the complete result and only then publishes it.
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

Provisional, unapproved example weights are 82 Grass / 8 Water / 6 Stone ground
/ 4 obstacle on the Surface and 88 Soil / 12 Stone underground. Correlated
noise, deterministic region growth and bounded clusters avoid uniform speckle.
Sand and Mud remain disabled because the current project has no supported
production definitions or yield contracts for them.

At least one finite food source and freshwater access/source must be reachable
near the Surface entrance without constructed infrastructure. Sources are
records on compatible terrain, not replacement terrain or loose drops. Exact
types, quantities and balance belong to a later approved resource slice.

## Validation gate

Before runtime publication, validate unique linked endpoints, protected clear
approaches, one connected starting chamber, a clear/reachable queen spawn,
reachable early sources, registered IDs, compatible terrain/source placement,
valid chunk ownership, bounds and Grass/Dirt predominance. Deterministic bounded
repairs may restore protected masks or relocate guarantee sources; structural or
configuration failures reject the original seed with diagnostics.

## WV-B2B boundary

The smallest future implementation is a pure bounded builder/validator plus an
explicit scenario selector and adapters into existing world authorities. It does
not include source consumption, extraction, hauling, foraging, persistence,
streaming, unloaded simulation, extra entrances or autonomous cross-view routes.

See also [World View Foundation](world-view-foundation.md), [Technical
Architecture](technical-architecture.md), [Excavation and
Terrain](excavation-terrain.md), and [Development Roadmap](development-roadmap.md).
