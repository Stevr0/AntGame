using System;
using System.Linq;
using AntColony.NavigationDebug;
using AntColony.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AntColony.EditorTools
{
    /// <summary>
    /// Recreates and validates the isolated Milestone 2.1D navigation scene.
    /// The scene contains one batched overlay object and no gameplay colliders.
    /// </summary>
    public static class NavigationPrototypeSetup
    {
        private const string ScenePath =
            "Assets/AntColony/Scenes/AntNavigationPrototype.unity";
        private const string MaterialFolder =
            "Assets/AntColony/Art/NavigationDebug";
        private const string OverlayMaterialPath =
            MaterialFolder + "/NavigationOverlay.mat";
        private const string BackgroundMaterialPath =
            "Assets/AntColony/Data/WorldMaterials/Background_Default.asset";
        private const string SolidMaterialPath =
            "Assets/AntColony/Data/WorldMaterials/Solid_Soil.asset";

        [MenuItem("Ant Colony/Milestone 2/Create Navigation Classification Prototype")]
        public static void CreateNavigationPrototype()
        {
            EnsureFolder("Assets/AntColony/Art", "NavigationDebug");

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                Application.isBatchMode
                    ? NewSceneMode.Single
                    : NewSceneMode.Additive);
            scene.name = "AntNavigationPrototype";

            // Load authored assets after replacing the untitled batch scene.
            // Loading first would allow NewSceneMode.Single to unload them before
            // their references are serialized onto the new scene components.
            BackgroundTileMaterialDefinition background =
                AssetDatabase.LoadAssetAtPath<BackgroundTileMaterialDefinition>(
                    BackgroundMaterialPath);
            SolidTileMaterialDefinition solid =
                AssetDatabase.LoadAssetAtPath<SolidTileMaterialDefinition>(
                    SolidMaterialPath);
            Require(background != null, "Default Background material asset is missing.");
            Require(solid != null, "Soil Solid material asset is missing.");

            Material overlayMaterial = GetOrCreateOverlayMaterial();

            GameObject overlayObject = new GameObject(
                "Navigation Classification Prototype",
                typeof(MeshFilter),
                typeof(MeshRenderer),
                typeof(NavigationTestWorldHost),
                typeof(NavigationDebugRenderer));
            SceneManager.MoveGameObjectToScene(overlayObject, scene);

            ConfigureHost(
                overlayObject.GetComponent<NavigationTestWorldHost>(),
                background,
                solid);
            ConfigureRenderer(
                overlayObject.GetComponent<NavigationDebugRenderer>(),
                overlayObject.GetComponent<NavigationTestWorldHost>(),
                overlayMaterial);

            GameObject cameraObject = new GameObject(
                "Main Camera",
                typeof(Camera),
                typeof(AudioListener));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 13.5f, -10f);

            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 24f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.065f, 0.08f);

            overlayObject.GetComponent<NavigationTestWorldHost>().TryRebuildFixture();
            overlayObject.GetComponent<NavigationDebugRenderer>().RefreshOverlay();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            if (!Application.isBatchMode)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);

            Debug.Log(
                "Created AntNavigationPrototype with an authoritative eight-chunk " +
                "fixture and one batched navigation-classification mesh.");
        }

        /// <summary>Command-line entry point used for repeatable scene authoring.</summary>
        public static void CreateNavigationPrototypeBatch()
        {
            CreateNavigationPrototype();
        }

        [MenuItem("Ant Colony/Milestone 2/Validate Navigation Classification Prototype")]
        public static void ValidateNavigationPrototype()
        {
            Scene existingScene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForValidation = !existingScene.IsValid() || !existingScene.isLoaded;
            Scene scene = openedForValidation
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                : existingScene;

            try
            {
                NavigationTestWorldHost[] hosts = FindInScene<NavigationTestWorldHost>(scene);
                NavigationDebugRenderer[] renderers =
                    FindInScene<NavigationDebugRenderer>(scene);
                Require(hosts.Length == 1, "Scene must contain exactly one world host.");
                Require(renderers.Length == 1, "Scene must contain exactly one debug renderer.");
                Require(FindInScene<Collider2D>(scene).Length == 0,
                    "Navigation prototype must not contain Collider2D components.");
                Require(FindInScene<Collider>(scene).Length == 0,
                    "Navigation prototype must not contain 3D Collider components.");
                Require(scene.GetRootGameObjects().Length == 2,
                    "Scene should contain only the overlay root and camera.");

                NavigationTestWorldHost host = hosts[0];
                NavigationDebugRenderer renderer = renderers[0];
                Require(host.TryRebuildFixture(), "World fixture could not initialize.");
                renderer.RefreshOverlay();
                Require(host.WorldData.ResidentChunkCount == 8,
                    "Fixture must contain exactly eight explicit resident chunks.");
                Require(renderer.DisplayedCellCount ==
                    renderer.RegionSize.x * renderer.RegionSize.y,
                    "Overlay did not classify every selected display cell.");

                RequireClassification(
                    host,
                    Vector2Int.zero,
                    NavigationCellOccupancy.Covered,
                    NavigationSupportStatus.NotApplicable);
                RequireClassification(
                    host,
                    NavigationTestWorldHost.MutationCell,
                    NavigationCellOccupancy.Exposed,
                    NavigationSupportStatus.Supported);
                RequireClassification(
                    host,
                    new Vector2Int(20, 8),
                    NavigationCellOccupancy.Exposed,
                    NavigationSupportStatus.Unsupported);
                RequireClassification(
                    host,
                    NavigationTestWorldHost.DiagonalOnlyCell,
                    NavigationCellOccupancy.Exposed,
                    NavigationSupportStatus.Unsupported);
                RequireClassification(
                    host,
                    NavigationTestWorldHost.UnknownSupportCell,
                    NavigationCellOccupancy.Exposed,
                    NavigationSupportStatus.Unknown);

                Debug.Log(
                    "Navigation prototype validation passed: authoritative fixture, " +
                    "bounded overlay, classification examples, and no colliders.");
            }
            finally
            {
                if (openedForValidation)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void ConfigureHost(
            NavigationTestWorldHost host,
            BackgroundTileMaterialDefinition background,
            SolidTileMaterialDefinition solid)
        {
            var serializedHost = new SerializedObject(host);
            serializedHost.FindProperty("backgroundMaterial").objectReferenceValue = background;
            serializedHost.FindProperty("solidMaterial").objectReferenceValue = solid;
            serializedHost.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(host);
        }

        private static void ConfigureRenderer(
            NavigationDebugRenderer renderer,
            NavigationTestWorldHost host,
            Material material)
        {
            var serializedRenderer = new SerializedObject(renderer);
            serializedRenderer.FindProperty("worldHost").objectReferenceValue = host;
            serializedRenderer.FindProperty("overlayMaterial").objectReferenceValue = material;
            serializedRenderer.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(renderer);
        }

        private static Material GetOrCreateOverlayMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(OverlayMaterialPath);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Sprites/Default");
            Require(shader != null, "Unity's Sprites/Default shader was not found.");
            material = new Material(shader)
            {
                name = "Navigation Classification Overlay"
            };
            material.color = Color.white;
            AssetDatabase.CreateAsset(material, OverlayMaterialPath);
            return material;
        }

        private static void RequireClassification(
            NavigationTestWorldHost host,
            Vector2Int cell,
            NavigationCellOccupancy occupancy,
            NavigationSupportStatus support)
        {
            WorldNavigationClassification result = host.Classifier.Classify(cell);
            Require(
                result.Occupancy == occupancy && result.Support == support,
                $"Cell {cell} was {result.Occupancy}/{result.Support}; " +
                $"expected {occupancy}/{support}.");
        }

        private static TComponent[] FindInScene<TComponent>(Scene scene)
            where TComponent : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<TComponent>(true))
                .ToArray();
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
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
