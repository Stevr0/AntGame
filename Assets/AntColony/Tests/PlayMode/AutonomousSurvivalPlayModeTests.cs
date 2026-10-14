using System.Collections;
using AntColony.Commands;
using AntColony.Development;
using AntColony.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AntColony.Tests.PlayMode
{
    public sealed class AutonomousSurvivalPlayModeTests
    {
        [UnityTest]
        public IEnumerator HealthyAnts_RemainAutonomousAndIdle()
        {
            yield return LoadScene();
            AntSelectionCommandController controller = FindHost().SelectionController;
            controller.SetAutonomousSurvivalEnabled(true);
            yield return new WaitForSeconds(1f);
            foreach (SelectableGridAnt ant in controller.SelectableAnts)
                Assert.That(ant.HasActiveOrder, Is.False);
        }

        [UnityTest]
        public IEnumerator HungryAnt_AutonomouslyNavigatesAndEatsPhysicalFood()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt ant = controller.SelectableAnts[0];
            ant.Record.TrySetNeedsForDevelopment(100, 40, 100, 20, out _);
            int foodBefore = TotalQuantity(host, SurvivalResourceKind.Food);
            controller.SetAutonomousSurvivalEnabled(true);
            yield return WaitForCompletedNeed(ant, 30f);
            Assert.That(ant.Record.Needs.Hunger, Is.GreaterThan(40));
            Assert.That(TotalQuantity(host, SurvivalResourceKind.Food),
                Is.EqualTo(foodBefore - 1));
        }

        [UnityTest]
        public IEnumerator ThirstyAndTiredAnts_StartIndependentActions()
        {
            yield return LoadScene();
            AntSelectionCommandController controller = FindHost().SelectionController;
            SelectableGridAnt drinker = controller.SelectableAnts[0];
            SelectableGridAnt sleeper = controller.SelectableAnts[1];
            drinker.Record.TrySetNeedsForDevelopment(100, 100, 40, 20, out _);
            sleeper.Record.TrySetNeedsForDevelopment(100, 100, 100, 70, out _);
            controller.SetAutonomousSurvivalEnabled(true);
            float timeout = Time.time + 3f;
            while ((!drinker.HasActiveOrder || !sleeper.HasActiveOrder) &&
                Time.time < timeout) yield return null;
            Assert.That(drinker.InteractionOrder.Kind,
                Is.EqualTo(SurvivalInteractionKind.Drink));
            Assert.That(sleeper.InteractionOrder.Kind,
                Is.EqualTo(SurvivalInteractionKind.Rest));
        }

        [UnityTest]
        public IEnumerator OrdinaryNeed_DoesNotCancelPlayerMovement()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt ant = controller.SelectableAnts[0];
            ant.Record.TrySetNeedsForDevelopment(100, 40, 100, 20, out _);
            controller.Select(ant);
            Assert.That(controller.TryIssueMoveOrder(
                SampleWorldGenerator.GridDirectDestination), Is.True);
            controller.SetAutonomousSurvivalEnabled(true);
            yield return new WaitForSeconds(1f);
            Assert.That(ant.OrderSource, Is.EqualTo(AntOrderSource.Player));
        }

        [UnityTest]
        public IEnumerator CriticalNeed_SafelyInterruptsPlayerMovement()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt ant = controller.SelectableAnts[0];
            ant.Record.TrySetNeedsForDevelopment(100, 0, 100, 20, out _);
            controller.Select(ant);
            controller.TryIssueMoveOrder(SampleWorldGenerator.GridDirectDestination);
            controller.SetAutonomousSurvivalEnabled(true);
            float timeout = Time.time + 3f;
            while (ant.OrderSource != AntOrderSource.AutonomousSurvival &&
                Time.time < timeout) yield return null;
            Assert.That(ant.InteractionOrder.Kind,
                Is.EqualTo(SurvivalInteractionKind.Eat));
        }

        [UnityTest]
        public IEnumerator DisablingAutonomy_CancelsAutomaticReservation()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt ant = controller.SelectableAnts[0];
            ant.Record.TrySetNeedsForDevelopment(100, 40, 100, 20, out _);
            controller.SetAutonomousSurvivalEnabled(true);
            float timeout = Time.time + 3f;
            while (!ant.HasActiveOrder && Time.time < timeout) yield return null;
            Vector2Int target = ant.InteractionOrder.TargetCell;
            controller.SetAutonomousSurvivalEnabled(false);
            host.SurvivalInteractionWorld.TryGetStorage(
                target, out SurvivalStorageSlot slot);
            Assert.That(slot.ReservedQuantity, Is.Zero);
            Assert.That(ant.HasActiveOrder, Is.False);
        }

        private static IEnumerator WaitForCompletedNeed(
            SelectableGridAnt ant, float timeoutSeconds)
        {
            float timeout = Time.time + timeoutSeconds;
            bool observedOrder = false;
            while (Time.time < timeout)
            {
                observedOrder |= ant.HasActiveOrder;
                if (observedOrder && !ant.HasActiveOrder) yield break;
                yield return null;
            }
            Assert.Fail("Autonomous survival order did not complete before timeout.");
        }

        private static int TotalQuantity(
            AntGameDevelopmentWorldHost host,
            SurvivalResourceKind kind)
        {
            int total = 0;
            foreach (SurvivalStorageSlot slot in host.SurvivalInteractionWorld.StorageSlots)
                if (slot.Kind == kind) total += slot.Quantity;
            return total;
        }

        private static IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync(
                "AntGameDevelopment", LoadSceneMode.Single);
            yield return null;
            PlayModeSimulationTestUtility.Resume();
        }

        private static AntGameDevelopmentWorldHost FindHost() =>
            Object.FindFirstObjectByType<AntGameDevelopmentWorldHost>();
    }
}
