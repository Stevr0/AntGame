using System.Text;
using AntColony.Commands;
using AntColony.Navigation;
using AntColony.World;
using AntColony.WorldPresentation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace AntColony.Development
{
    /// <summary>
    /// Small Play Mode panel for authoritative cell, clearance and movement data.
    /// Tile colours are presentation only and are never consulted here.
    /// </summary>
    public sealed class DevelopmentWorldInspector : MonoBehaviour
    {
        [SerializeField] private AntGameDevelopmentWorldHost worldHost;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Vector2Int selectedCell = new(-13, -8);

        private readonly StringBuilder textBuilder = new(512);
        private string statusMessage = "Click a world cell to inspect it.";
        private bool backgroundOnlyVisuals;
        private Vector2 panelScrollPosition;
        private DevelopmentUiCategory lastDrawnCategory;
        private static readonly int[] StressGroupSizes = { 1, 100, 300, 500 };

        public Vector2Int SelectedCell => selectedCell;
        public Rect DeveloperPanelGuiRect
        {
            get
            {
                AntSelectionCommandController controller =
                    worldHost?.SelectionController;
                bool ownsPanel = controller != null &&
                    controller.DevelopmentUiVisible &&
                    controller.ExpandedDevelopmentCategory is
                        DevelopmentUiCategory.World or DevelopmentUiCategory.Debug;
                return ownsPanel ? controller.DevelopmentPanelGuiRect : default;
            }
        }

        private void OnEnable()
        {
            UpdateUiBlocker();
        }

        private void Update()
        {
            UpdateUiBlocker();
            AntSelectionCommandController controller = worldHost?.SelectionController;
            if (controller == null || !controller.WorldInputEnabled) return;
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                Vector2 screenPoint = mouse.position.ReadValue();
                if (worldHost?.SelectionController == null ||
                    !worldHost.SelectionController.IsScreenPointBlocked(screenPoint))
                {
                    Vector3 worldPoint = worldCamera.ScreenToWorldPoint(screenPoint);
                    selectedCell = worldHost != null &&
                        worldHost.TilemapRenderer != null
                            ? worldHost.TilemapRenderer.WorldPositionToWorldCell(worldPoint)
                            : WorldTileCoordinates.WorldPositionToCell(worldPoint);
                    statusMessage = worldHost != null
                        ? $"Inspecting world cell {selectedCell}."
                        : "Selection updated; world host unavailable.";
                }
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.fKey.wasPressedThisFrame)
            {
                ToggleForeground();
            }
            if (keyboard.pKey.wasPressedThisFrame)
            {
                TogglePreparedBackground();
            }
        }

        private void OnGUI()
        {
            AntSelectionCommandController controller = worldHost?.SelectionController;
            if (controller == null || !controller.WorldInputEnabled ||
                !controller.DevelopmentUiVisible ||
                controller.ExpandedDevelopmentCategory is not
                    (DevelopmentUiCategory.World or DevelopmentUiCategory.Debug))
                return;

            // Each category opens at its top rather than inheriting the other
            // category's diagnostic scroll position.
            if (lastDrawnCategory != controller.ExpandedDevelopmentCategory)
            {
                panelScrollPosition = Vector2.zero;
                lastDrawnCategory = controller.ExpandedDevelopmentCategory;
            }

            GUILayout.BeginArea(DeveloperPanelGuiRect, GUI.skin.box);
            panelScrollPosition = GUILayout.BeginScrollView(panelScrollPosition);
            if (controller.ExpandedDevelopmentCategory == DevelopmentUiCategory.World)
                DrawWorldControls(controller);
            else
                DrawDebugControls();
            GUILayout.Space(8f);
            GUILayout.Label(statusMessage);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawWorldControls(AntSelectionCommandController controller)
        {
            GUILayout.Label("WORLD");
            GUILayout.Label($"Inspected cell: {selectedCell}");
            GUILayout.Label("Left-click inspects/selects; right-click issues context orders.");
            if (GUILayout.Button(controller.DesignationMode ==
                    WorldDesignationMode.Excavate
                    ? "Exit Extract Mode (Esc)" : "Enter Extract Mode"))
                controller.SetDesignationMode(controller.DesignationMode ==
                    WorldDesignationMode.Excavate
                        ? WorldDesignationMode.None : WorldDesignationMode.Excavate);
            GUILayout.Space(4f);
            ConstructionRecipe recipe = controller.SelectedConstructionRecipe;
            GUILayout.Label($"Conversion target: {recipe?.Result.DisplayName ?? "Unavailable"}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Previous Type")) controller.CycleConstructionRecipe(-1);
            if (GUILayout.Button("Next Type")) controller.CycleConstructionRecipe(1);
            GUILayout.EndHorizontal();
            if (GUILayout.Button(controller.DesignationMode ==
                    WorldDesignationMode.BackgroundConversion
                    ? "Exit Background Conversion Mode (Esc)"
                    : "Enter Background Conversion Mode"))
                controller.SetDesignationMode(controller.DesignationMode ==
                    WorldDesignationMode.BackgroundConversion
                        ? WorldDesignationMode.None
                        : WorldDesignationMode.BackgroundConversion);
            if (GUILayout.Button("Designate Inspected Cell for Conversion"))
                controller.TryDesignateConstruction(selectedCell, out statusMessage);
            if (GUILayout.Button("Cancel Conversion at Inspected Cell"))
                controller.TryCancelConstruction(selectedCell, out statusMessage);
            if (GUILayout.Button("Command Primary Grid Ant to Inspected Cell"))
            {
                worldHost.TryAssignGridDestination(
                    selectedCell,
                    out statusMessage);
            }
            if (GUILayout.Button("Command Legacy 2.4A Ant to Inspected Cell"))
            {
                worldHost.TryAssignAutonomousDestination(
                    selectedCell,
                    out statusMessage);
            }

            if (GUILayout.Button("Toggle Foreground Solid (F)"))
            {
                ToggleForeground();
            }
            if (GUILayout.Button("Toggle Prepared Background (P)"))
            {
                TogglePreparedBackground();
            }
            if (GUILayout.Button("Move Ant to Grip Fixture Top"))
            {
                SelectGripFixtureTop();
            }
            if (GUILayout.Button("Start Grid Falling / Recovery Fixture"))
            {
                worldHost.StartGridFallingFixture();
                statusMessage = "Grid ant released to Rigidbody2D gravity.";
            }
            if (GUILayout.Button(backgroundOnlyVisuals
                    ? "Restore Foreground Visuals"
                    : "Isolate Background Visuals"))
                ToggleBackgroundIsolation();
            if (GUILayout.Button("Regenerate Seeded World"))
            {
                statusMessage = worldHost.RebuildWorld()
                    ? "World regenerated from the configured seed."
                    : "World regeneration failed; inspect the Console.";
            }
        }

        private void DrawDebugControls()
        {
            GUILayout.Label("DEBUG");
            GUILayout.Label("WASD/arrows pan, wheel zoom, middle-drag pans, Home resets.");
            GUILayout.Label(BuildInspectionText());
            GUILayout.Space(6f);
            GUILayout.Label("GRID SCHEDULER STRESS GROUP");
            GUILayout.BeginHorizontal();
            foreach (int count in StressGroupSizes)
            {
                if (GUILayout.Button(count.ToString()))
                {
                    worldHost.SetGridStressGroupSize(count);
                    statusMessage = $"Stress group size set to {count}.";
                }
            }
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Spawn Selected Grid Group"))
            {
                worldHost.SpawnGridStressGroup(out statusMessage);
            }
            if (GUILayout.Button("Clear Extra Grid Ants"))
            {
                worldHost.ClearGridStressGroup();
                statusMessage = "Extra grid stress ants cleared.";
            }
            if (GUILayout.Button("Start Simultaneous Requests"))
            {
                worldHost.StartSimultaneousGridRequests();
                statusMessage = "All grid route requests queued together.";
            }
            if (GUILayout.Button("Start Staggered Requests"))
            {
                worldHost.StartStaggeredGridRequests();
                statusMessage = "Grid route requests are being staggered.";
            }
            if (GUILayout.Button("Cancel First Grid Request"))
            {
                statusMessage = worldHost.CancelFirstGridRequest()
                    ? "First grid-ant request cancelled."
                    : "No grid-ant request was available to cancel.";
            }
        }

        public void Configure(
            AntGameDevelopmentWorldHost host,
            Camera inspectionCamera)
        {
            worldHost = host;
            worldCamera = inspectionCamera;
            UpdateUiBlocker();
        }

        private void UpdateUiBlocker()
        {
            // The command controller owns the shared toolbar and panel bounds.
            // Clear the legacy secondary blocker so hiding the toolbar restores
            // the full Game view except for the small Show UI button.
            worldHost?.SelectionController?.SetAdditionalUiBlocker(default);
        }

        private string BuildInspectionText()
        {
            textBuilder.Clear();
            textBuilder.Append("World cell: ").Append(selectedCell).AppendLine();
            textBuilder.Append("Chunk: ")
                .Append(WorldTileCoordinates.GetChunkCoordinate(selectedCell))
                .AppendLine();
            if (worldHost != null && worldHost.IsWorldDataAvailable)
            {
                textBuilder.Append("Grip fixture: ")
                    .Append(worldHost.GripFixtureBottom)
                    .Append(" to ")
                    .Append(worldHost.GripFixtureTop)
                    .AppendLine();
            }

            Vector3Int tilemapCell =
                WorldTilemapRenderer.WorldCellToTilemapCell(selectedCell);
            textBuilder.Append("Rendered Background cell: ")
                .Append(tilemapCell)
                .AppendLine();
            AppendInactiveNodeRegistryDiagnostics(textBuilder);
            AppendAutonomousAntDiagnostics(textBuilder);
            AppendGridAntDiagnostics(textBuilder);

            if (worldHost == null ||
                !worldHost.TryGetCell(selectedCell, out WorldTileCellData cell))
            {
                textBuilder.Append("World data: unavailable");
                return textBuilder.ToString();
            }

            ChunkedWorldData worldData = worldHost.WorldData;
            TileBase renderedBackground = worldHost.TilemapRenderer != null &&
                worldHost.TilemapRenderer.BackgroundTilemap != null
                ? worldHost.TilemapRenderer.BackgroundTilemap.GetTile(tilemapCell)
                : null;
            textBuilder.Append("Rendered Background tile: ")
                .Append(renderedBackground != null ? renderedBackground.name : "None")
                .AppendLine();
            AppendRenderingDiagnostics(textBuilder, tilemapCell, renderedBackground);
            textBuilder.Append("Background: ")
                .Append(GetBackgroundName(worldData, cell.BackgroundMaterialId))
                .AppendLine();
            textBuilder.Append("Foreground: ")
                .Append(cell.HasSolidTile
                    ? GetSolidName(worldData, cell.SolidMaterialId)
                    : "None (exposed)")
                .AppendLine();

            bool blocking = cell.HasSolidTile &&
                worldData.TryGetSolidMaterialDefinition(
                    cell.SolidMaterialId,
                    out SolidTileMaterialDefinition solid) &&
                solid.BlocksMovement;
            textBuilder.Append("Blocks movement: ").Append(blocking).AppendLine();

            var evaluator = new AntMovementCapabilityEvaluator(worldData);
            AntMovementCapabilityResult capability = evaluator.Evaluate(
                WorldTileCoordinates.CellToWorldPosition(selectedCell),
                0f,
                AntCollisionProfile.OrdinaryAnt);
            textBuilder.Append("Physical clearance: ")
                .Append(capability.Clearance.Status)
                .AppendLine();
            textBuilder.Append("Background grip: ")
                .Append(capability.HasBackgroundGrip)
                .AppendLine();
            textBuilder.Append("Background speed: x")
                .Append(capability.MovementSpeedMultiplier.ToString("0.##"))
                .AppendLine();
            textBuilder.Append("Eligibility: ")
                .Append(capability.SuggestedState)
                .AppendLine();
            textBuilder.Append("Grounded: ")
                .Append(capability.HasGroundSupport)
                .Append("   Falling: ")
                .Append(capability.ShouldApplyGravity)
                .AppendLine();
            textBuilder.Append("Voluntary directions: ")
                .Append(FormatDirections(capability));
            return textBuilder.ToString();
        }

        private void AppendInactiveNodeRegistryDiagnostics(StringBuilder builder)
        {
            InactiveColonyResourceNodeRegistry registry =
                worldHost?.ColonyResourceNodes;
            if (registry == null)
            {
                builder.AppendLine("Inactive Node registry: unavailable");
                return;
            }

            ColonyResourceNodeRegistryDiagnostics diagnostics =
                registry.Diagnostics;
            builder.Append("Inactive Node registry: ")
                .Append(diagnostics.Lifecycle)
                .Append("   revision ")
                .Append(diagnostics.Revision)
                .AppendLine();
            builder.Append("Node scenario: ")
                .Append(diagnostics.Scenario)
                .AppendLine();
            builder.Append("Nodes total / Soil / Stone: ")
                .Append(diagnostics.NodeCount).Append(" / ")
                .Append(diagnostics.SoilCount).Append(" / ")
                .Append(diagnostics.StoneCount)
                .AppendLine();
            builder.Append("Invalid or stale / unsupported / duplicates: ")
                .Append(diagnostics.InvalidOrStaleCount).Append(" / ")
                .Append(diagnostics.UnsupportedSolidCount).Append(" / ")
                .Append(diagnostics.DuplicateNodeCount)
                .AppendLine();

            RegionCellId selected = RegionCellId.Colony(AntRegionIds.Colony,
                new ColonyCell(selectedCell.x, selectedCell.y));
            InactiveColonyNodeReadResult result = registry.TryGet(
                selected, out InactiveColonyResourceNodeSnapshot node);
            builder.Append("Selected inactive Node: ").Append(result);
            if (result == InactiveColonyNodeReadResult.Success)
            {
                builder.Append("   ").Append(node.DefinitionId)
                    .Append("   ").Append(node.Lifecycle)
                    .Append("   revision ").Append(node.Revision)
                    .AppendLine();
                builder.Append("Node ID: ").Append(node.NodeId).AppendLine();
            }
            else
            {
                builder.AppendLine();
            }
        }

        private void ToggleForeground()
        {
            statusMessage = worldHost != null &&
                worldHost.ToggleForegroundSolid(selectedCell)
                    ? "Foreground data, visual and collision refreshed."
                    : "Foreground toggle rejected: select a generated cell.";
        }

        private void AppendAutonomousAntDiagnostics(StringBuilder builder)
        {
            AntAutonomousMotor ant = worldHost != null
                ? worldHost.AutonomousAnt
                : null;
            if (ant == null || !ant.gameObject.activeInHierarchy)
            {
                builder.AppendLine("Autonomous ant: unavailable");
                return;
            }

            builder.Append("Ant position: ").Append(ant.Position).AppendLine();
            builder.Append("Locomotion: ").Append(ant.LocomotionState).AppendLine();
            builder.Append("Command: ").Append(ant.State)
                .Append("   Moving: ").Append(ant.IsMoving)
                .AppendLine();
            builder.Append("Ant Background: ")
                .Append(string.IsNullOrEmpty(ant.BackgroundMaterialId)
                    ? "Unavailable"
                    : GetBackgroundName(
                        worldHost.WorldData,
                        ant.BackgroundMaterialId))
                .AppendLine();
            builder.Append("Grip: ").Append(ant.HasBackgroundGrip)
                .Append("   Gravity eligible: ")
                .Append(ant.ShouldApplyGravity)
                .AppendLine();
            builder.Append("Movement speed: ")
                .Append(ant.EffectiveMovementSpeed.ToString("0.##"))
                .Append(" (base ")
                .Append(ant.MovementSpeed.ToString("0.##"))
                .AppendLine(")");
            builder.Append("Destination: ")
                .Append(ant.HasDestination ? ant.Destination.ToString() : "None")
                .AppendLine();
            builder.Append("Ant report: ").Append(ant.StatusMessage).AppendLine();
        }

        private void TogglePreparedBackground()
        {
            statusMessage = worldHost != null &&
                worldHost.TogglePreparedBackground(selectedCell)
                    ? $"Background replaced and refreshed at Tilemap cell " +
                        $"{WorldTilemapRenderer.WorldCellToTilemapCell(selectedCell)}."
                    : "Background toggle requires an exposed resident cell.";
        }

        private void AppendGridAntDiagnostics(StringBuilder builder)
        {
            GridNavigationMotor ant = worldHost != null
                ? worldHost.GridNavigationAnt
                : null;
            if (ant == null || !ant.gameObject.activeInHierarchy)
            {
                builder.AppendLine("Primary grid ant: unavailable");
                return;
            }

            builder.Append("Primary grid position: ").Append(ant.Position).AppendLine();
            builder.Append("Primary grid state: ").Append(ant.State)
                .Append("   route step ").Append(ant.RouteIndex)
                .Append('/').Append(ant.Route.Count).AppendLine();
            if (worldHost.GridNavigationMap != null)
            {
                builder.Append("Shared grid cache: ")
                    .Append(worldHost.GridNavigationMap.CachedAnchorCount)
                    .Append(" anchors, ")
                    .Append(worldHost.GridNavigationMap.CachedConnectionCount)
                    .Append(" edges, revision ")
                    .Append(worldHost.GridNavigationMap.Revision)
                    .AppendLine();
            }

            if (worldHost.GridNavigationScheduler != null)
            {
                GridNavigationSchedulerDiagnostics diagnostics =
                    worldHost.GridNavigationScheduler.Diagnostics;
                builder.Append("Selectable / stress grid ants: ")
                    .Append(worldHost.SelectableGridAnts.Count)
                    .Append(" / ")
                    .Append(worldHost.GridNavigationAnts.Count)
                    .Append("   requested stress size ")
                    .Append(worldHost.GridStressGroupSize)
                    .AppendLine();
                builder.Append("Scheduler pending/processing/delivery: ")
                    .Append(diagnostics.Pending).Append(" / ")
                    .Append(diagnostics.Processing).Append(" / ")
                    .Append(diagnostics.CompletedWaitingDelivery)
                    .AppendLine();
                builder.Append("Nodes last/total: ")
                    .Append(diagnostics.NodesExpandedLastUpdate).Append(" / ")
                    .Append(diagnostics.TotalNodesExpanded)
                    .Append("   peak queue ")
                    .Append(diagnostics.PeakQueueLength)
                    .AppendLine();
                builder.Append("Scheduler ms last/max: ")
                    .Append(diagnostics.LastUpdateMilliseconds.ToString("0.###"))
                    .Append(" / ")
                    .Append(diagnostics.MaximumUpdateMilliseconds.ToString("0.###"))
                    .Append("   invalidations ")
                    .Append(diagnostics.CacheInvalidations)
                    .AppendLine();
                builder.Append("Requests submitted/completed/cancelled: ")
                    .Append(diagnostics.TotalSubmitted).Append(" / ")
                    .Append(diagnostics.TotalCompleted).Append(" / ")
                    .Append(diagnostics.TotalCancelled)
                    .AppendLine();
            }

            builder.Append("Primary grid report: ")
                .Append(ant.StatusMessage).AppendLine();
        }

        private void SelectGripFixtureTop()
        {
            if (worldHost == null || !worldHost.IsWorldDataAvailable)
            {
                statusMessage = "Grip fixture unavailable: world data is missing.";
                return;
            }

            selectedCell = worldHost.GripFixtureTop;
            worldHost.TryAssignGridDestination(
                selectedCell,
                out string movementMessage);
            statusMessage =
                $"Grip fixture selected at {selectedCell}. {movementMessage}";
        }

        private void ToggleBackgroundIsolation()
        {
            if (worldHost == null || worldHost.TilemapRenderer == null)
            {
                statusMessage = "Background isolation unavailable: renderer is missing.";
                return;
            }

            backgroundOnlyVisuals = !backgroundOnlyVisuals;
            worldHost.TilemapRenderer.SetBackgroundVisible(true);
            worldHost.TilemapRenderer.SetForegroundVisible(!backgroundOnlyVisuals);
            statusMessage = backgroundOnlyVisuals
                ? "Background isolated; foreground visuals are hidden."
                : "Foreground visuals restored.";
        }

        private void AppendRenderingDiagnostics(
            StringBuilder builder,
            Vector3Int tilemapCell,
            TileBase renderedBackground)
        {
            WorldTilemapRenderer renderer = worldHost.TilemapRenderer;
            if (renderer == null || renderer.BackgroundTilemap == null)
            {
                builder.AppendLine("Background renderer: unavailable");
                return;
            }

            Tilemap tilemap = renderer.BackgroundTilemap;
            TilemapRenderer tilemapRenderer = tilemap.GetComponent<TilemapRenderer>();
            Tile tile = renderedBackground as Tile;
            Color tileColour = tile != null ? tile.color : Color.white;
            Color cellColour = tilemap.GetColor(tilemapCell);
            Color tilemapColour = tilemap.color;
            Color spritePixel = TryGetSpritePixel(tile, out Color sampledPixel)
                ? sampledPixel
                : Color.white;
            Color expectedColour = MultiplyColours(
                cellColour,
                tilemapColour,
                spritePixel);

            builder.Append("Tile asset colour: ")
                .Append(ColorUtility.ToHtmlStringRGBA(tileColour))
                .AppendLine();
            builder.Append("Tilemap cell colour: ")
                .Append(ColorUtility.ToHtmlStringRGBA(cellColour))
                .AppendLine();
            builder.Append("Tilemap component colour: ")
                .Append(ColorUtility.ToHtmlStringRGBA(tilemapColour))
                .AppendLine();
            builder.Append("Sprite pixel: ")
                .Append(ColorUtility.ToHtmlStringRGBA(spritePixel))
                .Append("   Expected: ")
                .Append(ColorUtility.ToHtmlStringRGBA(expectedColour))
                .AppendLine();
            builder.Append("Tile asset flags: ")
                .Append(tile != null ? tile.flags.ToString() : "Unavailable")
                .AppendLine();
            builder.Append("Tilemap cell flags: ")
                .Append(tilemap.GetTileFlags(tilemapCell))
                .AppendLine();

            Material material = tilemapRenderer != null
                ? tilemapRenderer.sharedMaterial
                : null;
            builder.Append("Renderer material/shader: ")
                .Append(material != null ? material.name : "None")
                .Append(" / ")
                .Append(material != null && material.shader != null
                    ? material.shader.name
                    : "None")
                .AppendLine();
            builder.Append("Sorting: ")
                .Append(tilemapRenderer != null
                    ? tilemapRenderer.sortingLayerName
                    : "Unavailable")
                .Append(" / ")
                .Append(tilemapRenderer != null
                    ? tilemapRenderer.sortingOrder
                    : 0)
                .AppendLine();
            builder.Append("Background active/enabled: ")
                .Append(tilemap.gameObject.activeInHierarchy)
                .Append(" / ")
                .Append(tilemapRenderer != null && tilemapRenderer.enabled)
                .AppendLine();
        }

        private static bool TryGetSpritePixel(Tile tile, out Color pixel)
        {
            if (tile != null && tile.sprite != null &&
                tile.sprite.texture != null && tile.sprite.texture.isReadable)
            {
                Rect textureRect = tile.sprite.textureRect;
                int x = Mathf.FloorToInt(textureRect.center.x);
                int y = Mathf.FloorToInt(textureRect.center.y);
                pixel = tile.sprite.texture.GetPixel(x, y);
                return true;
            }

            pixel = Color.white;
            return false;
        }

        private static Color MultiplyColours(params Color[] colours)
        {
            Color result = Color.white;
            foreach (Color colour in colours)
            {
                result.r *= colour.r;
                result.g *= colour.g;
                result.b *= colour.b;
                result.a *= colour.a;
            }

            return result;
        }

        private static string GetBackgroundName(
            ChunkedWorldData worldData,
            string materialId)
        {
            return worldData.TryGetBackgroundMaterialDefinition(
                materialId,
                out BackgroundTileMaterialDefinition material)
                    ? $"{material.DisplayName} ({materialId})"
                    : $"Unknown ({materialId})";
        }

        private static string GetSolidName(
            ChunkedWorldData worldData,
            string materialId)
        {
            return worldData.TryGetSolidMaterialDefinition(
                materialId,
                out SolidTileMaterialDefinition material)
                    ? $"{material.DisplayName} ({materialId})"
                    : $"Unknown ({materialId})";
        }

        private static string FormatDirections(AntMovementCapabilityResult result)
        {
            if (!result.AllowsHorizontalMovement &&
                !result.AllowsUpwardMovement &&
                !result.AllowsDownwardMovement)
            {
                return "None";
            }

            var builder = new StringBuilder();
            if (result.AllowsHorizontalMovement)
            {
                builder.Append("Left, Right");
            }
            AppendDirection(builder, result.AllowsUpwardMovement, "Up");
            AppendDirection(builder, result.AllowsDownwardMovement, "Down");
            return builder.ToString();
        }

        private static void AppendDirection(
            StringBuilder builder,
            bool available,
            string direction)
        {
            if (!available)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append(", ");
            }
            builder.Append(direction);
        }
    }
}
