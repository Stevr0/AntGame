using AntColony.NavigationDebug;
using UnityEditor;
using UnityEngine;

namespace AntColony.EditorTools
{
    /// <summary>Interactive Scene-view controls for the clearance test capsule.</summary>
    [CustomEditor(typeof(AntClearanceDebugRenderer))]
    public sealed class AntClearanceDebugRendererEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var debugRenderer = (AntClearanceDebugRenderer)target;

            EditorGUILayout.Space();
            if (GUILayout.Button("Run Authoritative Clearance Query"))
            {
                debugRenderer.RunQuery();
                SceneView.RepaintAll();
            }

            EditorGUILayout.LabelField("Clearance Result", EditorStyles.boldLabel);
            if (!debugRenderer.HasResult)
            {
                EditorGUILayout.LabelField("Status", "Not queried");
                return;
            }

            EditorGUILayout.LabelField("Status", debugRenderer.LastResult.Status.ToString());
            EditorGUILayout.LabelField(
                "Centre Cell",
                debugRenderer.LastResult.CentreCell.ToString());
            EditorGUILayout.LabelField(
                "Problem Cell",
                debugRenderer.LastResult.ProblemCell.ToString());
            EditorGUILayout.LabelField(
                "Touched Cells",
                debugRenderer.LastResult.TouchedCellCount.ToString());
            EditorGUILayout.HelpBox(
                "Green is Clear, red is Blocked, blue is Unsupported, amber is " +
                "Unavailable/Unknown, and grey is Invalid. This is a static " +
                "authoritative query, not reachability or swept-path validation.",
                MessageType.Info);
        }

        private void OnSceneGUI()
        {
            var debugRenderer = (AntClearanceDebugRenderer)target;
            EditorGUI.BeginChangeCheck();
            Vector3 selectedPosition = Handles.PositionHandle(
                debugRenderer.TestWorldPosition,
                Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(debugRenderer, "Move Clearance Test Position");
                debugRenderer.SetTestWorldPosition((Vector2)selectedPosition);
                EditorUtility.SetDirty(debugRenderer);
            }

            string resultText = debugRenderer.HasResult
                ? debugRenderer.LastResult.Status.ToString()
                : "Not queried";
            Handles.Label(
                (Vector3)debugRenderer.TestWorldPosition + Vector3.up * 0.75f,
                $"Clearance: {resultText}");
        }
    }
}
