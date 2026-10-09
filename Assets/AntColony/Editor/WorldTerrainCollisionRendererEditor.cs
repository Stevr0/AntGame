using System.Linq;
using AntColony.NavigationDebug;
using AntColony.World;
using AntColony.WorldPresentation;
using UnityEditor;
using UnityEngine;

namespace AntColony.EditorTools
{
    /// <summary>Development controls for inspecting the collision-only projection.</summary>
    [CustomEditor(typeof(WorldTerrainCollisionRenderer))]
    public sealed class WorldTerrainCollisionRendererEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var collisionRenderer = (WorldTerrainCollisionRenderer)target;
            Vector2Int inspectedCell = GetInspectedCell(collisionRenderer);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Collision Inspection", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("World Cell", inspectedCell.ToString());

            bool resolved = collisionRenderer.TryGetAuthoritativeBlockingState(
                inspectedCell,
                out bool blocksMovement);
            EditorGUILayout.LabelField(
                "Authoritative Blocking",
                resolved ? blocksMovement.ToString() : "Unavailable");
            bool projected = collisionRenderer.CollisionTilemap != null &&
                collisionRenderer.CollisionTilemap.HasTile(
                    WorldTilemapRenderer.WorldCellToTilemapCell(inspectedCell));
            EditorGUILayout.LabelField("Collision Tile Projected", projected.ToString());

            if (GUILayout.Button("Refresh Inspected Cell Collision"))
            {
                collisionRenderer.RefreshCell(inspectedCell);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Refresh Selected Collision Region"))
            {
                collisionRenderer.RefreshSelectedRegion();
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Toggle Test Solid and Refresh All"))
            {
                NavigationTestWorldHost host =
                    collisionRenderer.WorldDataProvider as NavigationTestWorldHost;
                if (host != null && host.ToggleSolidAt(
                    NavigationTestWorldHost.TilemapMutationCell))
                {
                    FindInScene<WorldTilemapRenderer>(collisionRenderer)?.RefreshCell(
                        NavigationTestWorldHost.TilemapMutationCell);
                    collisionRenderer.RefreshCell(
                        NavigationTestWorldHost.TilemapMutationCell);
                    FindInScene<NavigationDebugRenderer>(collisionRenderer)
                        ?.RefreshOverlay();
                    SceneView.RepaintAll();
                }
            }

            if (!string.IsNullOrEmpty(collisionRenderer.LastDiagnostic))
            {
                EditorGUILayout.HelpBox(
                    collisionRenderer.LastDiagnostic,
                    MessageType.Warning);
            }

            EditorGUILayout.HelpBox(
                "Collision occupancy comes only from the authoritative Solid " +
                "Material's Blocks Movement flag. Supports Navigation is inspected " +
                "separately by the navigation classifier.",
                MessageType.Info);
        }

        private static Vector2Int GetInspectedCell(
            WorldTerrainCollisionRenderer collisionRenderer)
        {
            NavigationDebugRenderer overlay =
                FindInScene<NavigationDebugRenderer>(collisionRenderer);
            return overlay != null
                ? overlay.InspectedCell
                : NavigationTestWorldHost.TilemapMutationCell;
        }

        private static TComponent FindInScene<TComponent>(
            WorldTerrainCollisionRenderer collisionRenderer)
            where TComponent : Component
        {
            return Object.FindObjectsByType<TComponent>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(candidate =>
                    candidate.gameObject.scene == collisionRenderer.gameObject.scene);
        }
    }
}
