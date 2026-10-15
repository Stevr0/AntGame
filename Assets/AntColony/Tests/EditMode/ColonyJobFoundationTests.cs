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
    public sealed class ColonyJobFoundationTests
    {
        private const string ScenePath =
            "Assets/AntColony/Scenes/AntGameDevelopment.unity";

        [TearDown]
        public void TearDown() =>
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [Test]
        public void AntsOwnIndependentWorkPriorityMatrices()
        {
            AntRecord first = CreateRecord("first");
            AntRecord second = CreateRecord("second", AntCaste.Queen);
            first.WorkPriorities.TrySet(
                AntWorkCategory.Excavation, AntWorkPriority.Highest, out _);
            Assert.That(first.WorkPriorities.Get(AntWorkCategory.Excavation),
                Is.EqualTo(AntWorkPriority.Highest));
            Assert.That(second.WorkPriorities.Get(AntWorkCategory.Excavation),
                Is.EqualTo(AntWorkPriority.Normal));
        }

        [TestCase(AntWorkPriority.Disabled)]
        [TestCase(AntWorkPriority.Highest)]
        [TestCase(AntWorkPriority.High)]
        [TestCase(AntWorkPriority.Normal)]
        [TestCase(AntWorkPriority.Low)]
        public void ValidPriorityValuesAreAccepted(AntWorkPriority priority)
        {
            Assert.That(CreateRecord("priority").WorkPriorities.TrySet(
                AntWorkCategory.Hauling, priority, out _), Is.True);
        }

        [Test]
        public void DisabledCategoryIsSkipped()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntRecord ant = CreateRecord("disabled");
            ant.WorkPriorities.TrySet(
                AntWorkCategory.Excavation, AntWorkPriority.Disabled, out _);
            ColonyJobRegistry jobs = RegistryWith(
                ("excavate", AntWorkCategory.Excavation, new Vector2Int(-10, -8)),
                ("construct", AntWorkCategory.Construction, new Vector2Int(2, -8)));
            Assert.That(Choose(host, ant, jobs, out ColonyJob selected, out _), Is.True);
            Assert.That(selected.Category, Is.EqualTo(AntWorkCategory.Construction));
        }

        [Test]
        public void LowSkillDoesNotDisqualifyOrdinaryWork()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntRecord ant = CreateRecord("unskilled");
            ColonyJobRegistry jobs = RegistryWith(
                ("excavate", AntWorkCategory.Excavation, new Vector2Int(-10, -8)));
            Assert.That(ant.Skills.Get(AntSkillId.Excavation).Level, Is.Zero);
            Assert.That(Choose(host, ant, jobs, out _, out _), Is.True);
        }

        [Test]
        public void SkillLevelDoesNotOverridePlayerPriority()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntRecord ant = CreateRecord("skilled");
            ant.TryAwardSkillExperience(
                AntSkillId.Construction, 5000, 1, 0, out _, out _);
            ant.WorkPriorities.TrySet(
                AntWorkCategory.Excavation, AntWorkPriority.Highest, out _);
            ant.WorkPriorities.TrySet(
                AntWorkCategory.Construction, AntWorkPriority.Low, out _);
            ColonyJobRegistry jobs = RegistryWith(
                ("far-excavate", AntWorkCategory.Excavation, new Vector2Int(8, -8)),
                ("near-construct", AntWorkCategory.Construction, new Vector2Int(-10, -8)));
            Choose(host, ant, jobs, out ColonyJob selected, out _);
            Assert.That(selected.Category, Is.EqualTo(AntWorkCategory.Excavation));
        }

        [Test]
        public void DuplicateActiveDesignationIsRejected()
        {
            ColonyJobRegistry jobs = new();
            Vector2Int cell = new(-10, -8);
            Assert.That(jobs.TryAdd(
                new ColonyJobId("one"), AntWorkCategory.Excavation, cell,
                out _, out _), Is.True);
            Assert.That(jobs.TryAdd(
                new ColonyJobId("two"), AntWorkCategory.Excavation, cell,
                out _, out string error), Is.False);
            Assert.That(error, Does.Contain("already exists"));
        }

        [Test]
        public void ReservationIsExclusive()
        {
            ColonyJobRegistry jobs = RegistryWith(
                ("job", AntWorkCategory.Hauling, new Vector2Int(-10, -8)));
            ColonyJob job = jobs.Jobs.Single();
            Assert.That(jobs.TryReserve(job.Id, new AntId("ant-a"), out _), Is.True);
            Assert.That(jobs.TryReserve(job.Id, new AntId("ant-b"), out _), Is.False);
        }

        [Test]
        public void HighestPriorityReachableCategoryWinsBeforeDistance()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntRecord ant = CreateRecord("priority-first");
            ant.WorkPriorities.TrySet(
                AntWorkCategory.Construction, AntWorkPriority.Highest, out _);
            ColonyJobRegistry jobs = RegistryWith(
                ("near", AntWorkCategory.Excavation, new Vector2Int(-10, -8)),
                ("far", AntWorkCategory.Construction, new Vector2Int(8, -8)));
            Choose(host, ant, jobs, out ColonyJob selected, out _);
            Assert.That(selected.Id.Value, Is.EqualTo("far"));
        }

        [Test]
        public void ClosestReachableJobWinsWithinPriority()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            ColonyJobRegistry jobs = RegistryWith(
                ("near", AntWorkCategory.Excavation, new Vector2Int(-10, -8)),
                ("far", AntWorkCategory.Excavation, new Vector2Int(8, -8)));
            Choose(host, CreateRecord("closest"), jobs, out ColonyJob selected, out _);
            Assert.That(selected.Id.Value, Is.EqualTo("near"));
        }

        [Test]
        public void NearbyUnreachableJobDoesNotBeatReachableJob()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            ColonyJobRegistry jobs = RegistryWith(
                ("blocked", AntWorkCategory.Excavation,
                    SampleWorldGenerator.GridUnreachableDestination),
                ("reachable", AntWorkCategory.Excavation, new Vector2Int(8, -8)));
            Choose(host, CreateRecord("reachable"), jobs, out ColonyJob selected, out _);
            Assert.That(selected.Id.Value, Is.EqualTo("reachable"));
        }

        [Test]
        public void EqualCostUsesDeterministicCellTieBreaker()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntRecord ant = CreateRecord("tie");
            ColonyJobRegistry jobs = RegistryWith(
                ("right", AntWorkCategory.Excavation, new Vector2Int(-6, -8)),
                ("left", AntWorkCategory.Excavation, new Vector2Int(-10, -8)));
            Choose(host, ant, jobs, out ColonyJob selected, out _);
            Assert.That(selected.TargetCell.x, Is.EqualTo(-10));
        }

        [Test]
        public void MultipleAntsClaimDifferentJobs()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntSelectionCommandController controller = host.SelectionController;
            controller.SetAutonomousColonyWorkEnabled(true);
            SelectableGridAnt first = controller.SelectableAnts[0];
            SelectableGridAnt second = controller.SelectableAnts[1];
            Assert.That(controller.EvaluateAutonomousColonyWorkNow(first, 1), Is.True);
            Assert.That(controller.EvaluateAutonomousColonyWorkNow(second, 1), Is.True);
            Assert.That(first.JobOrder.Job.Id, Is.Not.EqualTo(second.JobOrder.Job.Id));
        }

        [Test]
        public void SyntheticOrderUsesValidAdjacentWorkPosition()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            CreateOrder(host, CreateRecord("adjacent"), out SyntheticColonyJobOrder order,
                out _);
            Assert.That(SurvivalInteractionOrder.IsValidAdjacentInteractionAnchor(
                host.GridNavigationMap, order.ApproachCell, order.Job.TargetCell), Is.True);
        }

        [Test]
        public void WorkProgressAndSkillExperienceAdvanceDeterministically()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntRecord ant = CreateRecord("progress");
            CreateOrder(host, ant, out SyntheticColonyJobOrder order, out _);
            Assert.That(order.TryBegin(order.ApproachCell), Is.True);
            order.Advance(1, 1);
            Assert.That(order.Job.Progress, Is.EqualTo(0.25).Within(0.0001));
            Assert.That(ant.Skills.Get(AntSkillId.Excavation).TotalExperience,
                Is.EqualTo(5).Within(0.0001));
        }

        [Test]
        public void RestedMultiplierIncreasesSyntheticProgress()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntRecord normal = CreateRecord("normal");
            AntRecord rested = CreateRecord("rested");
            rested.TryRest(0.1, out _);
            CreateOrder(host, normal, out SyntheticColonyJobOrder normalOrder, out _);
            CreateOrder(host, rested, out SyntheticColonyJobOrder restedOrder, out _,
                new Vector2Int(2, -8));
            normalOrder.TryBegin(normalOrder.ApproachCell);
            restedOrder.TryBegin(restedOrder.ApproachCell);
            normalOrder.Advance(1, 1);
            restedOrder.Advance(1, 1);
            Assert.That(restedOrder.Job.Progress, Is.GreaterThan(normalOrder.Job.Progress));
        }

        [Test]
        public void InterruptedProgressPersistsAndReservationReleases()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntRecord ant = CreateRecord("interrupt");
            CreateOrder(host, ant, out SyntheticColonyJobOrder order, out _);
            order.TryBegin(order.ApproachCell);
            order.Advance(1, 1);
            order.Cancel("Safe boundary interruption.");
            Assert.That(order.Job.Progress, Is.GreaterThan(0));
            Assert.That(order.Job.Status, Is.EqualTo(ColonyJobStatus.Paused));
            Assert.That(order.Job.ReservationOwner.IsValid, Is.False);
        }

        [Test]
        public void CriticalSurvivalInterruptsWorkButOrdinaryNeedDoesNot()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt ant = controller.SelectableAnts[0];
            controller.SetAutonomousColonyWorkEnabled(true);
            controller.EvaluateAutonomousColonyWorkNow(ant, 1);
            ColonyJob work = ant.JobOrder.Job;
            ant.Record.TrySetNeedsForDevelopment(100, 40, 100, 20, out _);
            controller.SetAutonomousSurvivalEnabled(true);
            Assert.That(controller.EvaluateAutonomousSurvivalNow(ant, 2), Is.False);
            Assert.That(ant.OrderSource, Is.EqualTo(AntOrderSource.AutonomousWork));

            ant.Record.TrySetNeedsForDevelopment(100, 0, 100, 20, out _);
            Assert.That(controller.EvaluateAutonomousSurvivalNow(ant, 3), Is.True);
            Assert.That(ant.OrderSource, Is.EqualTo(AntOrderSource.AutonomousSurvival));
            Assert.That(work.Status, Is.EqualTo(ColonyJobStatus.Paused));
        }

        [Test]
        public void PlayerOrderPreemptsWorkAndPreservesJob()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt ant = controller.SelectableAnts[0];
            controller.SetAutonomousColonyWorkEnabled(true);
            controller.EvaluateAutonomousColonyWorkNow(ant, 1);
            ColonyJob job = ant.JobOrder.Job;
            controller.Select(ant);
            Assert.That(controller.TryIssueMoveOrder(
                SampleWorldGenerator.GridDirectDestination), Is.True);
            Assert.That(ant.OrderSource, Is.EqualTo(AntOrderSource.Player));
            Assert.That(job.Status, Is.EqualTo(ColonyJobStatus.Paused));
        }

        [Test]
        public void CancelledJobCannotContinueOrBeClaimed()
        {
            ColonyJobRegistry jobs = RegistryWith(
                ("cancel", AntWorkCategory.Excavation, new Vector2Int(-10, -8)));
            ColonyJob job = jobs.Jobs.Single();
            jobs.TryCancel(job.Id, "test", out _);
            Assert.That(jobs.TryReserve(job.Id, new AntId("ant"), out _), Is.False);
            Assert.That(job.Status, Is.EqualTo(ColonyJobStatus.Cancelled));
        }

        [TestCase(AntLifecycleStatus.Inactive)]
        [TestCase(AntLifecycleStatus.Deceased)]
        public void NonActiveAntReleasesWork(AntLifecycleStatus lifecycle)
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt ant = controller.SelectableAnts[0];
            controller.SetAutonomousColonyWorkEnabled(true);
            controller.EvaluateAutonomousColonyWorkNow(ant, 1);
            ColonyJob job = ant.JobOrder.Job;
            if (lifecycle == AntLifecycleStatus.Inactive) ant.Record.MarkInactive();
            else ant.Record.MarkDeceased();
            controller.EvaluateAutonomousColonyWorkNow(ant, 2);
            Assert.That(job.ReservationOwner.IsValid, Is.False);
            Assert.That(ant.HasActiveOrder, Is.False);
        }

        [Test]
        public void TerrainMutationBlocksWorkAndPreservesProgress()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntRecord ant = CreateRecord("terrain");
            CreateOrder(host, ant, out SyntheticColonyJobOrder order, out _);
            order.TryBegin(order.ApproachCell);
            order.Advance(1, 1);
            Assert.That(host.ToggleForegroundSolid(order.ApproachCell), Is.True);
            order.Advance(1, 2);
            Assert.That(order.Job.Status, Is.EqualTo(ColonyJobStatus.Blocked));
            Assert.That(order.Job.Progress, Is.GreaterThan(0));
        }

        [Test]
        public void CandidateEvaluationHasExplicitBound()
        {
            Assert.That(ColonyJobSelectionService.MaximumCandidatesPerEvaluation,
                Is.EqualTo(32));
        }

        [Test]
        public void ReachableJobAfterThirtyTwoUnreachableJobsIsEventuallySelected()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            ColonyJobRegistry jobs = RegistryWithUnreachableHaulingJobs(32);
            AddHaulingJob(jobs, "reachable-33", new Vector2Int(-10, -8));
            ColonyJobSelectionService selector = new();

            Assert.That(TryChooseOnce(host, CreateRecord("page-33"), jobs, selector,
                out _, out _, out string firstError), Is.False);
            Assert.That(selector.LastEvaluationInProgress, Is.True, firstError);
            Assert.That(selector.LastDiagnostics.ExaminedThisEvaluation, Is.EqualTo(32));
            Assert.That(selector.LastDiagnostics.EligibleCandidatesSeen, Is.EqualTo(32));
            Assert.That(selector.LastDiagnostics.UnreachableRejected, Is.EqualTo(32));
            Assert.That(selector.LastDiagnostics.HasPendingReachableCandidate, Is.False);

            Assert.That(TryChooseOnce(host, CreateRecord("page-33"), jobs, selector,
                out ColonyJob selected, out _, out string secondError), Is.True,
                secondError);
            Assert.That(selected.Id.Value, Is.EqualTo("reachable-33"));
            Assert.That(selector.LastDiagnostics.ReachableCandidatesSeen, Is.EqualTo(1));
            Assert.That(selector.LastDiagnostics.SelectedJobId, Is.EqualTo(selected.Id));
        }

        [Test]
        public void CompletePassPreservesHigherPriorityBeforeNearerLowerPriority()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            ColonyJobRegistry jobs = new();
            AddHaulingJob(jobs, "near-low", new Vector2Int(-10, -8));
            for (int index = 1; index < 32; index++)
                AddHaulingJob(jobs, $"blocked-{index:00}",
                    SampleWorldGenerator.GridUnreachableDestination);
            Assert.That(jobs.TryAdd(new ColonyJobId("far-high"),
                AntWorkCategory.Construction, new Vector2Int(8, -8),
                out _, out string addError), Is.True, addError);
            AntRecord ant = CreateRecord("priority-pages");
            ant.WorkPriorities.TrySet(
                AntWorkCategory.Hauling, AntWorkPriority.Low, out _);
            ant.WorkPriorities.TrySet(
                AntWorkCategory.Construction, AntWorkPriority.Highest, out _);

            ColonyJob selected = ChooseUntilComplete(
                host, ant, jobs, new ColonyJobSelectionService(), 2);
            Assert.That(selected.Id.Value, Is.EqualTo("far-high"));
        }

        [Test]
        public void InsertionDuringPassAppendsWithoutRestartingOrSkippingNewWork()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            ColonyJobRegistry jobs = RegistryWithUnreachableHaulingJobs(40);
            ColonyJobSelectionService selector = new();
            AntRecord ant = CreateRecord("insert-reset");
            Assert.That(TryChooseOnce(host, ant, jobs, selector,
                out _, out _, out _), Is.False);

            AddHaulingJob(jobs, "inserted-reachable", new Vector2Int(-10, -8));
            Assert.That(TryChooseOnce(host, ant, jobs, selector,
                out ColonyJob selected, out _, out string error), Is.True, error);
            Assert.That(selected.Id.Value, Is.EqualTo("inserted-reachable"));
        }

        [Test]
        public void RemovalDuringPassRestartsCursorWithoutSkippingShiftedWork()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            ColonyJobRegistry jobs = RegistryWithUnreachableHaulingJobs(32);
            AddHaulingJob(jobs, "reachable-after-remove", new Vector2Int(-10, -8));
            ColonyJobSelectionService selector = new();
            AntRecord ant = CreateRecord("remove-reset");
            Assert.That(TryChooseOnce(host, ant, jobs, selector,
                out _, out _, out _), Is.False);

            jobs.RemoveWhere(job => job.Id.Value == "blocked-00");
            Assert.That(TryChooseOnce(host, ant, jobs, selector,
                out ColonyJob selected, out _, out string error), Is.True, error);
            Assert.That(selected.Id.Value, Is.EqualTo("reachable-after-remove"));
        }

        [Test]
        public void CompletionDuringPassIsSkippedWithoutInvalidatingCursor()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            ColonyJobRegistry jobs = RegistryWithUnreachableHaulingJobs(32);
            AddHaulingJob(jobs, "completed-before-page", new Vector2Int(-10, -8));
            AddHaulingJob(jobs, "still-available", new Vector2Int(8, -8));
            ColonyJobSelectionService selector = new();
            AntRecord ant = CreateRecord("completion-skip");
            Assert.That(TryChooseOnce(host, ant, jobs, selector,
                out _, out _, out _), Is.False);

            AntId owner = new("other-ant");
            Assert.That(jobs.TryReserve(
                new ColonyJobId("completed-before-page"), owner, out _), Is.True);
            Assert.That(jobs.TryBegin(
                new ColonyJobId("completed-before-page"), owner, out _), Is.True);
            Assert.That(jobs.TryAddProgress(
                new ColonyJobId("completed-before-page"), owner, 1d,
                out _, out _), Is.True);

            Assert.That(TryChooseOnce(host, ant, jobs, selector,
                out ColonyJob selected, out _, out string error), Is.True, error);
            Assert.That(selected.Id.Value, Is.EqualTo("still-available"));
        }

        [Test]
        public void ReprioritisationDuringPassRestartsFromBeginning()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            ColonyJobRegistry jobs = RegistryWithUnreachableHaulingJobs(40);
            AddHaulingJob(jobs, "construction", new Vector2Int(-10, -8),
                AntWorkCategory.Construction);
            ColonyJobSelectionService selector = new();
            AntRecord ant = CreateRecord("priority-reset");
            Assert.That(TryChooseOnce(host, ant, jobs, selector,
                out _, out _, out _), Is.False);

            ant.WorkPriorities.TrySet(
                AntWorkCategory.Construction, AntWorkPriority.Highest, out _);
            Assert.That(TryChooseOnce(host, ant, jobs, selector,
                out _, out _, out _), Is.False);
            Assert.That(selector.LastDiagnostics.NextIndex, Is.EqualTo(32));
            ColonyJob selected = ChooseUntilComplete(host, ant, jobs, selector, 2);
            Assert.That(selected.Id.Value, Is.EqualTo("construction"));
        }

        [Test]
        public void TwoAntsStillReceiveDifferentExclusiveJobsAcrossPages()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            ColonyJobRegistry jobs = RegistryWithUnreachableHaulingJobs(32);
            AddHaulingJob(jobs, "first-reachable", new Vector2Int(-10, -8));
            AddHaulingJob(jobs, "second-reachable", new Vector2Int(8, -8));
            ColonyJobSelectionService selector = new();
            AntRecord firstAnt = CreateRecord("paged-ant-a");
            AntRecord secondAnt = CreateRecord("paged-ant-b");

            ColonyJob first = ChooseUntilComplete(host, firstAnt, jobs, selector, 2);
            Assert.That(jobs.TryReserve(first.Id, firstAnt.Id, out _), Is.True);
            ColonyJob second = ChooseUntilComplete(host, secondAnt, jobs, selector, 2);

            Assert.That(second.Id, Is.Not.EqualTo(first.Id));
            Assert.That(jobs.TryReserve(second.Id, secondAnt.Id, out _), Is.True);
        }

        [TestCase(32, 1)]
        [TestCase(64, 2)]
        [TestCase(256, 8)]
        [TestCase(1024, 32)]
        public void ScanCostRemainsBoundedAsRegistryGrows(
            int jobCount,
            int expectedEvaluations)
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            ColonyJobRegistry jobs = new();
            for (int index = 0; index < jobCount; index++)
                AddHaulingJob(jobs, $"scale-{index:0000}", new Vector2Int(-10, -8));
            ColonyJobSelectionService selector = new();
            AntRecord ant = CreateRecord($"scale-{jobCount}");
            string finalId = $"scale-{jobCount - 1:0000}";
            int evaluations = 0;
            ColonyJob selected = null;

            while (selected == null && evaluations <= expectedEvaluations)
            {
                evaluations++;
                bool chose = TryChooseOnce(host, ant, jobs, selector,
                    job => job.Id.Value == finalId,
                    out selected, out _, out string error);
                Assert.That(selector.LastDiagnostics.ExaminedThisEvaluation,
                    Is.LessThanOrEqualTo(
                        ColonyJobSelectionService.MaximumCandidatesPerEvaluation));
                if (!chose)
                    Assert.That(selector.LastEvaluationInProgress, Is.True, error);
            }

            Assert.That(evaluations, Is.EqualTo(expectedEvaluations));
            Assert.That(selected?.Id.Value, Is.EqualTo(finalId));
        }

        [Test]
        public void EqualCostSelectionIsRepeatableAcrossInsertionOrders()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            ColonyJobRegistry forward = RegistryWith(
                ("right", AntWorkCategory.Hauling, new Vector2Int(-6, -8)),
                ("left", AntWorkCategory.Hauling, new Vector2Int(-10, -8)));
            ColonyJobRegistry reverse = RegistryWith(
                ("left", AntWorkCategory.Hauling, new Vector2Int(-10, -8)),
                ("right", AntWorkCategory.Hauling, new Vector2Int(-6, -8)));

            Assert.That(Choose(host, CreateRecord("forward"), forward,
                out ColonyJob forwardChoice, out _), Is.True);
            Assert.That(Choose(host, CreateRecord("reverse"), reverse,
                out ColonyJob reverseChoice, out _), Is.True);
            Assert.That(forwardChoice.Id.Value, Is.EqualTo("left"));
            Assert.That(reverseChoice.Id.Value, Is.EqualTo("left"));
        }

        [Test]
        public void WorldResetRestoresSyntheticFixturesAndPriorityDefaults()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            SelectableGridAnt ant = host.SelectionController.SelectableAnts[0];
            ant.Record.WorkPriorities.TrySet(
                AntWorkCategory.Excavation, AntWorkPriority.Disabled, out _);
            host.ColonyJobRegistry.TryCancel(
                host.ColonyJobRegistry.Jobs.First().Id, "test", out _);
            Assert.That(host.RebuildWorld(), Is.True);
            Assert.That(host.SpawnOrResetSelectableGridAnts(out _), Is.True);
            Assert.That(host.ColonyJobRegistry.Count, Is.EqualTo(5));
            Assert.That(host.SelectionController.SelectableAnts[0].Record.WorkPriorities.Get(
                AntWorkCategory.Excavation), Is.EqualTo(AntWorkPriority.Normal));
        }

        private static bool Choose(
            AntGameDevelopmentWorldHost host,
            AntRecord ant,
            ColonyJobRegistry jobs,
            out ColonyJob selected,
            out Vector2Int approach)
        {
            Vector2Int start = WorldTileCoordinates.WorldPositionToCell(
                host.SelectableGridAnts[0].Position);
            return new ColonyJobSelectionService().TryChoose(
                ant, jobs, host.GridNavigationMap, start,
                out selected, out approach, out _);
        }

        private static bool TryChooseOnce(
            AntGameDevelopmentWorldHost host,
            AntRecord ant,
            ColonyJobRegistry jobs,
            ColonyJobSelectionService selector,
            out ColonyJob selected,
            out Vector2Int approach,
            out string error) =>
            TryChooseOnce(host, ant, jobs, selector, null,
                out selected, out approach, out error);

        private static bool TryChooseOnce(
            AntGameDevelopmentWorldHost host,
            AntRecord ant,
            ColonyJobRegistry jobs,
            ColonyJobSelectionService selector,
            System.Predicate<ColonyJob> eligibility,
            out ColonyJob selected,
            out Vector2Int approach,
            out string error)
        {
            Vector2Int start = WorldTileCoordinates.WorldPositionToCell(
                host.SelectableGridAnts[0].Position);
            return selector.TryChoose(ant, jobs, host.GridNavigationMap, start,
                eligibility, out selected, out approach, out error);
        }

        private static ColonyJob ChooseUntilComplete(
            AntGameDevelopmentWorldHost host,
            AntRecord ant,
            ColonyJobRegistry jobs,
            ColonyJobSelectionService selector,
            int maximumEvaluations)
        {
            for (int evaluation = 0; evaluation < maximumEvaluations; evaluation++)
            {
                if (TryChooseOnce(host, ant, jobs, selector,
                        out ColonyJob selected, out _, out string error))
                    return selected;
                Assert.That(selector.LastEvaluationInProgress, Is.True, error);
            }
            Assert.Fail($"No job was selected within {maximumEvaluations} evaluations.");
            return null;
        }

        private static void CreateOrder(
            AntGameDevelopmentWorldHost host,
            AntRecord ant,
            out SyntheticColonyJobOrder order,
            out ColonyJobRegistry jobs,
            Vector2Int? target = null)
        {
            jobs = RegistryWith(("job-" + ant.Id.Value, AntWorkCategory.Excavation,
                target ?? new Vector2Int(-10, -8)));
            Assert.That(Choose(host, ant, jobs, out ColonyJob job,
                out Vector2Int approach), Is.True);
            Assert.That(SyntheticColonyJobOrder.TryCreate(
                jobs, host.GridNavigationMap, ant, job, approach,
                out order, out string error), Is.True, error);
        }

        private static ColonyJobRegistry RegistryWith(
            params (string id, AntWorkCategory category, Vector2Int cell)[] definitions)
        {
            ColonyJobRegistry jobs = new();
            foreach ((string id, AntWorkCategory category, Vector2Int cell) in definitions)
                Assert.That(jobs.TryAdd(
                    new ColonyJobId(id), category, cell, out _, out string error),
                    Is.True, error);
            return jobs;
        }

        private static ColonyJobRegistry RegistryWithUnreachableHaulingJobs(int count)
        {
            ColonyJobRegistry jobs = new();
            for (int index = 0; index < count; index++)
                AddHaulingJob(jobs, $"blocked-{index:00}",
                    SampleWorldGenerator.GridUnreachableDestination);
            return jobs;
        }

        private static void AddHaulingJob(
            ColonyJobRegistry jobs,
            string id,
            Vector2Int cell,
            AntWorkCategory category = AntWorkCategory.Hauling)
        {
            Assert.That(jobs.TryAdd(new ColonyJobId(id), category, cell,
                out _, out string error), Is.True, error);
        }

        private static AntRecord CreateRecord(
            string id,
            AntCaste caste = AntCaste.Worker) =>
            new(new AntId(id), "Test Ant", new ColonyId("test-colony"), caste,
                new AntAttributes(5, 5, 5));

        private static AntGameDevelopmentWorldHost OpenHost()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AntGameDevelopmentWorldHost host = scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<AntGameDevelopmentWorldHost>(true))
                .Single();
            Assert.That(host.RebuildWorld(), Is.True);
            Assert.That(host.SpawnOrResetSelectableGridAnts(out string message),
                Is.True, message);
            return host;
        }
    }
}
