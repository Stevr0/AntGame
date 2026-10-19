# Isometric World View Runtime Presentation

**Status:** ISO-WV-2 — Implemented, Pending Manual Acceptance

The optional isometric World View now presents the complete unchanged 64 x 64
logical Surface. `SurfaceIsometricRenderer` reads all 4,096 authoritative cells
through the runtime adapter into one chunk-mode Tilemap; it does not create a
second grid or one GameObject per cell. Legacy square rendering remains the
default fallback.

## Presentation contract

- Grid cell size: `(1,0.5,1)`.
- Artwork: 32 x 32 canvas, 32 PPU, pivot `(0.5,0.25)`.
- Grass: `tile_040_GrassMain.png`.
- Water: `tile_104_flatWater.png`.
- Unsupported Background: `tile_009.png` with magenta tint.
- Resource Nodes are not terrain and remain visually deferred.

`SurfaceIsometricProjection` owns cell centres, continuous ant positions,
bounded mouse inverse conversion and projected map bounds. The coordinator uses
that one authority for terrain, ants, input, destination/entrance placeholders
and camera framing.

## Camera, ants and entrance

The Surface camera is orthographic and unrotated. Isometric Home and pan limits
come from projected map bounds, including viewport extents; Legacy and
isometric Surface camera states are remembered independently. Colony camera and
layer-9 culling remain isolated.

`SurfaceNavigationMotor` preserves its logical A* route and state while
projecting continuous movement onto the existing ant root. Selection collider,
indicator, identity, needs, skills and inventory therefore stay attached to the
same authoritative object. Pointer commands resolve to `SurfaceCell` before the
unchanged traversability/path checks. Dynamic sprites sort from projected
ground-contact Y.

The existing capacity-one entrance transaction remains authoritative. Region
activation switches the existing motor/presentation layer and creates no
duplicate ant renderer.

## Acceptance checklist

1. Open `AntGameDevelopment`, enter Play Mode, start generated seed `2203` and
   switch to **World View**.
2. Confirm Legacy is initially active, then choose **Isometric 64x64 World**.
3. Confirm the full terrain diamond, correct Grass/Water mapping and no absent
   cells or unexpected magenta fallback.
4. Press Home; pan and zoom to all bounds. Confirm the viewport remains over
   the projected world and Colony framing is independent.
5. Toggle modes after changing each camera view and confirm each state returns.
6. Bring the queen through the entrance. Confirm one projected ant/indicator,
   left-click selection, aligned right-click destination markers and smooth
   movement to valid centre/edge/corner cells.
7. Confirm Water, blocked and outside-world clicks are rejected.
8. Toggle renderers during an order; confirm cell, path and identity persist.
9. Complete and reuse the entrance in both directions; confirm no ghost visual.
10. Repeatedly switch Colony/World and confirm no presentation/input leakage.
11. Restore the fixture and confirm deterministic rebuilding.
12. Confirm the Console remains free of errors and exceptions.

Automated coverage checks 4,096-cell determinism, authority immutability,
projection/inverse boundaries, projected motion, camera bounds/Home, view
isolation, Legacy restoration and entrance reuse. Manual acceptance is still
required. Focused Edit/Play passes **41/41** and **7/7**; complete Play passes
**86/86**. Complete Edit passes **634/638** with four unrelated tests retaining
removed `Art/WorldTiles` paths instead of current `Art/ColonyView` paths.

## Deferred

ISO-WV-3 remains separately gated: Resource Nodes, loose stacks, Placements,
Storage, Construction stages, final World ant animations and entrance art.
Fog/pheromone overlays and shoreline autotiling also remain outside ISO-WV-2.
