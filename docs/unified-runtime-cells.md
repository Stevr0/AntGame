# Unified Runtime Cells

## Status

WV-B3A is a complete documentation gate. The architecture below is approved;
the runtime migration is not started. WV-B3B, WV-B3C, WV-B3D and WV-C1 require
separate approval. The authoritative detail is
[`Documentation/UnifiedRuntimeCellSpecification.md`](https://github.com/Stevr0/AntGame/blob/main/Documentation/UnifiedRuntimeCellSpecification.md).

## Cell model

Every resident Colony or Surface cell has exactly one natural Background and
may contain zero or one Node, Placement and one loose ground-resource stack.
The layers are logical data, not a requirement for separate Tilemaps.

| Layer | Owns | Key invariant |
| --- | --- | --- |
| Background | Natural terrain/environment | Always present; survives extraction and construction |
| Node | Finite natural resource and obstruction | Stable ID and exact-once quantity |
| Placement | Built/authored function | Preserves Background; at most one per cell |
| Loose stack | Recoverable ground resource | One type and one stack per cell |

Storage contents belong to a Storage Placement. Construction staging belongs to
the Construction Site. Neither is an independent ground stack. One quantity can
have only one authoritative owner.

## Authority boundary

The recommended design keeps `ChunkedWorldData` as the Colony backing store
behind a shared read contract and adds a Surface store behind the same contract.
A region registry routes typed `ColonyCell` and `SurfaceCell` requests. A central
mutation service validates compatibility, expected revisions, reservations and
capacity before one atomic commit.

Generated snapshots initialise runtime regions and then become immutable
provenance. Navigation maps, Tilemaps, colliders, temporary Surface visuals, HUD
and inspectors are projections. There must never be two mutable copies of Node
quantity, Placement occupancy or a ground stack.

## Preserved gameplay flow

`Node → ground stack → ant inventory → site staging / Storage → consumption`

- Extraction prepares physical output before reducing the Node.
- Hauling owns pickup and delivery.
- Construction begins only when all site requirements are staged.
- Completion consumes staging once and creates a Placement.
- Failed output placement retains a recoverable pending transaction; it never
  discards output or auto-fills inventory.

Current `Excavation*` APIs, quantity-one Colony Solids, functional Backgrounds,
drop registry, Storage world and construction sites are compatibility seams.
They remain active until their state is transferred and the replacement is
proven.

## Region boundaries

Colony and Surface share occupancy vocabulary, not geometry. Colony retains
signed chunks, side-on support/grip/gravity and grid navigation. Surface retains
its bounded top-down cells, obstacles, diagonal A* and separate motor/camera.
The nest entrance, residency revision and shared clock are unchanged.

Future Fog of War stores knowledge separately from physical cells. Future
Refining uses Placement-owned input staging and physical output, and remains
distinct from Construction installing a manufactured product.

## Generation publication

Version `wv-b2b.default.002` follows this target sequence:

1. Generate immutable initial data.
2. Validate the complete world.
3. Build private authoritative runtime regions.
4. Publish both regions atomically.
5. Initialise navigation/rendering projections.
6. Initialise ants, entrance and residency.
7. Begin shared simulation.

View switching never regenerates. Runtime mutations are never overwritten from
the initial snapshot.

## Candidate migration slices

1. **WV-B3B — authority foundation:** typed read contracts, revisions, central
   compatibility, atomic publication and read-only adapters.
2. **WV-B3C — Nodes and loose resources:** mutable Nodes, one-stack occupancy,
   failed-output recovery and narrow transaction adapters.
3. **WV-B3D — Placements:** site/Placement occupancy and one-function-at-a-time
   migration of functional Backgrounds, beginning with Material Storage.

These are candidate scopes, not implementation authorisation. Each slice keeps
its previous adapter until focused/full automation and its Unity manual gate
pass. Legacy classes are removed only after source, serialized GUID, reflection,
fixture and save-compatibility proof.
