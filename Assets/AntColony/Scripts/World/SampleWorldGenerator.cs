using System;
using UnityEngine;

namespace AntColony.World
{
    /// <summary>
    /// Stable material IDs used by the bounded development-world generator.
    /// Keeping IDs in one value makes the generator independent of scene assets.
    /// </summary>
    public readonly struct SampleWorldMaterialIds
    {
        public SampleWorldMaterialIds(
            string skyBackground,
            string earthBackground,
            string stoneBackground,
            string preparedBackground,
            string soilSolid,
            string stoneSolid)
        {
            SkyBackground = skyBackground;
            EarthBackground = earthBackground;
            StoneBackground = stoneBackground;
            PreparedBackground = preparedBackground;
            SoilSolid = soilSolid;
            StoneSolid = stoneSolid;
        }

        public string SkyBackground { get; }
        public string EarthBackground { get; }
        public string StoneBackground { get; }
        public string PreparedBackground { get; }
        public string SoilSolid { get; }
        public string StoneSolid { get; }
    }

    /// <summary>Immutable result and bounds for one generated sample world.</summary>
    public sealed class GeneratedSampleWorld
    {
        public GeneratedSampleWorld(
            ChunkedWorldData worldData,
            Vector2Int minimumChunk,
            Vector2Int sizeInChunks,
            int[] surfaceHeights,
            Vector2Int gripFixtureBottom,
            Vector2Int gripFixtureTop)
        {
            WorldData = worldData;
            MinimumChunk = minimumChunk;
            SizeInChunks = sizeInChunks;
            SurfaceHeights = surfaceHeights;
            GripFixtureBottom = gripFixtureBottom;
            GripFixtureTop = gripFixtureTop;
        }

        public ChunkedWorldData WorldData { get; }
        public Vector2Int MinimumChunk { get; }
        public Vector2Int SizeInChunks { get; }
        public int[] SurfaceHeights { get; }
        public Vector2Int GripFixtureBottom { get; }
        public Vector2Int GripFixtureTop { get; }
        public int WidthInCells => SizeInChunks.x * WorldTileCoordinates.ChunkSize;
        public int HeightInCells => SizeInChunks.y * WorldTileCoordinates.ChunkSize;
    }

    /// <summary>
    /// Builds the small, deterministic world used by AntGameDevelopment. It only
    /// writes through <see cref="ChunkedWorldData"/> and never creates chunks in
    /// response to a query.
    /// </summary>
    public static class SampleWorldGenerator
    {
        public static readonly Vector2Int DefaultMinimumChunk = new(-2, -2);
        public static readonly Vector2Int DefaultSizeInChunks = new(4, 4);
        public const int StoneBackgroundMaximumY = -24;
        public const int MainPassageY = -8;
        public const int GripFixtureX = -12;
        public const int GripFixtureHeight = 7;
        public static readonly Vector2Int GridDirectDestination = new(-2, MainPassageY);
        public static readonly Vector2Int GridDetourDestination = new(18, MainPassageY);
        public static readonly Vector2Int GridChunkBoundaryDestination = new(33, MainPassageY);
        public static readonly Vector2Int GridBlockingCell = new(10, MainPassageY);
        public static readonly Vector2Int GridUnreachableDestination = new(12, -16);
        public static readonly Vector2Int GridFallFixture = new(20, MainPassageY);

