using AntColony.World;
using UnityEngine;

namespace AntColony.NavigationDebug
{
    /// <summary>
    /// Owns the deterministic, development-only world used by the navigation
    /// prototype scene. All terrain is authored through ChunkedWorldData APIs.
    /// </summary>
    [ExecuteAlways]
    public sealed class NavigationTestWorldHost : MonoBehaviour
    {
        public static readonly Vector2Int MutationCell = new Vector2Int(0, 1);
        public static readonly Vector2Int UnknownSupportCell = new Vector2Int(10, 31);
        public static readonly Vector2Int DiagonalOnlyCell = new Vector2Int(-29, 21);

        [SerializeField] private BackgroundTileMaterialDefinition backgroundMaterial;
        [SerializeField] private SolidTileMaterialDefinition solidMaterial;
        [SerializeField] private int worldSeed = 2104;

        private ChunkedWorldData worldData;
        private WorldNavigationClassifier classifier;

        public bool IsInitialized => worldData != null && classifier != null;
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
                new[] { backgroundMaterial },
                new[] { solidMaterial });
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
            if (!IsInitialized)
            {
                return false;
            }

            worldData.GetCell(MutationCell, out WorldTileCellData cell);
            WorldCellMutationResult result = cell.HasSolidTile
                ? worldData.RemoveSolidMaterial(MutationCell)
                : worldData.SetSolidMaterial(MutationCell, solidMaterial.StableMaterialId);
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
            RequireSuccess(
                worldData.SetSolidMaterial(worldCell, solidMaterial.StableMaterialId),
                $"set fixture Solid Tile at {worldCell}");
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
