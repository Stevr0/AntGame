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
    public sealed class ExcavationJobTests
    {
        private const string ScenePath =
            "Assets/AntColony/Scenes/AntGameDevelopment.unity";

        [TearDown]
        public void TearDown() =>
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [Test]
        public void EligibleSolidCanBeDesignatedAndDuplicateIsRejected()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Assert.That(host.ExcavationDesignations.TryDesignate(
                AntGameDevelopmentWorldHost.ExcavationSoilFixture,
                out ExcavationDesignation designation, out _), Is.True);
            Assert.That(designation.Job.Category, Is.EqualTo(AntWorkCategory.Excavation));
            Assert.That(host.ExcavationDesignations.TryDesignate(
                designation.Cell, out _, out _), Is.False);
        }

        [Test]
        public void BackgroundOnlyAndNonExcavatableCellsAreRejected()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Assert.That(host.ExcavationDesignations.TryDesignate(
                new Vector2Int(-10, SampleWorldGenerator.MainPassageY),
                out _, out _), Is.False);
            Assert.That(host.ExcavationDesignations.TryDesignate(
                AntGameDevelopmentWorldHost.NonExcavatableFixture,
                out _, out _), Is.False);
        }

        [Test]
        public void AreaDesignationCreatesExpectedJobsAndCancellationIsTerminal()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            int added = host.ExcavationDesignations.DesignateArea(
                AntGameDevelopmentWorldHost.ExcavationAreaMinimum,
                AntGameDevelopmentWorldHost.ExcavationAreaMaximum, out _);
            Assert.That(added, Is.EqualTo(3));
            Assert.That(host.ExcavationDesignations.TryCancel(
                AntGameDevelopmentWorldHost.ExcavationAreaMinimum, out _), Is.True);
            Assert.That(host.ExcavationDesignations.Designations.Count(d =>
                d.State == ExcavationDesignationState.Cancelled), Is.EqualTo(1));
        }

        [Test]
        public void CancelledPartialExcavationReleasesAndCanBeDesignatedAgain()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Vector2Int cell = AntGameDevelopmentWorldHost.ExcavationSoilFixture;
            Assert.That(host.ExcavationDesignations.TryDesignate(cell,
                out ExcavationDesignation first, out string error), Is.True, error);
            AntRecord worker = CreateRecord("cancel-redesignate");
            Assert.That(host.ColonyJobRegistry.TryReserve(first.Job.Id, worker.Id,
                out error), Is.True, error);
            Assert.That(host.ColonyJobRegistry.TryBegin(first.Job.Id, worker.Id,
                out error), Is.True, error);
            Assert.That(host.ColonyJobRegistry.TryAddProgress(first.Job.Id, worker.Id,
                0.25d, out _, out error), Is.True, error);

            Assert.That(host.ExcavationDesignations.TryCancel(cell, out error),
                Is.True, error);
            Assert.That(first.State, Is.EqualTo(ExcavationDesignationState.Cancelled));
            Assert.That(first.Job.ReservationOwner.IsValid, Is.False);
            Assert.That(first.Job.Progress, Is.EqualTo(0.25d).Within(0.001d));
            Assert.That(host.ExcavationDesignations.TryDesignate(cell,
                out ExcavationDesignation replacement, out error), Is.True, error);
            Assert.That(replacement, Is.Not.SameAs(first));
            Assert.That(replacement.State, Is.EqualTo(ExcavationDesignationState.Pending));
            Assert.That(replacement.Job.Progress, Is.Zero);
        }

        [Test]
        public void ExcavationCanRepeatDesignationCancellationWithoutLimit()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Vector2Int cell = AntGameDevelopmentWorldHost.ExcavationStoneFixture;
            for (int cycle = 0; cycle < 6; cycle++)
            {
                Assert.That(host.ExcavationDesignations.TryDesignate(cell,
                    out ExcavationDesignation designation, out string error),
                    Is.True, $"Cycle {cycle + 1}: {error}");
                Assert.That(designation.State,
                    Is.EqualTo(ExcavationDesignationState.Pending));
                Assert.That(host.ExcavationDesignations.TryCancel(cell, out error),
                    Is.True, $"Cycle {cycle + 1}: {error}");
            }
        }

        [Test]
        public void CompletedExcavationCannotBeCancelledOrDesignatedAgain()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Vector2Int cell = AntGameDevelopmentWorldHost.ExcavationSoilFixture;
            host.ExcavationDesignations.TryDesignate(cell,
                out ExcavationDesignation designation, out _);
            AntRecord worker = CreateRecord("completed-terminal");
            host.ColonyJobRegistry.TryReserve(designation.Job.Id, worker.Id, out _);
            host.ColonyJobRegistry.TryBegin(designation.Job.Id, worker.Id, out _);
            host.ColonyJobRegistry.TryAddProgress(designation.Job.Id, worker.Id,
                1d, out _, out _);

            Assert.That(host.ExcavationDesignations.TryCancel(cell, out _), Is.False);
            Assert.That(host.ExcavationDesignations.TryDesignate(cell,
                out _, out _), Is.False);
            Assert.That(designation.State, Is.EqualTo(ExcavationDesignationState.Completed));
        }

        [TestCase(ExcavatedMaterialType.Soil)]
        [TestCase(ExcavatedMaterialType.Stone)]
        public void CompletionProducesMaterialAndPreservesBackground(
            ExcavatedMaterialType material)
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Vector2Int cell = material == ExcavatedMaterialType.Soil
                ? AntGameDevelopmentWorldHost.ExcavationSoilFixture
                : AntGameDevelopmentWorldHost.ExcavationStoneFixture;
            host.WorldData.GetCell(cell, out WorldTileCellData before);
            Assert.That(host.ExcavationTransaction.TryCommit(
                $"test.{material}", cell, before.SolidMaterialId,
                out WorldMaterialDrop drop, out string error), Is.True, error);
            host.WorldData.GetCell(cell, out WorldTileCellData after);
            Assert.That(after.HasSolidTile, Is.False);
            Assert.That(after.BackgroundMaterialId, Is.EqualTo(before.BackgroundMaterialId));
            Assert.That(drop.Material, Is.EqualTo(material));
            Assert.That(drop.Quantity, Is.EqualTo(1));
        }

        [Test]
        public void FailedDropCreationLeavesSolidAndRetryCannotDuplicate()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            int initialDropCount = host.MaterialDrops.Count;
            Vector2Int cell = AntGameDevelopmentWorldHost.ExcavationSoilFixture;
            host.WorldData.GetCell(cell, out WorldTileCellData before);
            host.MaterialDrops.RejectNextCreationForTesting = true;
            Assert.That(host.ExcavationTransaction.TryCommit(
                "stable-key", cell, before.SolidMaterialId, out _, out _), Is.False);
            host.WorldData.GetCell(cell, out WorldTileCellData afterFailure);
            Assert.That(afterFailure.HasSolidTile, Is.True);
            Assert.That(host.ExcavationTransaction.TryCommit(
                "stable-key", cell, before.SolidMaterialId, out _, out _), Is.True);
            Assert.That(host.ExcavationTransaction.TryCommit(
                "stable-key", cell, before.SolidMaterialId, out _, out _), Is.False);
            Assert.That(host.MaterialDrops.Count, Is.EqualTo(initialDropCount + 1));
        }

        [Test]
        public void SuccessfulCommitInvalidatesOnlyThroughTerrainRevision()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Vector2Int cell = AntGameDevelopmentWorldHost.ExcavationSoilFixture;
            int revision = host.GridNavigationMap.Revision;
            host.WorldData.GetCell(cell, out WorldTileCellData before);
            Assert.That(host.ExcavationTransaction.TryCommit(
                "revision", cell, before.SolidMaterialId, out _, out _), Is.True);
            Assert.That(host.GridNavigationMap.Revision, Is.GreaterThan(revision));
        }

        [Test]
        public void InaccessibleDesignationRemainsAndCanBecomeReachable()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Vector2Int blocked =
                AntGameDevelopmentWorldHost.ExcavationInitiallyBlockedFixture;
            Assert.That(host.ExcavationDesignations.TryDesignate(
                blocked, out ExcavationDesignation designation, out _), Is.True);
            Vector2Int start = new(-10, SampleWorldGenerator.MainPassageY);
            Assert.That(SurvivalInteractionOrder.TryMeasureApproach(
                host.GridNavigationMap, start, blocked, out _, out _, out _), Is.False);
            Vector2Int opening = AntGameDevelopmentWorldHost.ExcavationOpeningFixture;
            host.WorldData.GetCell(opening, out WorldTileCellData openingData);
            Assert.That(host.ExcavationTransaction.TryCommit(
                "opening", opening, openingData.SolidMaterialId, out _, out _), Is.True);
            Assert.That(SurvivalInteractionOrder.TryMeasureApproach(
                host.GridNavigationMap, start, blocked, out _, out _, out _), Is.True);
            Assert.That(designation.State, Is.EqualTo(ExcavationDesignationState.Pending));
        }

        [Test]
        public void OrderProgressAwardsXpAndInterruptionPreservesProgress()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntRecord ant = CreateRecord("worker");
            ExcavationJobOrder order = CreateOrder(host, ant,
                AntGameDevelopmentWorldHost.ExcavationSoilFixture);
            int dropsBeforePartialWork = host.MaterialDrops.Count;
            Assert.That(order.TryBegin(order.ApproachCell), Is.True);
            order.Advance(1d, 1d);
            Assert.That(order.Job.Progress, Is.EqualTo(0.25d).Within(0.001d));
            Assert.That(host.MaterialDrops.Count, Is.EqualTo(dropsBeforePartialWork));
            Assert.That(ant.Skills.Get(AntSkillId.Excavation).TotalExperience,
                Is.EqualTo(5d).Within(0.001d));
            order.Cancel("test interruption");
            Assert.That(order.Job.Status, Is.EqualTo(ColonyJobStatus.Paused));
            Assert.That(order.Job.Progress, Is.GreaterThan(0d));
        }

        [Test]
        public void RestedAntMakesMoreExcavationProgress()
        {
            AntGameDevelopmentWorldHost normalHost = OpenHost();
            AntRecord normal = CreateRecord("normal");
            ExcavationJobOrder normalOrder = CreateOrder(normalHost, normal,
                AntGameDevelopmentWorldHost.ExcavationSoilFixture);
            normalOrder.TryBegin(normalOrder.ApproachCell);
            normalOrder.Advance(1d, 1d);
            double normalProgress = normalOrder.Job.Progress;

            AntGameDevelopmentWorldHost restedHost = OpenHost();
            AntRecord rested = CreateRecord("rested");
            rested.TryRest(0.1d, out _);
            ExcavationJobOrder restedOrder = CreateOrder(restedHost, rested,
                AntGameDevelopmentWorldHost.ExcavationSoilFixture);
            restedOrder.TryBegin(restedOrder.ApproachCell);
            restedOrder.Advance(1d, 1d);
            Assert.That(restedOrder.Job.Progress, Is.GreaterThan(normalProgress));
        }

        [Test]
        public void WorldResetClearsRuntimeChangesAndRestoresAuthoredFixtures()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Vector2Int cell = AntGameDevelopmentWorldHost.ExcavationSoilFixture;
            host.ExcavationDesignations.TryDesignate(cell, out _, out _);
            host.WorldData.GetCell(cell, out WorldTileCellData before);
            host.ExcavationTransaction.TryCommit(
                "reset", cell, before.SolidMaterialId, out _, out _);
            Assert.That(host.RebuildWorld(), Is.True);
            Assert.That(host.ExcavationDesignations.Count, Is.Zero);
            Assert.That(host.MaterialDrops.Count, Is.EqualTo(2),
                "The two authored Soil/Stone pickup fixtures are rebuilt.");
            host.WorldData.GetCell(cell, out WorldTileCellData restored);
            Assert.That(restored.HasSolidTile, Is.True);
        }

        [Test]
        public void MultipleAntsReserveDifferentRealExcavations()
        {
            AntGameDevelopmentWorldHost host = OpenHostWithAnts();
            AntSelectionCommandController controller = host.SelectionController;
            host.ColonyJobRegistry.RemoveWhere(_ => true);
            controller.DesignateExcavationArea(
                AntGameDevelopmentWorldHost.ExcavationAreaMinimum,
                AntGameDevelopmentWorldHost.ExcavationAreaMaximum, out _);
            controller.SetAutonomousColonyWorkEnabled(true);
            Assert.That(controller.EvaluateAutonomousColonyWorkNow(
                controller.SelectableAnts[0], 1d), Is.True);
            Assert.That(controller.EvaluateAutonomousColonyWorkNow(
                controller.SelectableAnts[1], 1d), Is.True);
            Assert.That(controller.SelectableAnts[0].ExcavationOrder.Job.Id,
                Is.Not.EqualTo(controller.SelectableAnts[1].ExcavationOrder.Job.Id));
        }

        [Test]
        public void PlayerMovePreemptsRealExcavationAndPreservesJob()
        {
            AntGameDevelopmentWorldHost host = OpenHostWithAnts();
            AntSelectionCommandController controller = host.SelectionController;
            host.ColonyJobRegistry.RemoveWhere(_ => true);
            controller.TryDesignateExcavation(
                AntGameDevelopmentWorldHost.ExcavationSoilFixture, out _);
            controller.SetAutonomousColonyWorkEnabled(true);
            SelectableGridAnt ant = controller.SelectableAnts[0];
            Assert.That(controller.EvaluateAutonomousColonyWorkNow(ant, 1d), Is.True);
            ColonyJob job = ant.ExcavationOrder.Job;
            controller.Select(ant);
            Assert.That(controller.TryIssueMoveOrder(
                SampleWorldGenerator.GridDirectDestination), Is.True);
            Assert.That(job.Status, Is.EqualTo(ColonyJobStatus.Paused));
            Assert.That(job.ReservationOwner.IsValid, Is.False);
        }

        [Test]
        public void CriticalSurvivalPreemptsRealExcavation()
        {
            AntGameDevelopmentWorldHost host = OpenHostWithAnts();
            AntSelectionCommandController controller = host.SelectionController;
            host.ColonyJobRegistry.RemoveWhere(_ => true);
            controller.TryDesignateExcavation(
                AntGameDevelopmentWorldHost.ExcavationSoilFixture, out _);
            controller.SetAutonomousColonyWorkEnabled(true);
            SelectableGridAnt ant = controller.SelectableAnts[0];
            Assert.That(controller.EvaluateAutonomousColonyWorkNow(ant, 1d), Is.True);
            ColonyJob job = ant.ExcavationOrder.Job;
            ant.Record.TrySetNeedsForDevelopment(100, 0, 100, 20, out _);
            controller.SetAutonomousSurvivalEnabled(true);
            Assert.That(controller.EvaluateAutonomousSurvivalNow(ant, 2d), Is.True);
            Assert.That(job.Status, Is.EqualTo(ColonyJobStatus.Paused));
            Assert.That(ant.OrderSource, Is.EqualTo(AntOrderSource.AutonomousSurvival));
        }

        private static ExcavationJobOrder CreateOrder(
            AntGameDevelopmentWorldHost host, AntRecord ant, Vector2Int cell)
        {
            Assert.That(host.ExcavationDesignations.TryDesignate(
                cell, out ExcavationDesignation designation, out string error),
                Is.True, error);
            Vector2Int start = new(-10, SampleWorldGenerator.MainPassageY);
            Assert.That(SurvivalInteractionOrder.TryMeasureApproach(
                host.GridNavigationMap, start, cell, out Vector2Int approach,
                out _, out error), Is.True, error);
            Assert.That(ExcavationJobOrder.TryCreate(host.ColonyJobRegistry,
                host.GridNavigationMap, ant, designation, approach,
                host.ExcavationTransaction, out ExcavationJobOrder order,
                out error), Is.True, error);
            return order;
        }

        private static AntRecord CreateRecord(string id) => new(
            new AntId(id), "Test Ant", new ColonyId("test-colony"), AntCaste.Worker,
            new AntAttributes(5, 5, 5));

        private static AntGameDevelopmentWorldHost OpenHost()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AntGameDevelopmentWorldHost host = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<
                    AntGameDevelopmentWorldHost>(true)).Single();
            Assert.That(host.RebuildWorld(), Is.True);
            return host;
        }

        private static AntGameDevelopmentWorldHost OpenHostWithAnts()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Assert.That(host.SpawnOrResetSelectableGridAnts(out string error),
                Is.True, error);
            return host;
        }
    }
}