        /// <summary>Generates all data first; scene projection is a separate step.</summary>
        public static GeneratedSampleWorld Generate(
            WorldMaterialRegistry registry,
            SampleWorldMaterialIds materials,
            int seed,
            Vector2Int minimumChunk,
            Vector2Int sizeInChunks)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            if (sizeInChunks.x <= 0 || sizeInChunks.y <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sizeInChunks),
                    "The sample world must contain at least one chunk on each axis.");
            }

            ValidateMaterialIds(registry, materials);
            var worldData = new ChunkedWorldData(registry, seed);

            for (int chunkY = 0; chunkY < sizeInChunks.y; chunkY++)
            {
                for (int chunkX = 0; chunkX < sizeInChunks.x; chunkX++)
                {
                    RequireSuccess(worldData.CreateChunk(
                        minimumChunk + new Vector2Int(chunkX, chunkY),
                        materials.SkyBackground));
                }
            }

            int minimumX = minimumChunk.x * WorldTileCoordinates.ChunkSize;
            int minimumY = minimumChunk.y * WorldTileCoordinates.ChunkSize;
            int width = sizeInChunks.x * WorldTileCoordinates.ChunkSize;
            int height = sizeInChunks.y * WorldTileCoordinates.ChunkSize;
            var surfaceHeights = new int[width];

            for (int localX = 0; localX < width; localX++)
            {
                int worldX = minimumX + localX;
                int surfaceY = GetSurfaceHeight(worldX, seed);
                surfaceHeights[localX] = surfaceY;

                for (int localY = 0; localY < height; localY++)
                {
                    int worldY = minimumY + localY;
                    if (worldY > surfaceY)
                    {
                        continue;
                    }

                    var worldCell = new Vector2Int(worldX, worldY);
                    bool isStone = worldY <= StoneBackgroundMaximumY;
                    RequireSuccess(worldData.SetBackgroundMaterial(
                        worldCell,
                        isStone ? materials.StoneBackground : materials.EarthBackground));
                    RequireSuccess(worldData.SetSolidMaterial(
                        worldCell,
                        worldY >= surfaceY - 7
                            ? materials.SoilSolid
                            : materials.StoneSolid));
                }
            }

            CarveSampleGeometry(worldData, materials, minimumX, minimumY, width, height);
            Vector2Int gripFixtureBottom = PrepareSurfaceGripFixture(
                worldData,
                materials,
                seed,
                minimumX,
                minimumY,
                width,
                height,
                out Vector2Int gripFixtureTop);
            PrepareGridNavigationFixtures(
                worldData,
                materials,
                gripFixtureBottom,
                minimumX,
                minimumY,
                width,
                height);
            return new GeneratedSampleWorld(
                worldData,
                minimumChunk,
                sizeInChunks,
                surfaceHeights,
                gripFixtureBottom,
                gripFixtureTop);
        }

        /// <summary>Convenience overload for the authored four-by-four scene.</summary>
        public static GeneratedSampleWorld Generate(
            WorldMaterialRegistry registry,
            SampleWorldMaterialIds materials,
            int seed)
        {
            return Generate(
                registry,
                materials,
                seed,
                DefaultMinimumChunk,
                DefaultSizeInChunks);
        }

        private static void CarveSampleGeometry(
            ChunkedWorldData worldData,
            SampleWorldMaterialIds materials,
            int minimumX,
            int minimumY,
            int width,
            int height)
        {
            int maximumX = minimumX + width - 1;
            int maximumY = minimumY + height - 1;

            CarveRectangle(worldData, -48, 48, MainPassageY - 1, MainPassageY + 1);
            CarveEllipse(worldData, new Vector2Int(-27, -16), 8, 6);
            CarveEllipse(worldData, new Vector2Int(27, -27), 9, 7);
            CarveRectangle(worldData, -29, 27, -29, -27);
            CarveRectangle(worldData, -1, 1, MainPassageY, maximumY);
            CarveRectangle(worldData, 25, 27, -27, MainPassageY);

            // The green strip is deliberately exposed so clicking it proves that
            // Background grip changes without changing foreground occupancy.
            for (int y = MainPassageY - 1; y <= MainPassageY + 1; y++)
            {
                for (int x = -18; x <= -8; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (IsInside(cell, minimumX, minimumY, maximumX, maximumY))
                    {
                        RequireSuccess(worldData.SetBackgroundMaterial(
                            cell,
                            materials.PreparedBackground));
                    }
                }
            }
        }

        private static Vector2Int PrepareSurfaceGripFixture(
            ChunkedWorldData worldData,
            SampleWorldMaterialIds materials,
            int seed,
            int minimumX,
            int minimumY,
            int width,
            int height,
            out Vector2Int top)
        {
            int maximumX = minimumX + width - 1;
            int maximumY = minimumY + height - 1;
            int bottomY = GetSurfaceHeight(GripFixtureX, seed) + 1;
            int topY = bottomY + GripFixtureHeight - 1;
            var bottom = new Vector2Int(GripFixtureX, bottomY);
            top = new Vector2Int(GripFixtureX, topY);

            // Author a short, deterministic Natural Earth floor across the base
            // of the prepared strip. The procedural surface is intentionally
            // uneven, so a dedicated flat run makes grounded movement and the
            // Natural-to-Prepared transition reproducible in Play Mode.
            for (int x = GripFixtureX - 3; x <= GripFixtureX + 3; x++)
            {
                var exposedCell = new Vector2Int(x, bottomY);
                var supportCell = exposedCell + Vector2Int.down;
                if (!IsInside(
                        exposedCell,
                        minimumX,
                        minimumY,
                        maximumX,
                        maximumY) ||
                    !IsInside(
                        supportCell,
                        minimumX,
                        minimumY,
                        maximumX,
                        maximumY))
                {
                    continue;
                }

                RemoveSolidIfPresent(worldData, exposedCell);
                RequireSuccess(worldData.SetBackgroundMaterial(
                    exposedCell,
                    materials.EarthBackground));
                RequireSuccess(worldData.SetSolidMaterial(
                    supportCell,
                    materials.SoilSolid));
            }

            // The vertical strip begins in the ant's grounded centre cell. It
            // therefore demonstrates Grounded -> Gripping without teleporting.
            for (int y = bottomY; y <= topY; y++)
            {
                var cell = new Vector2Int(GripFixtureX, y);
                if (IsInside(cell, minimumX, minimumY, maximumX, maximumY))
                {
                    RequireSuccess(worldData.SetBackgroundMaterial(
                        cell,
                        materials.PreparedBackground));
                }
            }

            // A small top region provides clear horizontal and diagonal targets
            // while keeping the vertical strip visually obvious.
            for (int y = topY - 2; y <= topY; y++)
            {
                for (int x = GripFixtureX - 3; x <= GripFixtureX + 3; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (IsInside(cell, minimumX, minimumY, maximumX, maximumY))
                    {
                        RemoveSolidIfPresent(worldData, cell);
                        RequireSuccess(worldData.SetBackgroundMaterial(
                            cell,
                            materials.PreparedBackground));
                    }
                }
            }

            return bottom;
        }

        /// <summary>
        /// Adds deterministic 2.4B-only terrain cases without changing the
        /// production motor. All fixtures are ordinary authoritative cell edits.
        /// </summary>
        private static void PrepareGridNavigationFixtures(
            ChunkedWorldData worldData,
            SampleWorldMaterialIds materials,
            Vector2Int surfaceFixtureBottom,
            int minimumX,
            int minimumY,
            int width,
            int height)
        {
            int maximumX = minimumX + width - 1;
            int maximumY = minimumY + height - 1;

            // A three-cell-wide natural shaft joins the surface fixture to the
            // underground course and permits whole-body vertical traversal.
            CarveRectangle(
                worldData,
                -10,
                -8,
                MainPassageY,
                surfaceFixtureBottom.y);

            // One centre obstruction forces an eight-direction detour through
            // the existing three-cell-high passage.
            if (IsInside(
                    GridBlockingCell,
                    minimumX,
                    minimumY,
                    maximumX,
                    maximumY))
            {
                RequireSuccess(worldData.SetSolidMaterial(
                    GridBlockingCell,
                    materials.StoneSolid));
            }

            // This exposed pocket is deliberately surrounded by solid terrain.
            RemoveSolidIfPresent(worldData, GridUnreachableDestination);
            RequireSuccess(worldData.SetBackgroundMaterial(
                GridUnreachableDestination,
                materials.EarthBackground));

            // Three non-grippable cells create a short gravity drop onto the
            // solid immediately below the main passage, where recovery is valid.
            for (int y = MainPassageY - 1; y <= MainPassageY + 1; y++)
            {
                var cell = new Vector2Int(GridFallFixture.x, y);
                RequireSuccess(worldData.SetBackgroundMaterial(
                    cell,
                    materials.SkyBackground));
            }
        }

        private static void CarveRectangle(
            ChunkedWorldData worldData,
            int minimumX,
            int maximumX,
            int minimumY,
            int maximumY)
        {
            for (int y = minimumY; y <= maximumY; y++)
            {
                for (int x = minimumX; x <= maximumX; x++)
                {
                    RemoveSolidIfPresent(worldData, new Vector2Int(x, y));
                }
            }
        }

        private static void CarveEllipse(
            ChunkedWorldData worldData,
            Vector2Int centre,
            int radiusX,
            int radiusY)
        {
            for (int y = -radiusY; y <= radiusY; y++)
            {
                for (int x = -radiusX; x <= radiusX; x++)
                {
                    float normalisedX = x / (float)radiusX;
                    float normalisedY = y / (float)radiusY;
                    if (normalisedX * normalisedX + normalisedY * normalisedY <= 1f)
                    {
                        RemoveSolidIfPresent(worldData, centre + new Vector2Int(x, y));
                    }
                }
            }
        }

        private static void RemoveSolidIfPresent(
            ChunkedWorldData worldData,
            Vector2Int worldCell)
        {
            if (worldData.GetCell(worldCell, out WorldTileCellData cell) ==
                    WorldCellQueryResult.Available &&
                cell.HasSolidTile)
            {
                RequireSuccess(worldData.RemoveSolidMaterial(worldCell));
            }
        }

        private static bool IsInside(
            Vector2Int cell,
            int minimumX,
            int minimumY,
            int maximumX,
            int maximumY)
        {
            return cell.x >= minimumX && cell.x <= maximumX &&
                cell.y >= minimumY && cell.y <= maximumY;
        }

        private static int GetSurfaceHeight(int worldX, int seed)
        {
            const int anchorSpacing = 8;
            int leftAnchor = FloorDivide(worldX, anchorSpacing);
            int localX = worldX - leftAnchor * anchorSpacing;
            int leftHeight = 16 + HeightOffset(leftAnchor, seed);
            int rightHeight = 16 + HeightOffset(leftAnchor + 1, seed);
            return Mathf.RoundToInt(Mathf.Lerp(
                leftHeight,
                rightHeight,
                localX / (float)anchorSpacing));
        }

        private static int HeightOffset(int anchor, int seed)
        {
            unchecked
            {
                uint value = (uint)(anchor * 374761393 + seed * 668265263);
                value = (value ^ (value >> 13)) * 1274126177u;
                return (int)(value % 7u) - 3;
            }
        }

        private static int FloorDivide(int value, int divisor)
        {
            int quotient = value / divisor;
            int remainder = value % divisor;
            return remainder < 0 ? quotient - 1 : quotient;
        }

        private static void ValidateMaterialIds(
            WorldMaterialRegistry registry,
            SampleWorldMaterialIds materials)
        {
            if (!registry.ContainsBackgroundMaterial(materials.SkyBackground) ||
                !registry.ContainsBackgroundMaterial(materials.EarthBackground) ||
                !registry.ContainsBackgroundMaterial(materials.StoneBackground) ||
                !registry.ContainsBackgroundMaterial(materials.PreparedBackground) ||
                !registry.ContainsSolidMaterial(materials.SoilSolid) ||
                !registry.ContainsSolidMaterial(materials.StoneSolid))
            {
                throw new ArgumentException(
                    "Every sample-world material ID must exist in the supplied registry.",
                    nameof(materials));
            }
        }

        private static void RequireSuccess(WorldChunkCreationResult result)
        {
            if (result != WorldChunkCreationResult.Success)
            {
                throw new InvalidOperationException(
                    $"Sample-world chunk creation failed with {result}.");
            }
        }

        private static void RequireSuccess(WorldCellMutationResult result)
        {
            if (result != WorldCellMutationResult.Success)
            {
                throw new InvalidOperationException(
                    $"Sample-world cell mutation failed with {result}.");
            }
        }
    }
}
