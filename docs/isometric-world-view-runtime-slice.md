# ISO-WV-1 Runtime Isometric Terrain Slice

**Status:** ISO-WV-1D implemented - pending manual acceptance
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
down-left. The Grid is fixed at the accepted `(1,0.5,1)` cell size.

## ISO-WV-1B outcome and ISO-WV-1C cleanup

ISO-WV-1B tested whole-Grid compression. ISO-WV-1C removes all experimental
spacing fields, methods and buttons and restores the single accepted Grid
calibration. A user-edited flat grass interior now replaces the raised block in
the runtime catalog. The original raised sprite is retained for possible future
edge, corner or decorative use. No neighbour-aware autotiling is implemented.

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
| `background.surface.grass` | `Terrain/tile_040_GrassMain.png` |
| `background.surface.water` | `Terrain/tile_104_flatWater.png` |
| Unsupported ID | `tile_009` tinted bright magenta |

The presentation-only `SurfaceIsometricVisualCatalog` stores these authored
references. No water, rock, vegetation or resource artwork is silently used for
an incompatible role.

The former `tile_104.png` is a raised 32 x 20 silhouette that was imported with
a cropped 32 x 21 Sprite rect and centre pivot. It made correctly rendered
water cells appear oversized, overlapping or missing. ISO-WV-1D preserves that
original and maps Water to the user-supplied flat derivative, imported Full Rect
at 32 PPU with pivot `(0.5,0.25)`. No projection, Tilemap sorting or gameplay
state changed. The isolated calibration scene includes five labelled 4 x 4
water/grass layouts.

## Validation and limitations

The ISO-WV-1D combined Edit Mode regression passes **28/28**. The full
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
   8 x 8 patch using the fixed 1 x 0.5-unit Grid.
5. Confirm flat `tile_040_GrassMain` repeats without dirt sides or gaps, and
   `tile_104_flatWater` remains correctly aligned without overlap.
6. Confirm unsupported terrain is magenta and nodes/decorations are not
   substituted into the background.
7. Confirm no spacing percentage controls remain in the panel or renderer.
8. Confirm old Surface ant visuals are hidden in preview mode.
9. Switch to Colony View and confirm no Surface presentation leakage.
10. Advance time, switch back, and confirm simulation/identity/residency did not
   reset.
11. Select **Legacy Surface** and confirm the original presentation and ant
   renderer state return.
12. Open the isolated calibration preview and inspect all five labelled 4 x 4
    water layouts, then exit Play Mode and confirm a clean Console.

ISO-WV-1 is accepted. ISO-WV-1D remains pending the checks above.
