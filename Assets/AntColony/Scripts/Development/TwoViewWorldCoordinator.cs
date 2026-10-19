using System;
using System.Collections.Generic;
using AntColony.Commands;
using AntColony.Navigation;
using AntColony.PlayerUI;
using AntColony.World;
using AntColony.WorldPresentation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AntColony.Development
{
    public enum DevelopmentWorldView { Colony, Surface }
    public enum SurfaceTerrainRenderMode { LegacySquare, IsometricPreview }

    /// <summary>
    /// WV-B1 composition root. Both regions and all ant records stay loaded; this
    /// component switches only presentation/input and coordinates one entrance.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TwoViewWorldCoordinator : MonoBehaviour
    {
        public const int SurfacePresentationLayer = 9;
        public const string SurfacePresentationLayerName = "SurfaceWorld";
        public static readonly Vector2 SurfaceWorldOrigin = new(192f, 0f);
        public static readonly ColonyCell ColonyApproach = new(-1, -8);
        public static readonly ColonyCell ColonyArrival = new(0, -8);
        public static readonly SurfaceCell SurfaceApproach = new(5, 4);
        public static readonly SurfaceCell SurfaceArrival = new(6, 4);
        public static readonly EntranceId FoundingEntranceId =
            new("entrance.founding-nest.001");

        private AntGameDevelopmentWorldHost host;
        private Camera colonyCamera;
        private Camera surfaceCamera;
        private DevelopmentCameraController colonyCameraController;
        private DevelopmentCameraController surfaceCameraController;
        private SurfaceWorldData surfaceWorld;
        private AntRegionResidencyRegistry residencies;
        private NestEntranceTransitionService entrance;
        private readonly Dictionary<SelectableGridAnt, SurfaceNavigationMotor>
            surfaceMotors = new();
        private readonly Dictionary<SelectableGridAnt, Dictionary<GameObject, int>>
            originalAntPresentationLayers = new();
        private readonly Dictionary<Renderer, bool> originalAntRendererEnabled = new();
        private readonly Dictionary<Renderer, int> originalAntSortingOrders = new();
        private EntranceTransition activeTransition;
        private Transform surfacePresentationRoot;
        private GameObject legacySurfacePresentation;
        private GameObject isometricSurfacePresentation;
        private SurfaceIsometricRenderer surfaceIsometricRenderer;
        private SurfaceIsometricProjection surfaceIsometricProjection;
        private Rect isometricWorldBounds;
        private LineRenderer isometricDestinationMarker;
        private SpriteRenderer isometricEntranceVisual;
        private Material isometricMarkerMaterial;
        private Vector3 legacyCameraPosition;
        private float legacyCameraSize = 36f;
        private Vector3 isometricCameraPosition;
        private float isometricCameraSize = 20f;
        [SerializeField] private SurfaceTerrainRenderMode surfaceTerrainRenderMode =
            SurfaceTerrainRenderMode.LegacySquare;
        private Sprite temporarySprite;
        private string status = "WV-B1 two-view foundation loading.";
        private string seedText = "2203";

        private ColonyCell ActiveColonyApproach => host?.ProceduralWorld?.Entrance.ColonyApproach
            ?? ColonyApproach;
        private ColonyCell ActiveColonyArrival => host?.ProceduralWorld?.Entrance.ColonyArrival
            ?? ColonyArrival;
        private SurfaceCell ActiveSurfaceApproach => host?.ProceduralWorld?.Entrance.SurfaceApproach
            ?? SurfaceApproach;
        private SurfaceCell ActiveSurfaceArrival => host?.ProceduralWorld?.Entrance.SurfaceArrival
            ?? SurfaceArrival;

        public DevelopmentWorldView CurrentView { get; private set; } =
            DevelopmentWorldView.Colony;
        public SurfaceWorldData SurfaceWorld => surfaceWorld;
        public AntRegionResidencyRegistry Residencies => residencies;
        public NestEntranceTransitionService Entrance => entrance;
        public Camera ColonyCamera => colonyCamera;
        public Camera SurfaceCamera => surfaceCamera;
        public Transform SurfacePresentationRoot => surfacePresentationRoot;
        public GameObject LegacySurfacePresentation => legacySurfacePresentation;
        public GameObject IsometricSurfacePresentation => isometricSurfacePresentation;
        public SurfaceIsometricRenderer SurfaceIsometricRenderer =>
            surfaceIsometricRenderer;
        public SurfaceIsometricProjection SurfaceIsometricProjection =>
            surfaceIsometricProjection;
        public Rect IsometricWorldBounds => isometricWorldBounds;
        public DevelopmentCameraController SurfaceCameraController =>
            surfaceCameraController;
        public LineRenderer IsometricDestinationMarker =>
            isometricDestinationMarker;
        public SpriteRenderer IsometricEntranceVisual => isometricEntranceVisual;
        public SurfaceTerrainRenderMode SurfaceTerrainMode =>
            surfaceTerrainRenderMode;
        public string Status => status;

        private void Start()
        {
            host = GetComponent<AntGameDevelopmentWorldHost>();
            if (host == null || host.SelectionController == null ||
                host.SimulationClock == null)
            { enabled = false; return; }
            BuildSurfaceWorld();
            BuildCameras();
            ApplySurfaceTerrainRenderMode();
            InitializeAntResidencies();
            SwitchView(DevelopmentWorldView.Colony);
            status = "Colony View active. Surface remains loaded and simulated.";
            seedText = host.WorldSeed.ToString();
        }

        /// <summary>Rebinds view-only state after the development host changes scenario.</summary>
        public void RebuildScenario()
        {
            activeTransition = null;
            RestoreOriginalAntPresentation();
            surfaceMotors.Clear();
            originalAntPresentationLayers.Clear();
            originalAntRendererEnabled.Clear();
            originalAntSortingOrders.Clear();
            if (surfacePresentationRoot != null) Destroy(surfacePresentationRoot.gameObject);
            if (surfaceCamera != null) Destroy(surfaceCamera.gameObject);
            BuildSurfaceWorld();
            BuildCameras();
            ApplySurfaceTerrainRenderMode();
            InitializeAntResidencies();
            SwitchView(DevelopmentWorldView.Colony);
            seedText = host.WorldSeed.ToString();
            status = host.ActiveScenario == GeneratedWorldScenario.FoundingQueen
                ? host.GenerationDiagnostics.Summary : "Development fixture restored.";
        }

        private void OnDestroy()
        {
            if (temporarySprite != null)
            {
                Texture2D texture = temporarySprite.texture;
                Destroy(temporarySprite);
                if (texture != null) Destroy(texture);
            }
            if (isometricMarkerMaterial != null) Destroy(isometricMarkerMaterial);
        }

        private void Update()
        {
            if (host == null) return;
            SynchronizeCompletedLocalMovement();
            AdvanceTransition();
            RefreshSurfaceDynamicPresentation();
            if (CurrentView == DevelopmentWorldView.Surface)
                HandleSurfaceInput();
        }

        public void SwitchView(DevelopmentWorldView view)
        {
            CurrentView = view;
            bool colonyActive = view == DevelopmentWorldView.Colony;
            if (colonyCamera != null) colonyCamera.enabled = colonyActive;
            if (surfaceCamera != null) surfaceCamera.enabled = !colonyActive;
            colonyCameraController?.SetInputEnabled(colonyActive);
            surfaceCameraController?.SetInputEnabled(!colonyActive);
            host?.SelectionController?.SetWorldInputEnabled(colonyActive);
            host?.PlayerHud?.SetViewContext(colonyActive
                ? PlayerViewContext.Colony : PlayerViewContext.World);
            status = colonyActive
                ? "Colony View active."
                : "World View active: left-click an ant; right-click open terrain to move.";
        }

        /// <summary>
        /// Switches presentation only. The 64x64 Surface authority, navigation,
        /// ant transforms and simulation remain unchanged in both modes.
        /// </summary>
        public bool SetSurfaceTerrainRenderMode(SurfaceTerrainRenderMode mode)
        {
            if (legacySurfacePresentation == null ||
                isometricSurfacePresentation == null) return false;
            RememberSurfaceCameraState(surfaceTerrainRenderMode);
            surfaceTerrainRenderMode = mode;
            ApplySurfaceTerrainRenderMode();
            status = mode == SurfaceTerrainRenderMode.LegacySquare
                ? "Legacy square Surface terrain active."
                : "ISO-WV-2 full 64x64 isometric World View active.";
            return true;
        }

        private void ApplySurfaceTerrainRenderMode()
        {
            bool isometric = surfaceTerrainRenderMode ==
                SurfaceTerrainRenderMode.IsometricPreview;
            legacySurfacePresentation?.SetActive(!isometric);
            isometricSurfacePresentation?.SetActive(isometric);

            if (surfaceCamera != null && surfaceCameraController != null)
            {
                Vector3 position = isometric
                    ? isometricCameraPosition : legacyCameraPosition;
                float size = isometric ? isometricCameraSize : legacyCameraSize;
                surfaceCameraController.ConfigureBounds(
                    isometric
                        ? isometricWorldBounds.min
                        : SurfaceWorldOrigin,
                    isometric
                        ? isometricWorldBounds.max
                        : SurfaceWorldOrigin + new Vector2(64f, 64f),
                    isometric);
                surfaceCameraController.ConfigureHome(position, size);
                surfaceCameraController.SetCameraState(position, size);
            }

            ConfigureSurfaceMotorPresentation();
            RefreshSurfaceAntVisibility();
            RefreshSurfaceDynamicPresentation();
        }

        public bool TryUseEntrance(SelectableGridAnt ant)
        {
            if (ant?.Record == null || !residencies.TryGet(ant.Record.Id,
                    out AntResidency residency))
            { status = "Select a resident ant before using the entrance."; return false; }
            if ((CurrentView == DevelopmentWorldView.Colony &&
                 residency.RegionId != AntRegionIds.Colony) ||
                (CurrentView == DevelopmentWorldView.Surface &&
                 residency.RegionId != AntRegionIds.Surface))
            { status = "The selected ant is not in the active view."; return false; }

            var transitionId = new TransitionId(
                $"transition.{ant.Record.Id.Value}.{residency.Revision + 1}");
            if (!entrance.TryRequest(ant.Record.Id, transitionId,
                    out activeTransition))
            { status = $"Nest entrance: {entrance.CapacityStatus}"; return false; }

            bool accepted;
            if (residency.RegionId == AntRegionIds.Colony)
                accepted = ant.IssueMoveOrder(new Vector2Int(
                    ActiveColonyApproach.X, ActiveColonyApproach.Y));
            else
                accepted = surfaceMotors.TryGetValue(ant, out SurfaceNavigationMotor motor) &&
                    motor.MoveTo(ActiveSurfaceApproach);
            if (!accepted)
            {
                entrance.Cancel(activeTransition.Id);
                status = "The ant could not find a local route to the entrance.";
                return false;
            }
            status = $"{ant.DisplayName} reserved the entrance and is approaching.";
            return true;
        }

        private void AdvanceTransition()
        {
            if (activeTransition == null || activeTransition.IsTerminal) return;
            SelectableGridAnt ant = FindAnt(activeTransition.AntId);
            if (ant == null || ant.Record.LifecycleStatus !=
                AntColony.Ants.AntLifecycleStatus.Active)
            { entrance.Cancel(activeTransition.Id); status =
                "Entrance travel cancelled because the ant became inactive."; return; }

            if (activeTransition.State == EntranceTransitionState.Approaching)
            {
                bool arrived = activeTransition.Source.RegionId == AntRegionIds.Colony
                    ? ant.Motor.HasArrived
                    : surfaceMotors[ant].HasArrived;
                if (!arrived) return;
                entrance.MarkApproachComplete(activeTransition.Id);
            }
            if (activeTransition.State != EntranceTransitionState.ReadyToCommit) return;

            bool committed = entrance.TryCommit(activeTransition.Id,
                host.SimulationClock.IsRunning, ValidateDestination,
                destination => ActivateDestination(ant, destination),
                source => RestoreSource(ant, source));
            status = activeTransition.Status;
            if (committed || activeTransition.IsTerminal)
                entrance.ReleaseTerminal();
        }

        /// <summary>
        /// Keeps occupancy authority aligned with completed local movement. The
        /// active transition owns its ant's approach update, so it is excluded
        /// until the transaction reaches a terminal state.
        /// </summary>
        private void SynchronizeCompletedLocalMovement()
        {
            foreach (SelectableGridAnt ant in host.SelectionController.SelectableAnts)
            {
                if (ant?.Record == null ||
                    (activeTransition != null && !activeTransition.IsTerminal &&
                     activeTransition.AntId.Equals(ant.Record.Id)) ||
                    !residencies.TryGet(ant.Record.Id, out AntResidency residency))
                    continue;

                RegionDestination location;
                if (residency.RegionId == AntRegionIds.Surface &&
                    surfaceMotors.TryGetValue(ant, out SurfaceNavigationMotor surface) &&
                    surface.enabled && surface.HasArrived)
                {
                    location = RegionDestination.Surface(
                        AntRegionIds.Surface, surface.CurrentCell);
                }
                else if (residency.RegionId == AntRegionIds.Colony &&
                    ant.Motor.enabled && ant.Motor.HasArrived)
                {
                    Vector2Int cell = WorldTileCoordinates.WorldPositionToCell(
                        ant.Motor.Position);
                    location = RegionDestination.Colony(
                        AntRegionIds.Colony, new ColonyCell(cell.x, cell.y));
                }
                else continue;

                residencies.TryRelocate(ant.Record.Id, residency.Revision,
                    location, out _);
            }
        }

        private bool ValidateDestination(RegionDestination destination)
        {
            if (destination.Kind == RegionLocationKind.Surface &&
                !surfaceWorld.IsTraversable(destination.SurfaceCell)) return false;
            foreach (SelectableGridAnt ant in host.SelectionController.SelectableAnts)
            {
                if (ant?.Record == null || ant.Record.Id.Equals(activeTransition.AntId) ||
                    !residencies.TryGet(ant.Record.Id, out AntResidency other)) continue;
                if (other.Location.Equals(destination)) return false;
            }
            return true;
        }

        private bool ActivateDestination(SelectableGridAnt ant,
            RegionDestination destination)
        {
            if (destination.Kind == RegionLocationKind.Surface)
            {
                ant.CancelOrder("Colony approach completed.");
                ant.Motor.CancelMovement("Ant transferred to Surface.");
                ant.Motor.enabled = false;
                SurfaceNavigationMotor motor = surfaceMotors[ant];
                motor.enabled = true;
                bool initialized = motor.Initialize(surfaceWorld, host.SimulationClock,
                    SurfaceWorldOrigin, destination.SurfaceCell);
                if (initialized)
                {
                    ConfigureSurfaceMotorPresentation(motor);
                    SetAntPresentationRegion(ant, true);
                }
                return initialized;
            }

            SurfaceNavigationMotor surfaceMotor = surfaceMotors[ant];
            surfaceMotor.CancelMovement("Ant transferred to Colony.");
            surfaceMotor.enabled = false;
            ant.Motor.enabled = true;
            bool activated = ant.Motor.InitializeNavigation(host.NavigationWorldService,
                new Vector2Int(destination.ColonyCell.X, destination.ColonyCell.Y));
            if (activated) SetAntPresentationRegion(ant, false);
            return activated;
        }

        private void RestoreSource(SelectableGridAnt ant, AntResidency source)
        {
            if (source.Location.Kind == RegionLocationKind.Colony)
            {
                ant.Motor.enabled = true;
                ant.Motor.InitializeNavigation(host.NavigationWorldService,
                    new Vector2Int(source.Location.ColonyCell.X,
                        source.Location.ColonyCell.Y));
                surfaceMotors[ant].enabled = false;
                SetAntPresentationRegion(ant, false);
            }
            else
            {
                ant.Motor.enabled = false;
                surfaceMotors[ant].enabled = true;
                surfaceMotors[ant].Initialize(surfaceWorld, host.SimulationClock,
                    SurfaceWorldOrigin, source.Location.SurfaceCell);
                ConfigureSurfaceMotorPresentation(surfaceMotors[ant]);
                SetAntPresentationRegion(ant, true);
            }
        }

        private void HandleSurfaceInput()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || surfaceCamera == null) return;
            Vector2 screen = mouse.position.ReadValue();
            if (screen.y >= Screen.height - 92f ||
                host.SelectionController.IsScreenPointBlocked(screen)) return;
            Vector2 worldPoint = surfaceCamera.ScreenToWorldPoint(screen);
            if (mouse.leftButton.wasPressedThisFrame)
            {
                SelectableGridAnt best = null;
                foreach (SelectableGridAnt candidate in host.SelectionController.SelectableAnts)
                    if (candidate?.Record != null &&
                        residencies.IsInRegion(candidate.Record.Id, AntRegionIds.Surface) &&
                        candidate.ContainsWorldPoint(worldPoint)) best = candidate;
                host.SelectionController.Select(best);
                status = best != null ? $"Selected {best.DisplayName} in World View."
                    : "No Surface ant selected.";
            }
            if (mouse.rightButton.wasPressedThisFrame &&
                host.SelectionController.SelectedAnt is SelectableGridAnt selected &&
                selected.Record != null &&
                residencies.IsInRegion(selected.Record.Id, AntRegionIds.Surface) &&
                TrySurfaceWorldPointToCell(worldPoint, out SurfaceCell cell))
            {
                status = surfaceMotors[selected].MoveTo(cell)
                    ? $"{selected.DisplayName} moving to Surface {cell}."
                    : surfaceMotors[selected].StatusMessage;
            }
        }

        /// <summary>
        /// Converts the active Surface presentation point through its one mapping
        /// authority. It never queries or changes traversability by itself.
        /// </summary>
        public bool TrySurfaceWorldPointToCell(Vector2 worldPoint,
            out SurfaceCell cell)
        {
            if (surfaceTerrainRenderMode == SurfaceTerrainRenderMode.IsometricPreview &&
                surfaceIsometricProjection != null)
            {
                return surfaceIsometricProjection.TryWorldToCell(
                    worldPoint, new SurfaceCell(0, 0),
                    surfaceWorld?.Width ?? 0, surfaceWorld?.Height ?? 0,
                    out cell);
            }

            cell = new SurfaceCell(
                Mathf.FloorToInt(worldPoint.x - SurfaceWorldOrigin.x),
                Mathf.FloorToInt(worldPoint.y - SurfaceWorldOrigin.y));
            return surfaceWorld != null && surfaceWorld.Contains(cell);
        }

        private void InitializeAntResidencies()
        {
            residencies = new AntRegionResidencyRegistry();
            entrance = new NestEntranceTransitionService(residencies,
                host.ProceduralWorld == null
                    ? new NestEntranceDefinition(FoundingEntranceId, ColonyApproach,
                        ColonyArrival, SurfaceApproach, SurfaceArrival)
                    : new NestEntranceDefinition(FoundingEntranceId,
                        host.ProceduralWorld.Entrance.ColonyApproach,
                        host.ProceduralWorld.Entrance.ColonyArrival,
                        host.ProceduralWorld.Entrance.SurfaceApproach,
                        host.ProceduralWorld.Entrance.SurfaceArrival));
            host.SelectionController.BindRegionResidencies(residencies);
            foreach (SelectableGridAnt ant in host.SelectionController.SelectableAnts)
            {
                Vector2Int cell = WorldTileCoordinates.WorldPositionToCell(
                    ant.Motor.Position);
                residencies.Register(ant.Record.Id, RegionDestination.Colony(
                    AntRegionIds.Colony, new ColonyCell(cell.x, cell.y)));
                SurfaceNavigationMotor motor = ant.GetComponent<SurfaceNavigationMotor>();
                if (motor == null) motor = ant.gameObject.AddComponent<SurfaceNavigationMotor>();
                motor.enabled = false;
                surfaceMotors.Add(ant, motor);
                ConfigureSurfaceMotorPresentation(motor);
                SetAntPresentationRegion(ant, false);
            }
        }

        /// <summary>
        /// Changes only GameObjects that own renderers. The ant's authoritative
        /// root keeps its original physics layer, so presentation isolation cannot
        /// alter collision, navigation, orders, or simulation.
        /// </summary>
        private void SetAntPresentationRegion(SelectableGridAnt ant, bool surface)
        {
            if (ant == null) return;
            if (!originalAntPresentationLayers.TryGetValue(ant, out
                    Dictionary<GameObject, int> originalLayers))
            {
                originalLayers = new Dictionary<GameObject, int>();
                originalAntPresentationLayers.Add(ant, originalLayers);
            }

            foreach (Renderer renderer in ant.GetComponentsInChildren<Renderer>(true))
            {
                GameObject visual = renderer.gameObject;
                if (!originalLayers.ContainsKey(visual))
                    originalLayers.Add(visual, visual.layer);
                if (!originalAntRendererEnabled.ContainsKey(renderer))
                    originalAntRendererEnabled.Add(renderer, renderer.enabled);
                if (!originalAntSortingOrders.ContainsKey(renderer))
                    originalAntSortingOrders.Add(renderer, renderer.sortingOrder);
                visual.layer = surface
                    ? SurfacePresentationLayer
                    : originalLayers[visual];
                renderer.enabled = originalAntRendererEnabled[renderer];
                renderer.sortingOrder = surface && surfaceTerrainRenderMode ==
                    SurfaceTerrainRenderMode.IsometricPreview
                        ? CalculateIsometricSortingOrder(ant.transform.position.y,
                            originalAntSortingOrders[renderer])
                        : originalAntSortingOrders[renderer];
            }
        }

        private void RefreshSurfaceAntVisibility()
        {
            if (residencies == null || host?.SelectionController == null) return;
            foreach (SelectableGridAnt ant in host.SelectionController.SelectableAnts)
            {
                if (ant?.Record == null) continue;
                bool surface = residencies.IsInRegion(
                    ant.Record.Id, AntRegionIds.Surface);
                SetAntPresentationRegion(ant, surface);
            }
        }

        private void RestoreOriginalAntPresentation()
        {
            foreach (KeyValuePair<Renderer, bool> pair in originalAntRendererEnabled)
                if (pair.Key != null) pair.Key.enabled = pair.Value;

            foreach (KeyValuePair<Renderer, int> pair in originalAntSortingOrders)
                if (pair.Key != null) pair.Key.sortingOrder = pair.Value;

            foreach (KeyValuePair<SelectableGridAnt, Dictionary<GameObject, int>> ant
                     in originalAntPresentationLayers)
                foreach (KeyValuePair<GameObject, int> visual in ant.Value)
                    if (visual.Key != null) visual.Key.layer = visual.Value;
        }

        private SelectableGridAnt FindAnt(AntColony.Ants.AntId antId)
        {
            foreach (SelectableGridAnt ant in host.SelectionController.SelectableAnts)
                if (ant?.Record != null && ant.Record.Id.Equals(antId)) return ant;
            return null;
        }

        private void BuildCameras()
        {
            colonyCamera = host.SelectionController.WorldCamera;
            colonyCameraController = colonyCamera != null
                ? colonyCamera.GetComponent<DevelopmentCameraController>() : null;
            if (colonyCamera == null) return;
            Vector2 colonyMinimum = new(host.MinimumChunk.x *
                WorldTileCoordinates.ChunkSize, host.MinimumChunk.y *
                WorldTileCoordinates.ChunkSize);
            Vector2 colonyMaximum = colonyMinimum + new Vector2(
                host.SizeInChunks.x * WorldTileCoordinates.ChunkSize,
                host.SizeInChunks.y * WorldTileCoordinates.ChunkSize);
            colonyCameraController?.ConfigureBounds(colonyMinimum, colonyMaximum);
            var cameraObject = new GameObject("World View Camera");
            cameraObject.transform.SetParent(transform, false);
            cameraObject.transform.position = new Vector3(
                SurfaceWorldOrigin.x + 32f, SurfaceWorldOrigin.y + 32f, -10f);
            surfaceCamera = cameraObject.AddComponent<Camera>();
            surfaceCamera.CopyFrom(colonyCamera);
            int surfaceMask = 1 << SurfacePresentationLayer;
            // Spatial separation is useful for authoring, but culling is the
            // authoritative view boundary and remains correct at wide aspect ratios.
            colonyCamera.cullingMask &= ~surfaceMask;
            surfaceCamera.cullingMask = surfaceMask;
            surfaceCamera.orthographic = true;
            surfaceCamera.orthographicSize = 36f;
            surfaceCamera.backgroundColor = new Color32(85, 128, 83, 255);
            surfaceCameraController = cameraObject.AddComponent<DevelopmentCameraController>();
            legacyCameraPosition = cameraObject.transform.position;
            legacyCameraSize = 36f;
            Vector2 isometricCentre = isometricWorldBounds.center;
            isometricCameraPosition = new Vector3(
                isometricCentre.x, isometricCentre.y, -10f);
            isometricCameraSize = CalculateIsometricHomeSize(
                isometricWorldBounds, surfaceCamera.aspect, 1f);
            surfaceCameraController.ConfigureBounds(SurfaceWorldOrigin,
                SurfaceWorldOrigin + new Vector2(64f, 64f));
            surfaceCameraController.ConfigureHome(legacyCameraPosition,
                legacyCameraSize);
            surfaceCamera.enabled = false;
        }

        private void BuildSurfaceWorld()
        {
            // The host publishes this exact instance through the unified read
            // model. The view never creates a competing Surface data store.
            surfaceWorld = host.SurfaceWorld;
            if (surfaceWorld == null)
            {
                Debug.LogError("Surface runtime data was not published.", this);
                return;
            }
            var root = new GameObject(host.ProceduralWorld == null
                ? "Surface World (WV-B1 Fixture)" : "Surface World (WV-B2B Generated)");
            root.layer = SurfacePresentationLayer;
            root.transform.SetParent(transform, false);
            surfacePresentationRoot = root.transform;

            legacySurfacePresentation = new GameObject("Legacy Square Presentation");
            legacySurfacePresentation.layer = SurfacePresentationLayer;
            legacySurfacePresentation.transform.SetParent(root.transform, false);
            temporarySprite = CreateTemporarySprite();
            BuildLegacySurfacePresentation(legacySurfacePresentation.transform);

            isometricSurfacePresentation = new GameObject(
                "ISO-WV-2 Isometric 64x64 Presentation",
                typeof(SurfaceIsometricProjection),
                typeof(SurfaceIsometricRenderer));
            isometricSurfacePresentation.layer = SurfacePresentationLayer;
            isometricSurfacePresentation.transform.SetParent(root.transform, false);
            surfaceIsometricProjection =
                isometricSurfacePresentation.GetComponent<SurfaceIsometricProjection>();
            float centreX = (surfaceWorld.Width - 1) * 0.5f;
            float centreY = (surfaceWorld.Height - 1) * 0.5f;
            Vector2 centreOffset = new(
                (centreX - centreY) * 0.5f,
                -(centreX + centreY) * 0.25f);
            Vector2 presentationCentre = SurfaceWorldOrigin + new Vector2(32f, 32f);
            surfaceIsometricProjection.Configure(
                presentationCentre - centreOffset,
                SurfaceIsometricProjection.DefaultTileWidthPixels,
                SurfaceIsometricProjection.DefaultTileHeightPixels,
                SurfaceIsometricProjection.DefaultPixelsPerUnit);
            isometricWorldBounds = surfaceIsometricProjection.GetWorldBounds(
                new SurfaceCell(0, 0), surfaceWorld.Width, surfaceWorld.Height);

            surfaceIsometricRenderer =
                isometricSurfacePresentation.GetComponent<SurfaceIsometricRenderer>();
            SurfaceIsometricVisualCatalog visualCatalog =
                Resources.Load<SurfaceIsometricVisualCatalog>(
                    "SurfaceIsometricVisualCatalog");
            surfaceIsometricRenderer.Configure(
                host.RuntimeCells?.Current?.Surface,
                surfaceIsometricProjection,
                visualCatalog,
                SurfacePresentationLayer);
            SurfaceIsometricRenderResult isometricResult =
                surfaceIsometricRenderer.RenderRegion(new SurfaceCell(0, 0),
                    surfaceWorld.Width, surfaceWorld.Height);
            if (!isometricResult.Succeeded)
                Debug.LogError(
                    $"ISO-WV-2 world could not render: {isometricResult.Status} " +
                    $"at {isometricResult.ProblemCell}.", this);

            BuildIsometricDynamicPresentation();

            legacySurfacePresentation.SetActive(surfaceTerrainRenderMode ==
                SurfaceTerrainRenderMode.LegacySquare);
            isometricSurfacePresentation.SetActive(surfaceTerrainRenderMode ==
                SurfaceTerrainRenderMode.IsometricPreview);
        }

        private void BuildLegacySurfacePresentation(Transform root)
        {
            CreateVisual(root, "Surface Terrain",
                SurfaceWorldOrigin + new Vector2(32f, 32f), new Vector2(64f, 64f),
                new Color32(108, 151, 91, 255), -20);
            if (host.ProceduralWorld == null)
            {
                foreach (SurfaceCell obstacle in surfaceWorld.Obstacles)
                    CreateVisual(root, $"Obstacle {obstacle}",
                        SurfaceWorldOrigin + new Vector2(obstacle.X + 0.5f,
                            obstacle.Y + 0.5f), Vector2.one,
                        new Color32(80, 70, 61, 255), -10);
            }
            else
            {
                for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                {
                    GeneratedCellRecord cell = host.ProceduralWorld.SurfaceCells[x, y];
                    Color32 colour;
                    if (cell.BackgroundDefinitionId == ProceduralWorldGenerator.WaterBackgroundId)
                        colour = new Color32(63, 137, 190, 255);
                    else if (cell.Node?.DefinitionId == ProceduralWorldGenerator.StoneNodeId)
                        colour = new Color32(118, 119, 112, 255);
                    else if (cell.Node?.DefinitionId == ProceduralWorldGenerator.SeedNodeId)
                        colour = new Color32(232, 200, 93, 255);
                    else if (cell.Node?.DefinitionId == ProceduralWorldGenerator.TwigNodeId)
                        colour = new Color32(126, 82, 46, 255);
                    else if (cell.Node == null) continue;
                    else colour = new Color32(67, 90, 54, 255);
                    CreateVisual(root, $"Generated {x},{y}",
                        SurfaceWorldOrigin + new Vector2(x + 0.5f, y + 0.5f),
                        Vector2.one, colour, -10);
                }
            }
            CreateVisual(root, "Founding Nest Entrance",
                SurfaceWorldOrigin + new Vector2(ActiveSurfaceArrival.X + 0.5f,
                    ActiveSurfaceArrival.Y + 0.5f), new Vector2(2f, 2f),
                new Color32(38, 26, 20, 255), -5);
        }

        private void BuildIsometricDynamicPresentation()
        {
            var entranceObject = new GameObject("Isometric Nest Entrance");
            entranceObject.layer = SurfacePresentationLayer;
            entranceObject.transform.SetParent(isometricSurfacePresentation.transform,
                false);
            entranceObject.transform.position =
                surfaceIsometricProjection.CellToWorld(ActiveSurfaceArrival);
            entranceObject.transform.localScale = new Vector3(0.6f, 0.3f, 1f);
            isometricEntranceVisual = entranceObject.AddComponent<SpriteRenderer>();
            isometricEntranceVisual.sprite = temporarySprite;
            isometricEntranceVisual.color = new Color32(38, 26, 20, 255);
            isometricEntranceVisual.sortingOrder = CalculateIsometricSortingOrder(
                entranceObject.transform.position.y, 0);

            var markerObject = new GameObject("Isometric Destination Marker");
            markerObject.layer = SurfacePresentationLayer;
            markerObject.transform.SetParent(isometricSurfacePresentation.transform,
                false);
            isometricDestinationMarker = markerObject.AddComponent<LineRenderer>();
            isometricDestinationMarker.useWorldSpace = false;
            isometricDestinationMarker.loop = true;
            isometricDestinationMarker.positionCount = 4;
            isometricDestinationMarker.startWidth = 0.035f;
            isometricDestinationMarker.endWidth = 0.035f;
            isometricDestinationMarker.startColor = new Color32(255, 226, 90, 255);
            isometricDestinationMarker.endColor = new Color32(255, 226, 90, 255);
            isometricDestinationMarker.SetPosition(0, new Vector3(0f, 0.14f, 0f));
            isometricDestinationMarker.SetPosition(1, new Vector3(0.28f, 0f, 0f));
            isometricDestinationMarker.SetPosition(2, new Vector3(0f, -0.14f, 0f));
            isometricDestinationMarker.SetPosition(3, new Vector3(-0.28f, 0f, 0f));
            if (isometricMarkerMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null) isometricMarkerMaterial = new Material(shader)
                    { name = "ISO-WV-2 Destination Marker Material" };
            }
            if (isometricMarkerMaterial != null)
                isometricDestinationMarker.sharedMaterial = isometricMarkerMaterial;
            isometricDestinationMarker.enabled = false;
        }

        private void ConfigureSurfaceMotorPresentation()
        {
            foreach (SurfaceNavigationMotor motor in surfaceMotors.Values)
                ConfigureSurfaceMotorPresentation(motor);
        }

        private void ConfigureSurfaceMotorPresentation(SurfaceNavigationMotor motor)
        {
            if (motor == null) return;
            if (surfaceTerrainRenderMode == SurfaceTerrainRenderMode.IsometricPreview &&
                surfaceIsometricProjection != null)
            {
                motor.ConfigurePresentation(
                    surfaceIsometricProjection.LogicalToWorld,
                    ResolveIsometricSurfaceCell);
            }
            else
            {
                motor.RestoreLegacyPresentation();
            }
        }

        private SurfaceCell? ResolveIsometricSurfaceCell(Vector2 worldPosition)
        {
            return surfaceIsometricProjection != null && surfaceWorld != null &&
                surfaceIsometricProjection.TryWorldToCell(worldPosition,
                    new SurfaceCell(0, 0), surfaceWorld.Width, surfaceWorld.Height,
                    out SurfaceCell cell)
                ? cell
                : null;
        }

        private void RefreshSurfaceDynamicPresentation()
        {
            if (surfaceTerrainRenderMode != SurfaceTerrainRenderMode.IsometricPreview ||
                surfaceIsometricProjection == null) return;

            if (residencies != null && host?.SelectionController != null)
            {
                foreach (SelectableGridAnt ant in host.SelectionController.SelectableAnts)
                {
                    if (ant?.Record == null || !residencies.IsInRegion(
                            ant.Record.Id, AntRegionIds.Surface)) continue;
                    SetAntPresentationRegion(ant, true);
                }
            }

            SurfaceNavigationMotor selectedMotor = null;
            bool showDestination = CurrentView == DevelopmentWorldView.Surface &&
                host?.SelectionController?.SelectedAnt is SelectableGridAnt selected &&
                selected.Record != null && residencies != null &&
                residencies.IsInRegion(selected.Record.Id, AntRegionIds.Surface) &&
                surfaceMotors.TryGetValue(selected, out selectedMotor) &&
                selectedMotor.Route.Count > 0 &&
                (selectedMotor.State == SurfaceNavigationState.Moving ||
                 selectedMotor.State == SurfaceNavigationState.Arrived);
            if (isometricDestinationMarker == null) return;
            isometricDestinationMarker.enabled = showDestination;
            if (!showDestination) return;
            Vector2 position = surfaceIsometricProjection.CellToWorld(
                selectedMotor.Destination);
            isometricDestinationMarker.transform.position = position;
            isometricDestinationMarker.sortingOrder =
                CalculateIsometricSortingOrder(position.y, 50);
        }

        private int CalculateIsometricSortingOrder(float groundY, int offset)
        {
            float originY = surfaceIsometricProjection != null
                ? surfaceIsometricProjection.Origin.y : 0f;
            return 100 + Mathf.RoundToInt((originY - groundY) * 100f) + offset;
        }

        private void RememberSurfaceCameraState(SurfaceTerrainRenderMode mode)
        {
            if (surfaceCamera == null) return;
            if (mode == SurfaceTerrainRenderMode.IsometricPreview)
            {
                isometricCameraPosition = surfaceCamera.transform.position;
                isometricCameraSize = surfaceCamera.orthographicSize;
            }
            else
            {
                legacyCameraPosition = surfaceCamera.transform.position;
                legacyCameraSize = surfaceCamera.orthographicSize;
            }
        }

        public static float CalculateIsometricHomeSize(Rect bounds, float aspect,
            float margin)
        {
            float safeAspect = Mathf.Max(0.01f, aspect);
            return Mathf.Max(bounds.height * 0.5f + margin,
                (bounds.width * 0.5f + margin) / safeAspect);
        }

        private void CreateVisual(Transform parent, string name, Vector2 position,
            Vector2 scale, Color colour, int sortingOrder)
        {
            var item = new GameObject(name);
            item.layer = SurfacePresentationLayer;
            item.transform.SetParent(parent, false);
            item.transform.position = position;
            item.transform.localScale = scale;
            SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = temporarySprite;
            renderer.color = colour;
            renderer.sortingOrder = sortingOrder;
        }

        private static Sprite CreateTemporarySprite()
        {
            var texture = new Texture2D(1, 1) { name = "WV-B1 Temporary Pixel" };
            texture.SetPixel(0, 0, Color.white); texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f), 1f);
        }

        private void OnGUI()
        {
            if (host == null) return;
            var controlsRect = new Rect(
                Screen.width * 0.5f - 300f, 8f, 600f, 144f);
            host.SelectionController.SetAdditionalUiBlocker(controlsRect);
            GUILayout.BeginArea(controlsRect,
                GUI.skin.box);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Colony View", GUILayout.Height(28f)))
                SwitchView(DevelopmentWorldView.Colony);
            if (GUILayout.Button("World View", GUILayout.Height(28f)))
                SwitchView(DevelopmentWorldView.Surface);
            GUI.enabled = host.SelectionController.SelectedAnt != null;
            if (GUILayout.Button("Use Nest Entrance", GUILayout.Height(28f)))
                TryUseEntrance(host.SelectionController.SelectedAnt);
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Seed", GUILayout.Width(38f));
            seedText = GUILayout.TextField(seedText, GUILayout.Width(150f));
            if (GUILayout.Button("Start Generated", GUILayout.Height(24f)) &&
                long.TryParse(seedText, out long seed))
                host.TryStartScenario(GeneratedWorldScenario.FoundingQueen,
                    seed, out status);
            if (GUILayout.Button("Restore Fixture", GUILayout.Height(24f)))
                host.TryStartScenario(GeneratedWorldScenario.DevelopmentFixture,
                    host.WorldSeed, out status);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUI.enabled = surfaceTerrainRenderMode !=
                SurfaceTerrainRenderMode.LegacySquare;
            if (GUILayout.Button("Legacy Surface", GUILayout.Height(24f)))
                SetSurfaceTerrainRenderMode(SurfaceTerrainRenderMode.LegacySquare);
            GUI.enabled = surfaceTerrainRenderMode !=
                SurfaceTerrainRenderMode.IsometricPreview;
            if (GUILayout.Button("Isometric 64x64 World", GUILayout.Height(24f)))
                SetSurfaceTerrainRenderMode(
                    SurfaceTerrainRenderMode.IsometricPreview);
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.Label(status);
            GUILayout.EndArea();
        }
    }
}
