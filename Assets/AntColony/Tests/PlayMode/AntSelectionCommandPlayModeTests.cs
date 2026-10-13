using System.Collections;
using System.Linq;
using AntColony.Ants;
using AntColony.Commands;
using AntColony.Development;
using AntColony.Navigation;
using AntColony.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AntColony.Tests.PlayMode
{
    public sealed class AntSelectionCommandPlayModeTests
    {
        [UnityTest]
        public IEnumerator SceneAutomaticallySpawnsThreeSelectableAnts()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();

            Assert.That(host.SelectableGridAnts, Has.Count.EqualTo(3));
            Assert.That(host.SelectionController.SelectableAnts,
                Has.Count.EqualTo(3));
            Assert.That(
                host.SelectableGridAnts.Select(ant => ant.Position).Distinct().Count(),
                Is.EqualTo(3));
            Assert.That(host.SelectionController.SelectedAnt, Is.Null);
            Assert.That(host.SelectionController.SelectableAnts
                .Select(ant => ant.Record.Id).Distinct().Count(), Is.EqualTo(3));
            Assert.That(host.SelectionController.SelectableAnts
                .Count(ant => ant.Record.Caste == AntCaste.Queen), Is.EqualTo(1));
            Assert.That(host.SelectionController.SelectableAnts
                .Count(ant => ant.Record.Caste == AntCaste.Worker), Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator SelectionPreservesIdentityAndDeceasedAntRejectsMovement()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt first = controller.SelectableAnts[0];
            SelectableGridAnt second = controller.SelectableAnts[1];
            AntId firstId = first.Record.Id;
            AntId secondId = second.Record.Id;

            controller.Select(first);
            controller.Select(second);
            controller.Select(first);
            Assert.That(first.Record.Id, Is.EqualTo(firstId));
            Assert.That(second.Record.Id, Is.EqualTo(secondId));

            first.Record.MarkDeceased();
            Assert.That(controller.TryIssueMoveOrder(
                SampleWorldGenerator.GridDirectDestination), Is.False);
            Assert.That(first.Motor.HasPendingRequest, Is.False);
            Assert.That(host.AntRegistry.TryGet(firstId, out AntRecord retained),
                Is.True);
            Assert.That(retained, Is.SameAs(first.Record));
        }

        [UnityTest]
        public IEnumerator SelectionIndicatorFollowsMovingAntAndArrivalCompletesOrder()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt ant = controller.SelectableAnts[0];
            controller.Select(ant);

            Assert.That(controller.TryIssueMoveOrder(
                SampleWorldGenerator.GridDirectDestination), Is.True);
            yield return WaitForState(
                ant.Motor,
                GridNavigationMotorState.FollowingPath,
                5f);
            Assert.That(ant.SelectionIndicator.enabled, Is.True);
            Assert.That(ant.SelectionIndicator.transform.IsChildOf(ant.transform),
                Is.True);
            yield return WaitForState(
                ant.Motor,
                GridNavigationMotorState.Arrived,
                25f);
            ant.RefreshOrderStatus();

            Assert.That(ant.HasActiveOrder, Is.False);
            Assert.That(ant.ControlState, Is.EqualTo("Autonomous / Idle"));
            Assert.That(ant.OrderOutcome, Does.StartWith("Completed"));
            Assert.That(controller.SelectedAnt, Is.SameAs(ant));
            Assert.That(ant.SelectionIndicator.enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator TwoAntsKeepIndependentMovementAndCancellation()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt first = controller.SelectableAnts[0];
            SelectableGridAnt second = controller.SelectableAnts[1];

            controller.Select(first);
            Assert.That(controller.TryIssueMoveOrder(
                SampleWorldGenerator.GridDirectDestination), Is.True);
            controller.Select(second);
            Assert.That(controller.TryIssueMoveOrder(
                SampleWorldGenerator.GridDetourDestination), Is.True);
            yield return new WaitForFixedUpdate();
            Assert.That(first.HasActiveOrder, Is.True);
            Assert.That(second.HasActiveOrder, Is.True);

            Assert.That(controller.CancelSelectedOrder(), Is.True);
            Assert.That(second.Motor.State, Is.EqualTo(GridNavigationMotorState.Idle));
            Assert.That(first.Motor.State, Is.Not.EqualTo(GridNavigationMotorState.Idle));
            yield return WaitForState(
                first.Motor,
                GridNavigationMotorState.Arrived,
                25f);
            first.RefreshOrderStatus();
            Assert.That(first.OrderOutcome, Does.StartWith("Completed"));
            Assert.That(second.OrderOutcome, Does.StartWith("Cancelled"));
        }

        [UnityTest]
        public IEnumerator FailedOrderReturnsAntToAutonomousIdleWithReason()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt ant = controller.SelectableAnts[2];
            controller.Select(ant);

            Assert.That(controller.TryIssueMoveOrder(
                SampleWorldGenerator.GridUnreachableDestination), Is.True);
            yield return WaitForState(
                ant.Motor,
                GridNavigationMotorState.Blocked,
                10f);
            ant.RefreshOrderStatus();

            Assert.That(ant.HasActiveOrder, Is.False);
            Assert.That(ant.ControlState, Is.EqualTo("Autonomous / Idle"));
            Assert.That(ant.OrderOutcome, Does.StartWith("Failed"));
            Assert.That(ant.FailureReason, Is.Not.Empty);
            Assert.That(controller.SelectedAnt, Is.SameAs(ant));
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
            Assert.That(host.SelectionController, Is.Not.Null);
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
