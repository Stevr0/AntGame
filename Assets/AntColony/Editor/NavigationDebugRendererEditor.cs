using AntColony.NavigationDebug;
using AntColony.World;
using UnityEditor;
using UnityEngine;

namespace AntColony.EditorTools
{
    /// <summary>
    /// Provides cell inspection and explicit refresh/mutation controls without
    /// adding a gameplay UI to the prototype scene.
    /// </summary>
    [CustomEditor(typeof(NavigationDebugRenderer))]
    public sealed class NavigationDebugRendererEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var renderer = (NavigationDebugRenderer)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Developer Controls", EditorStyles.boldLabel);

            if (GUILayout.Button("Rebuild Test World and Overlay"))
            {
                renderer.WorldHost?.TryRebuildFixture();
                renderer.RefreshOverlay();
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Toggle Test Solid and Refresh"))
            {
                renderer.WorldHost?.ToggleMutationSolid();
                renderer.RefreshOverlay();
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Refresh Overlay"))
            {
                renderer.RefreshOverlay();
                SceneView.RepaintAll();
            }

            DrawInspectedCell(renderer);
        }

        private void OnSceneGUI()
        {
            var renderer = (NavigationDebugRenderer)target;
            Event current = Event.current;
            if (current.type == EventType.MouseDown &&
                current.button == 0 &&
                !current.alt)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);
                Plane overlayPlane = new Plane(Vector3.forward, renderer.transform.position);
                if (overlayPlane.Raycast(ray, out float distance))
                {
                    Undo.RecordObject(renderer, "Inspect Navigation Cell");
                    renderer.SetInspectedCell(
                        renderer.WorldPositionToCell(ray.GetPoint(distance)));
                    EditorUtility.SetDirty(renderer);
                    Repaint();
                    current.Use();
                }
            }

            Vector2 centre = WorldTileCoordinates.CellToWorldPosition(renderer.InspectedCell);
            Vector3 worldCentre = renderer.transform.TransformPoint(centre);
            Handles.color = Color.white;
            Handles.DrawWireCube(worldCentre, Vector3.one * 0.94f);
            Handles.Label(worldCentre + Vector3.up * 0.6f, renderer.InspectedCell.ToString());
        }

        private static void DrawInspectedCell(NavigationDebugRenderer renderer)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Selected Cell", EditorStyles.boldLabel);
            Vector2Int cell = renderer.InspectedCell;
            Vector2Int chunk = WorldTileCoordinates.GetChunkCoordinate(cell);
            Vector2Int local = WorldTileCoordinates.GetLocalCellCoordinate(cell);

            EditorGUILayout.LabelField("World Cell", cell.ToString());
            EditorGUILayout.LabelField("Chunk", chunk.ToString());
            EditorGUILayout.LabelField("Local Cell", local.ToString());

            NavigationTestWorldHost host = renderer.WorldHost;
            bool resident = host != null &&
                host.IsInitialized &&
                host.WorldData.ContainsChunk(chunk);
            EditorGUILayout.LabelField("Resident", resident ? "Yes" : "No");

            if (host == null || !host.IsInitialized)
            {
                EditorGUILayout.HelpBox("World fixture is not initialized.", MessageType.Warning);
                return;
            }

            WorldNavigationClassification result = host.Classifier.Classify(cell);
            EditorGUILayout.LabelField("Occupancy", result.Occupancy.ToString());
            EditorGUILayout.LabelField("Support", result.Support.ToString());
            EditorGUILayout.HelpBox(
                "Supported is terrain eligibility only. It does not prove ant-body " +
                "clearance, connectivity, or reachability.",
                MessageType.Info);
        }
    }
}
