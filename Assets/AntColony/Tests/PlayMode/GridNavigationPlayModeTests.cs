using System.Collections;
using AntColony.Development;
using AntColony.Navigation;
using AntColony.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AntColony.Tests.PlayMode
{
    public sealed class GridNavigationPlayModeTests
    {
        [UnityTest]
        public IEnumerator GridAnt_UsesSmoothIntermediatePositionsAndArrives()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            GridNavigationMotor ant = host.GridNavigationAnt;
            Vector2 start = ant.Position;

            Assert.That(host.TryAssignGridDestination(
                SampleWorldGenerator.GridDirectDestination,
                out string message), Is.True, message);
            yield return WaitForState(
                ant,
                GridNavigationMotorState.FollowingPath,
                5f);
            Vector2 destination = ant.Route[ant.Route.Count - 1].Position;
            yield return new WaitForFixedUpdate();

            Assert.That(ant.Position, Is.Not.EqualTo(start));
            Assert.That(ant.Position, Is.Not.EqualTo(destination),
                "The grid executor should interpolate, not teleport.");
            yield return WaitForState(ant, GridNavigationMotorState.Arrived, 20f);
            Assert.That(Vector2.Distance(ant.Position, destination), Is.LessThan(0.05f));
            Assert.That(ant.GetComponent<Rigidbody2D>().simulated, Is.False);
            Assert.That(ant.HasArrived, Is.True);
            Assert.That(ant.HasFailed, Is.False);
        }

        [UnityTest]
        public IEnumerator PreparedVerticalFixture_IsReachableByGridPath()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            GridNavigationMotor ant = host.GridNavigationAnt;

            Assert.That(host.TryAssignGridDestination(
                host.GripFixtureTop,
                out string message), Is.True, message);
            yield return WaitForState(
                ant,
                GridNavigationMotorState.FollowingPath,
                5f);
            Assert.That(ant.Route.Count, Is.GreaterThan(2));
            yield return WaitForState(ant, GridNavigationMotorState.Arrived, 20f);

            Assert.That(WorldTileCoordinates.WorldPositionToCell(ant.Position),
                Is.EqualTo(host.GripFixtureTop));
        }

        [UnityTest]
        public IEnumerator Falling_UsesRigidbodyThenRecoversToGridAnchor()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            GridNavigationMotor ant = host.GridNavigationAnt;
            Rigidbody2D body = ant.GetComponent<Rigidbody2D>();
            body.position = WorldTileCoordinates.CellToWorldPosition(
                SampleWorldGenerator.GridFallFixture);
            ant.transform.position = body.position;
            float startingY = body.position.y;

            ant.BeginFalling();
            Assert.That(body.simulated, Is.True);
            Assert.That(body.gravityScale, Is.GreaterThan(0f));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(body.linearVelocity.y, Is.LessThan(0f));
            yield return WaitForState(ant, GridNavigationMotorState.Idle, 5f);

            Assert.That(ant.Position.y, Is.LessThan(startingY));
            Assert.That(body.simulated, Is.False);
            Assert.That(body.gravityScale, Is.Zero);
        }

        [UnityTest]
        public IEnumerator MultipleAntsShareSchedulerAndKeepIndependentRequests()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            host.SetGridStressGroupSize(3);
            Assert.That(host.SpawnGridStressGroup(out string message),
                Is.True, message);

            host.StartSimultaneousGridRequests();
            Assert.That(host.GridNavigationAnts.Count, Is.EqualTo(3));
            foreach (GridNavigationMotor item in host.GridNavigationAnts)
            {
                Assert.That(item.NavigationWorld,
                    Is.SameAs(host.NavigationWorldService));
            }
            Assert.That(host.CancelFirstGridRequest(), Is.True);
            yield return new WaitForFixedUpdate();
            yield return WaitForTerminalStates(host, 40f);

            Assert.That(host.GridNavigationAnts[0].State,
                Is.EqualTo(GridNavigationMotorState.Idle));
            Assert.That(host.GridNavigationAnts[1].State,
                Is.EqualTo(GridNavigationMotorState.Arrived));
            Assert.That(host.GridNavigationAnts[2].State,
                Is.EqualTo(GridNavigationMotorState.Arrived));
            Assert.That(host.GridNavigationScheduler.Diagnostics.TotalSubmitted,
                Is.GreaterThanOrEqualTo(3));
        }

        [UnityTest]
        public IEnumerator TerrainMutationReplansActiveGridRoute()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            GridNavigationMotor ant = host.GridNavigationAnt;
            Assert.That(host.TryAssignGridDestination(
                SampleWorldGenerator.GridDetourDestination,
                out string message), Is.True, message);
            yield return WaitForState(
                ant,
                GridNavigationMotorState.FollowingPath,
                5f);

            Vector2Int mutation = new(4, SampleWorldGenerator.MainPassageY);
            Assert.That(host.ToggleForegroundSolid(mutation), Is.True);
            yield return WaitForState(
                ant,
                GridNavigationMotorState.Arrived,
                35f);

            Assert.That(host.GridNavigationScheduler.Diagnostics.CacheInvalidations,
                Is.GreaterThanOrEqualTo(1));
            Assert.That(ant.RetryCount,
                Is.LessThanOrEqualTo(ant.MaximumAutomaticRetries));
        }

        [UnityTest]
        public IEnumerator UnreachableDestinationReachesStableBlockedState()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            GridNavigationMotor ant = host.GridNavigationAnt;

            Assert.That(host.TryAssignGridDestination(
                SampleWorldGenerator.GridUnreachableDestination,
                out string message), Is.True, message);
            yield return WaitForState(
                ant,
                GridNavigationMotorState.Blocked,
                10f);

            Assert.That(ant.HasPendingRequest, Is.False);
            Assert.That(ant.HasFailed, Is.True);
            Assert.That(ant.FailureReason, Is.Not.Empty);
            Assert.That(ant.RetryCount,
                Is.LessThanOrEqualTo(ant.MaximumAutomaticRetries));
        }

        [UnityTest]
        public IEnumerator ReplacingAndCancellingProductionCommandRejectsStaleWork()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            GridNavigationMotor ant = host.GridNavigationAnt;

            Assert.That(ant.MoveTo(
                SampleWorldGenerator.GridDirectDestination), Is.True);
            long firstRequest = ant.PendingRequestId;
            Assert.That(firstRequest, Is.GreaterThan(0));
            Assert.That(ant.MoveTo(
                SampleWorldGenerator.GridDetourDestination), Is.True);
            long replacementRequest = ant.PendingRequestId;
            Assert.That(replacementRequest, Is.Not.EqualTo(firstRequest));

            ant.CancelMovement("Test production command cancellation.");
            Assert.That(ant.State, Is.EqualTo(GridNavigationMotorState.Idle));
            Assert.That(ant.HasPendingRequest, Is.False);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(ant.State, Is.EqualTo(GridNavigationMotorState.Idle),
                "Cancelled stale results must not regain control of the ant.");
        }

        [UnityTest]
        public IEnumerator FallingInterruptsThenResumesStoredDestination()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            GridNavigationMotor ant = host.GridNavigationAnt;
            Rigidbody2D body = ant.GetComponent<Rigidbody2D>();
            body.position = WorldTileCoordinates.CellToWorldPosition(
                SampleWorldGenerator.GridFallFixture + Vector2Int.left);
            ant.transform.position = body.position;

            Assert.That(ant.MoveTo(
                SampleWorldGenerator.GridDirectDestination), Is.True);
            yield return WaitForState(
                ant,
                GridNavigationMotorState.FollowingPath,
                5f);
            body.position = WorldTileCoordinates.CellToWorldPosition(
                SampleWorldGenerator.GridFallFixture);
            ant.transform.position = body.position;
            ant.BeginFalling("Test interruption.");
            yield return WaitForState(
                ant,
                GridNavigationMotorState.Arrived,
                20f);

            Assert.That(body.simulated, Is.False);
            Assert.That(ant.HasPendingRequest, Is.False);
            Assert.That(ant.RetryCount,
                Is.LessThanOrEqualTo(ant.MaximumAutomaticRetries));
        }

        private static IEnumerator LoadScene()
        {
            SceneManager.LoadScene("AntGameDevelopment", LoadSceneMode.Single);
            yield return null;
            PlayModeSimulationTestUtility.Resume();
            yield return new WaitForFixedUpdate();
        }

        private static AntGameDevelopmentWorldHost FindHost()
        {
            AntGameDevelopmentWorldHost host =
                Object.FindFirstObjectByType<AntGameDevelopmentWorldHost>();
            Assert.That(host, Is.Not.Null);
            Assert.That(host.GridNavigationAnt, Is.Not.Null);
            return host;
        }

        private static IEnumerator WaitForState(
            GridNavigationMotor ant,
            GridNavigationMotorState expected,
            float timeout)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (ant.State != expected && Time.realtimeSinceStartup < end)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(ant.State, Is.EqualTo(expected), ant.StatusMessage);
        }

        private static IEnumerator WaitForTerminalStates(
            AntGameDevelopmentWorldHost host,
            float timeout)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (Time.realtimeSinceStartup < end)
            {
                bool complete = true;
                foreach (GridNavigationMotor ant in host.GridNavigationAnts)
                {
                    if (ant.State == GridNavigationMotorState.WaitingForPath ||
                        ant.State == GridNavigationMotorState.FollowingPath ||
                        ant.State == GridNavigationMotorState.Recovering ||
                        ant.State == GridNavigationMotorState.Falling)
                    {
                        complete = false;
                        break;
                    }
                }

                if (complete)
                {
                    yield break;
                }

                yield return new WaitForFixedUpdate();
            }

            Assert.Fail("Grid stress ants did not reach terminal states in time.");
        }
    }
}
