# Ant Colony Art Direction and Tileset Foundation

## Status

**Implemented — Pending Manual Acceptance (14 October 2026).** The repository now preserves the two original source sheets and includes deterministic 32-pixel candidate libraries with Unity import metadata. This is an art-foundation milestone only: no candidate is assigned to a gameplay material, Tile asset, Tile Palette, scene, collider or navigation rule.

## Visual direction and palette

The current direction is detailed, readable pixel art with earthy natural materials and small high-contrast accents.

- **Colony View:** warm umber soil, cool neutral stone, dark excavated-space interiors, muted root and fungus details, and restrained moss/grass greens near exposed surface edges.
- **World View:** richer moss and leaf greens, warm path and soil browns, grey-blue water, autumn leaf accents, and natural rock, log, fungus and shrub props.
- Preserve material readability before decorative variety. Soil, stone, exposed Background and optional foreground Solid must remain distinguishable at gameplay zoom.
- Avoid smoothing, texture compression and mipmaps on prepared pixel-art textures. Runtime sampling uses Point filtering.
- Do not infer semantic terrain behavior from colour or appearance. Authoritative material definitions remain the gameplay source of truth.

The source palette has not been reduced to a fixed indexed palette. It contains extensive partial alpha and high-resolution colour variation, so any future palette reduction needs a separate visual review rather than an automatic quantisation pass.

## Asset locations

| Purpose | Colony View | World View |
| --- | --- | --- |
| Preserved original | `Assets/_AntGame/Art/Colony/Source/Colony_Tileset_Source.png` | `Assets/_AntGame/Art/World/Source/World_Tileset_Source.png` |
| Prepared candidates | `Assets/_AntGame/Art/Colony/Prepared/Colony_Tile_Candidates_32.png` | `Assets/_AntGame/Art/World/Prepared/World_Tile_Candidates_32.png` |
| Rebuild tool and measured rectangles | `Tools/Art/prepare_tilesets.py` and `Tools/Art/tileset_manifest.json` | Same shared tool and manifest |

Both originals are RGBA `1254x1254` images. They are preserved byte-for-byte. Their SHA-256 values and per-sprite source rectangles are recorded in `Tools/Art/tileset_manifest.json`.

## Preparation and import settings

The originals are not regular 32-pixel sheets. Visible cell spacing and cell dimensions vary, and the World source changes from a regular terrain block to variable-size props below it. The preparation tool therefore uses manually reviewed source boundaries rather than an automatic 32x32 slice.

Each extracted cell is placed on a square transparent canvas before a one-time reduction to `32x32`; it is never stretched independently on each axis. Pixels with alpha `0–8` are cleared to remove near-invisible fringe noise. All more-visible source pixels are retained. The result is a candidate library, not a seamless-production atlas.

Committed Unity metadata applies:

- Texture Type: **Sprite (2D and UI)**;
- Sprite Mode: **Multiple** for prepared sheets and **Single** for preserved originals;
- Pixels Per Unit: **32** so one prepared tile is one Unity world unit;
- Filter Mode: **Point**;
- Compression: **None**;
- Generate Mip Maps: **off**;
- Alpha Is Transparency: **on**; and
- fallback physics shape generation: **off** because collision remains data-driven.

The Colony output is `320x320` with 100 named sprites (`Colony_R01_C01` through `Colony_R10_C10`). The World output is `320x224` with 70 named terrain sprites (`World_R01_C01` through `World_R07_C10`). Names describe source position only; they do not assert gameplay semantics.

## Colony View conventions

Colony View keeps its established two-layer model:

- every resident cell has one permanent **Background** material;
- a cell may additionally have one foreground **Solid** material;
- excavation removes only the Solid;
- prepared or functional Background conversion changes only the Background; and
- visual Tilemaps remain projections of world data, never gameplay authority.

The source includes soil, stone, grass/surface edges, dark cavity compositions, roots, fungi, rocks and other decorations. Many cells combine a dark cavity, exposed edge and surrounding terrain in one image. Those composites cannot safely represent the existing independent Background and Solid layers without further art separation. They remain candidates and are not wired into `WorldTilemapRenderer`, material definitions, collision, excavation or navigation.

## World View conventions

World artwork is a separate library for the future top-down view. The prepared sheet contains only the regular top `7x10` terrain block. The variable-size shrubs, rocks, logs, mushrooms, roots, leaves and stump below that block remain in the preserved source because they do not have reliable single-tile footprints.

No World View scene, Tilemap, Tile Palette, terrain authority, traversal, exploration, view switching or transition system is introduced by this milestone. Future World View implementation must define its own surface coordinate, terrain and navigation rules before these candidates are assigned.

## Tile assets and palettes

No production `Tile` assets or Tile Palettes are committed yet. This is deliberate:

1. edge inspection has not demonstrated seamless repetition;
2. several Colony candidates combine visual layers that the runtime owns separately;
3. the World props need reviewed footprints and pivots; and
4. choosing a sprite for `Background_UnpreparedEarth`, `Solid_Soil` or another material would change current scene presentation before visual acceptance.

After manual approval, create separate **Colony Terrain**, **Colony Decoration** and **World Terrain** palettes. Create Tile assets only for accepted semantic mappings, keep their collider type `None`, and continue using the dedicated collision Tilemap driven by authoritative Solid data.

## Known limitations and future artwork

- No tile is currently certified seamless. Rounded borders, dark gutters and occasional red/yellow chromatic edge artefacts are visible in the sources and some prepared candidates.
- The 32-pixel reduction necessarily loses some fine source detail; it requires in-Editor zoom review before production use.
- Colony composites need separated Background, Solid, exposed-edge and decoration passes before runtime binding.
- Corners, T-junctions, inner/outer edges and transition rules need an explicit autotiling set or Rule Tile design after the layer separation is approved.
- World water, shoreline and path edges need adjacency coverage and edge-matching review.
- World props need individual transparent extraction, footprint, pivot, sorting-layer and overlap review.
- Functional Background visuals need distinct, legible states without changing their existing gameplay definitions.

## Manual Unity acceptance

Automated validation completed on 14 October 2026:

- deterministic preparation reported 170 candidates and `--verify-only` passed;
- Unity `6000.3.8f1` imported all four textures and the Editor validator confirmed dimensions, sprite counts and documented import settings;
- Unity batch validation exited with code 0;
- the strict Zensical documentation build reported `No issues found`; and
- these checks did not exercise appearance, seams or gameplay camera readability.

1. Open both Source textures and confirm they remain `1254x1254` RGBA originals.
2. Open each prepared texture and confirm Sprite Mode Multiple, PPU 32, Point filtering, no compression and no mipmaps.
3. Open Sprite Editor and confirm all named rectangles are exactly `32x32` with no overlap.
4. Inspect prepared sprites at 1x, 2x and normal gameplay camera zoom for lost detail and coloured fringe artefacts.
5. Place representative candidates on a temporary, non-gameplay Tilemap and test repeated horizontal and vertical edges; record failures rather than assuming seamlessness.
6. Compare candidate meaning against Background/Solid ownership before creating any production Tile assets.
7. Confirm the existing development scene, terrain visuals, collision, excavation, navigation and ant locomotion are unchanged.

Visual acceptance remains pending until those checks are performed in Unity.
