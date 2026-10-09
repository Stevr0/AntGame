using AntColony.World;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace AntColony.WorldPresentation
{
    /// <summary>Describes the outcome of one explicit Tilemap projection request.</summary>
    public enum WorldTilemapRefreshStatus
    {
        Success,
        RendererNotConfigured,
        ChunkUnavailable,
        UnknownBackgroundMaterial,
        MissingBackgroundVisual,
        UnknownSolidMaterial,
        MissingSolidVisual
    }

    /// <summary>
    /// Diagnostic returned by refresh and material-resolution operations. A
    /// failed projection never changes authoritative world data.
    /// </summary>
    public readonly struct WorldTilemapRefreshResult
    {
        public WorldTilemapRefreshResult(
            WorldTilemapRefreshStatus status,
            Vector2Int worldCell,
            string materialId = null)
        {
            Status = status;
            WorldCell = worldCell;
            MaterialId = materialId;
        }

        public WorldTilemapRefreshStatus Status { get; }
        public Vector2Int WorldCell { get; }
        public string MaterialId { get; }
        public bool Succeeded => Status == WorldTilemapRefreshStatus.Success;
    }

    /// <summary>
    /// Projects explicitly requested authoritative world cells into two Unity
    /// Tilemaps. Tilemaps are write-only presentation here; they are never read
    /// to infer terrain state or mutate <see cref="ChunkedWorldData"/>.
    /// </summary>
    [ExecuteAlways]
    public sealed class WorldTilemapRenderer : MonoBehaviour
    {
        private const int MaximumSelectedRegionAxis = 16;

        [SerializeField] private MonoBehaviour worldDataProvider;
        [SerializeField] private Tilemap backgroundTilemap;
        [SerializeField] private Tilemap foregroundTilemap;
        [SerializeField] private bool backgroundVisible = true;
        [SerializeField] private bool foregroundVisible = true;
        [SerializeField] private Vector2Int selectedRegionMinimumChunk =
            new Vector2Int(-2, -1);
        [SerializeField] private Vector2Int selectedRegionSizeInChunks =
            new Vector2Int(4, 2);

        private readonly TileBase[] backgroundChunkBuffer =
            new TileBase[WorldTileCoordinates.CellsPerChunk];
        private readonly TileBase[] foregroundChunkBuffer =
            new TileBase[WorldTileCoordinates.CellsPerChunk];
        private ChunkedWorldData boundWorldData;

        public MonoBehaviour WorldDataProvider => worldDataProvider;
        public Tilemap BackgroundTilemap => backgroundTilemap;
        public Tilemap ForegroundTilemap => foregroundTilemap;
        public Vector2Int SelectedRegionMinimumChunk => selectedRegionMinimumChunk;
        public Vector2Int SelectedRegionSizeInChunks => selectedRegionSizeInChunks;
        public bool BackgroundVisible => backgroundVisible;
        public bool ForegroundVisible => foregroundVisible;
        public string LastDiagnostic { get; private set; } = string.Empty;

        private ChunkedWorldData WorldData =>
            boundWorldData ??
            (worldDataProvider is IWorldDataProvider provider &&
                provider.IsWorldDataAvailable
                    ? provider.WorldData
                    : null);

        private void OnValidate()
        {
            selectedRegionSizeInChunks.x = Mathf.Clamp(
                selectedRegionSizeInChunks.x,
                1,
                MaximumSelectedRegionAxis);
            selectedRegionSizeInChunks.y = Mathf.Clamp(
                selectedRegionSizeInChunks.y,
                1,
                MaximumSelectedRegionAxis);
            ApplyVisibility();
        }

        /// <summary>
        /// Binds scene-independent world data for programmatic users and tests.
        /// Authored scenes normally use the serialized IWorldDataProvider component.
        /// </summary>
        public void BindWorldData(ChunkedWorldData worldData)
        {
            boundWorldData = worldData;
        }

        /// <summary>
        /// Assigns the two projection targets. This is useful for test fixtures;
        /// authored scenes normally store these references in the Inspector.
        /// </summary>
        public void SetTilemapReferences(Tilemap background, Tilemap foreground)
        {
            backgroundTilemap = background;
            foregroundTilemap = foreground;
            ApplyVisibility();
        }

        /// <summary>
        /// Refreshes exactly one coordinate. An unavailable cell clears only its
        /// two projected tiles and never allocates the missing chunk.
        /// </summary>
        public WorldTilemapRefreshResult RefreshCell(Vector2Int worldCell)
        {
            WorldTilemapRefreshResult result = RefreshCellWithoutDiagnostic(worldCell);
            PublishDiagnostic("cell refresh", result);
            return result;
        }

        /// <summary>
        /// Refreshes one explicitly requested 32x32 chunk with two bulk Tilemap
        /// writes. Missing chunks are cleared from presentation, not generated.
        /// </summary>
        public WorldTilemapRefreshResult RefreshChunk(Vector2Int chunkCoordinate)
        {
            WorldTilemapRefreshResult result = RefreshChunkWithoutDiagnostic(chunkCoordinate);
            PublishDiagnostic($"chunk refresh {chunkCoordinate}", result);
            return result;
        }

        /// <summary>
        /// Refreshes only the bounded chunk rectangle selected in the Inspector.
        /// This does not enumerate or scan unrelated resident chunks.
        /// </summary>
        public WorldTilemapRefreshResult RefreshSelectedRegion()
        {
            WorldTilemapRefreshResult firstFailure = Success(Vector2Int.zero);

            for (int offsetY = 0; offsetY < selectedRegionSizeInChunks.y; offsetY++)
            {
                for (int offsetX = 0; offsetX < selectedRegionSizeInChunks.x; offsetX++)
                {
                    var chunkCoordinate = new Vector2Int(
                        checked(selectedRegionMinimumChunk.x + offsetX),
                        checked(selectedRegionMinimumChunk.y + offsetY));
                    WorldTilemapRefreshResult result =
                        RefreshChunkWithoutDiagnostic(chunkCoordinate);
                    if (firstFailure.Succeeded && !result.Succeeded)
                    {
                        firstFailure = result;
                    }
                }
            }

            PublishDiagnostic("selected-region refresh", firstFailure);
            return firstFailure;
        }

        /// <summary>Controls Background Tilemap presentation only.</summary>
        public void SetBackgroundVisible(bool visible)
        {
            backgroundVisible = visible;
            ApplyVisibility();
        }

        /// <summary>Controls foreground Solid Tilemap presentation only.</summary>
        public void SetForegroundVisible(bool visible)
        {
            foregroundVisible = visible;
            ApplyVisibility();
        }

        /// <summary>
        /// Maps authoritative world-cell coordinates directly to Tilemap indices.
        /// The scene Grid offset, not this integer identity mapping, aligns centres.
        /// </summary>
        public static Vector3Int WorldCellToTilemapCell(Vector2Int worldCell)
        {
            return new Vector3Int(worldCell.x, worldCell.y, 0);
        }

        /// <summary>
        /// Converts a clicked world position through the authored Background
        /// Tilemap Grid. This keeps development selection aligned with the exact
        /// presentation cell even if the Grid transform changes later.
        /// </summary>
        public Vector2Int WorldPositionToWorldCell(Vector3 worldPosition)
        {
            if (backgroundTilemap == null)
            {
                return WorldTileCoordinates.WorldPositionToCell(worldPosition);
            }

            Vector3Int tilemapCell = backgroundTilemap.WorldToCell(worldPosition);
            return new Vector2Int(tilemapCell.x, tilemapCell.y);
        }

        /// <summary>Resolves a Background visual without substituting another tile.</summary>
        public static WorldTilemapRefreshResult ResolveBackgroundVisual(
            ChunkedWorldData worldData,
            string materialId,
            Vector2Int worldCell,
            out TileBase visualTile)
        {
            visualTile = null;
            if (worldData == null ||
                !worldData.TryGetBackgroundMaterialDefinition(
                    materialId,
                    out BackgroundTileMaterialDefinition definition))
            {
                return new WorldTilemapRefreshResult(
                    WorldTilemapRefreshStatus.UnknownBackgroundMaterial,
                    worldCell,
                    materialId);
            }

            visualTile = definition.VisualTile;
            return visualTile != null
                ? Success(worldCell)
                : new WorldTilemapRefreshResult(
                    WorldTilemapRefreshStatus.MissingBackgroundVisual,
                    worldCell,
                    materialId);
        }

        /// <summary>Resolves a Solid visual without consulting movement flags.</summary>
        public static WorldTilemapRefreshResult ResolveSolidVisual(
            ChunkedWorldData worldData,
            string materialId,
            Vector2Int worldCell,
            out TileBase visualTile)
        {
            visualTile = null;
            if (worldData == null ||
                !worldData.TryGetSolidMaterialDefinition(
                    materialId,
                    out SolidTileMaterialDefinition definition))
            {
                return new WorldTilemapRefreshResult(
                    WorldTilemapRefreshStatus.UnknownSolidMaterial,
                    worldCell,
                    materialId);
            }

            visualTile = definition.VisualTile;
            return visualTile != null
                ? Success(worldCell)
                : new WorldTilemapRefreshResult(
                    WorldTilemapRefreshStatus.MissingSolidVisual,
                    worldCell,
                    materialId);
        }

        private WorldTilemapRefreshResult RefreshCellWithoutDiagnostic(
            Vector2Int worldCell)
        {
            if (!IsConfigured(out ChunkedWorldData worldData))
            {
                return new WorldTilemapRefreshResult(
                    WorldTilemapRefreshStatus.RendererNotConfigured,
                    worldCell);
            }

            Vector3Int tilemapCell = WorldCellToTilemapCell(worldCell);
            if (worldData.GetCell(worldCell, out WorldTileCellData cell) ==
                WorldCellQueryResult.ChunkUnavailable)
            {
                SetAndRefreshTile(backgroundTilemap, tilemapCell, null);
                SetAndRefreshTile(foregroundTilemap, tilemapCell, null);
                return new WorldTilemapRefreshResult(
                    WorldTilemapRefreshStatus.ChunkUnavailable,
                    worldCell);
            }

            WorldTilemapRefreshResult firstFailure = ResolveBackgroundVisual(
                worldData,
                cell.BackgroundMaterialId,
                worldCell,
                out TileBase backgroundVisual);
            SetAndRefreshTile(backgroundTilemap, tilemapCell, backgroundVisual);

            if (!cell.HasSolidTile)
            {
                SetAndRefreshTile(foregroundTilemap, tilemapCell, null);
                return firstFailure;
            }

            WorldTilemapRefreshResult solidResult = ResolveSolidVisual(
                worldData,
                cell.SolidMaterialId,
                worldCell,
                out TileBase solidVisual);
            SetAndRefreshTile(foregroundTilemap, tilemapCell, solidVisual);
            return firstFailure.Succeeded ? solidResult : firstFailure;
        }

        private static void SetAndRefreshTile(
            Tilemap tilemap,
            Vector3Int tilemapCell,
            TileBase tile)
        {
            tilemap.SetTile(tilemapCell, tile);

            // SetTile updates Tilemap data immediately, but an explicit refresh
            // also invalidates the rendered chunk when two materials share the
            // same source Sprite and differ only by their Tile tint.
            tilemap.RefreshTile(tilemapCell);
        }

        private WorldTilemapRefreshResult RefreshChunkWithoutDiagnostic(
            Vector2Int chunkCoordinate)
        {
            Vector2Int minimumWorldCell = WorldTileCoordinates.GetWorldCellCoordinate(
                chunkCoordinate,
                Vector2Int.zero);
            if (!IsConfigured(out ChunkedWorldData worldData))
            {
                return new WorldTilemapRefreshResult(
                    WorldTilemapRefreshStatus.RendererNotConfigured,
                    minimumWorldCell);
            }

            if (!worldData.ContainsChunk(chunkCoordinate))
            {
                ClearChunkBuffers();
                ApplyChunkBuffers(minimumWorldCell);
                return new WorldTilemapRefreshResult(
                    WorldTilemapRefreshStatus.ChunkUnavailable,
                    minimumWorldCell);
            }

            WorldTilemapRefreshResult firstFailure = Success(minimumWorldCell);
            for (int localY = 0; localY < WorldTileCoordinates.ChunkSize; localY++)
            {
                for (int localX = 0; localX < WorldTileCoordinates.ChunkSize; localX++)
                {
                    var localCell = new Vector2Int(localX, localY);
                    Vector2Int worldCell = WorldTileCoordinates.GetWorldCellCoordinate(
                        chunkCoordinate,
                        localCell);
                    int index = WorldTileCoordinates.GetLocalCellIndex(localCell);
                    worldData.GetCell(worldCell, out WorldTileCellData cell);

                    WorldTilemapRefreshResult backgroundResult = ResolveBackgroundVisual(
                        worldData,
                        cell.BackgroundMaterialId,
                        worldCell,
                        out backgroundChunkBuffer[index]);
                    CaptureFirstFailure(ref firstFailure, backgroundResult);

                    if (!cell.HasSolidTile)
                    {
                        foregroundChunkBuffer[index] = null;
                        continue;
                    }

                    WorldTilemapRefreshResult solidResult = ResolveSolidVisual(
                        worldData,
                        cell.SolidMaterialId,
                        worldCell,
                        out foregroundChunkBuffer[index]);
                    CaptureFirstFailure(ref firstFailure, solidResult);
                }
            }

            ApplyChunkBuffers(minimumWorldCell);
            return firstFailure;
        }

        private void ApplyChunkBuffers(Vector2Int minimumWorldCell)
        {
            var bounds = new BoundsInt(
                minimumWorldCell.x,
                minimumWorldCell.y,
                0,
                WorldTileCoordinates.ChunkSize,
                WorldTileCoordinates.ChunkSize,
                1);
            backgroundTilemap.SetTilesBlock(bounds, backgroundChunkBuffer);
            foregroundTilemap.SetTilesBlock(bounds, foregroundChunkBuffer);
        }

        private void ClearChunkBuffers()
        {
            System.Array.Clear(backgroundChunkBuffer, 0, backgroundChunkBuffer.Length);
            System.Array.Clear(foregroundChunkBuffer, 0, foregroundChunkBuffer.Length);
        }

        private bool IsConfigured(out ChunkedWorldData worldData)
        {
            worldData = WorldData;
            return worldData != null &&
                backgroundTilemap != null &&
                foregroundTilemap != null;
        }

        private void ApplyVisibility()
        {
            if (backgroundTilemap != null &&
                backgroundTilemap.TryGetComponent(out TilemapRenderer backgroundRenderer))
            {
                backgroundRenderer.enabled = backgroundVisible;
            }

            if (foregroundTilemap != null &&
                foregroundTilemap.TryGetComponent(out TilemapRenderer foregroundRenderer))
            {
                foregroundRenderer.enabled = foregroundVisible;
            }
        }

        private void PublishDiagnostic(
            string operation,
            WorldTilemapRefreshResult result)
        {
            if (result.Succeeded)
            {
                LastDiagnostic = string.Empty;
                return;
            }

            string materialText = string.IsNullOrEmpty(result.MaterialId)
                ? string.Empty
                : $", material '{result.MaterialId}'";
            LastDiagnostic =
                $"World Tilemap {operation} reported {result.Status} at " +
                $"world cell {result.WorldCell}{materialText}. No world data was changed.";
            Debug.LogWarning(LastDiagnostic, this);
        }

        private static void CaptureFirstFailure(
            ref WorldTilemapRefreshResult firstFailure,
            WorldTilemapRefreshResult candidate)
        {
            if (firstFailure.Succeeded && !candidate.Succeeded)
            {
                firstFailure = candidate;
            }
        }

        private static WorldTilemapRefreshResult Success(Vector2Int worldCell)
        {
            return new WorldTilemapRefreshResult(
                WorldTilemapRefreshStatus.Success,
                worldCell);
        }
    }
}
