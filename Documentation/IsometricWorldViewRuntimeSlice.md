# ISO-WV-1 Runtime Isometric Terrain Slice

**Status:** ISO-WV-1 manually accepted; ISO-WV-1B spacing experiment pending manual acceptance
**Date:** 19 October 2026  
**Scope:** Presentation-only 8 x 8 Surface terrain preview. The authoritative
Surface remains 64 x 64 and all Colony, navigation, ant, job, resource,
construction and generation behavior is unchanged.

## Runtime architecture

`SurfaceIsometricRenderer` is a child presentation owned by
`TwoViewWorldCoordinator`. It reads the coordinator's published
`SurfaceRuntimeCellRegionAdapter`; it does not allocate a world model or mutate a
cell. Before drawing, it reads all 64 snapshots for logical cells `(29,29)`
through `(36,36)`. A failed read rejects the whole update, so a partial preview
is never presented.

One Unity `Grid` using the Isometric layout owns one `Tilemap`. Terrain cells are
tiles, not GameObjects. Logical cells map to tilemap cells as
`(-logicalY,-logicalX,0)`, which makes Unity's isometric Grid agree with the
accepted `SurfaceIsometricProjection`: logical X goes down-right and logical Y
goes down-left. At the default 100% spacing the Grid uses the accepted 1 x
0.5-unit cell footprint. Sprite pixels, PPU and import pivots remain unchanged.

## ISO-WV-1B adjustable spacing experiment

`SurfaceIsometricRenderer` exposes **Horizontal Spacing Percent** and
**Vertical Spacing Percent** under **ISO-WV-1B Tile Spacing Experiment** in the
Inspector. Both are clamped from 90% through 100% and default to 100%. They
scale only the preview Grid's `cellSize`; `SurfaceIsometricProjection`, logical
cells and gameplay state are unchanged. The preview centre stays anchored.

The development panel provides uniform **100%**, **97%**, **95%**, **93%** and
**90%** buttons. Independent X/Y experiments use the Inspector controls. Legacy
Surface remains available and does not reset the preview setting.

The full logical Surface, A*, motors, orders and ant transforms remain live in
both renderer modes. Resource Nodes are deliberately not written to the
background Tilemap.

## Temporary renderer toggle

The development World View panel exposes two buttons:

- **Legacy Surface** restores the existing full square presentation.
- **Isometric 8x8 Preview** displays the runtime window and adjusts only the
  existing orthographic Surface camera framing.

Legacy is the serialized and runtime default. The Colony camera and culling
mask are not changed. The isometric root remains on layer 9 (`SurfaceWorld`), so
the Colony camera excludes it. Surface-resident ant renderers are temporarily
hidden in isometric mode because ISO-WV-1 does not project ant visuals; their
authoritative roots and transforms are untouched and the original renderer
state is restored on return to Legacy mode or a scenario rebuild.

## Authored visual mapping

`SurfaceIsometricVisualCatalog` is a presentation-only authored catalog. A
dedicated catalog is used because ISO-WV-1 needs one stable runtime `Resources`
asset containing the small mapping and fallback; it does not duplicate mutable
world data or introduce another terrain definition authority.

| Runtime Background ID | Sprite | Treatment |
| --- | --- | --- |
| `background.surface.grass` | `Terrain/tile_040.png` | Raised grass block under test |
| `background.surface.water` | `Terrain/tile_104.png` | Water tile under test |
| Unsupported or missing mapping | `Terrain/tile_009.png` | Bright magenta tint |

No rock, vegetation or Resource Node is silently substituted for terrain.

## Known limitations

- Only the fixed logical window `(29,29)` to `(36,36)` is displayed.
- Resource Nodes, loose resources, placements, construction, entrance art,
  selection, orders, fog, pheromones and interaction overlays are not projected.
- Surface ants are hidden in isometric mode; movement still simulates.
- Isometric pointer-to-cell input is not implemented. Use the toggle for visual
  review only and return to Legacy mode for Surface commands.
