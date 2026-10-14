# Art Foundation

The custom Colony and World tilesets are preserved and prepared as separate candidate libraries. Milestone 3.5A additionally binds two reviewed full-cell Colony candidates through four existing visual Tile assets without changing gameplay authority.

## Direction

- Colony View uses warm soil, cool stone, dark excavated interiors and restrained green/root/fungus accents.
- World View uses richer vegetation greens, warm soil/path browns, grey-blue water and natural prop accents.
- Pixel edges stay crisp through Point filtering, no compression and no mipmaps.
- Appearance never defines terrain behavior; gameplay material definitions remain authoritative.

## Files and settings

| Library | Preserved source | Prepared candidates | Contents |
| --- | --- | --- | --- |
| Colony | `Assets/_AntGame/Art/Colony/Source/Colony_Tileset_Source.png` | `Assets/_AntGame/Art/Colony/Prepared/Colony_Tile_Candidates_32.png` | 100 position-named `32x32` candidates |
| World | `Assets/_AntGame/Art/World/Source/World_Tileset_Source.png` | `Assets/_AntGame/Art/World/Prepared/World_Tile_Candidates_32.png` | 70 position-named `32x32` terrain candidates |

Prepared sheets import as Sprite (2D and UI), Multiple, 32 Pixels Per Unit, Point filtered, uncompressed and without mipmaps. `Tools/Art/prepare_tilesets.py` rebuilds them from manually reviewed rectangles recorded in `Tools/Art/tileset_manifest.json`; it does not assume a uniform 32-pixel source grid or stretch a crop to change its aspect ratio.

## View boundaries

Colony View remains side-on and retains one permanent Background plus an optional foreground Solid per resident cell. Many source candidates visually combine cavities, exposed edges and surrounding terrain, so they are not yet safe replacements for those independent layers.

World View remains approved future design only. Its regular top `7x10` terrain block is prepared as a separate library, while variable-size props remain source-only pending reviewed footprints and pivots. No World scene, navigation, transition or exploration system is added.

## Limitations

- `Colony_R02_C01` now presents Soil/Natural Earth and `Colony_R04_C01` presents Stone/Natural Stone; Background variants use darker Tile colours.
- These mappings are reviewed full-cell textures but have not passed seamless-edge acceptance.
- Rounded borders, dark gutters and coloured edge artefacts remain visible in places.
- The one-time reduction from `1254x1254` source sheets to 32-pixel candidates loses fine detail.
- Colony art still needs semantic layer separation; World props need individual extraction.
- Wider production mapping and Colony/World Tile Palettes are deferred until visual and semantic review.

Automated preparation verification, Unity import validation and the strict documentation build pass. Visual, seamless-edge and gameplay-camera acceptance remain manual.

The full source analysis, future artwork needs and seven-step Unity inspection checklist are maintained in `Documentation/ArtDirection.md`.
