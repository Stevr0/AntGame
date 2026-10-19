# Isometric World View Runtime Presentation

**Status:** ISO-WV-2 — Implemented, Pending Manual Acceptance
**Date:** 19 October 2026

## Authority boundary

The World View remains an unchanged top-down 64 x 64 logical Surface grid.
`SurfaceWorldData`, `SurfaceRuntimeCellRegionAdapter`, Surface A*, resource
state, ant records, jobs, world generation and entrance residency transactions
remain authoritative. Isometric coordinates are presentation and input
conversion only; there is no elevation, gravity, cliff or duplicate grid.

Legacy square rendering remains the default development mode and can be
restored at any time without rebuilding or changing simulation state.

## Full-map renderer

`SurfaceIsometricRenderer.RenderRegion` reads all cells `(0,0)` through
`(63,63)` from the runtime adapter and writes exactly 4,096 tiles into one Unity
Isometric Tilemap. It does not create one GameObject per cell. Its Grid retains
the accepted `(1,0.5,1)` cell size and its Tilemap uses chunk rendering behind
dynamic objects.

The visual catalog retains these approved mappings:

| Background | Sprite | Import contract |
| --- | --- | --- |
| Grass | `tile_040_GrassMain.png` | 32 x 32 canvas, 32 PPU, pivot `(0.5,0.25)` |
| Water | `tile_104_flatWater.png` | 32 x 32 canvas, 32 PPU, pivot `(0.5,0.25)` |
| Unsupported ID | `tile_009.png`, magenta tint | conspicuous fallback |

Resource Nodes remain separate from Background terrain and are not given
ISO-WV-2 visuals.

## Shared coordinate mapping

`SurfaceIsometricProjection` is the sole isometric mapping authority:

- `CellToWorld` maps a logical cell centre to its displayed ground contact.
- `LogicalToWorld` maps continuous motor progress between cell centres.
- `TryWorldToCell` performs bounded diamond-aware inverse selection.
- `GetWorldBounds` derives the projected map bounds from logical corners and
  the calibrated half-diamond footprint.

`TwoViewWorldCoordinator` uses these methods for terrain origin, ants, mouse
commands, destination marker, placeholder entrance and camera framing. No
consumer carries a second projection formula.

## Camera and view switching

The Surface camera remains orthographic and unrotated. Its isometric Home
position is the projected world centre and its Home size fits the complete
diamond at the current aspect ratio. Pan bounds are projection-derived and
viewport-aware, so the view cannot be moved wholly outside the map. Zooming
reclamps the camera.

Legacy and isometric Surface positions/zoom are remembered independently.
Colony camera state is not read or changed by the Surface presentation toggle.
Layer 9 (`SurfaceWorld`) remains the culling boundary, and only the active view
camera/input context is enabled.

## Ant presentation and interaction

`SurfaceNavigationMotor` still receives and follows the same logical A* route.
Its continuous progress is stored in logical Surface coordinates and then
projected onto the existing authoritative ant root. This keeps identity,
skills, needs, inventory, collider, selection indicator and command ownership
on the original GameObject. Arrival, blocked state and `CurrentCell` change only
under the existing movement rules.

Left-click hit testing therefore follows the projected ant root. Right-click
uses the projection inverse, rejects points outside the logical 64 x 64 bounds,
then passes the resolved `SurfaceCell` through unchanged navigation and
traversability validation. The isometric destination diamond and placeholder
entrance share the same projection. Dynamic sorting uses projected
ground-contact Y; no gameplay elevation exists.

The existing `NestEntranceTransitionService` still owns capacity, approach,
commit, residency and release. Region changes enable the appropriate existing
motor and renderer layer; no duplicate or ghost ant visual is created.

## Automated acceptance

Focused coverage verifies:

- continuous/discrete projection agreement and bounded diamond inverse mapping;
- full 64 x 64 coverage, deterministic rebuild and unchanged source fingerprint;
- projection-derived bounds, Home sizing and viewport-aware camera clamping;
- logical A* route preservation with smooth projected movement;
- view isolation, Legacy restoration and entrance reuse.

Focused suites pass **41/41 Edit Mode** and **7/7 Play Mode**. The complete
graphics-enabled Play Mode suite passes **86/86**. Complete Edit Mode passes
**634/638**; four unrelated `ColonyPresentationTests` cases still request Tile
assets from removed `Assets/AntColony/Art/WorldTiles` paths rather than their
current `Assets/AntColony/Art/ColonyView` locations. The first headless full
Edit attempt also crashed in its existing rendered-pixel camera test, so the
reported full Edit result is from the required graphics-enabled rerun. These
automated checks do not replace direct Unity visual acceptance.

## Manual Unity acceptance

1. Open `Assets/AntColony/Scenes/AntGameDevelopment.unity` and enter Play Mode.
2. Confirm **Legacy Surface** remains selected initially.
3. Enter seed `2203`, choose **Start Generated**, then select **World View**.
4. Choose **Isometric 64x64 World**. Confirm the complete diamond appears with
   continuous approved Grass/Water and no magenta fallback for generated IDs.
5. Press **Home**. Confirm the complete diamond is framed. Pan to every extreme,
   zoom in/out, and confirm the viewport stays bounded to the projected world.
6. Change pan/zoom, toggle Legacy and back, and confirm each mode restores its
   own Surface camera state. Confirm Colony camera framing did not change.
7. Use the nest entrance to bring the queen to Surface. Confirm exactly one
   placeholder ant appears at the projected entrance with its selection marker.
8. Left-click that ant, then right-click open cells near the centre, each edge
   and each corner. Confirm the destination diamond aligns to the chosen cell
   and the ant moves smoothly along valid projected A* steps.
9. Right-click Water, an invalid/blocked destination and outside all four world
   edges. Confirm these do not create an invalid movement order.
10. Toggle Legacy while the ant is present and confirm its logical cell/order is
    preserved. Toggle back and confirm it reappears at the matching iso cell.
11. Clear the Surface arrival cell, return through the entrance, then reuse it.
    Confirm capacity/reuse behaviour and no duplicate or ghost ant visual.
12. Switch repeatedly between Colony and World views. Confirm no Surface tile,
    marker, entrance or ant leaks into Colony View and Colony input still works.
13. Restore the authored fixture and repeat the presentation toggle. Confirm
    deterministic reconstruction and that all three ant records remain intact.
14. Check the Console for errors, exceptions or missing visual warnings.

## Deferred to ISO-WV-3

- Resource Node and loose-resource presentation.
- Placement, Construction, Storage and staged-material presentation.
- Final World ant animation sets and final entrance art.
- Fog/pheromone overlays and terrain neighbour/shoreline autotiling.

These are known visual limitations, not permission to extend this milestone.
