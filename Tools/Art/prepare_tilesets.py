"""Build deterministic 32 px candidate sprite sheets from the authored sources.

The AI-assisted source sheets are not regular Unity grids. Their manually reviewed
cell boundaries are recorded below so regeneration never depends on an automatic
32x32 slice. Each source slot is padded to a square before resizing, which preserves
its aspect ratio. The resulting sprites are candidates for visual review; this tool
does not assert that opposite edges tile seamlessly or that composite artwork maps
to Colony View's independent Background and Solid layers.
"""

from __future__ import annotations

import argparse
import hashlib
import json
from dataclasses import dataclass
from pathlib import Path

from PIL import Image


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
TARGET_TILE_SIZE = 32
ALPHA_CLEANUP_THRESHOLD = 8


@dataclass(frozen=True)
class SheetSpecification:
    key: str
    source: Path
    output: Path
    column_edges: tuple[int, ...]
    row_edges: tuple[int, ...]

    @property
    def columns(self) -> int:
        return len(self.column_edges) - 1

    @property
    def rows(self) -> int:
        return len(self.row_edges) - 1


SHEETS = (
    SheetSpecification(
        key="Colony",
        source=REPOSITORY_ROOT
        / "Assets/_AntGame/Art/Colony/Source/Colony_Tileset_Source.png",
        output=REPOSITORY_ROOT
        / "Assets/_AntGame/Art/Colony/Prepared/Colony_Tile_Candidates_32.png",
        # Bounds follow the visible gaps between the ten authored columns and rows.
        # They are intentionally explicit because the spacing varies across the sheet.
        column_edges=(30, 154, 269, 391, 508, 625, 745, 864, 983, 1105, 1225),
        row_edges=(29, 159, 279, 391, 506, 621, 733, 843, 948, 1067, 1190),
    ),
    SheetSpecification(
        key="World",
        source=REPOSITORY_ROOT
        / "Assets/_AntGame/Art/World/Source/World_Tileset_Source.png",
        output=REPOSITORY_ROOT
        / "Assets/_AntGame/Art/World/Prepared/World_Tile_Candidates_32.png",
        # Only the regular top 7x10 terrain block is extracted. The variable-size
        # props below it stay in Source until each prop receives a reviewed footprint.
        column_edges=(30, 151, 272, 391, 510, 628, 747, 867, 986, 1106, 1226),
        row_edges=(30, 161, 273, 387, 502, 619, 735, 847),
    ),
)


def stable_hex(label: str, length: int = 32) -> str:
    return hashlib.sha256(label.encode("utf-8")).hexdigest()[:length]


def stable_internal_id(label: str) -> int:
    value = int.from_bytes(hashlib.sha256(label.encode("utf-8")).digest()[:8], "big")
    value &= (1 << 63) - 1
    return value or 1


def clear_nearly_transparent_pixels(image: Image.Image) -> Image.Image:
    """Remove only near-zero-alpha noise without touching visible source pixels."""
    cleaned = image.copy().convert("RGBA")
    pixels = cleaned.load()
    for y in range(cleaned.height):
        for x in range(cleaned.width):
            red, green, blue, alpha = pixels[x, y]
            if alpha <= ALPHA_CLEANUP_THRESHOLD:
                pixels[x, y] = (0, 0, 0, 0)
            else:
                pixels[x, y] = (red, green, blue, alpha)
    return cleaned


