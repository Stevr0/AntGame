using System.Text;
using AntColony.World;
using UnityEngine;
using UnityEngine.InputSystem;

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
                    selectedCell = WorldTileCoordinates.WorldPositionToCell(worldPoint);
                    statusMessage = "Selection updated.";
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
            if (GUILayout.Button("Regenerate Seeded World"))
            {
                statusMessage = worldHost.RebuildWorld()
                    ? "World regenerated from the configured seed."
                    : "World regeneration failed; inspect the Console.";
            }

            GUILayout.Space(8f);
            GUILayout.Label(statusMessage);
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

            if (worldHost == null ||
                !worldHost.TryGetCell(selectedCell, out WorldTileCellData cell))
            {
                textBuilder.Append("World data: unavailable");
                return textBuilder.ToString();
            }

            ChunkedWorldData worldData = worldHost.WorldData;
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

        private void TogglePreparedBackground()
        {
            statusMessage = worldHost != null &&
                worldHost.TogglePreparedBackground(selectedCell)
                    ? "Background replaced; foreground was preserved."
                    : "Background toggle requires exposed underground space.";
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
