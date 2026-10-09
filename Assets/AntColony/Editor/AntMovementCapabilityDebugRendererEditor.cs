using System.Linq;
using AntColony.NavigationDebug;
using AntColony.WorldPresentation;
using UnityEditor;
using UnityEngine;

namespace AntColony.EditorTools
{
    /// <summary>Interactive development controls for movement capability.</summary>
    [CustomEditor(typeof(AntMovementCapabilityDebugRenderer))]
    public sealed class AntMovementCapabilityDebugRendererEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var debugRenderer = (AntMovementCapabilityDebugRenderer)target;

            EditorGUILayout.Space();
            if (GUILayout.Button("Run Movement Capability Query"))
            {
                debugRenderer.RunQuery();
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Toggle Prepared Background, Refresh, and Requery"))
            {
                Undo.RecordObject(debugRenderer, "Toggle Test Background Material");
                if (debugRenderer.TogglePreparedBackground())
                {
                    RefreshBackgroundPresentation(debugRenderer);
                }

                SceneView.RepaintAll();
            }

            EditorGUILayout.LabelField("Movement Capability", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "World Position",
                debugRenderer.TestWorldPosition.ToString());
            EditorGUILayout.LabelField(
                "World Cell",
                debugRenderer.TestWorldCell.ToString());
            if (!debugRenderer.HasResult)
            {
                EditorGUILayout.LabelField("Status", "Not queried");
                return;
            }

            var result = debugRenderer.LastResult;
            EditorGUILayout.LabelField("Status", result.Status.ToString());
            EditorGUILayout.LabelField(
                "Background Material ID",
                result.BackgroundMaterialId ?? "Unavailable");
            EditorGUILayout.LabelField(
                "Physical Clearance",
                result.Clearance.Status.ToString());
            EditorGUILayout.LabelField("Ground Support", YesNo(result.HasGroundSupport));
            EditorGUILayout.LabelField("Background Grip", YesNo(result.HasBackgroundGrip));
            EditorGUILayout.LabelField(
                "Voluntary Horizontal",
                YesNo(result.AllowsHorizontalMovement));
            EditorGUILayout.LabelField(
                "Voluntary Upward",
                YesNo(result.AllowsUpwardMovement));
            EditorGUILayout.LabelField(
                "Voluntary Downward",
                YesNo(result.AllowsDownwardMovement));
            EditorGUILayout.LabelField(
                "Gravity Eligible",
                YesNo(result.ShouldApplyGravity));
            EditorGUILayout.LabelField("Suggested State", result.SuggestedState.ToString());
            EditorGUILayout.HelpBox(
                "This is a read-only local capability query. Falling is gravity-" +
                "driven and is not voluntary downward movement. Directional " +
                "connections and runtime movement are not evaluated here.",
                MessageType.Info);
        }

        private void OnSceneGUI()
        {
            var debugRenderer = (AntMovementCapabilityDebugRenderer)target;
            EditorGUI.BeginChangeCheck();
            Vector3 selectedPosition = Handles.PositionHandle(
                debugRenderer.TestWorldPosition,
                Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(debugRenderer, "Move Capability Test Position");
                debugRenderer.SetTestWorldPosition((Vector2)selectedPosition);
                EditorUtility.SetDirty(debugRenderer);
            }

            string state = debugRenderer.HasResult
                ? debugRenderer.LastResult.SuggestedState.ToString()
                : "Not queried";
            Handles.Label(
                (Vector3)debugRenderer.TestWorldPosition + Vector3.up * 0.5f,
                $"Capability: {state}");
        }

        private static void RefreshBackgroundPresentation(
            AntMovementCapabilityDebugRenderer debugRenderer)
        {
            WorldTilemapRenderer renderer = Object
                .FindObjectsByType<WorldTilemapRenderer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(candidate =>
                    candidate.gameObject.scene == debugRenderer.gameObject.scene);
            renderer?.RefreshCell(debugRenderer.TestWorldCell);
        }

        private static string YesNo(bool value)
        {
            return value ? "Yes" : "No";
        }
    }
}
