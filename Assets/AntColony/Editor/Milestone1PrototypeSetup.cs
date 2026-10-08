using System.Linq;
using AntColony.Ants;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace AntColony.EditorTools
{
    /// <summary>
    /// Creates and incrementally updates the authored scene and prefab used to
    /// verify the Milestone 1 surface-movement slices.
    /// </summary>
    public static class Milestone1PrototypeSetup
    {
        private const string RootFolder = "Assets/AntColony";
        private const string PlaceholderFolder = RootFolder + "/Art/Placeholder";
        private const string PrefabFolder = RootFolder + "/Prefabs/Ants";
        private const string SceneFolder = RootFolder + "/Scenes";
        private const string AntPrefabPath = PrefabFolder + "/PlayerAnt.prefab";
        private const string ScenePath = SceneFolder + "/AntMovementPrototype.unity";
        private const string SoilSpritePath = PlaceholderFolder + "/SoilPrototype.asset";
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";
        private const string ClimbableSurfaceLayer = "ClimbableSurface";

        /// <summary>
        /// Creates or refreshes the prototype assets. Existing prototype assets at
        /// the documented paths are intentionally replaced so this command remains
        /// predictable while the milestone is being established.
        /// </summary>
        [MenuItem("Ant Colony/Milestone 1/Create Horizontal Movement Prototype")]
        public static void CreateHorizontalMovementPrototype()
        {
            EnsureProjectFolders();

            int climbableLayer = EnsureLayerExists(ClimbableSurfaceLayer);
            Sprite antSprite = CreateSolidSpriteAsset(
                PlaceholderFolder + "/PlayerAntPrototype.asset",
                "Player Ant Prototype Sprite",
                new Color(0.30f, 0.08f, 0.04f));
            Sprite soilSprite = CreateSolidSpriteAsset(
                PlaceholderFolder + "/SoilPrototype.asset",
                "Soil Prototype Sprite",
                new Color(0.30f, 0.18f, 0.10f));

            GameObject antPrefab = CreateAntPrefab(antSprite, climbableLayer);
            Scene prototypeScene = CreatePrototypeScene(antPrefab, soilSprite, climbableLayer);

            EditorSceneManager.SaveScene(prototypeScene, ScenePath);
            EditorSceneManager.CloseScene(prototypeScene, true);
            AddPrototypeSceneToBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);

            Debug.Log(
                "Created the Milestone 1 surface-transition prototype. " +
                "Open AntMovementPrototype and test floor, wall, ceiling, and corner movement.");
        }

        private static void EnsureProjectFolders()
        {
            EnsureFolder("Assets", "AntColony");
            EnsureFolder(RootFolder, "Art");
            EnsureFolder(RootFolder + "/Art", "Placeholder");
            EnsureFolder(RootFolder, "Prefabs");
            EnsureFolder(RootFolder + "/Prefabs", "Ants");
            EnsureFolder(RootFolder, "Scenes");
            EnsureFolder(RootFolder, "Scripts");
            EnsureFolder(RootFolder + "/Scripts", "Ants");
            EnsureFolder(RootFolder + "/Scripts", "Camera");
            EnsureFolder(RootFolder, "Tests");
            EnsureFolder(RootFolder + "/Tests", "EditMode");
            EnsureFolder(RootFolder + "/Tests", "PlayMode");
        }

        private static void EnsureFolder(string parentFolder, string childFolder)
        {
            string completePath = parentFolder + "/" + childFolder;
            if (!AssetDatabase.IsValidFolder(completePath))
            {
                AssetDatabase.CreateFolder(parentFolder, childFolder);
            }
        }

        private static int EnsureLayerExists(string layerName)
        {
            int existingLayer = LayerMask.NameToLayer(layerName);
            if (existingLayer >= 0)
            {
                return existingLayer;
            }

            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");

            // Layers 0-7 are reserved by Unity. Use the first available user layer.
            for (int index = 8; index < 32; index++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(index);
                if (!string.IsNullOrEmpty(layer.stringValue))
                {
                    continue;
                }

                layer.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return index;
            }

            throw new System.InvalidOperationException(
                $"No free Unity layer is available for '{layerName}'.");
        }

        private static Sprite CreateSolidSpriteAsset(string assetPath, string spriteName, Color color)
        {
            if (AssetDatabase.LoadMainAssetAtPath(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            Texture2D texture = new Texture2D(1, 1)
            {
                name = spriteName + " Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();

            AssetDatabase.CreateAsset(texture, assetPath);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            sprite.name = spriteName;
            AssetDatabase.AddObjectToAsset(sprite, texture);
            AssetDatabase.ImportAsset(assetPath);

            return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().First();
        }

        private static GameObject CreateAntPrefab(Sprite antSprite, int climbableLayer)
        {
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputActions == null)
            {
                throw new System.InvalidOperationException(
                    $"The Input Action asset was not found at '{InputActionsPath}'.");
            }

            GameObject ant = new GameObject("Player Ant");

            SpriteRenderer renderer = ant.AddComponent<SpriteRenderer>();
            renderer.sprite = antSprite;
            renderer.sortingOrder = 1;

            Rigidbody2D body = ant.AddComponent<Rigidbody2D>();
            body.gravityScale = 4f;
            // The motor controls rotation so the ant can align its feet with a wall.
            body.constraints = RigidbodyConstraints2D.None;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            CapsuleCollider2D collider = ant.AddComponent<CapsuleCollider2D>();
            collider.direction = CapsuleDirection2D.Horizontal;
            collider.size = new Vector2(1.2f, 0.6f);

            PlayerInput playerInput = ant.AddComponent<PlayerInput>();
            playerInput.actions = inputActions;
            playerInput.defaultActionMap = "Player";
            playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

            AntSurfaceMotor motor = ant.AddComponent<AntSurfaceMotor>();
            SerializedObject serializedMotor = new SerializedObject(motor);
            serializedMotor.FindProperty("climbableSurfaces").intValue = 1 << climbableLayer;
            serializedMotor.ApplyModifiedPropertiesWithoutUndo();
            ant.AddComponent<PlayerMovementInput>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(ant, AntPrefabPath);
            Object.DestroyImmediate(ant);
            return prefab;
        }

        /// <summary>
        /// Adds the dedicated player-input component to the existing ant prefab
        /// without regenerating the scene, placeholder art, or unrelated assets.
        /// </summary>
        [MenuItem("Ant Colony/Milestone 1/Apply Directional Input Separation")]
        public static void ApplyDirectionalInputSeparation()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(AntPrefabPath);

            try
            {
                if (prefabRoot.GetComponent<PlayerInput>() == null ||
                    prefabRoot.GetComponent<AntSurfaceMotor>() == null)
                {
                    throw new System.InvalidOperationException(
                        "PlayerAnt.prefab is missing its PlayerInput or AntSurfaceMotor component.");
                }

                if (prefabRoot.GetComponent<PlayerMovementInput>() == null)
                {
                    prefabRoot.AddComponent<PlayerMovementInput>();
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, AntPrefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("PlayerAnt.prefab now forwards directional input through PlayerMovementInput.");
        }

        /// <summary>
        /// Migrates the existing prototype in place for the first wall-climbing
        /// slice. This deliberately preserves the authored prefab, scene, input
        /// setup, and placeholder art instead of regenerating unrelated assets.
        /// </summary>
        [MenuItem("Ant Colony/Milestone 1/Apply Wall Climbing Prototype")]
        public static void ApplyWallClimbingPrototype()
        {
            ApplySurfaceTransitionPrototype();
        }

        /// <summary>
        /// Expands the authored scene into the P1 floor/wall/ceiling course while
        /// keeping the existing prefab, input asset, and placeholder art.
        /// </summary>
        [MenuItem("Ant Colony/Milestone 1/Apply Surface Transition Course")]
        public static void ApplySurfaceTransitionPrototype()
        {
            int climbableLayer = EnsureLayerExists(ClimbableSurfaceLayer);
            Sprite soilSprite = AssetDatabase.LoadAllAssetsAtPath(SoilSpritePath)
                .OfType<Sprite>()
                .FirstOrDefault();

            if (soilSprite == null)
            {
                throw new System.InvalidOperationException(
                    $"The prototype soil sprite was not found at '{SoilSpritePath}'.");
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(AntPrefabPath);

            try
            {
                Rigidbody2D body = prefabRoot.GetComponent<Rigidbody2D>();
                AntSurfaceMotor motor = prefabRoot.GetComponent<AntSurfaceMotor>();
                if (body == null || motor == null)
                {
                    throw new System.InvalidOperationException(
                        "PlayerAnt.prefab is missing its Rigidbody2D or AntSurfaceMotor component.");
                }

                body.constraints = RigidbodyConstraints2D.None;

                SerializedObject serializedMotor = new SerializedObject(motor);
                serializedMotor.FindProperty("climbableSurfaces").intValue = 1 << climbableLayer;
                serializedMotor.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, AntPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForMigration = !scene.IsValid() || !scene.isLoaded;
            if (openedForMigration)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            try
            {
                ConfigureSurfaceTransitionCourse(scene, soilSprite, climbableLayer);

                GameObject playerAnt = scene.GetRootGameObjects()
                    .FirstOrDefault(root => root.name == "Player Ant");
                if (playerAnt != null)
                {
                    playerAnt.transform.SetPositionAndRotation(
                        new Vector3(-4.5f, -1.6f, 0f),
                        Quaternion.identity);
                }

                Camera camera = scene.GetRootGameObjects()
                    .Select(root => root.GetComponent<Camera>())
                    .FirstOrDefault(candidate => candidate != null);
                if (camera != null)
                {
                    camera.transform.position = new Vector3(0f, 0.5f, -10f);
                    camera.orthographicSize = 5.5f;
                }

                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            finally
            {
                if (openedForMigration && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Applied the P1 transition course with floor, walls, ceiling, and outside-corner block.");
        }

        /// <summary>
        /// Checks the authored references needed by this slice. This validation is
        /// intentionally structural; hands-on Play Mode testing is still required
        /// to judge movement feel, collision response, and visible alignment.
        /// </summary>
        [MenuItem("Ant Colony/Milestone 1/Validate Wall Climbing Prototype")]
        public static void ValidateWallClimbingPrototype()
        {
            ValidateSurfaceTransitionPrototype();
        }

        /// <summary>
        /// Structurally validates the complete transition test course. Movement
        /// quality and collision response still require Play Mode observation.
        /// </summary>
        [MenuItem("Ant Colony/Milestone 1/Validate Surface Transition Course")]
        public static void ValidateSurfaceTransitionPrototype()
        {
            int climbableLayer = LayerMask.NameToLayer(ClimbableSurfaceLayer);
            Require(climbableLayer >= 0, $"Layer '{ClimbableSurfaceLayer}' does not exist.");

            GameObject antPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AntPrefabPath);
            Require(antPrefab != null, $"Player prefab is missing at '{AntPrefabPath}'.");
            Require(antPrefab.GetComponent<PlayerInput>() != null, "Player prefab has no PlayerInput.");
            Require(
                antPrefab.GetComponent<PlayerMovementInput>() != null,
                "Player prefab has no PlayerMovementInput.");

            AntSurfaceMotor motor = antPrefab.GetComponent<AntSurfaceMotor>();
            Rigidbody2D body = antPrefab.GetComponent<Rigidbody2D>();
            Require(motor != null, "Player prefab has no AntSurfaceMotor.");
            Require(body != null, "Player prefab has no Rigidbody2D.");
            Require(
                body.constraints == RigidbodyConstraints2D.None,
                "Player Rigidbody2D rotation is constrained, so it cannot align to walls.");

            SerializedObject serializedMotor = new SerializedObject(motor);
            int climbableMask = serializedMotor.FindProperty("climbableSurfaces").intValue;
            Require(
                (climbableMask & (1 << climbableLayer)) != 0,
                "AntSurfaceMotor does not include the ClimbableSurface layer.");

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForValidation = !scene.IsValid() || !scene.isLoaded;
            if (openedForValidation)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            try
            {
                ValidateSurface(scene, "Prototype Floor", climbableLayer);
                ValidateSurface(scene, "Left Climbable Wall", climbableLayer);
                ValidateSurface(scene, "Right Climbable Wall", climbableLayer);
                ValidateSurface(scene, "Prototype Ceiling", climbableLayer);
                ValidateSurface(scene, "Outside Corner Block", climbableLayer);
                Require(
                    scene.GetRootGameObjects().Any(root => root.name == "Player Ant"),
                    "Prototype scene has no Player Ant prefab instance.");
            }
            finally
            {
                if (openedForValidation && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            Debug.Log(
                "Surface-transition prototype validation passed: prefab wiring, climbable mask, " +
                "rotatable Rigidbody2D, player instance, enclosed course, and outside-corner block are present.");
        }

        private static void ValidateSurface(Scene scene, string objectName, int expectedLayer)
        {
            GameObject[] matchingSurfaces = scene.GetRootGameObjects()
                .Where(root => root.name == objectName)
                .ToArray();
            Require(
                matchingSurfaces.Length == 1,
                $"Prototype scene must contain exactly one '{objectName}', but found {matchingSurfaces.Length}.");

            GameObject surface = matchingSurfaces[0];
            Require(surface != null, $"Prototype scene is missing '{objectName}'.");
            Require(
                surface.layer == expectedLayer,
                $"'{objectName}' is not on the ClimbableSurface layer.");
            Require(
                surface.GetComponent<BoxCollider2D>() != null,
                $"'{objectName}' has no BoxCollider2D.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new System.InvalidOperationException(message);
            }
        }

        private static Scene CreatePrototypeScene(
            GameObject antPrefab,
            Sprite soilSprite,
            int climbableLayer)
        {
            // Additive mode protects any scene the developer already has open.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            scene.name = "AntMovementPrototype";

            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0.5f, -10f);

            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.5f;
            camera.backgroundColor = new Color(0.09f, 0.06f, 0.04f);
            camera.clearFlags = CameraClearFlags.SolidColor;

            GameObject floor = new GameObject("Prototype Floor");
            floor.layer = climbableLayer;
            floor.transform.position = new Vector3(0f, -2.5f, 0f);
            floor.transform.localScale = new Vector3(16f, 1f, 1f);

            SpriteRenderer floorRenderer = floor.AddComponent<SpriteRenderer>();
            floorRenderer.sprite = soilSprite;
            floor.AddComponent<BoxCollider2D>();

            ConfigureSurfaceTransitionCourse(scene, soilSprite, climbableLayer);

            GameObject antInstance = (GameObject)PrefabUtility.InstantiatePrefab(antPrefab, scene);
            antInstance.name = "Player Ant";
            antInstance.transform.position = new Vector3(-4.5f, -1.6f, 0f);

            return scene;
        }

        private static void ConfigureSurfaceTransitionCourse(
            Scene scene,
            Sprite soilSprite,
            int climbableLayer)
        {
            CreateOrUpdateSurface(
                scene,
                "Left Climbable Wall",
                new Vector3(-7.5f, 0.5f, 0f),
                new Vector3(1f, 6f, 1f),
                soilSprite,
                climbableLayer);
            CreateOrUpdateSurface(
                scene,
                "Right Climbable Wall",
                new Vector3(7.5f, 0.5f, 0f),
                new Vector3(1f, 6f, 1f),
                soilSprite,
                climbableLayer);
            CreateOrUpdateSurface(
                scene,
                "Prototype Ceiling",
                new Vector3(0f, 4f, 0f),
                new Vector3(16f, 1f, 1f),
                soilSprite,
                climbableLayer);

            // This pillar meets the floor and exposes two convex upper corners.
            // It gives the ant room to reverse during an outside-corner arc while
            // keeping the main room's four concave corners easy to understand.
            CreateOrUpdateSurface(
                scene,
                "Outside Corner Block",
                new Vector3(0f, -0.75f, 0f),
                new Vector3(3f, 2.5f, 1f),
                soilSprite,
                climbableLayer);
        }

        private static void CreateOrUpdateSurface(
            Scene scene,
            string objectName,
            Vector3 position,
            Vector3 scale,
            Sprite sprite,
            int layer)
        {
            GameObject surface = scene.GetRootGameObjects()
                .FirstOrDefault(root => root.name == objectName);

            if (surface == null)
            {
                surface = new GameObject(objectName);
                SceneManager.MoveGameObjectToScene(surface, scene);
            }

            surface.layer = layer;
            surface.transform.SetPositionAndRotation(position, Quaternion.identity);
            surface.transform.localScale = scale;

            SpriteRenderer renderer = surface.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = surface.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = sprite;

            if (surface.GetComponent<BoxCollider2D>() == null)
            {
                surface.AddComponent<BoxCollider2D>();
            }
        }

        private static void AddPrototypeSceneToBuildSettings()
        {
            EditorBuildSettingsScene[] existingScenes = EditorBuildSettings.scenes;
            if (existingScenes.Any(scene => scene.path == ScenePath))
            {
                return;
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }
                .Concat(existingScenes)
                .ToArray();
        }
    }
}
