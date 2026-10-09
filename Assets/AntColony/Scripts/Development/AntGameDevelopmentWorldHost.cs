using System.Collections.Generic;
using AntColony.World;
using AntColony.WorldPresentation;
using UnityEngine;

namespace AntColony.Development
{
    /// <summary>
    /// Coordinates generation and existing projections for the integrated sample
    /// scene. Authoritative terrain remains owned solely by ChunkedWorldData.
    /// </summary>
    public sealed class AntGameDevelopmentWorldHost : MonoBehaviour, IWorldDataProvider
    {
        [Header("Deterministic sample")]
        [SerializeField] private int worldSeed = 2203;
        [SerializeField] private Vector2Int minimumChunk = new(-2, -2);
        [SerializeField] private Vector2Int sizeInChunks = new(4, 4);

        [Header("Background Materials")]
        [SerializeField] private BackgroundTileMaterialDefinition skyBackground;
        [SerializeField] private BackgroundTileMaterialDefinition earthBackground;
        [SerializeField] private BackgroundTileMaterialDefinition stoneBackground;
        [SerializeField] private BackgroundTileMaterialDefinition preparedBackground;

        [Header("Solid Materials")]
        [SerializeField] private SolidTileMaterialDefinition soilSolid;
        [SerializeField] private SolidTileMaterialDefinition stoneSolid;

        [Header("Existing presentation services")]
        [SerializeField] private WorldTilemapRenderer tilemapRenderer;
        [SerializeField] private WorldTerrainCollisionRenderer collisionRenderer;

        private GeneratedSampleWorld generatedWorld;

        public bool IsWorldDataAvailable => generatedWorld != null;
        public ChunkedWorldData WorldData => generatedWorld?.WorldData;
        public GeneratedSampleWorld GeneratedWorld => generatedWorld;
        public int WorldSeed => worldSeed;
        public Vector2Int MinimumChunk => minimumChunk;
        public Vector2Int SizeInChunks => sizeInChunks;
        public string SoilSolidMaterialId => soilSolid != null
            ? soilSolid.StableMaterialId
            : string.Empty;

        private void Awake()
        {
            RebuildWorld();
        }

        /// <summary>Recreates the bounded world and then refreshes both projections.</summary>
        public bool RebuildWorld()
        {
            if (!HasRequiredReferences())
            {
                generatedWorld = null;
                Debug.LogError(
                    "AntGameDevelopment world references are incomplete.",
                    this);
                return false;
            }

            var registry = new WorldMaterialRegistry(
                BackgroundDefinitions(),
                SolidDefinitions());
            generatedWorld = SampleWorldGenerator.Generate(
                registry,
                BuildMaterialIds(),
                worldSeed,
                minimumChunk,
                sizeInChunks);

            tilemapRenderer.BindWorldData(WorldData);
            collisionRenderer.BindWorldData(WorldData);
            RefreshAllChunks();
            return true;
        }

        /// <summary>Refreshes the exact authored region; it never discovers chunks.</summary>
        public void RefreshAllChunks()
        {
            if (!IsWorldDataAvailable)
            {
                return;
            }

            for (int y = 0; y < sizeInChunks.y; y++)
            {
                for (int x = 0; x < sizeInChunks.x; x++)
                {
                    Vector2Int chunk = minimumChunk + new Vector2Int(x, y);
                    tilemapRenderer.RefreshChunk(chunk);
                    collisionRenderer.RefreshChunk(chunk);
                }
            }
        }

        /// <summary>
        /// Development-only occupancy toggle. Both visual and collision projections
        /// are refreshed from authoritative data after a successful mutation.
        /// </summary>
        public bool ToggleForegroundSolid(Vector2Int worldCell)
        {
            if (!TryGetCell(worldCell, out WorldTileCellData cell))
            {
                return false;
            }

            WorldCellMutationResult result = cell.HasSolidTile
                ? WorldData.RemoveSolidMaterial(worldCell)
                : WorldData.SetSolidMaterial(worldCell, soilSolid.StableMaterialId);
            if (result != WorldCellMutationResult.Success)
            {
                return false;
            }

            tilemapRenderer.RefreshCell(worldCell);
            collisionRenderer.RefreshCell(worldCell);
            return true;
        }

        /// <summary>
        /// Development-only Background replacement for exposed underground space.
        /// Foreground occupancy and collision are deliberately left unchanged.
        /// </summary>
        public bool TogglePreparedBackground(Vector2Int worldCell)
        {
            if (!TryGetCell(worldCell, out WorldTileCellData cell) ||
                cell.HasSolidTile ||
                cell.BackgroundMaterialId == skyBackground.StableMaterialId)
            {
                return false;
            }

            string replacement = cell.BackgroundMaterialId ==
                preparedBackground.StableMaterialId
                    ? worldCell.y <= SampleWorldGenerator.StoneBackgroundMaximumY
                        ? stoneBackground.StableMaterialId
                        : earthBackground.StableMaterialId
                    : preparedBackground.StableMaterialId;
            if (WorldData.SetBackgroundMaterial(worldCell, replacement) !=
                WorldCellMutationResult.Success)
            {
                return false;
            }

            tilemapRenderer.RefreshCell(worldCell);
            return true;
        }

        public bool TryGetCell(Vector2Int worldCell, out WorldTileCellData cell)
        {
            if (!IsWorldDataAvailable)
            {
                cell = default;
                return false;
            }

            return WorldData.GetCell(worldCell, out cell) ==
                WorldCellQueryResult.Available;
        }

        /// <summary>Used only by the scene authoring utility and focused tests.</summary>
        public void Configure(
            int seed,
            Vector2Int authoredMinimumChunk,
            Vector2Int authoredSizeInChunks,
            BackgroundTileMaterialDefinition sky,
            BackgroundTileMaterialDefinition earth,
            BackgroundTileMaterialDefinition stoneBackgroundDefinition,
            BackgroundTileMaterialDefinition prepared,
            SolidTileMaterialDefinition soil,
            SolidTileMaterialDefinition stoneSolidDefinition,
            WorldTilemapRenderer visualRenderer,
            WorldTerrainCollisionRenderer terrainCollisionRenderer)
        {
            worldSeed = seed;
            minimumChunk = authoredMinimumChunk;
            sizeInChunks = authoredSizeInChunks;
            skyBackground = sky;
            earthBackground = earth;
            stoneBackground = stoneBackgroundDefinition;
            preparedBackground = prepared;
            soilSolid = soil;
            stoneSolid = stoneSolidDefinition;
            tilemapRenderer = visualRenderer;
            collisionRenderer = terrainCollisionRenderer;
        }

        private bool HasRequiredReferences()
        {
            return skyBackground != null && earthBackground != null &&
                stoneBackground != null && preparedBackground != null &&
                soilSolid != null && stoneSolid != null &&
                tilemapRenderer != null && collisionRenderer != null;
        }

        private SampleWorldMaterialIds BuildMaterialIds()
        {
            return new SampleWorldMaterialIds(
                skyBackground.StableMaterialId,
                earthBackground.StableMaterialId,
                stoneBackground.StableMaterialId,
                preparedBackground.StableMaterialId,
                soilSolid.StableMaterialId,
                stoneSolid.StableMaterialId);
        }

        private IEnumerable<BackgroundTileMaterialDefinition> BackgroundDefinitions()
        {
            yield return skyBackground;
            yield return earthBackground;
            yield return stoneBackground;
            yield return preparedBackground;
        }

        private IEnumerable<SolidTileMaterialDefinition> SolidDefinitions()
        {
            yield return soilSolid;
            yield return stoneSolid;
        }
    }
}
