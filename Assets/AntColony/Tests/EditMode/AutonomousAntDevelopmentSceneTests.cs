using System.Linq;
using System.Reflection;
using AntColony.Development;
using AntColony.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AntColony.Tests.EditMode
{
    /// <summary>Scene-backed spawn, wiring and mutation checks for 2.3A/2.3B.</summary>
    public sealed class AutonomousAntDevelopmentSceneTests
    {
        private const string ScenePath =
            "Assets/AntColony/Scenes/AntGameDevelopment.unity";

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void SafeSpawn_IsGroundedAndRepeatedSpawnKeepsExactlyOneAnt()
        {
            AntGameDevelopmentWorldHost host = OpenHost();

            Assert.That(host.SpawnOrResetAutonomousAnt(out string firstMessage),
                Is.True, firstMessage);
            AntAutonomousMotor firstAnt = host.AutonomousAnt;
            Assert.That(host.SpawnOrResetAutonomousAnt(out string secondMessage),
                Is.True, secondMessage);

            Assert.That(host.SpawnedAutonomousAntCount, Is.EqualTo(1));
            Assert.That(host.AutonomousAnt, Is.SameAs(firstAnt));
            var evaluator = new AntMovementCapabilityEvaluator(host.WorldData);
            AntMovementCapabilityResult capability = evaluator.Evaluate(
                firstAnt.Position,
                0f,
                AntCollisionProfile.OrdinaryAnt);
            Assert.That(
                capability.SuggestedState,
                Is.EqualTo(AntMovementEligibilityState.Grounded));
            Assert.That(capability.AllowsHorizontalMovement, Is.True);
        }

        [Test]
        public void InvalidBoundedSpawn_IsRejectedWithoutCreatingAnt()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            SetField(host, "preferredSpawnX", 0);
            SetField(host, "spawnSearchRadius", 0);

            LogAssert.Expect(
                LogType.Error,
                "No safe grounded spawn exists within 0 cells of X=0.");
            Assert.That(host.SpawnOrResetAutonomousAnt(out string message), Is.False);
            Assert.That(message, Does.Contain("No safe grounded spawn"));
            Assert.That(host.SpawnedAutonomousAntCount, Is.Zero);
        }

        [Test]
        public void NearbyDirectGroundDestination_IsAcceptedWithoutPlayerInput()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Assert.That(host.SpawnOrResetAutonomousAnt(out string spawnMessage),
                Is.True, spawnMessage);

            Vector2Int destinationCell = FindAcceptedDestination(host, 1, 12);

            Assert.That(host.AutonomousAnt.IsMoving, Is.True);
            Assert.That(destinationCell, Is.Not.EqualTo(default(Vector2Int)));
            string[] componentNames = host.AutonomousAnt
                .GetComponents<MonoBehaviour>()
                .Where(component => component != null)
                .Select(component => component.GetType().Name)
                .ToArray();
            Assert.That(componentNames, Does.Not.Contain("PlayerMovementInput"));
            Assert.That(componentNames, Does.Not.Contain("AntSurfaceMotor"));
        }

        [Test]
        public void SurfaceGripFixture_IsPreparedAndAcceptsVerticalCommand()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Assert.That(host.SpawnOrResetAutonomousAnt(out string spawnMessage),
                Is.True, spawnMessage);

            Assert.That(host.TryGetCell(
                host.GripFixtureBottom,
                out WorldTileCellData bottom), Is.True);
            Assert.That(host.WorldData.TryGetBackgroundMaterialDefinition(
                bottom.BackgroundMaterialId,
                out BackgroundTileMaterialDefinition material), Is.True);
            Assert.That(material.ProvidesClimbingGrip, Is.True);
            Assert.That(host.TryAssignAutonomousDestination(
                host.GripFixtureTop,
                out string movementMessage), Is.True, movementMessage);
            Assert.That(host.AutonomousAnt.Destination.y,
                Is.GreaterThan(host.AutonomousAnt.Position.y));
        }

        [Test]
        public void PreparedSurfaceCell_CanRoundTripToSkyForGripMutationTesting()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Vector2Int cell = host.GripFixtureTop;

            Assert.That(host.TogglePreparedBackground(cell), Is.True);
            Assert.That(host.TryGetCell(cell, out WorldTileCellData sky), Is.True);
            Assert.That(host.WorldData.TryGetBackgroundMaterialDefinition(
                sky.BackgroundMaterialId,
                out BackgroundTileMaterialDefinition unprepared), Is.True);
            Assert.That(unprepared.ProvidesClimbingGrip, Is.False);

            Assert.That(host.TogglePreparedBackground(cell), Is.True);
            Assert.That(host.TryGetCell(cell, out WorldTileCellData prepared), Is.True);
            Assert.That(host.WorldData.TryGetBackgroundMaterialDefinition(
                prepared.BackgroundMaterialId,
                out BackgroundTileMaterialDefinition restored), Is.True);
            Assert.That(restored.ProvidesClimbingGrip, Is.True);
        }

        [Test]
        public void UnsupportedAndRemoteDestinations_AreRejectedReadably()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Assert.That(host.SpawnOrResetAutonomousAnt(out string spawnMessage),
                Is.True, spawnMessage);

            Assert.That(
                host.TryAssignAutonomousDestination(new Vector2Int(0, -7), out string gap),
                Is.False);
            Assert.That(gap, Is.Not.Empty);
            Assert.That(
                host.TryAssignAutonomousDestination(new Vector2Int(1000, 1000), out string missing),
                Is.False);
            Assert.That(missing, Does.Contain("UnavailableWorldData"));
        }

        [Test]
        public void ExistingInspectionUiAndCameraRemainAuthoredOnce()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Assert.That(FindAll<DevelopmentWorldInspector>(scene), Has.Length.EqualTo(1));
            Assert.That(FindAll<DevelopmentCameraController>(scene), Has.Length.EqualTo(1));
            Assert.That(FindAll<AntAutonomousMotor>(scene), Is.Empty,
                "The runtime host, not the scene YAML, creates the one ant.");
        }

        private static AntGameDevelopmentWorldHost OpenHost()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AntGameDevelopmentWorldHost host =
                FindAll<AntGameDevelopmentWorldHost>(scene).Single();
            Assert.That(host.RebuildWorld(), Is.True);
            return host;
        }

        private static Vector2Int FindAcceptedDestination(
            AntGameDevelopmentWorldHost host,
            int minimumDistance,
            int maximumDistance)
        {
            Vector2Int antCell =
                WorldTileCoordinates.WorldPositionToCell(host.AutonomousAnt.Position);
            for (int distance = minimumDistance; distance <= maximumDistance; distance++)
            {
                foreach (int sign in new[] { 1, -1 })
                {
                    var candidate = new Vector2Int(
                        antCell.x + sign * distance,
                        antCell.y);
                    if (host.TryAssignAutonomousDestination(candidate, out _))
                    {
                        return candidate;
                    }
                }
            }

            Assert.Fail("The authored surface has no nearby accepted direct destination.");
            return default;
        }

        private static T[] FindAll<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName}.");
            field.SetValue(target, value);
        }
    }
}
