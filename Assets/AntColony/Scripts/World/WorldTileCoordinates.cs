using System;
using UnityEngine;

namespace AntColony.World
{
    /// <summary>
    /// Owns every conversion between world cells, chunk coordinates, local
    /// chunk cells, and Unity world positions.
    /// </summary>
    public static class WorldTileCoordinates
    {
        public const int ChunkSize = 32;
        public const int CellsPerChunk = ChunkSize * ChunkSize;
        public const float CellWorldSize = 1f;

        /// <summary>
        /// Returns the chunk containing a world cell. C# integer division rounds
        /// toward zero, so the negative remainder must be corrected to obtain
        /// mathematical floor division.
        /// </summary>
        public static Vector2Int GetChunkCoordinate(Vector2Int worldCell)
        {
            return new Vector2Int(
                FloorDivide(worldCell.x, ChunkSize),
                FloorDivide(worldCell.y, ChunkSize));
        }

        /// <summary>
        /// Returns a cell in the inclusive range 0..31 on each axis, including
        /// when the corresponding world coordinate is negative.
        /// </summary>
        public static Vector2Int GetLocalCellCoordinate(Vector2Int worldCell)
        {
            return new Vector2Int(
                PositiveRemainder(worldCell.x, ChunkSize),
                PositiveRemainder(worldCell.y, ChunkSize));
        }

        /// <summary>
        /// Combines a chunk coordinate and validated local coordinate into one
        /// world-cell coordinate.
        /// </summary>
        public static Vector2Int GetWorldCellCoordinate(
            Vector2Int chunkCoordinate,
            Vector2Int localCell)
        {
            ValidateLocalCell(localCell);

            return new Vector2Int(
                checked(chunkCoordinate.x * ChunkSize + localCell.x),
                checked(chunkCoordinate.y * ChunkSize + localCell.y));
        }

        /// <summary>
        /// Maps a cell to its centre in Unity world space. This preserves the
        /// prototype convention that integer coordinates are tile centres.
        /// </summary>
        public static Vector2 CellToWorldPosition(Vector2Int worldCell)
        {
            return new Vector2(
                worldCell.x * CellWorldSize,
                worldCell.y * CellWorldSize);
        }

        /// <summary>
        /// Maps a Unity world position to the cell whose centre owns that point.
        /// Exact half-unit boundaries belong to the cell on the positive side.
        /// </summary>
        public static Vector2Int WorldPositionToCell(Vector2 worldPosition)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPosition.x / CellWorldSize + 0.5f),
                Mathf.FloorToInt(worldPosition.y / CellWorldSize + 0.5f));
        }

        /// <summary>Returns the compact row-major index for a local chunk cell.</summary>
        public static int GetLocalCellIndex(Vector2Int localCell)
        {
            ValidateLocalCell(localCell);
            return localCell.y * ChunkSize + localCell.x;
        }

        private static int FloorDivide(int value, int divisor)
        {
            int quotient = value / divisor;
            int remainder = value % divisor;
            return remainder < 0 ? quotient - 1 : quotient;
        }

        private static int PositiveRemainder(int value, int divisor)
        {
            int remainder = value % divisor;
            return remainder < 0 ? remainder + divisor : remainder;
        }

        private static void ValidateLocalCell(Vector2Int localCell)
        {
            if (localCell.x < 0 || localCell.x >= ChunkSize ||
                localCell.y < 0 || localCell.y >= ChunkSize)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(localCell),
                    localCell,
                    $"Local cells must be inside 0..{ChunkSize - 1} on both axes.");
            }
        }
    }
}
