using System.Collections.Generic;
using UnityEngine;

namespace AntColony.World
{
    public enum WorldChunkCreationResult
    {
        Success,
        ChunkAlreadyExists,
        InvalidBackgroundMaterial
    }

    public enum WorldCellQueryResult
    {
        Available,
        ChunkUnavailable
    }

    public enum WorldCellMutationResult
    {
        Success,
        ChunkUnavailable,
        InvalidMaterial,
        SolidTileNotPresent
    }

    /// <summary>
    /// Immutable value returned by cell queries. A null SolidMaterialId means a
    /// generated, exposed cell; it never means that the chunk was unavailable.
    /// </summary>
    public readonly struct WorldTileCellData
    {
        public WorldTileCellData(string backgroundMaterialId, string solidMaterialId)
        {
            BackgroundMaterialId = backgroundMaterialId;
            SolidMaterialId = solidMaterialId;
        }

        public string BackgroundMaterialId { get; }
        public string SolidMaterialId { get; }
        public bool HasSolidTile => SolidMaterialId != null;
    }

    /// <summary>
    /// Authoritative, scene-independent owner of resident world chunks. Chunks
    /// are created only through CreateChunk; reads never generate terrain.
    /// </summary>
    public sealed class ChunkedWorldData
    {
        private readonly Dictionary<Vector2Int, WorldChunk> chunks = new();
        private readonly WorldMaterialRegistry materials;

        public ChunkedWorldData(WorldMaterialRegistry materials, int worldSeed = 0)
        {
            this.materials = materials ??
                throw new System.ArgumentNullException(nameof(materials));
            WorldSeed = worldSeed;
        }

        /// <summary>
        /// Reserved for future deterministic generation. This milestone does not
        /// generate chunks automatically or interpret the seed.
        /// </summary>
        public int WorldSeed { get; }

        public int ResidentChunkCount => chunks.Count;

        public bool ContainsChunk(Vector2Int chunkCoordinate)
        {
            return chunks.ContainsKey(chunkCoordinate);
        }

        public WorldChunkCreationResult CreateChunk(
            Vector2Int chunkCoordinate,
            string backgroundMaterialId)
        {
            if (!materials.ContainsBackgroundMaterial(backgroundMaterialId))
            {
                return WorldChunkCreationResult.InvalidBackgroundMaterial;
            }

            if (chunks.ContainsKey(chunkCoordinate))
            {
                return WorldChunkCreationResult.ChunkAlreadyExists;
            }

            chunks.Add(
                chunkCoordinate,
                new WorldChunk(chunkCoordinate, backgroundMaterialId));
            return WorldChunkCreationResult.Success;
        }

        public WorldCellQueryResult GetCell(
            Vector2Int worldCell,
            out WorldTileCellData cellData)
        {
            if (!TryGetCellChunk(worldCell, out WorldChunk chunk, out Vector2Int localCell))
            {
                cellData = default;
                return WorldCellQueryResult.ChunkUnavailable;
            }

            cellData = new WorldTileCellData(
                chunk.GetBackgroundMaterialId(localCell),
                chunk.GetSolidMaterialId(localCell));
            return WorldCellQueryResult.Available;
        }

        public WorldCellQueryResult GetBackgroundMaterialId(
            Vector2Int worldCell,
            out string materialId)
        {
            WorldCellQueryResult result = GetCell(worldCell, out WorldTileCellData cellData);
            materialId = result == WorldCellQueryResult.Available
                ? cellData.BackgroundMaterialId
                : null;
            return result;
        }

        public WorldCellQueryResult GetSolidMaterialId(
            Vector2Int worldCell,
            out string materialId)
        {
            WorldCellQueryResult result = GetCell(worldCell, out WorldTileCellData cellData);
            materialId = result == WorldCellQueryResult.Available
                ? cellData.SolidMaterialId
                : null;
            return result;
        }

        public WorldCellMutationResult SetBackgroundMaterial(
            Vector2Int worldCell,
            string backgroundMaterialId)
        {
            if (!materials.ContainsBackgroundMaterial(backgroundMaterialId))
            {
                return WorldCellMutationResult.InvalidMaterial;
            }

            if (!TryGetCellChunk(worldCell, out WorldChunk chunk, out Vector2Int localCell))
            {
                return WorldCellMutationResult.ChunkUnavailable;
            }

            // The material can change, but the Background Tile itself can never
            // be removed or replaced with a missing value.
            chunk.SetBackgroundMaterialId(localCell, backgroundMaterialId);
            return WorldCellMutationResult.Success;
        }

        public WorldCellMutationResult SetSolidMaterial(
            Vector2Int worldCell,
            string solidMaterialId)
        {
            if (!materials.ContainsSolidMaterial(solidMaterialId))
            {
                return WorldCellMutationResult.InvalidMaterial;
            }

            if (!TryGetCellChunk(worldCell, out WorldChunk chunk, out Vector2Int localCell))
            {
                return WorldCellMutationResult.ChunkUnavailable;
            }

            // One array slot represents the optional Solid Tile, so assigning a
            // material can never create two authoritative solids at one cell.
            chunk.SetSolidMaterialId(localCell, solidMaterialId);
            return WorldCellMutationResult.Success;
        }

        public WorldCellMutationResult RemoveSolidMaterial(Vector2Int worldCell)
        {
            if (!TryGetCellChunk(worldCell, out WorldChunk chunk, out Vector2Int localCell))
            {
                return WorldCellMutationResult.ChunkUnavailable;
            }

            return chunk.RemoveSolidMaterial(localCell)
                ? WorldCellMutationResult.Success
                : WorldCellMutationResult.SolidTileNotPresent;
        }

        /// <summary>
        /// Resolves the Background Material registered with this world. Visual
        /// projections use this access so they cannot accidentally pair the cell
        /// IDs with a different material registry.
        /// </summary>
        public bool TryGetBackgroundMaterialDefinition(
            string materialId,
            out BackgroundTileMaterialDefinition definition)
        {
            return materials.TryGetBackgroundMaterial(materialId, out definition);
        }

        /// <summary>
        /// Resolves the Solid Material registered with this world. Classification
        /// and presentation therefore use the same definitions that validated
        /// authoritative writes.
        /// </summary>
        public bool TryGetSolidMaterialDefinition(
            string materialId,
            out SolidTileMaterialDefinition definition)
        {
            return materials.TryGetSolidMaterial(materialId, out definition);
        }

        private bool TryGetCellChunk(
            Vector2Int worldCell,
            out WorldChunk chunk,
            out Vector2Int localCell)
        {
            Vector2Int chunkCoordinate =
                WorldTileCoordinates.GetChunkCoordinate(worldCell);
            localCell = WorldTileCoordinates.GetLocalCellCoordinate(worldCell);

            // Missing chunks are explicitly unavailable. A read must never turn
            // ungenerated territory into exposed terrain or allocate neighbours.
            return chunks.TryGetValue(chunkCoordinate, out chunk);
        }
    }
}
