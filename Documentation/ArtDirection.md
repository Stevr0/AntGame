# Ant Colony Art Direction and Tileset Foundation

## Shared visual-definition architecture — WV-B3D.3A addendum

Visual metadata now has one common definition base for stable lookup, gameplay
definition association, sorting, transform and fallback colour, with focused
category assets so Inspectors expose only meaningful states. Backgrounds expose
normal Colony/Surface sprites; Resource Nodes expose normal, worked and depleted
states; Placements expose construction, completed, damaged and destroyed states;
loose resources expose normal sprites. The Placement state resolver is
presentation-only and treats unknown durability as completed. No visual assets,
sprite assignment, renderer replacement, seasonal/biome variant or animation
was introduced; those can extend this seam later without changing gameplay
definitions.

## Status

**Milestone 3.5A implemented — Pending Manual Acceptance (14 October 2026).** The repository preserves both originals and deterministic candidate libraries. A deliberately small Colony allow-list is now assigned to four existing visual Tile assets; no sprite owns collision, navigation or material rules.

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

The source includes soil, stone, grass/surface edges, dark cavity compositions, roots, fungi, rocks and other decorations. Many cells combine a dark cavity, exposed edge and surrounding terrain in one image. Those composites cannot safely represent the existing independent Background and Solid layers without further art separation.

### Reviewed Colony candidate classification

| Classification | Candidates | Runtime decision |
| --- | --- | --- |
| Full-cell/repeatable terrain surfaces | `R02_C01–C10`, `R08_C01–C10` | Only `R02_C01` is allow-listed now; repetition is visually coherent but not certified seamless. |
| Foreground Solid artwork | `R01_C01–C10`, `R04_C01–C05`, `R05_C05` | `R05_C05` is allow-listed for Stone. Surface/edge variants need authored adjacency rules. |
| Independent Background artwork | None verified | Natural Earth/Stone use darker tints of the two full-cell allow-listed textures. Prepared and functional types retain temporary visuals. |
| Decorative overlays | `R05_C01–C06`, `R06_C01–C05`, `R07_C01–C10` | Deferred; roots, fungi, small rocks and props must not become terrain truth. |
| Composite illustrations requiring separation | `R03_C01–C10`, `R04_C06–C10`, `R05_C07–C10`, `R06_C06–C10`, `R09_C01–C10`, `R10_C01–C10` | Deferred because cavity, edge, Solid and decoration pixels are combined. |
| Unusable/not yet verified | Every candidate except `R02_C01`, `R05_C03` and `R05_C05` for their stated mappings | Kept in the library. No silent Tile binding or seamless claim. |

`Colony_R02_C01` maps to `Solid_Soil` and a darker `Background_UnpreparedEarth`. The intentionally distinct Stone mapping uses `Colony_R05_C05` for `Solid_Stone` and `Colony_R05_C03` for the darker `Background_UnpreparedStone`. The existing Tile assets retain stable GUIDs and `Collider Type: None`.

## World View conventions

World artwork is a separate library for the future top-down view. The prepared sheet contains only the regular top `7x10` terrain block. The variable-size shrubs, rocks, logs, mushrooms, roots, leaves and stump below that block remain in the preserved source because they do not have reliable single-tile footprints.

No World View scene, Tilemap, Tile Palette, terrain authority, traversal, exploration, view switching or transition system is introduced by this milestone. Future World View implementation must define its own surface coordinate, terrain and navigation rules before these candidates are assigned.

## Tile assets and palettes

Only the four existing natural Soil/Stone Tile assets above are custom-art bound. A general production palette remains deferred because:

1. edge inspection has not demonstrated seamless repetition;
2. several Colony candidates combine visual layers that the runtime owns separately;
3. the World props need reviewed footprints and pivots; and
4. functional Backgrounds need distinct, honest art rather than misleading reuse.

After manual approval, create separate **Colony Terrain**, **Colony Decoration** and **World Terrain** palettes only where they improve authored workflows. Keep visual Tile collider type `None` and continue using the dedicated collision Tilemap driven by authoritative Solid data.

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
- Unity batch validation exited with code 0 and verified the four allow-listed runtime mappings remain collider-free;
- focused 3.5A Edit Mode passed `11/11`, complete graphics-enabled Edit Mode passed `503/503`, and complete Play Mode passed `65/65`;
- the strict Zensical documentation build reported `No issues found`; and
- these checks did not exercise appearance, seams or gameplay camera readability.

1. Open both Source textures and confirm they remain `1254x1254` RGBA originals.
2. Open each prepared texture and confirm Sprite Mode Multiple, PPU 32, Point filtering, no compression and no mipmaps.
3. Open Sprite Editor and confirm all named rectangles are exactly `32x32` with no overlap.
4. Inspect prepared sprites at 1x, 2x and normal gameplay camera zoom for lost detail and coloured fringe artefacts.
5. Inspect repeated `Colony_R02_C01`, `Colony_R05_C03` and `Colony_R05_C05` mappings horizontally and vertically; record visible seams rather than assuming seamlessness.
6. Confirm no candidate outside the documented allow-list is silently bound to a gameplay Tile.
7. Confirm Soil/Stone foreground and darker natural Background presentation changed as documented while collision, excavation, navigation and ant locomotion remain unchanged.

Visual acceptance remains pending until those checks are performed in Unity.
