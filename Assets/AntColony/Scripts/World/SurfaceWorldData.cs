using System;
using System.Collections.Generic;

namespace AntColony.World
{
    /// <summary>Deterministic bounded Surface data, separate from Colony terrain.</summary>
    public sealed class SurfaceWorldData
    {
        public const int DevelopmentWidth = 64;
        public const int DevelopmentHeight = 64;
        public const int FutureChunkSize = 32;
        private readonly HashSet<SurfaceCell> obstacles;

        public SurfaceWorldData(int width, int height,
            IEnumerable<SurfaceCell> blockedCells = null)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));
            Width = width;
            Height = height;
            obstacles = blockedCells == null
                ? new HashSet<SurfaceCell>()
                : new HashSet<SurfaceCell>(blockedCells);
        }

        public int Width { get; }
        public int Height { get; }
        public IEnumerable<SurfaceCell> Obstacles => obstacles;
        public bool Contains(SurfaceCell cell) =>
            cell.X >= 0 && cell.Y >= 0 && cell.X < Width && cell.Y < Height;
        public bool IsObstacle(SurfaceCell cell) => obstacles.Contains(cell);
        public bool IsTraversable(SurfaceCell cell) => Contains(cell) && !IsObstacle(cell);

        public static SurfaceWorldData CreateDevelopmentFixture()
        {
            var blocked = new List<SurfaceCell>();
            AddRectangle(blocked, 18, 8, 20, 26);
            AddRectangle(blocked, 34, 28, 48, 30);
            AddRectangle(blocked, 42, 40, 44, 54);
            return new SurfaceWorldData(DevelopmentWidth, DevelopmentHeight, blocked);
        }

        private static void AddRectangle(List<SurfaceCell> cells,
            int minX, int minY, int maxX, int maxY)
        {
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
                cells.Add(new SurfaceCell(x, y));
        }
    }
}
