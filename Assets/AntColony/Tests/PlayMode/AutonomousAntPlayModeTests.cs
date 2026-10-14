using System.Collections;
using System.Collections.Generic;
using AntColony.Development;
using AntColony.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AntColony.Tests.PlayMode
{
    /// <summary>Physics-backed checks for bounded ground, grip and falling motion.</summary>
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
            var unsupportedCell = supportCell + Vector2Int.up;
            Assert.That(host.SetNonGrippableBackground(unsupportedCell), Is.True);
            Assert.That(host.ToggleForegroundSolid(supportCell), Is.True);

            yield return WaitForTerminalState(host.AutonomousAnt, 4f);

            Assert.That(
                host.AutonomousAnt.State,
                Is.EqualTo(AntAutonomousMovementState.Blocked));
            Assert.That(host.AutonomousAnt.Position.x * direction,
                Is.LessThan((supportCell.x + 0.5f * direction) * direction));
        }

        [UnityTest]
        public IEnumerator PreparedFixture_AllowsVerticalMovementAndSuppressesGravity()
        {
            yield return LoadDevelopmentScene();
            AntGameDevelopmentWorldHost host = FindHost();
            yield return new WaitForFixedUpdate();

            Assert.That(host.TryAssignAutonomousDestination(
                host.GripFixtureTop,
                out string upwardMessage), Is.True, upwardMessage);
            yield return WaitForTerminalState(host.AutonomousAnt, 8f);

            Rigidbody2D body = host.AutonomousAnt.GetComponent<Rigidbody2D>();
            Assert.That(host.AutonomousAnt.State,
                Is.EqualTo(AntAutonomousMovementState.Arrived));
            Assert.That(host.AutonomousAnt.LocomotionState,
                Is.EqualTo(AntAutonomousLocomotionState.Gripping));
            Assert.That(body.gravityScale, Is.Zero);
            Assert.That(host.AutonomousAnt.EffectiveMovementSpeed,
                Is.EqualTo(host.AutonomousAnt.MovementSpeed * 1.25f)
                    .Within(0.001f),
                "Prepared Background should apply the provisional speed bonus.");

            var diagonalTarget = host.GripFixtureTop +
                new Vector2Int(2, -2);
            Assert.That(host.TryAssignAutonomousDestination(
                diagonalTarget,
                out string diagonalMessage), Is.True, diagonalMessage);
            yield return WaitForTerminalState(host.AutonomousAnt, 5f);
            Assert.That(host.AutonomousAnt.Position.x,
                Is.EqualTo(diagonalTarget.x).Within(0.1f));
            Assert.That(host.AutonomousAnt.Position.y,
                Is.EqualTo(diagonalTarget.y).Within(0.1f));

            Assert.That(host.TryAssignAutonomousDestination(
                host.GripFixtureTop,
                out string returnMessage), Is.True, returnMessage);
            yield return WaitForTerminalState(host.AutonomousAnt, 5f);

            var floorCell = host.GripFixtureBottom + Vector2Int.down;
            Assert.That(host.TryAssignAutonomousDestination(
                floorCell,
                out string downwardMessage), Is.True, downwardMessage);
            yield return WaitForTerminalState(host.AutonomousAnt, 8f);
            yield return new WaitForFixedUpdate();

            Assert.That(host.AutonomousAnt.LocomotionState,
                Is.EqualTo(AntAutonomousLocomotionState.Grounded));
        }

        [UnityTest]
        public IEnumerator LostGrip_TriggersFallThenPreparedCellRegainsGrip()
        {
            yield return LoadDevelopmentScene();
            AntGameDevelopmentWorldHost host = FindHost();
            yield return new WaitForFixedUpdate();
            Assert.That(host.TryAssignAutonomousDestination(
                host.GripFixtureTop,
                out string message), Is.True, message);
            yield return WaitForTerminalState(host.AutonomousAnt, 8f);

            float startingY = host.AutonomousAnt.Position.y;
            Assert.That(host.TogglePreparedBackground(host.GripFixtureTop), Is.True);
            Assert.That(host.AutonomousAnt.LocomotionState,
                Is.EqualTo(AntAutonomousLocomotionState.Falling));
            Assert.That(host.TryAssignAutonomousDestination(
                host.GripFixtureTop,
                out _), Is.False,
                "An unsupported falling ant must not accept an upward command.");

            yield return WaitForLocomotionState(
                host.AutonomousAnt,
                AntAutonomousLocomotionState.Gripping,
                3f);
            Assert.That(host.AutonomousAnt.Position.y, Is.LessThan(startingY));
            Assert.That(
                host.AutonomousAnt.GetComponent<Rigidbody2D>().gravityScale,
                Is.Zero);

            for (int index = 0; index < 5; index++)
            {
                yield return new WaitForFixedUpdate();
                Assert.That(host.AutonomousAnt.LocomotionState,
                    Is.EqualTo(AntAutonomousLocomotionState.Gripping));
            }
        }

        [UnityTest]
        public IEnumerator RemovedFloor_UsesGravityAndLandsOnLowerTerrain()
        {
            yield return LoadDevelopmentScene();
            AntGameDevelopmentWorldHost host = FindHost();
            yield return new WaitForFixedUpdate();

            Vector2Int antCell = WorldTileCoordinates.WorldPositionToCell(
                host.AutonomousAnt.Position);
            Assert.That(host.SetNonGrippableBackground(antCell), Is.True);
            var supportCell = antCell + Vector2Int.down;
            float startingY = host.AutonomousAnt.Position.y;
            Assert.That(host.ToggleForegroundSolid(supportCell), Is.True);
            Assert.That(host.SetNonGrippableBackground(supportCell), Is.True);
            Assert.That(host.AutonomousAnt.LocomotionState,
                Is.EqualTo(AntAutonomousLocomotionState.Falling));

            yield return new WaitForFixedUpdate();
            float firstFallingVelocity =
                host.AutonomousAnt.GetComponent<Rigidbody2D>().linearVelocity.y;
            yield return new WaitForFixedUpdate();
            float secondFallingVelocity =
                host.AutonomousAnt.GetComponent<Rigidbody2D>().linearVelocity.y;
            Assert.That(firstFallingVelocity, Is.LessThan(0f));
            Assert.That(secondFallingVelocity, Is.LessThan(firstFallingVelocity),
                "Unsupported motion should accelerate under Rigidbody2D gravity.");

            yield return WaitForLocomotionState(
                host.AutonomousAnt,
                AntAutonomousLocomotionState.Grounded,
                4f,
                requireDifferentInitialState: true);

            Assert.That(host.AutonomousAnt.Position.y, Is.LessThan(startingY - 0.5f));
            Assert.That(
                host.AutonomousAnt.GetComponent<Rigidbody2D>().gravityScale,
                Is.Zero);

            float landedY = host.AutonomousAnt.Position.y;
            for (int index = 0; index < 5; index++)
            {
                yield return new WaitForFixedUpdate();
            }
            Assert.That(host.AutonomousAnt.LocomotionState,
                Is.EqualTo(AntAutonomousLocomotionState.Grounded));
            Assert.That(host.AutonomousAnt.Position.y,
                Is.EqualTo(landedY).Within(0.03f));
        }

        private static IEnumerator LoadDevelopmentScene()
        {
            SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
            yield return null;
            PlayModeSimulationTestUtility.Resume();
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
            string lastMessage = string.Empty;
            var rejected = new List<string>();
            for (int distance = minimumDistance; distance <= maximumDistance; distance++)
            {
                foreach (int sign in new[] { 1, -1 })
                {
                    var candidate = new Vector2Int(
                        antCell.x + sign * distance,
                        antCell.y);
                    if (host.TryAssignAutonomousDestination(
                        candidate,
                        out lastMessage))
                    {
                        return candidate;
                    }

                    rejected.Add($"{candidate}: {lastMessage}");
                }
            }

            Assert.Fail(
                $"No direct grounded destination was accepted near {antCell}. " +
                $"Position={host.AutonomousAnt.Position}, " +
                $"Locomotion={host.AutonomousAnt.LocomotionState}, " +
                $"Background={host.AutonomousAnt.BackgroundMaterialId}, " +
                $"H/U/D=" +
                $"{host.AutonomousAnt.LatestCapability.AllowsHorizontalMovement}/" +
                $"{host.AutonomousAnt.LatestCapability.AllowsUpwardMovement}/" +
                $"{host.AutonomousAnt.LatestCapability.AllowsDownwardMovement}. " +
                string.Join(" | ", rejected));
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

        private static IEnumerator WaitForLocomotionState(
            AntAutonomousMotor ant,
            AntAutonomousLocomotionState expected,
            float timeoutSeconds,
            bool requireDifferentInitialState = false)
        {
            float deadline = Time.time + timeoutSeconds;
            bool sawDifferentState = !requireDifferentInitialState ||
                ant.LocomotionState != expected;
            while (Time.time < deadline)
            {
                if (ant.LocomotionState != expected)
                {
                    sawDifferentState = true;
                }
                else if (sawDifferentState)
                {
                    yield break;
                }

                yield return new WaitForFixedUpdate();
            }

            Assert.Fail(
                $"Ant did not reach {expected}. Current state: " +
                $"{ant.LocomotionState}; {ant.StatusMessage}");
        }
    }
}
