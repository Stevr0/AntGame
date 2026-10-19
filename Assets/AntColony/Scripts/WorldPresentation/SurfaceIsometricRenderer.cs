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
        public const float MinimumSpacingPercent = 90f;
        public const float MaximumSpacingPercent = 100f;

        [Header("ISO-WV-1B Tile Spacing Experiment")]
        [SerializeField, Range(MinimumSpacingPercent, MaximumSpacingPercent)]
        private float horizontalSpacingPercent = MaximumSpacingPercent;
        [SerializeField, Range(MinimumSpacingPercent, MaximumSpacingPercent)]
        private float verticalSpacingPercent = MaximumSpacingPercent;

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

        public IReadOnlyRuntimeCellRegion SurfaceRegion => surfaceRegion;
        public SurfaceIsometricProjection Projection => projection;
        public Grid Grid => grid;
        public Tilemap BackgroundTilemap => backgroundTilemap;
        public SurfaceCell WindowMinimum => windowMinimum;
        public int RenderedCellCount => renderedCellCount;
        public int FallbackCellCount => fallbackCellCount;
        public float HorizontalSpacingPercent => horizontalSpacingPercent;
        public float VerticalSpacingPercent => verticalSpacingPercent;

        public void Configure(IReadOnlyRuntimeCellRegion authoritativeSurface,
            SurfaceIsometricProjection configuredProjection,
            SurfaceIsometricVisualCatalog visualCatalog,
            int presentationLayer)
        {
            surfaceRegion = authoritativeSurface;
            projection = configuredProjection;
            EnsureTilemap(presentationLayer);
            ApplyTileSpacing();
            BuildVisualLookup(visualCatalog);
        }

        /// <summary>
        /// Adjusts only the preview Grid spacing. The accepted projection and
        /// every logical/gameplay coordinate remain unchanged.
        /// </summary>
        public void SetTileSpacingPercent(float horizontalPercent,
            float verticalPercent)
        {
            horizontalSpacingPercent = Mathf.Clamp(horizontalPercent,
                MinimumSpacingPercent, MaximumSpacingPercent);
            verticalSpacingPercent = Mathf.Clamp(verticalPercent,
                MinimumSpacingPercent, MaximumSpacingPercent);
            ApplyTileSpacing();
        }

        public SurfaceIsometricRenderResult RenderWindow(SurfaceCell minimum)
        {
            if (surfaceRegion == null || projection == null ||
                backgroundTilemap == null || fallbackTile == null)
                return new SurfaceIsometricRenderResult(
                    SurfaceIsometricRenderStatus.MissingConfiguration,
                    0, 0, minimum);

            var snapshots = new RuntimeCellSnapshot[PreviewSize, PreviewSize];
            for (int y = 0; y < PreviewSize; y++)
            for (int x = 0; x < PreviewSize; x++)
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
            ApplyTileSpacing();
            renderedCellCount = 0;
            fallbackCellCount = 0;

            for (int y = 0; y < PreviewSize; y++)
            for (int x = 0; x < PreviewSize; x++)
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
            grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;

            var tilemapObject = new GameObject(
                "Authoritative Backgrounds", typeof(Tilemap), typeof(TilemapRenderer));
            tilemapObject.layer = presentationLayer;
            tilemapObject.transform.SetParent(gridObject.transform, false);
            backgroundTilemap = tilemapObject.GetComponent<Tilemap>();
            backgroundTilemap.tileAnchor = Vector3.zero;
            tilemapRenderer = tilemapObject.GetComponent<TilemapRenderer>();
            tilemapRenderer.sortingOrder = -20;
        }

        private void ApplyTileSpacing()
        {
            horizontalSpacingPercent = Mathf.Clamp(horizontalSpacingPercent,
                MinimumSpacingPercent, MaximumSpacingPercent);
            verticalSpacingPercent = Mathf.Clamp(verticalSpacingPercent,
                MinimumSpacingPercent, MaximumSpacingPercent);
            if (grid == null || projection == null) return;

            float baselineWidth = SurfaceIsometricProjection.DefaultTileWidthPixels /
                (float)SurfaceIsometricProjection.DefaultPixelsPerUnit;
            float baselineHeight = SurfaceIsometricProjection.DefaultTileHeightPixels /
                (float)SurfaceIsometricProjection.DefaultPixelsPerUnit;
            grid.cellSize = new Vector3(
                baselineWidth * horizontalSpacingPercent / 100f,
                baselineHeight * verticalSpacingPercent / 100f,
                1f);

            // Scaling absolute cell coordinates would otherwise move this small
            // preview away from its camera. Keep its centre cell on the accepted
            // 100% projection while allowing neighbouring cells to move closer.
            var anchor = new SurfaceCell(
                windowMinimum.X + PreviewSize / 2,
                windowMinimum.Y + PreviewSize / 2);
            grid.transform.position = projection.Origin;
            Vector3 actualAnchor = grid.CellToWorld(LogicalToTilemapCell(anchor));
            Vector2 acceptedAnchor = projection.CellToWorld(anchor);
            grid.transform.position += new Vector3(
                acceptedAnchor.x - actualAnchor.x,
                acceptedAnchor.y - actualAnchor.y,
                0f);
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

        private void OnValidate()
        {
            ApplyTileSpacing();
        }
    }
}

