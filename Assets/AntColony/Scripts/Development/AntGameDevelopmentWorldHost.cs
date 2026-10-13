using System.Collections.Generic;
using System.Linq;
using AntColony.Ants;
using AntColony.Commands;
using AntColony.Navigation;
using AntColony.World;
using AntColony.WorldPresentation;
using UnityEngine;

namespace AntColony.Development
{
    /// <summary>
    /// Coordinates generation and existing projections for the integrated sample
    /// scene. Authoritative terrain remains owned solely by ChunkedWorldData.
    /// </summary>
    [RequireComponent(
        typeof(GridNavigationWorldService),
        typeof(AntSelectionCommandController))]
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
        [SerializeField] private BackgroundTileMaterialDefinition foodStorageBackground;
        [SerializeField] private BackgroundTileMaterialDefinition waterStorageBackground;
        [SerializeField] private BackgroundTileMaterialDefinition sleepingBackground;
        [SerializeField] private BackgroundTileMaterialDefinition materialStorageBackground;

        [Header("Solid Materials")]
        [SerializeField] private SolidTileMaterialDefinition soilSolid;
        [SerializeField] private SolidTileMaterialDefinition stoneSolid;
        [SerializeField] private SolidTileMaterialDefinition blockingOnlySolid;

        [Header("Existing presentation services")]
        [SerializeField] private WorldTilemapRenderer tilemapRenderer;
        [SerializeField] private WorldTerrainCollisionRenderer collisionRenderer;

        [Header("Primary grid ant")]
        [SerializeField] private GridNavigationWorldService navigationWorldService;
        [SerializeField] private AntSelectionCommandController selectionController;
        [SerializeField] private GridNavigationMotor primaryGridAntPrefab;

        [Header("Legacy 2.4A comparison ant")]
        [SerializeField] private AntAutonomousMotor autonomousAntPrefab;
        [SerializeField] private int preferredSpawnX = -12;
        [SerializeField, Min(0)] private int spawnSearchRadius = 20;
        [SerializeField] private Transform destinationMarker;
        [SerializeField] private SpriteRenderer destinationMarkerRenderer;

        [Header("Grid navigation stress test")]
        [SerializeField, Range(1, 500)] private int gridStressGroupSize = 100;
        [SerializeField, Range(1, 100)] private int staggeredRequestsPerUpdate = 10;

