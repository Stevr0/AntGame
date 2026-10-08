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
    /// Creates the small, authored scene and prefab used to verify the first
    /// horizontal-movement slice of Milestone 1.
    /// </summary>
    public static class Milestone1PrototypeSetup
    {
        private const string RootFolder = "Assets/AntColony";
        private const string PlaceholderFolder = RootFolder + "/Art/Placeholder";
        private const string PrefabFolder = RootFolder + "/Prefabs/Ants";
        private const string SceneFolder = RootFolder + "/Scenes";
        private const string AntPrefabPath = PrefabFolder + "/PlayerAnt.prefab";
        private const string ScenePath = SceneFolder + "/AntMovementPrototype.unity";
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
                "Created the Milestone 1 horizontal movement prototype. " +
                "Open AntMovementPrototype and use A/D or Left/Right in Play Mode.");
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
            body.freezeRotation = true;
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
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.backgroundColor = new Color(0.09f, 0.06f, 0.04f);
            camera.clearFlags = CameraClearFlags.SolidColor;

            GameObject floor = new GameObject("Prototype Floor");
            floor.layer = climbableLayer;
            floor.transform.position = new Vector3(0f, -2.5f, 0f);
            floor.transform.localScale = new Vector3(16f, 1f, 1f);

            SpriteRenderer floorRenderer = floor.AddComponent<SpriteRenderer>();
            floorRenderer.sprite = soilSprite;
            floor.AddComponent<BoxCollider2D>();

            GameObject antInstance = (GameObject)PrefabUtility.InstantiatePrefab(antPrefab, scene);
            antInstance.name = "Player Ant";
            antInstance.transform.position = new Vector3(0f, -1.6f, 0f);

            return scene;
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
