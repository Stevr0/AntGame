using UnityEngine;

namespace AntColony.World
{
    /// <summary>
    /// Stores one resident 32x32 chunk. Arrays keep all 1,024 cells compact and
    /// avoid allocating an object for every tile.
    /// </summary>
    public sealed class WorldChunk
    {
        private readonly string[] backgroundMaterialIds =
            new string[WorldTileCoordinates.CellsPerChunk];
        private readonly string[] solidMaterialIds =
            new string[WorldTileCoordinates.CellsPerChunk];

        internal WorldChunk(Vector2Int chunkCoordinate, string backgroundMaterialId)
        {
            ChunkCoordinate = chunkCoordinate;

            // Every generated cell receives a real Background Tile immediately.
            // Missing chunks remain unavailable and are never represented by this class.
            for (int index = 0; index < backgroundMaterialIds.Length; index++)
            {
                backgroundMaterialIds[index] = backgroundMaterialId;
            }
        }

        public Vector2Int ChunkCoordinate { get; }
        public int CellCount => WorldTileCoordinates.CellsPerChunk;

        internal string GetBackgroundMaterialId(Vector2Int localCell)
        {
            return backgroundMaterialIds[WorldTileCoordinates.GetLocalCellIndex(localCell)];
        }

        internal string GetSolidMaterialId(Vector2Int localCell)
        {
            return solidMaterialIds[WorldTileCoordinates.GetLocalCellIndex(localCell)];
        }

        internal void SetBackgroundMaterialId(Vector2Int localCell, string materialId)
        {
            backgroundMaterialIds[WorldTileCoordinates.GetLocalCellIndex(localCell)] = materialId;
        }

        internal void SetSolidMaterialId(Vector2Int localCell, string materialId)
        {
            solidMaterialIds[WorldTileCoordinates.GetLocalCellIndex(localCell)] = materialId;
        }

        internal bool RemoveSolidMaterial(Vector2Int localCell)
        {
            int index = WorldTileCoordinates.GetLocalCellIndex(localCell);
            if (solidMaterialIds[index] == null)
            {
                return false;
            }

            solidMaterialIds[index] = null;
            return true;
        }
    }
}
