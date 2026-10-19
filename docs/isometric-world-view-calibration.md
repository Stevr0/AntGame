# ISO-WV-0 Isometric Art and Projection Calibration

**Status:** Implemented — automated tests passed; manual Unity acceptance pending  
**Date:** 19 October 2026  
**Scope:** Isolated presentation calibration only. The live World View, Colony View,
simulation, navigation, generation, resources, orders and authored gameplay content
are unchanged.

## Calibrated presentation contract

The verified logical ground footprint is a **32 x 16 pixel diamond** on a
32 x 32 pixel source canvas. At **32 pixels per unit**, the diamond is 1 world
unit wide and 0.5 world units high. Adjacent logical cells therefore move by
half a diamond:

- Positive logical X: `(+0.5, -0.25)` world units, down-right on screen.
- Positive logical Y: `(-0.5, -0.25)` world units, down-left on screen.

For an origin `(originX, originY)`, the presentation mapping is:

```text
worldX = originX + (cellX - cellY) * 0.5
worldY = originY - (cellX + cellY) * 0.25
```

`SurfaceIsometricProjection` implements this mapping and its inverse without
owning or mutating Surface data. The inverse uses a deterministic half-open
boundary rule: an exact shared midpoint resolves to the larger logical
coordinate. Signed coordinates are supported for future expansion.

All five calibration sprites use their full 32 x 32 canvas with a common
ground-contact pivot of **(16, 8) pixels**, or normalized **(0.5, 0.25)**. This
places the pivot at the centre of the 32 x 16 ground diamond. Raised art and
props extend above that shared ground plane; their pixels do not create
simulation height.

## Selected artwork

Transparent bounds below use PNG image coordinates measured from the top-left
and are inclusive. Source pixels remain unchanged.

| Role | Sprite | Alpha bounds | Opaque extent | Apparent use |
| --- | --- | --- | --- | --- |
| Flat ground | `Terrain/tile_101.png` | x 0-31, y 16-31 | 32 x 16 | One flat 32 x 16 diamond |
| Raised ground | `Terrain/tile_022.png` | x 0-31, y 4-31 | 32 x 28 | Raised grass block with the same base footprint |
| Water | `Terrain/tile_095.png` | x 0-31, y 16-31 | 32 x 16 | One flat water-pattern diamond |
| Rock/resource | `Terrain/tile_060.png` | x 5-28, y 0-29 | 24 x 30 | Tall rock decoration/resource candidate |
| Vegetation | `Terrain/tile_044.png` | x 5-26, y 6-27 | 22 x 22 | Flowering vegetation decoration |

Only these five assets were calibrated. Each is imported as Sprite (2D and UI),
Single, 32 PPU, Point filtered, uncompressed, without mipmaps, Full Rect, with
fallback physics generation disabled and the common pivot above. No collection-
wide importer rule was added.

## Isolated preview

`Assets/AntColony/Scenes/IsometricCalibrationPreview.unity` contains only:

- an 8 x 8 checkerboard grid built from the flat tile;
- water at logical cell (1,5);
- vegetation at (2,2);
- a labelled centre marker at (3,3);
- raised terrain at (4,4);
- a rock at (5,3); and
- an orthographic calibration camera.

Sorting order is derived from `cellX + cellY`; props receive a small offset
above their host ground cell. This is preview-only calibration behavior, not a
replacement renderer. The editor menu can rebuild the authored preview from the
five explicit sprite references.

## Open and inspect in Unity

1. Open the AntGame project in Unity 6000.3.8f1.
2. Choose **Ant Colony > Isometric > Open Calibration Preview**.
3. If the preview needs regeneration, choose **Ant Colony > Isometric > Rebuild
   Calibration Preview**.
4. Use the Game or Scene view with the orthographic **Calibration Camera**.
5. Do not add this scene to gameplay scene flow or Build Settings.

## Manual acceptance checklist

- [ ] The preview opens with no Console errors.
- [ ] Exactly 64 ground cells form a readable 8 x 8 diamond grid.
- [ ] Adjacent flat diamonds meet without unintended gaps or seams.
- [ ] The checkerboard makes every cell boundary unambiguous.
- [ ] The water cell occupies the same logical footprint as flat ground.
- [ ] Raised terrain remains anchored to its host cell and overlaps rear cells
      naturally.
- [ ] Rock and vegetation bases read as attached to their intended cell centres.
- [ ] The magenta marker identifies the centre of logical cell (3,3).
- [ ] Point filtering remains sharp at useful integer zoom levels.
- [ ] Rebuilding the preview produces the same layout.
- [ ] Opening or playing existing Colony/World development scenes shows no
      behavioral or visual change caused by this preview.

## Automated evidence

On 19 October 2026:

- `SurfaceIsometricProjectionTests`: **8/8 passed**. Coverage includes known
  positions, inverse conversion, signed coordinates, centre round trips,
  adjacency, boundary behavior, determinism, selected imports and scene
  isolation/structure.
- Existing `WorldViewFoundationTests`: **9/9 passed**.
- Full Edit Mode: **619/623 passed**. The four failures are existing
  `ColonyPresentationTests.RuntimeArtMappingUsesOnlyReviewedColliderFreeCandidates`
  cases, which request missing `Assets/AntColony/Art/WorldTiles/*.asset` files.
  ISO-WV-0 does not modify those tests, missing Colony assets or mappings.

Results are recorded under `Logs/ISO-WV-0-EditMode.xml` and
`Logs/ISO-WV-0-WorldViewRegression.xml`; the broad-run evidence is
`Logs/ISO-WV-0-FullEditMode.xml`. Manual visual acceptance is still required.

## Known limitations

- A common footprint and mathematically correct edge placement do not prove that
  every colour/detail transition in the purchased pack tiles seamlessly. Only
  the selected flat and water examples are in this preview; visual seam quality
  remains a manual acceptance item.
- The raised block is decorative depth only. It has no elevation, cliff,
  collision or navigation meaning.
- The rock and vegetation are calibration candidates, not assignments to live
  Resource Definitions or other gameplay data.
- The preview demonstrates static depth ordering. Live ant/object animation,
  fog of war, pheromones, mouse picking and chunk renderer integration belong to
  later approved milestones.
- Pixel sharpness still depends on final camera resolution and integer pixel
  scaling policy. ISO-WV-0 does not change the live camera.
- ISO-WV-0 itself did not authorise runtime integration. ISO-WV-1 was later
  approved and now consumes this projection contract; see
  [Isometric Runtime Terrain Slice](isometric-world-view-runtime-slice.md).

