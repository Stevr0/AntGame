using System;
using System.Linq;
using AntColony.Ants;
using AntColony.Commands;
using AntColony.Development;
using AntColony.Navigation;
using AntColony.PlayerUI;
using AntColony.World;
using AntColony.WorldPresentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

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
        private const string FoodStorageTilePath =
            "Assets/AntColony/Art/WorldTiles/Background_FoodStorage.asset";
        private const string WaterStorageTilePath =
            "Assets/AntColony/Art/WorldTiles/Background_WaterStorage.asset";
        private const string SleepingTilePath =
            "Assets/AntColony/Art/WorldTiles/Background_Sleeping.asset";
        private const string MaterialStorageTilePath =
            "Assets/AntColony/Art/WorldTiles/Background_MaterialStorage.asset";
        private const string NurseryTilePath =
            "Assets/AntColony/Art/WorldTiles/Background_Nursery.asset";
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
        private const string PrimaryGridAntPrefabPath =
            "Assets/AntColony/Prefabs/Ants/PrimaryGridAnt.prefab";
        private const string PlayerHudPrefabPath =
            "Assets/AntColony/Prefabs/UI/PlayerHud.prefab";
        private const string AntRosterEntryPrefabPath =
            "Assets/AntColony/Prefabs/UI/AntRosterEntry.prefab";

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
            AntSelectionCommandController selectionController =
                FindOne<AntSelectionCommandController>(scene);
            PlayerHudController playerHud = FindOne<PlayerHudController>(scene);

            Require(host != null, "Development world host is missing.");
            Require(visualRenderer != null, "World Tilemap renderer is missing.");
            Require(collisionRenderer != null, "Terrain collision renderer is missing.");
            Require(inspector != null, "Development inspector is missing.");
            Require(cameraController != null, "Development camera controller is missing.");
            Require(selectionController != null,
                "Ant selection and command controller is missing.");
            Require(playerHud != null && host.PlayerHud == playerHud,
                "The authored player HUD is missing or not bound to the host.");
            Require(FindAll<Canvas>(scene).Length == 1,
                "The scene must contain exactly one player HUD Canvas.");
            Require(FindAll<EventSystem>(scene).Length == 1,
                "The scene must contain exactly one EventSystem.");
            Require(FindAll<SimulationTimeControlStrip>(scene).Length == 0,
                "The temporary 3.4B time strip must be suppressed by the player HUD.");
            Require(!selectionController.DevelopmentUiVisible,
                "Developer Mode must start hidden.");
            Require(host.AutonomousAntPrefab != null,
                "Autonomous Ant prefab reference is missing.");
            Require(host.PrimaryGridAntPrefab != null,
                "Primary Grid Ant prefab reference is missing.");
            Require(host.NavigationWorldService != null,
                "Shared grid navigation service is missing.");
            ValidateAutonomousAntPrefab(host.AutonomousAntPrefab);
            ValidatePrimaryGridAntPrefab(host.PrimaryGridAntPrefab);
            Require(FindAll<GridNavigationWorldService>(scene).Length == 1,
                "The scene must contain exactly one shared navigation service.");
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
            GridNavigationMotor primaryGridAntPrefab =
                GetOrCreatePrimaryGridAntPrefab(antSprite);
            GetOrCreatePlayerHudPrefab();
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
            GetOrCreateTile(FoodStorageTilePath, whitePixelSprite,
                new Color32(195, 137, 44, 255), Tile.ColliderType.None);
            GetOrCreateTile(WaterStorageTilePath, whitePixelSprite,
                new Color32(40, 170, 231, 255), Tile.ColliderType.None);
            GetOrCreateTile(SleepingTilePath, whitePixelSprite,
                new Color32(167, 101, 211, 255), Tile.ColliderType.None);
            GetOrCreateTile(MaterialStorageTilePath, whitePixelSprite,
                new Color32(127, 185, 126, 255), Tile.ColliderType.None);
            GetOrCreateTile(NurseryTilePath, whitePixelSprite,
                new Color32(232, 143, 174, 255), Tile.ColliderType.None);
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
            BackgroundTileMaterialDefinition foodStorage =
                LoadBackground("Background_FoodStorage");
            BackgroundTileMaterialDefinition waterStorage =
                LoadBackground("Background_WaterStorage");
            BackgroundTileMaterialDefinition sleeping =
                LoadBackground("Background_Sleeping");
            BackgroundTileMaterialDefinition materialStorage =
                LoadBackground("Background_MaterialStorage");
            BackgroundTileMaterialDefinition nursery =
                LoadBackground("Background_Nursery");
            SolidTileMaterialDefinition soil = LoadSolid("Solid_Soil");
            SolidTileMaterialDefinition blockingOnly =
                LoadSolid("Solid_BlockingOnly");

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            GameObject sceneRoot = new("AntGameDevelopment");
            GameObject playerHudPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PlayerHudPrefabPath);
            Require(playerHudPrefab != null, "The authored Player HUD prefab is missing.");

            GameObject bootstrap = new(
                "World Bootstrap",
                typeof(GridNavigationWorldService),
                typeof(AntSelectionCommandController),
                typeof(AntGameDevelopmentWorldHost));
            bootstrap.transform.SetParent(sceneRoot.transform, false);
            AntGameDevelopmentWorldHost host =
                bootstrap.GetComponent<AntGameDevelopmentWorldHost>();
            GridNavigationWorldService navigationWorld =
                bootstrap.GetComponent<GridNavigationWorldService>();
            AntSelectionCommandController selectionController =
                bootstrap.GetComponent<AntSelectionCommandController>();

            GameObject eventSystemObject = new(
                "Player UI EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            eventSystemObject.transform.SetParent(sceneRoot.transform, false);
            eventSystemObject.GetComponent<InputSystemUIInputModule>()
                .AssignDefaultActions();

            PlayerHudController playerHud = ((GameObject)PrefabUtility.InstantiatePrefab(
                playerHudPrefab, scene)).GetComponent<PlayerHudController>();
            playerHud.name = "Player HUD";
            playerHud.transform.SetParent(sceneRoot.transform, false);

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
            selectionController.Configure(worldCamera, navigationWorld);

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
                blockingOnly,
                visualRenderer,
                collisionRenderer,
                navigationWorld,
                primaryGridAntPrefab,
                autonomousAntPrefab,
                markerObject.transform,
                markerRenderer,
                foodStorage,
                waterStorage,
                sleeping,
                materialStorage,
                nursery,
                playerHud);

            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene, ScenePath),
                "Unity could not save the integrated development scene.");
            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void GetOrCreatePlayerHudPrefab()
        {
            const string uiFolder = "Assets/AntColony/Prefabs/UI";
            if (!AssetDatabase.IsValidFolder(uiFolder))
                AssetDatabase.CreateFolder("Assets/AntColony/Prefabs", "UI");

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Require(font != null, "Unity's built-in UI font is unavailable.");
            AntRosterEntryView rosterEntryPrefab =
                CreateAntRosterEntryPrefab(font);

            GameObject root = new(
                "PlayerHud",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(PlayerHudController));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            AntRosterView roster = CreateRosterPanel(
                root.transform, font, rosterEntryPrefab);
            SimulationControlsView clock = CreateClockPanel(root.transform, font);
            SelectedAntInspectorView inspector =
                CreateInspectorPanel(root.transform, font);
            ColonyResourceSummaryView resources =
                CreateResourcePanel(root.transform, font);
            ColonyJobStatusView jobs = CreateJobPanel(root.transform, font);
            ColonyViewCommandBar commands = CreateCommandPanel(root.transform, font);
            root.GetComponent<PlayerHudController>().Configure(
                root.GetComponent<GraphicRaycaster>(), roster, inspector, clock,
                commands, resources, jobs);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PlayerHudPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            Require(saved != null, "Unity could not save the Player HUD prefab.");
            Require(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerHudPrefabPath)
                    .GetComponent<PlayerHudController>() != null,
                "The saved Player HUD prefab lost its controller.");
        }

        private static AntRosterEntryView CreateAntRosterEntryPrefab(Font font)
        {
            GameObject root = new(
                "AntRosterEntry",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement),
                typeof(AntRosterEntryView));
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(244f, 62f);
            root.GetComponent<Image>().color = new Color32(58, 50, 42, 245);
            root.GetComponent<LayoutElement>().preferredHeight = 62f;

            GameObject highlightObject = CreateStretchImage(
                root.transform, "Selected", new Color32(189, 139, 62, 95));
            Image highlight = highlightObject.GetComponent<Image>();
            highlight.raycastTarget = false;
            highlight.enabled = false;
            Text name = CreateText(root.transform, "Name", font, 16,
                TextAnchor.UpperLeft, new Color32(242, 226, 190, 255));
            SetRect(name.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(8f, 31f), new Vector2(-8f, -4f));
            Text detail = CreateText(root.transform, "Details", font, 12,
                TextAnchor.LowerLeft, new Color32(208, 198, 177, 255));
            SetRect(detail.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(8f, 4f), new Vector2(-8f, -27f));
            root.GetComponent<AntRosterEntryView>().Configure(
                root.GetComponent<Button>(), highlight, name, detail);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                root, AntRosterEntryPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            Require(saved != null, "Unity could not save the ant roster entry prefab.");
            return AssetDatabase.LoadAssetAtPath<GameObject>(AntRosterEntryPrefabPath)
                .GetComponent<AntRosterEntryView>();
        }

        private static AntRosterView CreateRosterPanel(Transform parent, Font font,
            AntRosterEntryView entryPrefab)
        {
            GameObject panel = CreatePanel(parent, "Ant Roster",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(8f, -8f), new Vector2(264f, 246f));
            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 4f;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            CreateLayoutText(panel.transform, "COLONY ANTS", font, 15, 24f);
            var entries = new AntRosterEntryView[3];
            for (int index = 0; index < entries.Length; index++)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
                    entryPrefab.gameObject);
                instance.name = $"Roster Entry {index + 1}";
                instance.transform.SetParent(panel.transform, false);
                entries[index] = instance.GetComponent<AntRosterEntryView>();
            }
            AntRosterView view = panel.AddComponent<AntRosterView>();
            view.Configure(entries);
            return view;
        }

        private static SimulationControlsView CreateClockPanel(
            Transform parent, Font font)
        {
            GameObject panel = CreatePanel(parent, "Simulation Controls",
                Vector2.one, Vector2.one, Vector2.one,
                new Vector2(-8f, -8f), new Vector2(430f, 92f));
            Text time = CreateText(panel.transform, "Time", font, 18,
                TextAnchor.UpperLeft, new Color32(242, 226, 190, 255));
            SetRect(time.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 1f),
                new Vector2(10f, -36f), new Vector2(-2f, -8f));
            Text status = CreateText(panel.transform, "Status", font, 13,
                TextAnchor.UpperRight, new Color32(208, 198, 177, 255));
            SetRect(status.rectTransform, new Vector2(0.5f, 1f), Vector2.one,
                new Vector2(2f, -36f), new Vector2(-10f, -8f));
            GameObject row = CreateHorizontalGroup(panel.transform, "Controls");
            SetRect(row.GetComponent<RectTransform>(), Vector2.zero, Vector2.right,
                new Vector2(10f, 8f), new Vector2(-10f, 44f));
            Button play = CreateButton(row.transform, "Play Pause", "Play", font,
                76f, out Text playLabel);
            Button normal = CreateButton(row.transform, "Speed 1x", "1×", font, 56f,
                out _);
            Button fast = CreateButton(row.transform, "Speed 2x", "2×", font, 56f,
                out _);
            Button veryFast = CreateButton(row.transform, "Speed 3x", "3×", font,
                56f, out _);
            SimulationControlsView view = panel.AddComponent<SimulationControlsView>();
            view.Configure(time, status, play, playLabel, normal, fast, veryFast);
            return view;
        }

        private static SelectedAntInspectorView CreateInspectorPanel(
            Transform parent, Font font)
        {
            GameObject panel = CreatePanel(parent, "Selected Ant Inspector",
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-8f, -22f), new Vector2(320f, 500f));
            Text title = CreateText(panel.transform, "Title", font, 15,
                TextAnchor.MiddleLeft, new Color32(242, 226, 190, 255));
            title.text = "SELECTED ANT";
            SetRect(title.rectTransform, new Vector2(0f, 1f), Vector2.one,
                new Vector2(10f, -34f), new Vector2(-10f, -6f));
            Text content = CreateText(panel.transform, "Content", font, 12,
                TextAnchor.UpperLeft, new Color32(224, 216, 198, 255));
            content.horizontalOverflow = HorizontalWrapMode.Wrap;
            content.verticalOverflow = VerticalWrapMode.Truncate;
            SetRect(content.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(10f, 46f), new Vector2(-10f, -38f));
            Button cancel = CreateButton(panel.transform, "Cancel Current Order",
                "Cancel current order", font, 180f, out _);
            SetRect(cancel.GetComponent<RectTransform>(), new Vector2(0f, 0f),
                new Vector2(1f, 0f), new Vector2(10f, 8f), new Vector2(-10f, 40f));
            SelectedAntInspectorView view =
                panel.AddComponent<SelectedAntInspectorView>();
            view.Configure(content, cancel);
            return view;
        }

        private static ColonyResourceSummaryView CreateResourcePanel(
            Transform parent, Font font)
        {
            GameObject panel = CreatePanel(parent, "Colony Resources",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(8f, -262f), new Vector2(430f, 84f));
            Text content = CreateText(panel.transform, "Content", font, 13,
                TextAnchor.MiddleLeft, new Color32(224, 216, 198, 255));
            SetRect(content.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(10f, 6f), new Vector2(-10f, -6f));
            ColonyResourceSummaryView view =
                panel.AddComponent<ColonyResourceSummaryView>();
            view.Configure(content);
            return view;
        }

        private static ColonyJobStatusView CreateJobPanel(Transform parent, Font font)
        {
            GameObject panel = CreatePanel(parent, "Colony Work Status",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(8f, -354f), new Vector2(430f, 162f));
            Text content = CreateText(panel.transform, "Content", font, 12,
                TextAnchor.UpperLeft, new Color32(224, 216, 198, 255));
            content.horizontalOverflow = HorizontalWrapMode.Wrap;
            content.verticalOverflow = VerticalWrapMode.Truncate;
            SetRect(content.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(10f, 8f), new Vector2(-10f, -8f));
            ColonyJobStatusView view = panel.AddComponent<ColonyJobStatusView>();
            view.Configure(content);
            return view;
        }

        private static ColonyViewCommandBar CreateCommandPanel(
            Transform parent, Font font)
        {
            GameObject panel = CreatePanel(parent, "Colony View Commands",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 8f), new Vector2(620f, 160f));
            GameObject row = CreateHorizontalGroup(panel.transform, "Tools");
            SetRect(row.GetComponent<RectTransform>(), new Vector2(0f, 1f),
                Vector2.one, new Vector2(10f, -44f), new Vector2(-10f, -8f));
            Button select = CreateButton(row.transform, "Select", "Select", font, 112f,
                out _);
            Button excavate = CreateButton(row.transform, "Excavate", "Excavate", font,
                128f, out _);
            Button build = CreateButton(row.transform, "Build", "Build", font, 112f,
                out _);
            Button cancel = CreateButton(row.transform, "Cancel Designation",
                "Cancel designation", font, 170f, out _);

            GameObject menu = new(
                "Build Recipes", typeof(RectTransform), typeof(Image),
                typeof(GridLayoutGroup));
            menu.transform.SetParent(panel.transform, false);
            menu.GetComponent<Image>().color = new Color32(43, 38, 33, 250);
            GridLayoutGroup grid = menu.GetComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(4, 4, 4, 4);
            grid.spacing = new Vector2(4f, 4f);
            grid.cellSize = new Vector2(190f, 34f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            SetRect(menu.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(-295f, -35f),
                new Vector2(295f, 43f));
            var recipeButtons = new Button[6];
            var recipeLabels = new Text[6];
            for (int index = 0; index < 6; index++)
                recipeButtons[index] = CreateButton(menu.transform,
                    $"Recipe {index + 1}", "Recipe", font, 190f,
                    out recipeLabels[index], 11);

            Text status = CreateText(panel.transform, "Command Status", font, 12,
                TextAnchor.MiddleCenter, new Color32(224, 216, 198, 255));
            SetRect(status.rectTransform, Vector2.zero, Vector2.right,
                new Vector2(10f, 6f), new Vector2(-10f, 34f));
            ColonyViewCommandBar view = panel.AddComponent<ColonyViewCommandBar>();
            view.Configure(select, excavate, build, cancel, menu,
                recipeButtons, recipeLabels, status);
            return view;
        }

        private static GameObject CreatePanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPosition, Vector2 size)
        {
            GameObject panel = new(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            Image image = panel.GetComponent<Image>();
            image.color = new Color32(38, 33, 29, 235);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return panel;
        }

        private static GameObject CreateStretchImage(
            Transform parent, string name, Color colour)
        {
            GameObject imageObject = new(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            imageObject.GetComponent<Image>().color = colour;
            SetRect(imageObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero);
            return imageObject;
        }

        private static Text CreateLayoutText(Transform parent, string value,
            Font font, int fontSize, float height)
        {
            Text text = CreateText(parent, value, font, fontSize,
                TextAnchor.MiddleLeft, new Color32(242, 226, 190, 255));
            text.text = value;
            LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            return text;
        }

        private static Text CreateText(Transform parent, string name, Font font,
            int fontSize, TextAnchor alignment, Color colour)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = colour;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateHorizontalGroup(Transform parent, string name)
        {
            GameObject row = new(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            return row;
        }

        private static Button CreateButton(Transform parent, string name,
            string label, Font font, float preferredWidth, out Text labelText,
            int fontSize = 13)
        {
            GameObject buttonObject = new(
                name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color32(77, 67, 55, 255);
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.preferredWidth = preferredWidth;
            layout.preferredHeight = 32f;
            labelText = CreateText(buttonObject.transform, "Label", font, fontSize,
                TextAnchor.MiddleCenter, new Color32(242, 232, 211, 255));
            labelText.text = label;
            SetRect(labelText.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(4f, 2f), new Vector2(-4f, -2f));
            return button;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
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

        private static GridNavigationMotor GetOrCreatePrimaryGridAntPrefab(
            Sprite antSprite)
        {
            GameObject root = new(
                "PrimaryGridAnt",
                typeof(Rigidbody2D),
                typeof(CapsuleCollider2D),
                typeof(GridNavigationMotor));
            GameObject visual = new("Visual", typeof(SpriteRenderer));
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = new Vector3(0.8f, 0.5f, 1f);

            SpriteRenderer spriteRenderer = visual.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = antSprite;
            spriteRenderer.color = new Color32(70, 225, 235, 180);
            spriteRenderer.sortingOrder = 6;

            Rigidbody2D body = root.GetComponent<Rigidbody2D>();
            body.simulated = false;
            body.gravityScale = 0f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;

            CapsuleCollider2D collider = root.GetComponent<CapsuleCollider2D>();
            collider.direction = CapsuleDirection2D.Horizontal;
            collider.size = AntCollisionProfile.OrdinaryAnt.Size;
            collider.excludeLayers = LayerMask.GetMask("Default");

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                root,
                PrimaryGridAntPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            Require(saved != null, "Unity could not save the Primary Grid Ant prefab.");
            return saved.GetComponent<GridNavigationMotor>();
        }

        private static void ValidatePrimaryGridAntPrefab(GridNavigationMotor prefab)
        {
            Rigidbody2D body = prefab.GetComponent<Rigidbody2D>();
            CapsuleCollider2D collider = prefab.GetComponent<CapsuleCollider2D>();
            Require(body != null && collider != null,
                "Primary Grid Ant requires one Rigidbody2D and CapsuleCollider2D.");
            Require(!body.simulated && body.gravityScale == 0f &&
                    body.constraints == RigidbodyConstraints2D.FreezeRotation,
                "Primary Grid Ant must start in stable grid-traversal physics state.");
            Require(collider.direction == CapsuleDirection2D.Horizontal &&
                    collider.size == AntCollisionProfile.OrdinaryAnt.Size,
                "Primary Grid Ant collider must match the ordinary-ant profile.");
            Require(prefab.GetComponent<PlayerMovementInput>() == null &&
                    prefab.GetComponent<AntSurfaceMotor>() == null &&
                    prefab.GetComponent<AntAutonomousMotor>() == null,
                "Primary Grid Ant must contain only the production grid motor.");
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
            ValidateTileColour(FoodStorageTilePath, new Color32(195, 137, 44, 255));
            ValidateTileColour(WaterStorageTilePath, new Color32(40, 170, 231, 255));
            ValidateTileColour(SleepingTilePath, new Color32(167, 101, 211, 255));
            ValidateTileColour(MaterialStorageTilePath, new Color32(127, 185, 126, 255));
            ValidateTileColour(NurseryTilePath, new Color32(232, 143, 174, 255));
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
