using System.Text;
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
        private const float PanelWidth = 390f;

        [SerializeField] private AntGameDevelopmentWorldHost worldHost;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Vector2Int selectedCell = new(-13, -8);

        private readonly StringBuilder textBuilder = new(512);
        private string statusMessage = "Click a world cell to inspect it.";
        private bool backgroundOnlyVisuals;
        private Vector2 panelScrollPosition;

        public Vector2Int SelectedCell => selectedCell;

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                Vector2 screenPoint = mouse.position.ReadValue();
                if (screenPoint.x > PanelWidth)
                {
                    Vector3 worldPoint = worldCamera.ScreenToWorldPoint(screenPoint);
                    selectedCell = worldHost != null &&
                        worldHost.TilemapRenderer != null
                            ? worldHost.TilemapRenderer.WorldPositionToWorldCell(worldPoint)
                            : WorldTileCoordinates.WorldPositionToCell(worldPoint);
                    if (worldHost != null)
                    {
                        worldHost.TryAssignAutonomousDestination(
                            selectedCell,
                            out statusMessage);
                    }
                    else
                    {
                        statusMessage = "Selection updated; world host unavailable.";
                    }
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
            GUILayout.BeginArea(
                new Rect(12f, 12f, PanelWidth - 20f, Screen.height - 24f),
                GUI.skin.box);
            panelScrollPosition = GUILayout.BeginScrollView(panelScrollPosition);
            GUILayout.Label("ANT GAME DEVELOPMENT WORLD");
            GUILayout.Label("Click terrain to inspect. WASD/arrows pan, wheel zoom,");
            GUILayout.Label("middle-drag pans, Home resets the camera.");
            GUILayout.Space(8f);
            GUILayout.Label(BuildInspectionText());
            GUILayout.Space(8f);

            if (GUILayout.Button("Toggle Foreground Solid (F)"))
            {
                ToggleForeground();
            }
            if (GUILayout.Button("Toggle Prepared Background (P)"))
            {
                TogglePreparedBackground();
            }
            if (GUILayout.Button(backgroundOnlyVisuals
                    ? "Restore Foreground Visuals"
                    : "Isolate Background Visuals"))
            {
                ToggleBackgroundIsolation();
            }
            if (GUILayout.Button("Regenerate Seeded World"))
            {
                statusMessage = worldHost.RebuildWorld()
                    ? "World regenerated from the configured seed."
                    : "World regeneration failed; inspect the Console.";
            }

            GUILayout.Space(8f);
            GUILayout.Label(statusMessage);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        public void Configure(
            AntGameDevelopmentWorldHost host,
            Camera inspectionCamera)
        {
            worldHost = host;
            worldCamera = inspectionCamera;
        }

        private string BuildInspectionText()
        {
            textBuilder.Clear();
            textBuilder.Append("World cell: ").Append(selectedCell).AppendLine();
            textBuilder.Append("Chunk: ")
                .Append(WorldTileCoordinates.GetChunkCoordinate(selectedCell))
                .AppendLine();

            Vector3Int tilemapCell =
                WorldTilemapRenderer.WorldCellToTilemapCell(selectedCell);
            textBuilder.Append("Rendered Background cell: ")
                .Append(tilemapCell)
                .AppendLine();
            AppendAutonomousAntDiagnostics(textBuilder);

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
            builder.Append("Ant state: ").Append(ant.State)
                .Append("   Moving: ").Append(ant.IsMoving)
                .AppendLine();
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
                    : "Background toggle requires exposed underground space.";
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
