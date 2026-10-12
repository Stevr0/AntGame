using System.Collections;
using AntColony.Development;
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
            Vector2 destination = ant.Route[ant.Route.Count - 1].Position;
            yield return new WaitForFixedUpdate();

            Assert.That(ant.Position, Is.Not.EqualTo(start));
            Assert.That(ant.Position, Is.Not.EqualTo(destination),
                "The grid executor should interpolate, not teleport.");
            yield return WaitForState(ant, GridNavigationMotorState.Arrived, 20f);
            Assert.That(Vector2.Distance(ant.Position, destination), Is.LessThan(0.05f));
            Assert.That(ant.GetComponent<Rigidbody2D>().simulated, Is.False);
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

        private static IEnumerator LoadScene()
        {
            SceneManager.LoadScene("AntGameDevelopment", LoadSceneMode.Single);
            yield return null;
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
    }
}
