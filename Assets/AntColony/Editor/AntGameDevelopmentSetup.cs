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

        private const string WhitePixelSpritePath =
            "Assets/AntColony/Art/WorldTiles/DevelopmentWhitePixel.asset";
        private const string SkyTilePath =
            "Assets/AntColony/Art/WorldTiles/Background_Sky.asset";
        private const string EarthTilePath =
            "Assets/AntColony/Art/WorldTiles/Background_UnpreparedEarth.asset";
        private const string StoneBackgroundTilePath =
            "Assets/AntColony/Art/WorldTiles/Background_UnpreparedStone.asset";
        private const string PreparedTilePath =
            "Assets/AntColony/Art/WorldTiles/Background_PreparedGrippable.asset";
        private const string SoilTilePath =
            "Assets/AntColony/Art/WorldTiles/Solid_Soil.asset";
        private const string StoneTilePath =
            "Assets/AntColony/Art/WorldTiles/Solid_Stone.asset";
        private const string StoneMaterialPath =
            "Assets/AntColony/Data/WorldMaterials/Solid_Stone.asset";
        private const string CollisionTilePath =
            "Assets/AntColony/Art/WorldTiles/BlockingTerrainCollision.asset";
        private const string AntSpritePath =
            "Assets/AntColony/Art/Placeholder/PlayerAntPrototype.asset";
        private const string AutonomousAntPrefabPath =
            "Assets/AntColony/Prefabs/Ants/AutonomousAnt.prefab";

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
            Require(host.AutonomousAntPrefab != null,
                "Autonomous Ant prefab reference is missing.");
            ValidateAutonomousAntPrefab(host.AutonomousAntPrefab);
            Require(FindAll<AntAutonomousMotor>(scene).Length == 0,
                "The autonomous ant must be spawned after world validation, not authored twice.");
            Require(scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Count(item => item.name == "Autonomous Destination Marker") == 1,
                "The scene must contain one autonomous destination marker.");
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
            ValidateDevelopmentPalette();

            Debug.Log("Integrated development world structural validation passed.");
        }

        private static void CreateAssetsAndScene()
        {
            Sprite whitePixelSprite = GetOrCreateWhitePixelSprite();
            Sprite antSprite = AssetDatabase.LoadAllAssetsAtPath(AntSpritePath)
                .OfType<Sprite>()
                .FirstOrDefault();
            Require(antSprite != null, "Existing development ant Sprite is missing.");
            AntAutonomousMotor autonomousAntPrefab =
                GetOrCreateAutonomousAntPrefab(antSprite);
            GetOrCreateTile(
                SkyTilePath,
                whitePixelSprite,
                new Color32(113, 155, 193, 255),
                Tile.ColliderType.None);
            GetOrCreateTile(
                EarthTilePath,
                whitePixelSprite,
                new Color32(81, 68, 57, 255),
                Tile.ColliderType.None);
            GetOrCreateTile(
                StoneBackgroundTilePath,
                whitePixelSprite,
                new Color32(67, 76, 86, 255),
                Tile.ColliderType.None);
            GetOrCreateTile(
                PreparedTilePath,
                whitePixelSprite,
                new Color32(100, 139, 98, 255),
                Tile.ColliderType.None);
            GetOrCreateTile(
                SoilTilePath,
                whitePixelSprite,
                new Color32(152, 99, 66, 255),
                Tile.ColliderType.None);
            Tile stoneTile = GetOrCreateTile(
                StoneTilePath,
                whitePixelSprite,
                new Color32(115, 126, 137, 255),
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
            worldCamera.backgroundColor = new Color32(36, 52, 66, 255);

            GameObject inspectionObject = new(
                "Development Inspection UI",
                typeof(DevelopmentWorldInspector));
            inspectionObject.transform.SetParent(sceneRoot.transform, false);
            inspectionObject.GetComponent<DevelopmentWorldInspector>()
                .Configure(host, worldCamera);

            GameObject markerObject = new(
                "Autonomous Destination Marker",
                typeof(SpriteRenderer));
            markerObject.transform.SetParent(sceneRoot.transform, false);
            markerObject.transform.localScale = new Vector3(0.24f, 0.24f, 1f);
            SpriteRenderer markerRenderer = markerObject.GetComponent<SpriteRenderer>();
            markerRenderer.sprite = whitePixelSprite;
            markerRenderer.color = new Color32(255, 224, 92, 255);
            markerRenderer.sortingOrder = 4;
            markerObject.SetActive(false);

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
                collisionRenderer,
                autonomousAntPrefab,
                markerObject.transform,
                markerRenderer);

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

        private static AntAutonomousMotor GetOrCreateAutonomousAntPrefab(
            Sprite antSprite)
        {
            GameObject root = new(
                "AutonomousAnt",
                typeof(Rigidbody2D),
                typeof(CapsuleCollider2D),
                typeof(AntAutonomousMotor));
            GameObject visual = new("Visual", typeof(SpriteRenderer));
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = new Vector3(0.96f, 0.3f, 1f);

            SpriteRenderer spriteRenderer = visual.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = antSprite;
            spriteRenderer.color = new Color32(247, 214, 82, 255);
            spriteRenderer.sortingOrder = 3;

            Rigidbody2D body = root.GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 4f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;

            CapsuleCollider2D collider = root.GetComponent<CapsuleCollider2D>();
            collider.direction = CapsuleDirection2D.Horizontal;
            collider.size = AntCollisionProfile.OrdinaryAnt.Size;

            root.GetComponent<AntAutonomousMotor>().Configure(
                body,
                spriteRenderer,
                2f,
                0.08f);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                root,
                AutonomousAntPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            Require(saved != null, "Unity could not save the Autonomous Ant prefab.");
            return saved.GetComponent<AntAutonomousMotor>();
        }

        private static void ValidateAutonomousAntPrefab(AntAutonomousMotor prefab)
        {
            Rigidbody2D body = prefab.GetComponent<Rigidbody2D>();
            CapsuleCollider2D collider = prefab.GetComponent<CapsuleCollider2D>();
            Require(body != null && collider != null,
                "Autonomous Ant requires one Rigidbody2D and CapsuleCollider2D.");
            Require(body.bodyType == RigidbodyType2D.Dynamic &&
                    body.constraints == RigidbodyConstraints2D.FreezeRotation,
                "Autonomous Ant Rigidbody settings are incorrect.");
            Require(collider.direction == CapsuleDirection2D.Horizontal &&
                    collider.size == AntCollisionProfile.OrdinaryAnt.Size,
                "Autonomous Ant collider must match the ordinary-ant profile.");
            Require(prefab.GetComponents<MonoBehaviour>()
                    .All(component => component == null ||
                        component.GetType().Name != "PlayerMovementInput" &&
                        component.GetType().Name != "AntSurfaceMotor"),
                "Autonomous Ant must not contain legacy player movement components.");
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

        private static Sprite GetOrCreateWhitePixelSprite()
        {
            Sprite existingSprite = AssetDatabase
                .LoadAllAssetsAtPath(WhitePixelSpritePath)
                .OfType<Sprite>()
                .FirstOrDefault();
            if (existingSprite != null)
            {
                return existingSprite;
            }

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "Development White Pixel Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply(false, false);
            AssetDatabase.CreateAsset(texture, WhitePixelSpritePath);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f,
                0,
                SpriteMeshType.FullRect);
            sprite.name = "Development White Pixel Sprite";
            AssetDatabase.AddObjectToAsset(sprite, texture);
            EditorUtility.SetDirty(texture);
            EditorUtility.SetDirty(sprite);
            AssetDatabase.SaveAssetIfDirty(texture);
            return sprite;
        }

        private static void ValidateDevelopmentPalette()
        {
            ValidateTileColour(SkyTilePath, new Color32(113, 155, 193, 255));
            ValidateTileColour(EarthTilePath, new Color32(81, 68, 57, 255));
            ValidateTileColour(
                StoneBackgroundTilePath,
                new Color32(67, 76, 86, 255));
            ValidateTileColour(PreparedTilePath, new Color32(100, 139, 98, 255));
            ValidateTileColour(SoilTilePath, new Color32(152, 99, 66, 255));
            ValidateTileColour(StoneTilePath, new Color32(115, 126, 137, 255));
        }

        private static void ValidateTileColour(string path, Color32 expectedColour)
        {
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            Require(tile != null, $"Development Tile is missing: {path}");
            Require(ColoursMatch((Color32)tile.color, expectedColour),
                $"Development Tile has an unexpected colour: {path}");
            Require(tile.sprite != null && tile.sprite.texture != null,
                $"Development Tile has no source Sprite: {path}");
            Require(ColoursMatch(
                    (Color32)tile.sprite.texture.GetPixel(0, 0),
                    new Color32(255, 255, 255, 255)),
                $"Development Tile source Sprite must be neutral white: {path}");
        }

        private static bool ColoursMatch(Color32 first, Color32 second)
        {
            return first.r == second.r && first.g == second.g &&
                first.b == second.b && first.a == second.a;
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
