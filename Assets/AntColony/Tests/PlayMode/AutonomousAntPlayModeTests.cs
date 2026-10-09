using System.Collections;
using AntColony.Development;
using AntColony.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AntColony.Tests.PlayMode
{
    /// <summary>Physics-backed checks for the first autonomous grounded ant.</summary>
    public sealed class AutonomousAntPlayModeTests
    {
        private const string SceneName = "AntGameDevelopment";

        [UnityTest]
        public IEnumerator OneAnt_MovesWithoutInputAndStopsAtDestination()
        {
            yield return LoadDevelopmentScene();
            AntGameDevelopmentWorldHost host = FindHost();
            yield return new WaitForFixedUpdate();

            Vector2Int destinationCell = FindAcceptedDestination(host, 1, 10);
            Vector2 expected = host.AutonomousAnt.Destination;
            yield return WaitForTerminalState(host.AutonomousAnt, 8f);

            Assert.That(host.SpawnedAutonomousAntCount, Is.EqualTo(1));
            Assert.That(
                host.AutonomousAnt.State,
                Is.EqualTo(AntAutonomousMovementState.Arrived));
            Assert.That(
                Mathf.Abs(host.AutonomousAnt.Position.x - expected.x),
                Is.LessThanOrEqualTo(host.AutonomousAnt.ArrivalTolerance + 0.03f));
            Assert.That(destinationCell, Is.Not.EqualTo(default(Vector2Int)));
        }

        [UnityTest]
        public IEnumerator NewBlockingTerrain_StopsMovementBeforeEntry()
        {
            yield return LoadDevelopmentScene();
            AntGameDevelopmentWorldHost host = FindHost();
            yield return new WaitForFixedUpdate();

            Vector2Int destinationCell = FindAcceptedDestination(host, 2, 10);
            Vector2Int antCell = WorldTileCoordinates.WorldPositionToCell(
                host.AutonomousAnt.Position);
            int direction = destinationCell.x > antCell.x ? 1 : -1;
            var blockingCell = new Vector2Int(antCell.x + direction, antCell.y);
            Assert.That(host.ToggleForegroundSolid(blockingCell), Is.True);

            yield return WaitForTerminalState(host.AutonomousAnt, 4f);

            Assert.That(
                host.AutonomousAnt.State,
                Is.EqualTo(AntAutonomousMovementState.Blocked));
            Assert.That(host.AutonomousAnt.Position.x * direction,
                Is.LessThan(blockingCell.x * direction));
        }

        [UnityTest]
        public IEnumerator RemovedGroundSupport_StopsMovementBeforeGap()
        {
            yield return LoadDevelopmentScene();
            AntGameDevelopmentWorldHost host = FindHost();
            yield return new WaitForFixedUpdate();

            Vector2Int destinationCell = FindAcceptedDestination(host, 2, 10);
            Vector2Int antCell = WorldTileCoordinates.WorldPositionToCell(
                host.AutonomousAnt.Position);
            int direction = destinationCell.x > antCell.x ? 1 : -1;
            var supportCell = new Vector2Int(
                antCell.x + direction,
                antCell.y - 1);
            Assert.That(host.ToggleForegroundSolid(supportCell), Is.True);

            yield return WaitForTerminalState(host.AutonomousAnt, 4f);

            Assert.That(
                host.AutonomousAnt.State,
                Is.EqualTo(AntAutonomousMovementState.Blocked));
            Assert.That(host.AutonomousAnt.Position.x * direction,
                Is.LessThan((supportCell.x + 0.5f * direction) * direction));
        }

        private static IEnumerator LoadDevelopmentScene()
        {
            SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
            yield return null;
            yield return new WaitForFixedUpdate();
        }

        private static AntGameDevelopmentWorldHost FindHost()
        {
            AntGameDevelopmentWorldHost host =
                Object.FindFirstObjectByType<AntGameDevelopmentWorldHost>();
            Assert.That(host, Is.Not.Null);
            Assert.That(host.AutonomousAnt, Is.Not.Null);
            return host;
        }

        private static Vector2Int FindAcceptedDestination(
            AntGameDevelopmentWorldHost host,
            int minimumDistance,
            int maximumDistance)
        {
            Vector2Int antCell = WorldTileCoordinates.WorldPositionToCell(
                host.AutonomousAnt.Position);
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

            Assert.Fail("No direct grounded destination was accepted near the spawn.");
            return default;
        }

        private static IEnumerator WaitForTerminalState(
            AntAutonomousMotor ant,
            float timeoutSeconds)
        {
            float deadline = Time.time + timeoutSeconds;
            while (ant.State == AntAutonomousMovementState.Moving &&
                Time.time < deadline)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(Time.time, Is.LessThan(deadline), ant.StatusMessage);
        }
    }
}