def prepare_sprite(source: Image.Image, box: tuple[int, int, int, int]) -> Image.Image:
    crop = clear_nearly_transparent_pixels(source.crop(box))
    side = max(crop.width, crop.height)
    square = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    square.alpha_composite(crop, ((side - crop.width) // 2, (side - crop.height) // 2))

    # LANCZOS is used only for the one-time reduction from the high-resolution
    # authored source. Unity then uses Point filtering so runtime pixels stay crisp.
    return square.resize(
        (TARGET_TILE_SIZE, TARGET_TILE_SIZE),
        resample=Image.Resampling.LANCZOS,
    )


def build_sheet(specification: SheetSpecification) -> dict[str, object]:
    source = Image.open(specification.source).convert("RGBA")
    output = Image.new(
        "RGBA",
        (
            specification.columns * TARGET_TILE_SIZE,
            specification.rows * TARGET_TILE_SIZE,
        ),
        (0, 0, 0, 0),
    )
    sprites: list[dict[str, object]] = []

    for row in range(specification.rows):
        for column in range(specification.columns):
            box = (
                specification.column_edges[column],
                specification.row_edges[row],
                specification.column_edges[column + 1],
                specification.row_edges[row + 1],
            )
            sprite = prepare_sprite(source, box)
            output.alpha_composite(
                sprite,
                (column * TARGET_TILE_SIZE, row * TARGET_TILE_SIZE),
            )
            sprites.append(
                {
                    "name": f"{specification.key}_R{row + 1:02d}_C{column + 1:02d}",
                    "row_from_top": row + 1,
                    "column_from_left": column + 1,
                    "source_rect_top_left": list(box),
                }
            )

    specification.output.parent.mkdir(parents=True, exist_ok=True)
    output.save(specification.output, optimize=True)
    write_multiple_sprite_meta(specification, sprites)

    alpha = source.getchannel("A")
    alpha_histogram = alpha.histogram()
    return {
        "key": specification.key,
        "source": specification.source.relative_to(REPOSITORY_ROOT).as_posix(),
        "source_sha256": hashlib.sha256(specification.source.read_bytes()).hexdigest(),
        "source_dimensions": list(source.size),
        "source_mode": source.mode,
        "fully_transparent_pixels": alpha_histogram[0],
        "partially_transparent_pixels": sum(alpha_histogram[1:255]),
        "fully_opaque_pixels": alpha_histogram[255],
        "prepared": specification.output.relative_to(REPOSITORY_ROOT).as_posix(),
        "prepared_dimensions": list(output.size),
        "sprite_count": len(sprites),
        "target_tile_pixels": TARGET_TILE_SIZE,
        "sprites": sprites,
    }


def write_multiple_sprite_meta(
    specification: SheetSpecification,
    sprites: list[dict[str, object]],
) -> None:
    asset_path = specification.output.relative_to(REPOSITORY_ROOT).as_posix()
    entries = []
    sprite_blocks = []
    name_table = []
    for sprite in sprites:
        name = str(sprite["name"])
        row = int(sprite["row_from_top"]) - 1
        column = int(sprite["column_from_left"]) - 1
        internal_id = stable_internal_id(f"{asset_path}:{name}")
        sprite_id = stable_hex(f"sprite:{asset_path}:{name}")
        unity_y = (specification.rows - row - 1) * TARGET_TILE_SIZE
        entries.append(
            "  - first:\n"
            f"      213: {internal_id}\n"
            f"    second: {name}"
        )
        sprite_blocks.append(
            "    - serializedVersion: 2\n"
            f"      name: {name}\n"
            "      rect:\n"
            "        serializedVersion: 2\n"
            f"        x: {column * TARGET_TILE_SIZE}\n"
            f"        y: {unity_y}\n"
            f"        width: {TARGET_TILE_SIZE}\n"
            f"        height: {TARGET_TILE_SIZE}\n"
            "      alignment: 0\n"
            "      pivot: {x: 0.5, y: 0.5}\n"
            "      border: {x: 0, y: 0, z: 0, w: 0}\n"
            "      customData:\n"
            "      outline: []\n"
            "      physicsShape: []\n"
            "      tessellationDetail: 0\n"
            "      bones: []\n"
            f"      spriteID: {sprite_id}\n"
            f"      internalID: {internal_id}\n"
            "      vertices: []\n"
            "      indices:\n"
            "      edges: []\n"
            "      weights: []"
        )
        name_table.append(f"      {name}: {internal_id}")

    meta = f"""fileFormatVersion: 2
guid: {stable_hex('guid:' + asset_path)}
TextureImporter:
  internalIDToNameTable:
{chr(10).join(entries)}
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 100
  spriteMode: 2
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 32
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: 0
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 1
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 100
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites:
{chr(10).join(sprite_blocks)}
    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable:
{chr(10).join(name_table)}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData: Art foundation candidate sprites; not seam-certified or gameplay-bound.
  assetBundleName:
  assetBundleVariant:
"""
    specification.output.with_suffix(specification.output.suffix + ".meta").write_text(
        meta,
        encoding="utf-8",
        newline="\n",
    )


def write_source_meta(source: Path) -> None:
    """Keep the preserved original inspectable without treating it as a sliced grid."""
    asset_path = source.relative_to(REPOSITORY_ROOT).as_posix()
    meta = f"""fileFormatVersion: 2
guid: {stable_hex('guid:' + asset_path)}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 100
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 32
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: 0
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 1
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 100
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData: Preserved source sheet; do not auto-slice as 32x32.
  assetBundleName:
  assetBundleVariant:
"""
    source.with_suffix(source.suffix + ".meta").write_text(
        meta,
        encoding="utf-8",
        newline="\n",
    )


def write_folder_meta(directory: Path) -> None:
    asset_path = directory.relative_to(REPOSITORY_ROOT).as_posix()
    meta = f"""fileFormatVersion: 2
guid: {stable_hex('folder-guid:' + asset_path)}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""
    directory.with_suffix(".meta").write_text(meta, encoding="utf-8", newline="\n")


def write_unity_metadata() -> None:
    directories = (
        REPOSITORY_ROOT / "Assets/_AntGame",
        REPOSITORY_ROOT / "Assets/_AntGame/Art",
        REPOSITORY_ROOT / "Assets/_AntGame/Art/Editor",
        REPOSITORY_ROOT / "Assets/_AntGame/Art/Colony",
        REPOSITORY_ROOT / "Assets/_AntGame/Art/Colony/Source",
        REPOSITORY_ROOT / "Assets/_AntGame/Art/Colony/Prepared",
        REPOSITORY_ROOT / "Assets/_AntGame/Art/World",
        REPOSITORY_ROOT / "Assets/_AntGame/Art/World/Source",
        REPOSITORY_ROOT / "Assets/_AntGame/Art/World/Prepared",
    )
    for directory in directories:
        write_folder_meta(directory)
    for specification in SHEETS:
        write_source_meta(specification.source)


def verify_outputs(manifest: dict[str, object]) -> None:
    for sheet in manifest["sheets"]:
        prepared = REPOSITORY_ROOT / str(sheet["prepared"])
        image = Image.open(prepared)
        if list(image.size) != sheet["prepared_dimensions"]:
            raise RuntimeError(f"Unexpected dimensions for {prepared}")
        if image.mode != "RGBA":
            raise RuntimeError(f"Expected RGBA output for {prepared}")
        meta = prepared.with_suffix(prepared.suffix + ".meta").read_text(encoding="utf-8")
        for required in (
            "enableMipMap: 0",
            "filterMode: 0",
            "spriteMode: 2",
            "spritePixelsToUnits: 32",
            "textureCompression: 0",
        ):
            if required not in meta:
                raise RuntimeError(f"Missing '{required}' in {prepared.name}.meta")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--verify-only",
        action="store_true",
        help="Verify committed prepared assets and metadata without rewriting them.",
    )
    arguments = parser.parse_args()

    manifest_path = REPOSITORY_ROOT / "Tools/Art/tileset_manifest.json"
    if arguments.verify_only:
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        verify_outputs(manifest)
        print("Verified prepared tilesets and Unity import metadata.")
        return

    manifest = {
        "format_version": 1,
        "notes": [
            "Coordinates are measured source slots, not an assumed 32x32 source grid.",
            "Sprites are aspect-preserving candidates and are not certified seamless.",
            "World props below the regular terrain block are retained only in Source.",
            "Colony composite cavities and edges are not bound to Background/Solid data.",
        ],
        "sheets": [build_sheet(specification) for specification in SHEETS],
    }
    write_unity_metadata()
    manifest_path.write_text(
        json.dumps(manifest, indent=2) + "\n",
        encoding="utf-8",
        newline="\n",
    )
    verify_outputs(manifest)
    print(f"Prepared {sum(sheet['sprite_count'] for sheet in manifest['sheets'])} candidates.")


if __name__ == "__main__":
    main()
