using System.Linq;
using AntColony.Ants;
using AntColony.Commands;
using AntColony.Development;
using AntColony.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AntColony.Tests.EditMode
{
    public sealed class AutonomousSurvivalTests
    {
        private const string ScenePath =
            "Assets/AntColony/Scenes/AntGameDevelopment.unity";
        private readonly AutonomousSurvivalDecisionService service = new();

        [TearDown]
        public void TearDown() =>
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [Test]
        public void HealthyAnt_HasNoSurvivalDecision()
        {
            Assert.That(service.Evaluate(CreateRecord().Needs).HasDecision, Is.False);
        }

        [TestCase(40, 100, 20, SurvivalInteractionKind.Eat)]
        [TestCase(100, 40, 20, SurvivalInteractionKind.Drink)]
        [TestCase(100, 100, 70, SurvivalInteractionKind.Rest)]
        public void OrdinaryNeed_SelectsExpectedInteraction(
            double hunger, double thirst, double fatigue,
            SurvivalInteractionKind expected)
        {
            AntRecord ant = CreateRecord(hunger, thirst, fatigue);
            Assert.That(service.Evaluate(ant.Needs).Kind, Is.EqualTo(expected));
        }

        [TestCase(40.01, 100, 20)]
        [TestCase(100, 40.01, 20)]
        [TestCase(100, 100, 69.99)]
        public void ValueJustOutsideInterventionBoundary_DoesNotTrigger(
            double hunger, double thirst, double fatigue)
        {
            Assert.That(service.Evaluate(
                CreateRecord(hunger, thirst, fatigue).Needs).HasDecision, Is.False);
        }

        [Test]
        public void CriticalThirst_OutranksCriticalHungerAndFatigue()
        {
            AutonomousSurvivalDecision decision =
                service.Evaluate(CreateRecord(0, 0, 100).Needs);
            Assert.That(decision.Kind, Is.EqualTo(SurvivalInteractionKind.Drink));
            Assert.That(decision.IsCritical, Is.True);
        }

        [Test]
        public void CriticalHunger_OutranksCriticalFatigue()
        {
            Assert.That(service.Evaluate(CreateRecord(0, 100, 100).Needs).Kind,
                Is.EqualTo(SurvivalInteractionKind.Eat));
        }

        [Test]
        public void OrdinaryGreatestNormalisedUrgencyWins()
        {
            Assert.That(service.Evaluate(CreateRecord(35, 20, 70).Needs).Kind,
                Is.EqualTo(SurvivalInteractionKind.Drink));
        }

        [Test]
        public void EqualOrdinaryUrgency_UsesDrinkBeforeEatTieBreaker()
        {
            Assert.That(service.Evaluate(CreateRecord(25, 25, 20).Needs).Kind,
                Is.EqualTo(SurvivalInteractionKind.Drink));
        }

        [Test]
        public void ClosestReachableTarget_SkipsSurroundedStorage()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Vector2Int start = WorldTileCoordinates.WorldPositionToCell(
                host.SelectableGridAnts[0].Position);
            Assert.That(service.TryChooseTarget(
                host.SurvivalInteractionWorld, host.GridNavigationMap, start,
                SurvivalInteractionKind.Eat, out Vector2Int target, out _), Is.True);
            Assert.That(target, Is.Not.EqualTo(host.SurvivalFixtureCells[7]));
        }

        [Test]
        public void FullyReservedFood_IsExcluded()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            foreach (SurvivalStorageSlot slot in host.SurvivalInteractionWorld.StorageSlots
                .Where(slot => slot.Kind == SurvivalResourceKind.Food))
            {
                host.SurvivalInteractionWorld.TryReserveResource(
                    slot.Cell, slot.Kind, $"owner-{slot.Cell}", slot.Quantity, out _);
            }
            Vector2Int start = WorldTileCoordinates.WorldPositionToCell(
                host.SelectableGridAnts[0].Position);
            Assert.That(service.TryChooseTarget(
                host.SurvivalInteractionWorld, host.GridNavigationMap, start,
                SurvivalInteractionKind.Eat, out _, out string error), Is.False);
            Assert.That(error, Does.Contain("No reachable available"));
        }

        [Test]
        public void OccupiedSleepingPosition_IsSkipped()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Vector2Int occupied = host.SurvivalFixtureCells[4];
            host.SurvivalInteractionWorld.TryReserveSleeping(occupied, "other", out _);
            Vector2Int start = WorldTileCoordinates.WorldPositionToCell(
                host.SelectableGridAnts[0].Position);
            Assert.That(service.TryChooseTarget(
                host.SurvivalInteractionWorld, host.GridNavigationMap, start,
                SurvivalInteractionKind.Rest, out Vector2Int target, out _), Is.True);
            Assert.That(target, Is.Not.EqualTo(occupied));
        }

        [Test]
        public void OrdinaryNeed_DoesNotInterruptPlayerMove()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt ant = controller.SelectableAnts[0];
            ant.Record.TrySetNeedsForDevelopment(100, 40, 100, 20, out _);
            controller.Select(ant);
            Assert.That(controller.TryIssueMoveOrder(
                SampleWorldGenerator.GridDirectDestination), Is.True);
            controller.SetAutonomousSurvivalEnabled(true);
            Assert.That(controller.EvaluateAutonomousSurvivalNow(ant, 1d), Is.False);
            Assert.That(ant.OrderSource, Is.EqualTo(AntOrderSource.Player));
        }

        [Test]
        public void CriticalNeed_InterruptsPlayerMoveOnlyAfterFindingTarget()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt ant = controller.SelectableAnts[0];
            ant.Record.TrySetNeedsForDevelopment(100, 0, 100, 20, out _);
            controller.Select(ant);
            controller.TryIssueMoveOrder(SampleWorldGenerator.GridDirectDestination);
            controller.SetAutonomousSurvivalEnabled(true);
            Assert.That(controller.EvaluateAutonomousSurvivalNow(ant, 1d), Is.True);
            Assert.That(ant.OrderSource,
                Is.EqualTo(AntOrderSource.AutonomousSurvival));
            Assert.That(ant.InteractionOrder.Kind,
                Is.EqualTo(SurvivalInteractionKind.Eat));
        }

        [Test]
        public void CriticalNeedWithoutSupply_DoesNotDiscardPlayerOrder()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            foreach (SurvivalStorageSlot slot in host.SurvivalInteractionWorld.StorageSlots
                .Where(slot => slot.Kind == SurvivalResourceKind.Food))
                host.SurvivalInteractionWorld.TryReserveResource(
                    slot.Cell, slot.Kind, $"owner-{slot.Cell}", slot.Quantity, out _);
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt ant = controller.SelectableAnts[0];
            ant.Record.TrySetNeedsForDevelopment(100, 0, 100, 20, out _);
            controller.Select(ant);
            controller.TryIssueMoveOrder(SampleWorldGenerator.GridDirectDestination);
            controller.SetAutonomousSurvivalEnabled(true);
            Assert.That(controller.EvaluateAutonomousSurvivalNow(ant, 1d), Is.False);
            Assert.That(ant.OrderSource, Is.EqualTo(AntOrderSource.Player));
        }

        [Test]
        public void SelectingAnotherAnt_DoesNotStopAutonomousOrder()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt mara = controller.SelectableAnts[0];
            mara.Record.TrySetNeedsForDevelopment(100, 40, 100, 20, out _);
            controller.SetAutonomousSurvivalEnabled(true);
            Assert.That(controller.EvaluateAutonomousSurvivalNow(mara, 1d), Is.True);
            controller.Select(controller.SelectableAnts[1]);
            Assert.That(mara.HasActiveOrder, Is.True);
        }

        [Test]
        public void TwoAnts_CanStartIndependentSurvivalOrders()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt first = controller.SelectableAnts[0];
            SelectableGridAnt second = controller.SelectableAnts[1];
            first.Record.TrySetNeedsForDevelopment(100, 40, 100, 20, out _);
            second.Record.TrySetNeedsForDevelopment(100, 100, 40, 20, out _);
            controller.SetAutonomousSurvivalEnabled(true);
            Assert.That(controller.EvaluateAutonomousSurvivalNow(first, 1d), Is.True);
            Assert.That(controller.EvaluateAutonomousSurvivalNow(second, 1d), Is.True,
                controller.GetAutonomousSurvivalState(second).FailureReason);
            Assert.That(first.InteractionOrder.Kind, Is.EqualTo(SurvivalInteractionKind.Eat));
            Assert.That(second.InteractionOrder.Kind, Is.EqualTo(SurvivalInteractionKind.Drink));
        }

        [Test]
        public void OrdinaryNeedChange_DoesNotReplaceActiveSurvivalOrder()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt ant = controller.SelectableAnts[0];
            ant.Record.TrySetNeedsForDevelopment(100, 40, 100, 20, out _);
            controller.SetAutonomousSurvivalEnabled(true);
            Assert.That(controller.EvaluateAutonomousSurvivalNow(ant, 1d), Is.True);
            Assert.That(ant.InteractionOrder.Kind, Is.EqualTo(SurvivalInteractionKind.Eat));

            ant.Record.TrySetNeedsForDevelopment(100, 40, 40, 20, out _);
            Assert.That(controller.EvaluateAutonomousSurvivalNow(ant, 2d), Is.False);
            Assert.That(ant.InteractionOrder.Kind, Is.EqualTo(SurvivalInteractionKind.Eat));
        }

        [Test]
        public void CriticalThirst_ReplacesActiveOrdinaryEatOrder()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt ant = controller.SelectableAnts[0];
            ant.Record.TrySetNeedsForDevelopment(100, 40, 100, 20, out _);
            controller.SetAutonomousSurvivalEnabled(true);
            Assert.That(controller.EvaluateAutonomousSurvivalNow(ant, 1d), Is.True);
            Assert.That(ant.InteractionOrder.Kind, Is.EqualTo(SurvivalInteractionKind.Eat));

            ant.Record.TrySetNeedsForDevelopment(100, 40, 0, 20, out _);
            Assert.That(controller.EvaluateAutonomousSurvivalNow(ant, 2d), Is.True);
            Assert.That(ant.InteractionOrder.Kind, Is.EqualTo(SurvivalInteractionKind.Drink));
            Assert.That(controller.GetAutonomousSurvivalState(ant).IntentIsCritical, Is.True);
        }

        [Test]
        public void RemovedTargetFunction_FailsSafelyAndBacksOff()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt ant = controller.SelectableAnts[0];
            ant.Record.TrySetNeedsForDevelopment(100, 40, 100, 20, out _);
            controller.SetAutonomousSurvivalEnabled(true);
            Assert.That(controller.EvaluateAutonomousSurvivalNow(ant, 1d), Is.True);

            SurvivalInteractionOrder order = ant.InteractionOrder;
            host.WorldData.GetCell(order.ApproachCell, out WorldTileCellData ordinary);
            host.WorldData.SetBackgroundMaterial(
                order.TargetCell, ordinary.BackgroundMaterialId);
            Assert.That(order.TryBeginInteraction(order.ApproachCell), Is.False);
            ant.CancelOrder(order.FailureReason);

            Assert.That(controller.EvaluateAutonomousSurvivalNow(ant, 2d), Is.False);
            AutonomousSurvivalAgentState state =
                controller.GetAutonomousSurvivalState(ant);
            Assert.That(state.ConsecutiveFailures, Is.EqualTo(1));
            Assert.That(state.NextEvaluationTime, Is.EqualTo(4d));
            Assert.That(state.FailureReason, Is.Not.Empty);
        }

        [Test]
        public void MissingSupply_UsesBoundedExponentialBackoff()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            foreach (SurvivalStorageSlot slot in host.SurvivalInteractionWorld.StorageSlots
                .Where(slot => slot.Kind == SurvivalResourceKind.Water))
                host.SurvivalInteractionWorld.TryReserveResource(
                    slot.Cell, slot.Kind, $"owner-{slot.Cell}", slot.Quantity, out _);
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt ant = controller.SelectableAnts[0];
            ant.Record.TrySetNeedsForDevelopment(100, 100, 40, 20, out _);
            controller.SetAutonomousSurvivalEnabled(true);
            controller.EvaluateAutonomousSurvivalNow(ant, 10d);
            AutonomousSurvivalAgentState state = controller.GetAutonomousSurvivalState(ant);
            Assert.That(state.NextEvaluationTime, Is.EqualTo(12d));
            Assert.That(controller.EvaluateAutonomousSurvivalNow(ant, 10.5d), Is.False);
            Assert.That(state.ConsecutiveFailures, Is.EqualTo(1));
        }

        [TestCase(AntLifecycleStatus.Inactive)]
        [TestCase(AntLifecycleStatus.Deceased)]
        public void NonActiveAnt_DoesNotIssueAutonomousOrder(AntLifecycleStatus status)
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt ant = controller.SelectableAnts[0];
            ant.Record.TrySetNeedsForDevelopment(100, 0, 100, 20, out _);
            if (status == AntLifecycleStatus.Inactive) ant.Record.MarkInactive();
            else ant.Record.MarkDeceased();
            controller.SetAutonomousSurvivalEnabled(true);
            Assert.That(controller.EvaluateAutonomousSurvivalNow(ant, 1d), Is.False);
            Assert.That(ant.HasActiveOrder, Is.False);
        }

        [Test]
        public void DisablingAutonomy_CancelsOrderAndReleasesReservation()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt ant = controller.SelectableAnts[0];
            ant.Record.TrySetNeedsForDevelopment(100, 40, 100, 20, out _);
            controller.SetAutonomousSurvivalEnabled(true);
            controller.EvaluateAutonomousSurvivalNow(ant, 1d);
            Vector2Int target = ant.InteractionOrder.TargetCell;
            controller.SetAutonomousSurvivalEnabled(false);
            host.SurvivalInteractionWorld.TryGetStorage(
                target, out SurvivalStorageSlot slot);
            Assert.That(slot.ReservedQuantity, Is.Zero);
            Assert.That(ant.HasActiveOrder, Is.False);
        }

        [Test]
        public void WorldReset_ClearsAutonomousState()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt ant = controller.SelectableAnts[0];
            ant.Record.TrySetNeedsForDevelopment(100, 40, 100, 20, out _);
            controller.SetAutonomousSurvivalEnabled(true);
            controller.EvaluateAutonomousSurvivalNow(ant, 1d);
            Assert.That(host.RebuildWorld(), Is.True);
            Assert.That(host.SpawnOrResetSelectableGridAnts(out string message), Is.True, message);
            Assert.That(controller.SelectableAnts.All(item => !item.HasActiveOrder), Is.True);
            Assert.That(controller.SelectableAnts.All(item =>
                controller.GetAutonomousSurvivalState(item).Intent == null), Is.True);
        }

        private static AntRecord CreateRecord(
            double hunger = 100d,
            double thirst = 100d,
            double fatigue = 20d)
        {
            var record = new AntRecord(
                new AntId($"ant-{hunger}-{thirst}-{fatigue}"),
                "Test Ant",
                new ColonyId("test-colony"),
                AntCaste.Worker,
                new AntAttributes(5, 5, 5));
            record.TrySetNeedsForDevelopment(100, hunger, thirst, fatigue, out _);
            return record;
        }

        private static AntGameDevelopmentWorldHost OpenHost()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AntGameDevelopmentWorldHost host = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<AntGameDevelopmentWorldHost>(true))
                .Single();
            Assert.That(host.RebuildWorld(), Is.True);
            Assert.That(host.SpawnOrResetSelectableGridAnts(out string message), Is.True, message);
            return host;
        }
    }
}
