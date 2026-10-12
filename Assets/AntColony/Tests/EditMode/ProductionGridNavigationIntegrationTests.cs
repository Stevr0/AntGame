using System.Linq;
using AntColony.Development;
using AntColony.Navigation;
using AntColony.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AntColony.Tests.EditMode
{
    /// <summary>Focused production ownership and prefab checks for Milestone 2.4D.</summary>
    public sealed class ProductionGridNavigationIntegrationTests
    {
        private const string ScenePath =
            "Assets/AntColony/Scenes/AntGameDevelopment.unity";
        private const string PrefabPath =
            "Assets/AntColony/Prefabs/Ants/PrimaryGridAnt.prefab";

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void PrimaryPrefab_UsesOnlyProductionGridMovement()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<GridNavigationMotor>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<Rigidbody2D>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<CapsuleCollider2D>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<AntAutonomousMotor>(), Is.Null);
            Assert.That(prefab.GetComponent<Rigidbody2D>().simulated, Is.False);
            string[] movementComponents = prefab.GetComponents<MonoBehaviour>()
                .Where(component => component != null)
                .Select(component => component.GetType().Name)
                .ToArray();
            Assert.That(movementComponents, Does.Not.Contain("PlayerMovementInput"));
            Assert.That(movementComponents, Does.Not.Contain("AntSurfaceMotor"));
        }

        [Test]
        public void DevelopmentWorld_HasOneSharedProductionNavigationService()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            GridNavigationWorldService[] services = Object
                .FindObjectsByType<GridNavigationWorldService>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            Assert.That(services, Has.Length.EqualTo(1));
            Assert.That(host.NavigationWorldService, Is.SameAs(services.Single()));
            Assert.That(host.NavigationWorldService.IsInitialized, Is.True);
            Assert.That(host.GridNavigationMap,
                Is.SameAs(host.NavigationWorldService.Map));
            Assert.That(host.GridNavigationScheduler,
                Is.SameAs(host.NavigationWorldService.Scheduler));

            int initializationCount = services[0].InitializationCount;
            Assert.That(services[0].Initialize(
                host.WorldData,
                AntCollisionProfile.OrdinaryAnt), Is.True);
            Assert.That(services[0].InitializationCount,
                Is.EqualTo(initializationCount),
                "The same active world must not create a duplicate map or scheduler.");
        }

        [Test]
        public void MovementCommand_WorksWithoutDevelopmentInspector()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Assert.That(host.TryFindSafeSpawnPosition(
                out Vector2 spawn,
                out string spawnMessage), Is.True, spawnMessage);
            Vector2Int spawnCell =
                WorldTileCoordinates.WorldPositionToCell(spawn);
            GridNavigationMotor ant = Object.Instantiate(host.PrimaryGridAntPrefab);

            try
            {
                Assert.That(ant.InitializeNavigation(
                    host.NavigationWorldService,
                    spawnCell), Is.True, ant.StatusMessage);
                Assert.That(ant.MoveTo(
                    SampleWorldGenerator.GridDirectDestination), Is.True);
                Assert.That(ant.State,
                    Is.EqualTo(GridNavigationMotorState.WaitingForPath));
                Assert.That(ant.HasPendingRequest, Is.True);
                Assert.That(ant.NavigationWorld,
                    Is.SameAs(host.NavigationWorldService));
            }
            finally
            {
                Object.DestroyImmediate(ant.gameObject);
            }
        }

        private static AntGameDevelopmentWorldHost OpenHost()
        {
            Scene scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);
            AntGameDevelopmentWorldHost host = scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<AntGameDevelopmentWorldHost>(true))
                .Single();
            Assert.That(host.RebuildWorld(), Is.True);
            return host;
        }
    }
}
