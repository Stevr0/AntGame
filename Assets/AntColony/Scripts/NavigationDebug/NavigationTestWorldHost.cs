using System.Collections.Generic;
using AntColony.World;
using UnityEngine;

namespace AntColony.NavigationDebug
{
    /// <summary>
    /// Owns the deterministic, development-only world used by the navigation
    /// prototype scene. All terrain is authored through ChunkedWorldData APIs.
    /// </summary>
    [ExecuteAlways]
    public sealed class NavigationTestWorldHost : MonoBehaviour, IWorldDataProvider
    {
        public static readonly Vector2Int MutationCell = new Vector2Int(0, 1);
        public static readonly Vector2Int TilemapMutationCell = Vector2Int.zero;
        public static readonly Vector2Int SupportOnlySolidCell = new Vector2Int(-34, 5);
        public static readonly Vector2Int BlockingOnlySolidCell = new Vector2Int(-32, 5);
        public static readonly Vector2Int UnknownSupportCell = new Vector2Int(10, 31);
        public static readonly Vector2Int DiagonalOnlyCell = new Vector2Int(-29, 21);
        public static readonly Vector2Int BackgroundMutationCell =
            new Vector2Int(20, 8);

        // Kept under its original field name so existing scene serialization
        // continues to resolve it as the fixture's unprepared Earth material.
        [SerializeField] private BackgroundTileMaterialDefinition backgroundMaterial;
        [SerializeField] private BackgroundTileMaterialDefinition skyBackgroundMaterial;
        [SerializeField] private BackgroundTileMaterialDefinition stoneBackgroundMaterial;
        [SerializeField] private BackgroundTileMaterialDefinition preparedBackgroundMaterial;
        [SerializeField] private SolidTileMaterialDefinition solidMaterial;
        [SerializeField] private SolidTileMaterialDefinition supportOnlyMaterial;
        [SerializeField] private SolidTileMaterialDefinition blockingOnlyMaterial;
        [SerializeField] private int worldSeed = 2104;

        private ChunkedWorldData worldData;
        private WorldNavigationClassifier classifier;

        public bool IsInitialized => worldData != null && classifier != null;
        public bool IsWorldDataAvailable => IsInitialized;
        public ChunkedWorldData WorldData => worldData;
        public WorldNavigationClassifier Classifier => classifier;

        private void OnEnable()
        {
            TryRebuildFixture();
        }

        private void OnValidate()
        {
            if (isActiveAndEnabled)
            {
                TryRebuildFixture();
            }
        }

        /// <summary>
        /// Recreates the explicit eight-chunk fixture. This is authored test data,
        /// not procedural generation, and it never reads legacy DirtBlock objects.
        /// </summary>
        public bool TryRebuildFixture()
        {
            if (backgroundMaterial == null || solidMaterial == null)
            {
                worldData = null;
                classifier = null;
                return false;
            }

            var registry = new WorldMaterialRegistry(
                BuildBackgroundDefinitions(),
                BuildSolidDefinitions());
            worldData = new ChunkedWorldData(registry, worldSeed);

            // Two rows of four chunks cover x=-64..63 and y=-32..31. The row
            // above remains absent so cells along y=31 demonstrate Unknown support.
            for (int chunkY = -1; chunkY <= 0; chunkY++)
            {
                for (int chunkX = -2; chunkX <= 1; chunkX++)
                {
                    RequireSuccess(
                        worldData.CreateChunk(
                            new Vector2Int(chunkX, chunkY),
                            backgroundMaterial.StableMaterialId),
                        "create fixture chunk");
                }
            }

            PopulateSolidFixture();
            classifier = new WorldNavigationClassifier(worldData);
            return true;
        }

        /// <summary>
        /// Adds or removes one Solid Tile through the authoritative mutation API.
        /// The renderer explicitly refreshes after this development-only action.
        /// </summary>
        public bool ToggleMutationSolid()
        {
            return ToggleSolidAt(MutationCell);
        }

        /// <summary>
        /// Development-only proof that Background replacement immediately changes
        /// capability data without changing or creating a foreground Solid Tile.
        /// </summary>
        public bool TogglePreparedBackground(Vector2Int worldCell)
        {
            if (!IsInitialized || preparedBackgroundMaterial == null ||
                worldData.GetCell(worldCell, out WorldTileCellData cell) !=
                    WorldCellQueryResult.Available)
            {
                return false;
            }

            string replacementId = cell.BackgroundMaterialId ==
                preparedBackgroundMaterial.StableMaterialId
                    ? backgroundMaterial.StableMaterialId
                    : preparedBackgroundMaterial.StableMaterialId;
            return worldData.SetBackgroundMaterial(worldCell, replacementId) ==
                WorldCellMutationResult.Success;
        }

