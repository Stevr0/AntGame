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

        [Header("Grid navigation stress test")]
        [SerializeField, Min(1)] private int schedulerNodeBudgetPerUpdate = 128;
        [SerializeField, Min(1)] private int schedulerNodesPerRequestSlice = 8;
        [SerializeField, Range(1, 500)] private int gridStressGroupSize = 100;
        [SerializeField, Range(1, 100)] private int staggeredRequestsPerUpdate = 10;

        private GeneratedSampleWorld generatedWorld;
        private AntAutonomousMotor autonomousAnt;
        private GridNavigationMap gridNavigationMap;
        private GridNavigationMotor gridNavigationAnt;
        private GridNavigationScheduler gridNavigationScheduler;
        private readonly List<GridNavigationMotor> gridNavigationAnts = new();
        private bool staggeredRequestsActive;
        private int staggeredRequestIndex;

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
        public GridNavigationMotor GridNavigationAnt => gridNavigationAnt;
        public GridNavigationMap GridNavigationMap => gridNavigationMap;
        public GridNavigationScheduler GridNavigationScheduler =>
            gridNavigationScheduler;
        public IReadOnlyList<GridNavigationMotor> GridNavigationAnts =>
            gridNavigationAnts;
        public int GridStressGroupSize => gridStressGroupSize;
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

            if (Application.isPlaying)
            {
                ClearExtraGridAnts();
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
            gridNavigationMap = new GridNavigationMap(
                generatedWorld.WorldData,
                AntCollisionProfile.OrdinaryAnt);
            gridNavigationScheduler = new GridNavigationScheduler(
                gridNavigationMap);

            tilemapRenderer.BindWorldData(WorldData);
            collisionRenderer.BindWorldData(WorldData);
            RefreshAllChunks();

            if (Application.isPlaying)
            {
                SpawnOrResetAutonomousAnt(out _);
                SpawnOrResetGridNavigationAnt(out _);
            }
            return true;
        }

        private void Update()
        {
            gridNavigationScheduler?.ProcessUpdate(
                schedulerNodeBudgetPerUpdate,
                schedulerNodesPerRequestSlice);
            ProcessStaggeredRequests();

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
            NotifyGridTerrainChanged(worldCell);
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
            NotifyGridTerrainChanged(worldCell);
            return true;
        }

        /// <summary>
        /// Development-only way to create an explicitly non-grippable exposed
        /// cell. This keeps fall/recovery fixtures unambiguous now that natural
        /// Earth and Stone both provide grip.
        /// </summary>
        public bool SetNonGrippableBackground(Vector2Int worldCell)
        {
            if (!TryGetCell(worldCell, out WorldTileCellData cell) ||
                cell.HasSolidTile ||
                WorldData.SetBackgroundMaterial(
                    worldCell,
                    skyBackground.StableMaterialId) !=
                WorldCellMutationResult.Success)
            {
                return false;
            }

            tilemapRenderer.RefreshCell(worldCell);
            autonomousAnt?.NotifyWorldChanged();
            NotifyGridTerrainChanged(worldCell);
            return true;
        }

        /// <summary>Assigns the separate experimental A* grid ant.</summary>
        public bool TryAssignGridDestination(
            Vector2Int selectedCell,
            out string message)
        {
            if (gridNavigationAnt == null ||
                !gridNavigationAnt.gameObject.activeInHierarchy)
            {
                message = "Grid destination rejected: experimental ant unavailable.";
                HideDestinationMarker();
                return false;
            }

            if (!gridNavigationMap.TryGetAnchor(
                    selectedCell,
                    out GridNavigationAnchor destination) ||
                !gridNavigationAnt.RequestDestination(selectedCell))
            {
                message = gridNavigationAnt.StatusMessage;
                HideDestinationMarker();
                return false;
            }

            ShowDestinationMarker(destination.Position);
            message = gridNavigationAnt.StatusMessage;
            return true;
        }

        /// <summary>
        /// Creates a cyan experimental ant without changing the production 2.4A
        /// prefab, controller, or authored scene references.
        /// </summary>
        public bool SpawnOrResetGridNavigationAnt(out string message)
        {
            if (!TryFindSafeSpawnPosition(out Vector2 spawnPosition, out message) ||
                gridNavigationMap == null)
            {
                return false;
            }

            Vector2Int spawnCell =
                WorldTileCoordinates.WorldPositionToCell(spawnPosition);
            if (gridNavigationAnt == null)
            {
                gridNavigationAnt = CreateGridNavigationAnt(
                    "Experimental Grid Ant",
                    includeVisual: true);
                gridNavigationAnts.Add(gridNavigationAnt);
            }

            gridNavigationAnt.gameObject.SetActive(true);
            gridNavigationAnt.Bind(
                WorldData,
                gridNavigationMap,
                gridNavigationScheduler,
                spawnCell);
            message = $"Experimental grid ant spawned at anchor {spawnCell}.";
            return gridNavigationAnt.State != GridNavigationMotorState.Blocked;
        }

        public void SetGridStressGroupSize(int count)
        {
            gridStressGroupSize = Mathf.Clamp(count, 1, 500);
        }

        /// <summary>
        /// Creates development-only agents that all share the same map and
        /// scheduler. Only a small visible subset gets SpriteRenderers so the
        /// stress harness does not benchmark hundreds of debug visuals.
        /// </summary>
        public bool SpawnGridStressGroup(out string message)
        {
            if (!SpawnOrResetGridNavigationAnt(out message) ||
                !TryFindSafeSpawnPosition(out Vector2 spawnPosition, out message))
            {
                return false;
            }

            ClearExtraGridAnts();
            Vector2Int spawnCell =
                WorldTileCoordinates.WorldPositionToCell(spawnPosition);
            for (int index = 1; index < gridStressGroupSize; index++)
            {
                GridNavigationMotor ant = CreateGridNavigationAnt(
                    $"Grid Stress Ant {index + 1}",
                    includeVisual: index < 40);
                ant.Bind(
                    WorldData,
                    gridNavigationMap,
                    gridNavigationScheduler,
                    spawnCell);
                gridNavigationAnts.Add(ant);
            }

            message =
                $"Spawned {gridNavigationAnts.Count} shared-map grid ants.";
            return true;
        }

        public void ClearGridStressGroup()
        {
            staggeredRequestsActive = false;
            foreach (GridNavigationMotor ant in gridNavigationAnts)
            {
                ant?.CancelNavigation("Stress group cleared.");
            }
            ClearExtraGridAnts();
        }

        public void StartSimultaneousGridRequests()
        {
            staggeredRequestsActive = false;
            for (int index = 0; index < gridNavigationAnts.Count; index++)
            {
                RequestStressDestination(gridNavigationAnts[index], index);
            }
        }

        public void StartStaggeredGridRequests()
        {
            staggeredRequestIndex = 0;
            staggeredRequestsActive = gridNavigationAnts.Count > 0;
        }

        public bool CancelFirstGridRequest()
        {
            if (gridNavigationAnts.Count == 0)
            {
                return false;
            }

            gridNavigationAnts[0].CancelNavigation(
                "First stress-ant request cancelled.");
            return true;
        }

        public void StartGridFallingFixture()
        {
            if (gridNavigationAnt == null)
            {
                return;
            }

            gridNavigationAnt.CancelNavigation(
                "Development falling fixture reset.");
            Rigidbody2D body = gridNavigationAnt.GetComponent<Rigidbody2D>();
            Vector2 fixturePosition = WorldTileCoordinates.CellToWorldPosition(
                SampleWorldGenerator.GridFallFixture);
            body.position = fixturePosition;
            gridNavigationAnt.transform.position = fixturePosition;
            gridNavigationAnt.BeginFalling(
                "Development falling fixture started.");
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

        private void NotifyGridTerrainChanged(Vector2Int worldCell)
        {
            gridNavigationMap?.InvalidateCell(worldCell);
            gridNavigationScheduler?.NotifyTerrainInvalidated();
            foreach (GridNavigationMotor ant in gridNavigationAnts)
            {
                ant?.NotifyTerrainChanged();
            }
        }

        private GridNavigationMotor CreateGridNavigationAnt(
            string objectName,
            bool includeVisual)
        {
            var root = new GameObject(objectName);
            root.transform.SetParent(transform, false);
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.simulated = false;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            CapsuleCollider2D capsule = root.AddComponent<CapsuleCollider2D>();
            capsule.direction = CapsuleDirection2D.Horizontal;
            capsule.size = AntCollisionProfile.OrdinaryAnt.Size;
            // Terrain colliders use ClimbableSurface (layer 8). Excluding
            // Default prevents grid ants and the 2.4A comparison ant from
            // physically pushing one another while retaining terrain collision.
            capsule.excludeLayers = 1 << LayerMask.NameToLayer("Default");
            GridNavigationMotor ant = root.AddComponent<GridNavigationMotor>();

            if (includeVisual)
            {
                var visual = new GameObject("Visual");
                visual.transform.SetParent(root.transform, false);
                SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
                SpriteRenderer source = autonomousAntPrefab != null
                    ? autonomousAntPrefab.GetComponentInChildren<SpriteRenderer>()
                    : null;
                if (source != null)
                {
                    renderer.sprite = source.sprite;
                    renderer.sharedMaterial = source.sharedMaterial;
                }

                renderer.color = new Color32(70, 225, 235, 180);
                renderer.sortingOrder = 6;
                visual.transform.localScale = new Vector3(0.8f, 0.5f, 1f);
            }

            return ant;
        }

        private void ClearExtraGridAnts()
        {
            for (int index = gridNavigationAnts.Count - 1; index >= 1; index--)
            {
                GridNavigationMotor ant = gridNavigationAnts[index];
                gridNavigationAnts.RemoveAt(index);
                if (ant != null)
                {
                    Destroy(ant.gameObject);
                }
            }
        }

        private void ProcessStaggeredRequests()
        {
            if (!staggeredRequestsActive)
            {
                return;
            }

            int end = Mathf.Min(
                gridNavigationAnts.Count,
                staggeredRequestIndex + staggeredRequestsPerUpdate);
            while (staggeredRequestIndex < end)
            {
                RequestStressDestination(
                    gridNavigationAnts[staggeredRequestIndex],
                    staggeredRequestIndex);
                staggeredRequestIndex++;
            }

            staggeredRequestsActive =
                staggeredRequestIndex < gridNavigationAnts.Count;
        }

        private void RequestStressDestination(
            GridNavigationMotor ant,
            int index)
        {
            Vector2Int destination = (index % 4) switch
            {
                0 => SampleWorldGenerator.GridDirectDestination,
                1 => SampleWorldGenerator.GridDetourDestination,
                2 => SampleWorldGenerator.GridChunkBoundaryDestination,
                _ => GripFixtureTop
            };
            GridNavigationRequestPriority priority = index % 9 == 0
                ? GridNavigationRequestPriority.High
                : index % 5 == 0
                    ? GridNavigationRequestPriority.Low
                    : GridNavigationRequestPriority.Normal;
            ant.RequestDestination(destination, priority);
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
