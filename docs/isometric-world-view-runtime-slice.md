# ISO-WV-1 Runtime Isometric Terrain Slice

**Status:** ISO-WV-1 manually accepted; ISO-WV-1B spacing experiment pending manual acceptance
**Date:** 19 October 2026

ISO-WV-1 adds a presentation-only 8 x 8 isometric Tilemap to the live World
View. It reads logical cells `(29,29)` through `(36,36)` from the published
`SurfaceRuntimeCellRegionAdapter`; the existing 64 x 64 Surface, A*, motors,
orders, resources, jobs and generation remain authoritative and unchanged.

## Renderer and projection

`SurfaceIsometricRenderer` owns one Unity Isometric `Grid` and one background
`Tilemap`, avoiding one GameObject per cell. Logical cells map to tilemap cells
as `(-logicalY,-logicalX,0)`. With the accepted 1 x 0.5-unit Grid cell, 32 PPU
sprites and `(0.5,0.25)` pivot, Unity's cell positions match
`SurfaceIsometricProjection`: logical X goes down-right and logical Y goes
down-left. The default remains the accepted 100% spacing.

## ISO-WV-1B spacing controls

The runtime `SurfaceIsometricRenderer` Inspector exposes **Horizontal Spacing
Percent** and **Vertical Spacing Percent**, each ranged from 90% to 100% under
**ISO-WV-1B Tile Spacing Experiment**. They adjust only the isometric Grid cell
size and keep the preview centre anchored; the accepted projection and logical
Surface do not change. The development panel provides uniform **100%**, **97%**,
**95%**, **93%** and **90%** presets. Legacy Surface does not reset spacing.

All 64 snapshots are read before the Tilemap changes. A missing/out-of-bounds
cell rejects the complete update; rendering never writes runtime cell state.
Resource Nodes remain separate and are not rendered as terrain.

## Development toggle

The World View development panel provides **Legacy Surface** and **Isometric
8x8 Preview**. Legacy remains the default. The toggle changes presentation
roots and Surface orthographic framing only. Layer-9 `SurfaceWorld` culling
continues to isolate the Colony camera.

Because ant projection is ISO-WV-2 work, Surface-resident ant renderers are
hidden in the isometric preview instead of being shown at incorrect square-grid
positions. Ant roots, simulation and movement are unchanged, and the renderer
state is restored in Legacy mode.

## Visual mapping

| Runtime Background ID | Isometric sprite |
| --- | --- |
| `background.surface.grass` | `Terrain/tile_040.png` |
| `background.surface.water` | `Terrain/tile_104.png` |
| Unsupported ID | `tile_009` tinted bright magenta |

The presentation-only `SurfaceIsometricVisualCatalog` stores these authored
references. No water, rock, vegetation or resource artwork is silently used for
an incompatible role.

## Validation and limitations

The ISO-WV-1B combined Edit Mode regression passes **29/29**. The full
World View foundation Play Mode class passes **6/6**, including the new
authority/toggle/culling check.

The preview has no isometric ant, Resource Node, entrance, placement, loose
resource, construction, selection, pointer conversion, fog or pheromone
presentation. It is fixed to 8 x 8 and intended for visual acceptance, not
normal World View commands.

## Manual acceptance

1. Open `Assets/AntColony/Scenes/AntGameDevelopment.unity` and enter Play Mode.
2. Confirm Colony View is unchanged, then open **World View**.
3. Confirm **Legacy Surface** is the default full-Surface renderer.
4. Select **Isometric 8x8 Preview** and inspect one centred, crisp, edge-aligned
   8 x 8 patch using the grass and water mappings above.
5. Confirm unsupported terrain is magenta and nodes/decorations are not
   substituted into the background.
6. Confirm old Surface ant visuals are hidden in preview mode.
7. Switch to Colony View and confirm no Surface presentation leakage.
8. Advance time, switch back, and confirm simulation/identity/residency did not
   reset.
9. Select **Legacy Surface** and confirm the original presentation and ant
   renderer state return.
10. Exit Play Mode and confirm a clean Console.

ISO-WV-1 is accepted. For ISO-WV-1B, compare every preset against 100%, checking
raised grass gaps, water overlap and neighbour ordering. Independent X/Y values
can be changed during Play Mode by selecting **ISO-WV-1 Isometric 8x8
Presentation** and editing the two renderer fields. Reduced spacing remains
experimental pending a clean Console and user approval. If no value fixes grass
without harming water, investigate neighbour-aware terrain separately.
