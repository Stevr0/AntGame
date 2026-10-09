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

        [Header("First autonomous ant")]
        [SerializeField] private AntAutonomousMotor autonomousAntPrefab;
        [SerializeField] private int preferredSpawnX = -12;
        [SerializeField, Min(0)] private int spawnSearchRadius = 20;
        [SerializeField] private Transform destinationMarker;
        [SerializeField] private SpriteRenderer destinationMarkerRenderer;

        private GeneratedSampleWorld generatedWorld;
        private AntAutonomousMotor autonomousAnt;

        public bool IsWorldDataAvailable => generatedWorld != null;
        public ChunkedWorldData WorldData => generatedWorld?.WorldData;
        public GeneratedSampleWorld GeneratedWorld => generatedWorld;
        public WorldTilemapRenderer TilemapRenderer => tilemapRenderer;
        public int WorldSeed => worldSeed;
        public Vector2Int MinimumChunk => minimumChunk;
        public Vector2Int SizeInChunks => sizeInChunks;
        public string SoilSolidMaterialId => soilSolid != null
            ? soilSolid.StableMaterialId
            : string.Empty;
        public AntAutonomousMotor AutonomousAnt => autonomousAnt;
        public AntAutonomousMotor AutonomousAntPrefab => autonomousAntPrefab;
        public Vector2Int GripFixtureBottom => generatedWorld != null
            ? generatedWorld.GripFixtureBottom
            : default;
        public Vector2Int GripFixtureTop => generatedWorld != null
            ? generatedWorld.GripFixtureTop
            : default;
        public int SpawnedAutonomousAntCount =>
            GetComponentsInChildren<AntAutonomousMotor>(true).Length;

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

            if (Application.isPlaying)
            {
                SpawnOrResetAutonomousAnt(out _);
            }
            return true;
        }

        private void Update()
        {
            if (destinationMarkerRenderer == null || autonomousAnt == null ||
                !destinationMarkerRenderer.gameObject.activeSelf)
            {
                return;
            }

            destinationMarkerRenderer.color = autonomousAnt.State ==
                AntAutonomousMovementState.Arrived
                    ? new Color32(114, 230, 136, 255)
                    : new Color32(255, 224, 92, 255);
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
            autonomousAnt?.NotifyWorldChanged();
            return true;
        }

        /// <summary>
        /// Development-only Background replacement for exposed underground space.
        /// Foreground occupancy and collision are deliberately left unchanged.
        /// </summary>
        public bool TogglePreparedBackground(Vector2Int worldCell)
        {
            if (!TryGetCell(worldCell, out WorldTileCellData cell) ||
                cell.HasSolidTile)
            {
                return false;
            }

            string replacement = cell.BackgroundMaterialId ==
                preparedBackground.StableMaterialId
                    ? GetUnpreparedBackgroundMaterialId(worldCell)
                    : preparedBackground.StableMaterialId;
            if (WorldData.SetBackgroundMaterial(worldCell, replacement) !=
                WorldCellMutationResult.Success)
            {
                return false;
            }

            tilemapRenderer.RefreshCell(worldCell);
            autonomousAnt?.NotifyWorldChanged();
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

        /// <summary>
        /// Finds a valid surface pose in a bounded X range and creates or resets
        /// exactly one autonomous development ant. Queries never create chunks.
        /// </summary>
        public bool SpawnOrResetAutonomousAnt(out string message)
        {
            if (!IsWorldDataAvailable || autonomousAntPrefab == null)
            {
                message = "Autonomous ant spawn failed: world or prefab is unavailable.";
                Debug.LogError(message, this);
                return false;
            }

            if (!TryFindSafeSpawnPosition(out Vector2 spawnPosition, out message))
            {
                if (autonomousAnt != null)
                {
                    autonomousAnt.gameObject.SetActive(false);
                }

                HideDestinationMarker();
                Debug.LogError(message, this);
                return false;
            }

            if (autonomousAnt == null)
            {
                autonomousAnt = Instantiate(
                    autonomousAntPrefab,
                    spawnPosition,
                    Quaternion.identity,
                    transform);
                autonomousAnt.name = "Autonomous Ant";
            }
            else
            {
                autonomousAnt.gameObject.SetActive(true);
                autonomousAnt.transform.SetPositionAndRotation(
                    spawnPosition,
                    Quaternion.identity);
                Rigidbody2D body = autonomousAnt.GetComponent<Rigidbody2D>();
                body.position = spawnPosition;
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }

            autonomousAnt.BindWorldData(WorldData);
            HideDestinationMarker();
            message = $"Autonomous ant spawned safely at {spawnPosition}.";
            return true;
        }

        /// <summary>Resolves and validates a clicked direct controlled destination.</summary>
        public bool TryAssignAutonomousDestination(
            Vector2Int selectedCell,
            out string message)
        {
            if (autonomousAnt == null || !autonomousAnt.gameObject.activeInHierarchy)
            {
                message = "Destination rejected: no autonomous ant is available.";
                HideDestinationMarker();
                return false;
            }

            var validator = new DirectGroundRouteValidator(
                WorldData,
                AntCollisionProfile.OrdinaryAnt);
            DirectGroundRouteResult poseResult = validator.ResolveControlledPosition(
                selectedCell,
                out Vector2 destination);
            if (!poseResult.Succeeded)
            {
                message = $"Destination rejected: {poseResult.Status}.";
                HideDestinationMarker();
                return false;
            }

            DirectGroundRouteResult routeResult =
                autonomousAnt.TrySetDestination(destination);
            if (!routeResult.Succeeded)
            {
                message = autonomousAnt.StatusMessage;
                HideDestinationMarker();
                return false;
            }

            ShowDestinationMarker(destination);
            message = $"Destination accepted at {destination}.";
            return true;
        }

        /// <summary>Searches only the generated sample's authored surface range.</summary>
        public bool TryFindSafeSpawnPosition(
            out Vector2 spawnPosition,
            out string message)
        {
            spawnPosition = default;
            if (!IsWorldDataAvailable)
            {
                message = "Safe spawn search requires resident world data.";
                return false;
            }

            int minimumX = minimumChunk.x * WorldTileCoordinates.ChunkSize;
            int maximumX = minimumX + generatedWorld.WidthInCells - 1;
            var validator = new DirectGroundRouteValidator(
                WorldData,
                AntCollisionProfile.OrdinaryAnt);

            for (int distance = 0; distance <= spawnSearchRadius; distance++)
            {
                int candidateCount = distance == 0 ? 1 : 2;
                for (int candidateIndex = 0;
                    candidateIndex < candidateCount;
                    candidateIndex++)
                {
                    int x = preferredSpawnX +
                        (candidateIndex == 0 ? distance : -distance);
                    if (x < minimumX || x > maximumX)
                    {
                        continue;
                    }

                    int surfaceY = generatedWorld.SurfaceHeights[x - minimumX];
                    var exposedCell = new Vector2Int(x, surfaceY + 1);
                    DirectGroundRouteResult result =
                        validator.ResolveGroundedPosition(
                            exposedCell,
                            out Vector2 candidatePosition);
                    if (result.Succeeded)
                    {
                        spawnPosition = candidatePosition;
                        message = string.Empty;
                        return true;
                    }
                }
            }

            message =
                $"No safe grounded spawn exists within {spawnSearchRadius} cells " +
                $"of X={preferredSpawnX}.";
            return false;
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
            WorldTerrainCollisionRenderer terrainCollisionRenderer,
            AntAutonomousMotor antPrefab,
            Transform marker,
            SpriteRenderer markerRenderer)
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
            autonomousAntPrefab = antPrefab;
            destinationMarker = marker;
            destinationMarkerRenderer = markerRenderer;
        }

        private bool HasRequiredReferences()
        {
            return skyBackground != null && earthBackground != null &&
                stoneBackground != null && preparedBackground != null &&
                soilSolid != null && stoneSolid != null &&
                tilemapRenderer != null && collisionRenderer != null;
        }

        private string GetUnpreparedBackgroundMaterialId(Vector2Int worldCell)
        {
            int minimumX = minimumChunk.x * WorldTileCoordinates.ChunkSize;
            int surfaceIndex = worldCell.x - minimumX;
            if (generatedWorld != null && surfaceIndex >= 0 &&
                surfaceIndex < generatedWorld.SurfaceHeights.Length &&
                worldCell.y > generatedWorld.SurfaceHeights[surfaceIndex])
            {
                return skyBackground.StableMaterialId;
            }

            return worldCell.y <= SampleWorldGenerator.StoneBackgroundMaximumY
                ? stoneBackground.StableMaterialId
                : earthBackground.StableMaterialId;
        }

        private void ShowDestinationMarker(Vector2 position)
        {
            if (destinationMarker == null || destinationMarkerRenderer == null)
            {
                return;
            }

            destinationMarker.position = new Vector3(position.x, position.y, 0f);
            destinationMarker.gameObject.SetActive(true);
            destinationMarkerRenderer.color = new Color32(255, 224, 92, 255);
        }

        private void HideDestinationMarker()
        {
            if (destinationMarker != null)
            {
                destinationMarker.gameObject.SetActive(false);
            }
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