        /// <summary>
        /// Adds or removes the Solid Tile at a requested resident cell. This is a
        /// development-only mutation seam used to prove that projections refresh
        /// from world data rather than treating their current visuals as state.
        /// </summary>
        public bool ToggleSolidAt(Vector2Int worldCell)
        {
            if (!IsInitialized)
            {
                return false;
            }

            if (worldData.GetCell(worldCell, out WorldTileCellData cell) !=
                WorldCellQueryResult.Available)
            {
                return false;
            }

            WorldCellMutationResult result = cell.HasSolidTile
                ? worldData.RemoveSolidMaterial(worldCell)
                : worldData.SetSolidMaterial(worldCell, solidMaterial.StableMaterialId);
            return result == WorldCellMutationResult.Success;
        }

        private void PopulateSolidFixture()
        {
            // Open surface spanning both negative and positive X chunk boundaries.
            FillHorizontal(-40, 40, 0);

            // A vertical wall and an overhang/ceiling.
            FillVertical(-20, 1, 12);
            FillHorizontal(-20, -8, 12);

            // A two-cell-high narrow tunnel between floor y=4 and roof y=7.
            FillHorizontal(-5, 7, 4);
            FillHorizontal(-5, 7, 7);
            FillVertical(-5, 4, 7);
            FillVertical(7, 4, 7);

            // A large chamber whose centre is intentionally unsupported.
            FillHorizontal(12, 28, 1);
            FillHorizontal(12, 28, 15);
            FillVertical(12, 1, 15);
            FillVertical(28, 1, 15);

            // The marked cell at (-29,21) sees this tile diagonally only.
            SetSolid(new Vector2Int(-30, 20));

            // Optional development materials prove that blocking and navigation
            // support remain independent properties in the authored scene.
            if (supportOnlyMaterial != null)
            {
                SetSolid(SupportOnlySolidCell, supportOnlyMaterial);
            }

            if (blockingOnlyMaterial != null)
            {
                SetSolid(BlockingOnlySolidCell, blockingOnlyMaterial);
            }
        }

        private void FillHorizontal(int minimumX, int maximumX, int y)
        {
            for (int x = minimumX; x <= maximumX; x++)
            {
                SetSolid(new Vector2Int(x, y));
            }
        }

        private void FillVertical(int x, int minimumY, int maximumY)
        {
            for (int y = minimumY; y <= maximumY; y++)
            {
                SetSolid(new Vector2Int(x, y));
            }
        }

        private void SetSolid(Vector2Int worldCell)
        {
            SetSolid(worldCell, solidMaterial);
        }

        private void SetSolid(
            Vector2Int worldCell,
            SolidTileMaterialDefinition material)
        {
            RequireSuccess(
                worldData.SetSolidMaterial(worldCell, material.StableMaterialId),
                $"set fixture Solid Tile at {worldCell}");
        }

        private IEnumerable<SolidTileMaterialDefinition> BuildSolidDefinitions()
        {
            yield return solidMaterial;
            if (supportOnlyMaterial != null)
            {
                yield return supportOnlyMaterial;
            }

            if (blockingOnlyMaterial != null)
            {
                yield return blockingOnlyMaterial;
            }
        }

        private IEnumerable<BackgroundTileMaterialDefinition>
            BuildBackgroundDefinitions()
        {
            yield return backgroundMaterial;
            if (skyBackgroundMaterial != null)
            {
                yield return skyBackgroundMaterial;
            }

            if (stoneBackgroundMaterial != null)
            {
                yield return stoneBackgroundMaterial;
            }

            if (preparedBackgroundMaterial != null)
            {
                yield return preparedBackgroundMaterial;
            }
        }

        private static void RequireSuccess(WorldChunkCreationResult result, string action)
        {
            if (result != WorldChunkCreationResult.Success)
            {
                throw new System.InvalidOperationException(
                    $"Could not {action}. Result: {result}.");
            }
        }

        private static void RequireSuccess(WorldCellMutationResult result, string action)
        {
            if (result != WorldCellMutationResult.Success)
            {
                throw new System.InvalidOperationException(
                    $"Could not {action}. Result: {result}.");
            }
        }
    }
}
