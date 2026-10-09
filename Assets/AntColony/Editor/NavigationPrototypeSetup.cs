using System;
using System.Linq;
using AntColony.NavigationDebug;
using AntColony.World;
using AntColony.WorldPresentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

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
        private const string WorldTileFolder =
            "Assets/AntColony/Art/WorldTiles";
        private const string BackgroundTilePath =
            WorldTileFolder + "/Background_Default.asset";
        private const string SolidTilePath =
            WorldTileFolder + "/Solid_Soil.asset";
        private const string PlaceholderSpritePath =
            "Assets/AntColony/Art/Placeholder/SoilPrototype.asset";
        private const string BackgroundMaterialPath =
            "Assets/AntColony/Data/WorldMaterials/Background_Default.asset";
        private const string SolidMaterialPath =
            "Assets/AntColony/Data/WorldMaterials/Solid_Soil.asset";

        [MenuItem("Ant Colony/Milestone 2/Create Navigation Classification Prototype")]
        public static void CreateNavigationPrototype()
        {
            EnsureFolder("Assets/AntColony/Art", "NavigationDebug");
            EnsureFolder("Assets/AntColony/Art", "WorldTiles");

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
            Sprite placeholderSprite = AssetDatabase
                .LoadAllAssetsAtPath(PlaceholderSpritePath)
                .OfType<Sprite>()
                .FirstOrDefault();
            Require(placeholderSprite != null, "The placeholder soil Sprite is missing.");
            Tile backgroundTile = GetOrCreateTile(
                BackgroundTilePath,
                placeholderSprite,
                new Color(0.24f, 0.19f, 0.16f, 1f));
            Tile solidTile = GetOrCreateTile(
                SolidTilePath,
                placeholderSprite,
                new Color(0.50f, 0.28f, 0.12f, 1f));
            AssignVisualTile(background, backgroundTile);
            AssignVisualTile(solid, solidTile);

            GameObject sceneRoot = new GameObject("AntNavigationPrototype");
            SceneManager.MoveGameObjectToScene(sceneRoot, scene);

            GameObject hostObject = new GameObject(
                "Navigation Test World Host",
                typeof(NavigationTestWorldHost));
            hostObject.transform.SetParent(sceneRoot.transform, false);
            NavigationTestWorldHost host =
                hostObject.GetComponent<NavigationTestWorldHost>();
            ConfigureHost(host, background, solid);

            GameObject gridObject = new GameObject(
                "World Grid",
                typeof(Grid),
                typeof(WorldTilemapRenderer));
            gridObject.transform.SetParent(sceneRoot.transform, false);
            // Unity's rectangular cells use corner-based Grid positions. Moving
            // the Grid by half a cell keeps Tilemap cell centres on integers.
            gridObject.transform.localPosition = new Vector3(-0.5f, -0.5f, 0f);
            Grid grid = gridObject.GetComponent<Grid>();
            grid.cellSize = Vector3.one;
            grid.cellLayout = GridLayout.CellLayout.Rectangle;
            grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;

            Tilemap backgroundTilemap = CreateTilemapChild(
                gridObject.transform,
                "Background Tilemap",
                0);
            Tilemap foregroundTilemap = CreateTilemapChild(
                gridObject.transform,
                "Foreground Solid Tilemap",
                1);
            WorldTilemapRenderer tilemapRenderer =
                gridObject.GetComponent<WorldTilemapRenderer>();
            ConfigureTilemapRenderer(
                tilemapRenderer,
                host,
                backgroundTilemap,
                foregroundTilemap);

            GameObject overlayObject = new GameObject(
                "Navigation Classification Prototype",
                typeof(MeshFilter),
                typeof(MeshRenderer),
                typeof(NavigationDebugRenderer));
            overlayObject.transform.SetParent(sceneRoot.transform, false);
            ConfigureRenderer(
                overlayObject.GetComponent<NavigationDebugRenderer>(),
                host,
                overlayMaterial);
            overlayObject.GetComponent<MeshRenderer>().sortingOrder = 10;

            GameObject cameraObject = new GameObject(
                "Main Camera",
                typeof(Camera),
                typeof(AudioListener));
            cameraObject.transform.SetParent(sceneRoot.transform, false);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.localPosition = new Vector3(0f, 13.5f, -10f);

            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 24f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.065f, 0.08f);

            host.TryRebuildFixture();
            tilemapRenderer.RefreshSelectedRegion();
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
                "fixture, two data-driven Tilemaps, and one batched navigation overlay.");
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
            BackgroundTileMaterialDefinition background =
                AssetDatabase.LoadAssetAtPath<BackgroundTileMaterialDefinition>(
                    BackgroundMaterialPath);
            SolidTileMaterialDefinition solid =
                AssetDatabase.LoadAssetAtPath<SolidTileMaterialDefinition>(
                    SolidMaterialPath);

            try
            {
                Require(background != null && solid != null,
                    "Default world material definitions are missing.");
                NavigationTestWorldHost[] hosts = FindInScene<NavigationTestWorldHost>(scene);
                NavigationDebugRenderer[] renderers =
                    FindInScene<NavigationDebugRenderer>(scene);
                WorldTilemapRenderer[] tilemapRenderers =
                    FindInScene<WorldTilemapRenderer>(scene);
                Tilemap[] tilemaps = FindInScene<Tilemap>(scene);
                Require(hosts.Length == 1, "Scene must contain exactly one world host.");
                Require(renderers.Length == 1, "Scene must contain exactly one debug renderer.");
                Require(tilemapRenderers.Length == 1,
                    "Scene must contain exactly one world Tilemap renderer.");
                Require(tilemaps.Length == 2,
                    "Scene must contain exactly two presentation Tilemaps.");
                Require(FindInScene<Collider2D>(scene).Length == 0,
                    "Navigation prototype must not contain Collider2D components.");
                Require(FindInScene<Collider>(scene).Length == 0,
                    "Navigation prototype must not contain 3D Collider components.");
                Require(FindInScene<Rigidbody2D>(scene).Length == 0,
                    "Navigation prototype must not contain terrain Rigidbody2D components.");
                Require(scene.GetRootGameObjects().Length == 1,
                    "Scene should contain one authored AntNavigationPrototype root.");

                NavigationTestWorldHost host = hosts[0];
                NavigationDebugRenderer renderer = renderers[0];
                WorldTilemapRenderer tilemapRenderer = tilemapRenderers[0];
                Require(host.TryRebuildFixture(), "World fixture could not initialize.");
                Require(tilemapRenderer.RefreshSelectedRegion().Succeeded,
                    "The selected Tilemap region did not refresh successfully.");
                renderer.RefreshOverlay();
                Require(host.WorldData.ResidentChunkCount == 8,
                    "Fixture must contain exactly eight explicit resident chunks.");
                Require(renderer.DisplayedCellCount ==
                    renderer.RegionSize.x * renderer.RegionSize.y,
                    "Overlay did not classify every selected display cell.");
                Require(tilemapRenderer.BackgroundTilemap != null &&
                    tilemapRenderer.ForegroundTilemap != null,
                    "Both Tilemap references must be assigned.");
                Require(FindInScene<TilemapRenderer>(scene).Length == 2,
                    "Each Tilemap must have exactly one TilemapRenderer.");
                Require(background.VisualTile != null && solid.VisualTile != null,
                    "Default world materials must reference authored Tile visuals.");

                int renderedBackgroundCount = tilemapRenderer.BackgroundTilemap
                    .GetTilesBlock(new BoundsInt(-64, -32, 0, 128, 64, 1))
                    .Count(tile => tile != null);
                Require(renderedBackgroundCount == 8 * WorldTileCoordinates.CellsPerChunk,
                    "Every cell in the eight resident chunks needs a Background visual.");
                Require(tilemapRenderer.BackgroundTilemap.GetTile(Vector3Int.zero) ==
                    background.VisualTile,
                    "The origin Background visual does not match its material definition.");
                Require(tilemapRenderer.ForegroundTilemap.GetTile(Vector3Int.zero) ==
                    solid.VisualTile,
                    "The origin Solid visual does not match its material definition.");
                Require(tilemapRenderer.BackgroundTilemap.GetTile(new Vector3Int(10, 32, 0)) ==
                    null,
                    "A missing chunk must not receive an automatic Background visual.");
                ValidateTilemapAlignment(tilemapRenderer.BackgroundTilemap);

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

                Require(host.ToggleSolidAt(NavigationTestWorldHost.TilemapMutationCell),
                    "Could not remove the Tilemap mutation Solid Tile.");
                tilemapRenderer.RefreshCell(NavigationTestWorldHost.TilemapMutationCell);
                renderer.RefreshOverlay();
                Require(tilemapRenderer.BackgroundTilemap.GetTile(Vector3Int.zero) ==
                    background.VisualTile,
                    "Removing a Solid Tile must preserve its Background visual.");
                Require(tilemapRenderer.ForegroundTilemap.GetTile(Vector3Int.zero) == null,
                    "Removing a Solid Tile must clear its foreground visual.");
                RequireClassification(
                    host,
                    NavigationTestWorldHost.TilemapMutationCell,
                    NavigationCellOccupancy.Exposed,
                    NavigationSupportStatus.Supported);

                Require(host.ToggleSolidAt(NavigationTestWorldHost.TilemapMutationCell),
                    "Could not restore the Tilemap mutation Solid Tile.");
                tilemapRenderer.RefreshCell(NavigationTestWorldHost.TilemapMutationCell);
                renderer.RefreshOverlay();
                Require(tilemapRenderer.ForegroundTilemap.GetTile(Vector3Int.zero) ==
                    solid.VisualTile,
                    "Restoring a Solid Tile must restore its foreground visual.");

                Debug.Log(
                    "Navigation prototype validation passed: authoritative fixture, " +
                    "aligned two-layer Tilemaps, bounded overlay, mutation refresh, " +
                    "classification examples, and no colliders.");
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

        private static Tilemap CreateTilemapChild(
            Transform parent,
            string objectName,
            int sortingOrder)
        {
            var tilemapObject = new GameObject(
                objectName,
                typeof(Tilemap),
                typeof(TilemapRenderer));
            tilemapObject.transform.SetParent(parent, false);
            Tilemap tilemap = tilemapObject.GetComponent<Tilemap>();
            // The Grid is shifted by half a cell, so the standard centre anchor
            // puts each Sprite pivot at the authoritative integer coordinate.
            tilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
            tilemapObject.GetComponent<TilemapRenderer>().sortingOrder = sortingOrder;
            return tilemap;
        }

        private static void ConfigureTilemapRenderer(
            WorldTilemapRenderer renderer,
            NavigationTestWorldHost host,
            Tilemap backgroundTilemap,
            Tilemap foregroundTilemap)
        {
            var serializedRenderer = new SerializedObject(renderer);
            serializedRenderer.FindProperty("worldDataProvider").objectReferenceValue = host;
            serializedRenderer.FindProperty("backgroundTilemap").objectReferenceValue =
                backgroundTilemap;
            serializedRenderer.FindProperty("foregroundTilemap").objectReferenceValue =
                foregroundTilemap;
            serializedRenderer.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(renderer);
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

        private static Tile GetOrCreateTile(
            string assetPath,
            Sprite sprite,
            Color colour)
        {
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                AssetDatabase.CreateAsset(tile, assetPath);
            }

            tile.sprite = sprite;
            tile.color = colour;
            tile.transform = Matrix4x4.identity;
            tile.flags = TileFlags.LockTransform;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static void AssignVisualTile(
            WorldTileMaterialDefinition definition,
            TileBase visualTile)
        {
            var serializedDefinition = new SerializedObject(definition);
            serializedDefinition.FindProperty("visualTile").objectReferenceValue = visualTile;
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }

        private static void ValidateTilemapAlignment(Tilemap tilemap)
        {
            Vector2Int[] requiredCells =
            {
                Vector2Int.zero,
                new Vector2Int(31, 31),
                new Vector2Int(32, 32),
                new Vector2Int(-1, -1),
                new Vector2Int(-32, -32),
                new Vector2Int(-33, -33)
            };

            foreach (Vector2Int worldCell in requiredCells)
            {
                Vector3 actual = tilemap.GetCellCenterWorld(
                    WorldTilemapRenderer.WorldCellToTilemapCell(worldCell));
                Vector2 expected = WorldTileCoordinates.CellToWorldPosition(worldCell);
                Require(
                    Mathf.Abs(actual.x - expected.x) < 0.0001f &&
                    Mathf.Abs(actual.y - expected.y) < 0.0001f,
                    $"Tilemap cell {worldCell} is centred at {actual}; expected {expected}.");
            }
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