        private GeneratedSampleWorld generatedWorld;
        private AntAutonomousMotor autonomousAnt;
        private GridNavigationMotor gridNavigationAnt;
        private readonly List<GridNavigationMotor> selectableGridAnts = new();
        private readonly List<GridNavigationMotor> gridStressAnts = new();
        private AntRegistry antRegistry;
        private AntEntityBindingRegistry antBindingRegistry;
        private bool staggeredRequestsActive;
        private int staggeredRequestIndex;
        private readonly List<Vector2Int> survivalFixtureCells = new();

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
        public GridNavigationWorldService NavigationWorldService =>
            navigationWorldService;
        public AntSelectionCommandController SelectionController =>
            selectionController;
        public GridNavigationMap GridNavigationMap => navigationWorldService?.Map;
        public GridNavigationScheduler GridNavigationScheduler =>
            navigationWorldService?.Scheduler;
        public IReadOnlyList<GridNavigationMotor> GridNavigationAnts =>
            gridStressAnts;
        public IReadOnlyList<GridNavigationMotor> SelectableGridAnts =>
            selectableGridAnts;
        public AntRegistry AntRegistry => antRegistry;
        public SurvivalInteractionWorld SurvivalInteractionWorld { get; private set; }
        public ColonyJobRegistry ColonyJobRegistry { get; private set; }
        public ExcavationDesignationRegistry ExcavationDesignations { get; private set; }
        public WorldMaterialDropRegistry MaterialDrops { get; private set; }
        public MaterialStorageWorld MaterialStorage { get; private set; }
        public ExcavationWorldTransaction ExcavationTransaction { get; private set; }
        public IReadOnlyList<Vector2Int> SurvivalFixtureCells => survivalFixtureCells;
        public int GridStressGroupSize => gridStressGroupSize;
        public AntAutonomousMotor AutonomousAntPrefab => autonomousAntPrefab;
        public GridNavigationMotor PrimaryGridAntPrefab => primaryGridAntPrefab;
        public Vector2Int GripFixtureBottom => generatedWorld != null
            ? generatedWorld.GripFixtureBottom
            : default;
        public Vector2Int GripFixtureTop => generatedWorld != null
            ? generatedWorld.GripFixtureTop
            : default;
        public int SpawnedAutonomousAntCount =>
            GetComponentsInChildren<AntAutonomousMotor>(true).Length;
        public static readonly Vector2Int ExcavationSoilFixture = new(-4, -6);
        public static readonly Vector2Int ExcavationAreaMinimum = new(4, -6);
        public static readonly Vector2Int ExcavationAreaMaximum = new(6, -6);
        public static readonly Vector2Int ExcavationStoneFixture = new(24, -26);
        public static readonly Vector2Int ExcavationOpeningFixture = new(12, -10);
        public static readonly Vector2Int ExcavationInitiallyBlockedFixture = new(12, -11);
        public static readonly Vector2Int NonExcavatableFixture = new(16, -6);
        public static readonly Vector2Int SoilDropFixture = new(-2, -8);
        public static readonly Vector2Int StoneDropFixture = new(8, -8);
        public static readonly Vector2Int GeneralMaterialStorageFixture = new(-12, -8);
        public static readonly Vector2Int SoilMaterialStorageFixture = new(9, -8);
        public static readonly Vector2Int StoneMaterialStorageFixture = new(22, -8);
        public static readonly Vector2Int FullMaterialStorageFixture = new(30, -8);
        public static readonly Vector2Int UnreachableMaterialStorageFixture = new(40, -20);

        private void Awake()
        {
            navigationWorldService ??= GetComponent<GridNavigationWorldService>();
            selectionController ??= GetComponent<AntSelectionCommandController>();
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
                ClearStressGridAnts();
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
            if (!BuildSurvivalFixtures(out string fixtureError))
            {
                Debug.LogError(fixtureError, this);
                return false;
            }
            if (!BuildMaterialStorageFixtures(out fixtureError))
            {
                Debug.LogError(fixtureError, this);
                return false;
            }
            if (!BuildExcavationFixtures(out fixtureError))
            {
                Debug.LogError(fixtureError, this);
                return false;
            }
            ColonyJobRegistry = new ColonyJobRegistry();
            MaterialDrops = new WorldMaterialDropRegistry();
            if (!BuildMaterialDropFixtures(out fixtureError))
            {
                Debug.LogError(fixtureError, this);
                return false;
            }
            ExcavationDesignations = new ExcavationDesignationRegistry(
                WorldData, ColonyJobRegistry);
            ExcavationTransaction = new ExcavationWorldTransaction(
                WorldData, MaterialDrops, OnExcavationCommitted);
            navigationWorldService ??= GetComponent<GridNavigationWorldService>();
            if (navigationWorldService == null ||
                !navigationWorldService.Initialize(
                    generatedWorld.WorldData,
                    AntCollisionProfile.OrdinaryAnt))
            {
                Debug.LogError(
                    "Grid navigation services could not initialise.",
                    this);
                return false;
            }

            tilemapRenderer.BindWorldData(WorldData);
            collisionRenderer.BindWorldData(WorldData);
            RefreshAllChunks();
            ResetDevelopmentAntRecords();
            selectionController.BindInteractionWorld(SurvivalInteractionWorld);
            selectionController.BindColonyJobs(ColonyJobRegistry);
            selectionController.BindExcavation(
                ExcavationDesignations, ExcavationTransaction, MaterialDrops);
            selectionController.BindMaterialStorage(MaterialStorage);
            if (!selectionController.ResetSyntheticJobFixtures(out string jobError))
            {
                Debug.LogError(jobError, this);
                return false;
            }

            if (Application.isPlaying)
            {
                SpawnOrResetAutonomousAnt(out _);
                SpawnOrResetSelectableGridAnts(out _);
            }
            return true;
        }

