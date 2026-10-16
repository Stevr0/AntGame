using System;
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
        SolidTileNotPresent,
        AuthorityRejected
    }

    /// <summary>
    /// Optional scenario-local guard for Solid replacement/removal after a
    /// Resource Node authority has been published. World data still owns the
    /// Solid; the guard only prevents a terrain write from bypassing resource
    /// conservation.
    /// </summary>
    public interface IWorldSolidMutationGuard
    {
        bool CanReplaceSolid(Vector2Int cell, string currentSolidId,
            string replacementSolidId, string authorityToken, out string reason);
        bool CanRemoveSolid(Vector2Int cell, string currentSolidId,
            string authorityToken, out string reason);
    }

    /// <summary>Notification emitted only after an authoritative terrain write.</summary>
    public readonly struct WorldCellAuthorityChange
    {
        public WorldCellAuthorityChange(Vector2Int worldCell, long oldRevision,
            long newRevision, RuntimeCellChangeCategory category)
        {
            WorldCell = worldCell;
            OldRevision = oldRevision;
            NewRevision = newRevision;
            Category = category;
        }

        public Vector2Int WorldCell { get; }
        public long OldRevision { get; }
        public long NewRevision { get; }
        public RuntimeCellChangeCategory Category { get; }
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
        private readonly Dictionary<Vector2Int, long> cellRevisions = new();
        private readonly WorldMaterialRegistry materials;
        private IWorldSolidMutationGuard solidMutationGuard;

        public ChunkedWorldData(WorldMaterialRegistry materials, long worldSeed = 0)
        {
            this.materials = materials ??
                throw new System.ArgumentNullException(nameof(materials));
            WorldSeed = worldSeed;
        }

        /// <summary>
        /// Scenario seed metadata. Chunk queries remain passive and never invoke
        /// generation, so changing views cannot recreate mutated terrain.
        /// </summary>
        public long WorldSeed { get; }

        public int ResidentChunkCount => chunks.Count;
        public event Action<WorldCellAuthorityChange> CellChanged;
        public float MaximumBackgroundMovementSpeedMultiplier =>
            materials.MaximumBackgroundMovementSpeedMultiplier;

        public string LastSolidMutationRejection { get; private set; } = string.Empty;

        public void BindSolidMutationGuard(IWorldSolidMutationGuard guard)
        {
            solidMutationGuard = guard;
        }

        public void UnbindSolidMutationGuard(IWorldSolidMutationGuard guard)
        {
            if (ReferenceEquals(solidMutationGuard, guard)) solidMutationGuard = null;
        }

        public bool ContainsChunk(Vector2Int chunkCoordinate)
        {
            return chunks.ContainsKey(chunkCoordinate);
        }

        /// <summary>Returns a stable copy so callers cannot mutate chunk ownership.</summary>
        public IReadOnlyList<Vector2Int> GetResidentChunkCoordinates()
        {
            var result = new Vector2Int[chunks.Count];
            chunks.Keys.CopyTo(result, 0);
            System.Array.Sort(result, CompareCoordinates);
            return result;
        }

        public long GetCellRevision(Vector2Int worldCell) =>
            cellRevisions.TryGetValue(worldCell, out long revision) ? revision : 0L;

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
            if (string.Equals(chunk.GetBackgroundMaterialId(localCell),
                    backgroundMaterialId, StringComparison.Ordinal))
                return WorldCellMutationResult.Success;
            chunk.SetBackgroundMaterialId(localCell, backgroundMaterialId);
            PublishChange(worldCell, RuntimeCellChangeCategory.Background);
            return WorldCellMutationResult.Success;
        }

        public WorldCellMutationResult SetSolidMaterial(
            Vector2Int worldCell,
            string solidMaterialId)
        {
            return SetSolidMaterial(worldCell, solidMaterialId, string.Empty);
        }

        public WorldCellMutationResult SetSolidMaterial(
            Vector2Int worldCell,
            string solidMaterialId,
            string authorityToken)
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
            string currentSolidId = chunk.GetSolidMaterialId(localCell);
            if (string.Equals(currentSolidId,
                    solidMaterialId, StringComparison.Ordinal))
                return WorldCellMutationResult.Success;
            if (solidMutationGuard != null &&
                !solidMutationGuard.CanReplaceSolid(worldCell, currentSolidId,
                    solidMaterialId, authorityToken, out string rejection))
            {
                LastSolidMutationRejection = rejection ?? string.Empty;
                return WorldCellMutationResult.AuthorityRejected;
            }
            chunk.SetSolidMaterialId(localCell, solidMaterialId);
            LastSolidMutationRejection = string.Empty;
            PublishChange(worldCell, RuntimeCellChangeCategory.Node);
            return WorldCellMutationResult.Success;
        }

        public WorldCellMutationResult RemoveSolidMaterial(Vector2Int worldCell)
        {
            return RemoveSolidMaterial(worldCell, string.Empty);
        }

        public WorldCellMutationResult RemoveSolidMaterial(Vector2Int worldCell,
            string authorityToken)
        {
            if (!TryGetCellChunk(worldCell, out WorldChunk chunk, out Vector2Int localCell))
            {
                return WorldCellMutationResult.ChunkUnavailable;
            }

            string currentSolidId = chunk.GetSolidMaterialId(localCell);
            if (currentSolidId == null)
                return WorldCellMutationResult.SolidTileNotPresent;
            if (solidMutationGuard != null &&
                !solidMutationGuard.CanRemoveSolid(worldCell, currentSolidId,
                    authorityToken, out string rejection))
            {
                LastSolidMutationRejection = rejection ?? string.Empty;
                return WorldCellMutationResult.AuthorityRejected;
            }
            if (!chunk.RemoveSolidMaterial(localCell))
                return WorldCellMutationResult.SolidTileNotPresent;
            LastSolidMutationRejection = string.Empty;
            PublishChange(worldCell, RuntimeCellChangeCategory.Node);
            return WorldCellMutationResult.Success;
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

        private void PublishChange(Vector2Int worldCell,
            RuntimeCellChangeCategory category)
        {
            long oldRevision = GetCellRevision(worldCell);
            long newRevision = oldRevision + 1L;
            cellRevisions[worldCell] = newRevision;
            CellChanged?.Invoke(new WorldCellAuthorityChange(worldCell,
                oldRevision, newRevision, category));
        }

        private static int CompareCoordinates(Vector2Int left, Vector2Int right)
        {
            int xComparison = left.x.CompareTo(right.x);
            return xComparison != 0 ? xComparison : left.y.CompareTo(right.y);
        }
    }
}