- Camera Home settings still describe the legacy full-Surface framing.
- Seam quality and final visual suitability require direct Unity inspection.
- Reduced spacing can overlap water or raised side pixels. It is an experiment,
  not an approved terrain-connectivity solution.

These are ISO-WV-2 candidates and are not defects in the ISO-WV-1 authority
boundary.

## Automated evidence

- ISO projection, renderer and World View foundation Edit Mode regression:
  **22/22 passed**.
- World View foundation Play Mode regression: **6/6 passed**.
- The focused new Play Mode toggle/isolation test is included in that result and
  also passed independently **1/1**.
- With ISO-WV-1B, the combined projection/renderer/World View Edit regression
  passes **29/29** and the complete World View foundation Play class passes
  **6/6**.

## Manual Unity acceptance

1. Open `Assets/AntColony/Scenes/AntGameDevelopment.unity` in Unity 6000.3.8f1.
2. Enter Play Mode and confirm Colony View initially looks and behaves as before.
3. Select **World View** and confirm **Legacy Surface** is active by default.
4. Optionally select generated founding-queen scenario seed `2203`; confirm the
   full 64 x 64 simulation remains available in Legacy mode.
5. Select **Isometric 8x8 Preview**.
6. Confirm exactly one centred 8 x 8 diamond terrain patch is visible, with
   crisp point-filtered pixels and edge-aligned 32 x 16 footprints.
7. Confirm grass uses `tile_040`, water uses `tile_104`, and no rock,
   vegetation or Resource Node is presented as background terrain.
8. Confirm any unsupported Background is conspicuous magenta rather than a
   silent art substitution.
9. If an ant is Surface-resident, confirm its old square-positioned visual is
   hidden; do not expect an isometric ant in this milestone.
10. Switch to **Colony View** and confirm no isometric tile, Surface overlay or
    Surface ant visual leaks into the Colony camera.
11. Let the simulation advance and switch back to World View. Confirm the same
    scenario remains loaded and the isometric mode is still selected.
12. Select **Legacy Surface** and confirm the full square presentation and
    original Surface ant renderer state return.
13. Confirm view switching did not reset ant identity, needs, inventory,
    residency, jobs, resources, construction or the simulation clock.
14. Exit Play Mode and confirm the Unity Console has no new errors, warnings or
    exceptions.

ISO-WV-1 has since passed this checklist.

### ISO-WV-1B manual comparison

1. Enter Play Mode, choose **World View**, then **Isometric 8x8 Preview**.
2. Confirm **100%** matches the accepted ISO-WV-1 presentation.
3. Compare **97%**, **95%**, **93%** and **90%** using the development buttons.
4. At each preset, inspect raised `tile_040` grass seams, `tile_104` water
   overlap, pixel sharpness and the apparent front/back order of neighbours.
5. For independent tuning, select runtime object **ISO-WV-1 Isometric 8x8
   Presentation**, find `SurfaceIsometricRenderer`, and adjust **Horizontal
   Spacing Percent** and **Vertical Spacing Percent** in the Inspector.
6. Return to **100%** and confirm the baseline is exactly restored.
7. Toggle **Legacy Surface**, return to the preview, and confirm the selected
   spacing was retained without changing Colony View or simulation state.
8. Confirm the Console has no new errors or exceptions.

Do not retain reduced spacing until the user confirms a specific X/Y result. If
no setting closes grass gaps without water overlap or incorrect silhouette
stacking, proceed to separately approved neighbour-aware terrain work.

## ISO-WV-2 boundary

The smallest next milestone is isometric dynamic presentation and interaction:
project Resource Nodes and entrance/placement overlays separately from terrain,
add correctly sorted ant visuals, and route pointer selection through the
accepted inverse projection. Fog/pheromones and expansion beyond the fixed
window should remain separately testable. Do not alter the underlying grid,
navigation, generation, jobs or Colony View.
