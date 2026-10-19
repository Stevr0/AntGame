using System;
using System.Collections.Generic;
using AntColony.World;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace AntColony.WorldPresentation
{
    public enum SurfaceIsometricRenderStatus
    {
        Success,
        MissingConfiguration,
        WindowOutsideAuthority,
        CellUnavailable
    }

    public readonly struct SurfaceIsometricRenderResult
    {
        public SurfaceIsometricRenderResult(SurfaceIsometricRenderStatus status,
            int renderedCellCount, int fallbackCellCount, SurfaceCell problemCell)
        {
            Status = status;
            RenderedCellCount = renderedCellCount;
            FallbackCellCount = fallbackCellCount;
            ProblemCell = problemCell;
        }

        public SurfaceIsometricRenderStatus Status { get; }
        public int RenderedCellCount { get; }
        public int FallbackCellCount { get; }
        public SurfaceCell ProblemCell { get; }
        public bool Succeeded => Status == SurfaceIsometricRenderStatus.Success;
    }

    /// <summary>
    /// Presentation-only projection of an authoritative Surface region into one
    /// Unity Isometric Tilemap. It never writes gameplay or navigation state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SurfaceIsometricRenderer : MonoBehaviour
    {
        public const int PreviewSize = 8;
        private readonly Dictionary<string, Tile> backgroundTiles =
            new(StringComparer.Ordinal);
        private readonly Dictionary<SurfaceCell, string> renderedBackgrounds = new();
        private readonly List<Tile> ownedTiles = new();

        private IReadOnlyRuntimeCellRegion surfaceRegion;
        private SurfaceIsometricProjection projection;
        private Grid grid;
        private Tilemap backgroundTilemap;
        private TilemapRenderer tilemapRenderer;
        private Tile fallbackTile;
        private SurfaceCell windowMinimum;
        private int renderedCellCount;
        private int fallbackCellCount;
        private int renderedWidth;
        private int renderedHeight;

        public IReadOnlyRuntimeCellRegion SurfaceRegion => surfaceRegion;
        public SurfaceIsometricProjection Projection => projection;
        public Grid Grid => grid;
        public Tilemap BackgroundTilemap => backgroundTilemap;
        public TilemapRenderer BackgroundTilemapRenderer => tilemapRenderer;
        public SurfaceCell WindowMinimum => windowMinimum;
        public int RenderedCellCount => renderedCellCount;
        public int FallbackCellCount => fallbackCellCount;
        public int RenderedWidth => renderedWidth;
        public int RenderedHeight => renderedHeight;

        public void Configure(IReadOnlyRuntimeCellRegion authoritativeSurface,
            SurfaceIsometricProjection configuredProjection,
            SurfaceIsometricVisualCatalog visualCatalog,
            int presentationLayer)
        {
            surfaceRegion = authoritativeSurface;
            projection = configuredProjection;
            EnsureTilemap(presentationLayer);
            BuildVisualLookup(visualCatalog);
        }

        public SurfaceIsometricRenderResult RenderWindow(SurfaceCell minimum)
        {
            return RenderRegion(minimum, PreviewSize, PreviewSize);
        }

        /// <summary>
        /// Rebuilds one authoritative logical rectangle. The current 64 x 64
        /// world uses this directly; explicit bounds also keep the presentation
        /// ready for a future chunk-sized caller without creating another grid.
        /// </summary>
        public SurfaceIsometricRenderResult RenderRegion(SurfaceCell minimum,
            int width, int height)
        {
            if (surfaceRegion == null || projection == null ||
                backgroundTilemap == null || fallbackTile == null ||
                width <= 0 || height <= 0)
                return new SurfaceIsometricRenderResult(
                    SurfaceIsometricRenderStatus.MissingConfiguration,
                    0, 0, minimum);

            var snapshots = new RuntimeCellSnapshot[width, height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var cell = new SurfaceCell(minimum.X + x, minimum.Y + y);
                RuntimeCellReadResult read = surfaceRegion.TryGetCell(
                    RegionCellId.Surface(surfaceRegion.RegionId, cell),
                    out RuntimeCellSnapshot snapshot);
                if (read == RuntimeCellReadResult.CellUnavailable)
                    return new SurfaceIsometricRenderResult(
                        SurfaceIsometricRenderStatus.WindowOutsideAuthority,
                        0, 0, cell);
                if (read != RuntimeCellReadResult.Success || snapshot == null)
                    return new SurfaceIsometricRenderResult(
                        SurfaceIsometricRenderStatus.CellUnavailable,
                        0, 0, cell);
                snapshots[x, y] = snapshot;
            }

            backgroundTilemap.ClearAllTiles();
            renderedBackgrounds.Clear();
            windowMinimum = minimum;
            renderedWidth = width;
            renderedHeight = height;
            renderedCellCount = 0;
            fallbackCellCount = 0;

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                RuntimeCellSnapshot snapshot = snapshots[x, y];
                SurfaceCell cell = snapshot.CellId.SurfaceCell;
                Vector3Int tileCell = LogicalToTilemapCell(cell);
                bool supported = backgroundTiles.TryGetValue(
                    snapshot.BackgroundDefinitionId, out Tile tile);
                tile ??= fallbackTile;
                backgroundTilemap.SetTile(tileCell, tile);
                backgroundTilemap.SetTileFlags(tileCell, TileFlags.None);
                backgroundTilemap.SetColor(tileCell, supported
                    ? Color.white
                    : new Color32(255, 0, 220, 255));
                renderedBackgrounds.Add(cell, snapshot.BackgroundDefinitionId);
                renderedCellCount++;
                if (!supported) fallbackCellCount++;
            }

            return new SurfaceIsometricRenderResult(
                SurfaceIsometricRenderStatus.Success,
                renderedCellCount, fallbackCellCount, default);
        }

        public bool TryGetRenderedBackground(SurfaceCell cell,
            out string backgroundDefinitionId) =>
            renderedBackgrounds.TryGetValue(cell, out backgroundDefinitionId);

        /// <summary>
        /// Unity's isometric Grid points positive cell Y up-left. Swapping and
        /// negating axes preserves ISO-WV-0: logical X down-right, Y down-left.
        /// </summary>
        public static Vector3Int LogicalToTilemapCell(SurfaceCell logicalCell) =>
            new(-logicalCell.Y, -logicalCell.X, 0);

        private void EnsureTilemap(int presentationLayer)
        {
            if (grid != null && backgroundTilemap != null) return;

            var gridObject = new GameObject("Isometric Terrain Grid", typeof(Grid));
            gridObject.layer = presentationLayer;
            gridObject.transform.SetParent(transform, false);
            grid = gridObject.GetComponent<Grid>();
            grid.cellLayout = GridLayout.CellLayout.Isometric;
            grid.cellSize = new Vector3(
                SurfaceIsometricProjection.DefaultTileWidthPixels /
                    (float)SurfaceIsometricProjection.DefaultPixelsPerUnit,
                SurfaceIsometricProjection.DefaultTileHeightPixels /
                    (float)SurfaceIsometricProjection.DefaultPixelsPerUnit,
                1f);
            grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;
            gridObject.transform.position = projection != null
                ? (Vector3)projection.Origin
                : Vector3.zero;

            var tilemapObject = new GameObject(
                "Authoritative Backgrounds", typeof(Tilemap), typeof(TilemapRenderer));
            tilemapObject.layer = presentationLayer;
            tilemapObject.transform.SetParent(gridObject.transform, false);
            backgroundTilemap = tilemapObject.GetComponent<Tilemap>();
            backgroundTilemap.tileAnchor = Vector3.zero;
            tilemapRenderer = tilemapObject.GetComponent<TilemapRenderer>();
            tilemapRenderer.mode = TilemapRenderer.Mode.Chunk;
            tilemapRenderer.sortingOrder = -20;
        }

        private void BuildVisualLookup(SurfaceIsometricVisualCatalog visualCatalog)
        {
            DestroyOwnedTiles();
            backgroundTiles.Clear();
            fallbackTile = null;

            if (visualCatalog == null || visualCatalog.FallbackSprite == null) return;
            fallbackTile = CreateTile(
                visualCatalog.FallbackSprite, visualCatalog.FallbackColour);
            foreach (SurfaceIsometricVisualCatalog.BackgroundEntry entry
                     in visualCatalog.Backgrounds)
            {
                if (entry == null || entry.Sprite == null ||
                    string.IsNullOrWhiteSpace(entry.GameplayDefinitionId)) continue;
                backgroundTiles[entry.GameplayDefinitionId] =
                    CreateTile(entry.Sprite, entry.Colour);
            }
        }

        private Tile CreateTile(Sprite sprite, Color colour)
        {
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = $"Runtime Isometric Tile ({sprite.name})";
            tile.sprite = sprite;
            tile.color = colour;
            tile.colliderType = Tile.ColliderType.None;
            ownedTiles.Add(tile);
            return tile;
        }

        private void DestroyOwnedTiles()
        {
            foreach (Tile tile in ownedTiles)
            {
                if (tile == null) continue;
                if (Application.isPlaying) Destroy(tile);
                else DestroyImmediate(tile);
            }
            ownedTiles.Clear();
        }

        private void OnDestroy()
        {
            DestroyOwnedTiles();
        }
    }
}

