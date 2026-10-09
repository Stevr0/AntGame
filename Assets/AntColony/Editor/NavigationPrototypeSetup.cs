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
    /// Recreates and validates the isolated Milestone 2 navigation scene.
    /// The scene keeps its batched overlay and authoritative collision projection
    /// separate from the visual Tilemaps and legacy movement prototype.
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
        private const string SkyBackgroundTilePath =
            WorldTileFolder + "/Background_Sky.asset";
        private const string EarthBackgroundTilePath =
            WorldTileFolder + "/Background_UnpreparedEarth.asset";
        private const string StoneBackgroundTilePath =
            WorldTileFolder + "/Background_UnpreparedStone.asset";
        private const string PreparedBackgroundTilePath =
            WorldTileFolder + "/Background_PreparedGrippable.asset";
        private const string SolidTilePath =
            WorldTileFolder + "/Solid_Soil.asset";
        private const string SupportOnlyTilePath =
            WorldTileFolder + "/Solid_SupportOnly.asset";
        private const string BlockingOnlyTilePath =
            WorldTileFolder + "/Solid_BlockingOnly.asset";
        private const string CollisionTilePath =
            WorldTileFolder + "/BlockingTerrainCollision.asset";
        private const string PlaceholderSpritePath =
            "Assets/AntColony/Art/Placeholder/SoilPrototype.asset";
        private const string SkyBackgroundMaterialPath =
            "Assets/AntColony/Data/WorldMaterials/Background_Sky.asset";
        private const string EarthBackgroundMaterialPath =
            "Assets/AntColony/Data/WorldMaterials/Background_UnpreparedEarth.asset";
        private const string StoneBackgroundMaterialPath =
            "Assets/AntColony/Data/WorldMaterials/Background_UnpreparedStone.asset";
        private const string PreparedBackgroundMaterialPath =
            "Assets/AntColony/Data/WorldMaterials/Background_PreparedGrippable.asset";
        private const string SolidMaterialPath =
            "Assets/AntColony/Data/WorldMaterials/Solid_Soil.asset";
        private const string SupportOnlyMaterialPath =
            "Assets/AntColony/Data/WorldMaterials/Solid_SupportOnly.asset";
        private const string BlockingOnlyMaterialPath =
            "Assets/AntColony/Data/WorldMaterials/Solid_BlockingOnly.asset";
        private const string ClimbableSurfaceLayer = "ClimbableSurface";

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
            SolidTileMaterialDefinition solid =
                AssetDatabase.LoadAssetAtPath<SolidTileMaterialDefinition>(
                    SolidMaterialPath);
            Require(solid != null, "Soil Solid material asset is missing.");

            Material overlayMaterial = GetOrCreateOverlayMaterial();
            Sprite placeholderSprite = AssetDatabase
                .LoadAllAssetsAtPath(PlaceholderSpritePath)
                .OfType<Sprite>()
                .FirstOrDefault();
            Require(placeholderSprite != null, "The placeholder soil Sprite is missing.");
            Tile skyBackgroundTile = GetOrCreateTile(
                SkyBackgroundTilePath,
                placeholderSprite,
                new Color(0.12f, 0.18f, 0.28f, 1f));
            Tile earthBackgroundTile = GetOrCreateTile(
                EarthBackgroundTilePath,
                placeholderSprite,
                new Color(0.24f, 0.19f, 0.16f, 1f));
            Tile stoneBackgroundTile = GetOrCreateTile(
                StoneBackgroundTilePath,
                placeholderSprite,
                new Color(0.28f, 0.29f, 0.32f, 1f));
            Tile preparedBackgroundTile = GetOrCreateTile(
                PreparedBackgroundTilePath,
                placeholderSprite,
                new Color(0.17f, 0.42f, 0.30f, 1f));
            Tile solidTile = GetOrCreateTile(
                SolidTilePath,
                placeholderSprite,
                new Color(0.50f, 0.28f, 0.12f, 1f),
                Tile.ColliderType.None);
            Tile supportOnlyTile = GetOrCreateTile(
                SupportOnlyTilePath,
                placeholderSprite,
                new Color(0.12f, 0.48f, 0.62f, 1f),
                Tile.ColliderType.None);
            Tile blockingOnlyTile = GetOrCreateTile(
                BlockingOnlyTilePath,
                placeholderSprite,
                new Color(0.56f, 0.18f, 0.48f, 1f),
                Tile.ColliderType.None);
            Tile collisionTile = GetOrCreateTile(
                CollisionTilePath,
                null,
                Color.clear,
                Tile.ColliderType.Grid);
            AssignVisualTile(solid, solidTile);
            BackgroundTileMaterialDefinition skyBackground =
                GetOrCreateBackgroundMaterial(
                    SkyBackgroundMaterialPath,
                    "background.sky",
                    "Sky",
                    skyBackgroundTile,
                    true,
                    false,
                    false,
                    false,
                    true);
            BackgroundTileMaterialDefinition earthBackground =
                GetOrCreateBackgroundMaterial(
                    EarthBackgroundMaterialPath,
                    "background.earth.unprepared",
                    "Unprepared Earth",
                    earthBackgroundTile,
                    true,
                    false,
                    false,
                    false,
                    true);
            BackgroundTileMaterialDefinition stoneBackground =
                GetOrCreateBackgroundMaterial(
                    StoneBackgroundMaterialPath,
                    "background.stone.unprepared",
                    "Unprepared Stone",
                    stoneBackgroundTile,
                    true,
                    false,
                    false,
                    false,
                    true);
            BackgroundTileMaterialDefinition preparedBackground =
                GetOrCreateBackgroundMaterial(
                    PreparedBackgroundMaterialPath,
                    "background.prepared.grippable",
                    "Prepared Grippable Background",
                    preparedBackgroundTile,
                    true,
                    true,
                    true,
                    true,
                    false);
            SolidTileMaterialDefinition supportOnlyMaterial =
                GetOrCreateSolidMaterial(
                    SupportOnlyMaterialPath,
                    "solid.debug.support-only",
                    "Development Support Only",
                    supportOnlyTile,
                    false,
                    true);
            SolidTileMaterialDefinition blockingOnlyMaterial =
                GetOrCreateSolidMaterial(
                    BlockingOnlyMaterialPath,
                    "solid.debug.blocking-only",
                    "Development Blocking Only",
                    blockingOnlyTile,
                    true,
                    false);

            GameObject sceneRoot = new GameObject("AntNavigationPrototype");
            SceneManager.MoveGameObjectToScene(sceneRoot, scene);

            GameObject hostObject = new GameObject(
                "Navigation Test World Host",
                typeof(NavigationTestWorldHost));
            hostObject.transform.SetParent(sceneRoot.transform, false);
            NavigationTestWorldHost host =
                hostObject.GetComponent<NavigationTestWorldHost>();
            ConfigureHost(
                host,
                earthBackground,
                skyBackground,
                stoneBackground,
                preparedBackground,
                solid,
                supportOnlyMaterial,
                blockingOnlyMaterial);

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
            WorldTerrainCollisionRenderer collisionRenderer =
                CreateCollisionTilemapChild(
                    gridObject.transform,
                    host,
                    collisionTile);
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

            GameObject clearanceObject = new GameObject(
                "Ant Clearance Inspector",
                typeof(AntClearanceDebugRenderer));
            clearanceObject.transform.SetParent(sceneRoot.transform, false);
            clearanceObject.GetComponent<AntClearanceDebugRenderer>().Configure(
                host,
                new Vector2(2f, 1f),
                AntCollisionProfile.OrdinaryAnt,
                0f);

            GameObject capabilityObject = new GameObject(
                "Ant Movement Capability Inspector",
                typeof(AntMovementCapabilityDebugRenderer));
            capabilityObject.transform.SetParent(sceneRoot.transform, false);
            capabilityObject.GetComponent<AntMovementCapabilityDebugRenderer>().Configure(
                host,
                NavigationTestWorldHost.BackgroundMutationCell,
                AntCollisionProfile.OrdinaryAnt,
                0f);

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
            collisionRenderer.RefreshSelectedRegion();
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
                "fixture, two visual Tilemaps, one blocking-only collision Tilemap, " +
                "one batched navigation overlay, and one ordinary-ant clearance " +
                "inspector, and one Background movement-capability inspector.");
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
            BackgroundTileMaterialDefinition skyBackground =
                AssetDatabase.LoadAssetAtPath<BackgroundTileMaterialDefinition>(
                    SkyBackgroundMaterialPath);
            BackgroundTileMaterialDefinition earthBackground =
                AssetDatabase.LoadAssetAtPath<BackgroundTileMaterialDefinition>(
                    EarthBackgroundMaterialPath);
            BackgroundTileMaterialDefinition stoneBackground =
                AssetDatabase.LoadAssetAtPath<BackgroundTileMaterialDefinition>(
                    StoneBackgroundMaterialPath);
            BackgroundTileMaterialDefinition preparedBackground =
                AssetDatabase.LoadAssetAtPath<BackgroundTileMaterialDefinition>(
                    PreparedBackgroundMaterialPath);
            SolidTileMaterialDefinition solid =
                AssetDatabase.LoadAssetAtPath<SolidTileMaterialDefinition>(
                    SolidMaterialPath);
            SolidTileMaterialDefinition supportOnly =
                AssetDatabase.LoadAssetAtPath<SolidTileMaterialDefinition>(
                    SupportOnlyMaterialPath);
            SolidTileMaterialDefinition blockingOnly =
                AssetDatabase.LoadAssetAtPath<SolidTileMaterialDefinition>(
                    BlockingOnlyMaterialPath);
            Tile collisionTile = AssetDatabase.LoadAssetAtPath<Tile>(CollisionTilePath);

            try
            {
                Require(skyBackground != null && earthBackground != null &&
                    stoneBackground != null && preparedBackground != null && solid != null &&
                    supportOnly != null && blockingOnly != null,
                    "Required world material definitions are missing.");
                Require(collisionTile != null,
                    "The blocking terrain collision Tile is missing.");
                NavigationTestWorldHost[] hosts = FindInScene<NavigationTestWorldHost>(scene);
                NavigationDebugRenderer[] renderers =
                    FindInScene<NavigationDebugRenderer>(scene);
                AntClearanceDebugRenderer[] clearanceRenderers =
                    FindInScene<AntClearanceDebugRenderer>(scene);
                AntMovementCapabilityDebugRenderer[] capabilityRenderers =
                    FindInScene<AntMovementCapabilityDebugRenderer>(scene);
                WorldTilemapRenderer[] tilemapRenderers =
                    FindInScene<WorldTilemapRenderer>(scene);
                WorldTerrainCollisionRenderer[] collisionRenderers =
                    FindInScene<WorldTerrainCollisionRenderer>(scene);
                Tilemap[] tilemaps = FindInScene<Tilemap>(scene);
                Require(hosts.Length == 1, "Scene must contain exactly one world host.");
                Require(renderers.Length == 1, "Scene must contain exactly one debug renderer.");
                Require(clearanceRenderers.Length == 1,
                    "Scene must contain exactly one ant clearance inspector.");
                Require(capabilityRenderers.Length == 1,
                    "Scene must contain exactly one movement-capability inspector.");
                Require(tilemapRenderers.Length == 1,
                    "Scene must contain exactly one world Tilemap renderer.");
                Require(collisionRenderers.Length == 1,
                    "Scene must contain exactly one terrain collision renderer.");
                Require(tilemaps.Length == 3,
                    "Scene must contain two visual and one collision Tilemap.");
                Require(FindInScene<Collider>(scene).Length == 0,
                    "Navigation prototype must not contain 3D Collider components.");
                Require(FindInScene<TilemapCollider2D>(scene).Length == 1,
                    "Scene must contain exactly one collision-only TilemapCollider2D.");
                Require(FindInScene<CompositeCollider2D>(scene).Length == 1,
                    "Scene must contain exactly one terrain CompositeCollider2D.");
                Require(FindInScene<Rigidbody2D>(scene).Length == 1,
                    "Scene must contain exactly one static terrain Rigidbody2D.");
                Require(scene.GetRootGameObjects().Length == 1,
                    "Scene should contain one authored AntNavigationPrototype root.");

                NavigationTestWorldHost host = hosts[0];
                NavigationDebugRenderer renderer = renderers[0];
                AntClearanceDebugRenderer clearanceRenderer = clearanceRenderers[0];
                AntMovementCapabilityDebugRenderer capabilityRenderer =
                    capabilityRenderers[0];
                WorldTilemapRenderer tilemapRenderer = tilemapRenderers[0];
                WorldTerrainCollisionRenderer collisionRenderer =
                    collisionRenderers[0];
                Require(host.TryRebuildFixture(), "World fixture could not initialize.");
                Require(tilemapRenderer.RefreshSelectedRegion().Succeeded,
                    "The selected Tilemap region did not refresh successfully.");
                Require(collisionRenderer.RefreshSelectedRegion().Succeeded,
                    "The selected terrain collision region did not refresh successfully.");
                renderer.RefreshOverlay();
                Require(clearanceRenderer.RunQuery(),
                    "The authored ant clearance query could not run.");
                Require(clearanceRenderer.LastResult.Status == AntClearanceStatus.Clear,
                    "The authored ordinary-ant clearance pose must be Clear.");
                Require(clearanceRenderer.BodySize == new Vector2(0.96f, 0.3f) &&
                    clearanceRenderer.BodyOffset == Vector2.zero &&
                    clearanceRenderer.CapsuleAxis == AntCapsuleAxis.Horizontal,
                    "The clearance inspector must match the PlayerAnt capsule profile.");
                Require(clearanceRenderer.GetComponents<Collider2D>().Length == 0 &&
                    clearanceRenderer.GetComponent<Rigidbody2D>() == null,
                    "The clearance inspector must not create gameplay physics components.");
                Require(capabilityRenderer.RunQuery(),
                    "The authored movement-capability query could not run.");
                Require(capabilityRenderer.LastResult.SuggestedState ==
                    AntMovementEligibilityState.Falling &&
                    capabilityRenderer.LastResult.IsPhysicallyClear,
                    "The unprepared chamber-centre query must be physically Clear and Falling.");
                Require(capabilityRenderer.GetComponents<Collider2D>().Length == 0 &&
                    capabilityRenderer.GetComponent<Rigidbody2D>() == null,
                    "The capability inspector must not create gameplay physics components.");
                Require(host.WorldData.ResidentChunkCount == 8,
                    "Fixture must contain exactly eight explicit resident chunks.");
                Require(renderer.DisplayedCellCount ==
                    renderer.RegionSize.x * renderer.RegionSize.y,
                    "Overlay did not classify every selected display cell.");
                Require(tilemapRenderer.BackgroundTilemap != null &&
                    tilemapRenderer.ForegroundTilemap != null,
                    "Both Tilemap references must be assigned.");
                Require(FindInScene<TilemapRenderer>(scene).Length == 3,
                    "Each Tilemap must have exactly one TilemapRenderer.");
                Require(skyBackground.VisualTile != null &&
                    earthBackground.VisualTile != null &&
                    stoneBackground.VisualTile != null &&
                    preparedBackground.VisualTile != null && solid.VisualTile != null &&
                    supportOnly.VisualTile != null && blockingOnly.VisualTile != null,
                    "All fixture world materials must reference authored Tile visuals.");
                ValidateBackgroundMovementMaterials(
                    skyBackground,
                    earthBackground,
                    stoneBackground,
                    preparedBackground);
                Require(!supportOnly.BlocksMovement && supportOnly.SupportsNavigation,
                    "Support-only fixture material flags are incorrect.");
                Require(blockingOnly.BlocksMovement && !blockingOnly.SupportsNavigation,
                    "Blocking-only fixture material flags are incorrect.");
                Require(collisionTile.colliderType == Tile.ColliderType.Grid,
                    "Collision Tile must use a full Grid collider shape.");
                Require(collisionRenderer.BlockingCollisionTile == collisionTile,
                    "Collision renderer must reference the authored collision Tile.");
                ValidateColliderConfiguration(collisionRenderer, tilemapRenderer);

                int renderedBackgroundCount = tilemapRenderer.BackgroundTilemap
                    .GetTilesBlock(new BoundsInt(-64, -32, 0, 128, 64, 1))
                    .Count(tile => tile != null);
                Require(renderedBackgroundCount == 8 * WorldTileCoordinates.CellsPerChunk,
                    "Every cell in the eight resident chunks needs a Background visual.");
                Require(tilemapRenderer.BackgroundTilemap.GetTile(Vector3Int.zero) ==
                    earthBackground.VisualTile,
                    "The origin Background visual does not match its material definition.");
                Require(tilemapRenderer.ForegroundTilemap.GetTile(Vector3Int.zero) ==
                    solid.VisualTile,
                    "The origin Solid visual does not match its material definition.");
                Require(tilemapRenderer.BackgroundTilemap.GetTile(new Vector3Int(10, 32, 0)) ==
                    null,
                    "A missing chunk must not receive an automatic Background visual.");
                ValidateTilemapAlignment(tilemapRenderer.BackgroundTilemap);
                ValidateTilemapAlignment(collisionRenderer.CollisionTilemap);

                Require(HasTerrainCollisionAt(collisionRenderer, Vector2Int.zero),
                    "Blocking soil at the origin must produce collision.");
                Require(!HasTerrainCollisionAt(
                        collisionRenderer,
                        NavigationTestWorldHost.SupportOnlySolidCell),
                    "A non-blocking support-only Solid must not produce collision.");
                Require(HasTerrainCollisionAt(
                        collisionRenderer,
                        NavigationTestWorldHost.BlockingOnlySolidCell),
                    "A blocking non-supporting Solid must produce collision.");
                ValidateHorizontalCollisionBoundaries(collisionRenderer);

                RequireClassification(
                    host,
                    Vector2Int.zero,
                    NavigationCellOccupancy.Covered,
                    NavigationSupportStatus.NotApplicable);

                Require(host.TogglePreparedBackground(
                        NavigationTestWorldHost.BackgroundMutationCell),
                    "Could not replace the test Background with prepared material.");
                Require(tilemapRenderer.RefreshCell(
                        NavigationTestWorldHost.BackgroundMutationCell).Succeeded,
                    "Prepared Background presentation did not refresh.");
                Require(capabilityRenderer.RunQuery() &&
                    capabilityRenderer.LastResult.SuggestedState ==
                        AntMovementEligibilityState.Gripping &&
                    capabilityRenderer.LastResult.HasBackgroundGrip,
                    "Prepared Background must change the test query to Gripping.");
                Require(host.TogglePreparedBackground(
                        NavigationTestWorldHost.BackgroundMutationCell),
                    "Could not restore the unprepared test Background.");
                Require(tilemapRenderer.RefreshCell(
                        NavigationTestWorldHost.BackgroundMutationCell).Succeeded,
                    "Restored Background presentation did not refresh.");
                Require(capabilityRenderer.RunQuery() &&
                    capabilityRenderer.LastResult.SuggestedState ==
                        AntMovementEligibilityState.Falling,
                    "Restoring unprepared Background must restore Falling capability.");
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
                RequireClassification(
                    host,
                    NavigationTestWorldHost.SupportOnlySolidCell,
                    NavigationCellOccupancy.Exposed,
                    NavigationSupportStatus.Unsupported);
                RequireClassification(
                    host,
                    NavigationTestWorldHost.BlockingOnlySolidCell,
                    NavigationCellOccupancy.Covered,
                    NavigationSupportStatus.NotApplicable);

                Require(host.ToggleSolidAt(NavigationTestWorldHost.TilemapMutationCell),
                    "Could not remove the Tilemap mutation Solid Tile.");
                tilemapRenderer.RefreshCell(NavigationTestWorldHost.TilemapMutationCell);
                collisionRenderer.RefreshCell(NavigationTestWorldHost.TilemapMutationCell);
                renderer.RefreshOverlay();
                Require(tilemapRenderer.BackgroundTilemap.GetTile(Vector3Int.zero) ==
                    earthBackground.VisualTile,
                    "Removing a Solid Tile must preserve its Background visual.");
                Require(tilemapRenderer.ForegroundTilemap.GetTile(Vector3Int.zero) == null,
                    "Removing a Solid Tile must clear its foreground visual.");
                Require(!HasTerrainCollisionAt(collisionRenderer, Vector2Int.zero),
                    "Removing a blocking Solid Tile must clear its collision.");
                RequireClassification(
                    host,
                    NavigationTestWorldHost.TilemapMutationCell,
                    NavigationCellOccupancy.Exposed,
                    NavigationSupportStatus.Supported);

                Require(host.ToggleSolidAt(NavigationTestWorldHost.TilemapMutationCell),
                    "Could not restore the Tilemap mutation Solid Tile.");
                tilemapRenderer.RefreshCell(NavigationTestWorldHost.TilemapMutationCell);
                collisionRenderer.RefreshCell(NavigationTestWorldHost.TilemapMutationCell);
                renderer.RefreshOverlay();
                Require(tilemapRenderer.ForegroundTilemap.GetTile(Vector3Int.zero) ==
                    solid.VisualTile,
                    "Restoring a Solid Tile must restore its foreground visual.");
                Require(HasTerrainCollisionAt(collisionRenderer, Vector2Int.zero),
                    "Restoring a blocking Solid Tile must restore its collision.");
                Require(CountTerrainCollidersAt(collisionRenderer, Vector2Int.zero) == 1,
                    "Merged terrain must not report duplicate collider geometry.");

                Debug.Log(
                    "Navigation prototype validation passed: authoritative fixture, " +
                    "aligned visual and collision Tilemaps, merged blocking-only " +
                    "terrain collision, mutation refresh, classification examples, " +
                    "ordinary-ant clearance and movement-capability inspection, " +
                    "Background replacement, and no duplicate collider geometry.");
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
            BackgroundTileMaterialDefinition earthBackground,
            BackgroundTileMaterialDefinition skyBackground,
            BackgroundTileMaterialDefinition stoneBackground,
            BackgroundTileMaterialDefinition preparedBackground,
            SolidTileMaterialDefinition solid,
            SolidTileMaterialDefinition supportOnly,
            SolidTileMaterialDefinition blockingOnly)
        {
            var serializedHost = new SerializedObject(host);
            serializedHost.FindProperty("backgroundMaterial").objectReferenceValue =
                earthBackground;
            serializedHost.FindProperty("skyBackgroundMaterial").objectReferenceValue =
                skyBackground;
            serializedHost.FindProperty("stoneBackgroundMaterial").objectReferenceValue =
                stoneBackground;
            serializedHost.FindProperty("preparedBackgroundMaterial").objectReferenceValue =
                preparedBackground;
            serializedHost.FindProperty("solidMaterial").objectReferenceValue = solid;
            serializedHost.FindProperty("supportOnlyMaterial").objectReferenceValue =
                supportOnly;
            serializedHost.FindProperty("blockingOnlyMaterial").objectReferenceValue =
                blockingOnly;
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

        private static WorldTerrainCollisionRenderer CreateCollisionTilemapChild(
            Transform parent,
            NavigationTestWorldHost host,
            TileBase collisionTile)
        {
            var collisionObject = new GameObject(
                "Foreground Collision Tilemap",
                typeof(Tilemap),
                typeof(TilemapRenderer),
                typeof(Rigidbody2D),
                typeof(TilemapCollider2D),
                typeof(CompositeCollider2D),
                typeof(WorldTerrainCollisionRenderer));
            collisionObject.transform.SetParent(parent, false);
            int climbableLayer = LayerMask.NameToLayer(ClimbableSurfaceLayer);
            Require(climbableLayer >= 0,
                $"Layer '{ClimbableSurfaceLayer}' must already exist.");
            collisionObject.layer = climbableLayer;

            Tilemap tilemap = collisionObject.GetComponent<Tilemap>();
            tilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
            collisionObject.GetComponent<TilemapRenderer>().enabled = false;

            Rigidbody2D body = collisionObject.GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            body.simulated = true;

            TilemapCollider2D tilemapCollider =
                collisionObject.GetComponent<TilemapCollider2D>();
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            CompositeCollider2D compositeCollider =
                collisionObject.GetComponent<CompositeCollider2D>();
            compositeCollider.geometryType = CompositeCollider2D.GeometryType.Polygons;
            compositeCollider.generationType = CompositeCollider2D.GenerationType.Manual;

            WorldTerrainCollisionRenderer renderer =
                collisionObject.GetComponent<WorldTerrainCollisionRenderer>();
            var serializedRenderer = new SerializedObject(renderer);
            serializedRenderer.FindProperty("worldDataProvider").objectReferenceValue = host;
            serializedRenderer.FindProperty("collisionTilemap").objectReferenceValue = tilemap;
            serializedRenderer.FindProperty("blockingCollisionTile").objectReferenceValue =
                collisionTile;
            serializedRenderer.FindProperty("tilemapCollider").objectReferenceValue =
                tilemapCollider;
            serializedRenderer.FindProperty("compositeCollider").objectReferenceValue =
                compositeCollider;
            serializedRenderer.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(renderer);
            return renderer;
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
            Color colour,
            Tile.ColliderType colliderType = Tile.ColliderType.None)
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
            tile.colliderType = colliderType;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static SolidTileMaterialDefinition GetOrCreateSolidMaterial(
            string assetPath,
            string stableId,
            string materialName,
            TileBase visualTile,
            bool blocksMovement,
            bool supportsNavigation)
        {
            SolidTileMaterialDefinition definition =
                AssetDatabase.LoadAssetAtPath<SolidTileMaterialDefinition>(assetPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<SolidTileMaterialDefinition>();
                definition.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                AssetDatabase.CreateAsset(definition, assetPath);
            }

            var serializedDefinition = new SerializedObject(definition);
            serializedDefinition.FindProperty("stableMaterialId").stringValue = stableId;
            serializedDefinition.FindProperty("displayName").stringValue = materialName;
            serializedDefinition.FindProperty("visualTile").objectReferenceValue = visualTile;
            serializedDefinition.FindProperty("blocksMovement").boolValue = blocksMovement;
            serializedDefinition.FindProperty("supportsNavigation").boolValue =
                supportsNavigation;
            serializedDefinition.FindProperty("isExcavatable").boolValue = false;
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static BackgroundTileMaterialDefinition GetOrCreateBackgroundMaterial(
            string assetPath,
            string stableId,
            string materialName,
            TileBase visualTile,
            bool allowsHorizontal,
            bool allowsUpward,
            bool allowsDownward,
            bool providesGrip,
            bool gravityWhenUnsupported)
        {
            BackgroundTileMaterialDefinition definition =
                AssetDatabase.LoadAssetAtPath<BackgroundTileMaterialDefinition>(assetPath);
            if (definition == null)
            {
                definition = ScriptableObject
                    .CreateInstance<BackgroundTileMaterialDefinition>();
                definition.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                AssetDatabase.CreateAsset(definition, assetPath);
            }

            var serializedDefinition = new SerializedObject(definition);
            serializedDefinition.FindProperty("stableMaterialId").stringValue = stableId;
            serializedDefinition.FindProperty("displayName").stringValue = materialName;
            serializedDefinition.FindProperty("visualTile").objectReferenceValue = visualTile;
            serializedDefinition.FindProperty("allowsHorizontalMovement").boolValue =
                allowsHorizontal;
            serializedDefinition.FindProperty("allowsUpwardMovement").boolValue =
                allowsUpward;
            serializedDefinition.FindProperty("allowsDownwardMovement").boolValue =
                allowsDownward;
            serializedDefinition.FindProperty("providesClimbingGrip").boolValue =
                providesGrip;
            serializedDefinition.FindProperty("gravityAppliesWhenUnsupported").boolValue =
                gravityWhenUnsupported;
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void ValidateBackgroundMovementMaterials(
            BackgroundTileMaterialDefinition sky,
            BackgroundTileMaterialDefinition earth,
            BackgroundTileMaterialDefinition stone,
            BackgroundTileMaterialDefinition prepared)
        {
            BackgroundTileMaterialDefinition[] unprepared = { sky, earth, stone };
            foreach (BackgroundTileMaterialDefinition material in unprepared)
            {
                Require(material.AllowsHorizontalMovement &&
                    !material.AllowsUpwardMovement &&
                    !material.AllowsDownwardMovement &&
                    !material.ProvidesClimbingGrip &&
                    material.GravityAppliesWhenUnsupported,
                    $"Background material '{material.name}' has invalid unprepared flags.");
            }

            Require(prepared.AllowsHorizontalMovement &&
                prepared.AllowsUpwardMovement &&
                prepared.AllowsDownwardMovement &&
                prepared.ProvidesClimbingGrip &&
                !prepared.GravityAppliesWhenUnsupported,
                "Prepared Background movement flags are incorrect.");
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

        private static void ValidateColliderConfiguration(
            WorldTerrainCollisionRenderer collisionRenderer,
            WorldTilemapRenderer visualRenderer)
        {
            Require(visualRenderer.BackgroundTilemap.GetComponent<Collider2D>() == null,
                "Background Tilemap must never have terrain collision.");
            Require(visualRenderer.ForegroundTilemap.GetComponent<Collider2D>() == null,
                "Visual foreground Tilemap must not duplicate terrain collision.");
            Require(collisionRenderer.TilemapCollider.compositeOperation ==
                Collider2D.CompositeOperation.Merge,
                "TilemapCollider2D must merge into the CompositeCollider2D.");
            Require(collisionRenderer.CompositeCollider.generationType ==
                CompositeCollider2D.GenerationType.Manual,
                "Composite collision generation must be explicitly synchronized.");
            Require(collisionRenderer.GetComponent<Rigidbody2D>().bodyType ==
                RigidbodyType2D.Static,
                "Terrain collision Rigidbody2D must be static.");
            Require(collisionRenderer.gameObject.layer ==
                LayerMask.NameToLayer(ClimbableSurfaceLayer),
                "Terrain collision must use the existing ClimbableSurface layer.");
            Require(!collisionRenderer.GetComponent<TilemapRenderer>().enabled,
                "The collision-only Tilemap must not render terrain artwork.");
        }

        private static void ValidateHorizontalCollisionBoundaries(
            WorldTerrainCollisionRenderer collisionRenderer)
        {
            int[] requiredXCoordinates = { 31, 32, -1, 0, -32, -33 };
            foreach (int x in requiredXCoordinates)
            {
                Require(
                    HasTerrainCollisionAt(
                        collisionRenderer,
                        new Vector2Int(x, 0)),
                    $"Expected blocking collision at world cell ({x}, 0).");
            }
        }

        private static bool HasTerrainCollisionAt(
            WorldTerrainCollisionRenderer collisionRenderer,
            Vector2Int worldCell)
        {
            return CountTerrainCollidersAt(collisionRenderer, worldCell) > 0;
        }

        private static int CountTerrainCollidersAt(
            WorldTerrainCollisionRenderer collisionRenderer,
            Vector2Int worldCell)
        {
            Vector2 position = WorldTileCoordinates.CellToWorldPosition(worldCell);
            return Physics2D.OverlapPointAll(position)
                .Count(collider =>
                    collider != null &&
                    collider.gameObject == collisionRenderer.gameObject);
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
