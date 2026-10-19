using System;
using AntColony.WorldPresentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AntColony.EditorTools
{
    /// <summary>Creates and opens the isolated ISO-WV-0 calibration scene.</summary>
    public static class IsometricCalibrationPreviewSceneBuilder
    {
        public const string ScenePath =
            "Assets/AntColony/Scenes/IsometricCalibrationPreview.unity";

        private const string SpriteFolder =
            "Assets/AntColony/Art/WorldView/Isometric/Terrain/";
        private const string FlatGroundPath = SpriteFolder + "tile_101.png";
        private const string RaisedGroundPath = SpriteFolder + "tile_022.png";
        private const string WaterPath = SpriteFolder + "tile_095.png";
        private const string RockPath = SpriteFolder + "tile_060.png";
        private const string VegetationPath = SpriteFolder + "tile_044.png";

        [MenuItem("Ant Colony/Isometric/Open Calibration Preview")]
        public static void OpenPreview()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Ant Colony/Isometric/Rebuild Calibration Preview")]
        public static void RebuildPreview()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            BuildAndSaveScene();
        }

        /// <summary>Batch entry point used by repository validation.</summary>
        public static void BuildBatch()
        {
            BuildAndSaveScene();
        }

        private static void BuildAndSaveScene()
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            scene.name = "IsometricCalibrationPreview";

            var root = new GameObject(
                "ISO-WV-0 Isometric Calibration",
                typeof(SurfaceIsometricProjection),
                typeof(IsometricCalibrationPreview));
            SceneManager.MoveGameObjectToScene(root, scene);

            SurfaceIsometricProjection projection =
                root.GetComponent<SurfaceIsometricProjection>();
            projection.Configure(
                Vector2.zero,
                SurfaceIsometricProjection.DefaultTileWidthPixels,
                SurfaceIsometricProjection.DefaultTileHeightPixels,
                SurfaceIsometricProjection.DefaultPixelsPerUnit);

            IsometricCalibrationPreview preview =
                root.GetComponent<IsometricCalibrationPreview>();
            preview.Configure(
                projection,
                LoadSprite(FlatGroundPath),
                LoadSprite(RaisedGroundPath),
                LoadSprite(WaterPath),
                LoadSprite(RockPath),
                LoadSprite(VegetationPath));
            preview.RebuildPreview();

            CreateCamera(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene, ScenePath),
                $"Unity could not save {ScenePath}.");
            AssetDatabase.SaveAssets();
            Debug.Log($"ISO-WV-0 calibration preview saved to {ScenePath}.");
        }

        private static void CreateCamera(Scene scene)
        {
            var cameraObject = new GameObject("Calibration Camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, -4.5f, -10f);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6.25f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(25, 31, 43, 255);
            camera.allowHDR = false;
            camera.allowMSAA = false;
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            Require(sprite != null, $"Required calibration Sprite is missing: {path}");
            return sprite;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
