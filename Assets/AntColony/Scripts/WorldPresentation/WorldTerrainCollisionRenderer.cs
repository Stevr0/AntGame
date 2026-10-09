using AntColony.World;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace AntColony.WorldPresentation
{
    /// <summary>Describes one explicit terrain-collision projection request.</summary>
    public enum WorldTerrainCollisionRefreshStatus
    {
        Success,
        RendererNotConfigured,
        ChunkUnavailable,
        UnknownSolidMaterial
    }

    /// <summary>
    /// Result returned by terrain-collision refresh operations. Projection errors
    /// never mutate authoritative world data.
    /// </summary>
    public readonly struct WorldTerrainCollisionRefreshResult
    {
        public WorldTerrainCollisionRefreshResult(
            WorldTerrainCollisionRefreshStatus status,
            Vector2Int worldCell,
            string materialId = null)
        {
            Status = status;
            WorldCell = worldCell;
            MaterialId = materialId;
        }

        public WorldTerrainCollisionRefreshStatus Status { get; }
        public Vector2Int WorldCell { get; }
        public string MaterialId { get; }
        public bool Succeeded => Status == WorldTerrainCollisionRefreshStatus.Success;
    }

    /// <summary>
    /// Projects blocking Solid Material occupancy into a collision-only Tilemap.
    /// It never reads collider shapes or Tilemap contents as authoritative terrain.
    /// </summary>
    [ExecuteAlways]
    public sealed class WorldTerrainCollisionRenderer : MonoBehaviour
    {
        private const int MaximumSelectedRegionAxis = 16;

        [SerializeField] private MonoBehaviour worldDataProvider;
        [SerializeField] private Tilemap collisionTilemap;
        [SerializeField] private TileBase blockingCollisionTile;
        [SerializeField] private TilemapCollider2D tilemapCollider;
        [SerializeField] private CompositeCollider2D compositeCollider;
        [SerializeField] private bool showCollisionOutlines;
        [SerializeField] private Vector2Int selectedRegionMinimumChunk =
            new Vector2Int(-2, -1);
        [SerializeField] private Vector2Int selectedRegionSizeInChunks =
            new Vector2Int(4, 2);

        private readonly TileBase[] collisionChunkBuffer =
            new TileBase[WorldTileCoordinates.CellsPerChunk];
        private ChunkedWorldData boundWorldData;

        public MonoBehaviour WorldDataProvider => worldDataProvider;
        public Tilemap CollisionTilemap => collisionTilemap;
        public TilemapCollider2D TilemapCollider => tilemapCollider;
        public CompositeCollider2D CompositeCollider => compositeCollider;
        public TileBase BlockingCollisionTile => blockingCollisionTile;
        public bool ShowCollisionOutlines => showCollisionOutlines;
        public Vector2Int SelectedRegionMinimumChunk => selectedRegionMinimumChunk;
        public Vector2Int SelectedRegionSizeInChunks => selectedRegionSizeInChunks;
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
        }

        private void OnDrawGizmos()
        {
            if (!showCollisionOutlines || collisionTilemap == null)
            {
                return;
            }

            Gizmos.color = new Color(0.15f, 0.85f, 1f, 0.9f);
            foreach (Vector3Int cell in collisionTilemap.cellBounds.allPositionsWithin)
            {
                if (collisionTilemap.HasTile(cell))
                {
                    Gizmos.DrawWireCube(
                        collisionTilemap.GetCellCenterWorld(cell),
                        Vector3.one * 0.96f);
                }
            }
        }

        /// <summary>Binds scene-independent world data for tests or runtime setup.</summary>
        public void BindWorldData(ChunkedWorldData worldData)
        {
            boundWorldData = worldData;
        }

        /// <summary>
        /// Assigns the collision projection and collider references. Authored scenes
        /// normally retain these references in serialized fields.
        /// </summary>
        public void SetCollisionReferences(
            Tilemap tilemap,
            TileBase collisionTile,
            TilemapCollider2D sourceCollider,
            CompositeCollider2D mergedCollider)
        {
            collisionTilemap = tilemap;
            blockingCollisionTile = collisionTile;
            tilemapCollider = sourceCollider;
            compositeCollider = mergedCollider;
        }

        /// <summary>
        /// Refreshes one cell and synchronously applies pending Tilemap and composite
        /// geometry changes before returning to development validation code.
        /// </summary>
        public WorldTerrainCollisionRefreshResult RefreshCell(Vector2Int worldCell)
        {
            WorldTerrainCollisionRefreshResult result =
                RefreshCellWithoutSynchronization(worldCell);
            SynchronizeCollisionGeometry();
            PublishDiagnostic("cell refresh", result);
            return result;
        }

        /// <summary>Refreshes one requested 32x32 chunk with one bulk Tilemap write.</summary>
        public WorldTerrainCollisionRefreshResult RefreshChunk(Vector2Int chunkCoordinate)
        {
            WorldTerrainCollisionRefreshResult result =
                RefreshChunkWithoutSynchronization(chunkCoordinate);
            SynchronizeCollisionGeometry();
            PublishDiagnostic($"chunk refresh {chunkCoordinate}", result);
            return result;
        }

        /// <summary>
        /// Refreshes only the bounded chunk rectangle selected in the Inspector,
        /// then rebuilds collider geometry once for the complete operation.
        /// </summary>
        public WorldTerrainCollisionRefreshResult RefreshSelectedRegion()
        {
            WorldTerrainCollisionRefreshResult firstFailure = Success(Vector2Int.zero);
            for (int offsetY = 0; offsetY < selectedRegionSizeInChunks.y; offsetY++)
            {
                for (int offsetX = 0; offsetX < selectedRegionSizeInChunks.x; offsetX++)
                {
                    var chunkCoordinate = new Vector2Int(
                        checked(selectedRegionMinimumChunk.x + offsetX),
                        checked(selectedRegionMinimumChunk.y + offsetY));
                    WorldTerrainCollisionRefreshResult result =
                        RefreshChunkWithoutSynchronization(chunkCoordinate);
                    CaptureFirstFailure(ref firstFailure, result);
                }
            }

            SynchronizeCollisionGeometry();
            PublishDiagnostic("selected-region refresh", firstFailure);
            return firstFailure;
        }

        /// <summary>
        /// Forces Unity to process pending TilemapCollider2D and manually generated
        /// composite geometry before an immediate Physics2D validation query.
        /// </summary>
        public bool SynchronizeCollisionGeometry()
        {
            if (tilemapCollider == null || compositeCollider == null)
            {
                return false;
            }

            if (tilemapCollider.hasTilemapChanges)
            {
                tilemapCollider.ProcessTilemapChanges();
            }

            compositeCollider.GenerateGeometry();
            Physics2D.SyncTransforms();
            return true;
        }

        /// <summary>
        /// Reads the authoritative Solid Material flags for inspection. Returning
        /// false means the cell or material could not be resolved.
        /// </summary>
        public bool TryGetAuthoritativeBlockingState(
            Vector2Int worldCell,
            out bool blocksMovement)
        {
            blocksMovement = false;
            ChunkedWorldData worldData = WorldData;
            if (worldData == null ||
                worldData.GetCell(worldCell, out WorldTileCellData cell) !=
                    WorldCellQueryResult.Available)
            {
                return false;
            }

            if (!cell.HasSolidTile)
            {
                return true;
            }

            if (!worldData.TryGetSolidMaterialDefinition(
                    cell.SolidMaterialId,
                    out SolidTileMaterialDefinition definition))
            {
                return false;
            }

            blocksMovement = definition.BlocksMovement;
            return true;
        }

        /// <summary>Enables or disables development-only collision cell outlines.</summary>
        public void SetCollisionOutlinesVisible(bool visible)
        {
            showCollisionOutlines = visible;
        }

        private WorldTerrainCollisionRefreshResult RefreshCellWithoutSynchronization(
            Vector2Int worldCell)
        {
            if (!IsConfigured(out ChunkedWorldData worldData))
            {
                return new WorldTerrainCollisionRefreshResult(
                    WorldTerrainCollisionRefreshStatus.RendererNotConfigured,
                    worldCell);
            }

            Vector3Int tilemapCell = WorldTilemapRenderer.WorldCellToTilemapCell(worldCell);
            if (worldData.GetCell(worldCell, out WorldTileCellData cell) ==
                WorldCellQueryResult.ChunkUnavailable)
            {
                collisionTilemap.SetTile(tilemapCell, null);
                return new WorldTerrainCollisionRefreshResult(
                    WorldTerrainCollisionRefreshStatus.ChunkUnavailable,
                    worldCell);
            }

            if (!cell.HasSolidTile)
            {
                collisionTilemap.SetTile(tilemapCell, null);
                return Success(worldCell);
            }

            if (!worldData.TryGetSolidMaterialDefinition(
                    cell.SolidMaterialId,
                    out SolidTileMaterialDefinition definition))
            {
                collisionTilemap.SetTile(tilemapCell, null);
                return new WorldTerrainCollisionRefreshResult(
                    WorldTerrainCollisionRefreshStatus.UnknownSolidMaterial,
                    worldCell,
                    cell.SolidMaterialId);
            }

            collisionTilemap.SetTile(
                tilemapCell,
                definition.BlocksMovement ? blockingCollisionTile : null);
            return Success(worldCell);
        }

        private WorldTerrainCollisionRefreshResult RefreshChunkWithoutSynchronization(
            Vector2Int chunkCoordinate)
        {
            Vector2Int minimumWorldCell = WorldTileCoordinates.GetWorldCellCoordinate(
                chunkCoordinate,
                Vector2Int.zero);
            if (!IsConfigured(out ChunkedWorldData worldData))
            {
                return new WorldTerrainCollisionRefreshResult(
                    WorldTerrainCollisionRefreshStatus.RendererNotConfigured,
                    minimumWorldCell);
            }

            if (!worldData.ContainsChunk(chunkCoordinate))
            {
                System.Array.Clear(
                    collisionChunkBuffer,
                    0,
                    collisionChunkBuffer.Length);
                ApplyChunkBuffer(minimumWorldCell);
                return new WorldTerrainCollisionRefreshResult(
                    WorldTerrainCollisionRefreshStatus.ChunkUnavailable,
                    minimumWorldCell);
            }

            WorldTerrainCollisionRefreshResult firstFailure = Success(minimumWorldCell);
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
                    collisionChunkBuffer[index] = null;
                    if (!cell.HasSolidTile)
                    {
                        continue;
                    }

                    if (!worldData.TryGetSolidMaterialDefinition(
                            cell.SolidMaterialId,
                            out SolidTileMaterialDefinition definition))
                    {
                        CaptureFirstFailure(
                            ref firstFailure,
                            new WorldTerrainCollisionRefreshResult(
                                WorldTerrainCollisionRefreshStatus.UnknownSolidMaterial,
                                worldCell,
                                cell.SolidMaterialId));
                        continue;
                    }

                    if (definition.BlocksMovement)
                    {
                        collisionChunkBuffer[index] = blockingCollisionTile;
                    }
                }
            }

            ApplyChunkBuffer(minimumWorldCell);
            return firstFailure;
        }

        private void ApplyChunkBuffer(Vector2Int minimumWorldCell)
        {
            collisionTilemap.SetTilesBlock(
                new BoundsInt(
                    minimumWorldCell.x,
                    minimumWorldCell.y,
                    0,
                    WorldTileCoordinates.ChunkSize,
                    WorldTileCoordinates.ChunkSize,
                    1),
                collisionChunkBuffer);
        }

        private bool IsConfigured(out ChunkedWorldData worldData)
        {
            worldData = WorldData;
            return worldData != null &&
                collisionTilemap != null &&
                blockingCollisionTile != null &&
                tilemapCollider != null &&
                compositeCollider != null;
        }

        private void PublishDiagnostic(
            string operation,
            WorldTerrainCollisionRefreshResult result)
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
                $"Terrain collision {operation} reported {result.Status} at " +
                $"world cell {result.WorldCell}{materialText}. No world data was changed.";
            Debug.LogWarning(LastDiagnostic, this);
        }

        private static void CaptureFirstFailure(
            ref WorldTerrainCollisionRefreshResult firstFailure,
            WorldTerrainCollisionRefreshResult candidate)
        {
            if (firstFailure.Succeeded && !candidate.Succeeded)
            {
                firstFailure = candidate;
            }
        }

        private static WorldTerrainCollisionRefreshResult Success(Vector2Int worldCell)
        {
            return new WorldTerrainCollisionRefreshResult(
                WorldTerrainCollisionRefreshStatus.Success,
                worldCell);
        }
    }
}
