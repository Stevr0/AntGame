using System;
using System.Linq;
using AntColony.Development;
using AntColony.World;
using AntColony.WorldPresentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace AntColony.EditorTools
{
    /// <summary>Creates the authored integrated development scene and minimal assets.</summary>
    public static class AntGameDevelopmentSetup
    {
        public const string ScenePath =
            "Assets/AntColony/Scenes/AntGameDevelopment.unity";

        private const string PlaceholderSpritePath =
            "Assets/AntColony/Art/Placeholder/SoilPrototype.asset";
        private const string StoneTilePath =
            "Assets/AntColony/Art/WorldTiles/Solid_Stone.asset";
        private const string StoneMaterialPath =
            "Assets/AntColony/Data/WorldMaterials/Solid_Stone.asset";
        private const string CollisionTilePath =
            "Assets/AntColony/Art/WorldTiles/BlockingTerrainCollision.asset";

        [MenuItem("Ant Colony/Build Integrated Development World")]
        public static void BuildIntegratedDevelopmentWorld()
        {
            CreateAssetsAndScene();
            Debug.Log($"Created integrated development scene at {ScenePath}.");
        }

        /// <summary>Batch-mode entry point used by repository validation.</summary>
        public static void GenerateFromCommandLine()
        {
            try
            {
                CreateAssetsAndScene();
                ValidateScene();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Ant Colony/Validate Integrated Development World")]
        public static void ValidateScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AntGameDevelopmentWorldHost host = FindOne<AntGameDevelopmentWorldHost>(scene);
            WorldTilemapRenderer visualRenderer = FindOne<WorldTilemapRenderer>(scene);
            WorldTerrainCollisionRenderer collisionRenderer =
                FindOne<WorldTerrainCollisionRenderer>(scene);
            DevelopmentWorldInspector inspector = FindOne<DevelopmentWorldInspector>(scene);
            DevelopmentCameraController cameraController =
                FindOne<DevelopmentCameraController>(scene);

            Require(host != null, "Development world host is missing.");
            Require(visualRenderer != null, "World Tilemap renderer is missing.");
            Require(collisionRenderer != null, "Terrain collision renderer is missing.");
            Require(inspector != null, "Development inspector is missing.");
            Require(cameraController != null, "Development camera controller is missing.");
            Require(FindAll<Tilemap>(scene).Length == 3,
                "The scene must contain exactly three Tilemaps.");
            Require(FindAll<TilemapCollider2D>(scene).Length == 1,
                "Only the collision Tilemap may contain a TilemapCollider2D.");
            Require(FindAll<CompositeCollider2D>(scene).Length == 1,
                "The scene must contain one merged terrain collider.");
            Require(scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .All(item =>
                        !item.name.StartsWith("Tile ", StringComparison.Ordinal)),
                "The scene must not use one GameObject per tile.");
            Require(!collisionRenderer.GetComponent<TilemapRenderer>().enabled,
                "Collision-only Tilemap rendering must remain hidden.");
            Require(collisionRenderer.TilemapCollider.compositeOperation ==
                    Collider2D.CompositeOperation.Merge,
                "Terrain Tilemap collision must merge into the composite.");

            Debug.Log("Integrated development world structural validation passed.");
        }

        private static void CreateAssetsAndScene()
        {
            Sprite placeholderSprite = AssetDatabase
                .LoadAllAssetsAtPath(PlaceholderSpritePath)
                .OfType<Sprite>()
                .FirstOrDefault();
            Require(placeholderSprite != null, "Placeholder tile Sprite is missing.");

            Tile stoneTile = GetOrCreateTile(
                StoneTilePath,
                placeholderSprite,
                new Color(0.38f, 0.40f, 0.44f, 1f),
                Tile.ColliderType.None);
            SolidTileMaterialDefinition stoneSolid = GetOrCreateSolidMaterial(
                StoneMaterialPath,
                "solid.stone",
                "Stone",
                stoneTile);
            Tile collisionTile = AssetDatabase.LoadAssetAtPath<Tile>(CollisionTilePath);
            Require(collisionTile != null, "Existing collision Tile asset is missing.");

            BackgroundTileMaterialDefinition sky = LoadBackground("Background_Sky");
            BackgroundTileMaterialDefinition earth =
                LoadBackground("Background_UnpreparedEarth");
            BackgroundTileMaterialDefinition stoneBackground =
                LoadBackground("Background_UnpreparedStone");
            BackgroundTileMaterialDefinition prepared =
                LoadBackground("Background_PreparedGrippable");
            SolidTileMaterialDefinition soil = LoadSolid("Solid_Soil");

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            GameObject sceneRoot = new("AntGameDevelopment");

            GameObject bootstrap = new(
                "World Bootstrap",
                typeof(AntGameDevelopmentWorldHost));
            bootstrap.transform.SetParent(sceneRoot.transform, false);
            AntGameDevelopmentWorldHost host =
                bootstrap.GetComponent<AntGameDevelopmentWorldHost>();

            GameObject gridObject = new(
                "World Grid",
                typeof(Grid),
                typeof(WorldTilemapRenderer));
            gridObject.transform.SetParent(sceneRoot.transform, false);
            gridObject.transform.localPosition = new Vector3(-0.5f, -0.5f, 0f);
            Grid grid = gridObject.GetComponent<Grid>();
            grid.cellSize = Vector3.one;
            grid.cellLayout = GridLayout.CellLayout.Rectangle;
            grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;

            Tilemap backgroundTilemap = CreateVisualTilemap(
                gridObject.transform,
                "Background Tilemap",
                0);
            Tilemap foregroundTilemap = CreateVisualTilemap(
                gridObject.transform,
                "Foreground Solid Tilemap",
                1);
            WorldTilemapRenderer visualRenderer =
                gridObject.GetComponent<WorldTilemapRenderer>();
            visualRenderer.SetTilemapReferences(backgroundTilemap, foregroundTilemap);

            WorldTerrainCollisionRenderer collisionRenderer = CreateCollisionTilemap(
                gridObject.transform,
                collisionTile);

            GameObject cameraObject = new(
                "Main Camera",
                typeof(Camera),
                typeof(AudioListener),
                typeof(DevelopmentCameraController));
            cameraObject.transform.SetParent(sceneRoot.transform, false);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.tag = "MainCamera";
            Camera worldCamera = cameraObject.GetComponent<Camera>();
            worldCamera.orthographic = true;
            worldCamera.orthographicSize = 30f;
            worldCamera.clearFlags = CameraClearFlags.SolidColor;
            worldCamera.backgroundColor = new Color(0.04f, 0.055f, 0.08f, 1f);

            GameObject inspectionObject = new(
                "Development Inspection UI",
                typeof(DevelopmentWorldInspector));
            inspectionObject.transform.SetParent(sceneRoot.transform, false);
            inspectionObject.GetComponent<DevelopmentWorldInspector>()
                .Configure(host, worldCamera);

            host.Configure(
                2203,
                SampleWorldGenerator.DefaultMinimumChunk,
                SampleWorldGenerator.DefaultSizeInChunks,
                sky,
                earth,
                stoneBackground,
                prepared,
                soil,
                stoneSolid,
                visualRenderer,
                collisionRenderer);

            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene, ScenePath),
                "Unity could not save the integrated development scene.");
            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static Tilemap CreateVisualTilemap(
            Transform parent,
            string objectName,
            int sortingOrder)
        {
            GameObject child = new(objectName, typeof(Tilemap), typeof(TilemapRenderer));
            child.transform.SetParent(parent, false);
            Tilemap tilemap = child.GetComponent<Tilemap>();
            tilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
            child.GetComponent<TilemapRenderer>().sortingOrder = sortingOrder;
            return tilemap;
        }

        private static WorldTerrainCollisionRenderer CreateCollisionTilemap(
            Transform parent,
            TileBase collisionTile)
        {
            GameObject child = new(
                "Foreground Collision Tilemap",
                typeof(Tilemap),
                typeof(TilemapRenderer),
                typeof(Rigidbody2D),
                typeof(TilemapCollider2D),
                typeof(CompositeCollider2D),
                typeof(WorldTerrainCollisionRenderer));
            child.transform.SetParent(parent, false);
            int climbableLayer = LayerMask.NameToLayer("ClimbableSurface");
            Require(climbableLayer >= 0, "Existing ClimbableSurface layer is missing.");
            child.layer = climbableLayer;

            Tilemap tilemap = child.GetComponent<Tilemap>();
            tilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
            child.GetComponent<TilemapRenderer>().enabled = false;
            Rigidbody2D body = child.GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            body.simulated = true;
            TilemapCollider2D tilemapCollider = child.GetComponent<TilemapCollider2D>();
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            CompositeCollider2D composite = child.GetComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            composite.generationType = CompositeCollider2D.GenerationType.Manual;

            WorldTerrainCollisionRenderer renderer =
                child.GetComponent<WorldTerrainCollisionRenderer>();
            renderer.SetCollisionReferences(
                tilemap,
                collisionTile,
                tilemapCollider,
                composite);
            renderer.SetCollisionOutlinesVisible(false);
            return renderer;
        }

        private static Tile GetOrCreateTile(
            string path,
            Sprite sprite,
            Color colour,
            Tile.ColliderType colliderType)
        {
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.name = System.IO.Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(tile, path);
            }

            tile.sprite = sprite;
            tile.color = colour;
            tile.transform = Matrix4x4.identity;
            tile.flags = TileFlags.LockTransform;
            tile.colliderType = colliderType;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static SolidTileMaterialDefinition GetOrCreateSolidMaterial(
            string path,
            string stableId,
            string displayName,
            TileBase visualTile)
        {
            SolidTileMaterialDefinition definition =
                AssetDatabase.LoadAssetAtPath<SolidTileMaterialDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<SolidTileMaterialDefinition>();
                definition.name = System.IO.Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(definition, path);
            }

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("stableMaterialId").stringValue = stableId;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("visualTile").objectReferenceValue = visualTile;
            serialized.FindProperty("blocksMovement").boolValue = true;
            serialized.FindProperty("supportsNavigation").boolValue = true;
            serialized.FindProperty("isExcavatable").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static BackgroundTileMaterialDefinition LoadBackground(string name)
        {
            string path = $"Assets/AntColony/Data/WorldMaterials/{name}.asset";
            BackgroundTileMaterialDefinition definition =
                AssetDatabase.LoadAssetAtPath<BackgroundTileMaterialDefinition>(path);
            Require(definition != null, $"Required Background Material is missing: {path}");
            return definition;
        }

        private static SolidTileMaterialDefinition LoadSolid(string name)
        {
            string path = $"Assets/AntColony/Data/WorldMaterials/{name}.asset";
            SolidTileMaterialDefinition definition =
                AssetDatabase.LoadAssetAtPath<SolidTileMaterialDefinition>(path);
            Require(definition != null, $"Required Solid Material is missing: {path}");
            return definition;
        }

        private static void AddSceneToBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes.Any(scene => scene.path == ScenePath))
            {
                return;
            }

            EditorBuildSettings.scenes = scenes
                .Append(new EditorBuildSettingsScene(ScenePath, true))
                .ToArray();
        }

        private static T FindOne<T>(Scene scene) where T : Component
        {
            return FindAll<T>(scene).SingleOrDefault();
        }

        private static T[] FindAll<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();
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