        private void Update()
        {
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

        private void ResetDevelopmentAntRecords()
        {
            // World regeneration is the explicit development reset boundary.
            // Visual-only rebinding through SpawnOrResetSelectableGridAnts keeps
            // the existing registry and therefore preserves skill and needs data.
            antRegistry = null;
            antBindingRegistry = null;
            selectionController?.ResetSimulationDebugClock();
        }

        /// <summary>Commands the primary production-grid ant.</summary>
        public bool TryAssignGridDestination(
            Vector2Int selectedCell,
            out string message)
        {
            if (gridNavigationAnt == null ||
                !gridNavigationAnt.gameObject.activeInHierarchy)
            {
                message = "Grid destination rejected: primary ant unavailable.";
                HideDestinationMarker();
                return false;
            }

            if (!NavigationWorldService.Map.TryGetAnchor(
                    selectedCell,
                    out GridNavigationAnchor destination) ||
                !gridNavigationAnt.MoveTo(selectedCell))
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
        /// Creates the reusable primary grid-ant prefab. The legacy 2.4A ant is
        /// spawned separately and never controls this Rigidbody2D.
        /// </summary>
        public bool SpawnOrResetGridNavigationAnt(out string message)
        {
            return SpawnOrResetSelectableGridAnts(out message);
        }

        /// <summary>
        /// Creates the three deterministic player-facing ants. They share the
        /// production map and scheduler but retain independent motors and orders.
        /// </summary>
        public bool SpawnOrResetSelectableGridAnts(out string message)
        {
            message = string.Empty;
            if (NavigationWorldService == null ||
                !NavigationWorldService.IsInitialized ||
                primaryGridAntPrefab == null || selectionController == null ||
                !TryFindSelectableSpawnCells(3, out List<Vector2Int> spawnCells,
                    out message))
            {
                return false;
            }

            selectionController.ClearSelectableAnts();
            if (!EnsureDevelopmentAntRecords(out message))
            {
                return false;
            }

            while (selectableGridAnts.Count < spawnCells.Count)
            {
                int number = selectableGridAnts.Count + 1;
                selectableGridAnts.Add(CreateGridNavigationAnt(
                    $"Selectable Grid Ant {number}",
                    includeVisual: true));
            }

            Color32[] colours =
            {
                new(70, 225, 235, 255),
                new(238, 112, 177, 255),
                new(143, 230, 92, 255)
            };
            for (int index = 0; index < spawnCells.Count; index++)
            {
                GridNavigationMotor ant = selectableGridAnts[index];
                ant.gameObject.SetActive(true);
                if (!ant.InitializeNavigation(
                        NavigationWorldService,
                        spawnCells[index]))
                {
                    message = ant.StatusMessage;
                    return false;
                }

                SelectableGridAnt selectable =
                    ant.GetComponent<SelectableGridAnt>();
                if (selectable == null)
                {
                    selectable = ant.gameObject.AddComponent<SelectableGridAnt>();
                }

                AntEntityBinding binding = ant.GetComponent<AntEntityBinding>();
                if (binding == null)
                {
                    binding = ant.gameObject.AddComponent<AntEntityBinding>();
                }

                AntRecord record = GetDevelopmentAntRecord(index);
                binding.Configure(record.Id.Value);
                if (!binding.Bind(antRegistry, antBindingRegistry, out message))
                {
                    return false;
                }

                selectable.Configure(colours[index]);
                selectable.BindInteractionWorld(SurvivalInteractionWorld);
                selectable.BindColonyJobs(ColonyJobRegistry);
                selectionController.RegisterSelectableAnt(selectable);
            }

            gridNavigationAnt = selectableGridAnts[0];
            message =
                $"Spawned {selectableGridAnts.Count} selectable grid ants at " +
                $"{string.Join(", ", spawnCells)}.";
            return true;
        }

        private bool EnsureDevelopmentAntRecords(out string message)
        {
            if (antRegistry != null)
            {
                message = string.Empty;
                return true;
            }

            var colonyId = new ColonyId("development-colony");
            antRegistry = new AntRegistry(colonyId);
            antBindingRegistry = new AntEntityBindingRegistry();
            AntRecord[] records =
            {
                new(
                    new AntId("ant-dev-queen-001"),
                    "Mara",
                    colonyId,
                    AntCaste.Queen,
                    new AntAttributes(7, 5, 8)),
                new(
                    new AntId("ant-dev-worker-002"),
                    "Pip",
                    colonyId,
                    AntCaste.Worker,
                    new AntAttributes(4, 8, 5)),
                new(
                    new AntId("ant-dev-worker-003"),
                    "Tala",
                    colonyId,
                    AntCaste.Worker,
                    new AntAttributes(6, 6, 7))
            };

            foreach (AntRecord record in records)
            {
                if (!antRegistry.TryRegister(record, out message))
                {
                    antRegistry = null;
                    antBindingRegistry = null;
                    return false;
                }
            }

            message = string.Empty;
            return true;
        }

        private AntRecord GetDevelopmentAntRecord(int index)
        {
            string id = index switch
            {
                0 => "ant-dev-queen-001",
                1 => "ant-dev-worker-002",
                _ => "ant-dev-worker-003"
            };
            antRegistry.TryGet(id, out AntRecord record);
            return record;
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
            if ((selectableGridAnts.Count < 3 &&
                    !SpawnOrResetSelectableGridAnts(out message)) ||
                !TryFindSafeSpawnPosition(out Vector2 spawnPosition, out message))
            {
                return false;
            }

            ClearStressGridAnts();
            Vector2Int spawnCell =
                WorldTileCoordinates.WorldPositionToCell(spawnPosition);
            for (int index = 0; index < gridStressGroupSize; index++)
            {
                GridNavigationMotor ant = CreateGridNavigationAnt(
                    $"Grid Stress Ant {index + 1}",
                    includeVisual: index < 40);
                ant.InitializeNavigation(NavigationWorldService, spawnCell);
                gridStressAnts.Add(ant);
            }

            message =
                $"Spawned {gridStressAnts.Count} shared-map stress ants.";
            return true;
        }

        public void ClearGridStressGroup()
        {
            staggeredRequestsActive = false;
            foreach (GridNavigationMotor ant in gridStressAnts)
            {
                ant?.CancelMovement("Stress group cleared.");
            }
            ClearStressGridAnts();
        }

        public void StartSimultaneousGridRequests()
        {
            staggeredRequestsActive = false;
            for (int index = 0; index < gridStressAnts.Count; index++)
            {
                RequestStressDestination(gridStressAnts[index], index);
            }
        }

        public void StartStaggeredGridRequests()
        {
            staggeredRequestIndex = 0;
            staggeredRequestsActive = gridStressAnts.Count > 0;
        }

        public bool CancelFirstGridRequest()
        {
            if (gridStressAnts.Count == 0)
            {
                return false;
            }

            gridStressAnts[0].CancelMovement(
                "First stress-ant request cancelled.");
            return true;
        }

        public void StartGridFallingFixture()
        {
            if (gridNavigationAnt == null)
            {
                return;
            }

            gridNavigationAnt.CancelMovement(
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

        /// <summary>
        /// Finds three grounded anchors with enough horizontal separation to be
        /// easy to distinguish and click at the development camera's home zoom.
        /// </summary>
        public bool TryFindSelectableSpawnCells(
            int count,
            out List<Vector2Int> spawnCells,
            out string message)
        {
            spawnCells = new List<Vector2Int>(count);
            if (!IsWorldDataAvailable || GridNavigationMap == null || count < 1)
            {
                message = "Selectable spawn search requires grid navigation.";
                return false;
            }

            // The survival course begins at the bottom of the existing authored
            // shaft. These three anchors are in one proven connected component,
            // so Mara, Pip and Tala can all reach every accessible 3.2B fixture.
            Vector2Int[] survivalSpawns =
            {
                new(-8, SampleWorldGenerator.MainPassageY),
                new(-5, SampleWorldGenerator.MainPassageY),
                new(-2, SampleWorldGenerator.MainPassageY)
            };
            if (count <= survivalSpawns.Length &&
                survivalSpawns.Take(count).All(cell =>
                    GridNavigationMap.TryGetAnchor(cell, out _)))
            {
                spawnCells.AddRange(survivalSpawns.Take(count));
                message = string.Empty;
                return true;
            }

            int minimumX = minimumChunk.x * WorldTileCoordinates.ChunkSize;
            int maximumX = minimumX + generatedWorld.WidthInCells - 1;
            int maximumDistance = Mathf.Max(spawnSearchRadius, count * 4);
            for (int distance = 0;
                distance <= maximumDistance && spawnCells.Count < count;
                distance++)
            {
                int candidateCount = distance == 0 ? 1 : 2;
                for (int candidateIndex = 0;
                    candidateIndex < candidateCount && spawnCells.Count < count;
                    candidateIndex++)
                {
                    int direction = candidateIndex == 0 ? 1 : -1;
                    int x = preferredSpawnX + distance * direction;
                    if (x < minimumX || x > maximumX ||
                        spawnCells.Exists(cell => Mathf.Abs(cell.x - x) < 3))
                    {
                        continue;
                    }

                    int surfaceY = generatedWorld.SurfaceHeights[x - minimumX];
                    var candidate = new Vector2Int(x, surfaceY + 1);
                    if (GridNavigationMap.TryGetAnchor(candidate, out _))
                    {
                        spawnCells.Add(candidate);
                    }
                }
            }

            if (spawnCells.Count == count)
            {
                message = string.Empty;
                return true;
            }

            message =
                $"Only {spawnCells.Count} of {count} separated selectable " +
                "grid-ant anchors were available.";
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
            GridNavigationWorldService sharedNavigationWorld,
            GridNavigationMotor gridAntPrefab,
            AntAutonomousMotor antPrefab,
            Transform marker,
            SpriteRenderer markerRenderer,
            BackgroundTileMaterialDefinition foodStorage = null,
            BackgroundTileMaterialDefinition waterStorage = null,
            BackgroundTileMaterialDefinition sleeping = null,
            BackgroundTileMaterialDefinition materialStorage = null)
        {
            worldSeed = seed;
            minimumChunk = authoredMinimumChunk;
            sizeInChunks = authoredSizeInChunks;
            skyBackground = sky;
            earthBackground = earth;
            stoneBackground = stoneBackgroundDefinition;
            preparedBackground = prepared;
            foodStorageBackground = foodStorage;
            waterStorageBackground = waterStorage;
            sleepingBackground = sleeping;
            materialStorageBackground = materialStorage;
            soilSolid = soil;
            stoneSolid = stoneSolidDefinition;
            tilemapRenderer = visualRenderer;
            collisionRenderer = terrainCollisionRenderer;
            navigationWorldService = sharedNavigationWorld;
            selectionController ??= GetComponent<AntSelectionCommandController>();
            primaryGridAntPrefab = gridAntPrefab;
            autonomousAntPrefab = antPrefab;
            destinationMarker = marker;
            destinationMarkerRenderer = markerRenderer;
        }

        private bool HasRequiredReferences()
        {
            return skyBackground != null && earthBackground != null &&
                stoneBackground != null && preparedBackground != null &&
                foodStorageBackground != null && waterStorageBackground != null &&
                sleepingBackground != null &&
                materialStorageBackground != null &&
                soilSolid != null && stoneSolid != null && blockingOnlySolid != null &&
                tilemapRenderer != null && collisionRenderer != null &&
                navigationWorldService != null && primaryGridAntPrefab != null;
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
            navigationWorldService?.NotifyTerrainChanged(worldCell);
            foreach (GridNavigationMotor ant in selectableGridAnts)
            {
                ant?.NotifyTerrainChanged();
            }
            foreach (GridNavigationMotor ant in gridStressAnts)
            {
                ant?.NotifyTerrainChanged();
            }
        }

        private void OnExcavationCommitted(Vector2Int worldCell)
        {
            tilemapRenderer.RefreshCell(worldCell);
            collisionRenderer.RefreshCell(worldCell);
            autonomousAnt?.NotifyWorldChanged();
            NotifyGridTerrainChanged(worldCell);
        }

        private GridNavigationMotor CreateGridNavigationAnt(
            string objectName,
            bool includeVisual)
        {
            GridNavigationMotor ant = Instantiate(
                primaryGridAntPrefab,
                transform);
            ant.name = objectName;
            if (!includeVisual)
            {
                foreach (SpriteRenderer renderer in
                    ant.GetComponentsInChildren<SpriteRenderer>())
                {
                    renderer.enabled = false;
                }
            }

            return ant;
        }

        private void ClearStressGridAnts()
        {
            for (int index = gridStressAnts.Count - 1; index >= 0; index--)
            {
                GridNavigationMotor ant = gridStressAnts[index];
                gridStressAnts.RemoveAt(index);
                if (ant != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(ant.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(ant.gameObject);
                    }
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
                gridStressAnts.Count,
                staggeredRequestIndex + staggeredRequestsPerUpdate);
            while (staggeredRequestIndex < end)
            {
                RequestStressDestination(
                    gridStressAnts[staggeredRequestIndex],
                    staggeredRequestIndex);
                staggeredRequestIndex++;
            }

            staggeredRequestsActive =
                staggeredRequestIndex < gridStressAnts.Count;
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
            ant.MoveTo(destination, priority);
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
            yield return foodStorageBackground;
            yield return waterStorageBackground;
            yield return sleepingBackground;
            yield return materialStorageBackground;
        }

        private bool BuildSurvivalFixtures(out string error)
        {
            survivalFixtureCells.Clear();
            SurvivalInteractionWorld = new SurvivalInteractionWorld(WorldData);
            int[] xCoordinates = { -6, -3, 0, 3, 6, 14, 17 };
            for (int index = 0; index < xCoordinates.Length; index++)
            {
                var cell = new Vector2Int(
                    xCoordinates[index], SampleWorldGenerator.MainPassageY);
                BackgroundTileMaterialDefinition definition = index < 2
                    ? foodStorageBackground
                    : index < 4 ? waterStorageBackground : sleepingBackground;
                if (WorldData.SetBackgroundMaterial(cell, definition.StableMaterialId) !=
                    WorldCellMutationResult.Success)
                {
                    error = $"Could not author survival fixture at {cell}.";
                    return false;
                }

                survivalFixtureCells.Add(cell);
                if (index < 2 && !SurvivalInteractionWorld.TryAddStorage(
                        cell, SurvivalResourceKind.Food, "resource.food.development", 6,
                        out error)) return false;
                if (index >= 2 && index < 4 &&
                    !SurvivalInteractionWorld.TryAddStorage(
                        cell, SurvivalResourceKind.Water, "resource.water.development", 8,
                        out error)) return false;
                if (index >= 4 &&
                    !SurvivalInteractionWorld.TryAddSleepingPosition(cell, out error))
                    return false;
            }

            // The existing surrounded pocket gives the context command a valid
            // functional target whose adjacent anchors cannot be reached.
            Vector2Int unreachable = SampleWorldGenerator.GridUnreachableDestination;
            if (WorldData.SetBackgroundMaterial(
                    unreachable, foodStorageBackground.StableMaterialId) !=
                WorldCellMutationResult.Success)
            {
                error = "Could not author the covered survival failure fixture.";
                return false;
            }
            survivalFixtureCells.Add(unreachable);
            if (!SurvivalInteractionWorld.TryAddStorage(
                    unreachable,
                    SurvivalResourceKind.Food,
                    "resource.food.development",
                    3,
                    out error))
            {
                return false;
            }
            error = string.Empty;
            return true;
        }

        private IEnumerable<SolidTileMaterialDefinition> SolidDefinitions()
        {
            yield return soilSolid;
            yield return stoneSolid;
            yield return blockingOnlySolid;
        }

        private bool BuildMaterialStorageFixtures(out string error)
        {
            error = string.Empty;
            MaterialStorage = new MaterialStorageWorld(WorldData);
            // Carve one cell inside intact underground Solid terrain. Its four
            // neighbours remain Solid, so this is a real unreachable fixture
            // without replacing the accepted survival fixture at (12, -16).
            WorldCellMutationResult pocketResult = WorldData.RemoveSolidMaterial(
                UnreachableMaterialStorageFixture);
            if (pocketResult != WorldCellMutationResult.Success &&
                pocketResult != WorldCellMutationResult.SolidTileNotPresent)
            {
                error = "Could not carve the unreachable Material Storage pocket.";
                return false;
            }
            (Vector2Int cell, MaterialStorageFilter filter, string resource,
                int quantity)[] fixtures =
            {
                (GeneralMaterialStorageFixture,
                    MaterialStorageFilter.AllBuildingMaterials,
                    MaterialResourceCatalog.GetStableId(ExcavatedMaterialType.Soil), 5),
                (SoilMaterialStorageFixture, MaterialStorageFilter.Soil, string.Empty, 0),
                (StoneMaterialStorageFixture, MaterialStorageFilter.Stone, string.Empty, 0),
                (FullMaterialStorageFixture, MaterialStorageFilter.AllBuildingMaterials,
                    MaterialResourceCatalog.GetStableId(ExcavatedMaterialType.Stone), 10),
                (UnreachableMaterialStorageFixture,
                    MaterialStorageFilter.AllBuildingMaterials, string.Empty, 0)
            };
            foreach (var fixture in fixtures)
            {
                if (WorldData.SetBackgroundMaterial(fixture.cell,
                        materialStorageBackground.StableMaterialId) !=
                        WorldCellMutationResult.Success ||
                    !MaterialStorage.TryAdd(fixture.cell, fixture.filter,
                        fixture.resource, fixture.quantity,
                        MaterialResourceCatalog.ProvisionalMaximumStack, out error))
                {
                    if (string.IsNullOrEmpty(error))
                        error = $"Could not author Material Storage at {fixture.cell}.";
                    return false;
                }
            }
            error = string.Empty;
            return true;
        }

        private bool BuildExcavationFixtures(out string error)
        {
            (Vector2Int cell, string material)[] fixtures =
            {
                (ExcavationSoilFixture, soilSolid.StableMaterialId),
                (new Vector2Int(4, -6), soilSolid.StableMaterialId),
                (new Vector2Int(5, -6), soilSolid.StableMaterialId),
                (new Vector2Int(6, -6), soilSolid.StableMaterialId),
                (ExcavationStoneFixture, stoneSolid.StableMaterialId),
                (ExcavationOpeningFixture, soilSolid.StableMaterialId),
                (ExcavationInitiallyBlockedFixture, soilSolid.StableMaterialId),
                (NonExcavatableFixture, blockingOnlySolid.StableMaterialId)
            };
            foreach ((Vector2Int cell, string material) in fixtures)
            {
                if (WorldData.SetSolidMaterial(cell, material) !=
                    WorldCellMutationResult.Success)
                {
                    error = $"Could not author excavation fixture at {cell}.";
                    return false;
                }
            }
            error = string.Empty;
            return true;
        }

        private bool BuildMaterialDropFixtures(out string error)
        {
            if (!MaterialDrops.TryAdd("development.drop.soil",
                    ExcavatedMaterialType.Soil, 4, SoilDropFixture,
                    out _, out error) ||
                !MaterialDrops.TryAdd("development.drop.stone",
                    ExcavatedMaterialType.Stone, 3, StoneDropFixture,
                    out _, out error)) return false;
            error = string.Empty;
            return true;
        }
    }
}
