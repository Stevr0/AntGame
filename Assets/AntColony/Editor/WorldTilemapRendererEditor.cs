using System.Linq;
using AntColony.NavigationDebug;
using AntColony.WorldPresentation;
using UnityEditor;
using UnityEngine;

namespace AntColony.EditorTools
{
    /// <summary>
    /// Coordinates the development-only mutation demonstration. Runtime Tilemap
    /// and navigation components remain independent; only this Editor tool asks
    /// both projections to refresh after an authoritative mutation.
    /// </summary>
    [CustomEditor(typeof(WorldTilemapRenderer))]
    public sealed class WorldTilemapRendererEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var tilemapRenderer = (WorldTilemapRenderer)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Developer Controls", EditorStyles.boldLabel);

            if (GUILayout.Button("Rebuild World and Refresh Selected Region"))
            {
                GetNavigationHost(tilemapRenderer)?.TryRebuildFixture();
                tilemapRenderer.RefreshSelectedRegion();
                RefreshNavigationOverlay(tilemapRenderer);
            }

            if (GUILayout.Button("Toggle Test Solid and Refresh Cell"))
            {
                NavigationTestWorldHost host = GetNavigationHost(tilemapRenderer);
                if (host != null && host.ToggleSolidAt(
                    NavigationTestWorldHost.TilemapMutationCell))
                {
                    tilemapRenderer.RefreshCell(
                        NavigationTestWorldHost.TilemapMutationCell);
                    RefreshNavigationOverlay(tilemapRenderer);
                }
            }

            if (GUILayout.Button("Refresh Selected Region"))
            {
                tilemapRenderer.RefreshSelectedRegion();
                SceneView.RepaintAll();
            }

            if (!string.IsNullOrEmpty(tilemapRenderer.LastDiagnostic))
            {
                EditorGUILayout.HelpBox(
                    tilemapRenderer.LastDiagnostic,
                    MessageType.Warning);
            }

            EditorGUILayout.HelpBox(
                $"Mutation test cell: {NavigationTestWorldHost.TilemapMutationCell}. " +
                "The first toggle removes its Solid Tile while leaving the " +
                "Background Tile intact; the next toggle restores it.",
                MessageType.Info);
        }

        private static void RefreshNavigationOverlay(
            WorldTilemapRenderer tilemapRenderer)
        {
            NavigationDebugRenderer overlay = Object
                .FindObjectsByType<NavigationDebugRenderer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(candidate =>
                    candidate.gameObject.scene == tilemapRenderer.gameObject.scene);
            overlay?.RefreshOverlay();
            SceneView.RepaintAll();
        }

        private static NavigationTestWorldHost GetNavigationHost(
            WorldTilemapRenderer tilemapRenderer)
        {
            return tilemapRenderer.WorldDataProvider as NavigationTestWorldHost;
        }
    }
}
